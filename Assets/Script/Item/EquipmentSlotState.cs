using Fusion;

public struct EquipmentSlotState : INetworkStruct
{
    public int itemId;
    public short ammo;
    public short stackCount;

    public bool IsEmpty => itemId == 0;
}