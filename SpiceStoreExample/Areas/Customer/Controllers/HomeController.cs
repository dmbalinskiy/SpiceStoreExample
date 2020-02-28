using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpiceStoreExample.Data;
using SpiceStoreExample.Models;
using SpiceStoreExample.Models.ViewModels;

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

            return View(IndexViewModel);
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
