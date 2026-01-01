using CoreLibrary.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SharedLibrary.Extensions;
using SilkDotNetLibrary.OpenGL.Apps;
using System;
using System.Threading.Tasks;

var builder = Host.CreateDefaultBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(theme: SystemConsoleTheme.Literate)
    .CreateBootstrapLogger();

builder.ConfigureServices((context, services) =>
{
    Log.Information("Configuring Service Provider...");
    services.AddShared();
    services.AddVeryMiniEngine(options =>
    {
        options.Title = "LearnOpenGL with Silk.NET";
        options.Width = 800;
        options.Height = 600;
    });
    //services.AddApplicationInsightsTelemetryWorkerService();
});
//.UseConsoleLifetime();

IHost host = builder.Build();
try
{
    Log.Information("Starting Host...");
    //using (host)
    //{
    //    await host.StartAsync();
    //    await host.WaitForShutdownAsync();
    //}
    using IServiceScope scope = host.Services.CreateScope();
    IApp app = scope.ServiceProvider.GetRequiredService<IApp>();
    app.Start();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;