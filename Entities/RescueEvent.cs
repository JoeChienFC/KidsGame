using System.Numerics;
using KidsGame.Assets;
using KidsGame.Audio;
using KidsGame.Effects;
using Raylib_cs;

namespace KidsGame.Entities;

public sealed class RescueEvent
{
    public const float InteractionPadding = 25f;

    public Vector2 Position { get; }
    public float Radius { get; } = 90f;
    public int RequiredHits { get; }
    public int CurrentHits { get; private set; }
    public bool Completed { get; private set; }
    public bool JustCompleted { get; private set; }
    public string Type { get; }
    public string Title { get; }
    public string CompleteSfx { get; }

    private float _completeTimer;
    private float _hitShake;
    private float _hitFlash;
    private float _animTime;

    // Kitten
    private float _kittenJump;
    private float _kittenJumpVel;

    // Balloon (3 balloons, individual offsets after release)
    private readonly Vector2[] _balloonAnchors =
    [
        new(34, -62),
        new(56, -50),
        new(43, -30),
    ];
    private static readonly Color[] BalloonColors =
    [
        new(255, 92, 123, 255),
        new(91, 196, 255, 255),
        new(255, 224, 83, 255),
    ];

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
        _animTime += dt;
        _hitShake = MathF.Max(0f, _hitShake - dt);
        _hitFlash = MathF.Max(0f, _hitFlash - dt * 2.2f);

