using Fusion;
using UnityEngine;

public class GameStateManager : NetworkBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Rules")]
    [SerializeField] private float roundTime = 180f;
    [SerializeField] private float roundEndDelay = 3f;
    [SerializeField] private float matchEndDelay = 5f;
    [SerializeField] private int targetScore = 5;

    [Header("Scene")]
    [SerializeField] private int roomSceneBuildIndex = 2;

    [Header("Respawn")]
    [SerializeField] private Transform[] copSpawnPoints;
    [SerializeField] private Transform[] robberSpawnPoints;

    [Networked] public GameState State { get; private set; }
    [Networked] public int RoundNumber { get; private set; }
    [Networked] public int CopScore { get; private set; }
    [Networked] public int RobberScore { get; private set; }
    [Networked] public PlayerRole RoundWinner { get; private set; }
    [Networked] public PlayerRole MatchWinner { get; private set; }

    [Networked] public int AliveCopCount { get; private set; }
    [Networked] public int AliveRobberCount { get; private set; }

    [Networked] private TickTimer roundTimer { get; set; }
    [Networked] private TickTimer stateTimer { get; set; }

    [Networked] private NetworkBool roomSceneLoadRequested { get; set; }

    public bool IsPlaying => State == GameState.Playing;

    public float RemainingRoundTime
    {
        get
        {
            float? time = roundTimer.RemainingTime(Runner);
            return time.HasValue ? time.Value : 0f;
        }
    }

    public override void Spawned()
    {
        Instance = this;

        if (!Object.HasStateAuthority)
            return;

        State = GameState.Waiting;
        RoundNumber = 0;
        CopScore = 0;
        RobberScore = 0;
        RoundWinner = PlayerRole.None;
        MatchWinner = PlayerRole.None;
        AliveCopCount = 0;
        AliveRobberCount = 0;
        roundTimer = TickTimer.None;
        stateTimer = TickTimer.None;
        roomSceneLoadRequested = false;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }

    public override void FixedUpdateNetwork() // good
    {
        if (!Object.HasStateAuthority)
            return;

        switch (State)
        {
            case GameState.Waiting:
                TryStartFirstRound();
                break;

            case GameState.Playing:
                UpdatePlaying();
                break;

            case GameState.RoundEnded:
                UpdateRoundEnded();
                break;

            case GameState.MatchEnded:
                UpdateGameEnded();
                break;
        }
    }

    private void TryStartFirstRound()
    {
        if (!ArePlayerObjectsReady())
            return;

        CountAlivePlayers(out int copCount, out int robberCount);

        if (copCount <= 0 || robberCount <= 0)
            return;

        StartNextRound();
    }

    private bool ArePlayerObjectsReady()
    {
        bool hasPlayer = false;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            hasPlayer = true;

            if (!Runner.TryGetPlayerObject(player, out NetworkObject obj))
                return false;

            if (obj == null)
                return false;

            if (obj.GetComponent<PlayerAvatar>() == null)
                return false;

            if (obj.GetComponent<PlayerHealth>() == null)
                return false;
        }

        return hasPlayer;
    }

    private void StartNextRound()
    {
        RoundNumber++;
        RoundWinner = PlayerRole.None;
        State = GameState.Playing;

        ResetAllPlayersForRound();

        CountAlivePlayers(out int copCount, out int robberCount);
        AliveCopCount = copCount;
        AliveRobberCount = robberCount;

        roundTimer = TickTimer.CreateFromSeconds(Runner, roundTime);
        stateTimer = TickTimer.None;

        Debug.Log($"[GameStateManager] Round {RoundNumber} Start");
    }

    private void UpdatePlaying()
    {
        if (AliveRobberCount <= 0)
        {
            EndRound(PlayerRole.Cop);
            return;
        }

        if (AliveCopCount <= 0)
        {
            EndRound(PlayerRole.Robber);
            return;
        }

        if (roundTimer.Expired(Runner))
        {
            EndRound(PlayerRole.Robber);
            return;
        }
    }

    public void ServerNotifyPlayerDied(PlayerRole role)
    {
        if (!Object.HasStateAuthority)
            return;

        if (State != GameState.Playing)
            return;

        if (role == PlayerRole.Cop)
            AliveCopCount = Mathf.Max(0, AliveCopCount - 1);
        else if (role == PlayerRole.Robber)
            AliveRobberCount = Mathf.Max(0, AliveRobberCount - 1);

        UpdatePlaying();
    }

    private void EndRound(PlayerRole winner)
    {
        if (State != GameState.Playing)
            return;

        RoundWinner = winner;
        State = GameState.RoundEnded;
        roundTimer = TickTimer.None;

        if (winner == PlayerRole.Cop)
            CopScore++;
        else if (winner == PlayerRole.Robber)
            RobberScore++;

        Debug.Log($"[GameStateManager] Round End. Winner={winner}");

        if (CopScore >= targetScore)
        {
            EndGame(PlayerRole.Cop);
            return;
        }

        if (RobberScore >= targetScore)
        {
            EndGame(PlayerRole.Robber);
            return;
        }

        stateTimer = TickTimer.CreateFromSeconds(Runner, roundEndDelay);
    }

    private void EndGame(PlayerRole winner)
    {
        MatchWinner = winner;
        State = GameState.MatchEnded;
        stateTimer = TickTimer.CreateFromSeconds(Runner, matchEndDelay);

        Debug.Log($"[GameStateManager] Game End. Winner : {winner}");
    }

    private void UpdateRoundEnded()
    {
        if (!stateTimer.Expired(Runner))
            return;

        StartNextRound();
    }

    private void UpdateGameEnded()
    {
        if (roomSceneLoadRequested)
            return;

        if (!stateTimer.Expired(Runner))
            return;

        if (!Runner.IsSceneAuthority)
            return;

        roomSceneLoadRequested = true;
        stateTimer = TickTimer.None;

        Runner.LoadScene(SceneRef.FromIndex(roomSceneBuildIndex));
    }

    private void CountAlivePlayers(out int copCount, out int robberCount)
    {
        copCount = 0;
        robberCount = 0;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(player, out NetworkObject obj))
                continue;

            if (obj == null)
                continue;

            PlayerAvatar avatar = obj.GetComponent<PlayerAvatar>();
            PlayerHealth health = obj.GetComponent<PlayerHealth>();

            if (avatar == null || health == null)
                continue;

            if (health.IsDead)
                continue;

            if (avatar.Role == PlayerRole.Cop)
                copCount++;
            else if (avatar.Role == PlayerRole.Robber)
                robberCount++;
        }
    }

    private void ResetAllPlayersForRound()
    {
        int copIndex = 0;
        int robberIndex = 0;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(player, out NetworkObject obj))
                continue;

            if (obj == null)
                continue;

            PlayerAvatar avatar = obj.GetComponent<PlayerAvatar>();
            if (avatar == null)
                continue;

            Transform spawnPoint = null;

            if (avatar.Role == PlayerRole.Cop)
            {
                spawnPoint = GetSpawnPoint(copSpawnPoints, copIndex);
                copIndex++;
            }
            else if (avatar.Role == PlayerRole.Robber)
            {
                spawnPoint = GetSpawnPoint(robberSpawnPoints, robberIndex);
                robberIndex++;
            }

            if (spawnPoint == null)
                continue;

            ResetPlayerForRound(obj, spawnPoint);
        }
    }

    private void ResetPlayerForRound(NetworkObject obj, Transform spawnPoint)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        PlayerHealth health = obj.GetComponent<PlayerHealth>();
        FallDamage fallDamage = obj.GetComponent<FallDamage>();
        PlayerItemController itemController = obj.GetComponent<PlayerItemController>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = spawnPoint.position;
            rb.rotation = spawnPoint.rotation;
        }

        if (health != null)
            health.ResetRound();

        if (fallDamage != null)
            fallDamage.ResetRound();

        if (itemController != null)
            itemController.ResetRound();
    }

    private Transform GetSpawnPoint(Transform[] points, int index)
    {
        if (points == null || points.Length == 0)
            return null;

        return points[index % points.Length];
    }

    public string GetCenterMessage()
    {
        if (State == GameState.RoundEnded)
            return $"{RoundWinner} WIN THIS ROUND";

        if (State == GameState.MatchEnded)
            return $"{MatchWinner} FINAL WIN";

        return "";
    }
}