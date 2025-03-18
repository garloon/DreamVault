using DreamVault.Api.Data;
using DreamVault.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DreamVault.Api.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class SheltersController : ControllerBase
{
    private readonly AppDbContext _context;

    public SheltersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Shelter>>> GetShelters()
    {
        return await _context.Shelters.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Shelter>> GetShelter(int id)
    {
        var shelter = await _context.Shelters.FindAsync(id);
        if (shelter == null)
        {
            return NotFound();
        }
        return shelter;
    }

    [HttpPost]
    public async Task<ActionResult<Shelter>> CreateShelter(Shelter shelter)
    {
        _context.Shelters.Add(shelter);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetShelter), new { id = shelter.Id }, shelter);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShelter(int id, Shelter shelter)
    {
        if (id != shelter.Id)
        {
            return BadRequest();
        }

        _context.Entry(shelter).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ShelterExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShelter(int id)
    {
        var shelter = await _context.Shelters.FindAsync(id);
        if (shelter == null)
        {
            return NotFound();
        }

        _context.Shelters.Remove(shelter);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("online-users/{shelterId}")]
    public async Task<IActionResult> GetOnlineUsers(int shelterId)
    {
        var onlineUsers = await _context.Users
            .Where(u => u.ShelterId == shelterId && u.IsOnline)
            .Select(u => new
            {
                Id = u.Id,
                Username = u.Username,
                Avatar = u.Avatar
            })
            .ToListAsync();

        return Ok(onlineUsers);
    }

    private bool ShelterExists(int id)
    {
        return _context.Shelters.Any(e => e.Id == id);
    }
}