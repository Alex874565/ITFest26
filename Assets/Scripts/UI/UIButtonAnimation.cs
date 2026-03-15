using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class UIButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverMultiplier = 1.08f;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private Vector3 originalScale = Vector3.one;

    private Tween currentTween;
    private Button button;

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

        currentTween?.Kill();

        currentTween = transform.DOScale(originalScale * hoverMultiplier, duration)
            .SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        currentTween?.Kill();

        currentTween = transform.DOScale(originalScale, duration)
            .SetEase(Ease.OutCubic);
    }

    private void OnDisable()
    {
        currentTween?.Kill();
        transform.localScale = originalScale;
    }
}