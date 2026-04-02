using Stride.Core;
using Stride.Engine;
using Stride.Games;
using HytaleAdmin.Scenes;

namespace HytaleAdmin.Core;

public class SceneManager
{
    private readonly Scene _rootScene;
    private readonly IServiceRegistry _services;
    private IGameScene? _currentScene;

    public SceneManager(Scene rootScene, IServiceRegistry services)
    {
        _rootScene = rootScene;
        _services = services;
    }

    public void LoadScene(IGameScene newScene)
    {
        _currentScene?.Unload(_rootScene);
        _currentScene = newScene;
        _currentScene.Load(_rootScene, _services);
    }

    public void Update(GameTime time)
    {
        _currentScene?.Update(time);
    }
}
