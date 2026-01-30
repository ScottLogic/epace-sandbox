using Microsoft.AspNetCore.Mvc;

namespace data_client.Controllers;

public interface IBlockchainController
{
    Task<IActionResult> GetStatusAsync();
    Task<IActionResult> StartAsync();
    Task<IActionResult> StopAsync();
}
