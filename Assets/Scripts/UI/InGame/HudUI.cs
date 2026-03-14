using TMPro;
using UnityEngine;
using DG.Tweening;

public class HudUI : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private RectTransform coinIcon;

    [Header("Coin Bounce")]
    [SerializeField] private float jumpHeight = 45f;
    [SerializeField] private float reboundHeight = 12f;
    [SerializeField] private float upDuration = 0.12f;
    [SerializeField] private float downDuration = 0.10f;
    [SerializeField] private float reboundUpDuration = 0.07f;
    [SerializeField] private float reboundDownDuration = 0.06f;

    [Header("Text Bounce")]
    [SerializeField] private float textPunch = 0.25f;
    [SerializeField] private float textDuration = 0.30f;

    private float coinStartY;

    private void Awake()
    {
        coinStartY = coinIcon.anchoredPosition.y;
    }

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
        scoreText.text = score.ToString();
        PlayAnimation();
    }

    private void PlayAnimation()
    {
        coinIcon.DOKill();
        scoreText.transform.DOKill();

        coinIcon.anchoredPosition = new Vector2(coinIcon.anchoredPosition.x, coinStartY);
        scoreText.transform.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();

        // Main jump
        seq.Append(
            coinIcon.DOAnchorPosY(coinStartY + jumpHeight, upDuration)
                .SetEase(Ease.OutQuad)
        );

        // Landing
        seq.Append(
            coinIcon.DOAnchorPosY(coinStartY, downDuration)
                .SetEase(Ease.InQuad)
        );

        // Small rebound
        seq.Append(
            coinIcon.DOAnchorPosY(coinStartY + reboundHeight, reboundUpDuration)
                .SetEase(Ease.OutQuad)
        );

        // Final settle
        seq.Append(
            coinIcon.DOAnchorPosY(coinStartY, reboundDownDuration)
                .SetEase(Ease.InQuad)
        );

        scoreText.transform
            .DOPunchScale(new Vector3(textPunch, textPunch, 0f), textDuration, 8, 0.8f)
            .SetDelay(0.03f);
    }
}