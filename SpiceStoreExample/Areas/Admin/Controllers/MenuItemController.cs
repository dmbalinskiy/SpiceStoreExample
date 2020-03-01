using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceStoreExample.Data;
using SpiceStoreExample.Models.ViewModels;
using SpiceStoreExample.Utility;

namespace SpiceStoreExample.Areas.Admin.Controllers
{
    [Authorize(Roles = Consts.ManagerUser)]
    [Area("Admin")]
    public class MenuItemController : Controller
    {
        //can be directly used from get/post methods, without passing as parameters
        [BindProperty]
        public MenuItemViewModel MenuItemVM { get; set; }

        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _hostingEnv = env;
            MenuItemVM = new MenuItemViewModel()
            {
                Category = _db.Category,
                MenuItem = new Models.MenuItem()
            };
        }

        public async Task<IActionResult> Index()
        {
            var menuItem = 
                await 
                _db.MenuItem
                .Include(m => m.Category)
                .Include(m => m.Subcategory)
                .ToListAsync();
            return View(menuItem);
        }

        //GET - CREATE
        public IActionResult Create()
        {
            return View(MenuItemVM);
        }

        //POST - CREATE
        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePOST()
        {
            MenuItemVM.MenuItem.SubcategoryId = Convert.ToInt32(Request.Form["MenuItem.SubcategoryId"].ToString());
            if(!ModelState.IsValid)
            {
                return View(MenuItemVM);
            }
            _db.MenuItem.Add(MenuItemVM.MenuItem);
            await _db.SaveChangesAsync();

            //images saving section
            string webRootPath = _hostingEnv.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var menuItemFromDb = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);
            if(files.Any())
            {
                //files have been uploaded
                var uploads = Path.Combine(webRootPath, "images");
                var extension = Path.GetExtension(files[0].FileName);
                using (var fs = new FileStream(
                    Path.Combine(
                        uploads, 
                        MenuItemVM.MenuItem.Id + extension), 
                    FileMode.Create))
                {
                    files[0].CopyTo(fs);
                }
                menuItemFromDb.Image = @"\images\" + MenuItemVM.MenuItem.Id + extension;
            }
            else
            {
                //no files were uploaded, use default
                var uploads = Path.Combine(webRootPath, @"images", Consts.DefaultFoodImage);
                System.IO.File.Copy(uploads, webRootPath + @"\images\" + MenuItemVM.MenuItem.Id + ".png");
                menuItemFromDb.Image = @"\images\" + MenuItemVM.MenuItem.Id + ".png";
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        //GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if(id==null)
            {
                return NotFound();
            }

            MenuItemVM.MenuItem = await 
                _db.MenuItem
                .Include(m => m.Category)
                .Include(m => m.Subcategory)
                .SingleOrDefaultAsync(m => m.Id == id);

            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }

            MenuItemVM.Subcategory = await 
                _db.Subcategory
                .Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId)
                .ToListAsync();

            return View(MenuItemVM);
        }

        //POST - EDIT
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPOST(int? id)
        {
            if(id==null)
            {
                return NotFound();
            }

            MenuItemVM.MenuItem.SubcategoryId = Convert.ToInt32(Request.Form["MenuItem.SubcategoryId"].ToString());
            if (!ModelState.IsValid)
            {
                //assign a subcategory from appropriate db table
                MenuItemVM.Subcategory = await
                    _db.Subcategory
                    .Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId)
                    .ToListAsync();
                return View(MenuItemVM);
            }

            //images saving section
            string webRootPath = _hostingEnv.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var menuItemFromDb = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);
            if (files.Any())
            {
                //new image has been uploaded
                var uploads = Path.Combine(webRootPath, "images");
                var extension_new = Path.GetExtension(files[0].FileName);

                //Delete the original file
                var imagePath = Path.Combine(webRootPath, menuItemFromDb.Image.TrimStart('\\'));
                if(System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                using (var fs = new FileStream(
                    Path.Combine(
                        uploads,
                        MenuItemVM.MenuItem.Id + extension_new),
                    FileMode.Create))
                {
                    files[0].CopyTo(fs);
                }
                menuItemFromDb.Image = @"\images\" + MenuItemVM.MenuItem.Id + extension_new;
            }

            //update status of current menu item record and pass it to database
            menuItemFromDb.Name = MenuItemVM.MenuItem.Name;
            menuItemFromDb.Description = MenuItemVM.MenuItem.Description;
            menuItemFromDb.Price = MenuItemVM.MenuItem.Price;
            menuItemFromDb.Spicyness = MenuItemVM.MenuItem.Spicyness;
            menuItemFromDb.CategoryId = MenuItemVM.MenuItem.CategoryId;
            menuItemFromDb.SubcategoryId = MenuItemVM.MenuItem.SubcategoryId;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if(id==null)
            {
                return NotFound();
            }
            var item = await _db.MenuItem
                .Include(x => x.Category)
                .Include(x => x.Subcategory)
                .Where(x => x.Id == id)
                .SingleOrDefaultAsync();
            if(item==null)
            {
                return NotFound();
            }
            MenuItemVM = new MenuItemViewModel()
            {
                MenuItem = item,
            };
            return View(MenuItemVM);
        }

        //POST - DETAILS
        [HttpPost, ActionName("Details")]
        [ValidateAntiForgeryToken]
        public IActionResult DetailsPost(int? id)
        {
            if(id != null && ModelState.IsValid)
            {
                return RedirectToAction(nameof(Edit), new { @id = id});
            }
            return View(MenuItemVM);

        }

        //GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var menuItem = 
                await _db.MenuItem
                .Include(t => t.Category)
                .Include(t => t.Subcategory)
                .Where(i => i.Id == id).SingleOrDefaultAsync();
            if(menuItem == null)
            {
                return NotFound();
            }
            MenuItemVM = new MenuItemViewModel()
            {
                MenuItem = menuItem
            };
            return View(MenuItemVM);
        }

        //POST - DELETE
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost()
        {
            if(ModelState.IsValid && 
                MenuItemVM != null && 
                MenuItemVM.MenuItem != null)
            {
                var item = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);
                _db.MenuItem.Remove(item);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(MenuItemVM);
        }

        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostingEnv;
    }
}