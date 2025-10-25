
namespace WebFindLove.Models.UnitOfWork
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task SaveChangesAsync();
    }
}
