using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button hubButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private MenuStaggerAnimation stagger;

    private void Awake()
    {
        resumeButton.onClick.AddListener(() =>
        {
            ServiceLocator.Instance.GameManager.ResumeGame();
            Hide();
            
        });
        closeButton.onClick.AddListener(() =>
        {
            Hide();
            ServiceLocator.Instance.GameManager.ResumeGame();
        });
        settingsButton.onClick.AddListener( () =>
        {
            ServiceLocator.Instance.UIManager.SettingsUI.Show();
        });
        hubButton.onClick.AddListener( () =>
        {
            stagger.CloseMenu(() =>
            {
                SceneManager.LoadScene("HubScene");
            });
        });
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        stagger.OpenMenu();
    }

    public void Hide()
    {
        stagger.CloseMenu(() =>
        {
            gameObject.SetActive(false);
        });
    }
}
