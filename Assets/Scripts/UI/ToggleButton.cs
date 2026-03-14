using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image targetImage;

    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    public bool IsOn { get; private set; }

    private void Awake()
    {
        button.onClick.AddListener(Toggle);
        UpdateVisual();
    }

    private void Toggle()
    {
        IsOn = !IsOn;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        targetImage.sprite = IsOn ? onSprite : offSprite;
    }
}