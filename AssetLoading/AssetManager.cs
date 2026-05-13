using Raylib_cs;

namespace KidsGame.Assets;

public sealed class AssetManager : IDisposable
{
    private readonly Dictionary<string, Texture2D> _textures = new();
    private readonly Dictionary<string, Sound> _sounds = new();
    private readonly Dictionary<string, Music> _music = new();
    private readonly HashSet<string> _missingWarnings = new();

    private Texture2D _fallbackTexture;
    private Font _font;
    private bool _customFontLoaded;

    private static readonly Dictionary<string, string> TexturePaths = new()
    {
        ["unicorn"] = "assets/characters/unicorn.png",
        ["princess_overlay"] = "assets/characters/princess_overlay.png",
        ["poli"] = "assets/characters/poli.png",
        ["kitten"] = "assets/animals/kitten.png",
        ["puppy"] = "assets/animals/puppy.png",
        ["bunny"] = "assets/animals/bunny.png",
        ["grass_tile"] = "assets/world/grass_tile.png",
        ["path_tile"] = "assets/world/path_tile.png",
        ["log"] = "assets/world/log.png",
        ["tree"] = "assets/world/tree.png",
        ["puddle"] = "assets/world/puddle.png",
        ["balloons"] = "assets/world/balloons.png",
        ["crystal"] = "assets/world/crystal.png",
        ["rainbow_particle"] = "assets/effects/rainbow_particle.png",
        ["heart_particle"] = "assets/effects/heart_particle.png",
        ["star_particle"] = "assets/effects/star_particle.png",
        ["firework"] = "assets/effects/firework.png",
        ["progress_bar_bg"] = "assets/ui/progress_bar_bg.png",
        ["progress_bar_fill"] = "assets/ui/progress_bar_fill.png",
        ["celebration_banner"] = "assets/ui/celebration_banner.png",
        ["sprite_princess"] = "assets/generated/sprites/princess.png",
        ["sprite_prince_window"] = "assets/generated/sprites/prince_window.png",
        ["sprite_castle"] = "assets/generated/sprites/castle.png",
        ["sprite_cottage"] = "assets/generated/sprites/cottage.png",
        ["sprite_cottage_damaged"] = "assets/generated/sprites/cottage_damaged.png",
        ["sprite_cottage_broken"] = "assets/generated/sprites/cottage_broken.png",
        ["sprite_witch"] = "assets/generated/sprites/witch.png",
        ["sprite_girl"] = "assets/generated/sprites/girl.png",
        ["sprite_apple"] = "assets/generated/sprites/apple.png",
        ["sprite_carrot"] = "assets/generated/sprites/carrot.png",
        ["sprite_dino_caged"] = "assets/generated/sprites/dino_caged.png",
        ["sprite_dino_cracked"] = "assets/generated/sprites/dino_cracked.png",
        ["sprite_poli"] = "assets/generated/sprites/poli.png",
        ["sprite_amber"] = "assets/generated/sprites/amber.png",
        ["sprite_helly"] = "assets/generated/sprites/helly.png",
        ["sprite_roy"] = "assets/generated/sprites/roy.png",
        ["sprite_dino_free"] = "assets/generated/sprites/dino_free.png",
        ["sprite_cage_open"] = "assets/generated/sprites/cage_open.png",
        ["sprite_witch_defeated"] = "assets/generated/sprites/witch_defeated.png",
        ["sprite_girl_free"] = "assets/generated/sprites/girl_free.png",
        ["sprite_castle_royal"] = "assets/generated/sprites/castle_royal.png",
        ["sprite_castle_gate"] = "assets/generated/sprites/castle_gate.png",
        ["sprite_royal_fountain"] = "assets/generated/sprites/royal_fountain.png",
        ["sprite_garden_arch"] = "assets/generated/sprites/garden_arch.png",
        ["sprite_forest_broadleaf"] = "assets/generated/sprites/forest_broadleaf.png",
        ["sprite_forest_pine"] = "assets/generated/sprites/forest_pine.png",
        ["sprite_forest_bush"] = "assets/generated/sprites/forest_bush.png",
        ["sprite_forest_mushroom"] = "assets/generated/sprites/forest_mushroom.png",
        ["sprite_forest_log"] = "assets/generated/sprites/forest_log.png",
        ["sprite_forest_rocks"] = "assets/generated/sprites/forest_rocks.png",
        ["sprite_forest_flowers"] = "assets/generated/sprites/forest_flowers.png",
        ["sprite_forest_path"] = "assets/generated/sprites/forest_path.png",
        ["sprite_robot_poli"] = "assets/generated/sprites/robot_poli.png",
        ["sprite_robot_amber"] = "assets/generated/sprites/robot_amber.png",
        ["sprite_robot_helly"] = "assets/generated/sprites/robot_helly.png",
        ["sprite_robot_roy"] = "assets/generated/sprites/robot_roy.png",
        ["sprite_princess_blue"] = "assets/generated/sprites/princess_blue.png",
        ["sprite_princess_yellow"] = "assets/generated/sprites/princess_yellow.png",
        ["sprite_princess_purple"] = "assets/generated/sprites/princess_purple.png",
        ["sprite_princess_caged"] = "assets/generated/sprites/princess_caged.png",
        ["sprite_princess_free"] = "assets/generated/sprites/princess_free.png",
        ["sprite_princess_friends"] = "assets/generated/sprites/princess_friends.png",
        ["sprite_princess_mermaid"] = "assets/generated/sprites/princess_mermaid.png",
        ["sprite_princess_ice"] = "assets/generated/sprites/princess_ice.png",
        ["sprite_princess_snow"] = "assets/generated/sprites/princess_snow.png",
        ["sprite_princess_cinderella"] = "assets/generated/sprites/princess_cinderella.png",
        ["sprite_princess_rapunzel"] = "assets/generated/sprites/princess_rapunzel.png",
        ["sprite_princess_rose"] = "assets/generated/sprites/princess_rose.png",
        ["story_witch"] = "assets/generated/sprites/story_witch.png",
        ["story_child"] = "assets/generated/sprites/story_child.png",
        ["story_cottage_open"] = "assets/generated/sprites/story_cottage_open.png",
        ["story_cottage_locked"] = "assets/generated/sprites/story_cottage_locked.png",
        ["story_children_locked_crying"] = "assets/generated/sprites/story_children_locked_crying.png",
        ["magic_child_pink"] = "assets/generated/sprites/magic_child_pink.png",
        ["magic_child_blue"] = "assets/generated/sprites/magic_child_blue.png",
        ["magic_child_yellow"] = "assets/generated/sprites/magic_child_yellow.png",
        ["sprite_crested_dino_caged"] = "assets/generated/sprites/crested_dino_caged.png",
        ["sprite_crested_dino_cracked"] = "assets/generated/sprites/crested_dino_cracked.png",
        ["sprite_crested_dino_free"] = "assets/generated/sprites/crested_dino_free.png",
        ["sprite_stego_dino_caged"] = "assets/generated/sprites/stego_dino_caged.png",
        ["sprite_stego_dino_cracked"] = "assets/generated/sprites/stego_dino_cracked.png",
        ["sprite_stego_dino_free"] = "assets/generated/sprites/stego_dino_free.png",
        ["sprite_brachio_dino_caged"] = "assets/generated/sprites/brachio_dino_caged.png",
        ["sprite_brachio_dino_cracked"] = "assets/generated/sprites/brachio_dino_cracked.png",
        ["sprite_brachio_dino_free"] = "assets/generated/sprites/brachio_dino_free.png",
        ["sprite_ptero_dino_caged"] = "assets/generated/sprites/ptero_dino_caged.png",
        ["sprite_ptero_dino_cracked"] = "assets/generated/sprites/ptero_dino_cracked.png",
        ["sprite_ptero_dino_free"] = "assets/generated/sprites/ptero_dino_free.png",
        ["magic_orb_pink"] = "assets/generated/sprites/magic_orb_pink.png",
        ["magic_orb_blue"] = "assets/generated/sprites/magic_orb_blue.png",
        ["magic_orb_yellow"] = "assets/generated/sprites/magic_orb_yellow.png",
        ["magic_gate_pink"] = "assets/generated/sprites/magic_gate_pink.png",
        ["magic_gate_blue"] = "assets/generated/sprites/magic_gate_blue.png",
        ["magic_gate_yellow"] = "assets/generated/sprites/magic_gate_yellow.png",
        ["dressup_unicorn"] = "assets/generated/sprites/dressup_unicorn.png",
        ["dressup_unicorn_alt"] = "assets/generated/sprites/dressup_unicorn_alt.png",
        ["unicorn_crown"] = "assets/generated/sprites/unicorn_crown.png",
        ["unicorn_bow"] = "assets/generated/sprites/unicorn_bow.png",
        ["unicorn_blanket"] = "assets/generated/sprites/unicorn_blanket.png",
        ["bg_story_witch_forest"] = "assets/generated/backgrounds/story_witch_forest.png",
        ["tap_dirty_dino"] = "assets/generated/sprites/tap_dirty_dino.png",
        ["tap_clean_dino"] = "assets/generated/sprites/tap_clean_dino.png",
        ["tap_wash_sponge"] = "assets/generated/sprites/tap_wash_sponge.png",
        ["tap_dino_egg"] = "assets/generated/sprites/tap_dino_egg.png",
        ["tap_dino_egg_cracked"] = "assets/generated/sprites/tap_dino_egg_cracked.png",
        ["tap_baby_dino"] = "assets/generated/sprites/tap_baby_dino.png",
        ["tap_firework_launcher"] = "assets/generated/sprites/tap_firework_launcher.png",
        ["tap_firework_burst"] = "assets/generated/sprites/tap_firework_burst.png",
        ["tap_poli_charger"] = "assets/generated/sprites/tap_poli_charger.png",
        ["tap_battery"] = "assets/generated/sprites/tap_battery.png",
        ["bg_dino_stage_meadow"] = "assets/generated/backgrounds/dino_stage_meadow.png",
        ["bg_dino_stage_jungle"] = "assets/generated/backgrounds/dino_stage_jungle.png",
        ["bg_dino_stage_valley"] = "assets/generated/backgrounds/dino_stage_valley.png",
    };