        if (Completed)
        {
            _completeTimer += dt;

            if (Type == "kitten")
            {
                _kittenJump += _kittenJumpVel * dt;
                _kittenJumpVel -= 1400f * dt;
                if (_kittenJump < 0f)
                {
                    _kittenJump = 0f;
                    var bounceStrength = MathF.Max(160f - _completeTimer * 80f, 0f);
                    _kittenJumpVel = _completeTimer < 2.4f ? bounceStrength : 0f;
                }
            }
        }
    }

    public void OnHit(ParticleSystem particles, AudioManager audio)
    {
        if (Completed) return;

        CurrentHits++;
        audio.Play("sfx_rescue_hit");
        _hitShake = 0.3f;
        _hitFlash = 0.4f;

        switch (Type)
        {
            case "kitten":
                particles.EmitStars(Position + new Vector2(0, 28), 5);
                particles.EmitHearts(Position, 8);
                break;
            case "balloon":
                particles.EmitStars(Position + new Vector2(0, -30), 4);
                particles.EmitHearts(Position + new Vector2(0, -40), 6);
                break;
            case "puppy":
                particles.EmitRainbow(Position + new Vector2(0, 30), 6);
                particles.EmitHearts(Position, 8);
                break;
            default:
                particles.EmitHearts(Position, 8);
                break;
        }

        if (CurrentHits >= RequiredHits)
        {
            Completed = true;
            JustCompleted = true;
            audio.Play("sfx_rescue_complete");
            audio.Play(CompleteSfx);
            particles.EmitHearts(Position, 50);
            particles.EmitFirework(Position, 30);
            particles.EmitStars(Position, 20);
            _kittenJumpVel = 320f;
        }
    }

    public bool ConsumeJustCompleted()
    {
        if (!JustCompleted) return false;
        JustCompleted = false;
        return true;
    }

    public bool IsInInteractionRange(Vector2 point)
    {
        return Vector2.Distance(point, Position) <= Radius + InteractionPadding;
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

        if (Completed && _completeTimer < 2.5f)
        {
            var jumpY = MathF.Sin(_completeTimer * 8f) * 8f;
            var alpha = (int)(255 * MathF.Max(0f, 1f - _completeTimer / 2.5f));
            Raylib.DrawTextEx(font, "完成!", Position + new Vector2(-32, -84 + jumpY), 26, 1, new Color(230, 80, 150, alpha));
        }
    }

    // ---------------- Kitten ----------------

    private void DrawKittenEvent(AssetManager assets)
    {
        var shakeX = _hitShake > 0f ? MathF.Sin(_animTime * 80f) * _hitShake * 8f : 0f;
        var shakeY = _hitShake > 0f ? MathF.Cos(_animTime * 75f) * _hitShake * 4f : 0f;

        if (!Completed)
        {
            var log = assets.GetTexture("log");
            var rotation = -8f + shakeX * 0.6f;
            DrawDustPuffs(Position + new Vector2(shakeX, 32 + shakeY));
            Raylib.DrawTexturePro(
                log,
                new Rectangle(0, 0, log.Width, log.Height),
                new Rectangle(Position.X + shakeX, Position.Y + 22 + shakeY, 110, 34),
                new Vector2(55, 17),
                rotation,
                FlashTint());

            // peeking kitten ears under log
            var earWiggle = _hitShake > 0f ? MathF.Sin(_animTime * 40f) * 3f : 0f;
            DrawAnimal(assets, "kitten", Position + new Vector2(0 + earWiggle, 8), new Color(255, 174, 66, 255));
        }
        else
        {
            // Log spinning off into the sky
            if (_completeTimer < 2.0f)
            {
                var log = assets.GetTexture("log");
                var t = _completeTimer;
                var logX = Position.X + 30 + t * 180f;
                var logY = Position.Y + 22 - t * 280f - t * t * 90f;
                var logRot = -8f + t * 720f;
                var alpha = (int)(255 * MathF.Max(0f, 1f - t / 2.0f));
                Raylib.DrawTexturePro(
                    log,
                    new Rectangle(0, 0, log.Width, log.Height),
                    new Rectangle(logX, logY, 110, 34),
                    new Vector2(55, 17),
                    logRot,
                    new Color(255, 255, 255, alpha));
            }

            // Bouncing happy kitten
            var kittenY = -8f - _kittenJump;
            DrawAnimal(assets, "kitten", Position + new Vector2(0, kittenY), new Color(255, 174, 66, 255));

            // Joy sparkles around kitten
            if ((int)(_completeTimer * 10) % 3 == 0)
            {
                // (tiny visual marker only — particles handled by GameScene burst)
            }
        }
    }

    // ---------------- Balloon ----------------

    private void DrawBalloonEvent(AssetManager assets)
    {
        var treeShake = _hitShake > 0f ? MathF.Sin(_animTime * 70f) * _hitShake * 4f : 0f;
        var tree = assets.GetTexture("tree");
        Raylib.DrawTexturePro(
            tree,
            new Rectangle(0, 0, tree.Width, tree.Height),
            new Rectangle(Position.X + treeShake, Position.Y + 18, 110, 120),
            new Vector2(55, 92),
            0,
            FlashTint());
        DrawFallingLeaves(Position + new Vector2(treeShake, -18));

        if (!Completed)
        {
            var rise = (float)CurrentHits / RequiredHits * -10f;
            for (var i = 0; i < 3; i++)
            {
                var wiggleX = MathF.Sin(_animTime * 2.5f + i * 1.3f) * 3f;
                var wiggleY = MathF.Cos(_animTime * 3f + i) * 2f;
                var hitWiggle = _hitShake > 0f ? MathF.Sin(_animTime * 60f + i) * _hitShake * 6f : 0f;
                var anchor = _balloonAnchors[i];
                var pos = Position + anchor + new Vector2(wiggleX + hitWiggle, wiggleY + rise);
                DrawBalloon(pos, BalloonColors[i], _animTime + i);
            }
            // String tying balloons together
            Raylib.DrawLineV(
                Position + new Vector2(44, -14 + rise),
                Position + new Vector2(22, 2),
                Color.DarkBrown);
        }
        else
        {
            for (var i = 0; i < 3; i++)
            {
                var t = _completeTimer + i * 0.2f;
                var driftX = MathF.Sin(t * 1.4f + i) * 30f + (i - 1) * 8f;
                var driftY = -t * 90f - 10f - i * 8f;
                var anchor = _balloonAnchors[i];
                var pos = Position + anchor + new Vector2(driftX, driftY);
                if (pos.Y > -80f)
                {
                    DrawBalloon(pos, BalloonColors[i], t);
                    var stringSwing = MathF.Sin(t * 5f + i) * 6f;
                    Raylib.DrawLineV(pos + new Vector2(0, 14), pos + new Vector2(stringSwing, 30), Color.DarkBrown);
                }
            }
        }
    }

    private static void DrawBalloon(Vector2 center, Color color, float t)
    {
        // body
        Raylib.DrawCircleV(center, 17, color);
        // highlight
        Raylib.DrawCircleV(center + new Vector2(-6, -6), 4, new Color(255, 255, 255, 180));
        // tie
        Raylib.DrawTriangle(
            center + new Vector2(-3, 14),
            center + new Vector2(3, 14),
            center + new Vector2(0, 18),
            color);
    }

    private void DrawDustPuffs(Vector2 center)
    {
        if (_hitShake <= 0f) return;

        var intensity = Math.Clamp(_hitShake / 0.3f, 0f, 1f);
        for (var i = 0; i < 6; i++)
        {
            var angle = i * MathF.Tau / 6f + 0.25f;
            var pulse = (MathF.Sin(_animTime * 16f + i) + 1f) * 0.5f;
            var offset = new Vector2(MathF.Cos(angle) * (24f + pulse * 18f), MathF.Sin(angle) * (8f + pulse * 8f));
            var alpha = (int)(115 * intensity * (1f - pulse * 0.25f));
            Raylib.DrawCircleV(center + offset, 7f + pulse * 6f, new Color(186, 165, 132, alpha));
        }
    }

    private void DrawFallingLeaves(Vector2 origin)
    {
        if (_hitShake <= 0f) return;

        var intensity = Math.Clamp(_hitShake / 0.3f, 0f, 1f);
        for (var i = 0; i < 9; i++)
        {
            var t = (_animTime * 2.4f + i * 0.19f) % 1f;
            var x = MathF.Sin(_animTime * 5f + i) * 34f + (i - 4) * 9f;
            var y = t * 76f;
            var rot = _animTime * 140f + i * 41f;
            var alpha = (int)(210 * intensity * (1f - t * 0.3f));
            var color = i % 2 == 0 ? new Color(113, 190, 91, alpha) : new Color(255, 203, 92, alpha);
            DrawLeaf(origin + new Vector2(x, y), 7f, rot, color);
        }
    }

    private static void DrawLeaf(Vector2 center, float size, float rotationDegrees, Color color)
    {
        var radians = rotationDegrees * MathF.PI / 180f;
        var axis = new Vector2(MathF.Cos(radians), MathF.Sin(radians));
        var side = new Vector2(-axis.Y, axis.X);
        Raylib.DrawTriangle(center + axis * size, center - axis * size * 0.9f + side * size * 0.55f, center - axis * size * 0.9f - side * size * 0.55f, color);
        Raylib.DrawLineEx(center - axis * size * 0.7f, center + axis * size * 0.8f, 1.2f, new Color(75, 110, 55, (int)color.A));
    }

    // ---------------- Puppy ----------------

    private void DrawPuppyEvent(AssetManager assets)
    {
        var bridgeProgress = Completed ? 1f : (float)CurrentHits / RequiredHits;

        if (!Completed)
        {
            var puddle = assets.GetTexture("puddle");
            var ripple = MathF.Sin(_animTime * 2f) * 0.06f;
            Raylib.DrawTexturePro(
                puddle,
                new Rectangle(0, 0, puddle.Width, puddle.Height),
                new Rectangle(Position.X - 8, Position.Y + 28, 126 * (1 + ripple), 62 * (1 + ripple)),
                new Vector2(63 * (1 + ripple), 31 * (1 + ripple)),
                0,
                FlashTint());

            // expanding ripple ring
            var ringT = _animTime % 2f;
            var ringAlpha = (int)(120 * (1f - ringT / 2f));
            Raylib.DrawCircleLines(
                (int)(Position.X - 8),
                (int)(Position.Y + 30),
                (int)(20 + ringT * 35f),
                new Color(170, 220, 255, ringAlpha));
        }

        // Rainbow bridge — builds with progress
        if (bridgeProgress > 0.01f)
        {
            DrawRainbowArch(Position + new Vector2(-70, 28), 140, 40, bridgeProgress);
        }

        // Puppy
        var puppyColor = new Color(174, 114, 62, 255);
        if (Completed)
        {
            if (_completeTimer < 1.6f)
            {
                // running across the bridge in an arc
                var runT = _completeTimer / 1.6f;
                var arcX = -60f + runT * 130f;
                var arcY = 20f - MathF.Sin(runT * MathF.PI) * 38f;
                var bouncing = MathF.Abs(MathF.Sin(runT * MathF.PI * 6f)) * 4f;
                DrawAnimal(assets, "puppy", Position + new Vector2(arcX, arcY - bouncing), puppyColor);
            }
            else
            {
                // settled on the other side, tail wag
                var wag = MathF.Sin((_completeTimer - 1.6f) * 8f) * 3f;
                DrawAnimal(assets, "puppy", Position + new Vector2(70 + wag, -8), puppyColor);
            }
        }
        else
        {
            var bob = MathF.Sin(_animTime * 3f) * 2f;
            DrawAnimal(assets, "puppy", Position + new Vector2(0, -18 + bob), puppyColor);
        }
    }

    private static void DrawRainbowArch(Vector2 origin, float width, float height, float progress)
    {
        var colors = new[]
        {
            new Color(255, 102, 102, 255),
            new Color(255, 175, 102, 255),
            new Color(255, 230, 102, 255),
            new Color(140, 230, 140, 255),
            new Color(120, 195, 255, 255),
            new Color(195, 140, 255, 255),
        };

        const int segments = 32;
        var visible = (int)MathF.Ceiling(segments * progress);

        for (var c = 0; c < colors.Length; c++)
        {
            var bandHeight = height - c * 5f;
            var bandY = c * 5f;
            for (var s = 0; s < visible && s < segments; s++)
            {
                var t1 = (float)s / segments;
                var t2 = (float)(s + 1) / segments;
                var p1 = origin + new Vector2(t1 * width, bandY - MathF.Sin(t1 * MathF.PI) * bandHeight);
                var p2 = origin + new Vector2(t2 * width, bandY - MathF.Sin(t2 * MathF.PI) * bandHeight);
                Raylib.DrawLineEx(p1, p2, 6, colors[c]);
            }
        }
    }

    // ---------------- Shared ----------------

    private Color FlashTint()
    {
        if (_hitFlash <= 0f) return Color.White;
        var t = MathF.Min(_hitFlash, 1f);
        var v = (int)(255 - 50 * t);
        return new Color(255, v, v, 255);
    }

    private void DrawAnimal(AssetManager assets, string name, Vector2 position, Color fallback)
    {
        var texture = assets.FindTexture(name);
        if (texture.HasValue)
        {
            var tex = texture.Value;
            Raylib.DrawTexturePro(
                tex,
                new Rectangle(0, 0, tex.Width, tex.Height),
                new Rectangle(position.X, position.Y, 50, 50),
                new Vector2(25, 25),
                0,
                Color.White);
            return;
        }

        Raylib.DrawCircleV(position, 20, fallback);
        Raylib.DrawCircleV(position + new Vector2(-12, -14), 7, fallback);
        Raylib.DrawCircleV(position + new Vector2(12, -14), 7, fallback);
        Raylib.DrawCircleV(position + new Vector2(-6, -3), 3, Color.Black);
        Raylib.DrawCircleV(position + new Vector2(6, -3), 3, Color.Black);
    }

    private void DrawProgress(Font font)
    {
        var ratio = Math.Clamp((float)CurrentHits / RequiredHits, 0f, 1f);
        var x = Position.X - 52;
        var y = Position.Y - 96;
        Raylib.DrawRectangleRounded(new Rectangle(x, y, 104, 18), 0.55f, 8, new Color(255, 255, 255, 220));
        Raylib.DrawRectangleRounded(new Rectangle(x + 3, y + 3, 98 * ratio, 12), 0.55f, 8, new Color(255, 120, 183, 255));
        Raylib.DrawRectangleRoundedLines(new Rectangle(x, y, 104, 18), 0.55f, 8, Color.DarkPurple);
        Raylib.DrawTextEx(font, $"{CurrentHits}/{RequiredHits}", new Vector2(x + 35, y - 24), 20, 1, Color.DarkPurple);
    }
}
