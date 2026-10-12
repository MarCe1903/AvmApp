using Core.Utilities.Results;
using Project4.Entities;

namespace Project4.Business
{
    public interface IOrderStatusService
    {
        IDataResult<List<OrderStatus>> GetAll();
        IDataResult<OrderStatus> GetById(int id);
        IResult Add(OrderStatus status);
        IResult Update(OrderStatus status);
        IResult DeleteById(int id);
    }
}