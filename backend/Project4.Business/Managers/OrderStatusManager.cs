using Core.Utilities.Results;
using Project4.DataAccess.Abstract;
using Project4.Entities;

namespace Project4.Business
{
    public class OrderStatusManager:IOrderStatusService
    {
        private readonly IOrderStatusDal _statusDal;

        public OrderStatusManager(IOrderStatusDal statusDal)
        {
            _statusDal = statusDal;
        }
        public IDataResult<List<OrderStatus>> GetAll() => new SuccessDataResult<List<OrderStatus>>(_statusDal.GetAll());
        public IDataResult<OrderStatus> GetById(int id) =>new SuccessDataResult<OrderStatus>(_statusDal.Get(x => x.Id == id));
        public IResult Add(OrderStatus status)
        {
            _statusDal.Add(status);
            return new SuccessResult("Durum eklendi.");
        }

        public IResult Update(OrderStatus status)
        {
            _statusDal.Update(status);
            return new SuccessResult("Durum guncellendi");
        }

        public IResult DeleteById(int id)
        {
            var entity = _statusDal.Get(x => x.Id == id);
            if (entity == null) return new ErrorResult("Durum bulunamadi");
            _statusDal.Delete(entity);
            return new SuccessResult("Durum silindi");
        }

        // public IResult Delete(int id)
        // {
        //     var entity = _statusDal.Get(x => x.Id == id);
        //     if (entity == null) return new ErrorResult("Durum bulunamadi");
        //     _statusDal.Delete(entity);
        //     return new SuccessResult("Durum silindi");
        // }
    }
}