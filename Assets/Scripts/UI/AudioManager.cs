using DG.Tweening;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    
    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [field: SerializeField] public AudioSource SfxSource { get; private set; }

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuMusic;
    //[SerializeField] private AudioClip gameplayMusic;

    [Header("Volumes")]
    [Range(0f,1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f,1f)] [SerializeField] private float musicVolume = 1f;
    [Range(0f,1f)] [SerializeField] private float sfxVolume = 1f;
    
    [Header("Pitch Settings")]
    [Range(0.5f, 2f)] [SerializeField] private float pitchVariationRange = 0.1f;

    private AudioClip currentMusic;

    private void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", .5f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", .5f));
        SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", .5f));
    }

    void ApplyVolumes()
    {
        musicSource.volume = musicVolume * masterVolume;
        SfxSource.volume = sfxVolume * masterVolume;
    }

    // MUSIC SWITCH
    public void PlayMusic(AudioClip clip)
    {
        Debug.Log("PLAY MUSIC CALLED");
        Debug.Log(clip);
        Debug.Log(currentMusic);
        if (clip == null || currentMusic == clip) return;
        currentMusic = clip;

        musicSource.DOKill();
        musicSource.DOFade(0f, 0.5f).SetUpdate(true).OnComplete(() =>
        {
            musicSource.clip = clip;
            musicSource.Play();
            musicSource.DOFade(musicVolume * masterVolume, 0.5f).SetUpdate(true);
        });
    }

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    // public void PlayGameplayMusic()
    // {
    //     Debug.Log("PLAY GAMEPLAY MUSIC CALLED");
    //     PlayMusic(gameplayMusic);
    // }

    // VOLUME
    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        ApplyVolumes();
    }

    public float GetMusicVolume() => musicVolume;

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        ApplyVolumes();
    }

    public float GetSFXVolume() => sfxVolume;

    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        ApplyVolumes();
    }

    public float GetMasterVolume() => masterVolume;

    // SFX
    // public void PlayUI(AudioClip clip)
    // {
    //     if (clip == null) return;
    //     SfxSource.PlayOneShot(clip);
    // }
    
    // public void PlayUIRandomPitch(AudioClip clip)
    // {
    //     if (clip == null) return;
    //     SfxSource.pitch = 1f + Random.Range(-pitchVariationRange, pitchVariationRange);
    //     SfxSource.PlayOneShot(clip);
    //     SfxSource.pitch = 1f; // Reset pitch after playing
    // }

    // public void StopMusic(float fadeDuration = 0.5f)
    // {
    //     musicSource.DOKill();
    //     // SetUpdate(true) is critical for pausing!
    //     musicSource.DOFade(0f, fadeDuration).SetUpdate(true).OnComplete(() =>
    //     {
    //         musicSource.Stop();
    //         musicSource.clip = null;
    //         currentMusic = null;
    //         musicSource.volume = musicVolume * masterVolume;
    //     });
    // }

    // public AudioSource GetMusicSource()
    // {
    //     return musicSource;
    // }
}