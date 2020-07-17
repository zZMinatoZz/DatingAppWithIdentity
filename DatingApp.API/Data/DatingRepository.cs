using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext _context;
        public DatingRepository(DataContext context)
        {
            _context = context;
        }
        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }
        // thuc hien tim kiem trong table 'Likes' xem user (id) da like user (recipientId) chua
        public async Task<Like> GetLike(int userId, int recipientId)
        {
            return await _context.Likes.
                FirstOrDefaultAsync(u => u.LikerId == userId && u.LikeeId == recipientId);
        }

        // get main photo of user
        public async Task<Photo> GetMainPhotoForUser(int userId)
        {
            // get all photos in database with userId and then get a main photo of user
            return await _context.Photos.Where(u => u.UserId == userId).
                FirstOrDefaultAsync(p => p.IsMain);
        }

        // get photo with id
        public async Task<Photo> GetPhoto(int id)
        {
            // ham GetPhoto() cung dc su dung trong delete photo, get photo theo id
            // nen ta can ignore global query filter de van co the delete or get photo chua dc approve
            var photo = await _context.Photos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id);

            return photo;
        }

        public async Task<User> GetUser(int id, bool isCurrentUser)
        {
            // convert from IEnumerable to IQueryable
            var query = _context.Users.Include(p => p.Photos).AsQueryable();

            // if is current user then ignore global query filter
            if (isCurrentUser)
                query = query.IgnoreQueryFilters();
            // 'photos' has relationship with 'users' table
            // get user with id and all photos of it
            // like 'SELECT * FROM Users JOIN Photos ON Users.Id = Photos.UserId;'
            // var user = await _context.Users.Include(p => p.Photos).FirstOrDefaultAsync(u => u.Id == id);
            var user = await query.FirstOrDefaultAsync(u => u.Id == id);
            return user;
        }

        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = _context.Users.Include(p => p.Photos)
                .OrderByDescending(u => u.LastActive).AsQueryable();
            // get all users not match with userParams id
            users = users.Where(u => u.Id != userParams.UserId);

            users = users.Where(u => u.Gender == userParams.Gender);

            if (userParams.Likers)
            {
                var userLikers = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikers.Contains(u.Id));
            }

            if (userParams.Likees)
            {
                var userLikees = await GetUserLikes(userParams.UserId, userParams.Likers);
                // get users theo list userid trong 'userLikees'
                users = users.Where(u => userLikees.Contains(u.Id));
            }

            if (userParams.MinAge != 18 || userParams.MaxAge != 99)
            {
                // minimum date of birth
                var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
                var maxDob = DateTime.Today.AddYears(-userParams.MinAge);
                // get all users from min age to max age
                users = users.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
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

        private async Task<IEnumerable<int>> GetUserLikes(int id, bool likers)
        {
            // get user kem theo truyen data 2 collection 'Likers' va 'Likees' vao tu database
            var user = await _context.Users.Include(x => x.Likers).Include(x => x.Likees)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (likers)
            {
                return user.Likers.Where(u => u.LikeeId == id).Select(i => i.LikerId);
            }
            else
            {
                // tim kiem nhung ng dc user nay like
                return user.Likees.Where(u => u.LikerId == id).Select(i => i.LikeeId);
            }
        }

        // if have any record was saved, return true, else return false
        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
        }

        public Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams)
        {
            // ThenInclude(): message include sender (user) then sender (user) include photos
            // tuong tu 'recipient'
            // convert IEnumerable to LinQ.IQueryable de su dung cac ham filter cua linq
            var messages = _context.Messages
                .Include(u => u.Sender).ThenInclude(p => p.Photos)
                .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                .AsQueryable();

            switch (messageParams.MessageContainer)
            {
                case "Inbox":
                    // filter nhung message co recipient = id params
                    // van hien thi trong inbox message neu ng gui da xoa nhung ng nhan chua xoa
                    messages = messages.Where(u => u.RecipientId == messageParams.UserId
                        && u.RecipientDeleted == false);
                    break;
                case "Outbox":
                    // van hien thi trong outbox message neu ng nhan da xoa nhung ng gui chua xoa
                    messages = messages.Where(u => u.SenderId == messageParams.UserId
                        && u.SenderDeleted == false);
                    break;
                default:
                    // // van hien thi trong unread message neu ng gui da xoa nhung ng nhan chua xoa
                    messages = messages.Where(u => u.RecipientId == messageParams.UserId
                        && u.RecipientDeleted == false && u.IsRead == false);
                    break;

            }
            // oderby theo time message gui
            messages = messages.OrderByDescending(d => d.MessageSent);
            // return ve list messages va thong so config pagination
            return PagedList<Message>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);

        }

        public async Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId)
        {
            // get list messages luong chat giua 2 users (conversation)
            // van get va hien thi message neu recipient chua xoa, va hien thi tren conversation cua recipient
            // nguoc lai se hide message do o conversation cua sender neu sender da xoa
            var messages = await _context.Messages
                .Include(u => u.Sender).ThenInclude(p => p.Photos)
                .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                .Where(m => m.RecipientId == userId && m.RecipientDeleted == false && m.SenderId == recipientId
                    || m.RecipientId == recipientId && m.SenderId == userId && m.SenderDeleted == false)
                .OrderByDescending(m => m.MessageSent)
                .ToListAsync();

            return messages;
        }
    }
}