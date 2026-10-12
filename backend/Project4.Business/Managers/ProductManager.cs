// Business/Concrete/ProductManager.cs

using Core.Utilities.Results;
using Project4.Business;
using Project4.DataAccess; // IProductDal için
using Project4.Entities;
using Project4.Entities.DTO;
using Core.DataAccess.EntityFramework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Project4.DataAccess.Abstract;

namespace Project4.Business
{
    public class ProductManager : IProductService
    {
        private readonly IProductDal _productDal;

        public ProductManager(IProductDal productDal)
        {
            _productDal = productDal;
        }

        // Değişti: IResult döndürüyor
        public IResult Add(Product product)
        {
            // BURAYA İŞ KURALLARI EKLENEBİLİR
            // Örneğin: if (product.ProductName.Length < 2) { return new ErrorResult("Ürün ismi en az 2 karakter olmalıdır."); }
            _productDal.Add(product);
            return new SuccessResult("Ürün başarıyla eklendi.");
        }

        // Değişti: IResult döndürüyor
        public IResult DeleteById(int productId)
        {
            var productToDelete= _productDal.Get(p=> p.ProductId == productId);
            if (productToDelete==null)
            {
                return new ErrorResult("Bu ID ile bir urun bulunmadi");
            }
            _productDal.Delete(productToDelete);
            return new SuccessResult("Urun basariyla silindi");

        }

        // Değişti: IDataResult döndürüyor
        public IDataResult<List<Product>> GetAll()
        {
            var productList = _productDal.GetAll();
            return new SuccessDataResult<List<Product>>(productList, "Ürünler listelendi.");
        }
        
        // Şimdilik bunu aynı bırakıyoruz
        public IResult Delete(int productId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Product>> GetAllAsync()
        {
            return _productDal.GetAllAsync();
        }

        // Değişti: IDataResult döndürüyor
        public IDataResult<Product> GetById(int id)
        {
            // DEĞİŞİKLİK BURADA:
            // Generic Get metodunu kullanarak ona spesifik bir kural veriyoruz:
            // "ProductId'si, benim sana verdiğim 'id' parametresine eşit olan Product'ı bul."
            var product = _productDal.Get(p => p.ProductId == id);

            // Eğer o kurala uyan bir ürün bulunamazsa, hata döndür.
            if (product == null)
            {
                return new ErrorDataResult<Product>("Bu ID'de bir ürün bulunamadı.");
            }

            // Ürün bulunduysa, başarıyla döndür.
            return new SuccessDataResult<Product>(product, "Ürün bulundu.");
        }

        // YENİ METODUMUZ: IDataResult döndürüyor
        public IDataResult<List<ProductDetailDto>> GetProductDetails()
        {
            var productDetails = _productDal.GetProductDetails();
            return new SuccessDataResult<List<ProductDetailDto>>(productDetails, "Ürün detayları getirildi.");
        }

        // Değişti: IResult döndürüyor
        public IResult Update(Product product)
        {
            // 1. Güncellenecek ürünü veritabanından ID'si ile bul.
            var entityToUpdate = _productDal.Get(p => p.ProductId == product.ProductId);

            if (entityToUpdate == null)
            {
                return new ErrorResult("Güncellenecek ürün bulunamadı.");
            }

            // 2. Dışarıdan gelen yeni bilgileri, bulduğumuz varlığın üzerine yaz.
            entityToUpdate.ProductName = product.ProductName;
            entityToUpdate.CategoryId = product.CategoryId;
            entityToUpdate.UnitPrice = product.UnitPrice;
            entityToUpdate.UnitsInStock = product.UnitsInStock;
            entityToUpdate.QuantityPerUnit = product.QuantityPerUnit;
        
            // 3. Değişiklikleri kaydet.
            _productDal.Update(entityToUpdate);

            return new SuccessResult("Ürün başarıyla güncellendi.");
        }
    }
}