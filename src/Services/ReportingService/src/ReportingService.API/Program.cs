using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ReportingService.API.Hubs;
using ReportingService.API.Middleware;
using ReportingService.API.Services;
using ReportingService.Application;
using ReportingService.Infrastructure;
using ReportingService.Infrastructure.Messaging;
using Serilog;
using Serilog.Sinks.Elasticsearch;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    if (builder.Environment.IsDevelopment())
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "..", ".env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
                var parts = line.Split('=', 2);
                if (parts.Length == 2) Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
            }
        }
    }

    builder.Configuration.AddEnvironmentVariables();

    builder.Host.UseSerilog((ctx, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("ServiceName", "ReportingService")
            .WriteTo.Console()
            .WriteTo.File("logs/reporting-service-.log", rollingInterval: RollingInterval.Day);

        var esUrl = ctx.Configuration["ELASTICSEARCH_URL"];
        if (!string.IsNullOrEmpty(esUrl))
        {
            cfg.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(esUrl))
            {
                IndexFormat = "epcl-reporting-{0:yyyy.MM.dd}",
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7
            });
        }
    });

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ApplicationAssemblyMarker>());
    builder.Services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();
    builder.Services.AddFluentValidationAutoValidation();

    // SignalR
    builder.Services.AddSignalR(opt =>
    {
        opt.EnableDetailedErrors = builder.Environment.IsDevelopment();
        opt.KeepAliveInterval = TimeSpan.FromSeconds(15);
        opt.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    });

    // Register SignalR notification service (bridges Infrastructure → API hubs)
    builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();

    var jwtSecret = builder.Configuration["JWT_SECRET_KEY"] ?? "EPCL-SuperSecret-JWT-Key-For-Development-Only-2024!@#";
    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["JWT_ISSUER"] ?? "EPCL",
                ValidAudience = builder.Configuration["JWT_AUDIENCE"] ?? "EPCL-Users",
                IssuerSigningKey = signingKey,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            // SignalR sends JWT via query string — extract it for hub auth
            opt.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "EPCL Reporting Service", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Bearer token", Name = "Authorization",
            In = ParameterLocation.Header, Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, [] }
        });
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
    });

    builder.Services.AddCors(opt =>
    {
        opt.AddDefaultPolicy(p => p.WithOrigins("http://localhost:4200", "http://localhost:5000")
            .AllowAnyMethod().AllowAnyHeader().AllowCredentials());
    });

    builder.Services.AddHealthChecks()
        .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
            "Server=localhost\\SQLEXPRESS;Database=EPCL_Reports;Trusted_Connection=True;TrustServerCertificate=True;");

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    // SignalR Hub endpoints
    app.MapHub<AdminHub>("/hubs/admin");
    app.MapHub<DealerHub>("/hubs/dealer");

    Log.Information("EPCL Reporting Service starting with SignalR hubs: /hubs/admin, /hubs/dealer");
    app.Run();
}
catch (Exception ex) { Log.Fatal(ex, "Reporting Service terminated unexpectedly"); }
finally { Log.CloseAndFlush(); }
