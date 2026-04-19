using Microsoft.AspNetCore.Mvc;
using MyFinBackend.Auth;
using MyFinBackend.Dto;
using MyFinBackend.Services;
using System.Security.Claims;

namespace MyFinBackend.Controller
{
    [Route("api/groups")]
    [ApiController]
    [ClerkAuthorize]
    public class GroupSplitSummaryController(ISplitCalculatorService calculator, IMonthCloseService monthClose) : ControllerBase
    {
        [HttpGet("{groupId}/split-summary")]
        public async Task<ActionResult<List<SplitMemberResultDto>>> GetSplitSummary(int groupId, int month, int year)
        {
            var result = await calculator.CalculateAsync(groupId, month, year, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                ServiceError.NotFound => NotFound(),
                _ => Ok(result.Data)
            };
        }

        [HttpGet("{groupId}/month-close/pending")]
        public async Task<ActionResult<List<PendingMonthDto>>> GetPendingMonths(int groupId)
        {
            var result = await monthClose.GetPendingMonthsAsync(groupId, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                _ => Ok(result.Data)
            };
        }

        [HttpGet("{groupId}/month-close/{month}/{year}")]
        public async Task<ActionResult<MonthCloseStatusDto>> GetMonthStatus(int groupId, int month, int year)
        {
            var result = await monthClose.GetStatusAsync(groupId, month, year, GetUserId());
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                _ => Ok(result.Data)
            };
        }

        [HttpPost("{groupId}/month-close/{month}/{year}/confirm")]
        public async Task<ActionResult<bool>> Confirm(int groupId, int month, int year)
        {
            var result = await monthClose.ConfirmAsync(groupId, GetUserId(), month, year);
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                ServiceError.Conflict => Conflict(),
                _ => Ok(result.Data)
            };
        }

        [HttpDelete("{groupId}/month-close/{month}/{year}/confirm")]
        public async Task<ActionResult> Unconfirm(int groupId, int month, int year)
        {
            var result = await monthClose.UnconfirmAsync(groupId, GetUserId(), month, year);
            return result.Error switch
            {
                ServiceError.Unauthorized => Forbid(),
                ServiceError.NotFound => NotFound(),
                ServiceError.Conflict => Conflict(),
                _ => NoContent()
            };
        }

        private string GetUserId() => User.FindFirstValue("sub")!;
    }
}
