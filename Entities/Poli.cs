using System.Numerics;
using KidsGame.Assets;
using KidsGame.Audio;
using KidsGame.Effects;
using Raylib_cs;

namespace KidsGame.Entities;

public sealed class Poli
{
    public Vector2 Position { get; private set; }
    public Vector2 Facing => _facing;

    private Vector2 _facing = new(0, 1);
    private float _bounceTimer;
    private float _sirenTimer;
    private float _moveTime;
    private bool _isRolling;

    public Poli(Vector2 startPosition)
    {
        Position = startPosition;
    }

    public void Update(float dt, Unicorn target, IReadOnlyList<RescueEvent> events, ParticleSystem particles, AudioManager audio)
    {
        var desired = target.Position - target.LastDirection * 74f;
        var toTarget = desired - Position;
        var distance = toTarget.Length();
        _isRolling = distance > 4f;
        if (_isRolling)
        {
            Position = Vector2.Lerp(Position, desired, Math.Clamp(5f * dt, 0f, 1f));
            _facing = Vector2.Normalize(toTarget);
            _moveTime += dt;
        }

        _bounceTimer = Math.Max(0f, _bounceTimer - dt);
        _sirenTimer = Math.Max(0f, _sirenTimer - dt);

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            var nearby = events.FirstOrDefault(e => !e.Completed && e.IsInInteractionRange(Position));
            if (nearby is not null)
            {
                nearby.OnHit(particles, audio);
                _bounceTimer = 0.24f;
                _sirenTimer = 0.25f;
            }
            else
            {
                audio.Play("sfx_button");
                particles.EmitFirework(Position, 5);
                _bounceTimer = 0.3f;
                _sirenTimer = 0.3f;
            }
        }
    }

    public void Draw(AssetManager assets)
    {
        var bounce = _bounceTimer > 0f ? -7f * MathF.Sin((_bounceTimer / 0.3f) * MathF.PI) : 0f;
        var roll = _isRolling ? MathF.Sin(_moveTime * 18f) * 2.2f : 0f;
        var squash = _isRolling ? 1f + MathF.Sin(_moveTime * 18f) * 0.04f : 1f;
        var drawPosition = Position + new Vector2(0, bounce + roll);

        Raylib.DrawEllipse((int)Position.X, (int)(Position.Y + 30), 30, 7, new Color(0, 0, 0, 90));

        var sirenGlow = _sirenTimer > 0f ? 120 + (int)(80 * MathF.Sin((float)Raylib.GetTime() * 22f)) : 0;
        if (sirenGlow > 0)
        {
            Raylib.DrawCircleV(drawPosition + new Vector2(0, -2), 56, new Color(255, 80, 100, sirenGlow / 4));
            Raylib.DrawCircleV(drawPosition + new Vector2(0, -2), 40, new Color(80, 180, 255, sirenGlow / 4));
        }

        var texture = assets.FindTexture("poli");
        if (texture.HasValue)
        {
            var frameWidth = texture.Value.Width / 4;
            var frame = DirectionIndex();
            var source = new Rectangle(frame * frameWidth, 0, frameWidth, texture.Value.Height);
            var w = 78f * squash;
            var h = 78f * (2f - squash);
            var dest = new Rectangle(drawPosition.X, drawPosition.Y, w, h);
            Raylib.DrawTexturePro(texture.Value, source, dest, new Vector2(w / 2f, h / 2f), 0, Color.White);
        }
        else
        {
            DrawFallbackPoli(drawPosition);
        }
    }

    private void DrawFallbackPoli(Vector2 drawPosition)
    {
        var rotation = AngleFromFacing();
        var body = new Rectangle(drawPosition.X, drawPosition.Y, 52, 38);
        Raylib.DrawRectanglePro(body, new Vector2(26, 19), rotation, new Color(52, 131, 230, 255));
        Raylib.DrawRectanglePro(new Rectangle(drawPosition.X, drawPosition.Y - 2, 30, 20), new Vector2(15, 10), rotation, new Color(174, 225, 255, 255));
        Raylib.DrawCircleV(drawPosition + Rotate(new Vector2(-21, -18), rotation), 6, Color.Black);
        Raylib.DrawCircleV(drawPosition + Rotate(new Vector2(21, -18), rotation), 6, Color.Black);
        Raylib.DrawCircleV(drawPosition + Rotate(new Vector2(-21, 18), rotation), 6, Color.Black);
        Raylib.DrawCircleV(drawPosition + Rotate(new Vector2(21, 18), rotation), 6, Color.Black);
        var flash = _sirenTimer > 0f && MathF.Sin((float)Raylib.GetTime() * 36f) > 0f ? Color.Red : new Color(68, 214, 255, 255);
        Raylib.DrawCircleV(drawPosition + Rotate(new Vector2(0, -19), rotation), 6, flash);
        Raylib.DrawRectanglePro(new Rectangle(drawPosition.X, drawPosition.Y + 23, 22, 6), new Vector2(11, 3), rotation, Color.White);
    }

    private float AngleFromFacing()
    {
        if (Math.Abs(_facing.X) > Math.Abs(_facing.Y))
        {
            return _facing.X > 0 ? 90f : -90f;
        }

        return _facing.Y > 0 ? 180f : 0f;
    }

    private int DirectionIndex()
    {
        if (Math.Abs(_facing.X) > Math.Abs(_facing.Y))
        {
            return _facing.X < 0 ? 2 : 3;
        }

        return _facing.Y < 0 ? 1 : 0;
    }

    private static Vector2 Rotate(Vector2 point, float degrees)
    {
        var radians = degrees * MathF.PI / 180f;
        return new Vector2(
            point.X * MathF.Cos(radians) - point.Y * MathF.Sin(radians),
            point.X * MathF.Sin(radians) + point.Y * MathF.Cos(radians));
    }
}
