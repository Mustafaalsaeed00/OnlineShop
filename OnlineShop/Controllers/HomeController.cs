using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OnlineShop.Models;
using OnlineShop.Models.Db;

namespace OnlineShop.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly OnlineShopContext _Context;
    public HomeController(ILogger<HomeController> logger , OnlineShopContext context)
    {
        _logger = logger;
        _Context = context;
    }

    public IActionResult Index()
    {
        var banners = _Context.Banners.ToList();
		ViewData["banners"] = banners;
		//--------------New Products-----

		var NewProducts = _Context.Products.OrderByDescending(p => p.Id).Take(8).ToList();
        ViewData["newProducts"] = NewProducts;
        //-------------Best Selling-------

        //-----Without database view-----
        //var BestSellingProductsIds = _Context.OrderDetails
        //    .GroupBy(o => o.ProductId)
        //    .Select(g => new { ProductId = g.Key, ProductCount = g.Count() })
        //    .OrderByDescending(g => g.ProductCount).Take(8).ToList();
        //var BestSelling = new List<Product>();
        //foreach(var group in BestSellingProductsIds)
        //{
        //    var Product = _Context.Products.FirstOrDefault(p => p.Id == group.ProductId);
        //    if (Product is not null)
        //        BestSelling.Add(Product);
        //}
        //----------------------------------

        //-----With database view------
        var BestSelling = _Context.BestSellingFinals.OrderByDescending(x=>x.TotalSum).Take(8).ToList();

		ViewData["bestSelling"] = BestSelling;
        //--------------------------------


		return View();
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
}
