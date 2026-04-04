using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using LoyaltyService.API.Middleware;
using LoyaltyService.Application;
using LoyaltyService.Infrastructure;
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
            .Enrich.WithProperty("ServiceName", "LoyaltyService")
            .WriteTo.Console()
            .WriteTo.File("logs/loyalty-service-.log", rollingInterval: RollingInterval.Day);

        var esUrl = ctx.Configuration["ELASTICSEARCH_URL"];
        if (!string.IsNullOrEmpty(esUrl))
        {
            cfg.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(esUrl))
            {
                IndexFormat = "epcl-loyalty-{0:yyyy.MM.dd}",
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7
            });
        }
    });

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ApplicationAssemblyMarker>());
    builder.Services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();
    builder.Services.AddFluentValidationAutoValidation();

    var jwtSecret = builder.Configuration["JWT_SECRET_KEY"] ?? "EPCL-SuperSecret-JWT-Key-For-Development-Only-2024!@#";
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["JWT_ISSUER"] ?? "EPCL",
                ValidAudience = builder.Configuration["JWT_AUDIENCE"] ?? "EPCL-Users",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "EPCL Loyalty Service", Version = "v1" });
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
            "Server=localhost\\SQLEXPRESS;Database=EPCL_Loyalty;Trusted_Connection=True;TrustServerCertificate=True;");

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

    Log.Information("EPCL Loyalty Service starting on {Urls}", string.Join(", ", app.Urls));
    app.Run();
}
catch (Exception ex) { Log.Fatal(ex, "Loyalty Service terminated unexpectedly"); }
finally { Log.CloseAndFlush(); }
