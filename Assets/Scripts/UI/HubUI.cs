using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HubUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button shopButton;
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
                SceneManager.LoadScene("Game");
            });
        });

        settingsButton.onClick.AddListener(() =>
        {
            ServiceLocator.Instance.UIManager.SettingsUI.Show();
        });
        shopButton.onClick.AddListener(() =>
        {
            ServiceLocator.Instance.UIManager.ShopUI.Show();
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

        ServiceLocator.Instance.AudioManager.PlayMenuMusic();
    }

    private void CheckPlayButton()
    {
        bool anyEnabled = ServiceLocator.Instance.PlayerManager.SelectedEquations.Count > 0;

        playButton.interactable = anyEnabled;
    }
}