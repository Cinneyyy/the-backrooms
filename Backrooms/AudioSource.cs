using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Backrooms;

public class AudioSource : IDisposable
{
    private readonly WaveStream stream;
    private readonly WaveOutEvent device;


    public PlaybackState state => device.PlaybackState;
    public float volume
    {
        get => device.Volume;
        set => device.Volume = Utils.Clamp(value, 0f, 1f);
    }
    public float time
    {
        get => (float)stream.CurrentTime.TotalSeconds;
        set => stream.CurrentTime = TimeSpan.FromSeconds(value);
    }
    public float time01
    {
        get => time / (float)stream.TotalTime.TotalSeconds;
        set => stream.CurrentTime = TimeSpan.FromSeconds(value * stream.TotalTime.TotalSeconds);
    }
    public float length => (float)stream.TotalTime.TotalSeconds;
    public bool loop { get; init; }
    public float loopPadding { get; init; } = 1f;


    public AudioSource(WaveStream stream, bool loop = false)
    {
        this.stream = stream;
        device = new();
        device.Init(stream);

        volume = 1f;
        this.loop = loop;
    }

    public AudioSource(Stream dataStream, string fileType, bool loop = false) : this(fileType.ToLower() switch {
        ".mp3" => new Mp3FileReader(dataStream),
        ".wav" => new WaveFileReader(dataStream),
        ".aiff" => new AiffFileReader(dataStream),
        _ => throw new($"Unsupported audio format: {fileType}")
    }, loop) { }

    public AudioSource(string fileName, bool loop = false) : this(Path.GetExtension(fileName).ToLower() switch {
        ".mp3" => new Mp3FileReader(fileName),
        ".wav" => new WaveFileReader(fileName),
        ".aiff" => new AiffFileReader(fileName),
        string f => throw new ($"Unsupported audio format: {f}")
    }, loop) { }


    void IDisposable.Dispose()
    {
        stream.Dispose();
        device.Dispose();
        GC.SuppressFinalize(this);
    }


    public void Play()
    {
        device.Play();
        if(loop)
            _ = WaitForPlaybackStop();
    }

    public void Pause()
        => device.Pause();

    public void Stop()
    {
        device.Stop();
        time = 0f;
    }


    private async Task WaitForPlaybackStop()
    {
        await Task.Delay(((length + loopPadding) * 1000f).Ceil());
        time = 0f;
        Play();
    }
}