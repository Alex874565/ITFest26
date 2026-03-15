using DG.Tweening;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [field: SerializeField] public AudioSource SfxSource { get; private set; }

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("Volumes")]
    [Range(0f,1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f,1f)] [SerializeField] private float musicVolume = 1f;
    [Range(0f,1f)] [SerializeField] private float sfxVolume = 1f;

    [Header("Pitch Settings")]
    [Range(0.5f, 2f)] [SerializeField] private float pitchVariationRange = 0.1f;

    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;
    private AudioClip currentMusic;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;

        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", .5f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", .5f));
        SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", .5f));
    }

    void ApplyVolumes()
    {
        musicSourceA.volume = musicVolume * masterVolume;
        musicSourceB.volume = 0f;
        SfxSource.volume = sfxVolume * masterVolume;
    }

    public void PlayMusic(AudioClip clip, float fadeTime = 1.5f)
    {
        if (clip == null || currentMusic == clip) return;

        currentMusic = clip;

        inactiveMusicSource.clip = clip;
        inactiveMusicSource.volume = 0f;
        inactiveMusicSource.Play();

        activeMusicSource.DOKill();
        inactiveMusicSource.DOKill();

        activeMusicSource.DOFade(0f, fadeTime).SetUpdate(true);
        inactiveMusicSource.DOFade(musicVolume * masterVolume, fadeTime).SetUpdate(true);

        // swap sources
        var temp = activeMusicSource;
        activeMusicSource = inactiveMusicSource;
        inactiveMusicSource = temp;
    }

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }

    public void StopMusic(float fadeDuration = 1f)
    {
        activeMusicSource.DOFade(0f, fadeDuration).SetUpdate(true)
            .OnComplete(() => activeMusicSource.Stop());
    }

    // SFX
    public void PlayUI(AudioClip clip)
    {
        if (clip == null) return;
        SfxSource.PlayOneShot(clip);
    }

    public void PlayUIRandomPitch(AudioClip clip)
    {
        if (clip == null) return;
        SfxSource.pitch = 1f + Random.Range(-pitchVariationRange, pitchVariationRange);
        SfxSource.PlayOneShot(clip);
        SfxSource.pitch = 1f;
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);

        musicSourceA.volume = musicVolume * masterVolume;
        musicSourceB.volume = musicVolume * masterVolume;
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);

        SfxSource.volume = sfxVolume * masterVolume;
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);

        musicSourceA.volume = musicVolume * masterVolume;
        musicSourceB.volume = musicVolume * masterVolume;
        SfxSource.volume = sfxVolume * masterVolume;
    }

    public float GetMasterVolume() => masterVolume;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
}