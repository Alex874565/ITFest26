using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;
using TMPro;

public class Drawer : MonoBehaviour
{
    [SerializeField] private float timeToEvaluate;

    [Header("UI")]
    [SerializeField] private RawImage drawSurface;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Texture")]
    [SerializeField] private int textureWidth = 1024;
    [SerializeField] private int textureHeight = 1024;

    [Header("Brush")]
    [SerializeField] private DrawerBrush brush;
    [SerializeField] private Color clearColor = Color.black;
    [SerializeField] private Color clearMaskColor = new Color(0f, 0f, 0f, 0f);

    [Header("Recognized Number Pop")]
    [SerializeField] private RectTransform popAnchor;
    [SerializeField] private float popMoveUp = 82f;
    [SerializeField] private float popDuration = 0.08f;
    [SerializeField] private float settleDuration = 0.06f;
    [SerializeField] private float wiggleDuration = 0.14f;
    [SerializeField] private float popScaleOvershoot = 1.28f;
    [SerializeField] private float popRotationPunch = 8f;

    [Header("Recognized Number Colors")]
    [SerializeField] private Color correctTint = new Color(0.36f, 0.60f, 0.34f, 1f); // warm leaf green
    [SerializeField] private Color wrongTint = new Color(0.78f, 0.36f, 0.32f, 1f);   // painterly red

    [Header("Recognized Number Chalk Look")]
    [SerializeField] private Texture2D chalkNoiseTexture;
    [SerializeField] private float chalkFaceDilate = -0.03f;
    [SerializeField] private float chalkOutlineSoftness = 0.05f;
    [SerializeField] private float chalkTextureScale = 3f;
    [SerializeField] private float chalkUnderlaySoftness = 0.05f;
    [SerializeField] private float chalkUnderlayDilate = 0.0f;
    [SerializeField] private Color chalkShadowColor = new Color(0f, 0f, 0f, 0.18f);

    public Action OnTimeToEvaluatePassed;

    public Texture2D DrawTexture { get; private set; }
    public Texture2D MaskTexture { get; private set; }

    private RectTransform _rectTransform;
    private bool _wasDrawingLastFrame;
    private bool _isDrawing;

    private float _timeSinceDrawing;
    private bool _evaluatedSinceDrawing = true;

    private Vector2 _lastDrawScreenPosition;
    private bool _hasLastDrawPosition;

    private Sequence _recognizedNumberSequence;

    private void Awake()
    {
        ServiceLocator.Instance.InputManager.OnPressStarted += OnPressedStarted;
        ServiceLocator.Instance.InputManager.OnPressReleased += OnPressedReleased;

        _rectTransform = drawSurface.rectTransform;

        DrawTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        DrawTexture.filterMode = FilterMode.Point;
        DrawTexture.wrapMode = TextureWrapMode.Clamp;

        MaskTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        MaskTexture.filterMode = FilterMode.Point;
        MaskTexture.wrapMode = TextureWrapMode.Clamp;

        drawSurface.texture = DrawTexture;

        Clear();
    }

    private void OnDestroy()
    {
        if (_recognizedNumberSequence != null && _recognizedNumberSequence.IsActive())
            _recognizedNumberSequence.Kill();

        if (ServiceLocator.Instance != null && ServiceLocator.Instance.InputManager != null)
        {
            ServiceLocator.Instance.InputManager.OnPressStarted -= OnPressedStarted;
            ServiceLocator.Instance.InputManager.OnPressReleased -= OnPressedReleased;
        }
    }

    private void Update()
    {
        if (!_isDrawing)
        {
            if (!_evaluatedSinceDrawing)
            {
                _timeSinceDrawing += Time.deltaTime;
                if (_timeSinceDrawing > timeToEvaluate)
                {
                    OnTimeToEvaluatePassed?.Invoke();
                    _evaluatedSinceDrawing = true;
                    Clear();
                }
            }

            _wasDrawingLastFrame = false;
            return;
        }

        if (!TryGetTexturePixelPosition(Input.mousePosition, out Vector2 pixelPos))
            return;

        _lastDrawScreenPosition = Input.mousePosition;
        _hasLastDrawPosition = true;

        if (!_wasDrawingLastFrame)
            brush.BeginStroke();

        brush.Draw(DrawTexture, MaskTexture, pixelPos);
        _wasDrawingLastFrame = true;
    }

    public void Clear()
    {
        Color[] visiblePixels = new Color[textureWidth * textureHeight];
        Color[] maskPixels = new Color[textureWidth * textureHeight];

        for (int i = 0; i < visiblePixels.Length; i++)
        {
            visiblePixels[i] = clearColor;
            maskPixels[i] = clearMaskColor;
        }

        DrawTexture.SetPixels(visiblePixels);
        DrawTexture.Apply();

        MaskTexture.SetPixels(maskPixels);
        MaskTexture.Apply();
    }

