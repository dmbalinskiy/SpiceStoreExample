using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceStoreExample.Data;

namespace SpiceStoreExample.ViewComponents
{
    
    public class UserNameViewComponent : ViewComponent
    {
        public UserNameViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var identity = (ClaimsIdentity)User.Identity;
            var claims = identity.FindFirst(ClaimTypes.NameIdentifier);
            var usedFromDb = await _db.ApplicationUser.FirstOrDefaultAsync(u => u.Id == claims.Value);
            return View(usedFromDb);
        }

        private readonly ApplicationDbContext _db;
    }
}
