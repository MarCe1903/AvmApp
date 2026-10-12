using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project4.Business;
using Project4.Entities;
using System;

namespace Project4.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : AppControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet("getall")]
        public IActionResult GetAll()
        {
            return FromApiDataResult(_productService.GetAll());
        }

        [HttpGet("getbyid")]
        public IActionResult GetById(int id)
        {
            return FromApiDataResult(_productService.GetById(id));
        }

        [HttpGet("getproductdetails")]
        public IActionResult GetProductDetails()
        {
            return FromApiDataResult(_productService.GetProductDetails());
        }

        [Authorize(Roles = "Admin,Seller")]
        [HttpPost("add")]
        public IActionResult Add(Product product)
        {
            return FromResult(_productService.Add(product));
        }

        [Authorize(Roles = "Admin,Seller")]
        [HttpPut("update")]
        public IActionResult Update(Product product)
        {
            try
            {
                return FromResult(_productService.Update(product));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatasi:{ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [Authorize(Roles = "Admin,Seller")]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            return FromResult(_productService.DeleteById(id));
        }
    }
}