    private bool TryGetTexturePixelPosition(Vector2 screenPos, out Vector2 pixelPos)
    {
        pixelPos = default;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                screenPos,
                null,
                out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = _rectTransform.rect;
        if (!rect.Contains(localPoint))
            return false;

        float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

        float x = normalizedX * (textureWidth - 1);
        float y = normalizedY * (textureHeight - 1);

        pixelPos = new Vector2(x, y);
        return true;
    }

    private void OnPressedStarted()
    {
        _isDrawing = true;
    }

    private void OnPressedReleased()
    {
        _isDrawing = false;
        _timeSinceDrawing = 0f;
        _evaluatedSinceDrawing = false;
    }

    public void PlayRecognizedNumberPop(
        RectTransform numberUI,
        bool isCorrect,
        CanvasGroup popupCanvasGroup = null,
        Graphic graphic = null,
        TMP_Text tmpText = null)
    {
        if (numberUI == null)
            return;

        RectTransform parentRect = popAnchor != null ? popAnchor : numberUI.parent as RectTransform;
        if (parentRect == null)
            return;

        if (popupCanvasGroup == null)
            popupCanvasGroup = numberUI.GetComponent<CanvasGroup>();

        if (tmpText == null)
            tmpText = numberUI.GetComponent<TMP_Text>();

        if (graphic == null && tmpText == null)
            graphic = numberUI.GetComponent<Graphic>();

        Canvas canvas = parentRect.GetComponentInParent<Canvas>();
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        Vector2 screenPos = _hasLastDrawPosition
            ? _lastDrawScreenPosition
            : RectTransformUtility.WorldToScreenPoint(uiCamera, drawSurface.rectTransform.position);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPos,
                uiCamera,
                out Vector2 localPoint))
        {
            localPoint = Vector2.zero;
        }

        Rect parentBounds = parentRect.rect;
        localPoint.x = Mathf.Clamp(localPoint.x, parentBounds.xMin + 40f, parentBounds.xMax - 40f);
        localPoint.y = Mathf.Clamp(localPoint.y, parentBounds.yMin + 40f, parentBounds.yMax - 40f);

        // Save original visual state
        Color originalColor = Color.white;
        if (tmpText != null)
            originalColor = tmpText.color;
        else if (graphic != null)
            originalColor = graphic.color;

        Material originalTmpMaterial = null;
        Material runtimeTmpMaterial = null;

        void RestoreVisualState()
        {
            numberUI.localRotation = Quaternion.identity;
            numberUI.localScale = Vector3.one;
            numberUI.anchoredPosition = localPoint;

            if (popupCanvasGroup != null)
                popupCanvasGroup.alpha = 1f;
        }

        void FinalCleanupAndHide()
        {
            RestoreVisualState();
            numberUI.gameObject.SetActive(false);

            if (graphic != null)
                graphic.color = originalColor;

            if (tmpText != null)
            {
                tmpText.color = originalColor;

                if (originalTmpMaterial != null)
                    tmpText.fontMaterial = originalTmpMaterial;

                if (runtimeTmpMaterial != null)
                    Destroy(runtimeTmpMaterial);
            }
        }

        // If replacing an existing popup, reset it instantly instead of hiding first.
        if (_recognizedNumberSequence != null)
        {
            _recognizedNumberSequence.Kill(false);
            _recognizedNumberSequence = null;

            RestoreVisualState();
            numberUI.gameObject.SetActive(true);
        }

        numberUI.SetParent(parentRect, false);
        numberUI.gameObject.SetActive(true);
        numberUI.anchoredPosition = localPoint;
        numberUI.localScale = isCorrect ? Vector3.one * 0.42f : Vector3.one * 0.55f;
        numberUI.localRotation = Quaternion.identity;

        if (popupCanvasGroup != null)
            popupCanvasGroup.alpha = isCorrect ? 0.18f : 1f;

        Color targetTint = isCorrect ? correctTint : wrongTint;

        if (graphic != null)
            graphic.color = targetTint;

        if (tmpText != null)
        {
            tmpText.color = isCorrect ? Color.white : targetTint;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.enableWordWrapping = false;

            originalTmpMaterial = tmpText.fontMaterial;
            runtimeTmpMaterial = new Material(originalTmpMaterial);

            if (runtimeTmpMaterial.HasProperty(ShaderUtilities.ID_FaceDilate))
                runtimeTmpMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, chalkFaceDilate);

            if (runtimeTmpMaterial.HasProperty(ShaderUtilities.ID_OutlineSoftness))
                runtimeTmpMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, chalkOutlineSoftness);

            if (runtimeTmpMaterial.HasProperty(ShaderUtilities.ID_UnderlayColor))
                runtimeTmpMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, chalkShadowColor);

            if (runtimeTmpMaterial.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
                runtimeTmpMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, chalkUnderlaySoftness);

            if (runtimeTmpMaterial.HasProperty(ShaderUtilities.ID_UnderlayDilate))
                runtimeTmpMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, chalkUnderlayDilate);

            if (chalkNoiseTexture != null && runtimeTmpMaterial.HasProperty(ShaderUtilities.ID_FaceTex))
            {
                runtimeTmpMaterial.SetTexture(ShaderUtilities.ID_FaceTex, chalkNoiseTexture);
                runtimeTmpMaterial.mainTextureScale = new Vector2(chalkTextureScale, chalkTextureScale);
            }

            tmpText.fontMaterial = runtimeTmpMaterial;
        }

        _recognizedNumberSequence = DOTween.Sequence();

        if (isCorrect)
        {
            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            Vector2 riseTarget = localPoint + new Vector2(14f * side, popMoveUp);

            _recognizedNumberSequence.Append(
                numberUI.DOScale(popScaleOvershoot, popDuration).SetEase(Ease.OutBack));

            if (popupCanvasGroup != null)
            {
                _recognizedNumberSequence.Join(
                    popupCanvasGroup.DOFade(1f, 0.14f).SetEase(Ease.OutSine));
            }

            if (tmpText != null)
            {
                tmpText.color = Color.Lerp(Color.white, correctTint, 0.35f);
                _recognizedNumberSequence.Join(
                    tmpText.DOColor(correctTint, 0.16f).SetEase(Ease.OutSine));
            }

            _recognizedNumberSequence.Join(
                numberUI.DOAnchorPos(riseTarget, 0.36f).SetEase(Ease.OutCubic));

            _recognizedNumberSequence.Join(
                numberUI.DORotate(new Vector3(0f, 0f, 8f * side), 0.16f).SetEase(Ease.OutQuad));

            _recognizedNumberSequence.Append(
                numberUI.DOScale(1f, settleDuration).SetEase(Ease.InOutQuad));

            _recognizedNumberSequence.Append(
                numberUI.DOPunchRotation(new Vector3(0f, 0f, popRotationPunch * side), wiggleDuration, 8, 0.7f));

            _recognizedNumberSequence.Join(
                numberUI.DOPunchAnchorPos(new Vector2(0f, 8f), wiggleDuration, 6, 0.45f));

            _recognizedNumberSequence.AppendInterval(0.18f);

            if (popupCanvasGroup != null)
                _recognizedNumberSequence.Append(
                    popupCanvasGroup.DOFade(0f, 0.18f).SetEase(Ease.OutQuad));
            else
                _recognizedNumberSequence.Append(
                    numberUI.DOScale(0f, 0.18f).SetEase(Ease.InBack));
        }
        else
        {
            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            Vector2 dropTarget = localPoint + new Vector2(0f, -70f);

            _recognizedNumberSequence.Append(
                numberUI.DOScale(1.24f, 0.12f).SetEase(Ease.OutBack));

            _recognizedNumberSequence.Append(
                numberUI.DOScale(1f, 0.08f).SetEase(Ease.OutQuad));

            _recognizedNumberSequence.Append(
                numberUI.DOScale(new Vector3(1.08f, 0.9f, 1f), 0.08f).SetEase(Ease.OutQuad));

            _recognizedNumberSequence.Append(
                numberUI.DOScale(Vector3.one, 0.08f).SetEase(Ease.OutQuad));

            _recognizedNumberSequence.Join(
                numberUI.DOPunchAnchorPos(new Vector2(22f * side, 0f), 0.22f, 8, 0.9f));

            _recognizedNumberSequence.Join(
                numberUI.DOPunchRotation(new Vector3(0f, 0f, 10f * side), 0.22f, 6, 0.8f));

            _recognizedNumberSequence.Append(
                numberUI.DOAnchorPos(dropTarget, 0.26f).SetEase(Ease.InQuad));

            _recognizedNumberSequence.Join(
                numberUI.DORotate(new Vector3(0f, 0f, 4f * side), 0.26f).SetEase(Ease.OutQuad));

            if (popupCanvasGroup != null)
            {
                _recognizedNumberSequence.Join(
                    popupCanvasGroup.DOFade(0f, 0.20f).SetEase(Ease.OutQuad));
            }
            else
            {
                _recognizedNumberSequence.Join(
                    numberUI.DOScale(0.92f, 0.20f).SetEase(Ease.InQuad));
            }
        }

        _recognizedNumberSequence.OnComplete(() =>
        {
            FinalCleanupAndHide();
            _recognizedNumberSequence = null;
        });

        _recognizedNumberSequence.OnKill(() =>
        {
            // When interrupted by a new popup, don't hide first.
            RestoreVisualState();
            _recognizedNumberSequence = null;
        });
    }
}