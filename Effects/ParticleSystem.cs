using System.Numerics;
using KidsGame.Assets;
using Raylib_cs;

namespace KidsGame.Effects;

public sealed class ParticleSystem
{
    private const int MaxParticles = 500;
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

    public void EmitRainbow(Vector2 position, int count) => Emit(position, ParticleType.Rainbow, count);
    public void EmitHearts(Vector2 position, int count) => Emit(position, ParticleType.Heart, count);
    public void EmitStars(Vector2 position, int count) => Emit(position, ParticleType.Star, count);
    public void EmitFirework(Vector2 position, int count) => Emit(position, ParticleType.Firework, count);

    public void Emit(Vector2 position, ParticleType type, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var angle = RandomFloat(0, MathF.Tau);
            var speed = type switch
            {
                ParticleType.Rainbow => RandomFloat(18f, 55f),
                ParticleType.Heart => RandomFloat(35f, 95f),
                ParticleType.Star => RandomFloat(50f, 130f),
                _ => RandomFloat(80f, 190f)
            };

            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            var life = type switch
            {
                ParticleType.Rainbow => 0.75f,
                ParticleType.Heart => 0.95f,
                ParticleType.Star => 0.7f,
                _ => 0.85f
            };

            _particles[_writeIndex] = new Particle
            {
                Alive = true,
                Type = type,
                Position = position + RandomVector(8f),
                Velocity = velocity,
                Life = life,
                MaxLife = life,
                Size = type == ParticleType.Firework ? RandomFloat(8f, 14f) : RandomFloat(10f, 18f),
                Rotation = RandomFloat(0, 360f),
                Color = ColorFor(type)
            };

            _writeIndex = (_writeIndex + 1) % MaxParticles;
        }
    }

    public void Update(float dt)
    {
        for (var i = 0; i < _particles.Length; i++)
        {
            if (!_particles[i].Alive)
            {
                continue;
            }

            _particles[i].Life -= dt;
            if (_particles[i].Life <= 0f)
            {
                _particles[i].Alive = false;
                continue;
            }

            _particles[i].Position += _particles[i].Velocity * dt;
            _particles[i].Velocity *= 0.96f;
            _particles[i].Velocity.Y += 18f * dt;
            _particles[i].Rotation += 120f * dt;
        }
    }

    public void Draw(AssetManager assets)
    {
        foreach (var particle in _particles)
        {
            if (!particle.Alive)
            {
                continue;
            }

            var alpha = Math.Clamp(particle.Life / particle.MaxLife, 0f, 1f);
            var tint = new Color(particle.Color.R, particle.Color.G, particle.Color.B, (byte)(255 * alpha));
            var textureName = particle.Type switch
            {
                ParticleType.Heart => "heart_particle",
                ParticleType.Star => "star_particle",
                ParticleType.Firework => "firework",
                _ => "rainbow_particle"
            };

            var texture = assets.GetTexture(textureName);
            var source = new Rectangle(0, 0, texture.Width, texture.Height);
            var dest = new Rectangle(particle.Position.X, particle.Position.Y, particle.Size, particle.Size);
            Raylib.DrawTexturePro(texture, source, dest, new Vector2(particle.Size / 2f), particle.Rotation, tint);
        }
    }

    private Color ColorFor(ParticleType type)
    {
        return type switch
        {
            ParticleType.Rainbow => RainbowColors[_rainbowIndex++ % RainbowColors.Length],
            ParticleType.Heart => new Color(255, 111, 176, 255),
            ParticleType.Star => new Color(255, 229, 89, 255),
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
