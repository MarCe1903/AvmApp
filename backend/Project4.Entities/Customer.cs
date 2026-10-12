namespace Project4.Entities
{
    public class Customer:IEntity, Core.Entities.IEntity
    {
        public string CustomerId { get; set; } // burda da string kullanmis 
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        
    }
}