using Microsoft.AspNetCore.Mvc;
using VetSqlClient.Models.DTOs;
using VetSqlClient.Services;

namespace VetSqlClient.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController(IDbService dbService) : ControllerBase
{
    [HttpGet("{id}/trips")]
    public async Task<IActionResult> ClientTrips([FromRoute] int id)
    {
        return Ok(await dbService.GetClientDetailsAsync(id));
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientCreateDTO dto)
    {
        await dbService.CreateClientAsync(dto);
        return Created();
    }
}