using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Project4.Business;
using Project4.Entities;
using Project4.WebAPI.Services;
using System;
using System.Threading.Tasks;

namespace Project4.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderStatusesController : AppControllerBase
    {
        private readonly IOrderStatusService _service;
        private readonly IEmailService _emailService;
        private readonly string? _notificationRecipient;

        public OrderStatusesController(IOrderStatusService service, IEmailService emailService, IOptions<MailSettings> mailSettings)
        {
            _service = service;
            _emailService = emailService;
            _notificationRecipient = mailSettings.Value.OrderStatusRecipient;
        }

        [HttpGet("getall")]
        public IActionResult GetAll()
        {
            return FromApiDataResult(_service.GetAll());
        }

        [HttpGet("getbyid/{id}")]
        public IActionResult GetById(int id)
        {
            return FromApiDataResult(_service.GetById(id));
        }

        [HttpPost("add")]
        public IActionResult Add([FromBody] OrderStatus status)
        {
            return FromResult(_service.Add(status));
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] OrderStatus status)
        {
            var result = _service.Update(status);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            if (!string.IsNullOrWhiteSpace(_notificationRecipient) &&
                (status.Code == "ON_ROAD" || status.Code == "DELIVERED"))
            {
                try
                {
                    await _emailService.SendAsync(
                        _notificationRecipient,
                        "Siparis durumu guncellendi",
                        $"<p>Siparis durumunuz: <b>{status.Description}</b></p>");

                    Console.WriteLine($"Mail sent for status:{status.Code}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Mail send failed:{ex.Message}");
                }
            }

            return Ok(result);
        }

        [HttpDelete("delete/{id}")]
        public IActionResult Delete(int id)
        {
            return FromResult(_service.DeleteById(id));
        }
    }
}
