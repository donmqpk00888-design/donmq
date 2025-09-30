namespace API.Dtos.Auth
{
    public class UserLoginParam
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Factory { get; set; }
    }
    public class UserForLoggedDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Factory { get; set; }
        public string Account { get; set; }
    }
    public class ResultResponse
    {
        public UserForLoggedDTO User { get; set; }
        public string Token { get; set; }
    }
}