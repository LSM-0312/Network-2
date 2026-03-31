using Fusion;
using UnityEngine;

public enum InputButton
{
    Jump = 0,
    Sprint = 1,
    Mouse0 = 2,
    Mouse1 = 3
}

public struct NetworkInputData : INetworkInput
{
    public Vector3 direction;
    public NetworkButtons buttons;
}