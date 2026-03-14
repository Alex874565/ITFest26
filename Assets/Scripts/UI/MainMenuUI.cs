using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    //[SerializeField] private Button settingsButton;
    //[SerializeField] private Button shopButton;
    [SerializeField] private MenuStaggerAnimation stagger;

    private void Awake()
    {
        playButton.onClick.AddListener(() =>
        {
            stagger.CloseMenu(() =>
            {
                SceneManager.LoadScene("HubScene");
            });
        });
        // settingsButton.onClick.AddListener(() =>
        // {
        //     ServiceLocator.Instance.UIManager.SettingsUI.Show();
        // });
    }

    private void Start()
    {
        stagger.OpenMenu();

        ServiceLocator.Instance.AudioManager.PlayMenuMusic();
    }

    public void HideMenu()
    {
        stagger.CloseMenu();
    }
}