    private static readonly Dictionary<string, string> SoundPaths = new()
    {
        ["sfx_collect"] = "assets/audio/sfx_collect.ogg",
        ["sfx_button"] = "assets/audio/sfx_button.ogg",
        ["sfx_rescue_hit"] = "assets/audio/sfx_rescue_hit.ogg",
        ["sfx_rescue_complete"] = "assets/audio/sfx_rescue_complete.ogg",
        ["sfx_meow"] = "assets/audio/sfx_meow.ogg",
        ["sfx_woof"] = "assets/audio/sfx_woof.ogg",
    };

    private static readonly Dictionary<string, string> MusicPaths = new()
    {
        ["bgm_main"] = "assets/audio/bgm_main_soft.wav",
        ["bgm_celebration"] = "assets/audio/bgm_celebration_soft.wav",
    };

    public void LoadAll()
    {
        var image = Raylib.GenImageColor(16, 16, Color.Magenta);
        _fallbackTexture = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);

        foreach (var (name, path) in TexturePaths)
        {
            if (File.Exists(path))
            {
                TryLoadTexture(name, path);
            }
            else
            {
                WarnMissing(path);
            }
        }

        foreach (var (name, path) in SoundPaths)
        {
            if (File.Exists(path))
            {
                TryLoadSound(name, path);
            }
            else
            {
                WarnMissing(path);
            }
        }

