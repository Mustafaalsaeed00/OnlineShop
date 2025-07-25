using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OnlineShop.Models.Db;
using OnlineShop.Models;
using OnlineShop.Models.ViewModel;
using PayPal.Api;
using System.Security.Claims;

namespace OnlineShop.Controllers
{
    public class CartController : Controller
    {
		
		private OnlineShopContext _context;
		private IHttpContextAccessor _httpContextAccessor;
		IConfiguration _configuration;
		public CartController(OnlineShopContext context, IHttpContextAccessor httpContextAccessor, IConfiguration iconfiguration)
		{
			_httpContextAccessor = httpContextAccessor;
			_configuration = iconfiguration;
			_context = context;
		}


		public IActionResult Index()
        {
			List<ProductCartViewModel> ProductCarts = GetProductsInCart();
            return View(ProductCarts);
        }

		public IActionResult ClearCart()
		{
			Response.Cookies.Delete("Cart");
			return Redirect("/Cart");
		}

		/// <summary>
		/// Add or update the shopping cart
		/// </summary>
		/// <param name="productId">Product ID</param>
		/// <param name="count">
		///  If quantity is zero, it means the intention is to remove the item. 
		/// This case is manually handled by us.
		/// </param>
		/// <returns></returns>
		[HttpPost]
		public IActionResult UpdateCart([FromBody]CartViewModel request)
		{
			var product = _context.Products.FirstOrDefault(p => p.Id == request.ProductId);
			if(product == null)
			{
				return NotFound();
			}
			if(product.Qty < request.Count)
			{
				return BadRequest();
			}

			var cartItems = GetCartItems();

			var foundProductInCart = cartItems.FirstOrDefault(p => p.ProductId == request.ProductId);
			if(foundProductInCart == null)
			{
				var NewCartItem = new CartViewModel();
				NewCartItem.ProductId = request.ProductId;
				NewCartItem.Count = request.Count;

				cartItems.Add(NewCartItem);
			}else
			{
				if(request.Count > 0)
				{
					foundProductInCart.Count = request.Count;
				}
				else
				{
					cartItems.Remove(foundProductInCart);
				}
			}

			var json = JsonConvert.SerializeObject(cartItems);

			CookieOptions options = new CookieOptions();
			options.Expires = DateTime.Now.AddDays(7);

			Response.Cookies.Append("Cart", json, options);

			var resulte = cartItems.Sum(p => p.Count);

			return Ok(resulte);

		}

		public List<CartViewModel> GetCartItems()
		{
			List<CartViewModel> CartList = new List<CartViewModel>();
			var prevCartItemsString = Request.Cookies["Cart"];
			if(!string.IsNullOrEmpty(prevCartItemsString))
			{
				CartList = JsonConvert.DeserializeObject<List<CartViewModel>>(prevCartItemsString);
			}
			return CartList;
		}

		public List<ProductCartViewModel> GetProductsInCart()
		{
			var cartItems = GetCartItems();

			if (!cartItems.Any())
			{
				return null;
			}

			var cartItemProductIds = cartItems.Select(x => x.ProductId).ToList();
			// Load products into memory
			var products = _context.Products
				.Where(p => cartItemProductIds.Contains(p.Id))
				.ToList();

			// Create the ProductCartViewModel list

			List<ProductCartViewModel> result = new List<ProductCartViewModel>();
			foreach (var item in products)
			{
				var newItem = new ProductCartViewModel
				{
					Id = item.Id,
					ImageName = item.ImageName,
					Price = (item.Price - (item.Discount ?? 0)),
					Title = item.Title,
					Count = cartItems.Single(x => x.ProductId == item.Id).Count,
					RowSumPrice = ((item.Price - (item.Discount ?? 0)) * cartItems.Single(x => x.ProductId == item.Id).Count),
				};

				result.Add(newItem);
			}
			return result;
		}

		public IActionResult SmallCart()
		{
			List<ProductCartViewModel> result = GetProductsInCart();
			return PartialView(result);
		}

		[Authorize]
		public IActionResult Checkout()
		{
			var order = new Models.Db.Order();
			var Shipping = _context.Settings.FirstOrDefault()?.Shipping;
			if(Shipping is not null)
			{
				order.Shipping = Shipping;
			}
			ViewData["Products"] = GetProductsInCart();
			return View(order);
		}

		[Authorize]
		[HttpPost]
		public IActionResult ApplyCouponCode([FromForm] string couponCode)
		{
			var order = new Models.Db.Order();


			var shipping = _context.Settings.First().Shipping;
			if (shipping != null)
			{
				order.Shipping = shipping;
			}

			var coupon = _context.Coupons.FirstOrDefault(c => c.Code == couponCode);


			if (coupon != null)
			{
				order.CouponCode = coupon.Code;
				order.CouponDiscount = coupon.Discount;
			}
			else
			{
				ViewData["Products"] = GetProductsInCart();
				TempData["message"] = "Coupon not exitst";
				return View("Checkout", order);
			}

			

			ViewData["Products"] = GetProductsInCart();
			return View("Checkout", order);
		}

