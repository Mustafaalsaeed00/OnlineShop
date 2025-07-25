using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
	[Authorize(Roles = "admin")]

	public class ProductController : Controller
    {
        private readonly OnlineShopContext _context;

        public ProductController(OnlineShopContext context)
        {
            _context = context;
        }

        // GET: Admin/Product
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.ToListAsync());
        }

        // GET: Admin/Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            var productGallery = _context.ProductGaleries.Where(p => p.ProductId == id).ToList();
            ViewBag.ProductGallery = productGallery;
            return View(product);
        }

        // GET: Admin/Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Product/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,FullDesc,Price,Discount,ImageName,Qty,Tags,VideoUrl")] Product product, IFormFile? MainImage, IFormFile[]? GalleryFile)
        {
            if (ModelState.IsValid)
            {

                if (MainImage != null)
                {
                    string imageExtention = Path.GetExtension(MainImage.FileName);
                    string imageName = Guid.NewGuid().ToString() + imageExtention;

                    string FullPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot\\images\\products\\", imageName);

                    using (var stream = new FileStream(FullPath, FileMode.Create))
                    {
                        MainImage.CopyTo(stream);
                    }
                    product.ImageName = imageName;
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                //Save GalleryImages In ImageProducts Folder
                //---------------------------------
                if (GalleryFile != null)
                {
                    foreach (var img in GalleryFile)
                    {
                        string imageExtention = Path.GetExtension(img.FileName);
                        string imageName = Guid.NewGuid().ToString() + imageExtention;

                        string FullPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot\\images\\products\\", imageName);

                        using (var stream = new FileStream(FullPath, FileMode.Create))
                        {
                            img.CopyTo(stream);
                        }
                        //Save Image In Database
                        //--------------------------------
                        ProductGalery productGalery = new ProductGalery();
                        productGalery.ProductId = product.Id;
                        productGalery.ImageName = imageName;
                        _context.ProductGaleries.Add(productGalery);
                        //--------------------------------
                    }
                    await _context.SaveChangesAsync();
                }
                //---------------------------------
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            var productGallery = _context.ProductGaleries.Where(p => p.ProductId == id).ToList();
            ViewBag.ProductGallery = productGallery;
            return View(product);
        }

        // POST: Admin/Product/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,FullDesc,Price,Discount,ImageName,Qty,Tags,VideoUrl")] 
        Product product, IFormFile? MainImage , IFormFile[]? GalleryFile)
        {
            

			if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);

					if (MainImage != null)
					{
						if (!string.IsNullOrEmpty(product.ImageName))
						{
							string ImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\products\\", product.ImageName);
							if (System.IO.File.Exists(ImagePath))
							{
								System.IO.File.Delete(ImagePath);
							}
						}

						string imageName = Guid.NewGuid().ToString() + Path.GetExtension(MainImage.FileName);

						string FullPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot\\images\\products\\", imageName);

						using (var stream = new FileStream(FullPath, FileMode.Create))
						{
							MainImage.CopyTo(stream);
						}
						product.ImageName = imageName;
					}

					//Save GalleryImages In ImageProducts Folder
					//---------------------------------
					if (GalleryFile != null)
					{
						foreach (var img in GalleryFile)
						{
							string imageExtention = Path.GetExtension(img.FileName);
							string imageName = Guid.NewGuid().ToString() + imageExtention;

							string FullPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot\\images\\products\\", imageName);

							using (var stream = new FileStream(FullPath, FileMode.Create))
							{
								img.CopyTo(stream);
							}
							//Save Image In Database
							//--------------------------------
							ProductGalery productGalery = new ProductGalery();
							productGalery.ProductId = product.Id;
							productGalery.ImageName = imageName;
							_context.ProductGaleries.Add(productGalery);
							//--------------------------------
						}
					}
					//---------------------------------

					await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Admin/Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }
			var productGallery = _context.ProductGaleries.Where(p => p.ProductId == id).ToList();
			ViewBag.ProductGallery = productGallery;
			return View(product);
        }

        // POST: Admin/Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
				//Delete MainImage From ProductImages File
				string ImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\products\\", product.ImageName);
				if (System.IO.File.Exists(ImagePath))
				{
					System.IO.File.Delete(ImagePath);
				}
				//------------------------

				//Delete Product Gallery From Database
				var productGallery = _context.ProductGaleries.Where(p => p.ProductId == product.Id);
				if (productGallery != null)
				{
					foreach (var p in productGallery)
					{
						//_context.ProductGaleries.Remove(p);
						//Delete Product Images From ProductImages File
						string ImgPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\products\\", p.ImageName);
						if (System.IO.File.Exists(ImgPath))
						{
							System.IO.File.Delete(ImgPath);
						}
					}
                    _context.ProductGaleries.RemoveRange(productGallery);
				}
				_context.Products.Remove(product);
            }

            

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

		public IActionResult DeleteImageGallery(int id)
        {
            if (id == null)
                return NotFound();

            var ProductImageGallery = _context.ProductGaleries.FirstOrDefault(p => p.Id == id);
            if (ProductImageGallery == null)
                return NotFound();
           
			//Delete Product Images From ProductImages File
			string ImgPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\products\\", ProductImageGallery.ImageName);
			if (System.IO.File.Exists(ImgPath))
			{
				System.IO.File.Delete(ImgPath);
			}

			_context.ProductGaleries.Remove(ProductImageGallery);
			_context.SaveChanges();
            var Product = _context.Products.FirstOrDefault(p => p.Id == ProductImageGallery.ProductId);

			var productGallery = _context.ProductGaleries.Where(ProductImg => ProductImg.Id == id).ToList();
			ViewBag.ProductGallery = productGallery;

			return RedirectToAction("Edit",Product);
        }

	}
}
