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
    [SerializeField] private ToggleButton additionButton;
    [SerializeField] private ToggleButton subtractionButton;
    [SerializeField] private ToggleButton multiplicationButton;
    [SerializeField] private ToggleButton divisionButton;

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

        additionButton.OnToggled += CheckPlayButton;
        subtractionButton.OnToggled += CheckPlayButton;
        multiplicationButton.OnToggled += CheckPlayButton;
        divisionButton.OnToggled += CheckPlayButton;
    }

    private void Start()
    {
        stagger.OpenMenu();
        CheckPlayButton();

        additionButton.SetState(true);
    }

    private void CheckPlayButton()
    {
        bool anyEnabled =
            additionButton.IsOn ||
            subtractionButton.IsOn ||
            multiplicationButton.IsOn ||
            divisionButton.IsOn;

        playButton.interactable = anyEnabled;
    }
}