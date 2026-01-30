using data_client.Clients;
using data_client.Services;
using Microsoft.AspNetCore.Mvc;

namespace data_client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlockchainController : ControllerBase, IBlockchainController
{
    private readonly IBlockchainService _blockchainService;
    private readonly IBlockchainClient _blockchainClient;
    private readonly ILogger<BlockchainController> _logger;

    public BlockchainController(
        IBlockchainService blockchainService,
        IBlockchainClient blockchainClient,
        ILogger<BlockchainController> logger)
    {
        _blockchainService = blockchainService;
        _blockchainClient = blockchainClient;
        _logger = logger;
    }

    [HttpGet("status")]
    public Task<IActionResult> GetStatusAsync()
    {
        var status = new
        {
            IsConnected = _blockchainClient.IsConnected,
            Timestamp = DateTime.UtcNow
        };

        return Task.FromResult<IActionResult>(Ok(status));
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartAsync()
    {
        try
        {
            await _blockchainService.StartAsync();
            _logger.LogInformation("Blockchain service started via API");
            return Ok(new { Message = "Blockchain service started" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start blockchain service");
            return StatusCode(500, new { Error = "Failed to start blockchain service", Details = ex.Message });
        }
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopAsync()
    {
        try
        {
            await _blockchainService.StopAsync();
            _logger.LogInformation("Blockchain service stopped via API");
            return Ok(new { Message = "Blockchain service stopped" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop blockchain service");
            return StatusCode(500, new { Error = "Failed to stop blockchain service", Details = ex.Message });
        }
    }
}
