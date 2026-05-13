using System.Numerics;
using KidsGame.Effects;
using KidsGame.Util;
using Raylib_cs;

namespace KidsGame.Scenes;

public sealed class DinoRescueScene : IScene
{
    private enum DinoTapMode
    {
        Charge,
        Rescue,
        Wash,
        Hatch,
        Fireworks,
    }

    private sealed class RescueVehicle
    {
        public string Name { get; }
        public RescueSprite Sprite { get; }
        public RescueSprite RobotSprite { get; }
        public Vector2 Start { get; }
        public Vector2 Target { get; }
        public float Size { get; }
        public float Phase { get; }

        public RescueVehicle(string name, RescueSprite sprite, RescueSprite robotSprite, Vector2 start, Vector2 target, float size, float phase)
        {
            Name = name;
            Sprite = sprite;
            RobotSprite = robotSprite;
            Start = start;
            Target = target;
            Size = size;
            Phase = phase;
        }
    }

    private sealed class DinoStage
    {
        public DinoTapMode Mode { get; }
        public string Name { get; }
        public string BackgroundKey { get; }
        public int RequiredHits { get; }
        public int RescueIndex { get; }

        public DinoStage(DinoTapMode mode, string name, string backgroundKey, int requiredHits, int rescueIndex = -1)
        {
            Mode = mode;
            Name = name;
            BackgroundKey = backgroundKey;
            RequiredHits = requiredHits;
            RescueIndex = rescueIndex;
        }
    }

    private sealed class TapBurst
    {
        public Vector2 Position { get; }
        public float Size { get; }
        public float Lifetime { get; }
        public float Phase { get; }
        public float Age { get; set; }

        public TapBurst(Vector2 position, float size, float lifetime, float phase)
        {
            Position = position;
            Size = size;
            Lifetime = lifetime;
            Phase = phase;
        }
    }

    private static readonly DinoStage[] Stages =
    [
        new(DinoTapMode.Charge, "波麗充電", "bg_dino_stage_meadow", 35),
        new(DinoTapMode.Rescue, "恐龍", "bg_dino_stage_meadow", 45, 0),
        new(DinoTapMode.Wash, "洗恐龍", "bg_dino_stage_jungle", 42),
        new(DinoTapMode.Rescue, "冠龍", "bg_dino_stage_jungle", 55, 1),
        new(DinoTapMode.Hatch, "孵恐龍蛋", "bg_dino_stage_valley", 40),
        new(DinoTapMode.Rescue, "劍龍", "bg_dino_stage_valley", 65, 2),
        new(DinoTapMode.Fireworks, "放煙火", "bg_dino_stage_meadow", 32),
        new(DinoTapMode.Rescue, "腕龍", "bg_dino_stage_jungle", 76, 3),
        new(DinoTapMode.Rescue, "翼龍", "bg_dino_stage_valley", 84, 4),
    ];

    private readonly Game _game;
    private readonly ParticleSystem _particles = new();
    private readonly FloatingTextSystem _floatingTexts = new();
    private readonly RescueVehicle[] _vehicles;
    private readonly List<TapBurst> _tapBursts = new();
    private readonly Vector2 _cagePosition = new(980, 360);
    private readonly Cloud[] _clouds;

    private float _time;
    private int _stage;
    private int _hits;
    private bool _complete;
    private float _hitShake;
    private float _hitAnimTimer;
    private float _transformTimer;
    private float _completeTimer;

    private float _zoomPunch;
    private float _globalShake;
    private float _flashAmount;
    private float _slowMoTimer;
    private float _ambientSparkleTimer;

    public DinoRescueScene(Game game)
    {
        _game = game;
        _vehicles =
        [
            new("波麗", RescueSprite.Poli, RescueSprite.RobotPoli, new Vector2(150, 240), new Vector2(792, 428), 118, 0f),
            new("赫麗", RescueSprite.Helly, RescueSprite.RobotHelly, new Vector2(330, 198), new Vector2(844, 238), 108, 1.2f),
            new("安寶", RescueSprite.Amber, RescueSprite.RobotAmber, new Vector2(150, 498), new Vector2(1010, 528), 118, 2.4f),
            new("羅伊", RescueSprite.Roy, RescueSprite.RobotRoy, new Vector2(338, 480), new Vector2(1128, 286), 126, 3.5f),
        ];

        _clouds = new Cloud[5];
        var rand = new Random(7);
        for (var i = 0; i < _clouds.Length; i++)
        {
            _clouds[i] = new Cloud(
                position: new Vector2((float)rand.NextDouble() * Game.ScreenWidth, 30f + (float)rand.NextDouble() * 110f),
                scale: 0.6f + (float)rand.NextDouble() * 0.6f,
                speed: 8f + (float)rand.NextDouble() * 12f);
        }
    }

