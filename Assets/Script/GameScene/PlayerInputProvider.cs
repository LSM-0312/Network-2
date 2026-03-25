using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class InputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    private bool _mouseButton0;
    private bool _mouseButton1;

    private void Update()
    {
        _mouseButton0 |= Input.GetMouseButton(0);
        _mouseButton1 |= Input.GetMouseButton(1);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.W)) z += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;

        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 camF = cam != null ? cam.forward : Vector3.forward;
        Vector3 camR = cam != null ? cam.right : Vector3.right;

        camF.y = 0f;
        camR.y = 0f;
        camF.Normalize();
        camR.Normalize();

        Vector3 moveWorld = camF * z + camR * x;
        moveWorld.y = 0f;

        float mag = Mathf.Clamp01(moveWorld.magnitude);
        data.direction = mag > 0.0001f ? moveWorld.normalized * mag : Vector3.zero;

        if (Input.GetKey(KeyCode.Space))
            data.jump.Set(0, true);

        if (Input.GetKey(KeyCode.LeftShift))
            data.sprint.Set(0, true);

        data.buttons.Set(NetworkInputData.MOUSEBUTTON0, _mouseButton0);
        data.buttons.Set(NetworkInputData.MOUSEBUTTON1, _mouseButton1);

        _mouseButton0 = false;
        _mouseButton1 = false;

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}