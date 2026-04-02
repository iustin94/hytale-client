using HytaleAdmin.Core;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ImGui;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Engine;
using Stride.Games;

using var game = new Game();
AppManager? appManager = null;
game.Run(null, Start, Update);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    RegisterShaderInDatabase(game, "ImGuiShader.sdsl");
    new ImGuiSystem(game.Services, game.GraphicsDeviceManager);

    var window = game.Window;
    window.AllowUserResizing = true;
    window.IsBorderLess = false;
    window.SetSize(new Stride.Core.Mathematics.Int2(1280, 720));

    var sceneManager = new SceneManager(rootScene, game.Services);
    appManager = new AppManager(sceneManager, game, rootScene);
    appManager.GoToEditor();
}

void Update(Scene rootScene, GameTime time)
{
    appManager?.Update(time);
}

static void RegisterShaderInDatabase(IGame game, string shaderFileName)
{
    var shaderPath = Path.Combine(AppContext.BaseDirectory, "Effects", shaderFileName);
    if (!File.Exists(shaderPath)) return;

    var shaderName = Path.GetFileNameWithoutExtension(shaderFileName);
    var dbUrl = $"shaders/{shaderName}.sdsl";

    var dbProviderService = game.Services.GetService<IDatabaseFileProviderService>();
    var dbProvider = dbProviderService?.FileProvider;
    if (dbProvider == null) return;

    var shaderBytes = File.ReadAllBytes(shaderPath);
    ObjectId objectId;
    using (var stream = new MemoryStream(shaderBytes))
    {
        objectId = dbProvider.ObjectDatabase.Write(stream);
    }
    dbProvider.ContentIndexMap[dbUrl] = objectId;
}
