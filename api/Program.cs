using System.Text.Json.Serialization;
using api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Minio;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Initializing WSUSSCN2-API");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container
    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    // Add API token authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddScheme<ApiTokenAuthenticationOptions, ApiTokenAuthenticationHandler>(
            JwtBearerDefaults.AuthenticationScheme, options => { });

    // Add authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("UpdatesRead", policy => policy.RequireClaim("permission", "updates:read"));
        options.AddPolicy("CabsRead", policy => policy.RequireClaim("permission", "cabs:read"));
        options.AddPolicy("SyncTrigger", policy => policy.RequireClaim("permission", "sync:trigger"));
        options.AddPolicy("Admin", policy => policy.RequireClaim("permission", "admin"));
    });

    // Add database context
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(GetConnectionString(builder.Configuration)));

    // Add Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = GetRedisConnectionString(builder.Configuration);
        options.InstanceName = "WSUSSCN2-API:";
    });

    // Add MinIO client
    builder.Services.AddSingleton<IMinioClient>(provider =>
    {
        var endpoint = builder.Configuration["MINIO_ENDPOINT"] ?? "http://minio:9000";
        var accessKey = builder.Configuration["MINIO_ACCESS_KEY"] ?? "admin";
        var secretKey = builder.Configuration["MINIO_SECRET_KEY"] ?? "password";

        return new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(endpoint.StartsWith("https"))
            .Build();
    });

    // Add services
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IUpdateService, UpdateService>();
    builder.Services.AddScoped<ICabService, CabService>();
    builder.Services.AddScoped<ISyncService, SyncService>();

    // Add Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "WSUSSCN2-API", Version = "v1" });
        c.AddSecurityDefinition("ApiToken", new OpenApiSecurityScheme
        {
            Description = "API Token Authentication",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "ApiToken"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiToken"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var corsOrigins = builder.Configuration["CORS_ORIGINS"]?.Split(',') ?? new[] { "http://localhost:3000" };
            policy.WithOrigins(corsOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped due to exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}

string GetConnectionString(IConfiguration configuration)
{
    var host = configuration["PG_HOST"] ?? "db";
    var port = configuration["PG_PORT"] ?? "5432";
    var database = configuration["PG_DATABASE"] ?? "wsus";
    var username = configuration["PG_USER"] ?? "postgres";
    var password = configuration["PG_PASSWORD"] ?? "postgres";

    return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}

string GetRedisConnectionString(IConfiguration configuration)
{
    var host = configuration["REDIS_HOST"] ?? "redis";
    var port = configuration["REDIS_PORT"] ?? "6379";
    var password = configuration["REDIS_PASSWORD"];

    return string.IsNullOrEmpty(password)
        ? $"{host}:{port}"
        : $"{host}:{port},password={password}";
}