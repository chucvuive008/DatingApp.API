using System;
using System.Threading.Tasks;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DataContext _context;
        public AuthRepository(DataContext context)
        {
            _context = context;
        }
        //check the given username and password if they match databse record, we return the user, esle return null 
        public async Task<User> Login(string username, string password)
        {
            //return user which have username = given username include the photo belong to user 
            var user = await _context.Users.Include(p => p.Photos).FirstOrDefaultAsync(c => c.Username == username);

            if(user == null)
                return null;
            //run VerifyPasswordHash method to verify the given password. if the method return false, we return null
            //this method verify by compare passwordHash and PasswordSalt of the user that we get from above with
            // passwordHash and passwordSalt that we generate from the given password
            if(!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;
            
            return user;
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            //using system security HMACSHA512 with the code is the given passwordSalt
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {                
                //compute the passwordHash using the given password
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                //comnpare every letter from given passwordHash and computePasswordHash
                for(int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i]) return false;
                }
            }

            return true;
        }
        //Register new user. Save new user with given user and password to database
        public async Task<User> Register(User user, string password)
        {
            byte[] passwordHash, passwordSalt;
            //Create passwordHash and PasswordSalt to save in database
            CreatePasswordHash(password,out passwordHash,out passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            //Add the new created user to the Database
            await _context.Users.AddAsync(user);
            //save the change to Database
            await _context.SaveChangesAsync();

            return user;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        public async Task<bool> UserExists(string username)
        {
            //if any user match the given username, return true
            if(await _context.Users.AnyAsync(x => x.Username == username))
                return true;

            return false;
        }
    }
}