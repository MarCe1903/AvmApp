namespace Project4.Entities
{
    public class OrderStatus:Core.Entities.IEntity
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }
}