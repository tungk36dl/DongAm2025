namespace WebFindLove.Helper.HelperServices.Mapper
{
    public interface IMapper
    {
        TDestination Map<TSource, TDestination>(TSource source);
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
        //IEnumerable<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> sourceList);

    }
}
