using Fusion;
using Fusion.Sockets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner runner;

    private bool jumpPressed;
    private bool mouse0Pressed;
    private bool mouse1Pressed;

    private bool slot1Pressed;
    private bool slot2Pressed;
    private bool slot3Pressed;
    private bool slot4Pressed;

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

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpPressed = true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            mouse0Pressed = true;

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            mouse1Pressed = true;

        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            slot1Pressed = true;

        if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
            slot2Pressed = true;

        if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame)
            slot3Pressed = true;

        if (Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame)
            slot4Pressed = true;
#else
        if (Input.GetKeyDown(KeyCode.Space))
            jumpPressed = true;

        if (Input.GetMouseButtonDown(0))
            mouse0Pressed = true;

        if (Input.GetMouseButtonDown(1))
            mouse1Pressed = true;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            slot1Pressed = true;

        if (Input.GetKeyDown(KeyCode.Alpha2))
            slot2Pressed = true;

        if (Input.GetKeyDown(KeyCode.Alpha3))
            slot3Pressed = true;

        if (Input.GetKeyDown(KeyCode.Alpha4))
            slot4Pressed = true;
#endif
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = default;

        float x = 0f;
        float z = 0f;
        bool sprint = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;

            sprint = Keyboard.current.leftShiftKey.isPressed;
        }
#else
        if (Input.GetKey(KeyCode.W)) z += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;

        sprint = Input.GetKey(KeyCode.LeftShift);
#endif

        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 camF = cam != null ? cam.forward : Vector3.forward;
        Vector3 camR = cam != null ? cam.right : Vector3.right;

        camF.y = 0f;
        camR.y = 0f;

        if (camF.sqrMagnitude > 0.0001f)
            camF.Normalize();
        else
            camF = Vector3.forward;

        if (camR.sqrMagnitude > 0.0001f)
            camR.Normalize();
        else
            camR = Vector3.right;

        Vector3 moveWorld = camF * z + camR * x;
        moveWorld.y = 0f;

        float mag = Mathf.Clamp01(moveWorld.magnitude);
        data.direction = mag > 0.0001f ? moveWorld.normalized * mag : Vector3.zero;

        data.cameraForward = camF;

        data.buttons.Set((int)InputButton.Sprint, sprint);
        data.buttons.Set((int)InputButton.Jump, jumpPressed);
        data.buttons.Set((int)InputButton.Mouse0, mouse0Pressed);
        data.buttons.Set((int)InputButton.Mouse1, mouse1Pressed);

        data.buttons.Set((int)InputButton.Slot1, slot1Pressed);
        data.buttons.Set((int)InputButton.Slot2, slot2Pressed);
        data.buttons.Set((int)InputButton.Slot3, slot3Pressed);
        data.buttons.Set((int)InputButton.Slot4, slot4Pressed);

        input.Set(data);

        jumpPressed = false;
        mouse0Pressed = false;
        mouse1Pressed = false;

        slot1Pressed = false;
        slot2Pressed = false;
        slot3Pressed = false;
        slot4Pressed = false;
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