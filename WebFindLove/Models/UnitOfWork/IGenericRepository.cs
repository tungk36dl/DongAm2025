using System.Linq.Expressions;

namespace WebFindLove.Models.UnitOfWork
{
    public interface IGenericRepository<TEntity, TKey> where TEntity : class
    {
        Task<TEntity?> FindByIdAsync(
            TKey id,
            params Expression<Func<TEntity, object>>[] includeProperties);

        Task<TEntity?> FindSingleAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            bool asTracking = false,
            params Expression<Func<TEntity, object>>[] includeProperties);

        IQueryable<TEntity> FindAll(
            Expression<Func<TEntity, bool>>? predicate = null,
            params Expression<Func<TEntity, object>>[] includeProperties);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);

        Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);

        Task<List<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            params Expression<Func<TEntity, object>>[] includeProperties);

        void Add(TEntity entity);
        void AddRange(IEnumerable<TEntity> entities);

        void Update(TEntity entity);

        void Remove(TEntity entity);
        void RemoveMultiple(IEnumerable<TEntity> entities);
    }
}
