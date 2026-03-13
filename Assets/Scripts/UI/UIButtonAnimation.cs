using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class UIButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Tween currentTween;
    private Vector3 originalScale;

    [SerializeField] private float hoverMultiplier = 1.08f;
    [SerializeField] private float duration = 0.2f;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Button button = GetComponent<Button>();
        if (button != null && !button.interactable)
            return;

        currentTween?.Kill();

        currentTween = transform.DOScale(originalScale * hoverMultiplier, duration)
                                .SetEase(Ease.OutBack)
                                .SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        currentTween?.Kill();

        currentTween = transform.DOScale(originalScale, duration)
                                .SetEase(Ease.OutCubic)
                                .SetUpdate(true);
    }
}