        foreach (var (name, path) in MusicPaths)
        {
            if (File.Exists(path))
            {
                TryLoadMusic(name, path);
            }
            else
            {
                WarnMissing(path);
            }
        }

        LoadFont();
    }

    public Texture2D GetTexture(string name)
    {
        return _textures.TryGetValue(name, out var texture) ? texture : _fallbackTexture;
    }

    public Texture2D? FindTexture(string name)
    {
        return _textures.TryGetValue(name, out var texture) ? texture : null;
    }

    public Sound? GetSound(string name)
    {
        return _sounds.TryGetValue(name, out var sound) ? sound : null;
    }

    public Music? GetMusic(string name)
    {
        return _music.TryGetValue(name, out var music) ? music : null;
    }

    public Font GetFont() => _font;

    public bool HasCustomFont => _customFontLoaded;

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            Raylib.UnloadTexture(texture);
        }

        foreach (var sound in _sounds.Values)
        {
            Raylib.UnloadSound(sound);
        }

        foreach (var music in _music.Values)
        {
            Raylib.UnloadMusicStream(music);
        }

        if (_customFontLoaded)
        {
            Raylib.UnloadFont(_font);
        }

        if (_fallbackTexture.Id != 0)
        {
            Raylib.UnloadTexture(_fallbackTexture);
        }
    }

    private void TryLoadTexture(string name, string path)
    {
        try
        {
            _textures[name] = Raylib.LoadTexture(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[assets] warning: failed to load texture {path}: {ex.Message}");
        }
    }

    private void TryLoadSound(string name, string path)
    {
        try
        {
            _sounds[name] = Raylib.LoadSound(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[assets] warning: failed to load sound {path}: {ex.Message}");
        }
    }

    private void TryLoadMusic(string name, string path)
    {
        try
        {
            _music[name] = Raylib.LoadMusicStream(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[assets] warning: failed to load music {path}: {ex.Message}");
        }
    }

    private void LoadFont()
    {
        var path = FindFontPath();
        if (path is null)
        {
            WarnMissing("assets/fonts/NotoSansTC-Regular.ttf");
            _font = Raylib.GetFontDefault();
            return;
        }

        try
        {
            var codepoints = BuildCodepoints();
            _font = Raylib.LoadFontEx(path, 32, codepoints, codepoints.Length);
            _customFontLoaded = _font.Texture.Id != 0;
            if (!_customFontLoaded)
            {
                _font = Raylib.GetFontDefault();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[assets] warning: failed to load font {path}: {ex.Message}");
            _font = Raylib.GetFontDefault();
        }
    }

    private static int[] BuildCodepoints()
    {
        const string text = "一下三上主之也了以任伊住倒公冠出到力功加動卡原反可吃向單同回在地坑城基堡壓夕多夜大女好始姐婆子孩安完寶尋小巫已幫弟彩忙恐意慶應成或戲房手打找抓拆拯按掉揮援擇擋收救方星晶更會有木林果森樹氣水波滿物狗獨獸王球由白祝移空第籠紅結繼續羅自與色草菜虹被要角變貓赫起跟輪近送連遊選還配鍵鎮開關除陽隊隻集需青靠顆顏風食飾麗點龍身黃亮二朋友入伍劍四五腕翼長頸高峽谷玫瑰魔法花園光門夥伴粉碎新能量發喜嗯品對很後想扮把披拿放最歡正漂用皇著藍蝴蝶裝事人來先制口帶序從控故段章走鎖依個再家石進都乾充別孵插敲泡洗淨澡火煙生的直穿蛋電：，·！/0123456789";
        return text.Distinct().Select(c => (int)c).Concat(Enumerable.Range(32, 95)).Distinct().ToArray();
    }

    private static string? FindFontPath()
    {
        var candidates = new[]
        {
            "assets/fonts/NotoSansTC-Regular.ttf",
            @"C:\Windows\Fonts\NotoSansTC-VF.ttf",
            @"C:\Windows\Fonts\msjh.ttc",
            @"C:\Windows\Fonts\mingliu.ttc",
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private void WarnMissing(string path)
    {
        if (_missingWarnings.Add(path))
        {
            Console.WriteLine($"[assets] warning: missing {path}; fallback will be used.");
        }
    }
}
