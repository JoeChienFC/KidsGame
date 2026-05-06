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
        const string text = "一下主之也了以任伊住倒公出到力功動卡原反可吃向單回在地坑城基堡壓夕多夜大女好始姐婆子孩安完寶尋小巫已幫弟彩忙恐意慶應成或戲房手打找抓拆拯按掉揮援擇擋收救方星晶更會有木林果森樹氣水波滿物狗獨獸王球由白祝移空第籠繼續羅自與草菜虹被要角變貓赫起跟輪近連遊選還鍵鎮開關除陽隊隻集需青靠顆食麗點龍身：，·！/0123456789";
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
