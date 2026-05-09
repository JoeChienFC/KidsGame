using System.Numerics;
using KidsGame.Effects;
using KidsGame.Util;
using Raylib_cs;

namespace KidsGame.Scenes;

public sealed class DinoRescueScene : IScene
{
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

    private static readonly int[] RequiredHitsByStage = [45, 68, 90, 108, 124];
    private readonly Game _game;
    private readonly ParticleSystem _particles = new();
    private readonly FloatingTextSystem _floatingTexts = new();
    private readonly RescueVehicle[] _vehicles;
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

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            HitCage();
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
        DrawDinoEvent();
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

    private int CurrentRequiredHits => RequiredHitsByStage[_stage];

    private bool HasNextStage => _stage < RequiredHitsByStage.Length - 1;

    private string CurrentDinoName => _stage switch
    {
        0 => "恐龍",
        1 => "冠龍",
        2 => "劍龍",
        3 => "腕龍",
        _ => "翼龍",
    };

    private string NextDinoName => Math.Min(_stage + 1, RequiredHitsByStage.Length - 1) switch
    {
        1 => "冠龍",
        2 => "劍龍",
        3 => "腕龍",
        _ => "翼龍",
    };

    private string CurrentStageLabel => _stage switch
    {
        0 => "第一關",
        1 => "第二關",
        2 => "第三關",
        3 => "第四關",
        _ => "第五關",
    };

    private string CurrentBackgroundKey => _stage switch
    {
        0 => "bg_dino_stage_meadow",
        1 => "bg_dino_stage_jungle",
        2 => "bg_dino_stage_valley",
        3 => "bg_dino_stage_jungle",
        _ => "bg_dino_stage_valley",
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
        _game.Audio.Play("sfx_rescue_complete");
        _particles.EmitFirework(_cagePosition, 36);
        _particles.EmitStars(_cagePosition + new Vector2(0, -80), 24);
        _floatingTexts.Spawn($"{CurrentStageLabel}：拯救{CurrentDinoName}!", _cagePosition + new Vector2(0, -170), new Color(255, 90, 160, 255), 42f, 2.2f);
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
        var stageTitle = $"{CurrentStageLabel}：{CurrentDinoName}被籠子關住了";
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
        0 => RescueSprite.DinoCaged,
        1 => RescueSprite.CrestedDinoCaged,
        2 => RescueSprite.StegoDinoCaged,
        3 => RescueSprite.BrachioDinoCaged,
        _ => RescueSprite.PteroDinoCaged,
    };

    private RescueSprite CurrentCrackedDinoSprite => _stage switch
    {
        0 => RescueSprite.DinoCracked,
        1 => RescueSprite.CrestedDinoCracked,
        2 => RescueSprite.StegoDinoCracked,
        3 => RescueSprite.BrachioDinoCracked,
        _ => RescueSprite.PteroDinoCracked,
    };

    private Rectangle CurrentCagedDinoBox(Vector2 shake, float impactPulse)
    {
        var baseSize = _stage switch
        {
            3 => 318f,
            4 => 300f,
            2 => 260f,
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
        var size = _stage >= 3 ? 282f : 245f;
        return new Rectangle(_cagePosition.X + 46, _cagePosition.Y + 34, size, size);
    }

    private Rectangle CurrentFreeDinoBox(float bob)
    {
        return _stage switch
        {
            3 => new Rectangle(_cagePosition.X - 96, _cagePosition.Y + 6 + bob, 318, 330),
            4 => new Rectangle(_cagePosition.X - 94, _cagePosition.Y - 28 + bob, 352, 230),
            0 => new Rectangle(_cagePosition.X - 128, _cagePosition.Y + 28 + bob, 192, 192),
            _ => new Rectangle(_cagePosition.X - 128, _cagePosition.Y + 28 + bob, 206, 206),
        };
    }

    private void DrawCurrentFreeDino(Rectangle rect, float rotation)
    {
        if (_stage == 0)
        {
            GeneratedSprites.TryDrawFinal(_game.Assets, FinalSprite.DinoFree, rect, Color.White, rotation);
            return;
        }

        var sprite = _stage switch
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
        var eased = SmoothStep(Progress);
        var active = _hits > 0 && !_complete;
        var transform = Math.Clamp(_transformTimer / 0.48f, 0f, 1f);
        var transformPulse = MathF.Sin(transform * MathF.PI);
        foreach (var vehicle in _vehicles)
        {
            var pos = Vector2.Lerp(vehicle.Start, vehicle.Target, eased);
            var bob = MathF.Sin(_time * 7f + vehicle.Phase) * (active ? 8f : 3f);
            var rotation = active ? MathF.Sin(_time * 10f + vehicle.Phase) * (5f + transformPulse * 4f) : 0f;
            var robotMode = _hits > 0;
            var size = vehicle.Size * (robotMode ? 1.24f : 1f) * (1f + transformPulse * 0.16f);

            Raylib.DrawEllipse((int)pos.X, (int)(pos.Y + vehicle.Size * 0.38f), (int)(vehicle.Size * 0.34f), 9, new Color(0, 0, 0, 55));
            if (transformPulse > 0.01f)
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
        DrawPill(new Rectangle(24, 22, 260, 46), $"拆籠子 {_hits} / {CurrentRequiredHits}", new Color(77, 167, 224, 255));

        DrawProgressBar(new Rectangle(760, 650, 360, 22));

        var tip = _complete
            ? (HasNextStage ? $"{CurrentDinoName}自由了! 下一關救{NextDinoName}" : $"{CurrentDinoName}自由了! 按任意鍵回選單")
            : $"連點空白鍵，波麗 赫麗 安寶 羅伊一起救{CurrentDinoName}";
        var measured = Raylib.MeasureTextEx(font, tip, 24, 1);
        Raylib.DrawTextEx(font, tip, new Vector2((Game.ScreenWidth - measured.X) / 2f, Game.ScreenHeight - 44), 24, 1, new Color(60, 85, 100, 230));
    }

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
