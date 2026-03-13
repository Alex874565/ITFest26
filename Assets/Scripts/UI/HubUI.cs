using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HubUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private MenuStaggerAnimation stagger;

    [Header("Operations Buttons")]
    [SerializeField] private Button additionButton;
    [SerializeField] private Button subtractionButton;
    [SerializeField] private Button multiplicationButton;
    [SerializeField] private Button divisionButton;

    private void Awake()
    {
        playButton.onClick.AddListener(() =>
        {
            stagger.CloseMenu(() =>
            {
                SceneManager.LoadScene("GameScene");
            });
        });
        settingsButton.onClick.AddListener(() =>
        {
            ServiceLocator.Instance.UIManager.SettingsUI.Show();
        });
        mainMenuButton.onClick.AddListener(() =>
        {
            stagger.CloseMenu(() =>
            {     
                SceneManager.LoadScene("MainMenuScene");
            });
        });
    }

    private void Start()
    {
        stagger.OpenMenu();

        //ServiceLocator.Instance.AudioManager.PlayMenuMusic();
    }
}
