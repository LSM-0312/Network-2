using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class RoomPlayer : NetworkBehaviour
{
    public static readonly List<RoomPlayer> ActivePlayers = new List<RoomPlayer>();

    public static event Action ActivePlayersChanged;
    public static event Action<RoomPlayer> AnyPlayerVisualChanged;

    [Networked, OnChangedRender(nameof(OnDisplayNameChanged))]
    public NetworkString<_32> DisplayName { get; set; }

    [Networked, OnChangedRender(nameof(OnReadyChanged))]
    public NetworkBool IsReady { get; set; }

    [Networked, OnChangedRender(nameof(OnHostChanged))]
    public NetworkBool IsHostPlayer { get; set; }

    [Networked, OnChangedRender(nameof(OnRoleChanged))]
    public int SelectedRoleValue { get; set; }

    public bool IsLocalPlayer => Object != null && Object.HasInputAuthority;

    public PlayerRole SelectedRole => (PlayerRole)SelectedRoleValue;

    public override void Spawned()
    {
        if (!ActivePlayers.Contains(this))
            ActivePlayers.Add(this);

        ActivePlayersChanged?.Invoke();

        // OnChangedRender는 첫 Spawn 시 자동 호출되지 않으므로 직접 초기화 알림
        RaiseVisualChanged();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        ActivePlayers.Remove(this);
        ActivePlayersChanged?.Invoke();
    }

    public void InitializeOnServer(string displayName, bool isHostPlayer)
    {
        if (!Runner.IsServer)
            return;

        DisplayName = displayName;
        IsHostPlayer = isHostPlayer;
        IsReady = false;
        SelectedRoleValue = (int)PlayerRole.Cop;
    }

    public void ToggleReady()
    {
        if (!Object.HasInputAuthority)
            return;

        if (IsHostPlayer)
            return;

        RPC_SetReady(!IsReady);
    }

    public void ToggleRole()
    {
        if (!Object.HasInputAuthority)
            return;

        int nextRole = SelectedRole == PlayerRole.Cop
            ? (int)PlayerRole.Robber
            : (int)PlayerRole.Cop;

        RPC_SetRole(nextRole);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetReady(NetworkBool value)
    {
        if (IsHostPlayer)
            return;

        IsReady = value;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetRole(int roleValue)
    {
        if (roleValue != (int)PlayerRole.Cop && roleValue != (int)PlayerRole.Robber)
            return;

        SelectedRoleValue = roleValue;
    }

    private void OnDisplayNameChanged() => RaiseVisualChanged();
    private void OnReadyChanged() => RaiseVisualChanged();
    private void OnHostChanged() => RaiseVisualChanged();
    private void OnRoleChanged() => RaiseVisualChanged();

    private void RaiseVisualChanged()
    {
        AnyPlayerVisualChanged?.Invoke(this);
    }
}