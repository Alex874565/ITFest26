using UnityEngine;

public class ServiceLocator : MonoBehaviour
{
    public static ServiceLocator Instance { get; private set; }
    
    [field: SerializeField] public InputManager InputManager { get; private set; }
    [field: SerializeField] public UIManager UIManager { get; private set; }
    public GameManager GameManager { get; private set; }
    public AudioManager AudioManager { get; private set; }
    public PlayerManager PlayerManager { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }

        Instance = this;

        AudioManager audioManager = FindFirstObjectByType<AudioManager>();

        if (audioManager)
        {
            AudioManager = audioManager;
        }

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        
        if (gameManager)
        {
            GameManager = gameManager;
        }

        PlayerManager playerManager = FindFirstObjectByType<PlayerManager>();

        if (playerManager)
        {
            PlayerManager = playerManager;
        }
    }
}