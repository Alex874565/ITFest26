using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquationProgressBarUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image fillImage;

    [Header("Animation")]
    [SerializeField] private float secondsPerPoint = 0.05f;
    [SerializeField] private float minSegmentDuration = 0.15f;
    [SerializeField] private float maxSegmentDuration = 0.6f;
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float popScaleMultiplier = 1.2f;
    [SerializeField] private Ease fillEase = Ease.InOutQuad;
    [SerializeField] private Ease popEase = Ease.OutBack;

    private Sequence _sequence;
    private Tween _popTween;
    private Vector3 _levelTextOriginalScale;

    private void Awake()
    {
        _levelTextOriginalScale = levelText.rectTransform.localScale;
    }

    private void OnDisable()
    {
        KillTweens();
    }

    private void KillTweens()
    {
        _sequence?.Kill();
        _sequence = null;

        _popTween?.Kill();
        _popTween = null;

        levelText.rectTransform.DOKill();
        fillImage.DOKill();
    }

    public void Setup(EquationType equationType, int previousScore, int newScore, List<int> thresholds)
    {
        KillTweens();

        titleText.text = equationType.ToString();

        if (thresholds == null || thresholds.Count == 0)
        {
            fillImage.fillAmount = 0f;
            levelText.text = "0";
            scoreText.text = $"{newScore}";
            return;
        }

        // Always start from the previous score visually.
        UpdateVisualsFromTotalScore(previousScore, thresholds);

        if (newScore <= previousScore)
        {
            UpdateVisualsFromTotalScore(newScore, thresholds);
            return;
        }

        _sequence = DOTween.Sequence().SetUpdate(true);

        int currentScore = previousScore;
        int currentLevel = GetLevel(currentScore, thresholds);

        // Animate through each threshold one by one.
        while (currentLevel < thresholds.Count && newScore >= thresholds[currentLevel])
        {
            int targetScore = thresholds[currentLevel];
            AppendScoreSegment(currentScore, targetScore, thresholds);

            int reachedLevel = currentLevel + 1;
            _sequence.AppendCallback(() =>
            {
                UpdateVisualsFromTotalScore(targetScore, thresholds);
                PlayLevelPop();
            });

            currentScore = targetScore;
            currentLevel = reachedLevel;
        }

        // Animate the remainder after the last crossed threshold.
        if (currentScore < newScore)
        {
            AppendScoreSegment(currentScore, newScore, thresholds);
        }

        _sequence.AppendCallback(() =>
        {
            UpdateVisualsFromTotalScore(newScore, thresholds);
        });
    }

    private void AppendScoreSegment(int fromScore, int toScore, List<int> thresholds)
    {
        float displayedScore = fromScore;
        float duration = Mathf.Clamp(
            (toScore - fromScore) * secondsPerPoint,
            minSegmentDuration,
            maxSegmentDuration
        );

        _sequence.Append(
            DOTween.To(
                    () => displayedScore,
                    value =>
                    {
                        displayedScore = value;
                        UpdateVisualsFromTotalScore(displayedScore, thresholds);
                    },
                    toScore,
                    duration)
                .SetEase(fillEase)
                .SetUpdate(true)
        );
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

        _popTween?.Kill();
        rect.localScale = _levelTextOriginalScale;

        _popTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(
                rect.DOScale(_levelTextOriginalScale * popScaleMultiplier, popDuration * 0.5f)
                    .SetEase(popEase)
                    .SetUpdate(true)
            )
            .Append(
                rect.DOScale(_levelTextOriginalScale, popDuration * 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true)
            );
    }
}