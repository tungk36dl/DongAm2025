using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace WebFindLove.Hubs
{
    public class UserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Return the NameIdentifier claim as userId
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}

