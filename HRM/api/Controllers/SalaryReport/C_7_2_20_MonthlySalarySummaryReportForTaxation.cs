using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API._Services.Interfaces.SalaryReport;
using API.DTOs.SalaryReport;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.SalaryReport
{
    public class C_7_2_20_MonthlySalarySummaryReportForTaxation : APIController
    {
        private readonly I_7_2_20_MonthlySalarySummaryReportForTaxation _service;

        public C_7_2_20_MonthlySalarySummaryReportForTaxation(I_7_2_20_MonthlySalarySummaryReportForTaxation service)
        {
            _service = service;
        }

        [HttpGet("GetTotalRows")]
        public async Task<IActionResult> GetTotalRows([FromQuery] MonthlySalarySummaryReportForTaxation_Param param)
        {
            var result = await _service.GetTotalRows(param);
            return Ok(result);
        }

        [HttpGet("DownloadFileExcel")]
        public async Task<IActionResult> DownloadFileExcel([FromQuery] MonthlySalarySummaryReportForTaxation_Param param)
        {
            var result = await _service.DownloadFileExcel(param, userName);
            return Ok(result);
        }

        [HttpGet("GetListFactory")]
        public async Task<IActionResult> GetListFactory(string language)
        {
            return Ok(await _service.GetListFactory(language,userName));
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