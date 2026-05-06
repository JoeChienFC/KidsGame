using System.Numerics;
using KidsGame.Assets;
using KidsGame.Audio;
using KidsGame.Effects;
using Raylib_cs;

namespace KidsGame.Entities;

public sealed class RescueEvent
{
    public Vector2 Position { get; }
    public float Radius { get; } = 90f;
    public int RequiredHits { get; }
    public int CurrentHits { get; private set; }
    public bool Completed { get; private set; }
    public string Type { get; }
    public string Title { get; }
    public string CompleteSfx { get; }

    private float _completeTimer;

    public RescueEvent(string type, string title, Vector2 position, int requiredHits, string completeSfx)
    {
        Type = type;
        Title = title;
        Position = position;
        RequiredHits = requiredHits;
        CompleteSfx = completeSfx;
    }

    public void Update(float dt)
    {
        if (Completed)
        {
            _completeTimer += dt;
        }
    }

    public bool JustCompleted { get; private set; }

    public void OnHit(ParticleSystem particles, AudioManager audio)
    {
        if (Completed)
        {
            return;
        }

        CurrentHits++;
        audio.Play("sfx_rescue_hit");
        particles.EmitHearts(Position, 12);
        particles.EmitStars(Position + new Vector2(0, -10), 4);

        if (CurrentHits >= RequiredHits)
        {
            Completed = true;
            JustCompleted = true;
            audio.Play("sfx_rescue_complete");
            audio.Play(CompleteSfx);
            particles.EmitHearts(Position, 50);
            particles.EmitFirework(Position, 30);
            particles.EmitStars(Position, 20);
        }
    }

    public bool ConsumeJustCompleted()
    {
        if (!JustCompleted) return false;
        JustCompleted = false;
        return true;
    }

    public void Draw(AssetManager assets, Font font, bool showProgress)
    {
        switch (Type)
        {
            case "kitten":
                DrawKittenEvent(assets);
                break;
            case "balloon":
                DrawBalloonEvent(assets);
                break;
            default:
                DrawPuppyEvent(assets);
                break;
        }

        if (showProgress && !Completed)
        {
            DrawProgress(font);
        }

        if (Completed)
        {
            var jump = MathF.Sin(_completeTimer * 10f) * 7f;
            Raylib.DrawTextEx(font, "完成!", Position + new Vector2(-32, -74 + jump), 24, 1, new Color(230, 80, 150, 255));
        }
    }

    private void DrawKittenEvent(AssetManager assets)
    {
        DrawAnimal(assets, "kitten", Position + new Vector2(0, Completed ? -8 : 8), new Color(255, 174, 66, 255));
        if (!Completed)
        {
            var log = assets.GetTexture("log");
            Raylib.DrawTexturePro(log, new Rectangle(0, 0, log.Width, log.Height), new Rectangle(Position.X, Position.Y + 22, 110, 34), new Vector2(55, 17), -8, Color.White);
        }
    }

    private void DrawBalloonEvent(AssetManager assets)
    {
        var tree = assets.GetTexture("tree");
        Raylib.DrawTexturePro(tree, new Rectangle(0, 0, tree.Width, tree.Height), new Rectangle(Position.X, Position.Y + 18, 110, 120), new Vector2(55, 92), 0, Color.White);
        if (!Completed)
        {
            Raylib.DrawCircleV(Position + new Vector2(34, -62), 17, new Color(255, 92, 123, 255));
            Raylib.DrawCircleV(Position + new Vector2(56, -50), 17, new Color(91, 196, 255, 255));
            Raylib.DrawCircleV(Position + new Vector2(43, -30), 17, new Color(255, 224, 83, 255));
            Raylib.DrawLineV(Position + new Vector2(44, -14), Position + new Vector2(22, 2), Color.DarkBrown);
        }
    }

    private void DrawPuppyEvent(AssetManager assets)
    {
        var puddle = assets.GetTexture("puddle");
        if (!Completed)
        {
            Raylib.DrawTexturePro(puddle, new Rectangle(0, 0, puddle.Width, puddle.Height), new Rectangle(Position.X - 8, Position.Y + 28, 126, 62), new Vector2(63, 31), 0, Color.White);
        }
        else
        {
            DrawRainbowBridge(Position + new Vector2(-70, 25));
        }

        DrawAnimal(assets, "puppy", Position + new Vector2(0, Completed ? -9 : -18), new Color(174, 114, 62, 255));
    }

    private void DrawAnimal(AssetManager assets, string name, Vector2 position, Color fallback)
    {
        var texture = assets.FindTexture(name);
        if (texture.HasValue)
        {
            Raylib.DrawTexturePro(texture.Value, new Rectangle(0, 0, texture.Value.Width, texture.Value.Height), new Rectangle(position.X, position.Y, 46, 46), new Vector2(23, 23), 0, Color.White);
            return;
        }

        Raylib.DrawCircleV(position, 20, fallback);
        Raylib.DrawCircleV(position + new Vector2(-12, -14), 7, fallback);
        Raylib.DrawCircleV(position + new Vector2(12, -14), 7, fallback);
        Raylib.DrawCircleV(position + new Vector2(-6, -3), 3, Color.Black);
        Raylib.DrawCircleV(position + new Vector2(6, -3), 3, Color.Black);
    }

    private static void DrawRainbowBridge(Vector2 origin)
    {
        var colors = new[]
        {
            Color.Red,
            Color.Orange,
            Color.Yellow,
            Color.Green,
            Color.SkyBlue,
            Color.Purple
        };

        for (var i = 0; i < colors.Length; i++)
        {
            Raylib.DrawLineEx(origin + new Vector2(0, i * 5), origin + new Vector2(140, i * 5), 5, colors[i]);
        }
    }

    private void DrawProgress(Font font)
    {
        var ratio = Math.Clamp((float)CurrentHits / RequiredHits, 0f, 1f);
        var x = Position.X - 52;
        var y = Position.Y - 84;
        Raylib.DrawRectangleRounded(new Rectangle(x, y, 104, 18), 0.55f, 8, new Color(255, 255, 255, 210));
        Raylib.DrawRectangleRounded(new Rectangle(x + 3, y + 3, 98 * ratio, 12), 0.55f, 8, new Color(255, 120, 183, 255));
        Raylib.DrawRectangleRoundedLines(new Rectangle(x, y, 104, 18), 0.55f, 8, Color.DarkPurple);
        Raylib.DrawTextEx(font, $"{CurrentHits}/{RequiredHits}", new Vector2(x + 35, y - 24), 20, 1, Color.DarkPurple);
    }
}
