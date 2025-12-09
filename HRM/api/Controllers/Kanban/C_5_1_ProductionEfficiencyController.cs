
using API._Services.Interfaces.Kanban;
using API.DTOs.Kanban;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Kanban
{
    public class C_5_1_ProductionEfficiencyController : APIController
    {
        private readonly I_5_1_ProductionEfficiency _service;
        public C_5_1_ProductionEfficiencyController(I_5_1_ProductionEfficiency service) => _service = service;

        [HttpGet("GetData")]
        public async Task<IActionResult> GetData([FromQuery] ProductionEfficiencyParam param)
        {
            var result = await _service.GetData(param);
            return Ok(result);
        }
    }
}