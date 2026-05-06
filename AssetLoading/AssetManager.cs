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
        ["bgm_main"] = "assets/audio/bgm_main.ogg",
        ["bgm_celebration"] = "assets/audio/bgm_celebration.ogg",
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
        const string text = "公主獨角獸與Poli救援隊按任意鍵開始方向鍵移動空白鍵幫忙水晶小動物已救了隻收集顆救援完成一起慶祝姐姐弟弟";
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
