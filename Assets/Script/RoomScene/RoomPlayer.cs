using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class RoomPlayer : NetworkBehaviour
{
    public static readonly List<RoomPlayer> ActivePlayers = new List<RoomPlayer>();

    [Networked] public NetworkString<_32> DisplayName { get; set; }
    [Networked] public NetworkBool IsReady { get; set; }
    [Networked] public NetworkBool IsHostPlayer { get; set; }

    public bool IsLocalPlayer => Object != null && Object.HasInputAuthority;

    public override void Spawned()
    {
        Debug.Log($"RoomPlayer Spawned: {DisplayName}");
        if (!ActivePlayers.Contains(this))
            ActivePlayers.Add(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        ActivePlayers.Remove(this);
    }

    public void InitializeOnServer(string displayName, bool isHostPlayer)
    {
        if (!Runner.IsServer)
            return;

        DisplayName = displayName;
        IsHostPlayer = isHostPlayer;
        IsReady = false;
    }

    public void ToggleReady()
    {
        if (!Object.HasInputAuthority)
            return;

        if (IsHostPlayer)
            return;

        RPC_SetReady(!IsReady);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetReady(NetworkBool value)
    {
        if (IsHostPlayer)
            return;

        IsReady = value;
    }
}