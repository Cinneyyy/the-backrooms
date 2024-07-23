using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Backrooms;

public class AudioSource : IDisposable
{
    public bool disposeStream;

    private WaveStream stream;
    private readonly WaveOutEvent device;
    private readonly PanningSampleProvider panningProvider;


    public PlaybackState state => device.PlaybackState;
    /// <summary>Value from 0f to 1f</summary>
    public float volume
    {
        get => device.Volume;
        set => device.Volume = Utils.Clamp01(value);
    }
    /// <summary>Value from 0f to the clip length in seconds</summary>
    public float time
    {
        get => (float)stream.CurrentTime.TotalSeconds;
        set => stream.CurrentTime = TimeSpan.FromSeconds(value);
    }
    /// <summary>Value from 0f to 1f</summary>
    public float time01
    {
        get => time / (float)stream.TotalTime.TotalSeconds;
        set => stream.CurrentTime = TimeSpan.FromSeconds(value * stream.TotalTime.TotalSeconds);
    }
    /// <summary>Value in seconds</summary>
    public float length => (float)stream.TotalTime.TotalSeconds;
    public bool loop { get; init; }
    /// <summary>Pause between subsequent loops in seconds</summary>
    public float loopPadding { get; init; } = 1f;
    public IPanStrategy panStrategy
    {
        get => panningProvider.PanStrategy;
        set => panningProvider.PanStrategy = value;
    }
    /// <summary>Value from -1f (left) to 1f (right)</summary>
    public float panning
    {
        get => panningProvider.Pan;
        set => panningProvider.Pan = value;
    }
    /// <summary>Value from 0f (left) to 1f (right)</summary>
    public float panning01
    {
        get => (panning + 1f) / 2f;
        set => panning = value * 2f - 1f;
    }


    public AudioSource(WaveStream stream, bool loop = false)
    {
        this.stream = stream;

        panningProvider = new(stream.ToSampleProvider().ToMono()) {
            Pan = 0f,
            PanStrategy = new LinearPanStrategy()
        };

        device = new();
        device.Init(panningProvider);

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