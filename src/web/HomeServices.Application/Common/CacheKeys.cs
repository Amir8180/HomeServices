namespace HomeServices.Application.Common;

/// <summary>
/// Centralised cache-key scheme. Keeps keys consistent, typed and easy to invalidate
/// by prefix (e.g. invalidate every category entry after a category changes).
/// </summary>
public static class CacheKeys
{
    private const char Separator = ':';

    public static class Categories
    {
        public const string Prefix = "categories";
        public static string All(bool activeOnly) => $"{Prefix}{Separator}all:{activeOnly}";
        public static string ByGroup(int group) => $"{Prefix}{Separator}group:{group}";
        public static string ById(int id) => $"{Prefix}{Separator}id:{id}";
        public static string SubCategories(int parentId) => $"{Prefix}{Separator}subs:{parentId}";
    }

    public static class Services
    {
        public const string Prefix = "services";
        public static string Paged(string hash) => $"{Prefix}{Separator}paged:{hash}";
        public static string ByCategory(int categoryId) => $"{Prefix}{Separator}cat:{categoryId}";
        public static string ById(int id) => $"{Prefix}{Separator}id:{id}";
        public static string BySlug(string slug) => $"{Prefix}{Separator}slug:{slug}";
    }

    public static class Experts
    {
        public const string Prefix = "experts";
        public static string TopRated(int count) => $"{Prefix}{Separator}top:{count}";
        public static string ByCategory(int categoryId) => $"{Prefix}{Separator}cat:{categoryId}";
        public static string ById(int id) => $"{Prefix}{Separator}id:{id}";
        public static string ByUserId(Guid userId) => $"{Prefix}{Separator}user:{userId}";
    }

    public static class SiteSettings
    {
        public const string Prefix = "settings";
        public const string Dictionary = "settings:dictionary";
    }

    public static class Reviews
    {
        public const string Prefix = "reviews";
        public static string ByExpert(Guid expertId) => $"{Prefix}{Separator}expert:{expertId}";
    }
}
