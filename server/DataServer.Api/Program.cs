using DataServer.Api.Hubs;
using DataServer.Api.Middleware;
using DataServer.Api.Services;
using DataServer.Application.Configuration;
using DataServer.Application.Interfaces;
using DataServer.Application.Services;
using DataServer.Connectors.Blockchain;
using DataServer.Infrastructure.Blockchain;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
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
        builder.Services.AddSerilog((services, lc) => lc
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console());

    
    builder.Services.AddMemoryCache();

    builder.Services.Configure<BlockchainSettings>(
        builder.Configuration.GetSection(BlockchainSettings.SectionName)
    );

    builder.Services.AddSingleton<IWebSocketClient, WebSocketClientWrapper>();
    builder.Services.AddSingleton<IBlockchainDataClient, BlockchainDataClient>();
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
