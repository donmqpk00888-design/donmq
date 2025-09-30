
using API._Services.Interfaces.SalaryReport;
using API.DTOs.SalaryReport;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.AttendanceMaintenance
{
  public class C_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport : APIController
  {
    private readonly I_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport _service;
    public C_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport(I_7_2_22_MonthlyAdditionsAndDeductionsSummaryReport service)
    {
      _service = service;
    }

    [HttpGet("GetFactoryList")]
    public async Task<IActionResult> GetFactoryList(string Lang)
    {
      var result = await _service.GetFactoryList(Lang, roleList);
      return Ok(result);
    }

    [HttpGet("GetDropDownList")]
    public async Task<IActionResult> GetDropDownList([FromQuery] MonthlyAdditionsAndDeductionsSummaryReport_Param param)
    {
      var result = await _service.GetDropDownList(param, roleList);
      return Ok(result);
    }
    
    [HttpGet("Process")]
    public async Task<IActionResult> Process([FromQuery] MonthlyAdditionsAndDeductionsSummaryReport_Param param)
    {
      var result = await _service.Process(param, userName);
      return Ok(result);
    }
  }
}