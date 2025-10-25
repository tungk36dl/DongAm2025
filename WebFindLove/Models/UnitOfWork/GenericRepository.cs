using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace WebFindLove.Models.UnitOfWork
{
    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
        where TEntity : class
    {
        protected readonly AppDbContext _context;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<TEntity> FindAll(
            Expression<Func<TEntity, bool>>? predicate = null,
            params Expression<Func<TEntity, object>>[] includeProperties)
        {
            IQueryable<TEntity> items = _context.Set<TEntity>().AsNoTracking();

            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                    items = items.Include(includeProperty);
            }

            if (predicate is not null)
                items = items.Where(predicate);

            return items;
        }

        public async Task<TEntity?> FindByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] includeProperties)
        {
            return await FindAll(e => EF.Property<TKey>(e, "Id")!.Equals(id), includeProperties)
                .FirstOrDefaultAsync();
        }

        public async Task<TEntity?> FindSingleAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            bool asTracking = false,
            params Expression<Func<TEntity, object>>[] includeProperties)
        {
            var query = FindAll(predicate, includeProperties);
            if (asTracking)
                query = query.AsTracking();

            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
            => await _context.Set<TEntity>().AnyAsync(predicate);

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
            => await (predicate == null
                ? _context.Set<TEntity>().CountAsync()
                : _context.Set<TEntity>().CountAsync(predicate));

        public void Add(TEntity entity) => _context.Set<TEntity>().Add(entity);

        public void AddRange(IEnumerable<TEntity> entities) => _context.Set<TEntity>().AddRange(entities);

        public void Update(TEntity entity) => _context.Set<TEntity>().Update(entity);

        public void Remove(TEntity entity) => _context.Set<TEntity>().Remove(entity);

        public void RemoveMultiple(IEnumerable<TEntity> entities) => _context.Set<TEntity>().RemoveRange(entities);

        public async Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? predicate = null, params Expression<Func<TEntity, object>>[] includeProperties)
        {
            return await FindAll(predicate, includeProperties).ToListAsync();
        }
    }
}
