using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceStoreExample.Data;
using SpiceStoreExample.Models;
using SpiceStoreExample.Models.ViewModels;
using SpiceStoreExample.Utility;

namespace SpiceStoreExample.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;

        [BindProperty]
        public OrderDetailsCart detailCart { get; set; }

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            detailCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };
            detailCart.OrderHeader.OrderTotal = 0;
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = _db.ShoppingCart.Where(c => c.ApplicationUserId == claim.Value);
            if (cart != null)
            {
                detailCart.listCart = cart.ToList();
            }

            foreach(var list in detailCart.listCart)
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                detailCart.OrderHeader.OrderTotal =
                    detailCart.OrderHeader.OrderTotal + list.MenuItem.Price * list.Count;
                list.MenuItem.Description = Consts.ConvertToRawHtml(list.MenuItem.Description);

                //trim description string
                if(list.MenuItem.Description.Length > 100)
                {
                    list.MenuItem.Description = list.MenuItem.Description.Substring(97) + "...";
                }

            }

            //before coupon assignment
            detailCart.OrderHeader.OrderTotalOriginal = detailCart.OrderHeader.OrderTotal;
            string strCouponCode = HttpContext.Session.GetString(Consts.CouponCode);
            Coupon coupon = null;
            if (strCouponCode != null)
            {
                detailCart.OrderHeader.CouponCode = strCouponCode;
                coupon = await _db.Coupon.Where(c => c.Name.ToLower() == detailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
            }
            detailCart.OrderHeader.OrderTotal =
                    Consts.DiscountedPrice(coupon, detailCart.OrderHeader.OrderTotalOriginal);

            return View(detailCart);
        }

        public IActionResult AddCoupon()
        {
            if(detailCart.OrderHeader.CouponCode == null)
            {
                detailCart.OrderHeader.CouponCode = null;
            }
            HttpContext.Session.SetString(Consts.CouponCode, detailCart.OrderHeader.CouponCode ?? string.Empty);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.SetString(Consts.CouponCode, string.Empty);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(c => c.Id == cartId);
            cart.Count += 1;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(c => c.Id == cartId);
            cart.Count -= 1;

            //remove cart at all, if count is 0
            if(cart.Count < 1)
            {
                _db.ShoppingCart.Remove(cart);
            }
            await _db.SaveChangesAsync();
            
            //also update counts inside session value
            var cnt = _db.ShoppingCart.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(Consts.ShoppingCartCount, cnt);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(c => c.Id == cartId);
            _db.ShoppingCart.Remove(cart);
            await _db.SaveChangesAsync();

            //also update counts inside session value
            var cnt = _db.ShoppingCart.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(Consts.ShoppingCartCount, cnt);

            return RedirectToAction(nameof(Index));
        }



        public async Task<IActionResult> Summary()
        {
            detailCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };
            detailCart.OrderHeader.OrderTotal = 0;
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ApplicationUser appUser = await _db.ApplicationUser.Where(c => c.Id == claim.Value).FirstOrDefaultAsync();


            var cart = _db.ShoppingCart.Where(c => c.ApplicationUserId == claim.Value);
            if (cart != null)
            {
                detailCart.listCart = cart.ToList();
            }

            foreach (var list in detailCart.listCart)
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                detailCart.OrderHeader.OrderTotal =
                    detailCart.OrderHeader.OrderTotal + list.MenuItem.Price * list.Count;
                list.MenuItem.Description = Consts.ConvertToRawHtml(list.MenuItem.Description);

            }
            //before coupon assignment
            detailCart.OrderHeader.OrderTotalOriginal = detailCart.OrderHeader.OrderTotal;
            detailCart.OrderHeader.Pickupname = appUser.Name;
            detailCart.OrderHeader.PhoneNumber = appUser.PhoneNumber;
            detailCart.OrderHeader.PickUpTime = DateTime.Now;


            string strCouponCode = HttpContext.Session.GetString(Consts.CouponCode);
            Coupon coupon = null;
            if (strCouponCode != null)
            {
                detailCart.OrderHeader.CouponCode = strCouponCode;
                coupon = await _db.Coupon.Where(c => c.Name.ToLower() == detailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
            }
            detailCart.OrderHeader.OrderTotal =
                    Consts.DiscountedPrice(coupon, detailCart.OrderHeader.OrderTotalOriginal);

            return View(detailCart);
        }


    }
}