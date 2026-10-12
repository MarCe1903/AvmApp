namespace Project4.Entities
{
    public class Personel:IEntity, Core.Entities.IEntity
    {
        public int Id { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
    }
}