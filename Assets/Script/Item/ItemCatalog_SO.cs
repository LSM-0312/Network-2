using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Scriptable Object/Items/Item Catalog")]
public class ItemCatalog : ScriptableObject
{
    [SerializeField] private ItemDefinition[] items;

    private Dictionary<int, ItemDefinition> cache;

    private void BuildCache()
    {
        if (cache != null)
            return;

        cache = new Dictionary<int, ItemDefinition>();

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            cache[items[i].itemId] = items[i];
        }
    }

    public ItemDefinition Get(int itemId)
    {
        BuildCache();
        cache.TryGetValue(itemId, out ItemDefinition item);
        return item;
    }
}