using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpiceStoreExample.Data;
using SpiceStoreExample.Models;
using SpiceStoreExample.Models.ViewModels;
using SpiceStoreExample.Utility;

namespace SpiceStoreExample.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        [BindProperty]
        public IndexViewModel IndexViewModel { get; set; }

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            IndexViewModel = new IndexViewModel
            {
                MenuItem = await _db.MenuItem
                    .Include(m => m.Category)
                    .Include(m => m.Subcategory)
                    .ToListAsync(),

                Category = await _db.Category.ToListAsync(),
                Coupon = await _db.Coupon.Where(c => c.IsActive).ToListAsync()
            };

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //claim isn't null -> user is authorized
            if(claim != null)
            {
                var cnt = _db.ShoppingCart.Where(u => u.ApplicationUserId == claim.Value).Count();
                HttpContext.Session.SetInt32(Consts.ShoppingCartCount, cnt);
            }

            return View(IndexViewModel);
        }

        [Authorize()]
        public async Task<IActionResult> Details(int id)
        {
            var menuItemFromDb = await _db.MenuItem
                .Include(t => t.Category)
                .Include(t => t.Subcategory)
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync();

            ShoppingCart cartObj = new ShoppingCart()
            {
                MenuItem = menuItemFromDb,
                MenuItemId = menuItemFromDb.Id
            };
            return View(cartObj);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(ShoppingCart CartObject)
        {
            CartObject.Id = 0;
            if(ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                CartObject.ApplicationUserId = claim.Value;

                ShoppingCart cartFromDb = await _db.ShoppingCart
                    .Where(c => c.ApplicationUserId == CartObject.ApplicationUserId &&
                    c.MenuItemId == CartObject.MenuItemId)
                    .FirstOrDefaultAsync();

                if(cartFromDb == null)
                {
                    //no previuos orders for such items
                    await _db.ShoppingCart.AddRangeAsync(CartObject);
                }
                else
                {
                    cartFromDb.Count = cartFromDb.Count + CartObject.Count;
                }
                await _db.SaveChangesAsync();
                var count = _db.ShoppingCart.Where(c => c.ApplicationUserId == CartObject.ApplicationUserId).Count();
                
                //saving data inside session
                HttpContext.Session.SetInt32(Consts.ShoppingCartCount, count);
                return RedirectToAction("Index");
            }
            else
            {
                var menuItemFromDb = await _db.MenuItem
                .Include(t => t.Category)
                .Include(t => t.Subcategory)
                .Where(m => m.Id == CartObject.MenuItemId)
                .FirstOrDefaultAsync();

                ShoppingCart cartObj = new ShoppingCart()
                {
                    MenuItem = menuItemFromDb,
                    MenuItemId = menuItemFromDb.Id
                };
                return View(cartObj);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private ApplicationDbContext _db;
        private readonly ILogger<HomeController> _logger;
    }
}
