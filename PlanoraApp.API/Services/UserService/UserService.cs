using Microsoft.EntityFrameworkCore;
using PlanoraApp.API.Infrastructure;
using PlanoraApp.API.Services.UserService.Models;

namespace PlanoraApp.API.Services.UserService
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(string id);
        Task<User> UpsertUserAsync(User user);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByIdAsync(string id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User> UpsertUserAsync(User user)
        {
            var existing = await _context.Users.FindAsync(user.Id);
            if (existing != null)
            {
                existing.Email = user.Email;
                existing.DisplayName = user.DisplayName;
                existing.Country = user.Country;
                existing.LastLoginAt = DateTime.UtcNow;
                _context.Users.Update(existing);
            }
            else
            {
                user.CreatedAt = DateTime.UtcNow;
                user.LastLoginAt = DateTime.UtcNow;
                _context.Users.Add(user);
            }
            await _context.SaveChangesAsync();
            return existing ?? user;
        }
    }
}
