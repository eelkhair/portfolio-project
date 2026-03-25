using CompanyApi.Application;
using Mapster;

namespace CompanyApi.Tests.Helpers;

public static class MapsterSetup
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        var config = TypeAdapterConfig.GlobalSettings;
        new Mappers().Register(config);
        _initialized = true;
    }
}
