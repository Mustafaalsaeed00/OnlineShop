using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using OnlineShop.Models.Db;
using System.Text.RegularExpressions;

namespace OnlineShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly OnlineShopContext _context;
		public ProductController(OnlineShopContext context)
		{
            _context = context;
		}
		public IActionResult Index()
        {
            List<Product> products = _context.Products.OrderByDescending(p => p.Id).ToList();
            return View(products);
        }

		public IActionResult Search(string SearchText)
		{
			List<Product> products = _context.Products.Where(p =>
			EF.Functions.Like(p.Title, "%" + SearchText + "%") ||
			EF.Functions.Like(p.Tags, "%" + SearchText + "%")
			).OrderBy(p => p.Title).ToList();
			return View("Index",products);
		}

		public IActionResult ProductDetails(int id)
		{
			Product? product = _context.Products.FirstOrDefault(p => p.Id == id);

			if(product is not null)
			{
				List<ProductGalery> productGalery = _context.ProductGaleries.Where(p => p.ProductId == id).ToList();
				ViewBag.ProductGallery = productGalery;
				List<Product> NewProdcuts = _context.Products.Where(p => p.Id != id).Take(6).OrderByDescending(p => p.Id).ToList();
				ViewBag.NewProducts = NewProdcuts;
				List<Comment> Comments = _context.Comments.Where(c => c.ProductId == id).OrderByDescending(p => p.CreatedDate).ToList();
				ViewBag.Comments = Comments;
				return View(product);

			}

			return NotFound();
		}

		[HttpPost]
		public IActionResult SubmitComment(Comment comment)
		{
			if(ModelState.IsValid)
			{
				Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
				Match match = regex.Match(comment.Email);
				if (!match.Success)
				{
					TempData["ErrorMessage"] = "Email is not valid";
					return Redirect("/Product/ProductDetails/" + comment.ProductId);
				}

				Comment newComment = new Comment();
				newComment.Name = comment.Name;
				newComment.Email = comment.Email;
				newComment.CommentText = comment.CommentText;
				newComment.ProductId = comment.ProductId;
				newComment.CreatedDate = DateTime.Now;

				_context.Comments.Add(newComment);
				_context.SaveChanges();

				TempData["SuccessMessage"] = "Your comment submited success fully";
				return Redirect("/Product/ProductDetails/" + comment.ProductId);

			}
			else
			{
				TempData["ErrorMessage"] = "Please complete youre information";
				return Redirect("/Product/ProductDetails/" + comment.ProductId);
			}
		}

	}
}
