using UnityEngine;

public class UIManager : MonoBehaviour
{
    [field: SerializeField] public SettingsUI SettingsUI {get; private set;}
    [field: SerializeField] public SettingsUI ShopUI {get; private set;}
}