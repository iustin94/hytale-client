using Stride.Core;
using Stride.Engine;
using Stride.Games;

namespace HytaleAdmin.Scenes;

public interface IGameScene
{
    void Load(Scene rootScene, IServiceRegistry services);
    void Update(GameTime time);
    void Unload(Scene rootScene);
}
