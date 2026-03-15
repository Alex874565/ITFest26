using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquationUI : MonoBehaviour
{
    [SerializeField] private EquationType type;
    [SerializeField] private Image fill;
    [SerializeField] private TextMeshProUGUI score;
    [SerializeField] private TextMeshProUGUI level;

    [SerializeField] private EquationsCategoriesDatabase equationsDatabase;

    private void Start()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager.Instance is null");
            return;
        }

        EquationCategoryData categoryData = equationsDatabase.GetEquationCategoryData(type);
        if (categoryData == null)
            return;

        int equationLevel = SaveManager.Instance.EquationLevels.TryGetValue(type, out int eqLevel) ? eqLevel : 0;
        int equationScore = SaveManager.Instance.EquationHighScores.TryGetValue(type, out int highScore) ? highScore : 0;

        if (equationLevel < categoryData.AchievmentThresholds.Count - 1)
        {
            int threshold = categoryData.AchievmentThresholds[equationLevel];
            fill.fillAmount = threshold > 0 ? Mathf.Clamp(equationScore / (float)threshold, 0f, 1f) : 0f;
            score.text = equationScore + " / " + threshold;
            level.text = (equationLevel + 1).ToString();

            Debug.Log(equationScore);
            Debug.Log(threshold);
            Debug.Log(fill.fillAmount);
        }
        else
        {
            fill.fillAmount = 1f;
            score.text = equationScore.ToString();
            level.text = "MAX";
        }
    }
}