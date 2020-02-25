using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceStoreExample.Data;
using SpiceStoreExample.Models;
using SpiceStoreExample.Utility;

namespace SpiceStoreExample.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CouponController : Controller
    {
        [BindProperty]
        public Coupon Coupon { get; set; }

        public CouponController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _db.Coupon.ToListAsync());
        }

        //GET - CREATE
        public IActionResult Create()
        {
            return View();
        }

        //POST - CREATE
        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost()
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                byte[] p1 = null;
                if (files.Any())
                {
                    using (var fs = files[0].OpenReadStream())
                    {
                        using (var ms = new MemoryStream())
                        {
                            await fs.CopyToAsync(ms);
                            p1 = ms.ToArray();
                        }
                    }
                    Coupon.Picture = p1;
                }
                _db.Coupon.Add(Coupon);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        //GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if(id ==null)
            {
                return NotFound();
            }
            Coupon = await _db.Coupon.SingleOrDefaultAsync(c => c.Id == id);
            if(Coupon ==null)
            {
                return NotFound();
            }

            ViewBag.ImgString = await strGetBase64ImageEncodingAsync(Coupon.Picture);
            return View(Coupon);
        }


        //POST - EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit()
        {
            if(Coupon == null || !ModelState.IsValid)
            {
                Coupon = new Coupon { };
                return View(Coupon);
            }
            var dbCoupon = _db.Coupon.SingleOrDefault(c => c.Id == Coupon.Id);

            //do "merge" operation on object from db and from view
            dbCoupon.Name = Coupon.Name;
            dbCoupon.Discount = Coupon.Discount;
            dbCoupon.CouponType = Coupon.CouponType;
            dbCoupon.IsActive = Coupon.IsActive;
            dbCoupon.MinimumAmount = Coupon.MinimumAmount;

            //check whether picture has been changed
            var files = Request.Form.Files;
            if(files.Any())
            {
                using (var stream = files[0].OpenReadStream())
                {
                    using (var ms = new MemoryStream())
                    {
                        await stream.CopyToAsync(ms);
                        dbCoupon.Picture = ms.ToArray();
                    }
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if(id ==null)
            {
                return NotFound();
            }
            Coupon = await _db.Coupon.SingleOrDefaultAsync(c => c.Id == id);
            if(Coupon == null)
            {
                return NotFound();
            }
            ViewBag.ImgString = await strGetBase64ImageEncodingAsync(Coupon.Picture);
            return View(Coupon);
        }

        //POST - DETAILS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details()
        {
            if(Coupon == null || !ModelState.IsValid)
            {
                Coupon = new Coupon();
                return View(Coupon);
            }
            return RedirectToAction(nameof(Edit), new { id = Coupon.Id });
        }

        //GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if(id ==null)
            {
                return NotFound();
            }
            Coupon = await _db.Coupon.SingleOrDefaultAsync(c => c.Id == id);
            if(Coupon == null)
            {
                return NotFound();
            }
            ViewBag.ImgString = await strGetBase64ImageEncodingAsync(Coupon.Picture);
            return View(Coupon);
        }

        //POST - DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete()
        {
            if(Coupon == null || !ModelState.IsValid)
            {
                Coupon = new Coupon();
                return View(Coupon);
            }
            _db.Coupon.Remove(Coupon);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        private async Task<string> strGetBase64ImageEncodingAsync(byte[] arr)
        {
            string strBase64Img = "";
            if (arr!= null)
            {
                strBase64Img = "data:image/jpeg;base64," +
                   Convert.ToBase64String(arr, 0, arr.Length);
            }
            else
            {
                byte[] p1 = null;
                string dummyPath =
                    System.IO.Path.Combine(_env.WebRootPath, @"images", StaticDetails.DefaultFoodImage);
                if (System.IO.File.Exists(dummyPath))
                {
                    using (var fs = System.IO.File.OpenRead(dummyPath))
                    {
                        using (var ms = new MemoryStream())
                        {
                            await fs.CopyToAsync(ms);
                            p1 = ms.ToArray();
                            strBase64Img = "data:image/jpeg;base64," +
                                Convert.ToBase64String(p1, 0, p1.Length);
                        }
                    }
                }
            }
            return strBase64Img;
        }

        private IWebHostEnvironment _env;
        private ApplicationDbContext _db;
    }
}