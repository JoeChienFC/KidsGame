using System.Numerics;
using KidsGame.Assets;
using KidsGame.Effects;
using Raylib_cs;

namespace KidsGame.Entities;

public sealed class Crystal
{
    private static readonly Random Random = new();

    public Vector2 Position { get; }
    public bool Collected { get; private set; }

    private float _spin;
    private float _twinkleTimer;

    public Crystal(Vector2 position)
    {
        Position = position;
        _twinkleTimer = (float)(new Random(position.GetHashCode()).NextDouble() * 1.2);
    }

    public void Update(float dt, ParticleSystem particles)
    {
        if (Collected) return;
        _spin += dt * 7f;
        _twinkleTimer -= dt;
        if (_twinkleTimer <= 0f)
        {
            _twinkleTimer = 1.4f + Random.NextSingle() * 0.8f;
            particles.EmitStars(Position + new Vector2(0, -4), 1);
        }
    }

    public bool TryCollect(Vector2 playerPosition, ParticleSystem particles)
    {
        if (Collected || Vector2.Distance(playerPosition, Position) > 36f)
        {
            return false;
        }

        Collected = true;
        particles.EmitStars(Position, 12);
        particles.EmitFirework(Position, 6);
        return true;
    }

    public void Draw(AssetManager assets)
    {
        if (Collected)
        {
            return;
        }

        var pulse = (MathF.Sin(_spin) + 1f) * 0.5f;
        var bob = MathF.Sin(_spin * 0.6f) * 3f;
        var center = Position + new Vector2(0, bob);

        // Glow halo
        var glowAlpha = (int)(70 + 50 * pulse);
        Raylib.DrawCircleV(center, 28f + pulse * 4f, new Color(170, 240, 255, glowAlpha / 2));
        Raylib.DrawCircleV(center, 18f + pulse * 3f, new Color(220, 250, 255, glowAlpha));

        // Light rays (rotating)
        var rayAlpha = (int)(110 + 80 * pulse);
        var rayColor = new Color(255, 255, 255, rayAlpha);
        for (var i = 0; i < 4; i++)
        {
            var ang = _spin * 0.6f + i * MathF.PI / 2f;
            var dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
            var len = 26f + pulse * 6f;
            Raylib.DrawLineEx(center - dir * 6f, center + dir * len, 2.4f, rayColor);
        }

        // Crystal body
        var bodyColor = new Color(125, 232, 255, 255);
        Raylib.DrawPoly(center, 4, 15f + pulse * 3f, 45f + _spin * 24f, bodyColor);
        Raylib.DrawPolyLines(center, 4, 17f + pulse * 3f, 45f + _spin * 24f, Color.White);
        Raylib.DrawCircleV(center + new Vector2(-4, -4), 3f, Color.White);
    }
}
