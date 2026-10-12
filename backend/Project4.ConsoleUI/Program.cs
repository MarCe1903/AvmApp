using Project4.Business;
using Project4.DataAccess;
using Project4.Entities;

namespace Project4.ConsoleUI
{
    class Program
    {
        static void Main(string[] args)
        {
            ProductManager productManager = new ProductManager(new EfProductDal());
            foreach (var product in productManager.GetAllAsync().Result)
            {
                Console.WriteLine(product.ProductName); 
            }
            //productManager.Delete(new Product {ProductId = 78,ProductName="Masa 2 ",CategoryId =1, QuantityPerUnit = "4 ayakli masa", UnitPrice = 1000, UnitsInStock = 10 }); //ProductId girmemize gerek yok zaten database otomatik ekleyebiliyor database kisminda auto increment yaziyor
            //Console.WriteLine(productManager.GetById(1).ProductName);
            // foreach (var product in productManager.GetAll())
            // {
            //     Console.WriteLine(product.ProductName);
            // }

            // try
            // { 
            //     productManager.Add(new Product { ProductId = 10, ProductName = "Laptop", QuantityPerUnit = "1", UnitPrice = 1000, UnitsInStock = 10 });
            // }
            // catch (DuplicateProductException exception)
            // { 
            //     Console.WriteLine(exception.Message);
            // }
            // PersonelManager personelManager = new PersonelManager(new EfPersonelDal());
            // foreach (var personel in personelManager.GetAll())
            // {
            //     Console.WriteLine("{0} / {1} / {2}",personel.Id,personel.Name,personel.Surname);
            // }


            
        }
    }
}

