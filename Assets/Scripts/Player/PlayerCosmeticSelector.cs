using UnityEngine;
using System.Collections.Generic;

public class PlayerCosmeticSelector : MonoBehaviour
{
    [SerializeField] private List<GameObject> playerCosmetics;

    private void Start()
    {
        int equippedCosmetic = -1;
        equippedCosmetic = SaveManager.Instance.Unlocks[UnlockableType.Character].FindIndex(state => state == CosmeticState.Equipped);
        if (equippedCosmetic != -1)
        {
            playerCosmetics.ForEach(cosmetic => cosmetic.SetActive(false));
            playerCosmetics[equippedCosmetic].SetActive(true);
        }
    }
}