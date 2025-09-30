using API._Services.Interfaces.SystemMaintenance;
using API.DTOs.SystemMaintenance;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.SystemMaintenance
{
    public class C_1_1_4_DirectoryProgramLanguageSetting : APIController
    {
        private readonly I_1_1_4_DirectoryProgramLanguageSetting _services;

        public C_1_1_4_DirectoryProgramLanguageSetting(I_1_1_4_DirectoryProgramLanguageSetting services)
        {
            _services = services;
        }
        [HttpGet("GetData")]
        public async Task<IActionResult> GetData([FromQuery] PaginationParam pagination, [FromQuery] DirectoryProgramLanguageSetting_Param param)
        {
            var result = await _services.GetData(pagination, param);
            return Ok(result);
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] DirectoryProgramLanguageSetting_Data model)
        {
            var result = await _services.Add(model, userName);
            return Ok(result);
        }

        [HttpPut("Update")]
        public async Task<ActionResult> Update([FromBody] DirectoryProgramLanguageSetting_Data model)
        {
            var result = await _services.Update(model, userName);
            return Ok(result);
        }

        [HttpDelete("Delete")]
        public async Task<ActionResult> Delete(string kind, string code)
        {
            var result = await _services.Delete(kind, code);
            return Ok(result);
        }

        [HttpGet("GetDetail")]
        public async Task<IActionResult> GetDetail(string kind, string code)
        {
            return Ok(await _services.GetDetail(kind, code));
        }

        [HttpGet("GetLanguage")]
        public async Task<IActionResult> GetLanguage()
        {
            return Ok(await _services.GetLanguage());
        }

        [HttpGet("GetProgram")]
        public async Task<IActionResult> GetProgram()
        {
            return Ok(await _services.GetCodeProgram());
        }

        [HttpGet("GetDirectory")]
        public async Task<IActionResult> GetDirectory()
        {
            return Ok(await _services.GetCodeDirectory());
        }
    }
}