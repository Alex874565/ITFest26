using UnityEngine;
using UnityEngine.UI;
using System;

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

    public Action OnTimeToEvaluatePassed;
    
    public Texture2D DrawTexture { get; private set; }
    public Texture2D MaskTexture { get; private set; }

    private RectTransform _rectTransform;
    private bool _wasDrawingLastFrame;

    private bool _isDrawing = false;

    private float _timeSinceDrawing = 0;
    private bool _evaluatedSinceDrawing = true;

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
}