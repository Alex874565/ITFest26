using UnityEngine;

public class ScreenScaler : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public BoxCollider2D targetCollider;
    public SpriteRenderer background;

    [Header("Design Resolution")]
    public float referenceWidth = 1920f;
    public float referenceHeight = 1080f;

    [Header("Camera")]
    [Tooltip("Orthographic size used in your original 1920x1080 desktop setup.")]
    public float referenceOrthographicSize = 5f;

    private Vector3 originalBackgroundScale;
    private Vector2 originalColliderSize;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("MobileScaler: No camera found.");
            return;
        }

        if (!cam.orthographic)
        {
            Debug.LogError("MobileScaler: This script requires an orthographic camera.");
            return;
        }

        if (background != null)
            originalBackgroundScale = background.transform.localScale;

        if (targetCollider != null)
            originalColliderSize = targetCollider.size;

        AdjustCamera();
        ScaleCollider();
        ScaleBackground();
    }
    
    int lastWidth;
    int lastHeight;

    void Start()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            AdjustCamera();
            ScaleCollider();
            ScaleBackground();
        }
    }

    private void AdjustCamera()
    {
        float referenceAspect = referenceWidth / referenceHeight;   // 1920 / 1080 = 16:9
        float currentAspect = (float)Screen.width / Screen.height;

        // Keep desktop framing for same/wider screens.
        // Expand vertically for taller/narrower screens.
        if (currentAspect < referenceAspect)
        {
            cam.orthographicSize = referenceOrthographicSize * (referenceAspect / currentAspect);
        }
        else
        {
            cam.orthographicSize = referenceOrthographicSize;
        }
    }

    private void ScaleCollider()
    {
        if (targetCollider == null)
            return;

        float referenceAspect = referenceWidth / referenceHeight;

        float referenceWorldHeight = referenceOrthographicSize * 2f;
        float referenceWorldWidth = referenceWorldHeight * referenceAspect;

        float currentWorldHeight = cam.orthographicSize * 2f;
        float currentWorldWidth = currentWorldHeight * cam.aspect;

        float widthMultiplier = currentWorldWidth / referenceWorldWidth;
        float heightMultiplier = currentWorldHeight / referenceWorldHeight;

        targetCollider.size = new Vector2(
            originalColliderSize.x * widthMultiplier,
            originalColliderSize.y * heightMultiplier
        );
    }

    private void ScaleBackground()
    {
        if (background == null || background.sprite == null)
            return;

        float referenceAspect = referenceWidth / referenceHeight;

        float referenceWorldHeight = referenceOrthographicSize * 2f;
        float referenceWorldWidth = referenceWorldHeight * referenceAspect;

        float currentWorldHeight = cam.orthographicSize * 2f;
        float currentWorldWidth = currentWorldHeight * cam.aspect;

        float widthMultiplier = currentWorldWidth / referenceWorldWidth;
        float heightMultiplier = currentWorldHeight / referenceWorldHeight;

        // Fill the screen while preserving your existing desktop scale
        float multiplier = Mathf.Max(widthMultiplier, heightMultiplier);

        background.transform.localScale = new Vector3(
            originalBackgroundScale.x * multiplier,
            originalBackgroundScale.y * multiplier,
            originalBackgroundScale.z
        );
    }
}
