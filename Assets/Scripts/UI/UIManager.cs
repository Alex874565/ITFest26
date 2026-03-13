using UnityEngine;

public class UIManager : MonoBehaviour
{
    [field: SerializeField] public SettingsUI SettingsUI {get; private set;}
    //[field: SerializeField] public ShopUI ShopUI {get; private set;}
    [field: SerializeField] public PauseUI PauseUI {get; private set;}
}