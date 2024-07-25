using Backrooms.Coroutines;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Backrooms;

public class AudioManager(Window win)
{
    public readonly Window win = win;


    public void PlayOneShot(WaveStream waveStream, float volume = 1f, float panning = 0f, float startTime = 0f, IPanStrategy panStrategy = null)
    {
        AudioSource tempSrc = new(waveStream) {
            volume = volume,
            panning = panning,
            time = startTime,
            panStrategy = panStrategy ?? new SinPanStrategy(),
            disposeStream = false
        };

        tempSrc.Play();

        Coroutine.DelayedAction(tempSrc.length - tempSrc.time, tempSrc.Dispose).StartCoroutine(win);
    }
    public void PlayOneShot(string resourceName, float volume = 1f, float panning = 0f, float startTime = 0f, IPanStrategy panStrategy = null)
        => PlayOneShot(Resources.audios[resourceName], volume, panning, startTime, panStrategy);
}