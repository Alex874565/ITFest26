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

    private void OnDestroy()
    {
        button.onClick.RemoveListener(Toggle);
    }

    private void Start()
    {
        RefreshFromState();
    }

    public void RefreshFromState()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager.Instance is null");
            return;
        }

        IsOn = SaveManager.Instance.IsEquationSelected(equationType);
        Debug.Log($"{equationType} RefreshFromState -> {IsOn}");
        UpdateVisualImmediate();
    }

    private void Toggle()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager.Instance is null");
            return;
        }

        ServiceLocator.Instance.PlayerManager.ToggleSelectEquation(equationType);

        IsOn = SaveManager.Instance.IsEquationSelected(equationType);
        Debug.Log($"{equationType} Toggle -> {IsOn}");

        UpdateVisualImmediate();
        OnToggled?.Invoke();
    }

    private void UpdateVisualImmediate()
    {
        if (targetImage == null)
        {
            Debug.LogError("Target image is null");
            return;
        }

        targetImage.DOKill();
        targetImage.transform.DOKill();

        Color c = targetImage.color;
        c.a = 1f;
        targetImage.color = c;

        targetImage.transform.localScale = Vector3.one;
        targetImage.sprite = IsOn ? onSprite : offSprite;

        targetImage.SetAllDirty();
    }
}