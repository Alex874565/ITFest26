using UnityEngine;
using DG.Tweening;
using TMPro;

public class DrawTutorialOverlay : MonoBehaviour
{
    [Header("Guide Text")]
    [SerializeField] private TMP_Text guideText;
    [SerializeField] private CanvasGroup guideCanvasGroup;
    [SerializeField] private string tutorialNumber = "2";
    [SerializeField] private float guideAlpha = 0.22f;

    [Header("Hand")]
    [SerializeField] private RectTransform handImage;
    [SerializeField] private CanvasGroup handCanvasGroup;
    [SerializeField] private float handAlpha = 0.95f;

    [Header("Path")]
    [SerializeField] private RectTransform[] pathPoints;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 1.4f;
    [SerializeField] private float startDelay = 0.2f;
    [SerializeField] private float loopDelay = 0.35f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool hideOnFirstInput = true;
    [SerializeField] private float handScalePunch = 0.06f;

    private Sequence _sequence;
    private bool _hiddenPermanently;

    private void Awake()
    {

        PrepareVisualState();
        ApplyText();
    }

    private void Start()
    {
        
        if (ServiceLocator.Instance != null && ServiceLocator.Instance.InputManager != null)
            ServiceLocator.Instance.InputManager.OnPressStarted += HandlePressStarted;
    }

    private void OnEnable()
    {
        if (playOnEnable && !_hiddenPermanently)
            ShowAndPlay();
    }

    private void OnDisable()
    {
        KillSequence();
    }

    private void OnDestroy()
    {
        KillSequence();

        if (ServiceLocator.Instance != null && ServiceLocator.Instance.InputManager != null)
            ServiceLocator.Instance.InputManager.OnPressStarted -= HandlePressStarted;
    }

    public void SetNumber(string value)
    {
        tutorialNumber = value;
        ApplyText();
    }

    public void SetPath(RectTransform[] newPath)
    {
        pathPoints = newPath;
    }

    public void ResetTutorial()
    {
        _hiddenPermanently = false;
        ShowAndPlay();
    }

    public void ShowAndPlay()
    {
        _hiddenPermanently = false;
        ShowInstant();
        Play();
    }

    public void ShowInstant()
    {
        KillSequence();
        ApplyText();

        if (guideText != null)
            guideText.gameObject.SetActive(true);

        if (guideCanvasGroup != null)
        {
            guideCanvasGroup.alpha = guideAlpha;
            guideCanvasGroup.interactable = false;
            guideCanvasGroup.blocksRaycasts = false;
        }

        if (handImage != null)
        {
            handImage.gameObject.SetActive(true);
            handImage.localScale = Vector3.one;

            if (pathPoints != null && pathPoints.Length > 0 && pathPoints[0] != null)
                handImage.anchoredPosition = pathPoints[0].anchoredPosition;
        }

        if (handCanvasGroup != null)
        {
            handCanvasGroup.alpha = handAlpha;
            handCanvasGroup.interactable = false;
            handCanvasGroup.blocksRaycasts = false;
        }
    }

    public void Play()
    {
        KillSequence();

        if (handImage == null || pathPoints == null || pathPoints.Length == 0)
            return;

        ShowInstant();

        _sequence = DOTween.Sequence();
        _sequence.AppendInterval(startDelay);

        float segmentDuration = pathPoints.Length > 1
            ? moveDuration / (pathPoints.Length - 1)
            : moveDuration;

        for (int i = 1; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] == null)
                continue;

            Vector2 target = pathPoints[i].anchoredPosition;

            _sequence.Append(
                handImage.DOAnchorPos(target, segmentDuration).SetEase(Ease.Linear));

            _sequence.Join(
                handImage.DOPunchScale(Vector3.one * handScalePunch, segmentDuration * 0.5f, 1, 0f));
        }

        if (loop)
        {
            _sequence.AppendInterval(loopDelay);
            _sequence.OnComplete(() =>
            {
                if (!_hiddenPermanently && isActiveAndEnabled)
                    Play();
            });
        }
    }

    public void Hide(bool permanent = true)
    {
        _hiddenPermanently = permanent;
        KillSequence();

        Sequence hideSequence = DOTween.Sequence();

        if (guideCanvasGroup != null)
            hideSequence.Join(guideCanvasGroup.DOFade(0f, fadeDuration));

        if (handCanvasGroup != null)
            hideSequence.Join(handCanvasGroup.DOFade(0f, fadeDuration));

        hideSequence.OnComplete(() =>
        {
            if (guideText != null)
                guideText.gameObject.SetActive(false);

            if (handImage != null)
                handImage.gameObject.SetActive(false);
        });
    }

    private void HandlePressStarted()
    {
        if (!hideOnFirstInput || _hiddenPermanently)
            return;

        Hide(true);
    }

    private void ApplyText()
    {
        if (guideText == null)
            return;

        guideText.text = tutorialNumber;
    }

    private void PrepareVisualState()
    {
        if (guideCanvasGroup != null)
        {
            guideCanvasGroup.interactable = false;
            guideCanvasGroup.blocksRaycasts = false;
        }

        if (handCanvasGroup != null)
        {
            handCanvasGroup.interactable = false;
            handCanvasGroup.blocksRaycasts = false;
        }
    }

    private void KillSequence()
    {
        if (_sequence != null && _sequence.IsActive())
        {
            _sequence.Kill();
            _sequence = null;
        }
    }
}