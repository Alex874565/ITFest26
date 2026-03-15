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
        EquationCategoryData categoryData = equationsDatabase.GetEquationCategoryData(type);
        int equationLevel = SaveManager.Instance.EquationLevels.TryGetValue(type, out int eqLevel) ? eqLevel : 0;
        int equationScore = SaveManager.Instance.EquationHighScores.TryGetValue(type, out int highScore) ? highScore : 0;
        if (categoryData != null)
        {
            if (equationLevel < categoryData.AchievmentThresholds.Count - 1)
            {
                fill.fillAmount = Mathf.Clamp(equationScore / (float)categoryData.AchievmentThresholds[equationLevel], 0, 1);
                score.text = equationScore + " / " + categoryData.AchievmentThresholds[equationLevel];
                level.text = (equationLevel + 1).ToString();
            }
            else
            {
                fill.fillAmount = 1;
                score.text = equationScore.ToString();
                level.text = "MAX";
            }
        }
    }
}