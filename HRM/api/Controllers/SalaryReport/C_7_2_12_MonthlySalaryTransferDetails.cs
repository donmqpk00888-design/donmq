using API._Services.Interfaces.SalaryReport;
using API.DTOs.SalaryReport;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.SalaryReport
{
    public class C_7_2_12_MonthlySalaryTransferDetails : APIController
    {
        private readonly I_7_2_12_MonthlySalaryTransferDetails _service;

        public C_7_2_12_MonthlySalaryTransferDetails(I_7_2_12_MonthlySalaryTransferDetails service)
        {
            _service = service;
        }

        [HttpGet("GetTotalRows")]
        public async Task<IActionResult> GetTotalRows([FromQuery] MonthlySalaryTransferDetailsParam param)
        {
            var result = await _service.GetTotalRows(param);
            return Ok(result);
        }

        [HttpGet("Download")]
        public async Task<IActionResult> Download([FromQuery] MonthlySalaryTransferDetailsParam param)
        {
            param.UserName = userName;
            var result = await _service.Download(param);
            return Ok(result);
        }

        [HttpGet("GetListFactory")]
        public async Task<IActionResult> GetListFactory(string language)
        {
            return Ok(await _service.GetListFactory(userName, language));
        }

        [HttpGet("GetListPermissionGroup")]
        public async Task<IActionResult> GetListPermissionGroup(string factory, string language)
        {
            return Ok(await _service.GetListPermissionGroup(factory, language));
        }

        [HttpGet("GetListDepartment")]
        public async Task<IActionResult> GetListDepartment(string factory, string language)
        {
            return Ok(await _service.GetListDepartment(factory, language));
        }
    }
}