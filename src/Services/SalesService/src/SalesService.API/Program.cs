using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SalesService.API.Middleware;
using SalesService.Application;
using SalesService.Infrastructure;
using Serilog;
using Serilog.Sinks.Elasticsearch;

// ── Bootstrap Serilog ──────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Load .env in development
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

    // ── Serilog ────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("ServiceName", "SalesService")
            .WriteTo.Console()
            .WriteTo.File("logs/sales-service-.log", rollingInterval: RollingInterval.Day);

        var esUrl = ctx.Configuration["ELASTICSEARCH_URL"];
        if (!string.IsNullOrEmpty(esUrl))
        {
            cfg.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(esUrl))
            {
                IndexFormat = "epcl-sales-{0:yyyy.MM.dd}",
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7
            });
        }
    });

    // ── Services ───────────────────────────────────────────────
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ApplicationAssemblyMarker>());
    builder.Services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();
    builder.Services.AddFluentValidationAutoValidation();

    // ── JWT ────────────────────────────────────────────────────
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

    // ── Swagger ────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "EPCL Sales Service", Version = "v1" });
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

    // ── CORS ───────────────────────────────────────────────────
    builder.Services.AddCors(opt =>
    {
        opt.AddDefaultPolicy(p => p.WithOrigins("http://localhost:4200", "http://localhost:5000")
            .AllowAnyMethod().AllowAnyHeader().AllowCredentials());
    });

    // ── Health Checks ──────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
            "Server=localhost\\SQLEXPRESS;Database=EPCL_Sales;Trusted_Connection=True;TrustServerCertificate=True;");

    var app = builder.Build();

    // ── Pipeline ───────────────────────────────────────────────
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("EPCL Sales Service starting on {Urls}", string.Join(", ", app.Urls));
    app.Run();
}
catch (Exception ex) { Log.Fatal(ex, "Sales Service terminated unexpectedly"); }
finally { Log.CloseAndFlush(); }