    public void Update(float dt)
    {
        _time += dt;
        _hitShake = MathF.Max(0f, _hitShake - dt * 3f);
        _hitAnimTimer = MathF.Max(0f, _hitAnimTimer - dt);
        _transformTimer = MathF.Max(0f, _transformTimer - dt);
        _zoomPunch = MathF.Max(0f, _zoomPunch - dt * 3.4f);
        _globalShake = MathF.Max(0f, _globalShake - dt * 9f);
        _flashAmount = MathF.Max(0f, _flashAmount - dt * 2.4f);

        var effectiveDt = dt;
        if (_slowMoTimer > 0f)
        {
            _slowMoTimer -= dt;
            effectiveDt = dt * 0.42f;
        }

        foreach (var c in _clouds) c.Update(effectiveDt);
        UpdateTapBursts(effectiveDt);

        if (_complete)
        {
            _completeTimer += effectiveDt;
            _particles.Update(effectiveDt);
            _floatingTexts.Update(dt);

            _ambientSparkleTimer -= dt;
            if (_completeTimer < 4f && _ambientSparkleTimer <= 0f)
            {
                _ambientSparkleTimer = 0.05f;
                var x = (float)Random.Shared.NextDouble() * Game.ScreenWidth;
                _particles.EmitConfetti(new Vector2(x, -10), 1);
            }

            if (HasNextStage && _completeTimer > 2.6f)
            {
                StartNextStage();
                return;
            }

            if (_completeTimer > 0.6f && Raylib.GetKeyPressed() != 0)
            {
                if (HasNextStage)
                {
                    StartNextStage();
                }
                else
                {
                    _game.ChangeScene(new TitleScene(_game));
                }
            }
            return;
        }

        if (AnyActionPressed())
        {
            HitCurrentStage();
        }

        _ambientSparkleTimer -= effectiveDt;
        if (_ambientSparkleTimer <= 0f)
        {
            _ambientSparkleTimer = 0.18f;
            var x = (float)Random.Shared.NextDouble() * Game.ScreenWidth;
            var y = 50f + (float)Random.Shared.NextDouble() * (Game.ScreenHeight - 200f);
            _particles.EmitSparkle(new Vector2(x, y), 1);
        }

        _particles.Update(effectiveDt);
        _floatingTexts.Update(dt);
    }

    private static bool AnyActionPressed()
    {
        return Raylib.GetKeyPressed() != 0 || Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    private void UpdateTapBursts(float dt)
    {
        for (var i = _tapBursts.Count - 1; i >= 0; i--)
        {
            _tapBursts[i].Age += dt;
            if (_tapBursts[i].Age >= _tapBursts[i].Lifetime)
            {
                _tapBursts.RemoveAt(i);
            }
        }
    }

    public void Draw()
    {
        Raylib.ClearBackground(new Color(162, 219, 255, 255));

        var shake = Vector2.Zero;
        if (_globalShake > 0.01f)
        {
            var t = (float)Raylib.GetTime();
            shake = new Vector2(MathF.Sin(t * 53f) * _globalShake, MathF.Cos(t * 47f) * _globalShake);
        }
        var zoom = 1f + _zoomPunch * 0.06f;
        var center = new Vector2(Game.ScreenWidth / 2f, Game.ScreenHeight / 2f);
        Raylib.BeginMode2D(new Camera2D
        {
            Target = center,
            Offset = center + shake,
            Rotation = 0,
            Zoom = zoom,
        });

        DrawMap();
        foreach (var c in _clouds) c.Draw();
        DrawStageEvent();
        DrawVehicles();

        _particles.Draw(_game.Assets);
        _floatingTexts.Draw(_game.Assets.GetFont());

        Raylib.EndMode2D();

        DrawVignette();

        if (_flashAmount > 0.01f)
        {
            var alpha = (int)(220 * Math.Clamp(_flashAmount, 0f, 1f));
            Raylib.DrawRectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight, new Color(255, 255, 255, alpha));
        }

        DrawHud();
    }

    private void HitCage()
    {
        _hits++;
        _hitShake = 0.38f;
        _hitAnimTimer = 0.36f;
        _transformTimer = 0.48f;
        _game.Audio.Play("sfx_rescue_hit");

        var burst = _cagePosition + new Vector2(MathF.Sin(_time * 15f) * 34f, MathF.Cos(_time * 11f) * 28f);
        _particles.EmitFirework(burst, 9);
        _particles.EmitStars(burst, 5);

        // Per-press feedback — every space-bar tap thumps the camera so the toddler feels it
        _zoomPunch = MathF.Max(_zoomPunch, 0.35f);
        _globalShake = MathF.Max(_globalShake, 3.5f);

        if (_hits == 1)
        {
            _floatingTexts.Spawn("救援變身!", _cagePosition + new Vector2(0, -148), new Color(255, 90, 160, 255), 36f, 1.5f);
            _zoomPunch = MathF.Max(_zoomPunch, 0.6f);
            _flashAmount = 0.4f;
        }

        if (_hits >= CurrentRequiredHits)
        {
            _complete = true;
            _completeTimer = 0f;
            _game.Audio.Play("sfx_rescue_complete");
            _particles.EmitFirework(_cagePosition, 70);
            _particles.EmitHearts(_cagePosition + new Vector2(0, -28), 50);
            _particles.EmitStars(_cagePosition, 46);
            _particles.EmitConfetti(_cagePosition + new Vector2(0, -40), 36);
            _floatingTexts.Spawn($"{CurrentDinoName}自由了!", _cagePosition + new Vector2(0, -170), new Color(255, 90, 160, 255), 46f, 2.0f);
            _zoomPunch = 1f;
            _slowMoTimer = 0.55f;
            _globalShake = 14f;
            _flashAmount = 0.95f;
        }
    }

