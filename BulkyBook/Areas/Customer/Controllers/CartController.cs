using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {

        private readonly IUnitOfWork _unitofWork;
        private readonly IEmailSender _emailSender;
        private TwilioSettings _twilioOptions { get; set; }
        private readonly UserManager<IdentityUser> _userManager;
       [BindProperty] //for the error object refernce not set to an instance on line 185
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender,UserManager<IdentityUser>userManager,
                              IOptions<TwilioSettings> twilionOptions)
        {
            _unitofWork = unitOfWork;
            _emailSender = emailSender;
            _userManager = userManager;
            _twilioOptions = twilionOptions.Value;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity; //using claims identity for individuality
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//gets the id and name of the login user

            ShoppingCartVM = new ShoppingCartVM() //retreiving shpping cart
            {
                OrderHeader = new Models.OrderHeader(),
                ListCart = _unitofWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value,includeProperties:"Product") //loading the shopping cart based on application user id
            };
            ShoppingCartVM.OrderHeader.OrderTotal = 0;//setting order total as default 0
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitofWork.ApplicationUser
                                                          .GetFirstOrDefault(u => u.Id == claim.Value,
                                                          includeProperties: "Company");
            foreach(var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price, list.Product.Price50, list.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
                list.Product.Description = SD.ConvertToRawHtml(list.Product.Description);
                if(list.Product.Description.Length > 100) //only show 100 characters
                {
                    list.Product.Description = list.Product.Description.Substring(0, 99) + "...";
                }
            }
            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Index")] //action attribute
        public async Task<IActionResult> INDEXPOST() //the name is different as this post method has the same parameters
                                                     //as of the Index
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity; //using claims identity for individuality
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//gets the id and name of the login user
            var user = _unitofWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            if(user==null)
            {
                ModelState.AddModelError(string.Empty, "Verification email is empty");

            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
            ModelState.AddModelError(string.Empty, "Verification email sent. Please check you email");
            return RedirectToAction("Index");
        }
        public IActionResult plus(int cartId)
        {
            var cart = _unitofWork.ShoppingCart.GetFirstOrDefault
                            (c => c.Id == cartId, includeProperties: "Product");
            cart.Count += 1;
            cart.Price = SD.GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
                                    cart.Product.Price50, cart.Product.Price100);
            _unitofWork.Save();
            return RedirectToAction(nameof(Index));

        }
        public IActionResult minus(int cartId)
        {
            var cart = _unitofWork.ShoppingCart.GetFirstOrDefault
                            (c => c.Id == cartId, includeProperties: "Product");
            if (cart.Count == 1)//if the item is last then remove the whole item altogether
            {
                var cnt = _unitofWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;

                _unitofWork.ShoppingCart.Remove(cart);
                _unitofWork.Save();
                HttpContext.Session.SetInt32(SD.ssShoppingCart, cnt - 1);
            }
            else
            {
                cart.Count -= 1;
                cart.Price = SD.GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
                                        cart.Product.Price50, cart.Product.Price100);
                _unitofWork.Save();
            }
            return RedirectToAction(nameof(Index));

        }

        public IActionResult remove(int cartId)
        {
            var cart = _unitofWork.ShoppingCart.GetFirstOrDefault
                            (c => c.Id == cartId, includeProperties: "Product");
           
                var cnt = _unitofWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;

                _unitofWork.ShoppingCart.Remove(cart);
                _unitofWork.Save();
                HttpContext.Session.SetInt32(SD.ssShoppingCart, cnt - 1);
           
            
            return RedirectToAction(nameof(Index));

        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier); // as we need user centric data we are using this

            ShoppingCartVM = new ShoppingCartVM()
            {
                OrderHeader = new Models.OrderHeader(),
                ListCart = _unitofWork.ShoppingCart.GetAll(c => c.ApplicationUserId == claim.Value,
                                                               includeProperties:"Product")
            };
             ShoppingCartVM.OrderHeader.ApplicationUser = _unitofWork.ApplicationUser
                                                          .GetFirstOrDefault(c => c.Id == claim.Value,
                                                           includeProperties: "Company");
            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price, list.Product.Price50, list.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
                
                
            }
            //populate orderheader properties from applicaion user
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            return View(ShoppingCartVM);


          
            
            

        }



        // use this when using strip payment
        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPost(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier); // as we need user centric data we are using this


            ShoppingCartVM.OrderHeader.ApplicationUser = _unitofWork.ApplicationUser
                                                              .GetFirstOrDefault(c => c.Id == claim.Value,
                                                               includeProperties: "Company");

            ShoppingCartVM.ListCart = _unitofWork.ShoppingCart
                                      .GetAll(c => c.ApplicationUserId == claim.Value, includeProperties: "Product");
            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value; //populate user id
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

            _unitofWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitofWork.Save();

            foreach (var item in ShoppingCartVM.ListCart)
            {
                //to populate the price
                item.Price = SD.GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100);
                OrderDetails orderDetails = new OrderDetails()
                {
                    ProductId = item.ProductId,
                    OrderId = ShoppingCartVM.OrderHeader.Id,
                    Price = item.Price,
                    Count = item.Count
                };
                ShoppingCartVM.OrderHeader.OrderTotal += orderDetails.Count * orderDetails.Price;
                _unitofWork.OrderDetails.Add(orderDetails);

            }

            _unitofWork.ShoppingCart.RemoveRange(ShoppingCartVM.ListCart);//clearing shopping cart after order placed
            _unitofWork.Save();
            HttpContext.Session.SetInt32(SD.ssShoppingCart, 0);//setting it on session

            if (stripeToken == null)
            {
            }
            else
            {
                //process payment
                var options = new ChargeCreateOptions
                {
                    Amount = Convert.ToInt32(ShoppingCartVM.OrderHeader.OrderTotal * 100),
                    Currency = "usd",
                    Description = "Order ID : " + ShoppingCartVM.OrderHeader.Id,
                    Source = stripeToken
                };
                var service = new ChargeService();
                Charge charge = service.Create(options);

                if (charge.BalanceTransactionId == null)
                {
                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
                }
                else
                {
                    ShoppingCartVM.OrderHeader.TransactionId = charge.BalanceTransactionId;
                }
                if (charge.Status.ToLower() == "succeeded")
                {
                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                    ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
                    ShoppingCartVM.OrderHeader.PaymentDate = DateTime.Now;
                }
            }
            _unitofWork.Save();
            return RedirectToAction("OrderConfirmation", "Cart", new { id = ShoppingCartVM.OrderHeader.Id });

        }

        //[HttpPost]
        //[ActionName("Summary")]
        //[ValidateAntiForgeryToken]
        //public IActionResult SummaryPost()
        //{
        //    var claimsIdentity = (ClaimsIdentity)User.Identity;
        //    var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier); // as we need user centric data we are using this


        //    ShoppingCartVM.OrderHeader.ApplicationUser = _unitofWork.ApplicationUser
        //                                                      .GetFirstOrDefault(c => c.Id == claim.Value,
        //                                                       includeProperties: "Company");

        //    ShoppingCartVM.ListCart = _unitofWork.ShoppingCart
        //                              .GetAll(c => c.ApplicationUserId == claim.Value, includeProperties: "Product");
        //    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
        //    ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
        //    ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value; //populate user id
        //    ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

        //    _unitofWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
        //    _unitofWork.Save();

        //    List<OrderDetails> orderDetailsList = new List<OrderDetails>();
        //    foreach(var item in ShoppingCartVM.ListCart)
        //    {
        //        //to populate the price
        //        item.Price = SD.GetPriceBasedOnQuantity(item.Count,item.Product.Price, item.Product.Price50, item.Product.Price100);
        //        OrderDetails orderDetails = new OrderDetails()
        //        {
        //            ProductId = item.ProductId,
        //            OrderId = ShoppingCartVM.OrderHeader.Id,
        //            Price = item.Price,
        //            Count = item.Count
        //        };
        //        ShoppingCartVM.OrderHeader.OrderTotal += orderDetails.Count * orderDetails.Price;
        //        _unitofWork.OrderDetails.Add(orderDetails);

        //    }

        //    _unitofWork.ShoppingCart.RemoveRange(ShoppingCartVM.ListCart);//clearing shopping cart after order placed
        //    _unitofWork.Save();
        //    HttpContext.Session.SetInt32(SD.ssShoppingCart, 0);//setting it on session

        //    return RedirectToAction("OrderConfirmation", "Cart", new { id = ShoppingCartVM.OrderHeader.Id });

        //}

        //orderconfirmation
        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitofWork.OrderHeader.GetFirstOrDefault(u => u.Id == id);
            TwilioClient.Init(_twilioOptions.AccountSid, _twilioOptions.AuthToken);
            try
            {
                var message = MessageResource.Create(
                    body: "Order Placed on Bulky Book. Your Order ID:" + id,
                    from: new Twilio.Types.PhoneNumber(_twilioOptions.PhoneNumber),
                    to: new Twilio.Types.PhoneNumber(orderHeader.PhoneNumber)
                    );
            }
            catch (Exception ex)
            {

            }



            return View(id);
        }

    }
}
