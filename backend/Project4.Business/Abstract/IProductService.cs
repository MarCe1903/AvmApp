// Business/Abstract/IProductService.cs

using Core.Utilities.Results; // Result yapıları için bu satırı ekleyin
using Project4.Entities;
using Project4.Entities.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Project4.Business
{
    public interface IProductService
    {
        // Değişti: Artık işlemin sonucunu ve veriyi birlikte döndürüyor
        IDataResult<List<Product>> GetAll();

        // Değişti: Tek bir ürün ve işlem sonucu döndürüyor
        IDataResult<Product> GetById(int id);

        // Değişti: Sadece işlem sonucu (başarılı/başarısız ve mesaj) döndürüyor
        IResult Add(Product product);
        IResult Update(Product product);
        IResult DeleteById(int id);
        
        // Şimdilik async metotları aynı bırakabiliriz, onları sonra güncelleriz.
        Task<List<Product>> GetAllAsync();
        
        // YENİ METODUMUZ: Detay listesini ve işlem sonucunu döndürüyor
        IDataResult<List<ProductDetailDto>> GetProductDetails();
    }
}