using System.Numerics;
using KidsGame.Assets;
using Raylib_cs;

namespace KidsGame.Effects;

public sealed class ParticleSystem
{
    private const int MaxParticles = 700;
    private readonly Particle[] _particles = new Particle[MaxParticles];
    private readonly Random _random = new();
    private int _writeIndex;
    private int _rainbowIndex;

    private static readonly Color[] RainbowColors =
    [
        new(255, 93, 93, 255),
        new(255, 178, 76, 255),
        new(255, 237, 84, 255),
        new(96, 219, 110, 255),
        new(82, 185, 255, 255),
        new(172, 126, 255, 255)
    ];

    private static readonly Color[] ConfettiColors =
    [
        new(255, 119, 168, 255),
        new(255, 200, 87, 255),
        new(120, 220, 255, 255),
        new(186, 138, 255, 255),
        new(140, 230, 140, 255),
    ];

    public void EmitRainbow(Vector2 position, int count) => Emit(position, ParticleType.Rainbow, count);
    public void EmitHearts(Vector2 position, int count) => Emit(position, ParticleType.Heart, count);
    public void EmitStars(Vector2 position, int count) => Emit(position, ParticleType.Star, count);
    public void EmitFirework(Vector2 position, int count) => Emit(position, ParticleType.Firework, count);
    public void EmitDust(Vector2 position, int count) => Emit(position, ParticleType.Dust, count);
    public void EmitSparkle(Vector2 position, int count) => Emit(position, ParticleType.Sparkle, count);
    public void EmitConfetti(Vector2 position, int count) => Emit(position, ParticleType.Confetti, count);

