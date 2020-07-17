using System.Collections.Generic;
using System.Linq;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace DatingApp.API.Data
{
    public class Seed
    {
        // private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public Seed(UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            // _context = context;
        }

        public void SeedUsers()
        {
            if (!_userManager.Users.Any())
            {
                var userData = System.IO.File.ReadAllText("Data/UserSeedData.json");
                // convert data json thanh list user
                var users = JsonConvert.DeserializeObject<List<User>>(userData);

                // define 1 list roles
                var roles = new List<Role>
                {
                    new Role{Name = "Member"},
                    new Role{Name = "Admin"},
                    new Role{Name = "Moderator"},
                    new Role{Name = "VIP"}
                };
                // add to database
                foreach (var role in roles)
                {
                    _roleManager.CreateAsync(role).Wait();
                }

                // all of the user manager methods are async
                foreach (var user in users)
                {
                    // byte[] passwordHash, passwordSalt;
                    // CreatePasswordHash("password", out passwordHash, out passwordSalt);
                    // user.PasswordHash = passwordHash;
                    // user.PasswordSalt = passwordSalt;
                    // user.UserName = user.UserName.ToLower();

                    // _context.Users.Add(user);

                    // set the photos of user already approved
                    user.Photos.SingleOrDefault().IsApproved = true;
                    // create users with password is 'password'
                    // CreateAsync la async nen can dung 'Wait()' de doi no thuc hien xong
                    _userManager.CreateAsync(user, "password").Wait();
                    // add role 'Member' for user
                    _userManager.AddToRoleAsync(user, "Member").Wait();
                }

                var adminUser = new User
                {
                    UserName = "Admin"
                };
                // return ve result
                IdentityResult result = _userManager.CreateAsync(adminUser, "namnh72").Result;
                // neu create account thanh cong
                if(result.Succeeded){
                    var admin = _userManager.FindByNameAsync("Admin").Result;
                    // add role for Admin account
                    _userManager.AddToRolesAsync(admin, new [] {"Admin", "Moderator"}).Wait();
                }
                // _context.SaveChanges();
            }

        }

        // private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        // {
        //     using (var hmac = new System.Security.Cryptography.HMACSHA512())
        //     {
        //         passwordSalt = hmac.Key;
        //         passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        //     }
        // }
    }
}