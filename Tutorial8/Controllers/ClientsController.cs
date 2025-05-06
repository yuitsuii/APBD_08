using Microsoft.AspNetCore.Mvc;
using Tutorial8.Services;
using Tutorial8.Models.DTOs;
namespace Tutorial8.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientsController : ControllerBase
{
    private readonly IClientsService _clientsService;

    public ClientsController(IClientsService clientsService)
    {
        _clientsService = clientsService;
    }

    //trips for given client with stated id
    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        try
        {
            var trips = await _clientsService.GetClientTrips(id);
            return Ok(trips);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    
    //adding client
    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientDTO client)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var newId = await _clientsService.CreateClient(client);
            return CreatedAtAction(nameof(CreateClient), new { id = newId }, new { id = newId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
    
    //registring client for a trip
    [HttpPut("{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientForTrip(int id, int tripId)
    {
        try
        {
            await _clientsService.RegisterClientForTrip(id, tripId);
            return Ok(new { message = "Client registered successfully" });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
    
    //removing registry from a trip
    [HttpDelete("{id}/trips/{tripId}")]
    public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
    {
        try
        {
            await _clientsService.UnregisterClientFromTrip(id, tripId);
            return Ok(new { message = "Client unregistered from trip successfully" });
        }
        catch (Exception e)
        {
            return NotFound(new { error = e.Message });
        }
    }
    
}