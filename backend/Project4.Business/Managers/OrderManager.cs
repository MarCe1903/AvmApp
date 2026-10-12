using Core.Utilities.Results;
using Project4.Business.Abstract;
using Project4.DataAccess.Abstract;
using Project4.Entities;
using System.Collections.Generic;
using Project4.DataAccess;
using Project4.Entities.DTO;

namespace Project4.Business.Concrete
{
    public class OrderManager : IOrderService
    {
        private readonly IOrderDal _orderDal;

        // ✅ Constructor Dependency Injection
        public OrderManager(IOrderDal orderDal)
        {
            _orderDal = orderDal;
        }

        public IDataResult<List<Order>> GetAll()
        {
            var orders = _orderDal.GetAll();
            return new SuccessDataResult<List<Order>>(orders, "Siparişler listelendi.");
        }

        public IDataResult<Order> GetById(int id)
        {
            var order = _orderDal.Get(o => o.OrderId == id);
            if (order == null)
                return new ErrorDataResult<Order>("Sipariş bulunamadı.");
            return new SuccessDataResult<Order>(order, "Sipariş bulundu.");
        }

        public IResult Add(Order order)
        {
            _orderDal.Add(order);
            return new SuccessResult("Sipariş eklendi.");
        }

        public IResult Update(Order order)
        {
            _orderDal.Update(order);
            return new SuccessResult("Sipariş güncellendi.");
        }

        public IResult DeleteById(int id)
        {
            var order = _orderDal.Get(o => o.OrderId == id);
            if (order == null)
                return new ErrorResult("Silinecek sipariş bulunamadı.");

            _orderDal.Delete(order);
            return new SuccessResult("Sipariş silindi.");
        }

        // public IResult Delete(int id)
        // {
        //     var order = _orderDal.Get(o => o.OrderId == id);
        //     if (order == null)
        //         return new ErrorResult("Silinecek sipariş bulunamadı.");
        //
        //     _orderDal.Delete(order);
        //     return new SuccessResult("Sipariş silindi.");
        // }

        // 🔹 Özel method: OrderStatus + Customer bilgilerini döndürür
        public IDataResult<List<OrderInformationDto>> GetOrderInformations()
        {
            var result = _orderDal.GetOrderInformations();
            return new SuccessDataResult<List<OrderInformationDto>>(result, "Sipariş detayları getirildi.");
        }
    }
}