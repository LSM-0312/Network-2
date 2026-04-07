using Fusion;
using UnityEngine;

public enum InputButton
{
    Jump = 0,
    Sprint = 1,
    Mouse0 = 2,
    Mouse1 = 3,

    Slot1 = 4,
    Slot2 = 5,
    Slot3 = 6,
    Slot4 = 7
}

public struct NetworkInputData : INetworkInput
{
    public Vector3 direction;
    public NetworkButtons buttons;
}