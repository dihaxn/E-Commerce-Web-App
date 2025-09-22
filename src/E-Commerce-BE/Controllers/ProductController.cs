using E_Commerce_BE.Models;
using E_Commerce_BE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce_BE.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("/Admin/[controller]/{action=Index}/{id?}")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly ISecureFileUploadService fileUploadService;
        private readonly ISanitizationService _sanitizationService;
        private readonly int pageSize = 5;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment environment,
            ISecureFileUploadService fileUploadService, ISanitizationService sanitizationService)
        {
            this.context = context;
            this.environment = environment;
            this.fileUploadService = fileUploadService;
            _sanitizationService = sanitizationService;
        }

        public IActionResult Index(int pageIndex, string? search, string? column, string? orderBy)
        {
            IQueryable<Product> query = context.Products;

            // Search functionality
            if (search != null)
            {
                var sanitizedSearch = _sanitizationService.Sanitize(search);
                query = query.Where(p => p.Name.Contains(sanitizedSearch) || p.Brand.Contains(sanitizedSearch));
            }

            // Sort functionality
            string[] validColumns = { "Id", "Name", "Brand", "Category", "Price", "CreatedAt" };
            string[] validOrderBy = { "desc", "asc" };

            if (!validColumns.Contains(column))
            {
                column = "Id";
            }

            if (!validOrderBy.Contains(orderBy))
            {
                orderBy = "desc";
            }

            if (column == "Name")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Name);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Name);
                }
            }
            else if (column == "Brand")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Brand);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Brand);
                }
            }
            else if (column == "Category")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Category);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Category);
                }
            }
            else if (column == "Price")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Price);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Price);
                }
            }
            else if (column == "CreatedAt")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.CreatedAt);
                }
                else
                {
                    query = query.OrderByDescending(p => p.CreatedAt);
                }
            }
            else
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Id);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Id);
                }
            }

            // pagination
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            decimal count = query.Count();
            int totalPages = (int)Math.Ceiling(count / pageSize);
            query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

            var products = query.ToList();

            ViewData["PageIndex"] = pageIndex;
            ViewData["TotalPages"] = totalPages;
            ViewData["Search"] = search ?? "";

            ViewData["Column"] = column;
            ViewData["OrderBy"] = orderBy;

            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDto productDto)
        {
            if (productDto.ImageFile == null)
            {
                ModelState.AddModelError("ImageFile", "The image file is required");
            }

            if (!ModelState.IsValid)
            {
                return View(productDto);
            }

            // Use secure file upload service
            var (isValid, fileName, errorMessage) = await fileUploadService.ValidateAndSaveFileAsync(productDto.ImageFile!);

            if (!isValid)
            {
                ModelState.AddModelError("ImageFile", errorMessage ?? "An error occurred during file upload.");
                return View(productDto);
            }

            // save the new product in the database
            Product product = new Product()
            {
                Name = _sanitizationService.Sanitize(productDto.Name),
                Brand = _sanitizationService.Sanitize(productDto.Brand),
                Category = _sanitizationService.Sanitize(productDto.Category),
                Price = productDto.Price,
                Description = _sanitizationService.Sanitize(productDto.Description ?? ""),
                ImageFileName = fileName,
                CreatedAt = DateTime.UtcNow,
            };

            context.Products.Add(product);
            context.SaveChanges();

            return RedirectToAction("Index", "Product");
        }

        public IActionResult Edit(int id)
        {
            var product = context.Products.Find(id);
            if (product == null)
            {
                return RedirectToAction("Index", "Product");
            }
            var productDto = new ProductDto()
            {
                Name = product.Name,
                Brand = product.Brand,
                Category = product.Category,
                Price = product.Price,
                Description = product.Description,
            };

            ViewData["ProductId"] = product.Id;
            ViewData["ImageFileName"] = product.ImageFileName;
            ViewData["CreatedAt"] = product.CreatedAt.ToString("MM/dd/yyyy");

            return View(productDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductDto productDto)
        {
            var product = context.Products.Find(id);

            if (product == null)
            {
                return RedirectToAction("Index", "Product");
            }

            if (!ModelState.IsValid)
            {
                ViewData["ProductId"] = product.Id;
                ViewData["ImageFileName"] = product.ImageFileName;
                ViewData["CreatedAt"] = product.CreatedAt.ToString("MM/dd/yyyy");
                return View(productDto);
            }

            // update the image file if it is not null
            string newFileName = product.ImageFileName;
            if (productDto.ImageFile != null)
            {
                var (isValid, fileName, errorMessage) = await fileUploadService.ValidateAndSaveFileAsync(productDto.ImageFile);

                if (!isValid)
                {
                    ModelState.AddModelError("ImageFile", errorMessage ?? "An error occurred during file upload.");
                    ViewData["ProductId"] = product.Id;
                    ViewData["ImageFileName"] = product.ImageFileName;
                    ViewData["CreatedAt"] = product.CreatedAt.ToString("MM/dd/yyyy");
                    return View(productDto);
                }

                // Delete the old image file
                fileUploadService.DeleteFile(product.ImageFileName);
                newFileName = fileName;
            }

            // update the product in the database
            product.Name = _sanitizationService.Sanitize(productDto.Name);
            product.Brand = _sanitizationService.Sanitize(productDto.Brand);
            product.Category = _sanitizationService.Sanitize(productDto.Category);
            product.Price = productDto.Price;
            product.Description = _sanitizationService.Sanitize(productDto.Description ?? "");
            product.ImageFileName = newFileName;

            context.SaveChanges();

            return RedirectToAction("Index", "Product");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var product = context.Products.Find(id);
            if (product == null)
            {
                return RedirectToAction("Index", "Product");
            }

            // Delete the image file using secure service
            fileUploadService.DeleteFile(product.ImageFileName);

            // delete the product from the database
            context.Products.Remove(product);
            context.SaveChanges(true);
            return RedirectToAction("Index", "Product");
        }
    }
}
