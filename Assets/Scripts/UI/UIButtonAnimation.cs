using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class UIButtonAnimation : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [SerializeField] private float hoverMultiplier = 1.08f;
    [SerializeField] private float pressMultiplier = 0.9f;
    [SerializeField] private float duration = 0.15f;

    [SerializeField] private Vector3 originalScale = Vector3.one;

    private Tween currentTween;
    private Button button;
    private bool isHovered;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void SetBaseScale(Vector3 scale)
    {
        originalScale = scale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
            return;

        isHovered = true;

        currentTween?.Kill();

        currentTween = transform.DOScale(originalScale * hoverMultiplier, duration)
            .SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        currentTween?.Kill();

        currentTween = transform.DOScale(originalScale, duration)
            .SetEase(Ease.OutCubic);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
            return;

        currentTween?.Kill();

        currentTween = transform.DOScale(originalScale * pressMultiplier, duration * 0.6f)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
            return;

        currentTween?.Kill();

        float targetMultiplier = isHovered ? hoverMultiplier : 1f;

        currentTween = transform.DOScale(originalScale * targetMultiplier, duration)
            .SetEase(Ease.OutBack);
    }

    private void OnDisable()
    {
        currentTween?.Kill();
        transform.localScale = originalScale;
    }
}