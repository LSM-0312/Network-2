using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class RoomManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static RoomManager Instance;

    public static event Action RoomVisualChanged;

    [Header("Prefabs")]
    [SerializeField] private NetworkObject roomPlayerPrefab;

    [Header("Scene")]
    [SerializeField] private int gameSceneBuildIndex = 3;

    [Header("Rule")]
    [SerializeField] private bool allowStartWithoutClients = false;

    [Networked, OnChangedRender(nameof(OnRoomNameChanged))]
    public NetworkString<_32> RoomDisplayName { get; set; }

    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayerObjects = new();

    public bool IsLocalHost => Runner != null && Runner.IsServer;
    public string RoomName => RoomDisplayName.ToString();

    public override void Spawned()
    {
        Instance = this;
        Runner.AddCallbacks(this);

        if (Runner.IsServer)
        {
            if (RoomDisplayName.ToString().Length == 0)
            {
                string sessionName = "Room";
                if (Runner.SessionInfo.IsValid)
                    sessionName = Runner.SessionInfo.Name;

                RoomDisplayName = sessionName;
            }

            SpawnMissingPlayers();
        }

        RaiseRoomVisualChanged();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;

        runner.RemoveCallbacks(this);
    }

    private void SpawnMissingPlayers()
    {
        foreach (PlayerRef player in Runner.ActivePlayers)
            EnsurePlayerSpawned(player);
    }

    private void EnsurePlayerSpawned(PlayerRef player)
    {
        if (!Runner.IsServer)
            return;

        if (spawnedPlayerObjects.ContainsKey(player))
            return;

        NetworkObject obj = Runner.Spawn(roomPlayerPrefab, Vector3.zero, Quaternion.identity, player);
        RoomPlayer roomPlayer = obj.GetComponent<RoomPlayer>();

        bool isHostPlayer = player == Runner.LocalPlayer;
        string displayName = isHostPlayer ? "Host" : $"Player {spawnedPlayerObjects.Count + 1}";

        roomPlayer.InitializeOnServer(displayName, isHostPlayer);

        Runner.SetPlayerObject(player, obj);
        spawnedPlayerObjects.Add(player, obj);

        RaiseRoomVisualChanged();
    }

    public RoomPlayer GetLocalRoomPlayer()
    {
        if (Runner == null)
            return null;

        if (!Runner.TryGetPlayerObject(Runner.LocalPlayer, out NetworkObject obj))
            return null;

        if (obj == null)
            return null;

        return obj.GetComponent<RoomPlayer>();
    }

    public bool CanStartMatch()
    {
        if (!IsLocalHost)
            return false;

        int clientCount = 0;
        int copCount = 0;
        int robberCount = 0;

        for (int i = 0; i < RoomPlayer.ActivePlayers.Count; i++)
        {
            RoomPlayer player = RoomPlayer.ActivePlayers[i];
            if (player == null)
                continue;

            if (player.SelectedRole == PlayerRole.Cop)
                copCount++;
            else if (player.SelectedRole == PlayerRole.Robber)
                robberCount++;
            else
                return false;

            if (player.IsHostPlayer)
                continue;

            clientCount++;

            if (!player.IsReady)
                return false;
        }

        if (!allowStartWithoutClients && clientCount == 0)
            return false;

        if (copCount == 0 || robberCount == 0)
            return false;

        return true;
    }

    private void CacheRoomSelectionsForGameScene()
    {
        if (GameNetworkManager.Instance == null)
        {
            Debug.LogError("GameNetworkManager.Instance가 없음");
            return;
        }

        GameNetworkManager.Instance.ClearCachedRoles();

        for (int i = 0; i < RoomPlayer.ActivePlayers.Count; i++)
        {
            RoomPlayer roomPlayer = RoomPlayer.ActivePlayers[i];

            if (roomPlayer == null || roomPlayer.Object == null)
                continue;

            PlayerRef player = roomPlayer.Object.InputAuthority;
            GameNetworkManager.Instance.CacheSelectedRole(player, roomPlayer.SelectedRole);

            Debug.Log($"[RoomManager] 역할 캐시 저장: player={player}, role={roomPlayer.SelectedRole}");
        }
    }

    public void StartMatch()
    {
        if (!IsLocalHost)
            return;

        if (!Runner.IsSceneAuthority)
            return;

        if (!CanStartMatch())
            return;

        CacheRoomSelectionsForGameScene();
        Runner.LoadScene(SceneRef.FromIndex(gameSceneBuildIndex));
    }

    public void LeaveRoom()
    {
        if (GameNetworkManager.Instance != null)
            GameNetworkManager.Instance.LeaveSessionAndReturnToLobby2();
        else
            Debug.LogError("GameNetworkManager.Instance가 null임");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        EnsurePlayerSpawned(player);
        RaiseRoomVisualChanged();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        if (spawnedPlayerObjects.TryGetValue(player, out NetworkObject obj))
        {
            if (obj != null)
                runner.Despawn(obj);

            spawnedPlayerObjects.Remove(player);
        }

        if (GameNetworkManager.Instance != null)
            GameNetworkManager.Instance.RemoveCachedRole(player);

        RaiseRoomVisualChanged();
    }

    private void OnRoomNameChanged()
    {
        RaiseRoomVisualChanged();
    }

    private void RaiseRoomVisualChanged()
    {
        RoomVisualChanged?.Invoke();
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
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
}