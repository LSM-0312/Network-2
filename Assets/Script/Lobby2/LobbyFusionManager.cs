using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyFusionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Scene Index")]
    [SerializeField] private int roomSceneBuildIndex = 2;

    [Header("Create Room Option")]
    [SerializeField] private string roomNamePrefix = "Room_";
    [SerializeField] private int maxPlayers = 4;

    private NetworkRunner lobbyRunner;
    private NetworkRunner roomRunner;
    private LobbyUIManager uiManager;

    private bool isRefreshing = false;
    private bool isCreatingRoom = false;
    private bool isLeaving = false;
    private bool isQuickJoining = false;

    private void Start()
    {
        uiManager = FindObjectOfType<LobbyUIManager>();
        StartLobbyWatching();
    }

    private NetworkRunner CreateRunner(string runnerName)
    {
        GameObject go = new GameObject(runnerName);
        DontDestroyOnLoad(go);

        NetworkRunner runner = go.AddComponent<NetworkRunner>();
        runner.ProvideInput = false;
        runner.AddCallbacks(this);

        return runner;
    }

    private async Task ShutdownRunner(NetworkRunner targetRunner)
    {
        if (targetRunner == null)
            return;

        try
        {
            targetRunner.RemoveCallbacks(this);
        }
        catch
        {
        }

        await targetRunner.Shutdown();

        if (targetRunner.gameObject != null)
            Destroy(targetRunner.gameObject);

        await Task.Delay(100);
    }

    private async Task ShutdownLobbyRunner()
    {
        if (lobbyRunner == null)
            return;

        NetworkRunner runnerToShutdown = lobbyRunner;
        lobbyRunner = null;
        await ShutdownRunner(runnerToShutdown);
    }

    private async Task ShutdownRoomRunner()
    {
        if (roomRunner == null)
            return;

        NetworkRunner runnerToShutdown = roomRunner;
        roomRunner = null;
        await ShutdownRunner(runnerToShutdown);
    }

    private async void StartLobbyWatching()
    {
        if (isRefreshing || isCreatingRoom || isLeaving || isQuickJoining)
            return;

        isRefreshing = true;

        await ShutdownLobbyRunner();

        lobbyRunner = CreateRunner("LobbyRunner");

        Debug.Log("Lobby 자동 갱신 시작");

        var result = await lobbyRunner.JoinSessionLobby(SessionLobby.ClientServer);

        Debug.Log("Lobby 접속 결과 = " + result.Ok);

        if (!result.Ok)
        {
            Debug.LogError("Lobby 접속 실패");
            await ShutdownLobbyRunner();
        }

        isRefreshing = false;
    }

    public async void RefreshLobby()
    {
        if (isRefreshing || isCreatingRoom || isLeaving || isQuickJoining)
            return;

        isRefreshing = true;

        uiManager = FindObjectOfType<LobbyUIManager>();
        if (uiManager != null)
            uiManager.ClearRoomList();

        await ShutdownLobbyRunner();

        lobbyRunner = CreateRunner("LobbyRunner");

        Debug.Log("수동 REFRESH: Lobby 재접속");

        var result = await lobbyRunner.JoinSessionLobby(SessionLobby.ClientServer);

        Debug.Log("수동 REFRESH 결과 = " + result.Ok);

        if (!result.Ok)
        {
            Debug.LogError("Lobby REFRESH 실패");
            await ShutdownLobbyRunner();
        }

        isRefreshing = false;
    }

    public async void CreateRoom()
    {
        if (isRefreshing || isCreatingRoom || isLeaving || isQuickJoining)
            return;

        isCreatingRoom = true;

        string roomName = roomNamePrefix + Random.Range(1000, 9999);

        await ShutdownLobbyRunner();
        await ShutdownRoomRunner();

        roomRunner = CreateRunner("RoomRunner");

        StartGameResult result = await roomRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            PlayerCount = maxPlayers,
            Scene = SceneRef.FromIndex(roomSceneBuildIndex)
        });

        Debug.Log($"방 생성 결과: {result.Ok}");

        if (!result.Ok)
        {
            Debug.LogError($"방 생성 실패: {result.ShutdownReason}");
            await ShutdownRoomRunner();
        }

        isCreatingRoom = false;
    }

    public async void QuickJoin()
    {
        if (isRefreshing || isCreatingRoom || isLeaving || isQuickJoining)
            return;

        isQuickJoining = true;

        await ShutdownLobbyRunner();
        await ShutdownRoomRunner();

        roomRunner = CreateRunner("QuickJoinRunner");

        StartGameResult result = await roomRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            MatchmakingMode = MatchmakingMode.FillRoom,
            Scene = SceneRef.FromIndex(roomSceneBuildIndex)
        });

        Debug.Log($"빠른 참가 결과: {result.Ok}");

        if (!result.Ok)
        {
            Debug.LogWarning($"참가 가능한 방이 없습니다. 사유: {result.ShutdownReason}");
            await ShutdownRoomRunner();
        }

        isQuickJoining = false;
    }

    public async void ReturnToLobby1()
    {
        if (isRefreshing || isCreatingRoom || isLeaving || isQuickJoining)
            return;

        isLeaving = true;

        await ShutdownLobbyRunner();
        await ShutdownRoomRunner();

        SceneManager.LoadScene("Lobby1");
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (runner != lobbyRunner)
            return;

        Debug.Log("세션 목록 업데이트 수신: " + sessionList.Count);

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

        uiManager = FindObjectOfType<LobbyUIManager>();
        if (uiManager != null)
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
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}