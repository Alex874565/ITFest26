using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class Drawer : MonoBehaviour
{
    [SerializeField] private float timeToEvaluate;

    [Header("UI")]
    [SerializeField] private RawImage drawSurface;

    [Header("Texture")]
    [SerializeField] private int textureWidth = 1024;
    [SerializeField] private int textureHeight = 1024;

    [Header("Brush")]
    [SerializeField] private DrawerBrush brush;
    [SerializeField] private Color clearColor = Color.black;
    [SerializeField] private Color clearMaskColor = new Color(0f, 0f, 0f, 0f);

    [Header("Recognized Number Pop")]
    [SerializeField] private RectTransform popAnchor; // optional: parent canvas/container for the number UI
    [SerializeField] private float popMoveUp = 80f;
    [SerializeField] private float popDuration = 0.15f;
    [SerializeField] private float settleDuration = 0.08f;
    [SerializeField] private float wiggleDuration = 0.2f;
    [SerializeField] private float popScaleOvershoot = 1.25f;
    [SerializeField] private float popRotationPunch = 10f;

    public Action OnTimeToEvaluatePassed;

    public Texture2D DrawTexture { get; private set; }
    public Texture2D MaskTexture { get; private set; }

    private RectTransform _rectTransform;
    private bool _wasDrawingLastFrame;
    private bool _isDrawing = false;

    private float _timeSinceDrawing = 0;
    private bool _evaluatedSinceDrawing = true;

    private Vector2 _lastDrawScreenPosition;
    private bool _hasLastDrawPosition;

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
        {
            brush.BeginStroke();
        }

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

    /// <summary>
    /// Plays a cute pop-out animation for the recognized number UI.
    /// Pass in the RectTransform of the number label / bubble you want animated.
    /// </summary>
    public void PlayRecognizedNumberPop(RectTransform numberUI, CanvasGroup canvasGroup = null)
    {
        if (numberUI == null)
            return;

        RectTransform parentRect = popAnchor != null ? popAnchor : numberUI.parent as RectTransform;
        if (parentRect == null)
            return;

        Vector2 screenPos = _hasLastDrawPosition
            ? _lastDrawScreenPosition
            : RectTransformUtility.WorldToScreenPoint(null, drawSurface.rectTransform.position);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPos,
                null,
                out Vector2 localPoint))
        {
            localPoint = Vector2.zero;
        }

        numberUI.SetParent(parentRect, false);
        numberUI.anchoredPosition = localPoint;
        numberUI.localScale = Vector3.zero;
        numberUI.localRotation = Quaternion.identity;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        numberUI.DOKill();
        if (canvasGroup != null)
            canvasGroup.DOKill();

        Sequence seq = DOTween.Sequence();

        if (canvasGroup != null)
            seq.Join(canvasGroup.DOFade(1f, 0.08f));

        seq.Append(numberUI.DOScale(popScaleOvershoot, popDuration).SetEase(Ease.OutBack))
            .Join(numberUI.DOAnchorPosY(localPoint.y + popMoveUp, popDuration + settleDuration).SetEase(Ease.OutQuad))
            .Append(numberUI.DOScale(0.92f, settleDuration).SetEase(Ease.OutQuad))
            .Append(numberUI.DOScale(1f, settleDuration).SetEase(Ease.OutQuad))
            .Append(numberUI.DOPunchRotation(new Vector3(0f, 0f, popRotationPunch), wiggleDuration, 8, 0.6f))
            .AppendInterval(0.35f);

        if (canvasGroup != null)
        {
            seq.Append(canvasGroup.DOFade(0f, 0.2f));
        }
        else
        {
            seq.Append(numberUI.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        }
    }
}