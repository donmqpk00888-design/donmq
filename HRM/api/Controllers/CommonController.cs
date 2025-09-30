using API._Services.Interfaces;
using API.DTOs;
using API.Helper.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class CommonController : APIController
    {
        private readonly I_Common _service;

        public CommonController(I_Common service)
        {
            _service = service;
        }
        [EncryptionHttp]
        [HttpGet("GetSystemInfo")]
        public async Task<IActionResult> GetSystemInfo()
        {
            var results = await _service.GetSystemInfo(userName);
            return Ok(results);
        }
        [HttpGet("GetPasswordReset")]
        public async Task<IActionResult> GetPasswordReset()
        {
            var results = await _service.GetPasswordReset(userName);
            return Ok(results);
        }
        [HttpGet("GetListFactoryMain")]
        public async Task<IActionResult> GetListFactoryMain([FromQuery] string language)
        {
            return Ok(await _service.GetListFactoryMain(language));
        }

        [HttpGet("GetListAttendanceOrLeave")]
        public async Task<IActionResult> GetListAttendanceOrLeave([FromQuery] string language)
        {
            return Ok(await _service.GetListAttendanceOrLeave(language));
        }

        [HttpGet("GetListWorkShiftType")]
        public async Task<IActionResult> GetListWorkShiftType([FromQuery] string language)
        {
            return Ok(await _service.GetListWorkShiftType(language));
        }

        [HttpGet("GetListReasonCode")]
        public async Task<IActionResult> GetListReasonCode([FromQuery] string language)
        {
            return Ok(await _service.GetListReasonCode(language));
        }

        [HttpGet("GetListBasicCodeListChar1")]
        public async Task<IActionResult> GetListBasicCodeListChar1([FromQuery] string language, [FromQuery] string typeSeq, [FromQuery] int kind, [FromQuery] string inputChar)
        {
            return Ok(await _service.GetListBasicCodeListChar1(language, typeSeq, kind, inputChar));
        }

        [HttpGet("GetListDepartment")]
        public async Task<IActionResult> GetListDepartment([FromQuery] string language, [FromQuery] string factory)
        {
            return Ok(await _service.GetListDepartment(language, factory));
        }

        [HttpGet("GetListAccountAdd")]
        public async Task<IActionResult> GetListAccountAdd([FromQuery] string language)
        {
            return Ok(await _service.GetListAccountAdd(userName, language));
        }
        [HttpGet("GetListEmployeeAdd")]
        public async Task<IActionResult> GetListEmployeeAdd(string factory, string language)
        {
            return Ok(await _service.GetListEmployeeAdd(factory, language));
        }

        [HttpGet("GetListPermissionGroup")]
        public async Task<IActionResult> GetListPermissionGroup([FromQuery] string language)
        {
            return Ok(await _service.GetListPermissionGroup(language));
        }

        [HttpGet("GetListSalaryItems")]
        public async Task<IActionResult> GetListSalaryItems([FromQuery] string language)
        {
            return Ok(await _service.GetListSalaryItems(language));
        }
    }
}