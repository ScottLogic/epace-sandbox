using DataServer.Api.Hubs;
using DataServer.Api.Middleware;
using DataServer.Api.Services;
using DataServer.Application.Configuration;
using DataServer.Application.Interfaces;
using DataServer.Application.Logging;
using DataServer.Application.Services;
using DataServer.Connectors.Blockchain;
using DataServer.Infrastructure.Blockchain;
using Microsoft.AspNetCore.SignalR;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder
        .Configuration.AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables();

    builder.Host.UseSerilog(
        (ctx, services, cfg) =>
            cfg
                .ReadFrom.Configuration(ctx.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
    );

    builder.Services.AddSignalR(options =>
    {
        options.AddFilter<HubExceptionFilter>();
    });
    builder.Services.AddMemoryCache();

    builder.Services.Configure<BlockchainSettings>(
        builder.Configuration.GetSection(BlockchainSettings.SectionName)
    );

    builder.Services.AddScoped<IAppLogger, AppLogger>();
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
