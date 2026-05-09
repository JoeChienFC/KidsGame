using System.Numerics;
using KidsGame.Effects;
using KidsGame.Util;
using Raylib_cs;

namespace KidsGame.Scenes;

public sealed class TitleScene : IScene
{
    private readonly Game _game;
    private readonly ParticleSystem _particles = new();
    private int _selectedMode;
    private float _time;
    private float _ambientSparkleTimer;
    private int _previousSelectedMode;
    private float _selectionPulse;

    public TitleScene(Game game)
    {
        _game = game;
        _game.Audio.PlayMusic("bgm_main");
    }

    public void Update(float dt)
    {
        _time += dt;

        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            _selectedMode = 1 - _selectedMode;
            _game.Audio.Play("sfx_button");
        }

        if (Raylib.IsKeyPressed(KeyboardKey.One) || Raylib.IsKeyPressed(KeyboardKey.Kp1))
        {
            StartMode(0);
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Two) || Raylib.IsKeyPressed(KeyboardKey.Kp2))
        {
            StartMode(1);
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            StartMode(_selectedMode);
        }

        if (_selectedMode != _previousSelectedMode)
        {
            _previousSelectedMode = _selectedMode;
            _selectionPulse = 1f;
        }
        _selectionPulse = MathF.Max(0f, _selectionPulse - dt * 3f);

        // Ambient floating sparkles across the whole title
        _ambientSparkleTimer -= dt;
        if (_ambientSparkleTimer <= 0f)
        {
            _ambientSparkleTimer = 0.12f;
            var x = (float)Random.Shared.NextDouble() * Game.ScreenWidth;
            var y = (float)Random.Shared.NextDouble() * (Game.ScreenHeight - 100);
            _particles.EmitSparkle(new Vector2(x, y), 1);
        }
        _particles.Update(dt);
    }

    public void Draw()
    {
        DrawBackdrop();
        _particles.Draw(_game.Assets);

        var font = _game.Assets.GetFont();
        var titleBob = MathF.Sin(_time * 1.6f) * 4f;
        DrawCentered(font, "公主與波麗救援隊", 68 + titleBob, 48, new Color(255, 255, 255, 120), new Vector2(2, 3));
        DrawCentered(font, "公主與波麗救援隊", 66 + titleBob, 48, new Color(95, 58, 128, 255));
        DrawCentered(font, "方向鍵選擇  Enter開始  也可以按1或2", 126, 24, new Color(62, 91, 128, 230));

        DrawModeCard(
            index: 0,
            rect: new Rectangle(92, 194, 500, 360),
            title: "1 公主拯救遊戲",
            subtitle: "三關公主魔法，最後裝扮獨角獸",
            accent: new Color(255, 117, 186, 255));

        DrawModeCard(
            index: 1,
            rect: new Rectangle(688, 194, 500, 360),
            title: "2 波麗拯救恐龍",
            subtitle: "五關恐龍救援，一起拆籠子",
            accent: new Color(77, 167, 224, 255));

        var pulse = (MathF.Sin(_time * 4f) + 1f) * 0.5f;
        DrawCentered(font, "空白鍵連點也會有反應", 626, 24, new Color(80, 75, 118, (int)(145 + pulse * 80)));

        DrawVignette();
    }

    private void StartMode(int mode)
    {
        _game.Audio.Play("sfx_button");
        _game.ChangeScene(mode == 0 ? new PrincessRescueScene(_game) : new DinoRescueScene(_game));
    }

    private void DrawModeCard(int index, Rectangle rect, string title, string subtitle, Color accent)
    {
        var selected = _selectedMode == index;
        var bobAmount = selected ? MathF.Sin(_time * 5f) * 4f - 5f : 0f;
        // Pulse scale grows briefly when this card just got selected
        var freshPulse = selected ? _selectionPulse : 0f;
        var scaleBoost = 1f + freshPulse * 0.04f;
        var scaledRect = new Rectangle(
            rect.X - rect.Width * (scaleBoost - 1f) * 0.5f,
            rect.Y + bobAmount - rect.Height * (scaleBoost - 1f) * 0.5f,
            rect.Width * scaleBoost,
            rect.Height * scaleBoost);

        Raylib.DrawRectangleRounded(new Rectangle(scaledRect.X + 8, scaledRect.Y + 12, scaledRect.Width, scaledRect.Height), 0.08f, 14, new Color(66, 62, 88, 45));
        Raylib.DrawRectangleRounded(scaledRect, 0.08f, 14, selected ? new Color(255, 255, 255, 245) : new Color(255, 255, 255, 218));
        Raylib.DrawRectangleRounded(new Rectangle(scaledRect.X, scaledRect.Y, scaledRect.Width, 12), 0.08f, 14, new Color(accent.R, accent.G, accent.B, selected ? 230 : 150));
        Raylib.DrawRectangleRoundedLines(scaledRect, 0.08f, 14, selected ? accent : new Color(180, 190, 205, 255));

        // Glow halo behind selected card
        if (selected)
        {
            var glowPulse = (MathF.Sin(_time * 3f) + 1f) * 0.5f;
            var glowAlpha = (int)(40 + glowPulse * 30);
            Raylib.DrawRectangleRounded(
                new Rectangle(scaledRect.X - 12, scaledRect.Y - 12, scaledRect.Width + 24, scaledRect.Height + 24),
                0.08f, 14,
                new Color(accent.R, accent.G, accent.B, glowAlpha));
        }

        var font = _game.Assets.GetFont();
        Raylib.DrawCircle((int)(scaledRect.X + 44), (int)(scaledRect.Y + 48), 18, accent);
        Raylib.DrawTextEx(font, title, new Vector2(scaledRect.X + 76, scaledRect.Y + 28), 31, 1, new Color(65, 61, 93, 255));
        Raylib.DrawTextEx(font, subtitle, new Vector2(scaledRect.X + 42, scaledRect.Y + 82), 20, 1, new Color(86, 95, 116, 230));

        if (index == 0)
        {
            DrawModeOnePreview(rect, bobAmount);
        }
        else
        {
            DrawModeTwoPreview(rect, bobAmount);
        }
    }

    private void DrawModeOnePreview(Rectangle rect, float lift)
    {
        var bob = MathF.Sin(_time * 3f) * 5f;
        Raylib.DrawCircleV(new Vector2(rect.X + 258, rect.Y + 252 + lift), 160, new Color(255, 235, 180, 42));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.CastleRoyal, new Rectangle(rect.X + 246, rect.Y + 238 + lift, 190, 190), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.PrinceWindow, new Rectangle(rect.X + 242, rect.Y + 168 + lift + bob, 102, 112), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.Princess, new Rectangle(rect.X + 94, rect.Y + 244 + lift + bob, 118, 136), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.PrincessMermaid, new Rectangle(rect.X + 54, rect.Y + 342 + lift - bob * 0.4f, 72, 82), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.PrincessIce, new Rectangle(rect.X + 136, rect.Y + 350 + lift + bob * 0.35f, 72, 82), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.PrincessRapunzel, new Rectangle(rect.X + 222, rect.Y + 356 + lift - bob * 0.25f, 72, 82), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.Cottage, new Rectangle(rect.X + 392, rect.Y + 252 + lift, 182, 158), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.Apple, new Rectangle(rect.X + 164, rect.Y + 322 + lift, 58, 58), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.Carrot, new Rectangle(rect.X + 224, rect.Y + 324 + lift, 58, 58), Color.White);
        DrawPreviewTexture("magic_orb_pink", new Rectangle(rect.X + 374, rect.Y + 382 + lift + bob * 0.2f, 54, 60));
        DrawPreviewTexture("unicorn_crown", new Rectangle(rect.X + 446, rect.Y + 380 + lift - bob * 0.2f, 70, 48));
    }

    private void DrawModeTwoPreview(Rectangle rect, float lift)
    {
        var bob = MathF.Sin(_time * 3f) * 5f;
        Raylib.DrawCircleV(new Vector2(rect.X + 276, rect.Y + 252 + lift), 160, new Color(170, 225, 255, 55));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.RobotPoli, new Rectangle(rect.X + 92, rect.Y + 250 + lift + bob, 116, 126), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.RobotHelly, new Rectangle(rect.X + 208, rect.Y + 206 + lift - bob, 112, 126), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.RobotAmber, new Rectangle(rect.X + 190, rect.Y + 312 + lift + bob, 112, 126), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.RobotRoy, new Rectangle(rect.X + 310, rect.Y + 292 + lift, 124, 136), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.DinoCaged, new Rectangle(rect.X + 398, rect.Y + 230 + lift, 130, 142), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.StegoDinoCaged, new Rectangle(rect.X + 436, rect.Y + 306 + lift - bob * 0.35f, 126, 142), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.BrachioDinoCaged, new Rectangle(rect.X + 380, rect.Y + 334 + lift + bob * 0.2f, 118, 128), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.PteroDinoCaged, new Rectangle(rect.X + 500, rect.Y + 350 + lift - bob * 0.2f, 106, 116), Color.White);
    }

    private void DrawPreviewTexture(string textureName, Rectangle box)
    {
        var texture = _game.Assets.FindTexture(textureName);
        if (!texture.HasValue) return;

        var scale = MathF.Min(box.Width / texture.Value.Width, box.Height / texture.Value.Height);
        var width = texture.Value.Width * scale;
        var height = texture.Value.Height * scale;
        Raylib.DrawTexturePro(
            texture.Value,
            new Rectangle(0, 0, texture.Value.Width, texture.Value.Height),
            new Rectangle(box.X, box.Y, width, height),
            new Vector2(width / 2f, height / 2f),
            0,
            Color.White);
    }

    private void DrawBackdrop()
    {
        for (var y = 0; y < 520; y += 8)
        {
            var t = y / 520f;
            var color = new Color(
                (int)(150 + t * 42),
                (int)(220 + t * 12),
                255,
                255);
            Raylib.DrawRectangle(0, y, Game.ScreenWidth, 8, color);
        }

        // Sun with gentle pulsing glow
        var sunPulse = (MathF.Sin(_time * 2.2f) + 1f) * 0.5f;
        Raylib.DrawCircle(1040, 112, 64f + sunPulse * 6f, new Color(255, 237, 128, 90));
        Raylib.DrawCircle(1040, 112, 54, new Color(255, 237, 128, 180));
        Raylib.DrawCircle(1040, 112, 36, new Color(255, 250, 190, 210));

        // Drifting clouds — slow horizontal motion based on time
        DrawDriftingCloud(180, 90, 0.55f, 0f);
        DrawDriftingCloud(420, 60, 0.7f, 1.7f);
        DrawDriftingCloud(740, 140, 0.5f, 3.4f);
        DrawDriftingCloud(900, 80, 0.6f, 0.8f);

        Raylib.DrawRectangle(0, 520, Game.ScreenWidth, 220, new Color(119, 202, 126, 255));
        Raylib.DrawRectangle(0, 510, Game.ScreenWidth, 22, new Color(133, 214, 135, 255));
        for (var i = 0; i < 18; i++)
        {
            var x = 40 + i * 76;
            var y = 545 + (i % 4) * 30;
            var bob = (int)(MathF.Sin(_time * 2f + i * 0.6f) * 1.2f);
            Raylib.DrawCircle(x, y + bob, 18, new Color(255, 232, 91, 255));
            Raylib.DrawCircle(x - 7, y - 2 + bob, 7, Color.White);
            Raylib.DrawCircle(x + 7, y - 2 + bob, 7, Color.White);
        }
    }

    private void DrawDriftingCloud(float baseX, float y, float scale, float phase)
    {
        var x = baseX + ((_time * 14f + phase * 100f) % (Game.ScreenWidth + 200)) - 100;
        var puff = new Color(255, 255, 255, 220);
        var s = scale;
        Raylib.DrawCircleV(new Vector2(x - 30 * s, y + 4 * s), 18 * s, new Color(220, 235, 250, 165));
        Raylib.DrawCircleV(new Vector2(x + 28 * s, y + 6 * s), 16 * s, new Color(220, 235, 250, 165));
        Raylib.DrawCircleV(new Vector2(x - 24 * s, y - 6 * s), 22 * s, puff);
        Raylib.DrawCircleV(new Vector2(x, y - 12 * s), 26 * s, puff);
        Raylib.DrawCircleV(new Vector2(x + 24 * s, y - 4 * s), 22 * s, puff);
        Raylib.DrawCircleV(new Vector2(x - 4 * s, y), 24 * s, puff);
    }

    private static void DrawVignette()
    {
        const int edge = 70;
        Raylib.DrawRectangleGradientV(0, 0, Game.ScreenWidth, edge, new Color(0, 0, 0, 50), new Color(0, 0, 0, 0));
        Raylib.DrawRectangleGradientV(0, Game.ScreenHeight - edge, Game.ScreenWidth, edge, new Color(0, 0, 0, 0), new Color(0, 0, 0, 60));
        Raylib.DrawRectangleGradientH(0, 0, edge, Game.ScreenHeight, new Color(0, 0, 0, 50), new Color(0, 0, 0, 0));
        Raylib.DrawRectangleGradientH(Game.ScreenWidth - edge, 0, edge, Game.ScreenHeight, new Color(0, 0, 0, 0), new Color(0, 0, 0, 50));
    }

    private static void DrawCentered(Font font, string text, float y, float size, Color color)
    {
        DrawCentered(font, text, y, size, color, Vector2.Zero);
    }

    private static void DrawCentered(Font font, string text, float y, float size, Color color, Vector2 offset)
    {
        var measured = Raylib.MeasureTextEx(font, text, size, 1);
        Raylib.DrawTextEx(font, text, new Vector2((Game.ScreenWidth - measured.X) / 2f, y) + offset, size, 1, color);
    }
}
