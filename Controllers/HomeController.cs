using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using FormsApp.Models;

namespace FormsApp.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        [HttpGet]
        public IActionResult Index(string searchString, string category)
        {
            var products = Repository.Products ?? new List<Product>();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products
                    .Where(p => !string.IsNullOrEmpty(p.Name) &&
                                p.Name.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(category) && int.TryParse(category, out int catId) && catId != 0)
            {
                products = products.Where(p => p.CategoryId == catId).ToList();
            }

            var model = new ProductViewModel
            {
                Products = products,
                Categories = Repository.Categories ?? new List<Category>(),
                SelectedCategory = category
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(Repository.Categories ?? new List<Category>(), "CategoryId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? imageFile)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            string? randomFileName = null;
            string? filePath = null;

            if (imageFile == null)
            {
                ModelState.AddModelError("", "Resim seçiniz");
            }
            else
            {
                var extension = Path.GetExtension(imageFile.FileName)?.ToLower();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Geçerli bir resim seçiniz");
                }
                else
                {
                    randomFileName = $"{Guid.NewGuid()}{extension}";
                    var imgFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");
                    if (!Directory.Exists(imgFolder)) Directory.CreateDirectory(imgFolder);
                    filePath = Path.Combine(imgFolder, randomFileName);
                }
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && filePath != null)
                {
                    // tek seferde dosyayı kaydet
                    using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                }

                model.Image = randomFileName ?? "";
                model.ProductId = (Repository.Products?.Count ?? 0) + 1;
                Repository.CreateProduct(model);
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(Repository.Categories ?? new List<Category>(), "CategoryId", "Name");
            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();

            var entity = Repository.Products?.FirstOrDefault(p => p.ProductId == id);
            if (entity == null) return NotFound();

            ViewBag.Categories = new SelectList(Repository.Categories ?? new List<Category>(), "CategoryId", "Name", entity.CategoryId);
            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? imageFile)
        {
            if (id != model.ProductId) return BadRequest();

            var existing = Repository.Products?.FirstOrDefault(p => p.ProductId == id);
            if (existing == null) return NotFound();

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            string? randomFileName = null;
            string? filePath = null;

            if (imageFile != null)
            {
                var extension = Path.GetExtension(imageFile.FileName)?.ToLower();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Geçerli bir resim seçiniz");
                }
                else
                {
                    randomFileName = $"{Guid.NewGuid()}{extension}";
                    var imgFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");
                    if (!Directory.Exists(imgFolder)) Directory.CreateDirectory(imgFolder);
                    filePath = Path.Combine(imgFolder, randomFileName);
                }
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && filePath != null)
                {
                    using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // eski resmi sil (isteğe bağlı)
                    if (!string.IsNullOrEmpty(existing.Image))
                    {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", existing.Image);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    existing.Image = randomFileName;
                }

                // diğer alanları güncelle
                existing.Name = model.Name;
                existing.CategoryId = model.CategoryId;
                existing.Price = model.Price;
                // gerekliyse diğer alanları da ekleyin

                // Repository içerisinde ayrı bir Update metodu yoksa 'existing' zaten liste içindeki referansı günceller.
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(Repository.Categories ?? new List<Category>(), "CategoryId", "Name", model.CategoryId);
            return View(model);
        }
    }
}
