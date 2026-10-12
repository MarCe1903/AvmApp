using Microsoft.AspNetCore.Mvc;
using Project4.Business.Abstract;
using Project4.Entities;
using System;

namespace Project4.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : AppControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("getorderinformations")]
        public IActionResult GetOrderInformations()
        {
            return FromApiDataResult(_orderService.GetOrderInformations());
        }

        [HttpGet("getall")]
        public IActionResult GetAll()
        {
            return FromApiDataResult(_orderService.GetAll());
        }

        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            return FromApiDataResult(_orderService.GetById(id));
        }

        [HttpPost("add")]
        public IActionResult Add(Order order)
        {
            return FromResult(_orderService.Add(order));
        }

        [HttpPut("update")]
        public IActionResult Update(Order order)
        {
            try
            {
                return FromResult(_orderService.Update(order));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatasi:{ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpDelete("delete/{id}")]
        public IActionResult Delete(int id)
        {
            return FromResult(_orderService.DeleteById(id));
        }
    }
}
