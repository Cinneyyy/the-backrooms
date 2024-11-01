using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Backrooms;

public class AudioPlayback : IDisposable
{
    private readonly WaveOutEvent waveOut;
    private readonly WaveStream stream;
    private readonly PanningSampleProvider panningProvider;
    private readonly bool disposeStream;


    public float volume
    {
        get => waveOut.Volume;
        set => waveOut.Volume = value;
    }
    public float panning
    {
        get => panningProvider.Pan;
        set => panningProvider.Pan = value;
    }
    public bool loop
    {
        get => (stream as LoopingWaveStream).loop;
        set => (stream as LoopingWaveStream).loop = value;
    }


    public AudioPlayback(WaveStream stream, bool disposeStream)
    {
        this.stream = stream;
        this.disposeStream = disposeStream;

        panningProvider = new(stream.ToSampleProvider().ToMono()) {
            Pan = 0f,
            PanStrategy = new SinPanStrategy()
        };

        waveOut = new();
        waveOut.Init(panningProvider);
    }

    public AudioPlayback(LoopingWaveStream loopingStream, bool disposeStream) : this(stream: loopingStream, disposeStream) { }


    ~AudioPlayback()
        => Dispose();


    public void Dispose()
    {
        waveOut.Dispose();
        if(disposeStream)
            stream.Dispose();

        GC.SuppressFinalize(this);
    }

    public void Play()
        => waveOut.Play();

    public void Stop()
        => waveOut.Stop();


    public static AudioPlayback PlayOneShot(WaveStream stream, bool disposeStream, float volume = 1f, float panning = 0f)
    {
        AudioPlayback playback = new(stream, disposeStream) {
            volume = volume,
            panning = panning
        };

        playback.Play();
        return playback;
    }
    public static AudioPlayback PlayOneShot(string clipId, float volume = 1f, float panning = 0f)
        => PlayOneShot(Resources.audios[clipId], false, volume, panning);
}