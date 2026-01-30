using data_client.Clients;
using data_client.Services;
using Marten;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarten(opts =>
{
    var connectionString = builder.Configuration.GetConnectionString("postgres");
    opts.Connection(connectionString!);
}).IntegrateWithWolverine();

builder.Host.UseWolverine(opts =>
{
    opts.Policies.AutoApplyTransactions();
});

builder.Services.Configure<BlockchainClientOptions>(
    builder.Configuration.GetSection("BlockchainClient"));
builder.Services.AddSingleton<IBlockchainClient, BlockchainClient>();
builder.Services.AddSingleton<IBlockchainService, BlockchainService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapWolverineEndpoints();

app.Run();
