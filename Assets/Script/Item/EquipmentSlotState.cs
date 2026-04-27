using Fusion;

public struct EquipmentSlotState : INetworkStruct
{
    public int itemId;
    public short ammo;

    public bool IsEmpty => itemId == 0;
}