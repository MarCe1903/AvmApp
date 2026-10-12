using System.Linq.Expressions;
using Core.DataAccess.EntityFramework;
using Project4.Entities.DTO;
using Microsoft.EntityFrameworkCore;
using Project4.Entities;

namespace Project4.DataAccess
{
    public class EfOrderDal : EfEntityRepositoryBase<Order, NorthwindContext>, IOrderDal

    {
        public List<OrderInformationDto> GetOrderInformations()
        {
            try
            {
                using (var context = new NorthwindContext())
                {
                    var result = from o in context.Orders
                        join c in context.Customers on o.CustomerId equals c.CustomerId
                        join s in context.OrderStatuses on o.OrderStatusId equals s.Id
                        select new OrderInformationDto
                        {
                            OrderId = o.OrderId,
                            CustomerName = c.ContactName,
                            OrderStatus = s.Description
                        };

                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" GetOrderInformations hatası: " + ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine("🔍 Inner Exception: " + ex.InnerException.Message);
                throw;
            }
        }

    }
}