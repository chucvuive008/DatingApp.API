using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

//this implement the IDatingRepository
namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext _context;

        public DatingRepository(DataContext context)
        {
            _context = context; // get data from database
        }
        //Add object to Database
        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }
        //Remove object from Database
        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }
        //This will get the main photo of a user from database
        public Task<Photo> GetMainPhotoForUser(int userId)
        {
            //return the photo which have property isMain = true from user who have id same as userId
            return _context.Photos.Where(u => u.UserId == userId).FirstOrDefaultAsync(p => p.IsMain);
        }
        //get photo with a given id
        public Task<Photo> GetPhoto(int id)
        {
            var photo = _context.Photos.FirstOrDefaultAsync(p => p.Id == id);

            return photo;
        }
        //get user with a given id
        public async Task<User> GetUser(int id)
        {
            var user = await _context.Users.Include(p => p.Photos).FirstOrDefaultAsync(u => u.Id == id);

            return user;
        }
        //return a PageList of User each PageList will have number of user = PageSize in the userParams
        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = _context.Users.Include(p => p.Photos).OrderByDescending(u => u.LastActive).AsQueryable();

            users = users.Where(u => u.Id != userParams.UserId);

            users = users.Where(u => u.Gender == userParams.Gender);
            
            //if user change the minAge and maxAge in the fontend. 
            if (userParams.MinAge != 18 || userParams.MaxAge != 99) {
                users = users.Where(u => u.DateOfBirth.CalculateAge() >= userParams.MinAge 
                    && u.DateOfBirth.CalculateAge() <= userParams.MaxAge);
            }

            if (!string.IsNullOrEmpty(userParams.OrderBy))
            {
                switch (userParams.OrderBy)
                {
                    case "created":
                        users = users.OrderByDescending(u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending(u => u.LastActive);
                        break;
                }
            }

            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }
        //return true if anychange, false if nochange
        public async Task<bool> SaveAll()
        {
            // if database have some change it will be > 0, if database don't have any change it will = 0
            return await _context.SaveChangesAsync() > 0;
        }
    }
}