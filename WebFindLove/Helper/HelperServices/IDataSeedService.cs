using WebFindLove.Models;

namespace WebFindLove.HelperServices
{
    public interface IDataSeedService
    {
        Task SeedDefaultAdminUserAsync();
    }
}
