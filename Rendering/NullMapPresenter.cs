using Hexa.NET.ImGui;
using HytaleAdmin.Models.Api;

namespace HytaleAdmin.Rendering;

public class NullMapPresenter : IPluginMapPresenter
{
    public void Draw(ImDrawListPtr drawList, MapRenderer mapRenderer, PluginEntitySummaryDto[] entities) { }
}
