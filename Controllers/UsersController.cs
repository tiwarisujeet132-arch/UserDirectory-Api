using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserDirectory.Api.Data;
using UserDirectory.Api.Models;

namespace UserDirectory.Api.Controllers;

[ApiController]
[Authorize(Policy = "GoogleUser")]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll()
    {
        return Ok(await _db.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
            return NotFound(new { message = $"User with ID {id} was not found." });

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> Create(User user)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        Normalize(user);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<User>> Update(int id, User user)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (id != user.Id)
            return BadRequest(new { message = "URL id and body id must match." });

        var existing = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);

        if (existing is null)
            return NotFound(new { message = $"User with ID {id} was not found." });

        Normalize(user);

        existing.Name = user.Name;
        existing.Age = user.Age;
        existing.City = user.City;
        existing.State = user.State;
        existing.Pincode = user.Pincode;

        await _db.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
            return NotFound(new { message = $"User with ID {id} was not found." });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static void Normalize(User user)
    {
        user.Name = user.Name.Trim();
        user.City = user.City.Trim();
        user.State = user.State.Trim();
        user.Pincode = user.Pincode.Trim();
    }
}