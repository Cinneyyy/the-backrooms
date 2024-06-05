using System;
using System.IO;
using NAudio.Wave;

namespace Backrooms;

public class AudioSource : IDisposable
{
    public bool loop;

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


    public AudioSource(WaveStream stream)
    {
        this.stream = stream;
        device = new();
        device.Init(stream);
        device.PlaybackStopped += (_, _) => {
            if(loop && state == PlaybackState.Stopped)
            {
                time = 0f;
                Play();
            }
        };
        volume = 1f;
    }

    public AudioSource(Stream dataStream, string fileType) : this(fileType.ToLower() switch {
        ".mp3" => new Mp3FileReader(dataStream),
        ".wav" => new WaveFileReader(dataStream),
        ".aiff" => new AiffFileReader(dataStream),
        _ => throw new($"Unsupported audio format: {fileType}")
    }) { }


    void IDisposable.Dispose()
    {
        stream.Dispose();
        device.Dispose();
        GC.SuppressFinalize(this);
    }


    public void Play()
        => device.Play();

    public void Pause()
        => device.Pause();

    public void Stop()
    {
        device.Stop();
        time = 0f;
    }
}