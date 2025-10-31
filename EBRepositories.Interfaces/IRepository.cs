namespace EBRepositories.Interfaces
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> GetQueryable();
        Task<IEnumerable<T>> GetAllAsync();
        Task<List<T>> GetAllAsyncList();
        Task<T?> GetByIdAsync(long id);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task SaveAsync();
        Task SoftDeleteAsync(long id);
    }
}
