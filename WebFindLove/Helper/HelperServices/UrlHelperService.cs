namespace WebFindLove.Helper.HelperServices
{
    public class UrlHelperService : IUrlHelperService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UrlHelperService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetFullUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return string.Empty;
            }
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null) return relativePath; // phòng null

            // Check if path already starts with / or http/https
            if (relativePath.StartsWith("/") || relativePath.StartsWith("http://") || relativePath.StartsWith("https://"))
            {
                return relativePath;
            }

            // Assume it's a filename in uploads/avatars
            var fullPath = $"/uploads/avatars/{relativePath}";
            return $"{request.Scheme}://{request.Host}{fullPath}";
        }
    }
}
