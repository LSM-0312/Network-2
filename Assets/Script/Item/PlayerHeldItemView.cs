using Fusion;
using UnityEngine;

public class PlayerHeldItemView : NetworkBehaviour
{
    [SerializeField] private PlayerEquipmentBar equipmentBar;
    [SerializeField] private ItemCatalog itemCatalog;
    [SerializeField] private Transform handSocket;

    private GameObject currentView;
    private int cachedItemId = -999;
    private byte cachedSlot = 255;

    public override void Render()
    {
        if (equipmentBar == null || itemCatalog == null || handSocket == null)
            return;

        EquipmentSlotState slot = equipmentBar.GetCurrentSlot();

        if (cachedItemId == slot.itemId && cachedSlot == equipmentBar.CurrentSlotIndex)
            return;

        cachedItemId = slot.itemId;
        cachedSlot = equipmentBar.CurrentSlotIndex;

        RefreshView(slot.itemId);
    }

    private void RefreshView(int itemId)
    {
        if (currentView != null)
            Destroy(currentView);

        if (itemId == 0)
            return;

        ItemDefinition item = itemCatalog.Get(itemId);
        if (item == null || item.heldViewPrefab == null)
            return;

        currentView = Instantiate(item.heldViewPrefab, handSocket);
        currentView.transform.localPosition = Vector3.zero;
        currentView.transform.localRotation = Quaternion.identity;
        currentView.transform.localScale = Vector3.one;
    }
}