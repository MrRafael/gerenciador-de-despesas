using Microsoft.AspNetCore.Mvc;
using MyFinBackend.Auth;
using MyFinBackend.Dto;
using MyFinBackend.Model;
using MyFinBackend.Services;
using System.Security.Claims;

namespace MyFinBackend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [ClerkAuthorize]
    public class UsersController(IUserService userService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(string id)
        {
            var result = await userService.GetByIdAsync(id, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                ServiceError.NotFound => NotFound(),
                _ => Ok(result.Data)
            };
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> PostUser(User user)
        {
            var result = await userService.CreateAsync(user, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                ServiceError.Conflict => Conflict(),
                _ => CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result.Data)
            };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(string id, User user)
        {
            var result = await userService.UpdateAsync(id, user, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                ServiceError.NotFound => NotFound(),
                _ => NoContent()
            };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var result = await userService.DeleteAsync(id, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                ServiceError.NotFound => NotFound(),
                _ => NoContent()
            };
        }

        private string GetUserId() => User.FindFirstValue("sub")!;
    }
}