		[Authorize]
		[HttpPost]
		public IActionResult Checkout(Models.Db.Order order)
		{
			if (!ModelState.IsValid)
			{

				ViewData["Products"] = GetProductsInCart();

				return View(order);
			}

			//-------------------------------------------------------

			//check and find coupon
			if (!string.IsNullOrEmpty(order.CouponCode))
			{
				var coupon = _context.Coupons.FirstOrDefault(c => c.Code == order.CouponCode);

				if (coupon != null)
				{
					order.CouponCode = coupon.Code;
					order.CouponDiscount = coupon.Discount;
				}
				else
				{
					TempData["message"] = "Coupon not exitst";
					ViewData["Products"] = GetProductsInCart();

					return View(order);
				}
			}

			var products = GetProductsInCart();

			order.Shipping = _context.Settings.First().Shipping;
			order.CreateDate = DateTime.Now;
			order.SubTotal = products.Sum(x => x.RowSumPrice);
			order.Total = (order.SubTotal + order.Shipping ?? 0);
			order.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

			if (order.CouponDiscount != null)
			{
				order.Total -= order.CouponDiscount;
			}

			_context.Orders.Add(order);
			_context.SaveChanges();

			//-------------------------------------------------------

			List<OrderDetail> orderDetails = new List<OrderDetail>();

			foreach (var item in products)
			{
				OrderDetail orderDetailItem = new OrderDetail()
				{
					Count = item.Count,
					ProductTitle = item.Title,
					ProductPrice = (decimal)item.Price,
					OrderId = order.Id,
					ProductId = item.Id
				};

				orderDetails.Add(orderDetailItem);
			}
			//-------------------------------------------------------
			_context.OrderDetails.AddRange(orderDetails);
			_context.SaveChanges();

			// Redirect to PayPal
			return Redirect("/Cart/RedirectToPayPal?orderId=" + order.Id);
		}

		public ActionResult RedirectToPayPal(int orderId)
		{
			var order = _context.Orders.Find(orderId);
			if (order == null)
			{
				return View("PaymentFailed");
			}

			var orderDetails = _context.OrderDetails.Where(x => x.OrderId == orderId).ToList();

			var clientId = _configuration.GetValue<string>("PayPal:Key");
			var clientSecret = _configuration.GetValue<string>("PayPal:Secret");
			var mode = _configuration.GetValue<string>("PayPal:mode");
			var apiContext = PaypalConfiguration.GetAPIContext(clientId, clientSecret, mode);

			try
			{
				string baseURI = $"{Request.Scheme}://{Request.Host}/cart/PaypalReturn?";
				var guid = Guid.NewGuid().ToString();

				var payment = new Payment
				{
					intent = "sale",
					payer = new Payer { payment_method = "paypal" },
					transactions = new List<Transaction>
	  {
		  new Transaction
		  {
			  description = $"Order {order.Id}",
			  invoice_number = Guid.NewGuid().ToString(),
			  amount = new Amount
			  {
				  currency = "USD",
				  total = order.Total?.ToString("F"),
                  //total = "5.00"
              },

              ////item_list = new ItemList
              ////{
              ////    items = orderDetails.Select(p => new Item
              ////    {
              ////        name = p.ProductTitle,
              ////        currency = "USD",
              ////        price = p.ProductPrice.ToString("F"),
              ////        quantity = p.Count.ToString(),
              ////        sku = p.ProductId.ToString(),
              ////    }).ToList(),

              ////},
          }
	  },
					redirect_urls = new RedirectUrls
					{
						cancel_url = $"{baseURI}&Cancel=true",
						return_url = $"{baseURI}orderId={order.Id}"
					}
				};
				//Add shipping price
				////payment.transactions[0].item_list.items.Add(new Item
				////{
				////    name = "Shipping cost",
				////    currency = "USD",
				////    price = order.Shipping?.ToString("F"),
				////    quantity = "1",
				////    sku = "1",
				////});

				var createdPayment = payment.Create(apiContext);
				var approvalUrl = createdPayment.links.FirstOrDefault(l => l.rel.ToLower() == "approval_url")?.href;

				_httpContextAccessor.HttpContext.Session.SetString("payment", createdPayment.id);
				return Redirect(approvalUrl);
			}
			catch (Exception e)
			{
				return View("PaymentFailed");
			}
		}

		public ActionResult PaypalReturn(int orderId, string PayerID)
		{
			var order = _context.Orders.Find(orderId);
			if (order == null)
			{
				return View("PaymentFailed");
			}

			var clientId = _configuration.GetValue<string>("PayPal:Key");
			var clientSecret = _configuration.GetValue<string>("PayPal:Secret");
			var mode = _configuration.GetValue<string>("PayPal:mode");
			var apiContext = PaypalConfiguration.GetAPIContext(clientId, clientSecret, mode);

			try
			{
				var paymentId = _httpContextAccessor.HttpContext.Session.GetString("payment");
				var paymentExecution = new PaymentExecution { payer_id = PayerID };
				var payment = new Payment { id = paymentId };

				var executedPayment = payment.Execute(apiContext, paymentExecution);

				if (executedPayment.state.ToLower() != "approved")
				{
					return View("PaymentFailed");
				}

				Response.Cookies.Delete("Cart");
				// Save the PayPal transaction ID and update order status
				order.TransId = executedPayment.transactions[0].related_resources[0].sale.id;
				order.Status = executedPayment.state.ToLower();

				//--------Reduce Qty----
				var OrderDetails = _context.OrderDetails.Where(o => o.OrderId == order.Id).ToList();
				var ProductIds = _context.OrderDetails.Select(o => o.ProductId).ToList();
				var Products = _context.Products.Where(p => ProductIds.Contains(p.Id)).ToList();
				foreach(var item in Products)
				{
					item.Qty -= OrderDetails.FirstOrDefault(o => o.ProductId == item.Id)?.Count;
				}
				_context.Products.UpdateRange(Products);
				//-------------------------------
				_context.SaveChanges();

				ViewData["orderId"] = order.Id;

				return View("PaymentSuccess");
			}
			catch (Exception)
			{
				return View("PaymentFailed");
			}
		}


	}
}
