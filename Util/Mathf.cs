using System.Numerics;
using Raylib_cs;

namespace KidsGame.Util;

public static class Mathf
{
    public static Vector2 Clamp(Vector2 value, float minX, float minY, float maxX, float maxY)
    {
        return new Vector2(Math.Clamp(value.X, minX, maxX), Math.Clamp(value.Y, minY, maxY));
    }

    public static float SmoothPulse(float time, float speed = 1f)
    {
        return (MathF.Sin(time * speed) + 1f) * 0.5f;
    }

    public static bool IsTextureReady(Texture2D texture) => texture.Id != 0;
}
