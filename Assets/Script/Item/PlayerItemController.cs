using Fusion;
using UnityEngine;

public class PlayerItemController : NetworkBehaviour
{
    [SerializeField] private PlayerEquipmentBar equipmentBar;

    [Networked] private NetworkButtons previousButtons { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!GetInput(out NetworkInputData input))
            return;

        HandleSlotSwap(input.buttons);

        if (input.buttons.WasPressed(previousButtons, (int)InputButton.Mouse0))
            UseCurrentItemPrimary();

        previousButtons = input.buttons;
    }

    private void HandleSlotSwap(NetworkButtons buttons)
    {
        if (buttons.WasPressed(previousButtons, (int)InputButton.Slot1))
            equipmentBar.SelectSlot(0);
        else if (buttons.WasPressed(previousButtons, (int)InputButton.Slot2))
            equipmentBar.SelectSlot(1);
        else if (buttons.WasPressed(previousButtons, (int)InputButton.Slot3))
            equipmentBar.SelectSlot(2);
        else if (buttons.WasPressed(previousButtons, (int)InputButton.Slot4))
            equipmentBar.SelectSlot(3);
    }

    private void UseCurrentItemPrimary()
    {
        ItemDefinition item = equipmentBar.GetCurrentItem();
        if (item == null)
            return;

        switch (item.useMode)
        {
            case ItemUseMode.None:
                break;

            case ItemUseMode.MeleeSwing:
                UseMelee(item);
                break;

            case ItemUseMode.HitscanShot:
                UseHitscan(item);
                break;

            case ItemUseMode.ThrowProjectile:
                UseThrow(item);
                break;
        }
    }

    private void UseMelee(ItemDefinition item)
    {
        // 여기서 기존 근접 공격 컴포넌트 호출
        // ex) GetComponent<MeleeAttack>()?.TryAttack();
    }

    private void UseHitscan(ItemDefinition item)
    {
        int slotIndex = equipmentBar.CurrentSlotIndex;

        if (item.defaultAmmo > 0)
        {
            if (!equipmentBar.TryConsumeAmmo(slotIndex, 1))
                return;
        }

        // 여기서 기존 총기/레이캐스트 로직 호출
    }

    private void UseThrow(ItemDefinition item)
    {
        int slotIndex = equipmentBar.CurrentSlotIndex;

        if (!equipmentBar.TryConsumeStack(slotIndex, 1))
            return;

        // 여기서 기존 투척 로직 호출
        // 섬광탄/연막탄 프리팹 스폰은 Host/StateAuthority 쪽에서 Runner.Spawn()으로 처리
    }
}