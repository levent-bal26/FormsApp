using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FormsApp.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

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

            // Arama filtresi
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products
                    .Where(p => !string.IsNullOrEmpty(p.Name) &&
                                p.Name.ToLower().Contains(searchString.ToLower()))
                    .ToList();
            }

            // Kategori filtresi
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
        public async Task<IActionResult> Create(Product model, IFormFile? imageFile)
        {
            if (imageFile == null)
            {
                ModelState.AddModelError("", "Resim seçiniz");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            string? randomFileName = null;

            if (imageFile != null)
            {
                var extension = Path.GetExtension(imageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Geçerli bir resim seçiniz");
                }
                else
                {
                    randomFileName = $"{Guid.NewGuid()}{extension}";
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", randomFileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                model.Image = randomFileName ?? "";
                model.ProductId = (Repository.Products?.Count ?? 0) + 1;
                Repository.CreateProduct(model);
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(Repository.Categories ?? new List<Category>(), "CategoryId", "Name");
            return View(model);
        }
    }
}
