using NLog;
using NLog.Extensions.Logging;
using rebuild;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Initializing WSUSSCN2-API Rebuild Service");

try
{
    IHost host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            // Add database context
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(GetConnectionString(hostContext.Configuration)));

            // Add MinIO client
            services.AddSingleton<IMinioClient>(provider =>
            {
                var endpoint = hostContext.Configuration["MINIO_ENDPOINT"] ?? "http://minio:9000";
                var accessKey = hostContext.Configuration["MINIO_ACCESS_KEY"] ?? "admin";
                var secretKey = hostContext.Configuration["MINIO_SECRET_KEY"] ?? "password";

                return new MinioClient()
                    .WithEndpoint(endpoint)
                    .WithCredentials(accessKey, secretKey)
                    .WithSSL(endpoint.StartsWith("https"))
                    .Build();
            });

            // Add services
            services.AddSingleton<IRebuildService, RebuildService>();

            // Add hosted service
            services.AddHostedService<RebuildWorker>();
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddNLog();
        })
        .Build();

    await host.RunAsync();
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
