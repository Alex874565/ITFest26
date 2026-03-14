using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private InputManager _inputManager;
    private static GameManager _instance;
    
    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        ResumeGame();
    }
    
    private void OnEnable()
    {
        _inputManager.OnEscapeAction += InputManager_OnEscapeAction;
        _playerController.OnDeath += PauseGame;
    }

    private void OnDisable()
    {
        _inputManager.OnEscapeAction -= InputManager_OnEscapeAction;
        _playerController.OnDeath -= PauseGame;
    }

    private void OnDestroy()
    {
        if (_instance != this) return;
    }

    public void InputManager_OnEscapeAction(object sender, EventArgs e)
    {
        var uiManager = ServiceLocator.Instance.UIManager;

        // 1. If Settings is open → close it
        if (uiManager.SettingsUI.gameObject.activeSelf)
        {
            uiManager.SettingsUI.Hide();
            return;
        }

        // 2. If Pause menu is open → resume
        if (uiManager.PauseUI.gameObject.activeSelf)
        {
            
            uiManager.PauseUI.Hide();
            ResumeGame();
            return;
        }

        // 3. Otherwise → pause
        
        uiManager.PauseUI.Show();
        PauseGame();
    }

    public void PauseGame()
    {
        Debug.Log("paused");
        IsPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Debug.Log("resumed");
        IsPaused = false;
        Time.timeScale = 1f;
    }
}