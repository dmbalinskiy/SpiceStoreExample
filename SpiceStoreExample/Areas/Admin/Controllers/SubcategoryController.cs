using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SpiceStoreExample.Data;
using SpiceStoreExample.Models;
using SpiceStoreExample.Models.ViewModels;

namespace SpiceStoreExample.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SubcategoryController : Controller
    {
        [TempData]
        public string StatusMessage { get; set; }

        public SubcategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        //Get INDEX
        public async Task<IActionResult> Index()
        {
            return View(await _db.Subcategory.Include(s => s.Category).ToListAsync());   
        }

        //GET - CREATE
        public async Task<IActionResult> Create()
        {
            //create and populate view model here for subcategory creation
            SubcategoryAndCategoryViewModel model = await getEmptyVm();
            return View(model);
        }

        //POST - CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubcategoryAndCategoryViewModel subcatCatVm)
        {
            if(ModelState.IsValid)
            {
                var existingSubcats =
                    _db.Subcategory.Include(s => s.Category)
                    .Where(
                        s =>
                        s.Name == subcatCatVm.Subcategory.Name &&
                        s.CategoryId == subcatCatVm.Subcategory.CategoryId);
                if(existingSubcats.Any())
                {
                    //Error - display
                    StatusMessage = $"Error : Subcategory exists under {existingSubcats.First().Category.Name}" +
                        $" category. Please use another name";
                }
                else
                {
                    _db.Subcategory.Add(subcatCatVm.Subcategory);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            //return back to view with model created from scratch
            return View(await getEmptyVm(subcatCatVm.Subcategory, StatusMessage));
        }

        private async Task<SubcategoryAndCategoryViewModel> getEmptyVm(
            Subcategory subcat = null, string msg = null)
        {
            if(subcat != null && subcat.Category == null)
            {
                subcat.Category = await _db.Category.FindAsync(subcat.CategoryId);
            }

            return new SubcategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubcategoryList = await _db.Subcategory.Select(s => s.Name)
                    .OrderBy(sn => sn).Distinct().ToListAsync(),
                Subcategory = subcat ?? new Subcategory(),
                StatusMessage = msg ?? ""
            };
        }

        [ActionName("GetSubcategory")]
        public async Task<IActionResult> GetSubcategory(int id)
        {
            List<Subcategory> subcategories = new List<Subcategory>();
            subcategories = await (from subcategory in _db.Subcategory
                                   where subcategory.CategoryId == id
                                    select subcategory).ToListAsync();
            return Json(new SelectList(subcategories, "Key", "Name"));
        }


        //GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if(id==null)
            {
                return NotFound();
            }

            var subcat = await _db.Subcategory.SingleOrDefaultAsync(m => m.Key == id);
            if(subcat == null)
            {
                return NotFound();
            }

            //create and populate view model here for subcategory editing
            return View(await getEmptyVm(subcat));
        }

        //POST - EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubcategoryAndCategoryViewModel subcatCatVm)
        {
            if (ModelState.IsValid)
            {
                var existingSubcats =
                    _db.Subcategory.Include(s => s.Category)
                    .Where(
                        s =>
                        s.Name == subcatCatVm.Subcategory.Name &&
                        s.CategoryId == subcatCatVm.Subcategory.CategoryId);
                if (existingSubcats.Any())
                {
                    //Error - display
                    StatusMessage = $"Error : Subcategory exists under {existingSubcats.First().Category.Name}" +
                        $" category. Please use another name";
                }
                else
                {
                    var subcat = await _db.Subcategory.FindAsync(subcatCatVm.Subcategory.Key);
                    //only update currently change property
                    subcat.Name = subcatCatVm.Subcategory.Name;
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            //return back to view with model created from scratch
            return View(await getEmptyVm(subcatCatVm.Subcategory, StatusMessage));
        }

        //GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if(id==null)
            {
                return NotFound();
            }
            var subcat = await _db.Subcategory.SingleOrDefaultAsync(s => s.Key == id);
            if(subcat==null)
            {
                return NotFound();
            }
            return View(await getEmptyVm(subcat));
        }

        //POST - DETAILS
        [HttpPost()]
        [ValidateAntiForgeryToken]
        public IActionResult Details(SubcategoryAndCategoryViewModel vm)
        {
            if(vm != null && ModelState.IsValid)
            {
                //workaround - redirect to action with parameter like instance of anonymus type
                return RedirectToAction(nameof(Edit), new { id = vm.Subcategory.Key });
            }
            return View(vm);
        }

        //GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if(id ==null)
            {
                return NotFound();
            }
            var subcat = await _db.Subcategory.SingleOrDefaultAsync(s => s.Key == id);
            if(subcat == null)
            {
                return NotFound();
            }
            return View(await getEmptyVm(subcat));
        }

        //POST - DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(SubcategoryAndCategoryViewModel vm)
        {
            if(ModelState.IsValid)
            {
                _db.Subcategory.Remove(vm.Subcategory);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }


        private ApplicationDbContext _db;
    }
}