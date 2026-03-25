using JobApi.Application.Mappers;
using Mapster;

namespace JobApi.Tests.Helpers;

public static class MapsterSetup
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        var config = TypeAdapterConfig.GlobalSettings;
        new JobMapper().Register(config);
        _initialized = true;
    }
}
