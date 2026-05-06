using System.Numerics;
using Raylib_cs;

namespace KidsGame.Effects;

public enum ParticleType
{
    Rainbow,
    Heart,
    Star,
    Firework
}

public struct Particle
{
    public bool Alive;
    public ParticleType Type;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
    public float MaxLife;
    public float Size;
    public float Rotation;
    public Color Color;
}