    private void HitCurrentStage()
    {
        if (CurrentMode == DinoTapMode.Rescue)
        {
            HitCage();
            return;
        }

        _hits++;
        _hitShake = 0.26f;
        _hitAnimTimer = 0.34f;
        _transformTimer = 0.42f;
        _game.Audio.Play("sfx_rescue_hit");
        _zoomPunch = MathF.Max(_zoomPunch, CurrentMode == DinoTapMode.Fireworks ? 0.48f : 0.32f);
        _globalShake = MathF.Max(_globalShake, CurrentMode == DinoTapMode.Fireworks ? 4.8f : 2.8f);

        var focus = CurrentModeFocus;
        switch (CurrentMode)
        {
            case DinoTapMode.Charge:
                _particles.EmitStars(focus + new Vector2(0, -35), 8);
                _particles.EmitSparkle(focus + new Vector2(0, -80), 3);
                AddTapBurst(focus + new Vector2(0, -42), 96f, 0.48f);
                if (_hits == 1) _floatingTexts.Spawn("開始充電!", focus + new Vector2(0, -148), new Color(77, 167, 224, 255), 34f, 1.4f);
                break;
            case DinoTapMode.Wash:
                _particles.EmitSparkle(focus + new Vector2(Random.Shared.Next(-85, 86), Random.Shared.Next(-72, 65)), 5);
                _particles.EmitStars(focus + new Vector2(Random.Shared.Next(-70, 70), Random.Shared.Next(-70, 52)), 4);
                AddTapBurst(focus + new Vector2(Random.Shared.Next(-90, 90), Random.Shared.Next(-80, 60)), 76f, 0.7f);
                if (_hits == 1) _floatingTexts.Spawn("泡泡洗一洗!", focus + new Vector2(0, -150), new Color(77, 167, 224, 255), 34f, 1.4f);
                break;
            case DinoTapMode.Hatch:
                _particles.EmitStars(focus + new Vector2(Random.Shared.Next(-52, 53), Random.Shared.Next(-72, 45)), 7);
                AddTapBurst(focus + new Vector2(0, -20), 92f, 0.5f);
                if (_hits == 1) _floatingTexts.Spawn("敲敲恐龍蛋!", focus + new Vector2(0, -148), new Color(255, 174, 64, 255), 34f, 1.4f);
                break;
            case DinoTapMode.Fireworks:
                var burst = new Vector2(620 + Random.Shared.Next(-260, 300), 170 + Random.Shared.Next(-80, 220));
                _particles.EmitFirework(burst, 26);
                _particles.EmitStars(burst, 14);
                AddTapBurst(burst, 130f + Random.Shared.Next(0, 60), 0.95f);
                if (_hits == 1) _floatingTexts.Spawn("放煙火!", new Vector2(640, 110), new Color(255, 90, 160, 255), 38f, 1.4f);
                break;
        }

        if (_hits >= CurrentRequiredHits)
        {
            CompleteCurrentStage();
        }
    }

    private void AddTapBurst(Vector2 position, float size, float lifetime)
    {
        _tapBursts.Add(new TapBurst(position, size, lifetime, Random.Shared.NextSingle() * MathF.Tau));
        if (_tapBursts.Count > 18)
        {
            _tapBursts.RemoveAt(0);
        }
    }

    private void CompleteCurrentStage()
    {
        _complete = true;
        _completeTimer = 0f;
        _game.Audio.Play("sfx_rescue_complete");
        var focus = CurrentModeFocus;
        _particles.EmitFirework(focus, 70);
        _particles.EmitHearts(focus + new Vector2(0, -28), 50);
        _particles.EmitStars(focus, 46);
        _particles.EmitConfetti(focus + new Vector2(0, -40), 36);
        _floatingTexts.Spawn(CurrentCompleteText, focus + new Vector2(0, -170), new Color(255, 90, 160, 255), 46f, 2.0f);
        _zoomPunch = 1f;
        _slowMoTimer = 0.55f;
        _globalShake = 14f;
        _flashAmount = 0.95f;
    }

    private DinoStage CurrentStage => Stages[_stage];

    private DinoTapMode CurrentMode => CurrentStage.Mode;

    private int CurrentRequiredHits => CurrentStage.RequiredHits;

    private bool HasNextStage => _stage < Stages.Length - 1;

    private string CurrentDinoName => CurrentStage.Name;

    private string NextDinoName => Stages[Math.Min(_stage + 1, Stages.Length - 1)].Name;

    private string CurrentStageLabel => $"第{_stage + 1}關";

    private string CurrentBackgroundKey => CurrentStage.BackgroundKey;

    private string CurrentActionName => CurrentMode switch
    {
        DinoTapMode.Charge => "充電",
        DinoTapMode.Wash => "洗乾淨",
        DinoTapMode.Hatch => "孵蛋",
        DinoTapMode.Fireworks => "煙火",
        _ => "救援",
    };

    private string CurrentStageTitle => CurrentMode switch
    {
        DinoTapMode.Charge => "幫波麗充電",
        DinoTapMode.Wash => "幫恐龍洗澡",
        DinoTapMode.Hatch => "孵出小恐龍",
        DinoTapMode.Fireworks => "一起放煙火",
        _ => $"拯救{CurrentDinoName}",
    };

    private string CurrentCompleteText => CurrentMode switch
    {
        DinoTapMode.Charge => "波麗充滿電!",
        DinoTapMode.Wash => "恐龍洗乾淨了!",
        DinoTapMode.Hatch => "小恐龍出生了!",
        DinoTapMode.Fireworks => "煙火好漂亮!",
        _ => $"{CurrentDinoName}自由了!",
    };

    private Vector2 CurrentModeFocus => CurrentMode switch
    {
        DinoTapMode.Charge => new Vector2(890, 378),
        DinoTapMode.Wash => new Vector2(930, 390),
        DinoTapMode.Hatch => new Vector2(930, 390),
        DinoTapMode.Fireworks => new Vector2(900, 500),
        _ => _cagePosition,
    };

    private float Progress => Math.Clamp(_hits / (float)CurrentRequiredHits, 0f, 1f);

