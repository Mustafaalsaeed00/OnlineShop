using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models.Db;
using System.Security.Claims;

namespace OnlineShop.Areas.User.Controllers
{
	[Area("User")]
	[Authorize]
	public class HomeController : Controller
	{
		private readonly OnlineShopContext _context;

		public HomeController(OnlineShopContext context)
		{
			this._context = context;
		}
		public IActionResult Index()
		{
			int UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
			var user = _context.Users.FirstOrDefault(u => u.Id == UserId);
			return View(user);
		}
	}
}
