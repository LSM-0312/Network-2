using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef copPrefab;
    [SerializeField] private NetworkPrefabRef robberPrefab;

    private NetworkRunner runner;
    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new();

    private void Start()
    {
        if (GameNetworkManager.Instance == null)
        {
            Debug.LogError("GameNetworkManager.Instance가 없음");
            return;
        }

        runner = GameNetworkManager.Instance.GetRunner();

        if (runner == null)
        {
            Debug.LogError("Runner가 없음");
            return;
        }

        runner.AddCallbacks(this);
    }

    private void OnDestroy()
    {
        if (runner != null)
            runner.RemoveCallbacks(this);
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer)
            return;

        foreach (PlayerRef player in runner.ActivePlayers)
            EnsurePlayerSpawned(player);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        EnsurePlayerSpawned(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        if (spawnedPlayers.TryGetValue(player, out var obj))
        {
            if (obj != null)
                runner.Despawn(obj);

            spawnedPlayers.Remove(player);
        }
    }

    private void EnsurePlayerSpawned(PlayerRef player)
    {
        if (spawnedPlayers.ContainsKey(player))
            return;

        PlayerRole role = GetSelectedRole(player);
        NetworkPrefabRef prefabToSpawn = role == PlayerRole.Cop ? copPrefab : robberPrefab;
        Vector3 spawnPos = GetSpawnPosition(role);

        NetworkObject obj = runner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, player);
        runner.SetPlayerObject(player, obj);

        spawnedPlayers.Add(player, obj);
    }

    private PlayerRole GetSelectedRole(PlayerRef player)
    {
        RoomPlayer[] roomPlayers = FindObjectsOfType<RoomPlayer>(true);

        foreach (var roomPlayer in roomPlayers)
        {
            if (roomPlayer != null && roomPlayer.Object != null && roomPlayer.Object.InputAuthority == player)
                return roomPlayer.SelectedRole;
        }

        return PlayerRole.Robber;
    }

    private Vector3 GetSpawnPosition(PlayerRole role)
    {
        return role == PlayerRole.Cop
            ? new Vector3(-2f, 1f, 0f)
            : new Vector3(2f, 1f, 0f);
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}