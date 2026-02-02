using DataServer.Api.Hubs;
using DataServer.Api.Services;
using DataServer.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<IBlockchainDataService, BlockchainDataService>();
builder.Services.AddHostedService<BlockchainHubService>();

var app = builder.Build();

app.MapHub<BlockchainHub>("/blockchain");

app.Run();
