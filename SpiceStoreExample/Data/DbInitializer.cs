using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpiceStoreExample.Models;
using SpiceStoreExample.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiceStoreExample.Data
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbInitializer(ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task Initialize()
        {
            try
            {
                if(_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex)
            {

            }

            if(_db.Roles.Any(r => r.Name == Consts.ManagerUser))
            {
                return;
            }

            await _roleManager.CreateAsync(new IdentityRole(Consts.ManagerUser));
            await _roleManager.CreateAsync(new IdentityRole(Consts.KitchenUser));
            await _roleManager.CreateAsync(new IdentityRole(Consts.FrontDeskUser));
            await _roleManager.CreateAsync(new IdentityRole(Consts.CustomerEndUser));

            var result = await _userManager.CreateAsync(new ApplicationUser
            {
                UserName = "admin@gmail.com",
                Email = "admin@gmail.com",
                Name = "Admin",
                EmailConfirmed = true,
                PhoneNumber = "333-444-5555"
            }, "Admin123Admin#");

            IdentityUser user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "admin@gmail.com");
            await _userManager.AddToRoleAsync(user, Consts.ManagerUser);

        }
    }
}
