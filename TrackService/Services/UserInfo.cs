
namespace TrackService.Services
{
    public interface IUserInfo
    {
        string SecurityToken { get; set; }
    }
    public class UserInfo : IUserInfo
    {
        public string SecurityToken { get; set; }
    }
}