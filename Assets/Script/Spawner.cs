using Fusion;
using Fusion.Addons.Physics;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Spawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef playerPrefab;

    private NetworkRunner runner;
    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new();

    private bool copAssigned = false;

    async void StartGame(GameMode mode)
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        runner.AddCallbacks(this);

        var inputProvider = gameObject.AddComponent<InputProvider>();
        runner.AddCallbacks(inputProvider);

        gameObject.AddComponent<RunnerSimulatePhysics3D>();

        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        var sceneInfo = new NetworkSceneInfo();
        if (sceneRef.IsValid)
            sceneInfo.AddSceneRef(sceneRef, LoadSceneMode.Single);

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = sceneInfo,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
        });

        Debug.Log($"Runner started. IsServer={runner.IsServer}");
    }

    private void OnGUI()
    {
        if (runner == null)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
                StartGame(GameMode.Host);

            if (GUI.Button(new Rect(0, 50, 200, 40), "Join"))
                StartGame(GameMode.Client);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        Vector3 spawnPos = new Vector3(spawnedPlayers.Count * 2.5f, 1f, 0f);

        NetworkObject playerObj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
        runner.SetPlayerObject(player, playerObj);

        PlayerAvatar avatar = playerObj.GetComponent<PlayerAvatar>();
        if (avatar != null)
        {
            if (!copAssigned)
            {
                avatar.Role = PlayerRole.Cop;
                copAssigned = true;
                Debug.Log($"Player {player} -> Cop");
            }
            else
            {
                avatar.Role = PlayerRole.Robber;
                Debug.Log($"Player {player} -> Robber");
            }
        }

        spawnedPlayers.Add(player, playerObj);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        if (spawnedPlayers.TryGetValue(player, out var obj))
        {
            runner.Despawn(obj);
            spawnedPlayers.Remove(player);
        }
    }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}