using TMPro;
using UnityEngine;

public class HudUI : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;

    private void OnEnable()
    {
        playerController.OnScoreChanged += UpdateScore;
    }

    private void OnDisable()
    {
        playerController.OnScoreChanged -= UpdateScore;
    }

    private void UpdateScore(int score)
    {
        scoreText.text = $"{score}";
    }
}