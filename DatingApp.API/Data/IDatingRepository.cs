using System.Collections.Generic;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;

namespace DatingApp.API.Data
{
    public interface IDatingRepository
    {
        // add T (class) method
         void Add<T>(T entity) where T:class;
         void Delete<T>(T entity) where T:class;
         Task<bool> SaveAll();
         //get all users
         Task<PagedList<User>> GetUsers(UserParams userParams);
         //get one user
         Task<User> GetUser(int id, bool isCurrentUser);
         Task<Photo> GetPhoto(int id);
         Task<Photo> GetMainPhotoForUser(int userId);
         // id user, id ng like user
         Task<Like> GetLike(int userId, int recipientId);
         Task<Message> GetMessage(int id);
         // inbox, outbox, unread message
         Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams);
         // conversation between 2 users
         Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId);
    }
}