using System.Numerics;
using KidsGame.Assets;
using KidsGame.Effects;
using KidsGame.Util;
using Raylib_cs;

namespace KidsGame.Scenes;

public sealed class CelebrationScene : IScene
{
    private readonly Game _game;
    private readonly ParticleSystem _particles = new();
    private readonly int _collectedCrystals;
    private readonly int _round;
    private readonly Theme _nextTheme;
    private float _time;
    private float _fireworkTimer;
    private float _confettiTimer;
    private readonly Random _rand = new();

    public CelebrationScene(Game game, int collectedCrystals, int round)
    {
        _game = game;
        _collectedCrystals = collectedCrystals;
        _round = round;
        _nextTheme = Theme.ForRound(round + 1);
        _game.Audio.PlayMusic("bgm_celebration");
        _game.Audio.Play("sfx_rescue_complete");
    }

    public void Update(float dt)
    {
        _time += dt;
        _fireworkTimer -= dt;
        _confettiTimer -= dt;

        if (_fireworkTimer <= 0f)
        {
            _fireworkTimer = 0.32f;
            var x = _rand.Next(140, Game.ScreenWidth - 140);
            var y = _rand.Next(80, 280);
            _particles.EmitFirework(new Vector2(x, y), 20);
            _particles.EmitStars(new Vector2(x, y), 8);
        }

        if (_confettiTimer <= 0f)
        {
            _confettiTimer = 0.05f;
            var x = _rand.NextSingle() * Game.ScreenWidth;
            _particles.EmitHearts(new Vector2(x, -10), 1);
        }

        _particles.Update(dt);

        if (_time >= 5f)
        {
            _game.ChangeScene(new GameScene(_game, _round + 1));
        }
        else if (Raylib.GetKeyPressed() != 0)
        {
            // skip celebration early
            _game.ChangeScene(new GameScene(_game, _round + 1));
        }
    }

    public void Draw()
    {
        var bgT = _time * 0.4f;
        var rTop = (byte)(140 + 80 * MathF.Sin(bgT));
        var gTop = (byte)(180 + 50 * MathF.Sin(bgT + 1.7f));
        var bTop = (byte)(220 + 30 * MathF.Sin(bgT + 3.1f));
        Raylib.ClearBackground(new Color(rTop, gTop, bTop, (byte)255));

        for (var i = 0; i < 9; i++)
        {
            var bob = MathF.Sin(_time * 1.6f + i) * 8f;
            Raylib.DrawCircle(80 + i * 160, (int)(110 + (i % 2) * 35 + bob), 42, new Color(255, 255, 255, 110));
        }

        var ribbonColors = new[]
        {
            new Color(255, 102, 102, 255),
            new Color(255, 175, 102, 255),
            new Color(255, 230, 102, 255),
            new Color(140, 230, 140, 255),
            new Color(120, 195, 255, 255),
            new Color(195, 140, 255, 255),
        };
        for (var i = 0; i < ribbonColors.Length; i++)
        {
            Raylib.DrawRectangle(0, i * 8, Game.ScreenWidth, 8, ribbonColors[i]);
        }

        _particles.Draw(_game.Assets);

        var font = _game.Assets.GetFont();
        DrawTitleText(font, $"第 {_round} 輪救援完成!", 80, 56, new Color(255, 255, 255, 255));
        DrawCentered(font, $"救了 3 隻小動物，收集了 {_collectedCrystals} 顆水晶", 158, 30, new Color(255, 248, 193, 255));

        DrawDancingAnimal(_game.Assets, "kitten", new Vector2(380, 460), 0f, new Color(255, 177, 73, 255));
        DrawDancingAnimal(_game.Assets, "puppy", new Vector2(640, 480), 1.1f, new Color(176, 117, 67, 255));
        DrawDancingAnimal(_game.Assets, "bunny", new Vector2(900, 460), 2.2f, new Color(245, 245, 245, 255));

        DrawCentered(font, $"下一輪：{_nextTheme.Name}", 612, 28, new Color(80, 50, 100, 240));
        DrawCentered(font, "按任意鍵繼續", 648, 24, new Color(80, 50, 100, 200));
    }

    private void DrawDancingAnimal(AssetManager assets, string name, Vector2 position, float phase, Color fallback)
    {
        var jump = MathF.Abs(MathF.Sin(_time * 5f + phase)) * 38f;
        var sway = MathF.Sin(_time * 3f + phase) * 6f;
        var pos = position + new Vector2(sway, -jump);

        Raylib.DrawEllipse((int)position.X, (int)(position.Y + 60), 50, 10, new Color(0, 0, 0, 80));

        var ringPulse = (MathF.Sin(_time * 4f + phase) + 1f) * 0.5f;
        Raylib.DrawCircleV(pos + new Vector2(0, 30), 60f + ringPulse * 8f, new Color(255, 255, 255, 50));

        var texture = assets.FindTexture(name);
        if (texture.HasValue)
        {
            var tex = texture.Value;
            var size = 110f;
            Raylib.DrawTexturePro(tex, new Rectangle(0, 0, tex.Width, tex.Height), new Rectangle(pos.X, pos.Y, size, size), new Vector2(size / 2f, size / 2f), 0, Color.White);
        }
        else
        {
            Raylib.DrawCircleV(pos, 46, fallback);
            Raylib.DrawCircleV(pos + new Vector2(-26, -32), 16, fallback);
            Raylib.DrawCircleV(pos + new Vector2(26, -32), 16, fallback);
            Raylib.DrawCircleV(pos + new Vector2(-14, -5), 5, Color.Black);
            Raylib.DrawCircleV(pos + new Vector2(14, -5), 5, Color.Black);
            Raylib.DrawLineEx(pos + new Vector2(-14, 18), pos + new Vector2(14, 18), 5, new Color(90, 58, 86, 255));
        }

        var heartY = pos.Y - 70 + MathF.Sin(_time * 3f + phase) * 6f;
        DrawHeart(new Vector2(pos.X, heartY), 8 + ringPulse * 3, new Color(255, 105, 180, 255));
    }

    private static void DrawHeart(Vector2 center, float radius, Color color)
    {
        Raylib.DrawCircleV(center + new Vector2(-radius * 0.5f, -radius * 0.2f), radius * 0.7f, color);
        Raylib.DrawCircleV(center + new Vector2(radius * 0.5f, -radius * 0.2f), radius * 0.7f, color);
        Raylib.DrawTriangle(
            center + new Vector2(-radius, 0),
            center + new Vector2(radius, 0),
            center + new Vector2(0, radius * 1.2f),
            color);
    }

    private static void DrawCentered(Font font, string text, float y, float size, Color color)
    {
        var measured = Raylib.MeasureTextEx(font, text, size, 1);
        Raylib.DrawTextEx(font, text, new Vector2((Game.ScreenWidth - measured.X) / 2f, y), size, 1, color);
    }

    private void DrawTitleText(Font font, string text, float y, float size, Color color)
    {
        var bounce = MathF.Sin(_time * 3f) * 6f;
        var measured = Raylib.MeasureTextEx(font, text, size, 1);
        var pos = new Vector2((Game.ScreenWidth - measured.X) / 2f, y + bounce);
        Raylib.DrawTextEx(font, text, pos + new Vector2(3, 4), size, 1, new Color(80, 30, 80, 150));
        Raylib.DrawTextEx(font, text, pos, size, 1, color);
    }
}
