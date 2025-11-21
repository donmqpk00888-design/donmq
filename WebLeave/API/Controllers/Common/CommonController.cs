
using System.Net;
using API._Services.Interfaces.Common;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Common
{
    public class CommonController : ApiController
    {
        private readonly ICommonService _serviceCommon;
        public CommonController(ICommonService serviceCommon)
        {
            _serviceCommon = serviceCommon;
        }

        [HttpGet("GetCompanys")]
        public async Task<IActionResult> GetCompanys()
        {
            var data = await _serviceCommon.GetCompanys();
            return Ok(data);
        }

        [HttpGet("GetAreas")]
        public async Task<IActionResult> GetAreas()
        {
            var data = await _serviceCommon.GetAreas();
            return Ok(data);
        }

        [HttpGet("GetBuildings")]
        public async Task<IActionResult> GetBuildings()
        {
            var data = await _serviceCommon.GetBuildings();
            return Ok(data);
        }

        [HttpGet("GetDepartments")]
        public async Task<IActionResult> GetDepartments()
        {
            var data = await _serviceCommon.GetDepartments();
            return Ok(data);
        }

        [HttpGet("GetCommentArchives")]
        public async Task<IActionResult> GetCommentArchives()
        {
            var data = await _serviceCommon.GetCommentArchives();
            return Ok(data);
        }

        [HttpGet("GetBrowserInfo")]
        public async Task<IActionResult> GetBrowserInfo([FromQuery] string ipLocal, [FromQuery] string username)
        {
            var data = await _serviceCommon.GetLoginDetectInfo(username);
            IPAddress remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;
            if (remoteIpAddress != null)
            {
                if (remoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    remoteIpAddress = Dns.GetHostEntry(remoteIpAddress).AddressList
                        .First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                }
                data.IpLocal = remoteIpAddress.ToString();
            }
            return Ok(data);
        }
        [HttpGet("GetSeverTime")]
        public IActionResult SeverTime()
        {
            var _serverTime = _serviceCommon.GetServerTime();
            return Ok(_serverTime);
        }
    }
}