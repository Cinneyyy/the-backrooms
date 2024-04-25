using System.Windows.Media;
using System.Media;
using System.Runtime.InteropServices;
using System;

namespace Backrooms;

public partial class AudioSource(string audioName)
{
    public bool playing;
    public bool loop;
    public float volume;

    // [potential] todo: give each AudioSource a unique SoundPlayer, instead of the same one, such that multiple instances of the same sound can be played and managed at once
    private readonly SoundPlayer soundPlayer = Resources.audios[audioName];


    public void Play()
    {
        SetWaveOutVolume(volume);
        if(loop) soundPlayer.PlayLooping();
        else soundPlayer.Play();
    }

    public void Stop()
    {
    }


    public static void SetWaveOutVolume(float volume)
    {
        uint vol = (uint)(volume * 10_000f);
        _ = waveOutSetVolume(nint.Zero, (vol & 0xffff) | (vol << 0xf));
    }
    public static float GetWaveOutVolume()
    {
        _ = waveOutGetVolume(nint.Zero, out uint dwVolume);
        return (dwVolume & 0xffff) / 10_000f;
    }


    [LibraryImport("winmm.dll")]
    private static partial int waveOutSetVolume(IntPtr hwo, uint dwVolume);

    [LibraryImport("winmm.dll")]
    private static partial int waveOutGetVolume(IntPtr hwo, out uint dwVolume);
}