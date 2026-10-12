using Core.DataAccess.EntityFramework;
using Project4.DataAccess.Abstract;
using Project4.Entities;

namespace Project4.DataAccess
{
    public class EfOrderStatusDal:EfEntityRepositoryBase<OrderStatus,NorthwindContext>,IOrderStatusDal
    {
        
    }
}