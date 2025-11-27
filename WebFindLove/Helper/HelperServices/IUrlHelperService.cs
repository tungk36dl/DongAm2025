namespace WebFindLove.Helper.HelperServices
{
    public interface IUrlHelperService
    {
        string GetFullUrl(string? relativePath);
        string GetUrl(string? path);
    }
}
