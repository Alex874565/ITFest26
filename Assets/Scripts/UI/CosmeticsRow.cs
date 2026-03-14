using System.Collections.Generic;
using UnityEngine;

public class CosmeticRow : MonoBehaviour
{
    private List<CosmeticItemUI> cosmetics = new List<CosmeticItemUI>();

    public void Register(CosmeticItemUI item)
    {
        if (!cosmetics.Contains(item))
            cosmetics.Add(item);
    }

    public void Equip(CosmeticItemUI itemToEquip)
    {
        foreach (var item in cosmetics)
        {
            if (item == itemToEquip)
                item.SetEquipped();
            else
                item.SetOwned();
        }
    }
}