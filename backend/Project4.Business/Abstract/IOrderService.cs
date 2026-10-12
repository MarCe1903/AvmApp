using Core.Utilities.Results;
using Project4.Entities;
using Project4.Entities.DTO;

namespace Project4.Business.Abstract
{
    public interface IOrderService
    {
        IDataResult<List<Order>> GetAll();
        IDataResult<Order> GetById(int id);
        IResult Add(Order order);
        IResult Update(Order order);
        IResult DeleteById(int id);
        IDataResult<List<OrderInformationDto>> GetOrderInformations();
    }
}