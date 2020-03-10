using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceStoreExample.Data;
using SpiceStoreExample.Models;
using SpiceStoreExample.Models.ViewModels;
using SpiceStoreExample.Utility;

namespace SpiceStoreExample.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private ApplicationDbContext _db;

        //records per page
        private int PageSize = 1;

        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Confirm(int id)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(o => o.ApplicationUser)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == claim.Value),

                OrderDetails = await _db.OrderDetails.Include(o => o.MenuItem)
                .Where(o => o.OrderId == id)
                .ToListAsync()
            };

            return View(orderDetailsViewModel);
        }


        [Authorize]
        public async Task<IActionResult> OrderHistory(/*current page*/int productPage = 1)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel olvm = new OrderListViewModel
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            List<OrderHeader> orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                .Where(u => u.UserId == claim.Value)
                .ToListAsync();
            foreach (var oh in orderHeaderList)
            {
                OrderDetailsViewModel odvm = new OrderDetailsViewModel
                {
                    OrderHeader = oh,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == oh.Id).ToListAsync()
                };
                olvm.Orders.Add(odvm);
            }

            var count = olvm.Orders.Count;
            olvm.Orders = olvm.Orders.OrderByDescending(p => p.OrderHeader.Id)
                .Skip((productPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            olvm.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItems = count,
                UrlParam = "/Customer/Order/OrderHistory?productPage=:"
            };

            return View(olvm);
        }

        public async Task<IActionResult> GetOrderDetails(int Id)
        {
            OrderDetailsViewModel odvm = new OrderDetailsViewModel
            {
                OrderHeader = await _db.OrderHeader.FirstOrDefaultAsync(m => m.Id == Id),
                OrderDetails = await _db.OrderDetails
                .Include(o => o.MenuItem)
                .Where(m => m.OrderId == Id)
                .ToListAsync()
            };
            odvm.OrderHeader.ApplicationUser =
                await _db.ApplicationUser.FirstOrDefaultAsync(u => u.Id == odvm.OrderHeader.UserId);
            return PartialView("_IndividualOrderDetails", odvm);
        }

        public async Task<IActionResult> GetOrderStatus(int Id)
        {
            var item = await _db.OrderHeader.Where(oh => oh.Id == Id).FirstOrDefaultAsync();
            var status = item.Status;
            string imgPath = "";
            if(status == Consts.statusSubmitted)
            {
                imgPath = "OrderPlaced.png";
            }
            else if(status == Consts.statusInProcess)
            {
                imgPath = "InKitchen.png";
            }
            else if(status == Consts.statusReady)
            {
                imgPath = "ReadyForPickup.png";
            }
            else if(status == Consts.statusCompleted)
            {
                imgPath = "completed.png";
            }
            return Json( @"\images\" + imgPath);
        }

        [Authorize(Roles = Consts.KitchenUser + "," + Consts.ManagerUser)]
        public async Task<IActionResult> ManageOrder()
        {
            List<OrderDetailsViewModel> orderDetailsVM = new List<OrderDetailsViewModel>();
            List<OrderHeader> orderHeaderList = await _db.OrderHeader
                .Where(u => u.Status == Consts.statusSubmitted || u.Status == Consts.statusInProcess)
                .OrderByDescending(o => o.PickUpTime)
                .ToListAsync();
            foreach (var oh in orderHeaderList)
            {
                OrderDetailsViewModel odvm = new OrderDetailsViewModel
                {
                    OrderHeader = oh,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == oh.Id).ToListAsync()
                };
                orderDetailsVM.Add(odvm);
            }
            return View(orderDetailsVM.OrderBy(o => o.OrderHeader.PickUpTime));
        }

        public IActionResult Create()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}