using System.Numerics;
using KidsGame.Entities;
using KidsGame.Effects;
using KidsGame.Util;
using Raylib_cs;

namespace KidsGame.Scenes;

public sealed class GameScene : IScene
{
    private readonly Game _game;
    private readonly Unicorn _unicorn = new(new Vector2(640, 360));
    private readonly Poli _poli = new(new Vector2(570, 420));
    private readonly ParticleSystem _particles = new();
    private readonly FloatingTextSystem _floatingTexts = new();
    private readonly List<Crystal> _crystals;
    private readonly List<RescueEvent> _events;
    private readonly Theme _theme;
    private readonly int _round;

    private readonly List<Rectangle> _softObstacles =
    [
        new(150, 106, 118, 75),
        new(990, 74, 126, 120),
        new(585, 550, 140, 65),
        new(70, 70, 70, 82),
        new(1125, 88, 78, 86),
    ];

    private readonly (Vector2 Pos, Color Color, float Size)[] _flowers;
    private readonly Butterfly[] _butterflies;
    private readonly (Vector2 Pos, float Size, float Twinkle)[] _stars;

    private float _time;
    private bool _celebrating;
    private float _flashAmount;
    private float _slowMoTimer;
    private float _shakeAmount;

    public GameScene(Game game) : this(game, 1) { }

    public GameScene(Game game, int round)
    {
        _game = game;
        _round = round;
        _theme = Theme.ForRound(round);
        _game.Audio.PlayMusic("bgm_main");

        var rand = new Random(round * 17 + 31);

        _crystals = GenerateCrystals(rand);
        _events =
        [
            new("kitten", "小貓被倒木壓住", new Vector2(200, 150), 5, "sfx_meow"),
            new("balloon", "氣球卡在大樹", new Vector2(1050, 150), 6, "sfx_rescue_complete"),
            new("puppy", "小狗被水坑擋住", new Vector2(640, 580), 7, "sfx_woof"),
        ];

        var flowerColors = new[]
        {
            new Color(255, 119, 168, 255),
            new Color(255, 200, 87, 255),
            new Color(186, 138, 255, 255),
            new Color(255, 255, 255, 255),
            new Color(255, 145, 196, 255),
        };
        var flowers = new List<(Vector2, Color, float)>();
        for (var i = 0; i < 60; i++)
        {
            var x = rand.NextSingle() * Game.ScreenWidth;
            var y = rand.NextSingle() * Game.ScreenHeight;
            var pos = new Vector2(x, y);
            if (_events.Any(e => Vector2.Distance(e.Position, pos) < 90)) continue;
            if (Math.Abs(x - 643) < 80) continue;
            if (Math.Abs(y - 370) < 80) continue;
            flowers.Add((pos, flowerColors[rand.Next(flowerColors.Length)], 4f + rand.NextSingle() * 3.5f));
        }
        _flowers = flowers.ToArray();

        _butterflies = new Butterfly[3];
        for (var i = 0; i < _butterflies.Length; i++)
        {
            _butterflies[i] = new Butterfly(
                center: new Vector2(rand.NextSingle() * 1000 + 140, rand.NextSingle() * 480 + 120),
                radius: 60f + rand.NextSingle() * 50f,
                speed: 0.7f + rand.NextSingle() * 0.5f,
                phase: rand.NextSingle() * MathF.Tau,
                color: flowerColors[rand.Next(flowerColors.Length)]);
        }

        _stars = new (Vector2, float, float)[80];
        for (var i = 0; i < _stars.Length; i++)
        {
            _stars[i] = (
                new Vector2(rand.NextSingle() * Game.ScreenWidth, rand.NextSingle() * 320f),
                1.2f + rand.NextSingle() * 1.8f,
                rand.NextSingle() * MathF.Tau);
        }
    }

