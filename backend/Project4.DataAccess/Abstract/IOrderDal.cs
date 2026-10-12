using Core.DataAccess;
using Project4.Entities;
using Project4.Entities.DTO;

namespace Project4.DataAccess
{
    public interface IOrderDal:IEntityRepository<Order>
    {
        List<OrderInformationDto> GetOrderInformations();
    }
}