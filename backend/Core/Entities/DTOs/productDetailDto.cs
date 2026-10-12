namespace Core.Entities.DTOs
{
    public class productDetailDto:IDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public short UnitsInStock { get; set; }
        public decimal UnitPrice { get; set; }
        
    }
}