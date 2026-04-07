using Fusion;
using UnityEngine;

public class PlayerEquipmentBar : NetworkBehaviour, IAfterSpawned
{
    [SerializeField] private PlayerAvatar avatar;
    [SerializeField] private ItemCatalog itemCatalog;
    [SerializeField] private RoleDefaultItems roleDefaultItems;

    //테스트
    [SerializeField] private ItemDefinition testMainItem;
    [SerializeField] private ItemDefinition testThrowableItem;
    [SerializeField] private int testThrowableCount = 2;

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

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority && !initialized)
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
            ? roleDefaultItems.copSlot1Item
            : roleDefaultItems.robberSlot1Item;

        SetSlotFromItem(0, slot1Item);
        ClearSlot(1);
        ClearSlot(2);
        ClearSlot(3);

        //테스트~
        if (testMainItem != null)
            EquipMainItem(testMainItem);

        if (testThrowableItem != null)
            EquipThrowable(testThrowableItem, testThrowableCount);
        //~테스트

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

    public void SetSlotFromItem(int index, ItemDefinition item, int ammoOverride = -1, int stackOverride = -1)
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
            ammo = (short)(ammoOverride >= 0 ? ammoOverride : item.defaultAmmo),
            stackCount = (short)(stackOverride >= 0 ? stackOverride : item.defaultStack)
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
        SetSlotFromItem(1, item, ammoOverride, 1);
    }

    public void EquipSubItem(ItemDefinition item, int ammoOverride = -1)
    {
        SetSlotFromItem(2, item, ammoOverride, 1);
    }

    public void EquipThrowable(ItemDefinition item, int stackOverride = -1)
    {
        SetSlotFromItem(3, item, -1, stackOverride);
    }

    public bool TryConsumeAmmo(int slotIndex, int amount)
    {
        if (!Object.HasStateAuthority)
            return false;

        EquipmentSlotState slot = Slots.Get(slotIndex);

        if (slot.IsEmpty || slot.ammo < amount)
            return false;

        slot.ammo -= (short)amount;
        Slots.Set(slotIndex, slot);
        return true;
    }

    public bool TryConsumeStack(int slotIndex, int amount)
    {
        if (!Object.HasStateAuthority)
            return false;

        EquipmentSlotState slot = Slots.Get(slotIndex);

        if (slot.IsEmpty || slot.stackCount < amount)
            return false;

        slot.stackCount -= (short)amount;

        if (slot.stackCount <= 0)
            Slots.Set(slotIndex, default);
        else
            Slots.Set(slotIndex, slot);

        return true;
    }
}