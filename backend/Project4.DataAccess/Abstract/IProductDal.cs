using Project4.Entities;
using Project4.Entities.DTO;
using Core.DataAccess;
using System.Collections.Generic;

namespace Project4.DataAccess.Abstract
{
    public interface IProductDal:IEntityRepository<Product>,IAsyncEntityRepository<Product>
    { 
        List<ProductDetailDto> GetProductDetails();
        
    }
    
}

