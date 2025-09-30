using API.Dtos.Auth;

namespace API._Services.Interfaces.Auth
{
    [DependencyInjection(ServiceLifetime.Scoped)]
    public interface I_Auth
    {
        Task<OperationResult> Login(UserLoginParam userForLogin);
        Task<List<KeyValuePair<string, string>>> GetListFactory();
        Task<List<KeyValuePair<string, string>>> GetDirection();
        Task<List<KeyValuePair<string, string>>> GetProgram(string direction);
        Task<List<string>> GetListLangs();
    }
}