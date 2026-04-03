using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Cache.CacheManager;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

// ──────────────────────────────────────────────────────────────────
// Serilog bootstrap
// ──────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Ocelot", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("ServiceName", "EPCLGateway")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/gateway-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(
        Environment.GetEnvironmentVariable("ELASTICSEARCH_URL") ?? "http://localhost:9200"))
    {
        IndexFormat = "epcl-gateway-{0:yyyy.MM.dd}",
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
        MinimumLogEventLevel = LogEventLevel.Information
    })
    .CreateLogger();

try
{
    Log.Information("Starting EPCL API Gateway");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ── Load .env in development ─────────────────────────────────
    if (builder.Environment.IsDevelopment())
    {
        var envPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", ".env");
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

    // ── Ocelot configuration ─────────────────────────────────────
    builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

    // ── JWT Authentication ───────────────────────────────────────
    var jwtSecretKey = builder.Configuration["JWT_SECRET_KEY"]
        ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
        ?? throw new InvalidOperationException("JWT_SECRET_KEY is not configured");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer("Bearer", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["JWT_ISSUER"]
                    ?? Environment.GetEnvironmentVariable("JWT_ISSUER"),
                ValidateAudience = true,
                ValidAudience = builder.Configuration["JWT_AUDIENCE"]
                    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecretKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    Log.Warning("JWT challenge issued for {Path}", context.Request.Path);
                    return Task.CompletedTask;
                }
            };
        });

    // ── Ocelot services with CacheManager ────────────────────────
    builder.Services
        .AddOcelot(builder.Configuration)
        .AddCacheManager(x => x.WithDictionaryHandle());

    // ── CORS ─────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // ── Health check ─────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    // ──────────────────────────────────────────────────────────────
    // Pipeline
    // ──────────────────────────────────────────────────────────────
    var app = builder.Build();

    // Correlation ID middleware — inject before Ocelot processes
    app.Use(async (context, next) =>
    {
        const string header = "X-Correlation-ID";
        if (!context.Request.Headers.ContainsKey(header))
            context.Request.Headers[header] = Guid.NewGuid().ToString();

        var correlationId = context.Request.Headers[header].ToString();
        context.Response.Headers[header] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            Log.Information("Gateway → {Method} {Path}", context.Request.Method, context.Request.Path);
            await next();
        }
    });

    app.UseCors();

    // Gateway info endpoint
    app.MapGet("/", () => Results.Ok(new
    {
        service = "EPCL API Gateway",
        version = "1.0.0",
        status = "running",
        documentation = "All routes use /gateway/* prefix → forwarded to downstream microservices",
        routes = new[]
        {
            "/gateway/auth/*     → Identity Service   (port 5217)",
            "/gateway/users/*    → Identity Service   (port 5217)",
            "/gateway/stations/* → Station Service    (port 5143)",
            "/gateway/inventory/*→ Inventory Service  (port 5134)",
            "/gateway/sales/*    → Sales Service      (port 5167)",
            "/gateway/reports/*  → Reporting Service   (port 5062)",
            "/gateway/fraud/*    → Fraud Service       (port 5237)",
            "/gateway/notifications/* → Notification Service (port 5037)",
            "/gateway/audit/*    → Audit Service       (port 5268)",
            "/gateway/loyalty/*  → Loyalty Service     (port 5192)",
            "/gateway/fleet/*    → Sales Service       (port 5167)"
        }
    }));

    app.MapHealthChecks("/health");

    // Ocelot takes over routing
    await app.UseOcelot();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EPCL API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
