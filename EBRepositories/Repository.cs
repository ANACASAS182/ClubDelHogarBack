using EBData;
using EBEntities.Common;
using EBRepositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace EBRepositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly EBDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(EBDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<List<T>> GetAllAsyncList()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(long id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        // Soft Delete
        public async Task SoftDeleteAsync(long id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity is ISoftDeletable deletableEntity)
            {
                deletableEntity.Eliminado = true;
                deletableEntity.FechaEliminacion = DateTime.Now;
                _dbSet.Update(entity);
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
