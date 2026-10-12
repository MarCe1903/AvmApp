using System.Linq.Expressions;
using Project4.Entities;

namespace Project4.DataAccess
{
    public class EfPersonelDal:IPersonelDal
    {
        public List<Personel> GetAll()
        {
            using (NorthwindContext context = new NorthwindContext())
            {
                return context.Personels.ToList();
            }
        }

        public Personel GetById(int id)
        {
            throw new NotImplementedException();
        }

        public List<Personel> GetAll(Expression<Func<Personel, bool>> filter = null)
        {
            throw new NotImplementedException();
        }

        public Personel Get(Expression<Func<Personel, bool>> filter)
        {
            throw new NotImplementedException();
        }

        public void Add(Personel entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Personel entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(Personel entity)
        {
            throw new NotImplementedException();
        }
    }
}