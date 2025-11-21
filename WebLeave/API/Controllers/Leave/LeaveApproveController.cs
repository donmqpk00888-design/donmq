
using API._Services.Interfaces.Leave;
using API.Dtos.Leave.LeaveApprove;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers.Leave
{
    public class LeaveApproveController : ApiController
    {
        private readonly ILeaveApproveService _leaveApproveService;
        public LeaveApproveController(ILeaveApproveService leaveApproveService)
        {
            _leaveApproveService = leaveApproveService;
        }

        [HttpGet("GetCategory")]
        public async Task<IActionResult> GetCategory([FromQuery] string lang)
        {
            var data = await _leaveApproveService.GetCategory(lang);
            return Ok(data);
        }

        [HttpGet("GetLeaveData")]
        public async Task<IActionResult> GetLeaveData([FromQuery] SearchLeaveApproveDto paramsSearch, [FromQuery] PaginationParam pagination, bool isPaging = true)
        {
            var data = await _leaveApproveService.GetLeaveData(paramsSearch, pagination, isPaging);
            return Ok(data);
        }

        [HttpPut("UpdateLeaveData")]
        public async Task<ActionResult> UpdateLeaveData([FromBody] List<LeaveDataApproveDto> models, [FromQuery] Boolean checkUpdate)
        {
            var result = await _leaveApproveService.UpdateLeaveData(models, checkUpdate);
            return Ok(result);
        }

        [HttpGet("ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromQuery] PaginationParam pagination, [FromQuery] SearchLeaveApproveDto dto)
        {
            var result = await _leaveApproveService.ExportExcel(pagination, dto);
            return Ok(result);
        }
    }
}