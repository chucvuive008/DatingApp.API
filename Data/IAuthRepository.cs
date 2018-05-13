
using System.Threading.Tasks;
using DatingApp.API.Models;

namespace DatingApp.API.Data
{
    //Use to retrieve data from databse to deal with user Authentication
    public interface IAuthRepository
    {
         Task<User> Register(User user, string password);
         Task<User> Login(string username, string password);
         Task<bool> UserExists(string username);
    }
}