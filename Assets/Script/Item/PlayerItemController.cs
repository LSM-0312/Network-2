using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;

public class PlayerItemController : NetworkBehaviour
{
    [SerializeField] private PlayerEquipmentBar equipmentBar;
    [SerializeField] private MeleeAttack meleeAttack;
    [SerializeField] private Transform throwPoint;

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
        // 현재 아이템 가져오기
        ItemDefinition item = equipmentBar.GetCurrentItem();
        if (item == null)
            return;

        int slotIndex = equipmentBar.CurrentSlotIndex;

        if (item is MeleeItemDefinition meleeItem)
        {
            UseMelee(meleeItem);
            return;
        }

        if (item is ThrowableItemDefinition throwableItem)
        {
            UseThrowable(slotIndex, throwableItem);
            return;
        }
    }

    private void UseMelee(MeleeItemDefinition item)
    {
        if (meleeAttack == null)
            return;

        meleeAttack.TryAttack(item);
    }

    private void UseThrowable(int slotIndex, ThrowableItemDefinition item)
    {
        if (throwPoint == null)
            return;

        if (!equipmentBar.TryConsumeAmmo(slotIndex, 1))
            return;

        Runner.Spawn(
            item.projectilePrefab,
            throwPoint.position,
            throwPoint.rotation,
            Object.InputAuthority,
            (runner, obj) =>
            {
                ThrowableProjectile projectile = obj.GetComponent<ThrowableProjectile>();
                if (projectile != null)
                    projectile.Init(Object.InputAuthority, item.throwForce, item.upwardForce, item.heldLocalScale);
            }
        );
    }
}