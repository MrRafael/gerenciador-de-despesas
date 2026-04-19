using Microsoft.AspNetCore.Mvc;
using MyFinBackend.Auth;
using MyFinBackend.Dto;
using MyFinBackend.Services;
using System.Security.Claims;

namespace MyFinBackend.Controller
{
    [Route("api/groups/{groupId}/split-configs")]
    [ApiController]
    [ClerkAuthorize]
    public class GroupSplitConfigController(IGroupSplitConfigService splitConfigService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<GroupSplitConfigReturnDto>>> GetByGroup(int groupId)
        {
            var result = await splitConfigService.GetByGroupIdAsync(groupId, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                _ => Ok(result.Data)
            };
        }

        [HttpPost]
        public async Task<ActionResult<GroupSplitConfigReturnDto>> Create(int groupId, CreateGroupSplitConfigDto dto)
        {
            var result = await splitConfigService.CreateAsync(groupId, GetUserId(), dto);
            return result.Error switch
            {
                ServiceError.NotFound => NotFound(),
                ServiceError.Unauthorized => Forbid(),
                ServiceError.Conflict => Conflict(),
                _ => StatusCode(StatusCodes.Status201Created, result.Data)
            };
        }

        [HttpPut("{configId}")]
        public async Task<ActionResult<GroupSplitConfigReturnDto>> Update(int groupId, int configId, UpdateGroupSplitConfigDto dto)
        {
            var result = await splitConfigService.UpdateAsync(configId, GetUserId(), dto);
            return result.Error switch
            {
                ServiceError.NotFound => NotFound(),
                ServiceError.Unauthorized => Forbid(),
                ServiceError.Conflict => Conflict(),
                _ => Ok(result.Data)
            };
        }

        [HttpDelete("{configId}")]
        public async Task<ActionResult> Delete(int groupId, int configId)
        {
            var result = await splitConfigService.DeleteAsync(configId, GetUserId());
            return result.Error switch
            {
                ServiceError.NotFound => NotFound(),
                ServiceError.Unauthorized => Forbid(),
                ServiceError.Conflict => Conflict(),
                _ => NoContent()
            };
        }

        private string GetUserId() => User.FindFirstValue("sub")!;
    }
}
