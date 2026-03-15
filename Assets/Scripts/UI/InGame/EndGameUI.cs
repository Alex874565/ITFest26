using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameUI : MonoBehaviour
{
    [SerializeField] private EquationsCategoriesDatabase equationsDatabase;
    [SerializeField] private PlayerManager playerManager;
    [Header("UI References")]
    [SerializeField] private Transform barsParent;
    [SerializeField] private EquationProgressBarUI barPrefab;
    [SerializeField] private TextMeshProUGUI moneyText;
    
    private readonly List<EquationProgressBarUI> spawnedBars = new();

    private void Awake()
    {
        gameObject.SetActive(false);
        playerManager.OnEndGameValuesCalculated += Show;
    }

    private void OnDestroy()
    {
        playerManager.OnEndGameValuesCalculated -= Show;
    }
    
    public void Show(
        Dictionary<EquationType, int> previousScores,
        Dictionary<EquationType, int> newScores, int moneyGained)
    {
        gameObject.SetActive(true);
        ClearBars();

        foreach (var pair in newScores)
        {
            EquationType type = pair.Key;
            int newScore = pair.Value;
            int previousScore = previousScores.TryGetValue(type, out int oldValue) ? oldValue : 0;

            EquationCategoryData categoryData = equationsDatabase.GetEquationCategoryData(type);
            if (categoryData == null)
                continue;

            EquationProgressBarUI bar = Instantiate(barPrefab, barsParent);
            List<int> thresholdLevels = categoryData.AchievmentThresholds;
            bar.Setup(type, previousScore, newScore, thresholdLevels);
            spawnedBars.Add(bar);
        }
        
        moneyText.text = $"{moneyGained}";
    }

    private void ClearBars()
    {
        for (int i = 0; i < spawnedBars.Count; i++)
        {
            if (spawnedBars[i] != null)
                Destroy(spawnedBars[i].gameObject);
        }

        spawnedBars.Clear();
    }

    public void BackToHub()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("HubScene");
    }
    
    public void Retry()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Game");
    }
    
    public void OpenSettings()
    {
        ServiceLocator.Instance.UIManager.SettingsUI.Show();
    }
}