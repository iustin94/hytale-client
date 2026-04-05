using Hexa.NET.ImGui;
using HytaleAdmin.Models.Api;

namespace HytaleAdmin.Rendering;

public interface IPluginMapPresenter
{
    void Draw(ImDrawListPtr drawList, MapRenderer mapRenderer, PluginEntitySummaryDto[] entities);
}
