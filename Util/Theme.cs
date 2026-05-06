using System.Numerics;
using Raylib_cs;

namespace KidsGame.Util;

public sealed class Theme
{
    public string Name { get; }
    public Color BackgroundColor { get; }
    public Color GrassTint { get; }
    public Color OverlayTint { get; }    // applied as full-screen overlay (low alpha)
    public Color FlowerBoost { get; }    // tint added to flowers
    public bool ShowStars { get; }
    public Color StarColor { get; }

    private Theme(string name, Color bg, Color grass, Color overlay, Color flowerBoost, bool stars, Color starColor)
    {
        Name = name;
        BackgroundColor = bg;
        GrassTint = grass;
        OverlayTint = overlay;
        FlowerBoost = flowerBoost;
        ShowStars = stars;
        StarColor = starColor;
    }

    public static readonly Theme Meadow = new(
        name: "彩虹草原",
        bg: new Color(134, 207, 126, 255),
        grass: new Color(255, 255, 255, 255),
        overlay: new Color(0, 0, 0, 0),
        flowerBoost: new Color(255, 255, 255, 255),
        stars: false,
        starColor: Color.White);

    public static readonly Theme Sunset = new(
        name: "夕陽小鎮",
        bg: new Color(255, 178, 145, 255),
        grass: new Color(255, 218, 200, 255),
        overlay: new Color(255, 130, 90, 50),
        flowerBoost: new Color(255, 220, 200, 255),
        stars: false,
        starColor: Color.White);

    public static readonly Theme Night = new(
        name: "星空之夜",
        bg: new Color(46, 50, 110, 255),
        grass: new Color(150, 170, 220, 255),
        overlay: new Color(30, 30, 90, 90),
        flowerBoost: new Color(220, 220, 255, 255),
        stars: true,
        starColor: new Color(255, 250, 200, 255));

    private static readonly Theme[] Cycle = [Meadow, Sunset, Night];

    public static Theme ForRound(int round) => Cycle[(round - 1) % Cycle.Length];

    public static int Count => Cycle.Length;
}
