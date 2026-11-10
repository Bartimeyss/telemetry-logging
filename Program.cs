using Elastic.Channels;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        ".logs/start-host-log-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Начало запуска приложения");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddRazorPages();

    builder.Host.UseSerilog((hostingContext, services, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(hostingContext.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", hostingContext.HostingEnvironment.EnvironmentName)
            .WriteTo.Elasticsearch([new Uri("http://localhost:9200")], options =>
            {
                options.DataStream = new DataStreamName("logs", "telemetry-logging", "demo");
                options.BootstrapMethod = BootstrapMethod.Failure;
                options.ConfigureChannel = channelOptions =>
                {
                    channelOptions.BufferOptions = new BufferOptions
                    {
                        ExportMaxConcurrency = 10
                    };
                };
            }, transport =>
            {
                transport.Authentication(new BasicAuthentication("elastic", "changeme"));
            })
    );

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseSerilogRequestLogging();

    app.UseRouting();
    app.UseAuthorization();
    app.MapRazorPages();

    Log.Information("Приложение успешно сконфигурировано");

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Ошибка при запуске приложения");
}
finally
{
    Log.CloseAndFlush();
}
