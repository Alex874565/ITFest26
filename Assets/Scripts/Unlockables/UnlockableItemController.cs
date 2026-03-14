using UnityEngine;

public class UnlockableItemController : MonoBehaviour
{
    [SerializeField] private UnlockableType type;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private PlayerManager playerManager;

    private void Awake()
    {
        playerManager = FindFirstObjectByType<PlayerManager>();
        Initialize();
        playerManager.OnDataLoaded += Initialize;
    }
    
    private void Initialize()
    {
        UnlockableData data = playerManager.GetEquippedItemData(type);
        if(data != null)
            spriteRenderer.sprite = data.Sprite;
    }
}