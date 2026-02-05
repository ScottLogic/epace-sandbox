using DataServer.Api.Hubs;
using DataServer.Api.Middleware;
using DataServer.Api.Services;
using DataServer.Application.Configuration;
using DataServer.Application.Interfaces;
using DataServer.Application.Services;
using DataServer.Connectors.Blockchain;
using DataServer.Infrastructure.Blockchain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

// Logger established here to log program loading
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Application is starting");
    var builder = WebApplication.CreateBuilder(args);

    builder
        .Configuration.AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables();

    builder.Services.AddSignalR(options =>
    {
        options.AddFilter<HubExceptionFilter>();
    });
    builder.Services.AddSerilog(
        (services, lc) =>
            lc
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
    );

    builder.Services.AddMemoryCache();

    builder.Services.Configure<BlockchainSettings>(
        builder.Configuration.GetSection(BlockchainSettings.SectionName)
    );
    builder.Services.Configure<ConnectionManagerSettings>(
        builder.Configuration.GetSection(ConnectionManagerSettings.SectionName)
    );

    builder.Services.AddSingleton<IWebSocketClient, WebSocketClientWrapper>();
    builder.Services.AddSingleton<IBlockchainDataClient, BlockchainDataClient>();
    builder.Services.AddSingleton<IDelayProvider, TaskDelayProvider>();
    builder.Services.AddSingleton<IConnectionManager>(sp =>
    {
        var connectable = sp.GetRequiredService<IBlockchainDataClient>();
        var delayProvider = sp.GetRequiredService<IDelayProvider>();
        var settings = sp.GetRequiredService<IOptions<ConnectionManagerSettings>>().Value;
        return new ConnectionManager(connectable, delayProvider, settings);
    });
    builder.Services.AddSingleton<IBlockchainDataRepository, InMemoryBlockchainDataRepository>();
    builder.Services.AddSingleton<IBlockchainDataService, BlockchainDataService>();

    builder.Services.AddHostedService<BlockchainHubService>();

    var app = builder.Build();

    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    app.UseSerilogRequestLogging();
    app.MapHub<BlockchainHub>("/blockchain");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
