using Project4.DataAccess;
using Project4.Entities;

namespace Project4.Business
{
    public class PersonelManager:IPersonelService
    { 
        IPersonelDal _personelDal;
        public PersonelManager(IPersonelDal personelDal)
        {
            _personelDal = personelDal;
        }

        public List<Personel> GetAll()
        {
          return _personelDal.GetAll();
        }
    }
}