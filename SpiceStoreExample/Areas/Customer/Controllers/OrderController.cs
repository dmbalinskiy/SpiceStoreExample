using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly IEmailSender _emailSender;

        //records per page
        private int PageSize = 1;

        public OrderController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
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
                    OrderDetails = await 
                    _db
                    .OrderDetails.Include(o => o.MenuItem)
                    .Where(o => o.OrderId == oh.Id).ToListAsync()
                };
                orderDetailsVM.Add(odvm);
            }
            return View(orderDetailsVM.OrderBy(o => o.OrderHeader.PickUpTime).ToList());
        }


        [Authorize(Roles = Consts.KitchenUser + "," + Consts.ManagerUser)]
        public async Task<IActionResult> OrderPrepare(int OrderId)
        {
            OrderHeader oh = await _db.OrderHeader.FindAsync(OrderId);
            oh.Status = Consts.statusInProcess;
            await _db.SaveChangesAsync();
            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize(Roles = Consts.KitchenUser + "," + Consts.ManagerUser)]
        public async Task<IActionResult> OrderReady(int OrderId)
        {
            OrderHeader oh = await _db.OrderHeader.FindAsync(OrderId);
            oh.Status = Consts.statusReady;
            await _db.SaveChangesAsync();

            //Todo: email logic for user notification
            //send an cancelation email
            await _emailSender.SendEmailAsync(
                _db.Users.Where(u => u.Id == oh.UserId).FirstOrDefault().Email,
            "Spice - order ready for pickup " + oh.Id,
            "Order is ready for pickup!");

            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize(Roles = Consts.KitchenUser + "," + Consts.ManagerUser)]
        public async Task<IActionResult> OrderCancel(int OrderId)
        {
            OrderHeader oh = await _db.OrderHeader.FindAsync(OrderId);
            oh.Status = Consts.statusCanceled;
            await _db.SaveChangesAsync();
            
            //send an cancelation email
            await _emailSender.SendEmailAsync(
                _db.Users.Where(u => u.Id == oh.UserId).FirstOrDefault().Email,
            "Spice - order canceled " + oh.Id,
            "Order has been canceled successfully");

            return RedirectToAction("ManageOrder", "Order");
        }


        [Authorize]
        public async Task<IActionResult> OrderPickup(
            /*current page*/int productPage = 1,
            string searchName = null,
            string searchPhone = null, 
            string searchEmail = null)
        {
            //var claimsIdentity = (ClaimsIdentity)User.Identity;
            //var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel olvm = new OrderListViewModel
            {
                Orders = new List<OrderDetailsViewModel>()
            };
            StringBuilder param = new StringBuilder();
            param.Append("/Customer/Order/OrderPickup?productPage=:");
            bool bHasSearchCriteria = false;
            param.Append("&searchName=");
            if(searchName != null)
            {
                param.Append(searchName);
                bHasSearchCriteria = true;
            }

            param.Append("&searchEmail=");
            if(searchEmail != null)
            {
                param.Append(searchEmail);
                bHasSearchCriteria = true;
            }

            param.Append("&searchPhone=");
            if(searchPhone != null)
            {
                param.Append(searchPhone);
                bHasSearchCriteria = true;
            }

            List<OrderHeader> orderHeaderList = null;
            if (bHasSearchCriteria)
            {
                orderHeaderList = new List<OrderHeader>();
                if(searchName != null)
                {
                    orderHeaderList =
                        await _db.OrderHeader.Include(o => o.ApplicationUser)
                        .Where(u => u.Pickupname.ToLower().Contains(searchName.ToLower()))
                        .OrderByDescending(o => o.OrderDate)
                        .ToListAsync();
                }
                else if(searchEmail != null)
                {
                    orderHeaderList =
                        await _db.OrderHeader.Include(o => o.ApplicationUser)
                        .Where(u => u.ApplicationUser.Email.ToLower().Contains(searchEmail.ToLower()))
                        .OrderByDescending(o => o.OrderDate)
                        .ToListAsync();
                }
                else if (searchPhone != null)
                {
                    orderHeaderList =
                        await _db.OrderHeader.Include(o => o.ApplicationUser)
                        .Where(u => u.PhoneNumber.ToLower().Contains(searchPhone.ToLower()))
                        .OrderByDescending(o => o.OrderDate)
                        .ToListAsync();
                }
            }
            else
            {
                orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                .Where(u => u.Status == Consts.statusReady)
                .ToListAsync();
            }

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
                UrlParam = param.ToString()
            };

            return View(olvm);
        }


        [Authorize(Roles = Consts.FrontDeskUser + "," + Consts.ManagerUser)]
        [HttpPost]
        [ActionName("OrderPickup")]
        public async Task<IActionResult> OrderPickupPost(int orderId)
        {
            OrderHeader oh = await _db.OrderHeader.FindAsync(orderId);
            oh.Status = Consts.statusCompleted;
            await _db.SaveChangesAsync();

            //send an cancelation email
            await _emailSender.SendEmailAsync(
                _db.Users.Where(u => u.Id == oh.UserId).FirstOrDefault().Email,
            "Spice - order completed " + oh.Id,
            "Order has been completed successfully");

            return RedirectToAction("OrderPickup", "Order");
        }

    }
}