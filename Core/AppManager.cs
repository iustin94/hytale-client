using HytaleAdmin.Models.Domain;
using HytaleAdmin.Scenes;
using HytaleAdmin.Services;
using Stride.Engine;
using Stride.Games;

namespace HytaleAdmin.Core;

public class AppManager
{
    private readonly SceneManager _sceneManager;
    private readonly ServiceContainer _services;

    public AppManager(SceneManager sceneManager, Game game, Scene rootScene)
    {
        _sceneManager = sceneManager;

        var config = new EditorConfig();
        var apiClient = new HytaleApiClient { BaseUrl = config.ApiBaseUrl };

        _services = new ServiceContainer
        {
            ApiClient = apiClient,
            MapData = new MapDataService(),
            EntityData = new EntityDataService(),
            Selection = new SelectionService(),
            Config = config,
            Game = game
        };
    }

    public void GoToEditor()
    {
        _sceneManager.LoadScene(new EditorScene(_services));
    }

    // Unused fade parameter removed from SceneManager

    public void Update(GameTime time)
    {
        _sceneManager.Update(time);
    }
}
