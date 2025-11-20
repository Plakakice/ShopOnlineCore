using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShopOnlineCore.Models;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Index(string? category, string? search, decimal? minPrice, decimal? maxPrice, int page = 1, int pageSize = 6)
        {
            // Ép pageSize về tối đa 6 để đảm bảo mỗi trang chỉ hiển thị 6 sản phẩm
            pageSize = pageSize <= 0 ? 6 : Math.Min(pageSize, 6);
            var products = _context.Products.AsQueryable();

            var categoryQueryValue = category?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(categoryQueryValue))
            {
                // Tránh dùng ToLower/ToUpper trên DB SQLite (không xử lý Unicode đầy đủ)
                // So khớp theo nguyên bản; mỗi danh mục tách biệt, không alias
                products = products.Where(p => p.Category != null &&
                                               (p.Category == categoryQueryValue ||
                                                p.Category.Contains(categoryQueryValue)));
                ViewData["CategoryFilter"] = categoryQueryValue;
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

            var totalCount = await products.CountAsync();
            var productList = await products
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["SliderMin"] = sliderMin;
            ViewData["SliderMax"] = sliderMax;
            ViewData["FilterMin"] = minPrice ?? sliderMin;
            ViewData["FilterMax"] = maxPrice ?? sliderMax;
            ViewData["SelectedCategoryQuery"] = categoryQueryValue;
            ViewData["SelectedSearchQuery"] = searchQueryValue;
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalCount"] = totalCount;

            return View(productList);
        }

        // =============================
        // Chi tiết sản phẩm
        // =============================
        public async Task<IActionResult> Details(int id, int? showMessage = null)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            // Lấy 4 sản phẩm ngẫu nhiên khác (có thể bạn sẽ thích)
            var relatedProducts = await _context.Products
                .Where(p => p.Id != id)
                .OrderBy(p => Guid.NewGuid())
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;
            
            // Nếu có showMessage, gán vào ViewBag để hiển thị
            if (showMessage == 1 && TempData["Success"] != null)
            {
                ViewBag.SuccessMessage = TempData["Success"];
            }
            
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
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, List<IFormFile>? files, List<string>? existingImages)
        {
            var old = await _context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            if (old == null) return NotFound();

            // Cập nhật danh sách ảnh: giữ lại các ảnh được chọn (existingImages)
            if (existingImages != null && existingImages.Any())
            {
                old.ImageGallery = existingImages.ToList();
            }
            else
            {
                old.ImageGallery = new List<string>();
            }

            // Thêm ảnh mới nếu có upload
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
            }

            // Đảm bảo ImageUrl luôn có giá trị
            if (old.ImageGallery.Any())
            {
                old.ImageUrl = old.ImageGallery.First();
            }

            old.Name = product.Name;
            old.Category = product.Category;
            old.Price = product.Price;
            old.Description = product.Description;
            old.Stock = product.Stock;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật sản phẩm thành công!";
            return RedirectToAction("Index");
        }

        // =============================
        // XÓA (Admin)
        // =============================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