    private void StartNextStage()
    {
        if (!HasNextStage) return;

        _stage++;
        _hits = 0;
        _complete = false;
        _completeTimer = 0f;
        _hitShake = 0f;
        _hitAnimTimer = 0f;
        _transformTimer = 0.7f;
        _zoomPunch = 0.8f;
        _globalShake = 8f;
        _flashAmount = 0.75f;
        _slowMoTimer = 0.35f;
        _tapBursts.Clear();
        _game.Audio.Play("sfx_rescue_complete");
        _particles.EmitFirework(CurrentModeFocus, 36);
        _particles.EmitStars(CurrentModeFocus + new Vector2(0, -80), 24);
        _floatingTexts.Spawn($"{CurrentStageLabel}：{CurrentStageTitle}!", CurrentModeFocus + new Vector2(0, -170), new Color(255, 90, 160, 255), 42f, 2.2f);
    }

    private void DrawMap()
    {
        DrawStageBackground();
        Raylib.DrawRectangle(0, 0, 500, Game.ScreenHeight, new Color(185, 230, 255, 226));
        Raylib.DrawRectangleRounded(new Rectangle(36, 86, 418, 538), 0.06f, 12, new Color(255, 255, 255, 150));
        Raylib.DrawRectangleRoundedLines(new Rectangle(36, 86, 418, 538), 0.06f, 12, new Color(104, 168, 207, 172));
        Raylib.DrawRectangleRounded(new Rectangle(535, 128, 682, 442), 0.08f, 14, new Color(255, 255, 255, 74));
        Raylib.DrawRectangleRounded(new Rectangle(562, 156, 625, 370), 0.08f, 14, new Color(112, 198, 124, 172));
        Raylib.DrawRectangleRounded(new Rectangle(622, 520, 480, 78), 0.35f, 18, new Color(207, 181, 119, 230));
        Raylib.DrawRectangleRoundedLines(new Rectangle(622, 520, 480, 78), 0.35f, 18, new Color(175, 146, 91, 220));

        for (var i = 0; i < 18; i++)
        {
            var x = 582 + i * 38;
            var bushBob = MathF.Sin(_time * 1.3f + i * 0.4f) * 1.2f;
            Raylib.DrawCircle(x, 112 + (i % 3) * 18 + (int)bushBob, 14, new Color(92, 168, 92, 255));
            Raylib.DrawCircle(x + 10, 632 - (i % 4) * 16 - (int)bushBob, 13, new Color(76, 152, 84, 255));
        }

        var font = _game.Assets.GetFont();
        Raylib.DrawTextEx(font, "救援基地", new Vector2(74, 74), 30, 1, new Color(45, 87, 120, 255));
        var stageTitle = $"{CurrentStageLabel}：{CurrentStageTitle}";
        Raylib.DrawTextEx(font, stageTitle, new Vector2(742, 56), 30, 1, new Color(66, 88, 75, 255));
    }