    private List<Crystal> GenerateCrystals(Random rand)
    {
        var positions = new List<Vector2>();
        var attempts = 0;
        while (positions.Count < 8 && attempts < 200)
        {
            attempts++;
            var x = 80 + rand.NextSingle() * (Game.ScreenWidth - 160);
            var y = 130 + rand.NextSingle() * (Game.ScreenHeight - 260);
            var pos = new Vector2(x, y);
            // avoid rescue events
            if (Vector2.Distance(pos, new Vector2(200, 150)) < 110) continue;
            if (Vector2.Distance(pos, new Vector2(1050, 150)) < 110) continue;
            if (Vector2.Distance(pos, new Vector2(640, 580)) < 110) continue;
            // avoid spawn (center)
            if (Vector2.Distance(pos, new Vector2(640, 380)) < 90) continue;
            // avoid existing
            if (positions.Any(p => Vector2.Distance(p, pos) < 90)) continue;
            positions.Add(pos);
        }

        var fallbackPositions = new[]
        {
            new Vector2(150, 258),
            new Vector2(330, 450),
            new Vector2(452, 310),
            new Vector2(575, 230),
            new Vector2(710, 452),
            new Vector2(865, 310),
            new Vector2(1055, 265),
            new Vector2(1130, 500),
        };
        foreach (var fallback in fallbackPositions)
        {
            if (positions.Count >= 8) break;
            if (positions.All(p => Vector2.Distance(p, fallback) >= 70f))
            {
                positions.Add(fallback);
            }
        }

        return positions.Select(p => new Crystal(p)).ToList();
    }

    public void Update(float dt)
    {
        _time += dt;
        _flashAmount = MathF.Max(0f, _flashAmount - dt * 2.4f);
        _shakeAmount = MathF.Max(0f, _shakeAmount - dt * 6f);

        var effectiveDt = dt;
        if (_slowMoTimer > 0f)
        {
            _slowMoTimer -= dt;
            effectiveDt = dt * 0.45f;
        }

        _unicorn.Update(effectiveDt, _particles, _softObstacles);
        _poli.Update(effectiveDt, _unicorn, _events, _particles, _game.Audio);

        foreach (var crystal in _crystals)
        {
            crystal.Update(effectiveDt, _particles);
            if (crystal.TryCollect(_unicorn.Position, _particles))
            {
                _game.Audio.Play("sfx_collect");
                _floatingTexts.Spawn("+1", crystal.Position + new Vector2(0, -20), new Color(120, 220, 255, 255), 32f);
            }
        }

        foreach (var rescueEvent in _events)
        {
            rescueEvent.Update(effectiveDt);
            if (rescueEvent.ConsumeJustCompleted())
            {
                _flashAmount = 0.85f;
                _slowMoTimer = 0.4f;
                _shakeAmount = 8f;
                _floatingTexts.Spawn("救到了!", rescueEvent.Position + new Vector2(0, -100), new Color(255, 90, 160, 255), 44f, 1.6f);
            }
        }

        foreach (var b in _butterflies) b.Update(effectiveDt);
        _particles.Update(effectiveDt);
        _floatingTexts.Update(dt);

        if (!_celebrating && _events.All(e => e.Completed))
        {
            _celebrating = true;
            _game.ChangeScene(new CelebrationScene(_game, _crystals.Count(c => c.Collected), _round));
        }
    }

    public void Draw()
    {
        var shake = Vector2.Zero;
        if (_shakeAmount > 0.01f)
        {
            var t = (float)Raylib.GetTime();
            shake = new Vector2(MathF.Sin(t * 53f) * _shakeAmount, MathF.Cos(t * 47f) * _shakeAmount);
        }

        Raylib.ClearBackground(_theme.BackgroundColor);

        if (_theme.ShowStars)
        {
            DrawNightSky();
        }

        Raylib.BeginMode2D(new Camera2D
        {
            Target = -shake,
            Offset = Vector2.Zero,
            Rotation = 0,
            Zoom = 1f,
        });

        DrawMap();
        DrawFlowers();

        foreach (var crystal in _crystals)
        {
            crystal.Draw(_game.Assets);
        }

        var font = _game.Assets.GetFont();
        foreach (var rescueEvent in _events)
        {
            var near = Vector2.Distance(_poli.Position, rescueEvent.Position) <= rescueEvent.Radius + 25f;
            rescueEvent.Draw(_game.Assets, font, near);
        }

        _particles.Draw(_game.Assets);
        _unicorn.Draw(_game.Assets);
        _poli.Draw(_game.Assets);
        foreach (var b in _butterflies) b.Draw();
        _floatingTexts.Draw(font);

        Raylib.EndMode2D();

        // theme overlay
        if (_theme.OverlayTint.A > 0)
        {
            Raylib.DrawRectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight, _theme.OverlayTint);
        }