    public void Emit(Vector2 position, ParticleType type, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var angle = RandomFloat(0, MathF.Tau);
            var speed = type switch
            {
                ParticleType.Rainbow => RandomFloat(18f, 65f),
                ParticleType.Heart => RandomFloat(40f, 110f),
                ParticleType.Star => RandomFloat(50f, 150f),
                ParticleType.Dust => RandomFloat(20f, 60f),
                ParticleType.Sparkle => RandomFloat(8f, 26f),
                ParticleType.Confetti => RandomFloat(140f, 320f),
                _ => RandomFloat(80f, 220f)
            };

            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;

            // Per-type velocity tweaks for character feel
            switch (type)
            {
                case ParticleType.Heart:
                    velocity.Y -= 40f; // hearts drift up
                    break;
                case ParticleType.Dust:
                    velocity.Y = MathF.Abs(velocity.Y) * -0.25f - 8f; // low arcs
                    break;
                case ParticleType.Confetti:
                    velocity.Y = -MathF.Abs(velocity.Y) - 80f; // pop up then fall
                    break;
            }

            var life = type switch
            {
                ParticleType.Rainbow => 0.7f,
                ParticleType.Heart => 1.05f,
                ParticleType.Star => 0.7f,
                ParticleType.Dust => 0.45f,
                ParticleType.Sparkle => 1.2f,
                ParticleType.Confetti => 1.6f,
                _ => 0.95f
            };

            var size = type switch
            {
                ParticleType.Firework => RandomFloat(8f, 14f),
                ParticleType.Dust => RandomFloat(8f, 14f),
                ParticleType.Sparkle => RandomFloat(4f, 8f),
                ParticleType.Confetti => RandomFloat(8f, 14f),
                _ => RandomFloat(10f, 18f),
            };

            var gravity = type switch
            {
                ParticleType.Rainbow => 12f,
                ParticleType.Heart => -10f,
                ParticleType.Star => 22f,
                ParticleType.Dust => -2f,
                ParticleType.Sparkle => 0f,
                ParticleType.Confetti => 320f,
                _ => 22f,
            };

            var drag = type switch
            {
                ParticleType.Rainbow => 0.94f,
                ParticleType.Heart => 0.965f,
                ParticleType.Star => 0.94f,
                ParticleType.Dust => 0.86f,
                ParticleType.Sparkle => 0.985f,
                ParticleType.Confetti => 0.985f,
                _ => 0.95f,
            };

            _particles[_writeIndex] = new Particle
            {
                Alive = true,
                Type = type,
                Position = position + RandomVector(8f),
                Velocity = velocity,
                Life = life,
                MaxLife = life,
                Size = size,
                Rotation = RandomFloat(0, 360f),
                Spin = type == ParticleType.Confetti ? RandomFloat(-720f, 720f) : 120f,
                Color = ColorFor(type),
                Gravity = gravity,
                Drag = drag,
            };

            _writeIndex = (_writeIndex + 1) % MaxParticles;
        }
    }

    public void Update(float dt)
    {
        for (var i = 0; i < _particles.Length; i++)
        {
            if (!_particles[i].Alive) continue;

            _particles[i].Life -= dt;
            if (_particles[i].Life <= 0f)
            {
                _particles[i].Alive = false;
                continue;
            }

            _particles[i].Position += _particles[i].Velocity * dt;
            // Frame-rate independent drag
            var dragThisFrame = MathF.Pow(_particles[i].Drag, dt * 60f);
            _particles[i].Velocity *= dragThisFrame;
            _particles[i].Velocity.Y += _particles[i].Gravity * dt;
            _particles[i].Rotation += _particles[i].Spin * dt;
        }
    }

    public void Draw(AssetManager assets)
    {
        foreach (var p in _particles)
        {
            if (!p.Alive) continue;

            var lifeT = Math.Clamp(p.Life / p.MaxLife, 0f, 1f);
            // Smooth fade-out for the last 30% of life
            var alphaT = lifeT < 0.3f ? lifeT / 0.3f : 1f;
            // Pop-in scale for hearts/firework
            var sizeT = p.Type == ParticleType.Heart || p.Type == ParticleType.Firework
                ? 0.6f + Math.Min(1f, (p.MaxLife - p.Life) * 6f) * 0.4f
                : 1f;

            var tint = new Color(p.Color.R, p.Color.G, p.Color.B, (byte)(p.Color.A * alphaT));

            switch (p.Type)
            {
                case ParticleType.Sparkle:
                    {
                        var s = p.Size * sizeT;
                        Raylib.DrawCircleV(p.Position, s * 0.55f, tint);
                        Raylib.DrawLineEx(p.Position + new Vector2(-s, 0), p.Position + new Vector2(s, 0), 1.2f, tint);
                        Raylib.DrawLineEx(p.Position + new Vector2(0, -s), p.Position + new Vector2(0, s), 1.2f, tint);
                        continue;
                    }
                case ParticleType.Dust:
                    {
                        var s = p.Size * sizeT;
                        Raylib.DrawCircleV(p.Position, s * 0.7f, tint);
                        continue;
                    }
                case ParticleType.Confetti:
                    {
                        var s = p.Size * sizeT;
                        Raylib.DrawRectanglePro(
                            new Rectangle(p.Position.X, p.Position.Y, s * 1.2f, s * 0.5f),
                            new Vector2(s * 0.6f, s * 0.25f),
                            p.Rotation,
                            tint);
                        continue;
                    }
            }

            var textureName = p.Type switch
            {
                ParticleType.Heart => "heart_particle",
                ParticleType.Star => "star_particle",
                ParticleType.Firework => "firework",
                _ => "rainbow_particle"
            };

            var texture = assets.GetTexture(textureName);
            var source = new Rectangle(0, 0, texture.Width, texture.Height);
            var size = p.Size * sizeT;
            var dest = new Rectangle(p.Position.X, p.Position.Y, size, size);
            Raylib.DrawTexturePro(texture, source, dest, new Vector2(size / 2f), p.Rotation, tint);
        }
    }

    private Color ColorFor(ParticleType type)
    {
        return type switch
        {
            ParticleType.Rainbow => RainbowColors[_rainbowIndex++ % RainbowColors.Length],
            ParticleType.Heart => new Color(255, 111, 176, 255),
            ParticleType.Star => new Color(255, 229, 89, 255),
            ParticleType.Dust => new Color(220, 200, 165, 200),
            ParticleType.Sparkle => RainbowColors[_random.Next(RainbowColors.Length)],
            ParticleType.Confetti => ConfettiColors[_random.Next(ConfettiColors.Length)],
            _ => RainbowColors[_random.Next(RainbowColors.Length)]
        };
    }

    private float RandomFloat(float min, float max) => min + (float)_random.NextDouble() * (max - min);

    private Vector2 RandomVector(float radius)
    {
        var angle = RandomFloat(0, MathF.Tau);
        var distance = RandomFloat(0, radius);
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }
}
