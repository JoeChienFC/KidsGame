using KidsGame.Assets;
using KidsGame.Audio;
using KidsGame.Scenes;
using Raylib_cs;

namespace KidsGame;

public sealed class Game
{
    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;

    public AssetManager Assets { get; private set; } = null!;
    public AudioManager Audio { get; private set; } = null!;

    private IScene _currentScene = null!;

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "公主獨角獸與 Poli 救援隊");
        Raylib.InitAudioDevice();
        Raylib.SetTargetFPS(60);

        Assets = new AssetManager();
        Assets.LoadAll();
        Audio = new AudioManager(Assets);

        ChangeScene(new TitleScene(this));

        while (!Raylib.WindowShouldClose())
        {
            var dt = MathF.Min(Raylib.GetFrameTime(), 1f / 30f);
            Audio.Update();
            _currentScene.Update(dt);

            Raylib.BeginDrawing();
            _currentScene.Draw();
            Raylib.EndDrawing();
        }

        Audio.Dispose();
        Assets.Dispose();
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }

    public void ChangeScene(IScene next) => _currentScene = next;
}