        if (_flashAmount > 0.01f)
        {
            var alpha = (int)(220 * Math.Clamp(_flashAmount, 0f, 1f));
            Raylib.DrawRectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight, new Color(255, 255, 255, alpha));
        }

        DrawHud(font);
    }

    private void DrawNightSky()
    {
        foreach (var (pos, size, twinklePhase) in _stars)
        {
            var twinkle = (MathF.Sin(_time * 2.5f + twinklePhase) + 1f) * 0.5f;
            var alpha = (int)(140 + 115 * twinkle);
            var c = new Color((int)_theme.StarColor.R, (int)_theme.StarColor.G, (int)_theme.StarColor.B, alpha);
            Raylib.DrawCircleV(pos, size * (0.7f + twinkle * 0.5f), c);
            // sparkle cross
            if (twinkle > 0.7f)
            {
                Raylib.DrawLineEx(pos + new Vector2(-size * 2.2f, 0), pos + new Vector2(size * 2.2f, 0), 1f, c);
                Raylib.DrawLineEx(pos + new Vector2(0, -size * 2.2f), pos + new Vector2(0, size * 2.2f), 1f, c);
            }
        }

        // gentle moon for night
        Raylib.DrawCircleV(new Vector2(1100, 90), 38, new Color(255, 245, 200, 255));
        Raylib.DrawCircleV(new Vector2(1088, 80), 32, new Color(46, 50, 110, 255));
    }

    private void DrawMap()
    {
        var grass = _game.Assets.GetTexture("grass_tile");
        for (var y = 0; y < Game.ScreenHeight; y += 64)
        {
            for (var x = 0; x < Game.ScreenWidth; x += 64)
            {
                Raylib.DrawTexturePro(grass, new Rectangle(0, 0, grass.Width, grass.Height), new Rectangle(x, y, 64, 64), Vector2.Zero, 0, _theme.GrassTint);
            }
        }

        Raylib.DrawRectangleRounded(new Rectangle(585, 0, 116, Game.ScreenHeight), 0.35f, 18, new Color(205, 179, 116, 255));
        Raylib.DrawRectangleRounded(new Rectangle(0, 318, Game.ScreenWidth, 104), 0.35f, 18, new Color(205, 179, 116, 255));

        for (var x = 32; x < Game.ScreenWidth; x += 56)
        {
            Raylib.DrawCircle(x, 326, 3, new Color(245, 220, 168, 200));
            Raylib.DrawCircle(x, 414, 3, new Color(245, 220, 168, 200));
        }
        for (var y = 24; y < Game.ScreenHeight; y += 56)
        {
            Raylib.DrawCircle(593, y, 3, new Color(245, 220, 168, 200));
            Raylib.DrawCircle(693, y, 3, new Color(245, 220, 168, 200));
        }

        DrawTree(new Vector2(92, 116), 0.75f);
        DrawTree(new Vector2(1168, 130), 0.82f);
        DrawTree(new Vector2(935, 90), 0.65f);
        DrawTree(new Vector2(1120, 238), 0.62f);
    }

    private void DrawFlowers()
    {
        foreach (var (pos, color, size) in _flowers)
        {
            var bob = MathF.Sin(_time * 1.4f + pos.X * 0.01f) * 0.6f;
            var p = new Vector2(pos.X, pos.Y + bob);
            var tinted = TintColor(color, _theme.FlowerBoost);
            for (var i = 0; i < 5; i++)
            {
                var ang = i * MathF.Tau / 5f;
                var off = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * size;
                Raylib.DrawCircleV(p + off, size * 0.65f, tinted);
            }
            Raylib.DrawCircleV(p, size * 0.55f, new Color(255, 232, 110, 255));
        }
    }

    private static Color TintColor(Color baseColor, Color tint)
    {
        return new Color(
            (baseColor.R * tint.R) / 255,
            (baseColor.G * tint.G) / 255,
            (baseColor.B * tint.B) / 255,
            (int)baseColor.A);
    }

    private void DrawTree(Vector2 position, float scale)
    {
        var tree = _game.Assets.GetTexture("tree");
        Raylib.DrawTexturePro(tree, new Rectangle(0, 0, tree.Width, tree.Height), new Rectangle(position.X, position.Y, 96 * scale, 110 * scale), new Vector2(48 * scale, 86 * scale), 0, Color.White);
    }

    private void DrawHud(Font font)
    {
        var collected = _crystals.Count(c => c.Collected);
        var rescued = _events.Count(e => e.Completed);
        DrawPill(new Rectangle(24, 22, 235, 46), $"水晶 {collected} / {_crystals.Count}", font, new Color(77, 167, 224, 255));
        DrawPill(new Rectangle(Game.ScreenWidth - 265, 22, 240, 46), $"救援 {rescued} / {_events.Count}", font, new Color(237, 91, 149, 255));

        var roundLabel = $"第 {_round} 輪 · {_theme.Name}";
        var measured = Raylib.MeasureTextEx(font, roundLabel, 24, 1);
        var roundRect = new Rectangle((Game.ScreenWidth - measured.X) / 2f - 24, 22, measured.X + 48, 46);
        Raylib.DrawRectangleRounded(roundRect, 0.55f, 12, new Color(255, 255, 255, 220));
        Raylib.DrawTextEx(font, roundLabel, new Vector2(roundRect.X + 24, roundRect.Y + 12), 24, 1, new Color(95, 58, 128, 255));

        var tip = "姐姐方向鍵移動，弟弟空白鍵幫忙";
        var tipMeasured = Raylib.MeasureTextEx(font, tip, 22, 1);
        Raylib.DrawTextEx(font, tip, new Vector2((Game.ScreenWidth - tipMeasured.X) / 2f, Game.ScreenHeight - 42), 22, 1, new Color(60, 85, 100, 215));
    }

    private static void DrawPill(Rectangle rect, string text, Font font, Color accent)
    {
        Raylib.DrawRectangleRounded(rect, 0.55f, 12, new Color(255, 255, 255, 230));
        Raylib.DrawCircle((int)(rect.X + 25), (int)(rect.Y + 23), 13, accent);
        Raylib.DrawTextEx(font, text, new Vector2(rect.X + 48, rect.Y + 11), 24, 1, new Color(65, 61, 93, 255));
    }
}

internal sealed class Butterfly
{
    private readonly Vector2 _center;
    private readonly float _radius;
    private readonly float _speed;
    private readonly float _phase;
    private readonly Color _color;
    private float _t;

    public Butterfly(Vector2 center, float radius, float speed, float phase, Color color)
    {
        _center = center;
        _radius = radius;
        _speed = speed;
        _phase = phase;
        _color = color;
    }

    public void Update(float dt) => _t += dt;

    public void Draw()
    {
        var ang = _t * _speed + _phase;
        var pos = _center + new Vector2(MathF.Sin(ang) * _radius, MathF.Sin(ang * 2f) * _radius * 0.5f);
        var flap = MathF.Sin(_t * 18f);
        var wingW = 7f + flap * 2.5f;
        var wingH = 6f;
        Raylib.DrawEllipse((int)(pos.X - 5), (int)pos.Y, (int)wingW, (int)wingH, _color);
        Raylib.DrawEllipse((int)(pos.X + 5), (int)pos.Y, (int)wingW, (int)wingH, _color);
        Raylib.DrawCircleV(pos, 2.4f, new Color(60, 40, 80, 255));
    }
}
