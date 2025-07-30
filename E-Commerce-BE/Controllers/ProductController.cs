using E_Commerce_BE.Models;
using E_Commerce_BE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce_BE.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly int pageSize = 5;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            this.context = context;
            this.environment = environment;
        }

        public IActionResult Index(int pageIndex  , string? search)
        {

            IQueryable<Product> query = context.Products;


            // Search functionality
            if (search != null)
            {
                query = query.Where(p => p.Name.Contains(search) || p.Brand.Contains(search));
            }


            query = query.OrderByDescending(p => p.Id);

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

            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(ProductDto productDto)
        {
            if (productDto.ImageFile == null)
            {

                ModelState.AddModelError("ImageFile", "The image file is required");
            }

            if (!ModelState.IsValid)
            {

                return View(productDto);
            }

            // save the image file
            string newFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            newFileName += Path.GetExtension(productDto.ImageFile!.FileName);

            string imageFullPath = environment.WebRootPath + "/products/" + newFileName;
            using (var stream = System.IO.File.Create(imageFullPath))
            {
                productDto.ImageFile.CopyTo(stream);
            }

            // save the new product in the database
            Product product = new Product()
            {
                Name = productDto.Name,
                Brand = productDto.Brand,
                Category = productDto.Category,
                Price = productDto.Price,
                Description = productDto.Description,
                ImageFileName = newFileName,
                CreatedAt = DateTime.Now,
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
        public IActionResult Edit(int id, ProductDto productDto)
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
                newFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                newFileName += Path.GetExtension(productDto.ImageFile!.FileName);



                string imageFullPath = environment.WebRootPath + "/products/" + newFileName;
                using (var stream = System.IO.File.Create(imageFullPath))
                {
                    productDto.ImageFile.CopyTo(stream);

                }
                // delete the old image file
                string oldImageFullPath = environment.WebRootPath + "/products/" + product.ImageFileName;
                System.IO.File.Delete(oldImageFullPath);

            }


            // update the product in the database
            product.Name = productDto.Name;
            product.Brand = productDto.Brand;
            product.Category = productDto.Category;
            product.Price = productDto.Price;
            product.Description = productDto.Description;
            product.ImageFileName = newFileName;

            context.SaveChanges();

            return RedirectToAction("Index", "Product");


        }


        public IActionResult Delete(int id)
        {
            var product = context.Products.Find(id);
            if (product == null)
            {
                return RedirectToAction("Index", "Product");
            }
            // delete the image file
            string imageFullPath = environment.WebRootPath + "/products/" + product.ImageFileName;
            System.IO.File.Delete(imageFullPath);
            // delete the product from the database
            context.Products.Remove(product);
            context.SaveChanges(true);
            return RedirectToAction("Index", "Product");

        }
    }
}
