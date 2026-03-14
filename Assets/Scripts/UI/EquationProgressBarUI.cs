using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class EquationProgressBarUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image fillImage;

    [Header("Animation")]
    [SerializeField] private float secondsPerPoint = 0.05f;
    [SerializeField] private float minSegmentDuration = 0.4f;
    [SerializeField] private float maxAnimationDuration = 1.5f;
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float popScaleMultiplier = 1.2f;
    [SerializeField] private Ease fillEase = Ease.InOutQuad;
    [SerializeField] private Ease popEase = Ease.OutBack;

    private Sequence _sequence;
    private Vector3 _levelTextOriginalScale;

    private void Awake()
    {
        _levelTextOriginalScale = levelText.rectTransform.localScale;
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        levelText.rectTransform.DOKill();
        fillImage.DOKill();
    }

    public void Setup(EquationType equationType, int previousScore, int newScore, List<int> thresholds)
    {
        _sequence?.Kill();
        levelText.rectTransform.DOKill();
        fillImage.DOKill();

        titleText.text = equationType.ToString();

        if (thresholds == null || thresholds.Count == 0)
        {
            fillImage.fillAmount = 0f;
            levelText.text = "0";
            scoreText.text = $"{newScore}";
            return;
        }

        UpdateVisualsFromTotalScore(previousScore, thresholds);

        if (newScore <= previousScore)
        {
            UpdateVisualsFromTotalScore(newScore, thresholds);
            return;
        }

        float tweenProgress = 0f;
        float displayedScore = previousScore;
        int lastLevel = GetLevel(previousScore, thresholds);

        float duration = Mathf.Clamp(
            (newScore - previousScore) * secondsPerPoint,
            minSegmentDuration,
            maxAnimationDuration
        );

        _sequence = DOTween.Sequence().SetUpdate(true);

        _sequence.Append(
            DOTween.To(
                    () => tweenProgress,
                    x =>
                    {
                        tweenProgress = x;
                        displayedScore = Mathf.Lerp(previousScore, newScore, tweenProgress);

                        int currentDisplayedScore = Mathf.FloorToInt(displayedScore);
                        int currentLevel = GetLevel(currentDisplayedScore, thresholds);

                        while (lastLevel < currentLevel)
                        {
                            lastLevel++;
                            PlayLevelPop();
                        }

                        UpdateVisualsFromTotalScore(displayedScore, thresholds);
                    },
                    1f,
                    duration)
                .SetEase(fillEase)
                .SetUpdate(true)
        );

        _sequence.AppendCallback(() =>
        {
            UpdateVisualsFromTotalScore(newScore, thresholds);
        });
    }

    private void UpdateVisualsFromTotalScore(int totalScore, List<int> thresholds)
    {
        UpdateVisualsFromTotalScore((float)totalScore, thresholds);
    }

    private void UpdateVisualsFromTotalScore(float totalScore, List<int> thresholds)
    {
        int roundedScore = Mathf.FloorToInt(totalScore);
        int level = GetLevel(roundedScore, thresholds);
        bool isMaxed = level >= thresholds.Count;

        if (isMaxed)
        {
            fillImage.fillAmount = 1f;
            levelText.text = "MAX";
            scoreText.text = $"{roundedScore}";
            return;
        }

        int previousThreshold = level == 0 ? 0 : thresholds[level - 1];
        int nextThreshold = thresholds[level];

        float progressInLevel = totalScore - previousThreshold;
        float requiredInLevel = nextThreshold - previousThreshold;

        float fill = requiredInLevel > 0f ? progressInLevel / requiredInLevel : 0f;

        levelText.text = $"{level}";
        scoreText.text = $"{roundedScore}/{nextThreshold}";
        fillImage.fillAmount = Mathf.Clamp01(fill);
    }

    private int GetLevel(int score, List<int> thresholds)
    {
        int level = 0;

        for (int i = 0; i < thresholds.Count; i++)
        {
            if (score >= thresholds[i])
                level++;
            else
                break;
        }

        return level;
    }

    private void PlayLevelPop()
    {
        RectTransform rect = levelText.rectTransform;

        rect.DOKill();
        rect.localScale = _levelTextOriginalScale;

        Sequence popSequence = DOTween.Sequence().SetUpdate(true);
        popSequence.Append(
            rect.DOScale(_levelTextOriginalScale * popScaleMultiplier, popDuration * 0.5f)
                .SetEase(popEase)
                .SetUpdate(true)
        );
        popSequence.Append(
            rect.DOScale(_levelTextOriginalScale, popDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
        );
    }
}