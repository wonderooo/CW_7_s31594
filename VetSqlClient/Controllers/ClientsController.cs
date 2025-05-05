using Microsoft.AspNetCore.Mvc;
using VetSqlClient.Services;

namespace VetSqlClient.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController(IDbService dbService) : ControllerBase
{
    [HttpGet("{id}/trips")]
    public async Task<IActionResult> ClientTrips([FromRoute] int id)
    {
        return Ok(await dbService.GetTripsDetailsAsync());
    }
}