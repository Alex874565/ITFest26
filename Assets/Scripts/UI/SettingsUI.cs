using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private MenuStaggerAnimation stagger;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        closeButton.onClick.AddListener(() =>
        {
            Hide();
        });
    }

    private void Start()
    {
        // Start hidden
        gameObject.SetActive(false);

        var audio = ServiceLocator.Instance.AudioManager;

        masterSlider.value = audio.GetMasterVolume();
        musicSlider.value = audio.GetMusicVolume();
        sfxSlider.value = audio.GetSFXVolume();

        masterSlider.onValueChanged.AddListener(audio.SetMasterVolume);
        musicSlider.onValueChanged.AddListener(audio.SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(audio.SetSFXVolume);
    }

    public void Show()
    {
        gameObject.SetActive(true); // must activate first for animation to work
        stagger.OpenMenu();          // stagger in buttons, text, etc.
    }

    public void Hide()
    {
        // Close menu with stagger, then deactivate
        stagger.CloseMenu(() =>
        {
            gameObject.SetActive(false);
        });
    }
}