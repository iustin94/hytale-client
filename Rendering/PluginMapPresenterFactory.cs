using HytaleAdmin.Models.Api;

namespace HytaleAdmin.Rendering;

public static class PluginMapPresenterFactory
{
    public static IPluginMapPresenter Create(MapPresenterDto? config)
    {
        if (config == null) return new NullMapPresenter();
        return config.Shape switch
        {
            "circle" => new CircleMapPresenter(config),
            _ => new NullMapPresenter()
        };
    }
}
