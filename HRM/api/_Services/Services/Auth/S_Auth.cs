using System.Security.Claims;
using API.Data;
using API.Dtos.Auth;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using API._Services.Interfaces.Auth;

namespace API._Services.Services.Auth

{
    public class S_Auth : BaseServices, I_Auth
    {
        private readonly IConfiguration _configuration;
        public S_Auth(IConfiguration configuration, DBContext dbContext) : base(dbContext)
        {
            _configuration = configuration;
        }

        public async Task<List<KeyValuePair<string, string>>> GetListFactory()
        {
            return await _repositoryAccessor.HRMS_Basic_Code.FindAll(x => x.Type_Seq == "2").Select(x => new KeyValuePair<string, string>(x.Code, x.Code_Name))
            .Distinct().ToListAsync();
        }
        public async Task<List<KeyValuePair<string, string>>> GetDirection()
        {
            var data = await _repositoryAccessor.HRMS_SYS_Directory
                .FindAll()
                .Select(x => new KeyValuePair<string, string>(
                    x.Directory_Code,
                    x.Directory_Name
                )).Distinct().ToListAsync();
            return data;
        }
        public async Task<List<KeyValuePair<string, string>>> GetProgram(string direction)
        {
            List<string> filterProgram = new() { "4.1.2", "4.1.3", "4.1.4", "4.1.5" };
            var data = await _repositoryAccessor.HRMS_SYS_Program
                .FindAll(x => x.Parent_Directory_Code == direction && !filterProgram.Contains(x.Program_Code))
                .Select(x => new KeyValuePair<string, string>(
                    x.Program_Code,
                    x.Program_Name
            )).Distinct().ToListAsync();
            return data;
        }
        public async Task<List<string>> GetListLangs()
        {
            var data = await _repositoryAccessor.HRMS_SYS_Language.FindAll(x => x.IsActive).Select(x => x.Language_Code.ToLower()).ToListAsync();
            return data;
        }
        public async Task<OperationResult> Login(UserLoginParam userForLogin)
        {
            var user = await _repositoryAccessor.HRMS_Basic_Account.FirstOrDefaultAsync(x => x.Account.Trim() == userForLogin.Username.Trim() && x.IsActive && x.Factory.Trim() == userForLogin.Factory.Trim());
            if (user == null)
                return new OperationResult(false);
            if (user.Password != userForLogin.Password)
                return new OperationResult(false);
            var userLogged = new UserForLoggedDTO
            {
                Id = user.Account,
                Factory = user.Factory,
                Account = user.Account,
                Name = user.Name,
            };
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userLogged.Account), };
            _repositoryAccessor.HRMS_Basic_Account_Role.FindAll(x => x.Account.Trim() == user.Account.Trim())
                .Select(x => x.Role).ToList().ForEach(role => { claims.Add(new Claim(ClaimTypes.Role, role)); });
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMonths(2),
                SigningCredentials = creds
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return new OperationResult(true, new ResultResponse() { Token = tokenHandler.WriteToken(token), User = userLogged });
        }
    }
}