using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShopOnlineCore.Models;

namespace ShopOnlineCore.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _uploadPath;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        // =============================
        // Danh sách sản phẩm
        // =============================
        public IActionResult Index(string? category, string? search, decimal? minPrice, decimal? maxPrice)
        {
            var products = _context.Products.AsQueryable();

            var categoryQueryValue = category?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(categoryQueryValue))
            {
                var normalizedCategory = categoryQueryValue.ToLower();
                products = products.Where(p => p.Category.ToLower().Contains(normalizedCategory));
                ViewData["CategoryFilter"] = char.ToUpper(normalizedCategory[0]) + normalizedCategory.Substring(1);
            }

            var searchQueryValue = search?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(searchQueryValue))
            {
                var searchValue = searchQueryValue.ToLower();
                products = products.Where(p =>
                    (!string.IsNullOrEmpty(p.Name) && p.Name.ToLower().Contains(searchValue)) ||
                    (!string.IsNullOrEmpty(p.Category) && p.Category.ToLower().Contains(searchValue)) ||
                    (!string.IsNullOrEmpty(p.Description) && p.Description.ToLower().Contains(searchValue)));
                ViewData["SearchTerm"] = searchQueryValue;
            }

            var priceQuery = _context.Products.Select(p => p.Price);
            var hasProducts = priceQuery.Any();
            decimal sliderMin = hasProducts ? priceQuery.Min() : 0m;
            decimal sliderMax = hasProducts ? priceQuery.Max() : 0m;

            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            {
                (minPrice, maxPrice) = (maxPrice, minPrice);
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value);
            }

            var productList = products.OrderByDescending(p => p.Id).ToList();

            ViewData["SliderMin"] = sliderMin;
            ViewData["SliderMax"] = sliderMax;
            ViewData["FilterMin"] = minPrice ?? sliderMin;
            ViewData["FilterMax"] = maxPrice ?? sliderMax;
            ViewData["SelectedCategoryQuery"] = categoryQueryValue;
            ViewData["SelectedSearchQuery"] = searchQueryValue;

            return View(productList);
        }

        // =============================
        // Chi tiết sản phẩm
        // =============================
        public IActionResult Details(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // =============================
        // TẠO (Admin)
        // =============================
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Create(Product product, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                product.ImageGallery = new List<string>();

                if (files != null && files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(_uploadPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }
                        var relativePath = "/images/products/" + fileName;
                        product.ImageGallery.Add(relativePath);
                    }

                    if (product.ImageGallery.Any())
                        product.ImageUrl = product.ImageGallery.First();
                }

                _context.Products.Add(product);
                _context.SaveChanges();

                TempData["Message"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("Index");
            }
            return View(product);
        }

        // =============================
        // SỬA (Admin)
        // =============================
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Edit(Product product, List<IFormFile> files)
        {
            var old = _context.Products.FirstOrDefault(p => p.Id == product.Id);
            if (old == null) return NotFound();

            if (files != null && files.Count > 0)
            {
                old.ImageGallery ??= new List<string>();

                foreach (var file in files)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(_uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }
                    var relativePath = "/images/products/" + fileName;
                    old.ImageGallery.Add(relativePath);
                }

                if (string.IsNullOrEmpty(old.ImageUrl))
                    old.ImageUrl = old.ImageGallery.First();
            }

            old.Name = product.Name;
            old.Category = product.Category;
            old.Price = product.Price;
            old.Description = product.Description;

            _context.SaveChanges();
            TempData["Message"] = "Cập nhật thành công!";
            return RedirectToAction("Index");
        }

        // =============================
        // XÓA (Admin)
        // =============================
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
