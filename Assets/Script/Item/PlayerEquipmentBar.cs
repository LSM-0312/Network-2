using Fusion;
using UnityEngine;

public class PlayerEquipmentBar : NetworkBehaviour, IAfterSpawned
{
    [Header("Refs")]
    [SerializeField] private PlayerAvatar avatar;
    [SerializeField] private ItemCatalog itemCatalog;

    [Header("Default Slot1")]
    [SerializeField] private ItemDefinition copDefaultItem;
    [SerializeField] private ItemDefinition robberDefaultItem;

    [Header("Robber Start Throwable")]
    [SerializeField] private ThrowableItemDefinition robberStartThrowableItem;
    [SerializeField] private int robberStartThrowableAmmo = 2;

    [Networked, Capacity(4)]
    public NetworkArray<EquipmentSlotState> Slots => default;

    [Networked]
    public byte CurrentSlotIndex { get; set; }

    [Networked] private NetworkBool initialized { get; set; }

    public override void Spawned()
    {
        if (avatar == null)
            TryGetComponent(out avatar);
    }

    public void AfterSpawned()
    {
        TryInitializeDefaultSlots();
    }

    private void TryInitializeDefaultSlots()
    {
        if (!Object.HasStateAuthority)
            return;

        if (initialized)
            return;

        if (avatar == null)
            return;

        if (avatar.Role == PlayerRole.None)
            return;

        ItemDefinition slot1Item = avatar.Role == PlayerRole.Cop
            ? copDefaultItem
            : robberDefaultItem;

        SetSlotFromItem(0, slot1Item);
        ClearSlot(1);
        ClearSlot(2);
        ClearSlot(3);

        if (avatar.Role == PlayerRole.Robber && robberStartThrowableItem != null)
            EquipThrowable(robberStartThrowableItem, robberStartThrowableAmmo);

        CurrentSlotIndex = 0;
        initialized = true;
    }

    public EquipmentSlotState GetSlot(int index)
    {
        return Slots.Get(index);
    }

    public EquipmentSlotState GetCurrentSlot()
    {
        return Slots.Get(CurrentSlotIndex);
    }

    public ItemDefinition GetItemInSlot(int index)
    {
        EquipmentSlotState state = Slots.Get(index);

        if (state.IsEmpty || itemCatalog == null)
            return null;

        return itemCatalog.Get(state.itemId);
    }

    public ItemDefinition GetCurrentItem()
    {
        return GetItemInSlot(CurrentSlotIndex);
    }

    public void SelectSlot(int index)
    {
        if (!Object.HasStateAuthority)
            return;

        if (index < 0 || index >= 4)
            return;

        CurrentSlotIndex = (byte)index;
    }

    public void SetSlotFromItem(int index, ItemDefinition item, int ammoOverride = -1)
    {
        if (!Object.HasStateAuthority)
            return;

        if (index < 0 || index >= 4)
            return;

        if (item == null)
        {
            ClearSlot(index);
            return;
        }

        EquipmentSlotState state = new EquipmentSlotState
        {
            itemId = item.itemId,
            ammo = (short)(ammoOverride >= 0 ? ammoOverride : item.defaultAmmo)
        };

        Slots.Set(index, state);
    }

    public void ClearSlot(int index)
    {
        if (!Object.HasStateAuthority)
            return;

        if (index < 0 || index >= 4)
            return;

        Slots.Set(index, default);
    }

    public void EquipMainItem(ItemDefinition item, int ammoOverride = -1)
    {
        SetSlotFromItem(1, item, ammoOverride);
    }

    public void EquipSubItem(ItemDefinition item, int ammoOverride = -1)
    {
        SetSlotFromItem(2, item, ammoOverride);
    }

    public void EquipThrowable(ThrowableItemDefinition item, int ammoOverride = -1)
    {
        SetSlotFromItem(3, item, ammoOverride);
    }

    public bool TryConsumeAmmo(int slotIndex, int amount)
    {
        if (!Object.HasStateAuthority)
            return false;

        EquipmentSlotState slot = Slots.Get(slotIndex);

        if (slot.IsEmpty || slot.ammo < amount)
            return false;

        slot.ammo -= (short)amount;

        if (slot.ammo <= 0)
            Slots.Set(slotIndex, default);
        else
            Slots.Set(slotIndex, slot);

        return true;
    }
}