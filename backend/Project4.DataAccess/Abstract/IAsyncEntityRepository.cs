using Project4.Entities;

namespace Project4.DataAccess
{
    public interface IAsyncEntityRepository<T> where T : class,IEntity,new()
    {
        Task<List<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
    }
}