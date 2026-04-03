using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using InventoryService.API.Middleware;
using InventoryService.Application;
using InventoryService.Infrastructure;

// ── Serilog Bootstrap ────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("ServiceName", "InventoryService")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/inventory-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(
        Environment.GetEnvironmentVariable("ELASTICSEARCH_URL") ?? "http://localhost:9200"))
    {
        IndexFormat = "epcl-inventory-{0:yyyy.MM.dd}",
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
        MinimumLogEventLevel = LogEventLevel.Information
    })
    .CreateLogger();

try
{
    Log.Information("Starting EPCL Inventory Service");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ── Load .env ────────────────────────────────────────────────
    if (builder.Environment.IsDevelopment())
    {
        var envPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "..", "..", ".env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                    Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
            }
        }
    }
    builder.Configuration.AddEnvironmentVariables();

    // ── MediatR + FluentValidation ───────────────────────────────
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
    builder.Services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

    // ── Infrastructure ───────────────────────────────────────────
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── JWT Authentication ───────────────────────────────────────
    var jwtKey = builder.Configuration["JWT_SECRET_KEY"]
        ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
        ?? throw new InvalidOperationException("JWT_SECRET_KEY missing");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["JWT_ISSUER"]
                    ?? Environment.GetEnvironmentVariable("JWT_ISSUER"),
                ValidateAudience = true,
                ValidAudience = builder.Configuration["JWT_AUDIENCE"]
                    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });
    builder.Services.AddAuthorization();

    // ── Controllers ──────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "EPCL Inventory Service", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new()
        {
            Description = "JWT Bearer token", Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer", BearerFormat = "JWT"
        });
        c.AddSecurityRequirement(new()
        {
            {
                new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
        var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
    });

    // ── CORS ─────────────────────────────────────────────────────
    builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
        p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

    // ── Health Checks ────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "sqlserver", tags: ["db", "ready"]);

    // ── Pipeline ─────────────────────────────────────────────────
    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory v1"));
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Inventory Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
