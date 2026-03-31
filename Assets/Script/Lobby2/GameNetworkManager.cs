using Fusion;
using Fusion.Addons.Physics;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static GameNetworkManager Instance { get; private set; }

    [Header("Scene")]
    [SerializeField] private int roomSceneBuildIndex = 2;
    [SerializeField] private string lobby1SceneName = "Lobby1";
    [SerializeField] private string lobby2SceneName = "Lobby2";

    [Header("Create Room")]
    [SerializeField] private string roomNamePrefix = "Room_";
    [SerializeField] private int maxPlayers = 4;

    private NetworkRunner runner;
    private NetworkSceneManagerDefault sceneManager;
    private LobbyUIManager uiManager;

    private readonly List<SessionInfo> cachedSessionList = new();
    private readonly Dictionary<PlayerRef, PlayerRole> cachedSelectedRoles = new();

    private bool isBusy = false;
    private bool isInSession = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != lobby2SceneName)
            return;

        Debug.Log("Lobby2 씬 로드 감지");

        uiManager = FindObjectOfType<LobbyUIManager>(true);

        if (runner == null && !isInSession && !isBusy)
            await EnsureRunnerAndJoinLobby();
        else
            UpdateQuickJoinButton();
    }

    private NetworkRunner CreateRunner()
    {
        GameObject go = new GameObject("GameRunner");
        DontDestroyOnLoad(go);

        runner = go.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        sceneManager = go.AddComponent<NetworkSceneManagerDefault>();
        go.AddComponent<RunnerSimulatePhysics3D>();

        runner.AddCallbacks(this);
        return runner;
    }

    private async Task EnsureRunnerAndJoinLobby()
    {
        if (isBusy)
            return;

        isBusy = true;

        if (runner == null)
            CreateRunner();

        cachedSessionList.Clear();

        uiManager = FindObjectOfType<LobbyUIManager>(true);
        if (uiManager != null)
            uiManager.ClearRoomList();

        var result = await runner.JoinSessionLobby(SessionLobby.ClientServer);

        Debug.Log($"Lobby 접속 결과: {result.Ok}");

        if (!result.Ok)
            Debug.LogError($"Lobby 접속 실패: {result.ShutdownReason}");

        isBusy = false;
        UpdateQuickJoinButton();
    }

    public async void RefreshLobby()
    {
        if (isBusy || runner == null || isInSession)
            return;

        isBusy = true;
        cachedSessionList.Clear();

        uiManager = FindObjectOfType<LobbyUIManager>(true);
        if (uiManager != null)
            uiManager.ClearRoomList();

        var result = await runner.JoinSessionLobby(SessionLobby.ClientServer);

        Debug.Log($"Lobby REFRESH 결과: {result.Ok}");

        if (!result.Ok)
            Debug.LogError($"Lobby REFRESH 실패: {result.ShutdownReason}");

        isBusy = false;
        UpdateQuickJoinButton();
    }

    public async void CreateRoom()
    {
        if (isBusy || runner == null || isInSession)
            return;

        isBusy = true;

        string roomName = roomNamePrefix + UnityEngine.Random.Range(1000, 9999);

        StartGameResult result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            PlayerCount = maxPlayers,
            Scene = SceneRef.FromIndex(roomSceneBuildIndex),
            SceneManager = sceneManager
        });

        Debug.Log($"방 생성 결과: {result.Ok}");

        if (result.Ok)
            isInSession = true;
        else
            Debug.LogError($"방 생성 실패: {result.ShutdownReason}");

        isBusy = false;
    }

    public async void QuickJoin()
    {
        if (isBusy || runner == null || isInSession)
            return;

        if (!HasJoinableRoom())
        {
            Debug.LogWarning("참가 가능한 방이 없습니다.");
            UpdateQuickJoinButton();
            return;
        }

        isBusy = true;

        StartGameResult result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            MatchmakingMode = MatchmakingMode.FillRoom,
            Scene = SceneRef.FromIndex(roomSceneBuildIndex),
            SceneManager = sceneManager
        });

        Debug.Log($"빠른 참가 결과: {result.Ok}");

        if (result.Ok)
            isInSession = true;
        else
            Debug.LogWarning($"빠른 참가 실패: {result.ShutdownReason}");

        isBusy = false;
    }

    public async void ReturnToLobby1()
    {
        if (isBusy)
            return;

        isBusy = true;
        await ShutdownRunner();
        SceneManager.LoadScene(lobby1SceneName);
    }

    public async void LeaveSessionAndReturnToLobby2()
    {
        if (isBusy)
            return;

        isBusy = true;
        await ShutdownRunner();
        SceneManager.LoadScene(lobby2SceneName);
    }

    private async Task ShutdownRunner()
    {
        if (runner == null)
        {
            sceneManager = null;
            isInSession = false;
            isBusy = false;
            cachedSessionList.Clear();
            cachedSelectedRoles.Clear();
            UpdateQuickJoinButton();
            return;
        }

        try
        {
            runner.RemoveCallbacks(this);
        }
        catch
        {
        }

        await runner.Shutdown();

        if (runner.gameObject != null)
            Destroy(runner.gameObject);

        runner = null;
        sceneManager = null;
        isInSession = false;
        isBusy = false;

        cachedSessionList.Clear();
        cachedSelectedRoles.Clear();
        UpdateQuickJoinButton();
    }

    private bool HasJoinableRoom()
    {
        for (int i = 0; i < cachedSessionList.Count; i++)
        {
            SessionInfo session = cachedSessionList[i];

            if (!session.IsVisible)
                continue;

            if (!session.IsOpen)
                continue;

            if (session.PlayerCount >= session.MaxPlayers)
                continue;

            return true;
        }

        return false;
    }

    private void UpdateQuickJoinButton()
    {
        uiManager = FindObjectOfType<LobbyUIManager>(true);
        if (uiManager == null || uiManager.btnQuickJoin == null)
            return;

        if (isInSession || isBusy)
        {
            uiManager.btnQuickJoin.interactable = false;
            return;
        }

        uiManager.btnQuickJoin.interactable = HasJoinableRoom();
    }

    public NetworkRunner GetRunner()
    {
        return runner;
    }

    public bool HasRunner()
    {
        return runner != null;
    }

    public bool IsInSession()
    {
        return isInSession;
    }

    public void CacheSelectedRole(PlayerRef player, PlayerRole role)
    {
        cachedSelectedRoles[player] = role;
    }

    public bool TryGetCachedRole(PlayerRef player, out PlayerRole role)
    {
        return cachedSelectedRoles.TryGetValue(player, out role);
    }

    public void RemoveCachedRole(PlayerRef player)
    {
        cachedSelectedRoles.Remove(player);
    }

    public void ClearCachedRoles()
    {
        cachedSelectedRoles.Clear();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (runner != this.runner || isInSession)
            return;

        cachedSessionList.Clear();
        cachedSessionList.AddRange(sessionList);

        List<RoomInfoData> roomList = new();

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

        uiManager = FindObjectOfType<LobbyUIManager>(true);
        if (uiManager != null)
            uiManager.RefreshRoomList(roomList);

        UpdateQuickJoinButton();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        isInSession = false;
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("서버 연결 성공");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"서버 연결 해제: {reason}");
        isInSession = false;
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"서버 연결 실패: {reason}");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log($"씬 로드 완료: {SceneManager.GetActiveScene().name}");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("씬 로드 시작");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"플레이어 입장: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"플레이어 퇴장: {player}");
        RemoveCachedRole(player);
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}