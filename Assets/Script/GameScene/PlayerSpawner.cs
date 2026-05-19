using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef playerPrefab;

    [Header("Team Spawn Points")]
    [SerializeField] private Transform[] copSpawnPoints;
    [SerializeField] private Transform[] robberSpawnPoints;

    private NetworkRunner runner;
    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new();
    private readonly HashSet<PlayerRef> pendingPlayers = new();

    private bool pendingSpawnCheck;
    private float nextRoleMissingLogTime;

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

        if (runner.IsServer)
            BeginSpawnCheck();
    }

    private void Update()
    {
        if (runner == null || !runner.IsServer)
            return;

        if (!pendingSpawnCheck)
            return;

        TrySpawnAllPlayers();

        if (AreAllPlayersHandled())
        {
            pendingSpawnCheck = false;
            Debug.Log("[PlayerSpawner] 모든 플레이어 처리 완료");
        }
    }

    private void OnDestroy()
    {
        if (runner != null)
            runner.RemoveCallbacks(this);
    }

    private void BeginSpawnCheck()
    {
        pendingSpawnCheck = true;
        TrySpawnAllPlayers();
    }

    private void TrySpawnAllPlayers()
    {
        foreach (PlayerRef player in runner.ActivePlayers)
            EnsurePlayerSpawned(player);
    }

    private bool AreAllPlayersHandled()
    {
        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (!spawnedPlayers.ContainsKey(player) && !pendingPlayers.Contains(player))
                return false;
        }

        return true;
    }

    private bool EnsurePlayerSpawned(PlayerRef player)
    {
        if (!runner.IsServer)
            return false;

        if (spawnedPlayers.ContainsKey(player))
            return true;

        if (pendingPlayers.Contains(player))
            return false;

        if (GameNetworkManager.Instance == null)
            return false;

        if (!GameNetworkManager.Instance.TryGetCachedRole(player, out PlayerRole role))
        {
            if (Time.time >= nextRoleMissingLogTime)
            {
                Debug.LogWarning($"[PlayerSpawner] 역할 캐시 없음: {player}");
                nextRoleMissingLogTime = Time.time + 1f;
            }

            return false;
        }

        Vector3 spawnPos = GetSpawnPosition(player, role);
        pendingPlayers.Add(player);

        runner.SpawnAsync(
            playerPrefab,
            spawnPos,
            Quaternion.identity,
            player,
            (spawnRunner, obj) =>
            {
                PlayerAvatar avatar = obj.GetComponent<PlayerAvatar>();
                if (avatar != null)
                    avatar.SetInitialRole(role);
            },
            default,
            result => OnSpawnCompleted(player, role, result)
        );

        Debug.Log($"[PlayerSpawner] SpawnAsync 요청: player={player}, role={role}, pos={spawnPos}");
        return false;
    }

    private void OnSpawnCompleted(PlayerRef player, PlayerRole role, NetworkSpawnOp result)
    {
        pendingPlayers.Remove(player);

        if (runner == null)
            return;

        if (!result.IsSpawned || result.Object == null)
        {
            Debug.LogError($"[PlayerSpawner] 스폰 실패: player={player}, role={role}, status={result.Status}");
            pendingSpawnCheck = true;
            return;
        }

        if (!IsPlayerStillActive(player))
        {
            runner.Despawn(result.Object);
            return;
        }

        if (spawnedPlayers.ContainsKey(player))
        {
            runner.Despawn(result.Object);
            return;
        }

        spawnedPlayers.Add(player, result.Object);
        runner.SetPlayerObject(player, result.Object);

        Debug.Log($"[PlayerSpawner] 스폰 완료: player={player}, role={role}, obj={result.Object.name}");
    }

    private bool IsPlayerStillActive(PlayerRef targetPlayer)
    {
        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (player == targetPlayer)
                return true;
        }

        return false;
    }

    private Vector3 GetSpawnPosition(PlayerRef targetPlayer, PlayerRole targetRole)
    {
        Transform[] spawnPoints = targetRole == PlayerRole.Cop ? copSpawnPoints : robberSpawnPoints;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError($"스폰 포인트가 비어있음. role={targetRole}");
            return Vector3.zero;
        }

        int order = GetTeamOrder(targetPlayer, targetRole);

        if (order >= spawnPoints.Length)
        {
            Debug.LogWarning($"스폰 포인트 수 부족. role={targetRole}, order={order}");
            order %= spawnPoints.Length;
        }

        return spawnPoints[order].position;
    }

    private int GetTeamOrder(PlayerRef targetPlayer, PlayerRole targetRole)
    {
        List<PlayerRef> sameTeamPlayers = new();

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (GameNetworkManager.Instance != null &&
                GameNetworkManager.Instance.TryGetCachedRole(player, out PlayerRole role) &&
                role == targetRole)
            {
                sameTeamPlayers.Add(player);
            }
        }

        sameTeamPlayers.Sort((a, b) => a.RawEncoded.CompareTo(b.RawEncoded));

        for (int i = 0; i < sameTeamPlayers.Count; i++)
        {
            if (sameTeamPlayers[i] == targetPlayer)
                return i;
        }

        return 0;
    }

    public bool TryGetSpawnedAvatar(PlayerRef player, out NetworkObject avatarObject)
    {
        return spawnedPlayers.TryGetValue(player, out avatarObject);
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer)
            return;

        BeginSpawnCheck();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        BeginSpawnCheck();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        pendingPlayers.Remove(player);

        if (spawnedPlayers.TryGetValue(player, out NetworkObject obj))
        {
            if (obj != null)
                runner.Despawn(obj);

            spawnedPlayers.Remove(player);
        }
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