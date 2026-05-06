using System.Numerics;
using KidsGame.Assets;
using KidsGame.Effects;
using KidsGame.Util;
using Raylib_cs;

namespace KidsGame.Entities;

public sealed class Unicorn
{
    public Vector2 Position { get; private set; }
    public Vector2 LastDirection { get; private set; } = new(0, 1);
    public Rectangle Bounds => new(Position.X - 26, Position.Y - 26, 52, 52);

    private const float Speed = 180f;
    private float _particleTimer;
    private float _walkTime;
    private float _idleTime;
    private bool _isMoving;

    public Unicorn(Vector2 startPosition)
    {
        Position = startPosition;
    }

    public void Update(float dt, ParticleSystem particles, IReadOnlyList<Rectangle> softObstacles)
    {
        var input = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.Up))
        {
            input.Y -= 1;
        }

        if (Raylib.IsKeyDown(KeyboardKey.Down))
        {
            input.Y += 1;
        }

        if (Raylib.IsKeyDown(KeyboardKey.Left))
        {
            input.X -= 1;
        }

        if (Raylib.IsKeyDown(KeyboardKey.Right))
        {
            input.X += 1;
        }

        if (input.LengthSquared() <= 0f)
        {
            _isMoving = false;
            _idleTime += dt;
            if (_idleTime > 1.2f)
            {
                _idleTime = 0f;
                particles.EmitStars(Position + new Vector2(0, -28), 2);
            }
            return;
        }

        _isMoving = true;
        _idleTime = 0f;
        input = Vector2.Normalize(input);
        LastDirection = input;
        _walkTime += dt;

        MoveWithSoftCollision(new Vector2(input.X * Speed * dt, 0), softObstacles);
        MoveWithSoftCollision(new Vector2(0, input.Y * Speed * dt), softObstacles);
        Position = Mathf.Clamp(Position, 36, 44, Game.ScreenWidth - 36, Game.ScreenHeight - 38);

        _particleTimer += dt;
        while (_particleTimer >= 0.06f)
        {
            var trailPos = Position + new Vector2(-LastDirection.X * 22f, -LastDirection.Y * 22f);
            particles.EmitRainbow(trailPos, 3);
            _particleTimer -= 0.06f;
        }
    }

    public bool IsMoving => _isMoving;
    public float WalkTime => _walkTime;

    public void Draw(AssetManager assets)
    {
        var bob = _isMoving ? MathF.Sin(_walkTime * 14f) * 4.5f : MathF.Sin((float)Raylib.GetTime() * 2.2f) * 1.6f;
        var squash = _isMoving ? 1f + MathF.Sin(_walkTime * 14f) * 0.06f : 1f;
        var shadowAlpha = 110 - (int)Math.Abs(bob) * 4;
        Raylib.DrawEllipse((int)Position.X, (int)(Position.Y + 36), 36, 9, new Color(0, 0, 0, shadowAlpha));

        var texture = assets.FindTexture("unicorn");
        if (texture.HasValue)
        {
            var frame = DirectionIndex(LastDirection);
            var unicornCenter = Position + new Vector2(0, 7 - bob);
            DrawTextureCharacterScaled(texture.Value, frame, 98, unicornCenter, Color.White, squash);

            var princess = assets.FindTexture("princess_overlay");
            if (princess.HasValue)
            {
                DrawTextureCharacterScaled(princess.Value, frame, 56, Position + PrincessOffset(frame) + new Vector2(0, -bob), Color.White, squash);
            }
        }
        else
        {
            DrawFallbackUnicorn(bob);
        }
    }

    private void MoveWithSoftCollision(Vector2 delta, IReadOnlyList<Rectangle> softObstacles)
    {
        if (delta.LengthSquared() <= 0f)
        {
            return;
        }

        var next = Position + delta;
        var nextBounds = new Rectangle(next.X - 26, next.Y - 26, 52, 52);
        foreach (var obstacle in softObstacles)
        {
            if (Raylib.CheckCollisionRecs(nextBounds, obstacle))
            {
                return;
            }
        }

        Position = next;
    }

    private static void DrawTextureCharacterScaled(Texture2D texture, int frame, float size, Vector2 position, Color tint, float squash)
    {
        var frameWidth = texture.Width / 4;
        var frameHeight = texture.Height;
        var hasSheet = frameWidth > 0;
        var source = hasSheet
            ? new Rectangle(frame * frameWidth, 0, frameWidth, frameHeight)
            : new Rectangle(0, 0, texture.Width, texture.Height);
        var w = size * squash;
        var h = size * (2f - squash);
        var dest = new Rectangle(position.X, position.Y, w, h);
        Raylib.DrawTexturePro(texture, source, dest, new Vector2(w / 2f, h / 2f), 0, tint);
    }

    private void DrawFallbackUnicorn(float bob)
    {
        var body = new Rectangle(Position.X - 30, Position.Y - 16 + bob, 60, 32);
        Raylib.DrawEllipse((int)Position.X, (int)(Position.Y + bob), 32, 20, new Color(246, 246, 255, 255));
        Raylib.DrawEllipse((int)(Position.X + 22 * LastDirection.X), (int)(Position.Y - 14 + 8 * LastDirection.Y + bob), 17, 17, new Color(255, 247, 252, 255));
        Raylib.DrawRectangleRounded(body, 0.8f, 10, new Color(236, 236, 255, 255));
        Raylib.DrawCircleV(Position + new Vector2(10 * LastDirection.X, -22 + bob), 8, new Color(255, 205, 235, 255));
        Raylib.DrawTriangle(
            Position + new Vector2(20 * LastDirection.X, -32 + bob),
            Position + new Vector2(12 * LastDirection.X - 5, -18 + bob),
            Position + new Vector2(12 * LastDirection.X + 5, -18 + bob),
            new Color(255, 224, 99, 255));
        Raylib.DrawCircleV(Position + new Vector2(-25, -15 + bob), 8, new Color(255, 111, 176, 255));
        Raylib.DrawCircleV(Position + new Vector2(-31, -7 + bob), 7, new Color(255, 202, 84, 255));
        Raylib.DrawCircleV(Position + new Vector2(-26, 2 + bob), 7, new Color(108, 204, 255, 255));
        Raylib.DrawRectangle((int)Position.X - 14, (int)Position.Y - 43, 28, 10, new Color(255, 171, 215, 255));
        Raylib.DrawTriangle(new Vector2(Position.X - 11, Position.Y - 43), new Vector2(Position.X - 5, Position.Y - 57), new Vector2(Position.X + 1, Position.Y - 43), new Color(255, 226, 82, 255));
        Raylib.DrawTriangle(new Vector2(Position.X - 1, Position.Y - 43), new Vector2(Position.X + 5, Position.Y - 59), new Vector2(Position.X + 11, Position.Y - 43), new Color(255, 226, 82, 255));
    }

    private static int DirectionIndex(Vector2 direction)
    {
        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
        {
            return direction.X < 0 ? 2 : 3;
        }

        return direction.Y < 0 ? 0 : 1;
    }

    private static Vector2 PrincessOffset(int frame)
    {
        return frame switch
        {
            0 => new Vector2(0, -39),
            1 => new Vector2(0, -37),
            2 => new Vector2(-5, -39),
            _ => new Vector2(5, -39)
        };
    }
}
