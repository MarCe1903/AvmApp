namespace Project4.Entities
{
    public class Order:IEntity, Core.Entities.IEntity
    {
        public Customer? Customer { get; set; }
        public string CustomerId { get; set; } //burda string kullanmis 
        public int OrderId { get; set; }
        public int OrderStatusId { get; set; }
        public OrderStatus? OrderStatus { get; set; }
    }
}