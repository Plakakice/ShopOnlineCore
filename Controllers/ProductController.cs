using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShopOnlineCore.Models;
using Microsoft.EntityFrameworkCore;
using ShopOnlineCore.Services;

namespace ShopOnlineCore.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductService _productService;
        private readonly string _uploadPath;

        public ProductController(ApplicationDbContext context, IProductService productService)
        {
            _context = context;
            _productService = productService;
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        // =============================
        // Danh sách sản phẩm
        // =============================
        public async Task<IActionResult> Index(string? category, string? search, decimal? minPrice, decimal? maxPrice, string? sort, int page = 1, int pageSize = 8)
        {
            // Ép pageSize về tối đa 8 để đảm bảo mỗi trang chỉ hiển thị 8 sản phẩm
            pageSize = pageSize <= 0 ? 8 : Math.Min(pageSize, 8);
            var products = _context.Products.AsQueryable();

            var categoryQueryValue = category?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(categoryQueryValue))
            {
                // Filter by Category Name (via join)
                products = products.Include(p => p.CategoryObj)
                                   .Where(p => p.CategoryObj != null && p.CategoryObj.Name == categoryQueryValue);
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

            // Calculate Total Count BEFORE paging/sorting logic for random
            var totalCount = await products.CountAsync();
            List<Product> productList;

            // Sắp xếp
            if (string.IsNullOrEmpty(sort)) // Default (Random)
            {
                // Tối ưu hóa: Thay vì OrderBy(Guid.NewGuid()) ở DB (rất chậm), lấy ID về và xáo trộn ở Memory
                var allIds = await products.Select(p => p.Id).ToListAsync();
                var rnd = new Random();
                var shuffledIds = allIds.OrderBy(x => rnd.Next()).ToList();

                // Chỉ lấy những sản phẩm thuộc trang hiện tại (dựa trên shuffled IDs)
                var pagedIds = shuffledIds.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var pagedProducts = await _context.Products
                    .Where(p => pagedIds.Contains(p.Id))
                    .ToListAsync();
                
                // Sắp xếp lại danh sách kết quả theo thứ tự trong pagedIds
                productList = pagedProducts.OrderBy(p => pagedIds.IndexOf(p.Id)).ToList();
            }
            else
            {
                switch (sort)
                {
                    case "price_asc":
                        products = products.OrderBy(p => p.Price);
                        break;
                    case "price_desc":
                        products = products.OrderByDescending(p => p.Price);
                        break;
                }
                
                productList = await products
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }

            ViewData["SliderMin"] = sliderMin;
            ViewData["SliderMax"] = sliderMax;
            ViewData["FilterMin"] = minPrice ?? sliderMin;
            ViewData["FilterMax"] = maxPrice ?? sliderMax;
            ViewData["SelectedCategoryQuery"] = categoryQueryValue;
            ViewData["SelectedSearchQuery"] = searchQueryValue;
            ViewData["CurrentSort"] = sort; // Lưu trạng thái sort
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalCount"] = totalCount;

            // Fetch categories for sidebar
            ViewBag.Categories = await _context.Categories.ToListAsync();

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
            // Tối ưu: Dùng IProductService
            var relatedProducts = await _productService.GetRandomProductsAsync(4, id);

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
        public IActionResult Create()
        {
            ViewBag.Categories = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Categories, "Id", "Name");
            return View();
        }
    
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Create(Product product, List<IFormFile> files, int selectedMainImageIndex = 0)
        {
            if (ModelState.IsValid)
            {
                // Set Category string for backward compatibility (optional)
                var categoryObj = _context.Categories.Find(product.CategoryId);
                if (categoryObj != null) product.Category = categoryObj.Name;

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
                    {
                        // Nếu index hợp lệ thì dùng, không thì lấy ảnh đầu tiên
                        if (selectedMainImageIndex >= 0 && selectedMainImageIndex < product.ImageGallery.Count)
                        {
                            product.ImageUrl = product.ImageGallery[selectedMainImageIndex];
                        }
                        else
                        {
                            product.ImageUrl = product.ImageGallery.First();
                        }
                    }
                }

                _context.Products.Add(product);
                _context.SaveChanges();

                TempData["Message"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("Index");
            }
            ViewBag.Categories = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Categories, "Id", "Name", product.CategoryId);
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
            ViewBag.Categories = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, List<IFormFile>? files, List<string>? existingImages, string? selectedMainImage)
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

            // Cập nhật ảnh đại diện (ImageUrl)
            if (!string.IsNullOrEmpty(selectedMainImage) && old.ImageGallery.Contains(selectedMainImage))
            {
                // Nếu người dùng chọn ảnh cụ thể
                old.ImageUrl = selectedMainImage;
            }
            else if (old.ImageGallery.Any())
            {
                // Nếu ảnh hiện tại không còn trong danh sách (do bị xóa), chọn ảnh đầu tiên làm mặc định
                if (string.IsNullOrEmpty(old.ImageUrl) || !old.ImageGallery.Contains(old.ImageUrl))
                {
                    old.ImageUrl = old.ImageGallery.First();
                }
            }
            else
            {
                // Không còn ảnh nào
                old.ImageUrl = string.Empty;
            }

            old.Name = product.Name;
            old.CategoryId = product.CategoryId;
            var categoryObj = await _context.Categories.FindAsync(product.CategoryId);
            if (categoryObj != null) old.Category = categoryObj.Name; // Sync string
            
            old.Price = product.Price;
            old.SalePrice = product.SalePrice;
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
