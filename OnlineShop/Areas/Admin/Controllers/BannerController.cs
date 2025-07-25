using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace OnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
	[Authorize(Roles = "admin")]

	public class BannerController : Controller
    {
        private readonly OnlineShopContext _context;

        public BannerController(OnlineShopContext context)
        {
            _context = context;
        }

        // GET: Admin/Banner
        public async Task<IActionResult> Index()
        {
            return View(await _context.Banners.ToListAsync());
        }

        // GET: Admin/Banner/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var banner = await _context.Banners
                .FirstOrDefaultAsync(m => m.Id == id);
            if (banner == null)
            {
                return NotFound();
            }

            return View(banner);
        }

        // GET: Admin/Banner/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Banner/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,SubTitle,ImageName,Priority,Link,Position")] Banner banner , IFormFile ImageFile)
        {
            //Save image
            if(ImageFile != null)
            {
                banner.ImageName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(ImageFile.FileName);
                string fn = Directory.GetCurrentDirectory();
                string imagePath = Path.Combine(fn, "wwwroot\\images\\banners\\", banner.ImageName);
                using(var stream = new FileStream(imagePath , FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }
            }

            //------------------

            if (ModelState.IsValid)
            {
                _context.Add(banner);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(banner);
        }

        // GET: Admin/Banner/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
            {
                return NotFound();
            }
            return View(banner);
        }

        // POST: Admin/Banner/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,SubTitle,ImageName,Priority,Link,Position")] Banner banner ,IFormFile? ImageFile)
        {
            if (id != banner.Id)
            {
                return NotFound();
            }

			//Save image
			if (ImageFile != null)
			{
				//-----------------
				string org_fn;
				org_fn = Path.Combine(Directory.GetCurrentDirectory() , "wwwroot\\images\\banners\\" , banner.ImageName);

				if (System.IO.File.Exists(org_fn))
				{
					System.IO.File.Delete(org_fn);
				}
				//-----------------
				banner.ImageName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
				//-----------------
				string ImagePath;
				ImagePath = Path.Combine(Directory.GetCurrentDirectory() , "wwwroot\\images\\banners\\" , banner.ImageName);

				using (var stream = new FileStream(ImagePath, FileMode.Create))
				{
					ImageFile.CopyTo(stream);
				}

			}

			if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(banner);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BannerExists(banner.Id))
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
            return View(banner);
        }

        // GET: Admin/Banner/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var banner = await _context.Banners
                .FirstOrDefaultAsync(m => m.Id == id);
            if (banner == null)
            {
                return NotFound();
            }

            return View(banner);
        }

        // POST: Admin/Banner/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                _context.Banners.Remove(banner);
                string imagePath = "wwwroot\\images\\banners\\" + banner.ImageName;

                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BannerExists(int id)
        {
            return _context.Banners.Any(e => e.Id == id);
        }


	}
}
