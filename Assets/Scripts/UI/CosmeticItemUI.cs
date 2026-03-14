using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CosmeticItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;

    [SerializeField] private TextMeshProUGUI actionText;
    [SerializeField] private GameObject priceContainer;
    [SerializeField] private TextMeshProUGUI priceText;

    [Header("Sprites")]
    [SerializeField] private Sprite buySprite;
    [SerializeField] private Sprite equipSprite;
    [SerializeField] private Sprite equippedSprite;

    private CosmeticState state;
    private int price;

    private CosmeticRow row;

    private void Awake()
    {
        row = GetComponentInParent<CosmeticRow>();
        row.Register(this);

        button.onClick.AddListener(OnClicked);
    }

    public void Initialize(int cosmeticPrice, bool owned, bool equipped)
    {
        price = cosmeticPrice;

        if (!owned)
            state = CosmeticState.Locked;
        else if (equipped)
            state = CosmeticState.Equipped;
        else
            state = CosmeticState.Owned;

        priceText.text = price.ToString();

        UpdateVisual();
    }

    private void OnClicked()
    {
        switch (state)
        {
            case CosmeticState.Locked:
                TryBuy();
                break;

            case CosmeticState.Owned:
                row.Equip(this);
                break;
        }
    }

    private void TryBuy()
    {
        var player = ServiceLocator.Instance.PlayerManager;

        // if (player.Money < price)
        //     return;

        player.SetMoney ( player.Money - price);
        state = CosmeticState.Owned;

        UpdateVisual();
    }

    public void SetEquipped()
    {
        state = CosmeticState.Equipped;
        UpdateVisual();
    }

    public void SetOwned()
    {
        if (state != CosmeticState.Locked)
        {
            state = CosmeticState.Owned;
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        switch (state)
        {
            case CosmeticState.Locked:

                buttonImage.sprite = buySprite;
                priceContainer.SetActive(true);
                actionText.gameObject.SetActive(false);

                break;

            case CosmeticState.Owned:

                buttonImage.sprite = equipSprite;
                priceContainer.SetActive(false);
                actionText.gameObject.SetActive(true);
                actionText.text = "EQUIP";

                break;

            case CosmeticState.Equipped:

                buttonImage.sprite = equippedSprite;
                priceContainer.SetActive(false);
                actionText.gameObject.SetActive(true);
                actionText.text = "EQUIPPED";

                break;
        }
    }
}