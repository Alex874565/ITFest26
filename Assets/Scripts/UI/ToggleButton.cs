using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class ToggleButton : MonoBehaviour
{
    [SerializeField] private EquationType equationType;
    [SerializeField] private Button button;
    [SerializeField] private Image targetImage;

    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    public bool IsOn { get; private set; }

    public event Action OnToggled;

    private void Awake()
    {
        button.onClick.AddListener(Toggle);
    }

    private void Start()
    {
        IsOn = ServiceLocator.Instance.PlayerManager.SelectedEquations.Contains(equationType);
        UpdateVisual();
    }

    private void Toggle()
    {
        IsOn = !IsOn;
        ServiceLocator.Instance.PlayerManager.ToggleSelectEquation(equationType);
        UpdateVisual();
        OnToggled?.Invoke();
    }

    private void UpdateVisual()
    {
        targetImage.DOKill();
        targetImage.transform.DOKill();
        Sprite newSprite = IsOn ? onSprite : offSprite;

        Sequence seq = DOTween.Sequence();

        seq.Append(targetImage.DOFade(0.5f, 0.08f));
        seq.Join(targetImage.transform.DOScale(0.9f, 0.08f));

        seq.AppendCallback(() =>
        {
            targetImage.sprite = newSprite;
        });

        seq.Append(targetImage.DOFade(1f, 0.12f));
        seq.Join(targetImage.transform.DOScale(1f, 0.12f));
    }
}