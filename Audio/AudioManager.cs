using KidsGame.Assets;
using Raylib_cs;

namespace KidsGame.Audio;

public sealed class AudioManager : IDisposable
{
    private readonly AssetManager _assets;
    private readonly Dictionary<string, double> _lastPlayedAt = new();
    private Music? _currentMusic;
    private string? _currentMusicName;

    private const double MinIntervalSeconds = 0.05;

    public AudioManager(AssetManager assets)
    {
        _assets = assets;
    }

    public void Play(string name)
    {
        var now = Raylib.GetTime();
        if (_lastPlayedAt.TryGetValue(name, out var last) && now - last < MinIntervalSeconds)
        {
            return;
        }

        var sound = _assets.GetSound(name);
        if (sound.HasValue)
        {
            Raylib.PlaySound(sound.Value);
            _lastPlayedAt[name] = now;
        }
    }

    public void PlayMusic(string name)
    {
        if (_currentMusicName == name)
        {
            return;
        }

        StopMusic();
        var music = _assets.GetMusic(name);
        if (!music.HasValue)
        {
            return;
        }

        _currentMusic = music.Value;
        _currentMusicName = name;
        Raylib.SetMusicVolume(_currentMusic.Value, 0.6f);
        Raylib.PlayMusicStream(_currentMusic.Value);
    }

    public void Update()
    {
        if (_currentMusic.HasValue)
        {
            Raylib.UpdateMusicStream(_currentMusic.Value);
        }
    }

    public void StopMusic()
    {
        if (_currentMusic.HasValue)
        {
            Raylib.StopMusicStream(_currentMusic.Value);
        }

        _currentMusic = null;
        _currentMusicName = null;
    }

    public void Dispose() => StopMusic();
}
