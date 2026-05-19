using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomDisconnectHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private int lobbySceneBuildIndex = 0;

    private NetworkRunner runner;
    private bool isReturning;

    private void OnEnable()
    {
        TryRegister();
    }

    private void Start()
    {
        TryRegister();
    }

    private void Update()
    {
        if (runner == null)
            TryRegister();
    }

    private void OnDisable()
    {
        if (runner != null)
            runner.RemoveCallbacks(this);
    }

    private void TryRegister()
    {
        if (runner != null)
            return;

        runner = FindFirstObjectByType<NetworkRunner>();

        if (runner == null)
            return;

        runner.AddCallbacks(this);
    }

    private void ReturnToLobby()
    {
        if (isReturning)
            return;

        isReturning = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (runner != null)
            runner.RemoveCallbacks(this);

        SceneManager.LoadScene(lobbySceneBuildIndex);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        ReturnToLobby();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        ReturnToLobby();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}