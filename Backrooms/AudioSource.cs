using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Backrooms;

public class AudioSource : IDisposable
{
    public bool disposeStream;

    private WaveStream stream;
    private readonly WaveOutEvent device;


    public PlaybackState state => device.PlaybackState;
    public float volume
    {
        get => device.Volume;
        set => device.Volume = Utils.Clamp01(value);
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

    public AudioSource(Stream dataStream, string codec, bool loop = false) : this(Utils.StreamToWaveStream(dataStream, codec), loop) { }

    public AudioSource(string fileName, bool loop = false) : this(Utils.FileToWaveStream(fileName), loop) { }


    public void Dispose()
    {
        if(disposeStream)
            stream.Dispose();
        device.Dispose();
        GC.SuppressFinalize(this);
    }

    public void SetWaveStream(WaveStream stream, bool disposePrev)
    {
        if(disposePrev)
            this.stream.Dispose();

        this.stream = stream;
        device.Init(stream);
    }
    public void SetWaveStream(Stream dataStream, string codec, bool disposePrev)
    {
        if(disposePrev)
            stream.Dispose();

        stream = Utils.StreamToWaveStream(dataStream, codec);
        device.Init(stream);
    }
    public void SetWaveStream(string fileName, bool disposePrev)
    {
        if(disposePrev)
            stream.Dispose();

        stream = Utils.FileToWaveStream(fileName);
        device.Init(stream);
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