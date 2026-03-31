using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MatchRoster : NetworkBehaviour
{
    public static MatchRoster Instance { get; private set; }

    public const int MAX_PLAYERS = 4;

    [Networked] public NetworkBool IsLocked { get; private set; }
    [Networked] public int ActivationSeq { get; private set; }

    [Networked, Capacity(MAX_PLAYERS)]
    private NetworkDictionary<PlayerRef, byte> _activeRoles => default;

    [Networked, Capacity(MAX_PLAYERS)]
    private NetworkDictionary<PlayerRef, byte> _activeTeamOrders => default;

    [Networked, Capacity(MAX_PLAYERS)]
    private NetworkDictionary<PlayerRef, byte> _pendingRoles => default;

    [Networked, Capacity(MAX_PLAYERS)]
    private NetworkDictionary<PlayerRef, byte> _pendingTeamOrders => default;

    public override void Spawned()
    {
        Instance = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }

    public void ServerClearAll()
    {
        if (!Object.HasStateAuthority)
            return;

        RemoveAll(_activeRoles);
        RemoveAll(_activeTeamOrders);
        RemoveAll(_pendingRoles);
        RemoveAll(_pendingTeamOrders);

        IsLocked = false;
        ActivationSeq = 0;
    }

    public void ServerAddInitialPlayer(PlayerRef player, PlayerRole role)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsLocked)
            return;

        byte order = GetNextTeamOrder(role);
        SetActivePlayer(player, role, order);
    }

    public bool ServerAssignLateJoinAsPending(PlayerRef player, out PlayerRole assignedRole)
    {
        assignedRole = PlayerRole.None;

        if (!Object.HasStateAuthority)
            return false;

        if (!IsLocked)
            return false;

        if (HasActivePlayer(player))
        {
            assignedRole = GetActiveRole(player);
            return true;
        }

        if (IsPendingPlayer(player))
        {
            assignedRole = GetPendingRole(player);
            return true;
        }

        assignedRole = DecideBalancedRole();
        byte order = GetNextTeamOrder(assignedRole);
        SetPendingPlayer(player, assignedRole, order);
        return true;
    }

    public int ServerPromotePendingPlayers()
    {
        if (!Object.HasStateAuthority)
            return 0;

        List<PlayerRef> promoteTargets = new();

        foreach (var pair in _pendingRoles)
            promoteTargets.Add(pair.Key);

        for (int i = 0; i < promoteTargets.Count; i++)
        {
            PlayerRef player = promoteTargets[i];
            PlayerRole role = (PlayerRole)_pendingRoles.Get(player);
            byte order = _pendingTeamOrders.ContainsKey(player) ? _pendingTeamOrders.Get(player) : (byte)0;

            SetActivePlayer(player, role, order);
            _pendingRoles.Remove(player);

            if (_pendingTeamOrders.ContainsKey(player))
                _pendingTeamOrders.Remove(player);
        }

        if (promoteTargets.Count > 0)
            ActivationSeq++;

        return promoteTargets.Count;
    }

    public void ServerRemovePlayer(PlayerRef player)
    {
        if (!Object.HasStateAuthority)
            return;

        if (_activeRoles.ContainsKey(player))
            _activeRoles.Remove(player);

        if (_activeTeamOrders.ContainsKey(player))
            _activeTeamOrders.Remove(player);

        if (_pendingRoles.ContainsKey(player))
            _pendingRoles.Remove(player);

        if (_pendingTeamOrders.ContainsKey(player))
            _pendingTeamOrders.Remove(player);
    }

    public void ServerLock()
    {
        if (!Object.HasStateAuthority)
            return;

        IsLocked = true;
        ActivationSeq++;
    }

    public bool HasActivePlayer(PlayerRef player)
    {
        return _activeRoles.ContainsKey(player);
    }

    public bool IsPendingPlayer(PlayerRef player)
    {
        return _pendingRoles.ContainsKey(player);
    }

    public bool TryGetRole(PlayerRef player, out PlayerRole role)
    {
        role = PlayerRole.None;

        if (_activeRoles.ContainsKey(player))
        {
            role = (PlayerRole)_activeRoles.Get(player);
            return true;
        }

        return false;
    }

    public bool TryGetPendingRole(PlayerRef player, out PlayerRole role)
    {
        role = PlayerRole.None;

        if (_pendingRoles.ContainsKey(player))
        {
            role = (PlayerRole)_pendingRoles.Get(player);
            return true;
        }

        return false;
    }

    public bool TryGetTeamOrder(PlayerRef player, out int order)
    {
        order = 0;

        if (_activeTeamOrders.ContainsKey(player))
        {
            order = _activeTeamOrders.Get(player);
            return true;
        }

        return false;
    }

    public int GetActiveRoleCount(PlayerRole role)
    {
        int count = 0;

        foreach (var pair in _activeRoles)
        {
            if ((PlayerRole)pair.Value == role)
                count++;
        }

        return count;
    }

    public int GetPendingRoleCount(PlayerRole role)
    {
        int count = 0;

        foreach (var pair in _pendingRoles)
        {
            if ((PlayerRole)pair.Value == role)
                count++;
        }

        return count;
    }

    private void SetActivePlayer(PlayerRef player, PlayerRole role, byte order)
    {
        byte value = (byte)role;

        if (_activeRoles.ContainsKey(player))
            _activeRoles.Set(player, value);
        else
            _activeRoles.Add(player, value);

        if (_activeTeamOrders.ContainsKey(player))
            _activeTeamOrders.Set(player, order);
        else
            _activeTeamOrders.Add(player, order);
    }

    private void SetPendingPlayer(PlayerRef player, PlayerRole role, byte order)
    {
        byte value = (byte)role;

        if (_pendingRoles.ContainsKey(player))
            _pendingRoles.Set(player, value);
        else
            _pendingRoles.Add(player, value);

        if (_pendingTeamOrders.ContainsKey(player))
            _pendingTeamOrders.Set(player, order);
        else
            _pendingTeamOrders.Add(player, order);
    }

    private PlayerRole GetActiveRole(PlayerRef player)
    {
        return _activeRoles.ContainsKey(player) ? (PlayerRole)_activeRoles.Get(player) : PlayerRole.None;
    }

    private PlayerRole GetPendingRole(PlayerRef player)
    {
        return _pendingRoles.ContainsKey(player) ? (PlayerRole)_pendingRoles.Get(player) : PlayerRole.None;
    }

    private PlayerRole DecideBalancedRole()
    {
        int copCount = GetTotalRoleCount(PlayerRole.Cop);
        int robberCount = GetTotalRoleCount(PlayerRole.Robber);

        if (copCount <= robberCount)
            return PlayerRole.Cop;

        return PlayerRole.Robber;
    }

    private int GetTotalRoleCount(PlayerRole role)
    {
        return GetActiveRoleCount(role) + GetPendingRoleCount(role);
    }

    private byte GetNextTeamOrder(PlayerRole role)
    {
        byte nextOrder = 0;

        foreach (var pair in _activeRoles)
        {
            if ((PlayerRole)pair.Value != role)
                continue;

            if (_activeTeamOrders.ContainsKey(pair.Key))
            {
                byte current = _activeTeamOrders.Get(pair.Key);
                if (current >= nextOrder)
                    nextOrder = (byte)(current + 1);
            }
        }

        foreach (var pair in _pendingRoles)
        {
            if ((PlayerRole)pair.Value != role)
                continue;

            if (_pendingTeamOrders.ContainsKey(pair.Key))
            {
                byte current = _pendingTeamOrders.Get(pair.Key);
                if (current >= nextOrder)
                    nextOrder = (byte)(current + 1);
            }
        }

        return nextOrder;
    }

    private void RemoveAll(NetworkDictionary<PlayerRef, byte> dict)
    {
        List<PlayerRef> keys = new();

        foreach (var pair in dict)
            keys.Add(pair.Key);

        for (int i = 0; i < keys.Count; i++)
            dict.Remove(keys[i]);
    }
}