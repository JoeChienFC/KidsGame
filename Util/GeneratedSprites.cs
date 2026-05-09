using KidsGame.Assets;
using Raylib_cs;

namespace KidsGame.Util;

public enum RescueSprite
{
    Princess,
    PrinceWindow,
    Castle,
    Cottage,
    CottageDamaged,
    CottageBroken,
    Witch,
    Girl,
    Apple,
    Carrot,
    DinoCaged,
    DinoCracked,
    Poli,
    Amber,
    Helly,
    Roy,
    CastleRoyal,
    CastleGate,
    RoyalFountain,
    GardenArch,
    ForestBroadleaf,
    ForestPine,
    ForestBush,
    ForestMushroom,
    ForestLog,
    ForestRocks,
    ForestFlowers,
    ForestPath,
    RobotPoli,
    RobotAmber,
    RobotHelly,
    RobotRoy,
    PrincessBlue,
    PrincessYellow,
    PrincessPurple,
    PrincessCaged,
    PrincessFree,
    PrincessFriends,
    PrincessMermaid,
    PrincessIce,
    PrincessSnow,
    PrincessCinderella,
    PrincessRapunzel,
    PrincessRose,
    CrestedDinoCaged,
    CrestedDinoCracked,
    CrestedDinoFree,
    StegoDinoCaged,
    StegoDinoCracked,
    StegoDinoFree,
    BrachioDinoCaged,
    BrachioDinoCracked,
    BrachioDinoFree,
    PteroDinoCaged,
    PteroDinoCracked,
    PteroDinoFree
}

public enum FinalSprite
{
    DinoFree,
    CageOpen,
    WitchDefeated,
    GirlFree
}

public static class GeneratedSprites
{
    public static bool TryDraw(AssetManager assets, RescueSprite sprite, Rectangle box, Color tint, float rotation = 0f)
    {
        var textureName = sprite switch
        {
            RescueSprite.Princess => "sprite_princess",
            RescueSprite.PrinceWindow => "sprite_prince_window",
            RescueSprite.Castle => "sprite_castle",
            RescueSprite.Cottage => "sprite_cottage",
            RescueSprite.CottageDamaged => "sprite_cottage_damaged",
            RescueSprite.CottageBroken => "sprite_cottage_broken",
            RescueSprite.Witch => "sprite_witch",
            RescueSprite.Girl => "sprite_girl",
            RescueSprite.Apple => "sprite_apple",
            RescueSprite.Carrot => "sprite_carrot",
            RescueSprite.DinoCaged => "sprite_dino_caged",
            RescueSprite.DinoCracked => "sprite_dino_cracked",
            RescueSprite.Poli => "sprite_poli",
            RescueSprite.Amber => "sprite_amber",
            RescueSprite.Helly => "sprite_helly",
            RescueSprite.Roy => "sprite_roy",
            RescueSprite.CastleRoyal => "sprite_castle_royal",
            RescueSprite.CastleGate => "sprite_castle_gate",
            RescueSprite.RoyalFountain => "sprite_royal_fountain",
            RescueSprite.GardenArch => "sprite_garden_arch",
            RescueSprite.ForestBroadleaf => "sprite_forest_broadleaf",
            RescueSprite.ForestPine => "sprite_forest_pine",
            RescueSprite.ForestBush => "sprite_forest_bush",
            RescueSprite.ForestMushroom => "sprite_forest_mushroom",
            RescueSprite.ForestLog => "sprite_forest_log",
            RescueSprite.ForestRocks => "sprite_forest_rocks",
            RescueSprite.ForestFlowers => "sprite_forest_flowers",
            RescueSprite.ForestPath => "sprite_forest_path",
            RescueSprite.RobotPoli => "sprite_robot_poli",
            RescueSprite.RobotAmber => "sprite_robot_amber",
            RescueSprite.RobotHelly => "sprite_robot_helly",
            RescueSprite.RobotRoy => "sprite_robot_roy",
            RescueSprite.PrincessBlue => "sprite_princess_blue",
            RescueSprite.PrincessYellow => "sprite_princess_yellow",
            RescueSprite.PrincessPurple => "sprite_princess_purple",
            RescueSprite.PrincessCaged => "sprite_princess_caged",
            RescueSprite.PrincessFree => "sprite_princess_free",
            RescueSprite.PrincessFriends => "sprite_princess_friends",
            RescueSprite.PrincessMermaid => "sprite_princess_mermaid",
            RescueSprite.PrincessIce => "sprite_princess_ice",
            RescueSprite.PrincessSnow => "sprite_princess_snow",
            RescueSprite.PrincessCinderella => "sprite_princess_cinderella",
            RescueSprite.PrincessRapunzel => "sprite_princess_rapunzel",
            RescueSprite.PrincessRose => "sprite_princess_rose",
            RescueSprite.CrestedDinoCaged => "sprite_crested_dino_caged",
            RescueSprite.CrestedDinoCracked => "sprite_crested_dino_cracked",
            RescueSprite.CrestedDinoFree => "sprite_crested_dino_free",
            RescueSprite.StegoDinoCaged => "sprite_stego_dino_caged",
            RescueSprite.StegoDinoCracked => "sprite_stego_dino_cracked",
            RescueSprite.StegoDinoFree => "sprite_stego_dino_free",
            RescueSprite.BrachioDinoCaged => "sprite_brachio_dino_caged",
            RescueSprite.BrachioDinoCracked => "sprite_brachio_dino_cracked",
            RescueSprite.BrachioDinoFree => "sprite_brachio_dino_free",
            RescueSprite.PteroDinoCaged => "sprite_ptero_dino_caged",
            RescueSprite.PteroDinoCracked => "sprite_ptero_dino_cracked",
            RescueSprite.PteroDinoFree => "sprite_ptero_dino_free",
            _ => null,
        };

        return TryDrawTexture(assets, textureName, box, tint, rotation);
    }

    public static bool TryDrawFinal(AssetManager assets, FinalSprite sprite, Rectangle box, Color tint, float rotation = 0f)
    {
        var textureName = sprite switch
        {
            FinalSprite.DinoFree => "sprite_dino_free",
            FinalSprite.CageOpen => "sprite_cage_open",
            FinalSprite.WitchDefeated => "sprite_witch_defeated",
            FinalSprite.GirlFree => "sprite_girl_free",
            _ => null,
        };

        return TryDrawTexture(assets, textureName, box, tint, rotation);
    }

    private static bool TryDrawTexture(AssetManager assets, string? textureName, Rectangle box, Color tint, float rotation)
    {
        if (textureName is null) return false;

        var texture = assets.FindTexture(textureName);
        if (!texture.HasValue) return false;

        DrawFit(texture.Value, box, tint, rotation);
        return true;
    }

    private static void DrawFit(Texture2D texture, Rectangle box, Color tint, float rotation)
    {
        var scale = MathF.Min(box.Width / texture.Width, box.Height / texture.Height);
        var width = texture.Width * scale;
        var height = texture.Height * scale;
        var dest = new Rectangle(box.X, box.Y, width, height);
        var source = new Rectangle(0, 0, texture.Width, texture.Height);
        Raylib.DrawTexturePro(texture, source, dest, new System.Numerics.Vector2(width / 2f, height / 2f), rotation, tint);
    }
}
