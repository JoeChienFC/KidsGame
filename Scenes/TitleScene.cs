using System.Numerics;
using Raylib_cs;

namespace KidsGame.Scenes;

public sealed class TitleScene : IScene
{
    private readonly Game _game;
    private float _time;

    public TitleScene(Game game)
    {
        _game = game;
        _game.Audio.PlayMusic("bgm_main");
    }

    public void Update(float dt)
    {
        _time += dt;
        if (Raylib.GetKeyPressed() != 0 || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            _game.Audio.Play("sfx_button");
            _game.ChangeScene(new GameScene(_game));
        }
    }

    public void Draw()
    {
        Raylib.ClearBackground(new Color(164, 222, 255, 255));
        DrawMeadow();

        var font = _game.Assets.GetFont();
        DrawCentered(font, "公主獨角獸與 Poli 救援隊", 116, 44, new Color(95, 58, 128, 255));
        DrawCentered(font, "方向鍵移動  空白鍵幫忙救援", 188, 28, new Color(62, 91, 128, 255));

        var bob = MathF.Sin(_time * 3f) * 8f;
        DrawTitleCharacters(new Vector2(500, 392 + bob), new Vector2(760, 410 - bob * 0.6f));

        var alpha = (byte)(130 + 90 * ((MathF.Sin(_time * 4f) + 1f) * 0.5f));
        DrawCentered(font, "按任意鍵開始", 610, 30, new Color(80, 75, 118, (int)alpha));
    }

    private static void DrawMeadow()
    {
        Raylib.DrawRectangle(0, 520, Game.ScreenWidth, 220, new Color(119, 202, 126, 255));
        for (var i = 0; i < 18; i++)
        {
            var x = 40 + i * 76;
            var y = 545 + (i % 4) * 30;
            Raylib.DrawCircle(x, y, 18, new Color(255, 232, 91, 255));
            Raylib.DrawCircle(x - 7, y - 2, 7, Color.White);
            Raylib.DrawCircle(x + 7, y - 2, 7, Color.White);
        }
    }

    private void DrawCentered(Font font, string text, float y, float size, Color color)
    {
        var measured = Raylib.MeasureTextEx(font, text, size, 1);
        Raylib.DrawTextEx(font, text, new Vector2((Game.ScreenWidth - measured.X) / 2f, y), size, 1, color);
    }

    private void DrawTitleCharacters(Vector2 unicornPosition, Vector2 poliPosition)
    {
        var unicorn = _game.Assets.FindTexture("unicorn");
        if (unicorn.HasValue)
        {
            DrawSheetFrame(unicorn.Value, 1, unicornPosition, 160);
            var princess = _game.Assets.FindTexture("princess_overlay");
            if (princess.HasValue)
            {
                DrawSheetFrame(princess.Value, 1, unicornPosition + new Vector2(0, -64), 86);
            }
        }
        else
        {
            DrawTitleUnicorn(unicornPosition);
        }

        var poli = _game.Assets.FindTexture("poli");
        if (poli.HasValue)
        {
            DrawSheetFrame(poli.Value, 0, poliPosition, 126);
        }
        else
        {
            DrawTitlePoli(poliPosition);
        }
    }

    private static void DrawSheetFrame(Texture2D texture, int frame, Vector2 position, float size)
    {
        var frameWidth = texture.Width / 4;
        var source = new Rectangle(frame * frameWidth, 0, frameWidth, texture.Height);
        var dest = new Rectangle(position.X, position.Y, size, size);
        Raylib.DrawTexturePro(texture, source, dest, new Vector2(size / 2f), 0, Color.White);
    }

    private static void DrawTitleUnicorn(Vector2 position)
    {
        Raylib.DrawEllipse((int)position.X, (int)position.Y, 72, 42, new Color(245, 245, 255, 255));
        Raylib.DrawCircleV(position + new Vector2(58, -34), 34, new Color(255, 248, 254, 255));
        Raylib.DrawTriangle(position + new Vector2(72, -78), position + new Vector2(55, -44), position + new Vector2(86, -45), new Color(255, 222, 82, 255));
        Raylib.DrawCircleV(position + new Vector2(-58, -38), 18, new Color(255, 117, 186, 255));
        Raylib.DrawCircleV(position + new Vector2(-76, -18), 18, new Color(255, 201, 83, 255));
        Raylib.DrawCircleV(position + new Vector2(-58, 2), 18, new Color(94, 198, 255, 255));
        Raylib.DrawRectangle((int)position.X + 22, (int)position.Y - 96, 70, 18, new Color(255, 153, 211, 255));
        Raylib.DrawTriangle(position + new Vector2(25, -96), position + new Vector2(38, -128), position + new Vector2(51, -96), new Color(255, 226, 82, 255));
        Raylib.DrawTriangle(position + new Vector2(53, -96), position + new Vector2(66, -133), position + new Vector2(79, -96), new Color(255, 226, 82, 255));
    }

    private static void DrawTitlePoli(Vector2 position)
    {
        Raylib.DrawRectangleRounded(new Rectangle(position.X - 58, position.Y - 34, 116, 78), 0.35f, 10, new Color(50, 130, 231, 255));
        Raylib.DrawRectangleRounded(new Rectangle(position.X - 33, position.Y - 26, 66, 40), 0.4f, 10, new Color(173, 226, 255, 255));
        Raylib.DrawCircleV(position + new Vector2(-43, 42), 14, Color.Black);
        Raylib.DrawCircleV(position + new Vector2(43, 42), 14, Color.Black);
        Raylib.DrawCircleV(position + new Vector2(0, -42), 13, Color.Red);
        Raylib.DrawRectangle((int)position.X - 25, (int)position.Y + 47, 50, 12, Color.White);
    }
}
