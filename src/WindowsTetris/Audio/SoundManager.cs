using System.IO;
using System.Windows.Media;

namespace WindowsTetris.Audio;

/// <summary>
/// Thin wrapper over WPF <see cref="MediaPlayer"/> instances that plays the BGM
/// (looping) and the one-shot sound effects ported from the original project.
/// Missing files are tolerated so the game still runs without audio.
/// </summary>
public sealed class SoundManager
{
    private readonly string _audioDir;
    private readonly MediaPlayer _bgm = new();
    private readonly Dictionary<string, MediaPlayer> _effects = new();
    private bool _muted;
    private bool _bgmReady;

    public SoundManager()
    {
        _audioDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Audio");

        string bgmPath = Path.Combine(_audioDir, "tetris.mp3");
        if (File.Exists(bgmPath))
        {
            _bgm.Open(new Uri(bgmPath));
            _bgm.Volume = 0.4;
            _bgm.MediaEnded += (_, _) =>
            {
                _bgm.Position = TimeSpan.Zero;
                _bgm.Play();
            };
            _bgmReady = true;
        }

        Register("start", "start.wav", 0.6);
        Register("menu", "menu_move.wav", 0.6);
        Register("blip", "blip.wav", 0.7);
        Register("clear", "tetris-win.mp3", 0.7);
    }

    public bool IsMuted => _muted;

    private void Register(string key, string file, double volume)
    {
        string path = Path.Combine(_audioDir, file);
        if (!File.Exists(path)) return;
        var player = new MediaPlayer();
        player.Open(new Uri(path));
        player.Volume = volume;
        _effects[key] = player;
    }

    public void PlayEffect(string key)
    {
        if (_muted) return;
        if (_effects.TryGetValue(key, out var player))
        {
            player.Position = TimeSpan.Zero;
            player.Play();
        }
    }

    public void StartBgm()
    {
        if (!_bgmReady) return;
        _bgm.Position = TimeSpan.Zero;
        if (!_muted) _bgm.Play();
    }

    public void StopBgm()
    {
        if (_bgmReady) _bgm.Stop();
    }

    public void PauseBgm()
    {
        if (_bgmReady) _bgm.Pause();
    }

    public void ResumeBgm()
    {
        if (_bgmReady && !_muted) _bgm.Play();
    }

    public void SetMuted(bool muted)
    {
        _muted = muted;
        if (_muted)
        {
            _bgm.Volume = 0;
        }
        else
        {
            _bgm.Volume = 0.4;
        }
    }
}
