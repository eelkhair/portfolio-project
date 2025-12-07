using System.Web;

namespace ConnectorAPI.Helpers;

public static class ODataRouteBuilder
{
    private static string Build(string path, IDictionary<string, string>? query = null)
    {
        if (query is null || query.Count == 0)
            return path;

        var qs = HttpUtility.ParseQueryString(string.Empty);

        foreach (var kvp in query)
            qs[kvp.Key] = kvp.Value;

        return $"{path}?{qs}";
    }

    public static string CompanyById(Guid id, Action<IDictionary<string, string>>? configureQuery = null)
    {
        var q = new Dictionary<string, string>();
        configureQuery?.Invoke(q);
        return Build($"odata/companies({id})", q);
    }

    public static string UserById(Guid id, Action<IDictionary<string, string>>? configureQuery = null)
    {
        var q = new Dictionary<string, string>();
        configureQuery?.Invoke(q);
        return Build($"odata/users({id})", q);
    }
}