using DataServer.Api.Hubs;
using DataServer.Api.Middleware;
using DataServer.Api.Services;
using DataServer.Application.Configuration;
using DataServer.Application.Interfaces;
using DataServer.Application.Services;
using DataServer.Connectors.Blockchain;
using DataServer.Infrastructure.Blockchain;

var builder = WebApplication.CreateBuilder(args);

builder
    .Configuration.AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

builder.Services.AddSignalR();
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

app.UseGlobalExceptionHandler();

app.MapHub<BlockchainHub>("/blockchain");

app.Run();
