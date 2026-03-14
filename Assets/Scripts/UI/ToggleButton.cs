using UnityEngine;
using UnityEngine.UI;
using System;

public class ToggleButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image targetImage;

    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    public bool IsOn { get; private set; }

    public event Action OnToggled;

    private void Awake()
    {
        button.onClick.AddListener(Toggle);
        UpdateVisual();
    }

    private void Toggle()
    {
        IsOn = !IsOn;
        UpdateVisual();
        OnToggled?.Invoke();
    }

    public void SetState(bool value)
    {
        IsOn = value;
        UpdateVisual();
        OnToggled?.Invoke();
    }

    private void UpdateVisual()
    {
        targetImage.sprite = IsOn ? onSprite : offSprite;
    }
}