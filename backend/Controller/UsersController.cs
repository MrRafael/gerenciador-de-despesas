using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFinBackend.Auth;
using MyFinBackend.Database;
using MyFinBackend.Model;
using System.Security.Claims;

namespace MyFinBackend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [ClerkAuthorize]
    public class UsersController : ControllerBase
    {
        private readonly FinanceContext _dbContext;

        public UsersController(FinanceContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var contextUserId = GetUserIdFromContext();
            return await _dbContext.Users.Where(x => x.Id == contextUserId).ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            var contextUserId = GetUserIdFromContext();
            if(contextUserId != id)
            {
                return BadRequest();
            }

            var user = await _dbContext.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(string id, User user)
        {
            var contextUserId = GetUserIdFromContext();
            if (id != user.Id || contextUserId != id)
            {
                return BadRequest();
            }

            _dbContext.Entry(user).State = EntityState.Modified;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
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

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            var contextUserId = GetUserIdFromContext();
            if (contextUserId != user.Id)
            {
                return BadRequest();
            }

            _dbContext.Users.Add(user);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (UserExists(user.Id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetUser", new { id = user.Id }, user);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var contextUserId = GetUserIdFromContext();
            if (contextUserId != id)
            {
                return BadRequest();
            }

            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(string id)
        {
            return _dbContext.Users.Any(e => e.Id == id);
        }

        private string GetUserIdFromContext()
        {
            var mainClaims = User;
            var userId = mainClaims.FindFirstValue("sub");

            return userId;
        }
    }
}
