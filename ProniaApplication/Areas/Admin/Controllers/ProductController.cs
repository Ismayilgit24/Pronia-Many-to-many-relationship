﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.EntityFrameworkCore;
using ProniaApplication.Areas.ViewModels;
using ProniaApplication.Areas.ViewModels;
using ProniaApplication.DAL;
using ProniaApplication.Models;
using ProniaApplication.Utilities.Extension;

namespace ProniaApplication.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        public AppDBContext _context { get; }
        public IWebHostEnvironment _env { get; }
        public ProductController(AppDBContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }



        public async Task<IActionResult> Index()
        {
            List<GetProductAdminVM> productsVMs = await _context.Products
                .Include(p => p.category)
                .Include(p => p.productsImages.Where(pi => pi.IsPrimary == true))
                .Select(p => new GetProductAdminVM
                {
                    Name = p.Name,
                    Price = p.Price,
                    CategoryName = p.category.Name,
                    Image = p.productsImages[0].ImageURL,
                    Id = p.Id
                }
                )
                .ToListAsync();



            return View(productsVMs);
        }

        public async Task<IActionResult> Create()
        {
            CreateProductVM productVM = new CreateProductVM
            {
                Colors = await _context.Colors.ToListAsync(),
                Sizes = await _context.Sizes.ToListAsync(),
                Tags = await _context.Tags.ToListAsync(),
                Categories = await _context.Categories.ToListAsync()
            };
            return View(productVM);
        }

        [HttpPost]

        public async Task<IActionResult> Create(CreateProductVM productVM)
        {
            productVM.Categories = await _context.Categories.ToListAsync();
            productVM.Tags = await _context.Tags.ToListAsync();
            productVM.Colors = await _context.Colors.ToListAsync();
            productVM.Sizes = await _context.Sizes.ToListAsync();

            if (!ModelState.IsValid)
            {
                return View(productVM);
            }

            if (!productVM.MainPhoto.ValidateType("image/"))
            {
                ModelState.AddModelError(nameof(productVM.MainPhoto), "File type is incorrect");
                return View(productVM);
            }
            if (!productVM.MainPhoto.ValidateSize(Utilities.Enums.FileSize.MB, 2))
            {
                ModelState.AddModelError(nameof(productVM.MainPhoto), "File size is incorrect");
                return View(productVM);
            }
            if (!productVM.HoverPhoto.ValidateType("image/"))
            {
                ModelState.AddModelError(nameof(productVM.HoverPhoto), "File type is incorrect");
                return View(productVM);
            }
            if (!productVM.HoverPhoto.ValidateSize(Utilities.Enums.FileSize.MB, 2))
            {
                ModelState.AddModelError(nameof(productVM.HoverPhoto), "File size is incorrect");
                return View(productVM);
            }


            bool result = productVM.Categories.Any(c=>c.Id == productVM.CategoryId);

            if (!result)
            {
                ModelState.AddModelError(nameof(CreateProductVM), "Category does not exist");
                return View(productVM);
            }


            if(productVM.Tags is not null)
            {
                bool tagResult = productVM.TagIds.Any(tId => !productVM.Tags.Exists(t => t.Id == tId));

                if (tagResult)
                {
                    ModelState.AddModelError(nameof(CreateProductVM.TagIds), "Tags are wrong");
                    return View(productVM);
                }
            }

            if (productVM.Colors is not null)
            {
                bool colorResult = productVM.ColorIds.Any(cId => !productVM.Colors.Exists(c => c.Id == cId));

                if (colorResult)
                {
                    ModelState.AddModelError(nameof(CreateProductVM.ColorIds), "Colors are wrong");
                    return View(productVM);
                }
            }
            if (productVM.Sizes is not null)
            {
                bool sizeResult = productVM.SizeIds.Any(sId => !productVM.Sizes.Exists(s => s.Id == sId));

                if (sizeResult)
                {
                    ModelState.AddModelError(nameof(CreateProductVM.SizeIds), "Colors are wrong");
                    return View(productVM);
                }
            }

            ProductsImage main = new()
            {
                ImageURL = await productVM.MainPhoto.CreatFileAsync(_env.WebRootPath, "assets", "images", "website-images"),
                IsPrimary = true,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };
            ProductsImage hover = new()
            {
                ImageURL = await productVM.HoverPhoto.CreatFileAsync(_env.WebRootPath, "assets", "images", "website-images"),
                IsPrimary = false,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };





            Product product = new()
            {
                Name = productVM.Name,
                SKU = productVM.SKU,
                CategoryId = productVM.CategoryId.Value,
                Description = productVM.Description,
                Price = productVM.Price.Value,
                CreatedAt = DateTime.Now,
                IsDeleted = false,
                productsImages = new List<ProductsImage> { main, hover }
            };

            if(productVM.TagIds is not null)
            {
                product.ProductTags = productVM.TagIds.Select(tId => new ProductTag { TagId = tId }).ToList();
            }
            if(productVM.ColorIds is not null)
            {
                product.ProductColors = productVM.ColorIds.Select(cId => new ProductColor { ColorId = cId }).ToList();
            }
            if (productVM.SizeIds is not null)
            {
                product.ProductSizes = productVM.SizeIds.Select(sId => new ProductSize { SizeId = sId }).ToList();
            }

            //foreach (int tId in productVM.TagIds)
            //{
            //    product.ProductTags.Add(new ProductTag
            //    {
            //        TagId = tId,
            //    });
            //}

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));


           
        }

        public async Task<IActionResult> Update(int? id)
        {
            if (id is null || id < 1) return BadRequest();
            Product product = await _context.Products.Include(p=>p.ProductTags).Include(p=>p.ProductColors).FirstOrDefaultAsync(p => p.Id == id);
            if (product is null) return NotFound();

            

            UpdateProductVM productVM = new()
            {
                Name = product.Name,
                SKU = product.SKU,
                CategoryId = product.CategoryId,
                Price = product.Price,
                Description = product.Description,
                TagIds = product.ProductTags.Select(pt => pt.TagId).ToList(),
                ColorIds = product.ProductColors.Select(pc => pc.ColorId).ToList(),
                Categories = await _context.Categories.ToListAsync(),
                Tags = await _context.Tags.ToListAsync(),
                Colors = await _context.Colors.ToListAsync(),
                Sizes = await _context.Sizes.ToListAsync()
            };

            return View(productVM);
        }

        [HttpPost]
        public async Task<IActionResult> Update(int? id, UpdateProductVM productVM)
        {
            if (id is null || id < 1) return BadRequest();
            productVM.Categories = await _context.Categories.ToListAsync();
            productVM.Tags = await _context.Tags.ToListAsync();
            productVM.Colors = await _context.Colors.ToListAsync();
            productVM.Sizes = await _context.Sizes.ToListAsync();

            if (!ModelState.IsValid)
            {
                return View(productVM);
            }
            Product existed = await _context.Products.Include(p=>p.ProductTags).Include(p=>p.ProductColors).Include(p=>p.ProductSizes).FirstOrDefaultAsync(p => p.Id == id);
           
            if (existed is null) return NotFound();

            if(existed.CategoryId != productVM.CategoryId)
            {
                bool result = productVM.Categories.Any(c => c.Id == productVM.CategoryId);

                if (!result)
                {
                    return View(productVM);
                }
            }


            if (productVM.Tags is not null)
            {
                bool tagResult = productVM.TagIds.Any(tId => !productVM.Tags.Exists(t => t.Id == tId));

                if (tagResult)
                {
                    ModelState.AddModelError(nameof(UpdateProductVM.TagIds), "Tags are wrong");
                    return View(productVM);
                }
            }

           if(productVM.TagIds is null)
            {
                productVM.TagIds = new();
            }
                _context.ProductTags
                    .RemoveRange(existed.ProductTags
                    .Where(pTag =>!productVM.TagIds
                    .Exists(tId => tId == pTag.TagId))
                    .ToList());



                _context.ProductTags.AddRange(productVM.TagIds
                    .Where(tId=>!existed.ProductTags.Exists(pTag => pTag.TagId == tId))
                    .ToList()
                    .Select(tId => new ProductTag { TagId = tId, ProductId = existed.Id }));


            if (productVM.Colors is not null)
            {
                bool colorResult = productVM.ColorIds.Any(cId => !productVM.Colors.Exists(c => c.Id == cId));

                if (colorResult)
                {
                    ModelState.AddModelError(nameof(UpdateProductVM.ColorIds), "Tags are wrong");
                    return View(productVM);
                }
            }
            if (productVM.ColorIds is null)
            {
                productVM.ColorIds = new();
            }
            _context.ProductColors
                    .RemoveRange(existed.ProductColors
                    .Where(pColor => !productVM.ColorIds
                    .Exists(cId => cId == pColor.ColorId))
                    .ToList());



            _context.ProductColors
                .AddRange(productVM.ColorIds
                .Where(cId => !existed.ProductColors.Exists(pColor => pColor.ColorId == cId))
                .ToList()
                .Select(cId => new ProductColor { ColorId = cId, ProductId = existed.Id }));


            if (productVM.Sizes is not null)
            {
                bool sizeResult = productVM.SizeIds.Any(sId => !productVM.Sizes.Exists(s => s.Id == sId));

                if (sizeResult)
                {
                    ModelState.AddModelError(nameof(UpdateProductVM.SizeIds), "Sizes are wrong");
                    return View(productVM);
                }
            }
            if (productVM.SizeIds is null)
            {
                productVM.SizeIds = new();
            }
            _context.ProductSizes
                    .RemoveRange(existed.ProductSizes
                    .Where(pSize => !productVM.SizeIds
                    .Exists(sId => sId == pSize.SizeId))
                    .ToList());



            _context.ProductSizes
                .AddRange(productVM.SizeIds
                .Where(sId => !existed.ProductSizes.Exists(pSize => pSize.SizeId == sId))
                .ToList()
                .Select(sId => new ProductSize { SizeId = sId, ProductId = existed.Id }));


            existed.SKU = productVM.SKU;
            existed.Price = productVM.Price.Value;
            existed.Name = productVM.Name;
            existed.CategoryId = productVM.CategoryId.Value;
            existed.Description = productVM.Description;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
