using Microsoft.EntityFrameworkCore;
using Project4.Entities;
using Project4.Entities.DTO;
using System.Linq;
using Project4.DataAccess;
using Core.DataAccess.EntityFramework;
using Project4.DataAccess.Abstract;

namespace Project4.DataAccess
{
   
    public class EfProductDal:EfEntityRepositoryBase<Product,NorthwindContext>,IProductDal
    {
        List<Product> _products;
        public EfProductDal()
        {
            
        }
        public List<Product> GetAll()
        {
            using (NorthwindContext context = new NorthwindContext())
            {
                return context.Products.ToList();
            };
            //dispose(bellekten hizlica atmak istedigimiz yontem)
        }
        
        public Task<List<Product>> GetAllAsync()
        {
            NorthwindContext context = new NorthwindContext();
            return context.Products.ToListAsync();
        }
        public Product GetById(int id)
        {
            using (NorthwindContext context = new NorthwindContext())
            {
                return context.Products.SingleOrDefault(p => p.ProductId == id);
            }
        }
        public Task<Product> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }
      

        public void Add(Product product)
        {
            using (NorthwindContext context = new NorthwindContext())
            {
                context.Products.Add(product);
                context.SaveChanges();
            }
        }
        
        //Async ile calisirken disposeble calismaktan cekinmemiz gerekiyor islem yarim kalmasin diye ondan dolayi using kullanmamamiz gerekiyor
        public async Task AddAsync(Product entity)
        {
            NorthwindContext context = new NorthwindContext();
            await context.Products.AddAsync(entity);
            await context.SaveChangesAsync();
        }
       
        public void Update(Product product)
        {
            using (NorthwindContext context = new NorthwindContext())
            {
                var productToUpdate = context.Products.SingleOrDefault(p => p.ProductId == product.ProductId);
                productToUpdate.ProductName = product.ProductName;
                productToUpdate.UnitPrice = product.UnitPrice;
                productToUpdate.UnitsInStock = product.UnitsInStock;
                productToUpdate.QuantityPerUnit = product.QuantityPerUnit;
                productToUpdate.CategoryId = product.CategoryId;
               
                context.SaveChanges();
            }
        }
        public Task UpdateAsync(Product entity)
        {
            throw new NotImplementedException();
        }
       
        public void Delete(Product product)
        {
            using (NorthwindContext context = new NorthwindContext())
            {
                context.Products.Remove(context.Products.SingleOrDefault(p => p.ProductId == product.ProductId));//burada direkt databasedekiyle bag kurmamiz lazim ki calissin yontem
                context.SaveChanges();
            }
        }
        public Task DeleteAsync(Product entity)
        {
            throw new NotImplementedException();
        }

        public List<ProductDetailDto> GetProductDetails()
        {
            using (NorthwindContext context = new NorthwindContext())
            {
                var result = from p in context.Products
                    join c in context.Categories
                        on p.CategoryId equals c.CategoryId
                    select new ProductDetailDto
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        CategoryName = c.CategoryName,
                        UnitsInStock = p.UnitsInStock,
                        UnitPrice = p.UnitPrice,
                        CategoryId=p.CategoryId
                    };
                return result.ToList();
            }
        }
    }
    
}
