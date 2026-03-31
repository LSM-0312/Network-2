using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MatchRosterBoot : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkObject matchRosterPrefab;

    private NetworkRunner runner;
    private bool initialized;
    private bool spawnRequested;
    private float nextWaitLogTime;

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
            TryInitializeRoster();
    }

    private void Update()
    {
        if (initialized)
            return;

        if (runner == null || !runner.IsServer)
            return;

        TryInitializeRoster();
    }

    private void OnDestroy()
    {
        if (runner != null)
            runner.RemoveCallbacks(this);
    }

    private void TryInitializeRoster()
    {
        if (MatchRoster.Instance == null)
        {
            if (!spawnRequested)
            {
                if (matchRosterPrefab == null)
                {
                    Debug.LogError("matchRosterPrefab이 비어있음");
                    return;
                }

                runner.Spawn(matchRosterPrefab, Vector3.zero, Quaternion.identity);
                spawnRequested = true;
                Debug.Log("[MatchRosterBootstrapper] MatchRoster 스폰 요청");
            }

            if (Time.time >= nextWaitLogTime)
            {
                Debug.Log("[MatchRosterBootstrapper] MatchRoster 생성 대기중");
                nextWaitLogTime = Time.time + 1f;
            }

            return;
        }

        if (MatchRoster.Instance.IsLocked)
        {
            initialized = true;
            return;
        }

        if (GameNetworkManager.Instance == null)
        {
            Debug.LogError("GameNetworkManager.Instance가 없음");
            return;
        }

        List<PlayerRef> players = new();

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (!GameNetworkManager.Instance.TryGetCachedRole(player, out _))
            {
                if (Time.time >= nextWaitLogTime)
                {
                    Debug.Log($"[MatchRosterBootstrapper] 역할 캐시 대기중: {player}");
                    nextWaitLogTime = Time.time + 1f;
                }
                return;
            }

            players.Add(player);
        }

        players.Sort((a, b) => a.RawEncoded.CompareTo(b.RawEncoded));

        MatchRoster.Instance.ServerClearAll();

        for (int i = 0; i < players.Count; i++)
        {
            PlayerRef player = players[i];
            GameNetworkManager.Instance.TryGetCachedRole(player, out PlayerRole role);
            MatchRoster.Instance.ServerAddInitialPlayer(player, role);
        }

        MatchRoster.Instance.ServerLock();
        initialized = true;

        Debug.Log($"[MatchRosterBootstrapper] 초기 MatchRoster 확정 / Active Cop={MatchRoster.Instance.GetActiveRoleCount(PlayerRole.Cop)} / Active Robber={MatchRoster.Instance.GetActiveRoleCount(PlayerRole.Robber)}");

        GameNetworkManager.Instance.ClearCachedRoles();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer)
            return;

        TryInitializeRoster();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        if (!initialized)
        {
            TryInitializeRoster();
            return;
        }

        if (MatchRoster.Instance == null)
            return;

        if (!MatchRoster.Instance.IsLocked)
            return;

        if (MatchRoster.Instance.HasActivePlayer(player) || MatchRoster.Instance.IsPendingPlayer(player))
            return;

        if (MatchRoster.Instance.ServerAssignLateJoinAsPending(player, out PlayerRole role))
            Debug.Log($"[MatchRosterBootstrapper] 중도입장 관전 등록 / player={player}, reservedRole={role}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        if (MatchRoster.Instance != null)
            MatchRoster.Instance.ServerRemovePlayer(player);
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