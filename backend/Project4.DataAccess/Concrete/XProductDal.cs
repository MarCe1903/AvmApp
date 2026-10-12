using System.Linq.Expressions;
using Project4.DataAccess.Abstract;
using Project4.Entities;
using Project4.Entities.DTO;

namespace Project4.DataAccess
{
    public class XProductDal:IProductDal
    {
        List<Product> _products;

        public XProductDal()
        {
            _products = new List<Product>
            {
                new Product{ProductId = 1,ProductName = "Acer x Bilgisayar",QuantityPerUnit = "32 Gb Ram",UnitPrice = 10000,UnitsInStock = 10},
                new Product{ProductId = 2,ProductName = "Asus x Bilgisayar",QuantityPerUnit = "16 Gb Ram",UnitPrice = 5000,UnitsInStock = 20},
                new Product{ProductId = 3,ProductName = "Mac x Bilgisayar",QuantityPerUnit = "8 Gb Ram",UnitPrice = 20000,UnitsInStock = 30},
                new Product{ProductId = 4,ProductName = "Hp x Bilgisayar",QuantityPerUnit = "32 Gb Ram",UnitPrice = 7000,UnitsInStock = 0},
                new Product{ProductId = 5,ProductName = "Monster x Bilgisayar",QuantityPerUnit = "64 Gb Ram",UnitPrice = 15000,UnitsInStock = 50}
            };
        }
        public List<Product> GetAll()
        {
            return _products;
        }

        public Product GetById(int id)
        {
            throw new NotImplementedException();
        }

        public List<Product> GetAll(Expression<Func<Product, bool>> filter = null)
        {
            throw new NotImplementedException();
        }

        public Product Get(Expression<Func<Product, bool>> filter)
        {
            throw new NotImplementedException();
        }

        public void Add(Product product)
        {
            Console.WriteLine("X  ile eklendi");
        }

        public void Update(Product product)
        {
            throw new NotImplementedException();
        }

        public void Delete(Product product)
        {
            throw new NotImplementedException();
        }

        public List<ProductDetailDto> GetProductDetails()
        {
            throw new NotImplementedException();
        }

        public Task<List<Product>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Product> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(Product entity)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Product entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(Product entity)
        {
            throw new NotImplementedException();
        }
    }
}