using System.Numerics;
using Raylib_cs;

namespace KidsGame.Effects;

public sealed class FloatingTextSystem
{
    private struct Item
    {
        public bool Alive;
        public string Text;
        public Vector2 Position;
        public float Life;
        public float MaxLife;
        public float Size;
        public Color Color;
    }

    private const int MaxItems = 32;
    private readonly Item[] _items = new Item[MaxItems];
    private int _writeIndex;

    public void Spawn(string text, Vector2 position, Color color, float size = 28f, float life = 1.0f)
    {
        _items[_writeIndex] = new Item
        {
            Alive = true,
            Text = text,
            Position = position,
            Life = life,
            MaxLife = life,
            Size = size,
            Color = color,
        };
        _writeIndex = (_writeIndex + 1) % MaxItems;
    }

    public void Update(float dt)
    {
        for (var i = 0; i < _items.Length; i++)
        {
            if (!_items[i].Alive) continue;
            _items[i].Life -= dt;
            if (_items[i].Life <= 0f)
            {
                _items[i].Alive = false;
                continue;
            }
            _items[i].Position.Y -= 60f * dt;
        }
    }

    public void Draw(Font font)
    {
        foreach (var item in _items)
        {
            if (!item.Alive) continue;
            var t = item.Life / item.MaxLife;
            var alpha = (int)(255 * Math.Clamp(t * 1.4f, 0f, 1f));
            var scale = 1f + (1f - t) * 0.25f;
            var size = item.Size * scale;
            var measured = Raylib.MeasureTextEx(font, item.Text, size, 1);
            var origin = new Vector2(item.Position.X - measured.X / 2f, item.Position.Y - measured.Y / 2f);
            var shadow = new Color(0, 0, 0, alpha / 3);
            Raylib.DrawTextEx(font, item.Text, origin + new Vector2(2, 2), size, 1, shadow);
            var c = new Color((int)item.Color.R, (int)item.Color.G, (int)item.Color.B, alpha);
            Raylib.DrawTextEx(font, item.Text, origin, size, 1, c);
        }
    }
}
