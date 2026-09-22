using UnityEngine;

// Scene-local: replay reloads Game and starts a fresh pair of audio sources.
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioClip shotClip;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.22f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.7f;

    private void Awake()
    {
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
        }
        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
        }
        ApplyVolumes();
    }

    private void OnEnable() => PlayMusic();

    private void OnDisable()
    {
        StopMusic();
        if (sfxSource != null)
            sfxSource.Stop();
    }

    private void OnValidate() => ApplyVolumes();

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume;
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    public void PlayMusic()
    {
        if (!isActiveAndEnabled || musicSource == null || musicClip == null)
            return;
        if (musicSource.isPlaying && musicSource.clip == musicClip)
            return;

        musicSource.clip = musicClip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void PlayShot() => PlaySfx(shotClip);

    public void PlaySfx(AudioClip clip)
    {
        if (!isActiveAndEnabled || sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip);
    }
}