    private void DrawStageBackground()
    {
        var texture = _game.Assets.FindTexture(CurrentBackgroundKey);
        if (texture.HasValue)
        {
            Raylib.DrawTexturePro(
                texture.Value,
                new Rectangle(0, 0, texture.Value.Width, texture.Value.Height),
                new Rectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight),
                Vector2.Zero,
                0,
                Color.White);
            Raylib.DrawRectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight, new Color(255, 255, 255, 26));
            return;
        }

        Raylib.DrawRectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight, new Color(126, 205, 134, 255));
    }

    private void DrawStageEvent()
    {
        if (CurrentMode == DinoTapMode.Rescue)
        {
            DrawDinoEvent();
            return;
        }

        DrawTapMiniGameEvent();
    }

    private void DrawTapMiniGameEvent()
    {
        switch (CurrentMode)
        {
            case DinoTapMode.Charge:
                DrawChargeEvent();
                break;
            case DinoTapMode.Wash:
                DrawWashEvent();
                break;
            case DinoTapMode.Hatch:
                DrawHatchEvent();
                break;
            case DinoTapMode.Fireworks:
                DrawFireworksEvent();
                break;
        }

        DrawTapBursts();
    }

    private void DrawChargeEvent()
    {
        var focus = CurrentModeFocus;
        var pulse = (MathF.Sin(_time * 5f) + 1f) * 0.5f;
        var impact = Math.Clamp(_hitAnimTimer / 0.34f, 0f, 1f);
        var impactPulse = MathF.Sin(impact * MathF.PI);

        Raylib.DrawEllipse((int)focus.X, (int)(focus.Y + 164), 170, 20, new Color(0, 0, 0, 55));
        DrawNamedTexture("tap_poli_charger", new Rectangle(focus.X + 84, focus.Y + 30 - impactPulse * 6f, 220 + impactPulse * 16f, 235 + impactPulse * 16f), Color.White);
        DrawNamedTexture("tap_battery", new Rectangle(focus.X - 140, focus.Y - 86, 205, 120), new Color(255, 255, 255, 185));
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.Poli, new Rectangle(focus.X - 155, focus.Y + 92 + pulse * 3f, 158, 158), Color.White, MathF.Sin(_time * 6f) * 2f);

        var bar = new Rectangle(focus.X - 184, focus.Y + 210, 368, 28);
        Raylib.DrawRectangleRounded(bar, 0.45f, 10, new Color(255, 255, 255, 235));
        Raylib.DrawRectangleRounded(new Rectangle(bar.X + 5, bar.Y + 5, (bar.Width - 10) * Progress, bar.Height - 10), 0.45f, 10, new Color(255, 215, 72, 255));
        Raylib.DrawRectangleRoundedLines(bar, 0.45f, 10, new Color(45, 87, 120, 255));

        for (var i = 0; i < 6; i++)
        {
            var angle = _time * 5.5f + i * MathF.Tau / 6f;
            var p = focus + new Vector2(MathF.Cos(angle) * (110f + pulse * 12f), MathF.Sin(angle) * 72f);
            Raylib.DrawCircleV(p, 5f + pulse * 2f, new Color(255, 226, 82, 205));
        }
    }

    private void DrawWashEvent()
    {
        var focus = CurrentModeFocus;
        var impact = Math.Clamp(_hitAnimTimer / 0.34f, 0f, 1f);
        var impactPulse = MathF.Sin(impact * MathF.PI);
        var bob = MathF.Sin(_time * 4f) * 5f;
        var dirtyAlpha = (int)(255 * (1f - Progress));
        var cleanAlpha = (int)(110 + 145 * Progress);

        Raylib.DrawEllipse((int)focus.X, (int)(focus.Y + 142), 185, 22, new Color(0, 0, 0, 55));
        DrawNamedTexture("tap_clean_dino", new Rectangle(focus.X, focus.Y + bob, 310, 275), new Color(255, 255, 255, cleanAlpha));
        if (dirtyAlpha > 12)
        {
            DrawNamedTexture("tap_dirty_dino", new Rectangle(focus.X, focus.Y + bob, 320, 278), new Color(255, 255, 255, dirtyAlpha));
        }

        var spongeAngle = _time * 5f + Progress * MathF.Tau * 2f;
        var spongePos = focus + new Vector2(MathF.Cos(spongeAngle) * 125f, -28f + MathF.Sin(spongeAngle * 1.2f) * 78f);
        DrawNamedTexture("tap_wash_sponge", new Rectangle(spongePos.X, spongePos.Y - impactPulse * 12f, 126 + impactPulse * 18f, 98 + impactPulse * 14f), Color.White, MathF.Sin(_time * 12f) * 8f);

        for (var i = 0; i < 18; i++)
        {
            var angle = i * MathF.Tau / 18f + _time * 0.9f;
            var radius = 98f + (i % 5) * 13f + Progress * 26f;
            var p = focus + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * 0.62f);
            Raylib.DrawCircleV(p, 6f + i % 4, new Color(225, 248, 255, 135));
            Raylib.DrawCircleLines((int)p.X, (int)p.Y, 6 + i % 4, new Color(255, 255, 255, 150));
        }
    }

    private void DrawHatchEvent()
    {
        var focus = CurrentModeFocus;
        var impact = Math.Clamp(_hitAnimTimer / 0.34f, 0f, 1f);
        var impactPulse = MathF.Sin(impact * MathF.PI);
        var shake = _hitShake > 0f ? MathF.Sin(_time * 72f) * _hitShake * 18f : 0f;
        var eggTexture = Progress < 0.58f ? "tap_dino_egg" : "tap_dino_egg_cracked";

        Raylib.DrawEllipse((int)focus.X, (int)(focus.Y + 155), 150, 20, new Color(0, 0, 0, 55));
        if (_complete)
        {
            var bob = MathF.Sin(_time * 5f) * 7f;
            DrawNamedTexture("tap_baby_dino", new Rectangle(focus.X, focus.Y + bob, 305, 305), Color.White, MathF.Sin(_time * 5f) * 2f);
        }
        else
        {
            DrawNamedTexture(eggTexture, new Rectangle(focus.X + shake, focus.Y - impactPulse * 8f, 280 + impactPulse * 18f, 300 + impactPulse * 18f), Color.White, shake * 0.18f);
        }

        if (Progress > 0.35f && !_complete)
        {
            var crackCount = Math.Clamp((int)(Progress * 9f), 3, 9);
            for (var i = 0; i < crackCount; i++)
            {
                var start = focus + new Vector2(-64 + i * 15, -38 + (i % 3) * 22);
                Raylib.DrawLineEx(start, start + new Vector2(10, 18), 3f, new Color(255, 255, 255, 210));
            }
        }
    }

    private void DrawFireworksEvent()
    {
        var focus = CurrentModeFocus;
        var impact = Math.Clamp(_hitAnimTimer / 0.34f, 0f, 1f);
        var impactPulse = MathF.Sin(impact * MathF.PI);

        Raylib.DrawRectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight, new Color(42, 64, 128, 48));
        DrawNamedTexture("tap_firework_launcher", new Rectangle(focus.X, focus.Y - impactPulse * 6f, 260 + impactPulse * 16f, 220 + impactPulse * 16f), Color.White);
        GeneratedSprites.TryDraw(_game.Assets, RescueSprite.RobotPoli, new Rectangle(672, 528 + MathF.Sin(_time * 4f) * 4f, 132, 132), Color.White, MathF.Sin(_time * 4f) * 2f);

        for (var i = 0; i < 26; i++)
        {
            var x = 548 + i * 27;
            var y = 96 + (i * 47 % 310);
            var twinkle = (MathF.Sin(_time * 4f + i) + 1f) * 0.5f;
            Raylib.DrawCircle(x, y, 2f + twinkle * 2f, new Color(255, 245, 172, (int)(120 + twinkle * 100)));
        }
    }

    private void DrawTapBursts()
    {
        foreach (var burst in _tapBursts)
        {
            var t = Math.Clamp(burst.Age / burst.Lifetime, 0f, 1f);
            var alpha = (int)(255 * (1f - t));
            var scale = 0.65f + t * 0.6f;
            DrawNamedTexture("tap_firework_burst", new Rectangle(burst.Position.X, burst.Position.Y, burst.Size * scale, burst.Size * scale), new Color(255, 255, 255, alpha), burst.Phase * 57.2958f + t * 24f);
        }
    }

    private void DrawDinoEvent()
    {
        var shake = _hitShake > 0f
            ? new Vector2(MathF.Sin(_time * 72f) * _hitShake * 22f, MathF.Cos(_time * 64f) * _hitShake * 9f)
            : Vector2.Zero;
        var impact = Math.Clamp(_hitAnimTimer / 0.36f, 0f, 1f);
        var impactPulse = MathF.Sin(impact * MathF.PI);

        if (_complete)
        {
            var bob = MathF.Sin(_time * 5f) * 7f;
            GeneratedSprites.TryDrawFinal(_game.Assets, FinalSprite.CageOpen, CurrentOpenCageBox(), Color.White);
            DrawCurrentFreeDino(CurrentFreeDinoBox(bob), MathF.Sin(_time * 5f) * 2f);
            return;
        }

        var sprite = Progress >= 0.5f ? CurrentCrackedDinoSprite : CurrentCagedDinoSprite;
        GeneratedSprites.TryDraw(_game.Assets, sprite, CurrentCagedDinoBox(shake, impactPulse), Color.White, shake.X * 0.05f);
        DrawCageDamage(impactPulse);

        if (_hits > 0)
        {
            for (var i = 0; i < 5; i++)
            {
                var angle = _time * 4f + i * MathF.Tau / 5f;
                var p = _cagePosition + new Vector2(MathF.Cos(angle) * 132f, MathF.Sin(angle) * 88f);
                Raylib.DrawCircleV(p, 5f + MathF.Sin(_time * 8f + i) * 2f, new Color(255, 226, 82, 210));
            }
        }
    }

    private RescueSprite CurrentCagedDinoSprite => _stage switch
    {
        _ when CurrentStage.RescueIndex == 0 => RescueSprite.DinoCaged,
        _ when CurrentStage.RescueIndex == 1 => RescueSprite.CrestedDinoCaged,
        _ when CurrentStage.RescueIndex == 2 => RescueSprite.StegoDinoCaged,
        _ when CurrentStage.RescueIndex == 3 => RescueSprite.BrachioDinoCaged,
        _ => RescueSprite.PteroDinoCaged,
    };

    private RescueSprite CurrentCrackedDinoSprite => _stage switch
    {
        _ when CurrentStage.RescueIndex == 0 => RescueSprite.DinoCracked,
        _ when CurrentStage.RescueIndex == 1 => RescueSprite.CrestedDinoCracked,
        _ when CurrentStage.RescueIndex == 2 => RescueSprite.StegoDinoCracked,
        _ when CurrentStage.RescueIndex == 3 => RescueSprite.BrachioDinoCracked,
        _ => RescueSprite.PteroDinoCracked,
    };

    private Rectangle CurrentCagedDinoBox(Vector2 shake, float impactPulse)
    {
        var baseSize = _stage switch
        {
            _ when CurrentStage.RescueIndex == 3 => 318f,
            _ when CurrentStage.RescueIndex == 4 => 300f,
            _ when CurrentStage.RescueIndex == 2 => 260f,
            _ => 245f,
        };
        return new Rectangle(
            _cagePosition.X + shake.X,
            _cagePosition.Y + shake.Y - impactPulse * 6f,
            baseSize + impactPulse * 14f,
            baseSize + impactPulse * 12f);
    }

    private Rectangle CurrentOpenCageBox()
    {
        var size = CurrentStage.RescueIndex >= 3 ? 282f : 245f;
        return new Rectangle(_cagePosition.X + 46, _cagePosition.Y + 34, size, size);
    }

    private Rectangle CurrentFreeDinoBox(float bob)
    {
        return _stage switch
        {
            _ when CurrentStage.RescueIndex == 3 => new Rectangle(_cagePosition.X - 96, _cagePosition.Y + 6 + bob, 318, 330),
            _ when CurrentStage.RescueIndex == 4 => new Rectangle(_cagePosition.X - 94, _cagePosition.Y - 28 + bob, 352, 230),
            _ when CurrentStage.RescueIndex == 0 => new Rectangle(_cagePosition.X - 128, _cagePosition.Y + 28 + bob, 192, 192),
            _ => new Rectangle(_cagePosition.X - 128, _cagePosition.Y + 28 + bob, 206, 206),
        };
    }

    private void DrawCurrentFreeDino(Rectangle rect, float rotation)
    {
        if (CurrentStage.RescueIndex == 0)
        {
            GeneratedSprites.TryDrawFinal(_game.Assets, FinalSprite.DinoFree, rect, Color.White, rotation);
            return;
        }

        var sprite = CurrentStage.RescueIndex switch
        {
            1 => RescueSprite.CrestedDinoFree,
            2 => RescueSprite.StegoDinoFree,
            3 => RescueSprite.BrachioDinoFree,
            _ => RescueSprite.PteroDinoFree,
        };
        GeneratedSprites.TryDraw(_game.Assets, sprite, rect, Color.White, rotation);
    }

    private void DrawCageDamage(float impactPulse)
    {
        var crackColor = new Color(68, 73, 86, 225);
        var crackCount = Math.Clamp((int)(Progress * 10f) + (_hitAnimTimer > 0 ? 2 : 0), 0, 12);
        for (var i = 0; i < crackCount; i++)
        {
            var x = -82 + i * 15;
            var y = -58 + (i % 4) * 24;
            var start = _cagePosition + new Vector2(x, y);
            var mid = start + new Vector2(9, 18);
            var end = mid + new Vector2(-7 + i % 3 * 7, 20);
            Raylib.DrawLineEx(start, mid, 3f, crackColor);
            Raylib.DrawLineEx(mid, end, 3f, crackColor);
        }

        if (Progress > 0.62f)
        {
            for (var i = 0; i < 7; i++)
            {
                var fall = (Progress - 0.62f) * 96f;
                var center = _cagePosition + new Vector2(-95 + i * 32, 34 + fall + MathF.Sin(_time * 7f + i) * 6f);
                Raylib.DrawLineEx(center + new Vector2(-9, -18), center + new Vector2(9, 18), 4f, new Color(90, 94, 105, 210));
            }
        }

        if (impactPulse <= 0.01f) return;

        Raylib.DrawCircleLines((int)_cagePosition.X, (int)_cagePosition.Y, (int)(112 + impactPulse * 48f), new Color(255, 245, 120, (int)(200 * impactPulse)));
        for (var i = 0; i < 16; i++)
        {
            var angle = i * MathF.Tau / 16f + _time;
            var p = _cagePosition + new Vector2(MathF.Cos(angle) * (96f + impactPulse * 38f), MathF.Sin(angle) * (70f + impactPulse * 24f));
            Raylib.DrawCircleV(p, 4f + i % 3, new Color(255, 226, 82, (int)(230 * impactPulse)));
        }
    }

    private void DrawVehicles()
    {
        var rescueMode = CurrentMode == DinoTapMode.Rescue;
        var eased = rescueMode ? SmoothStep(Progress) : 0f;
        var active = rescueMode && _hits > 0 && !_complete;
        var transform = Math.Clamp(_transformTimer / 0.48f, 0f, 1f);
        var transformPulse = MathF.Sin(transform * MathF.PI);
        foreach (var vehicle in _vehicles)
        {
            var pos = Vector2.Lerp(vehicle.Start, vehicle.Target, eased);
            var bob = MathF.Sin(_time * 7f + vehicle.Phase) * (active ? 8f : 3f);
            var rotation = active ? MathF.Sin(_time * 10f + vehicle.Phase) * (5f + transformPulse * 4f) : 0f;
            var robotMode = rescueMode && _hits > 0;
            var size = vehicle.Size * (robotMode ? 1.24f : 1f) * (1f + transformPulse * 0.16f);

            Raylib.DrawEllipse((int)pos.X, (int)(pos.Y + vehicle.Size * 0.38f), (int)(vehicle.Size * 0.34f), 9, new Color(0, 0, 0, 55));
            if (rescueMode && transformPulse > 0.01f)
            {
                DrawTransformAura(pos + new Vector2(0, bob), vehicle.Phase, transformPulse);
            }
            if (active)
            {
                DrawRobotWorkAction(pos + new Vector2(0, bob), vehicle.Phase, transformPulse);
            }
            GeneratedSprites.TryDraw(_game.Assets, robotMode ? vehicle.RobotSprite : vehicle.Sprite, new Rectangle(pos.X, pos.Y + bob, size, size), Color.White, rotation);
            DrawVehicleName(vehicle.Name, pos + new Vector2(0, vehicle.Size * 0.54f));
        }
    }

    private void DrawTransformAura(Vector2 center, float phase, float pulse)
    {
        var radius = 46f + pulse * 36f;
        Raylib.DrawCircleV(center, radius, new Color(255, 245, 120, (int)(45 * pulse)));
        Raylib.DrawCircleLines((int)center.X, (int)center.Y, (int)radius, new Color(255, 255, 255, (int)(180 * pulse)));
        for (var i = 0; i < 6; i++)
        {
            var angle = _time * 7f + phase + i * MathF.Tau / 6f;
            var p = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (radius + 8f);
            Raylib.DrawCircleV(p, 5f, new Color(255, 226, 82, (int)(230 * pulse)));
        }
    }

    private void DrawRobotWorkAction(Vector2 center, float phase, float transformPulse)
    {
        var towardCage = _cagePosition - center;
        if (towardCage.LengthSquared() <= 0.01f) return;

        var dir = Vector2.Normalize(towardCage);
        var side = new Vector2(-dir.Y, dir.X);
        var reach = 48f + transformPulse * 16f + MathF.Sin(_time * 18f + phase) * 9f;
        var hand = center + dir * reach + side * MathF.Sin(_time * 10f + phase) * 10f;
        var hitPoint = _cagePosition + side * MathF.Sin(_time * 7f + phase) * 42f + new Vector2(0, MathF.Cos(_time * 8f + phase) * 24f);

        Raylib.DrawLineEx(center + dir * 18f + side * 10f, hand, 9f, new Color(70, 86, 112, 230));
        Raylib.DrawLineEx(hand, hitPoint, 7f, new Color(110, 120, 136, 230));
        Raylib.DrawCircleV(hand, 9f, new Color(80, 88, 104, 245));

        var pulse = 0.45f + transformPulse * 0.55f;
        for (var i = 0; i < 5; i++)
        {
            var angle = -MathF.PI / 2f + i * MathF.PI / 4f + phase * 0.1f;
            var p1 = hitPoint + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 8f;
            var p2 = hitPoint + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (24f + transformPulse * 12f);
            Raylib.DrawLineEx(p1, p2, 3f, new Color(255, 207, 72, (int)(220 * pulse)));
        }
    }

    private void DrawVehicleName(string name, Vector2 position)
    {
        var font = _game.Assets.GetFont();
        var measured = Raylib.MeasureTextEx(font, name, 22, 1);
        var rect = new Rectangle(position.X - measured.X / 2f - 12, position.Y, measured.X + 24, 30);
        Raylib.DrawRectangleRounded(rect, 0.45f, 10, new Color(255, 255, 255, 220));
        Raylib.DrawTextEx(font, name, new Vector2(rect.X + 12, rect.Y + 5), 22, 1, new Color(65, 61, 93, 255));
    }

    private void DrawHud()
    {
        var font = _game.Assets.GetFont();
        DrawPill(new Rectangle(24, 22, 280, 46), $"{CurrentActionName} {_hits} / {CurrentRequiredHits}", new Color(77, 167, 224, 255));

        DrawProgressBar(new Rectangle(760, 650, 360, 22));

        var tip = _complete
            ? (HasNextStage ? $"{CurrentCompleteText} 下一關{NextDinoName}" : $"{CurrentCompleteText} 按任意鍵回選單")
            : CurrentTipText;
        var measured = Raylib.MeasureTextEx(font, tip, 24, 1);
        Raylib.DrawTextEx(font, tip, new Vector2((Game.ScreenWidth - measured.X) / 2f, Game.ScreenHeight - 44), 24, 1, new Color(60, 85, 100, 230));
    }

    private string CurrentTipText => CurrentMode switch
    {
        DinoTapMode.Charge => "一直按任意鍵，幫波麗充電出發",
        DinoTapMode.Wash => "一直按任意鍵，幫恐龍洗泡泡澡",
        DinoTapMode.Hatch => "一直按任意鍵，敲敲恐龍蛋",
        DinoTapMode.Fireworks => "一直按任意鍵，放好多煙火",
        _ => $"一直按任意鍵，波麗 赫麗 安寶 羅伊一起救{CurrentDinoName}",
    };

    private void DrawPill(Rectangle rect, string text, Color accent)
    {
        var font = _game.Assets.GetFont();
        Raylib.DrawRectangleRounded(rect, 0.55f, 12, new Color(255, 255, 255, 230));
        Raylib.DrawCircle((int)(rect.X + 25), (int)(rect.Y + 23), 13, accent);
        Raylib.DrawTextEx(font, text, new Vector2(rect.X + 48, rect.Y + 11), 23, 1, new Color(65, 61, 93, 255));
    }

    private void DrawProgressBar(Rectangle rect)
    {
        Raylib.DrawRectangleRounded(rect, 0.45f, 10, new Color(255, 255, 255, 228));
        Raylib.DrawRectangleRounded(new Rectangle(rect.X + 4, rect.Y + 4, (rect.Width - 8) * Progress, rect.Height - 8), 0.45f, 10, new Color(255, 119, 168, 255));
        Raylib.DrawRectangleRoundedLines(rect, 0.45f, 10, Color.DarkPurple);
    }

    private bool DrawNamedTexture(string textureName, Rectangle box, Color tint, float rotation = 0f)
    {
        var texture = _game.Assets.FindTexture(textureName);
        if (!texture.HasValue) return false;

        var scale = MathF.Min(box.Width / texture.Value.Width, box.Height / texture.Value.Height);
        var width = texture.Value.Width * scale;
        var height = texture.Value.Height * scale;
        Raylib.DrawTexturePro(
            texture.Value,
            new Rectangle(0, 0, texture.Value.Width, texture.Value.Height),
            new Rectangle(box.X, box.Y, width, height),
            new Vector2(width / 2f, height / 2f),
            rotation,
            tint);
        return true;
    }

    private void DrawVignette()
    {
        const int edge = 90;
        Raylib.DrawRectangleGradientV(0, 0, Game.ScreenWidth, edge, new Color(0, 0, 0, 70), new Color(0, 0, 0, 0));
        Raylib.DrawRectangleGradientV(0, Game.ScreenHeight - edge, Game.ScreenWidth, edge, new Color(0, 0, 0, 0), new Color(0, 0, 0, 80));
        Raylib.DrawRectangleGradientH(0, 0, edge, Game.ScreenHeight, new Color(0, 0, 0, 60), new Color(0, 0, 0, 0));
        Raylib.DrawRectangleGradientH(Game.ScreenWidth - edge, 0, edge, Game.ScreenHeight, new Color(0, 0, 0, 0), new Color(0, 0, 0, 60));
    }

    private static float SmoothStep(float value)
    {
        value = Math.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    private sealed class Cloud
    {
        private Vector2 _position;
        private readonly float _scale;
        private readonly float _speed;

        public Cloud(Vector2 position, float scale, float speed)
        {
            _position = position;
            _scale = scale;
            _speed = speed;
        }

        public void Update(float dt)
        {
            _position.X += _speed * dt;
            if (_position.X - 80f * _scale > Game.ScreenWidth)
            {
                _position.X = -80f * _scale;
            }
        }

        public void Draw()
        {
            var puff = new Color(255, 255, 255, 215);
            var shade = new Color(220, 235, 250, 165);
            var s = _scale;
            Raylib.DrawCircleV(_position + new Vector2(-26, 4) * s, 18 * s, shade);
            Raylib.DrawCircleV(_position + new Vector2(28, 6) * s, 16 * s, shade);
            Raylib.DrawCircleV(_position + new Vector2(-22, -6) * s, 22 * s, puff);
            Raylib.DrawCircleV(_position + new Vector2(0, -10) * s, 26 * s, puff);
            Raylib.DrawCircleV(_position + new Vector2(22, -4) * s, 22 * s, puff);
            Raylib.DrawCircleV(_position + new Vector2(-2, 0) * s, 24 * s, puff);
        }
    }
}
