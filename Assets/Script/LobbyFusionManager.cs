using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyFusionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Scene Index")]
    [SerializeField] private int roomSceneBuildIndex = 2;

    [Header("Create Room Option")]
    [SerializeField] private string roomNamePrefix = "Room_";
    [SerializeField] private int maxPlayers = 4;

    private NetworkRunner runner;
    private LobbyUIManager uiManager;
    private bool isProcessing = false;

    private async void Start()
    {
        uiManager = FindObjectOfType<LobbyUIManager>();
        await CreateLobbyRunnerAndJoinLobby();
    }

    private NetworkRunner CreateNewRunner(string runnerObjectName)
    {
        GameObject go = new GameObject(runnerObjectName);
        DontDestroyOnLoad(go);

        NetworkRunner newRunner = go.AddComponent<NetworkRunner>();
        newRunner.ProvideInput = false;
        newRunner.AddCallbacks(this);

        return newRunner;
    }

    private async Task ShutdownRunner()
    {
        if (runner == null)
            return;

        await runner.Shutdown();

        if (runner != null && runner.gameObject != null)
            Destroy(runner.gameObject);

        runner = null;

        await Task.Delay(200);
    }

    private async Task CreateLobbyRunnerAndJoinLobby()
    {
        await ShutdownRunner();

        runner = CreateNewRunner("LobbyRunner");
        await runner.JoinSessionLobby(SessionLobby.Shared);
    }

    public async void RefreshLobby()
    {
        if (isProcessing)
            return;

        isProcessing = true;

        uiManager = FindObjectOfType<LobbyUIManager>();

        if (runner == null)
        {
            await CreateLobbyRunnerAndJoinLobby();
            isProcessing = false;
            return;
        }

        await runner.JoinSessionLobby(SessionLobby.Shared);

        isProcessing = false;
    }

    public async void CreateRoom()
    {
        if (isProcessing)
            return;

        isProcessing = true;

        string roomName = roomNamePrefix + Random.Range(1000, 9999);

        await ShutdownRunner();

        runner = CreateNewRunner("RoomRunner");

        StartGameResult result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = roomName,
            PlayerCount = maxPlayers,
            Scene = SceneRef.FromIndex(roomSceneBuildIndex)
        });

        Debug.Log($"방 생성 결과: {result.Ok}");

        if (!result.Ok)
        {
            Debug.LogError($"방 생성 실패: {result.ShutdownReason}");
        }

        isProcessing = false;
    }

    public async void ReturnToLobby1()
    {
        if (isProcessing)
            return;

        isProcessing = true;

        await ShutdownRunner();
        SceneManager.LoadScene("Lobby1");
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        uiManager = FindObjectOfType<LobbyUIManager>();
        if (uiManager == null)
            return;

        List<RoomInfoData> roomList = new List<RoomInfoData>();

        foreach (SessionInfo session in sessionList)
        {
            RoomInfoData info = new RoomInfoData
            {
                roomName = session.Name,
                currentPlayers = session.PlayerCount,
                maxPlayers = session.MaxPlayers,
                isOpen = session.IsOpen
            };

            roomList.Add(info);
        }

        uiManager.RefreshRoomList(roomList);
    }

    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"서버 연결 실패: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"서버 연결 해제: {reason}");
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("씬 로드 완료");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("씬 로드 시작");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"러너 종료: {shutdownReason}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}