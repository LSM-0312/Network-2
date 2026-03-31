using Fusion;
using UnityEngine;

public class GameState : NetworkBehaviour
{
    public static GameState Instance { get; private set; }

    public const int MAX_PLAYERS = 4;

    [Header("Rules")]
    [SerializeField] private int _winScore = 5;
    [SerializeField] private float _postWinSeconds = 5f;

    [Networked] public NetworkBool IsGameStarted { get; private set; }

    [Networked, Capacity(MAX_PLAYERS)] private NetworkArray<NetworkBool> _connected => default;
    [Networked, Capacity(MAX_PLAYERS)] private NetworkArray<NetworkBool> _ready => default;
    [Networked, Capacity(MAX_PLAYERS)] private NetworkArray<int> _scores => default;

    [Networked, Capacity(MAX_PLAYERS)] private NetworkDictionary<PlayerRef, byte> _playerToSlot => default;
    [Networked, Capacity(MAX_PLAYERS)] private NetworkDictionary<PlayerRef, byte> _playerToRole => default;

    [Networked] private int ScoreSeq { get; set; }
    [Networked] private byte LastScoreSlot { get; set; }

    [Networked] public NetworkBool IsRoundOver { get; private set; }
    [Networked] public byte WinnerSlot { get; private set; }
    [Networked] private PlayerRef WinnerPlayer { get; set; }

    [Networked] private TickTimer _endTimer { get; set; }
    [Networked] public int LobbyResetSeq { get; private set; }

    [Header("Score FX (Local)")]
    [SerializeField] private AudioSource _scoreAudio;
    [SerializeField] private ParticleSystem _scoreParticle;

    private ChangeDetector _cd;

    public override void Spawned()
    {
        Instance = this;
        _cd = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (Object.HasStateAuthority)
        {
            ScoreSeq = 0;
            LastScoreSlot = 255;

            IsRoundOver = false;
            WinnerSlot = 255;
            WinnerPlayer = PlayerRef.None;

            _endTimer = TickTimer.None;
            LobbyResetSeq = 0;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (IsRoundOver && _endTimer.Expired(Runner))
            Server_ResetToLobbyState();
    }

    public override void Render()
    {
        if (_cd == null) return;

        foreach (var change in _cd.DetectChanges(this))
        {
            if (change == nameof(ScoreSeq))
            {
                if (_scoreAudio) _scoreAudio.Play();
                if (_scoreParticle) _scoreParticle.Play(true);
            }
        }
    }

    private int MaxSlots => MAX_PLAYERS;

    public bool TryGetFreeSlot(out byte slot)
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (_connected.Get(i) == false)
            {
                slot = (byte)i;
                return true;
            }
        }

        slot = 255;
        return false;
    }

    public void RegisterPlayerSlot(PlayerRef player, byte slot)
    {
        if (!Object.HasStateAuthority) return;
        if (slot >= MaxSlots) return;

        if (_playerToSlot.ContainsKey(player))
            _playerToSlot.Set(player, slot);
        else
            _playerToSlot.Add(player, slot);

        RegisterSlot(slot);
    }

    public void UnregisterPlayerSlot(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        if (_playerToSlot.ContainsKey(player))
        {
            byte slot = _playerToSlot.Get(player);
            _playerToSlot.Remove(player);
            UnregisterSlot(slot);
        }

        if (_playerToRole.ContainsKey(player))
            _playerToRole.Remove(player);
    }

    public bool TryGetSlot(PlayerRef player, out byte slot)
    {
        slot = 255;

        if (_playerToSlot.ContainsKey(player))
        {
            slot = _playerToSlot.Get(player);
            return true;
        }

        return false;
    }

    public void ServerSetRole(PlayerRef player, PlayerRole role)
    {
        if (!Object.HasStateAuthority) return;

        byte value = (byte)role;

        if (_playerToRole.ContainsKey(player))
            _playerToRole.Set(player, value);
        else
            _playerToRole.Add(player, value);
    }

    public bool TryGetRole(PlayerRef player, out PlayerRole role)
    {
        role = PlayerRole.Robber;

        if (_playerToRole.ContainsKey(player))
        {
            role = (PlayerRole)_playerToRole.Get(player);
            return true;
        }

        return false;
    }

    private void RegisterSlot(byte slot)
    {
        if (!Object.HasStateAuthority) return;
        if (slot >= MaxSlots) return;

        _connected.Set(slot, true);
        _ready.Set(slot, false);
    }

    private void UnregisterSlot(byte slot)
    {
        if (!Object.HasStateAuthority) return;
        if (slot >= MaxSlots) return;

        _connected.Set(slot, false);
        _ready.Set(slot, false);
        _scores.Set(slot, 0);
    }

    public void ServerSetReady(byte slot, bool ready)
    {
        if (!Object.HasStateAuthority) return;
        if (IsGameStarted || IsRoundOver) return;
        if (slot >= MaxSlots) return;
        if (_connected.Get(slot) == false) return;

        _ready.Set(slot, ready);
        CheckAllReadyThenStart();
    }

    private void CheckAllReadyThenStart()
    {
        if (!Object.HasStateAuthority) return;
        if (IsGameStarted || IsRoundOver) return;

        bool any = false;

        for (int i = 0; i < MaxSlots; i++)
        {
            if (_connected.Get(i))
            {
                any = true;

                if (_ready.Get(i) == false)
                    return;
            }
        }

        if (!any) return;

        for (int i = 0; i < MaxSlots; i++)
        {
            if (_connected.Get(i))
                _scores.Set(i, 0);
        }

        WinnerSlot = 255;
        WinnerPlayer = PlayerRef.None;
        IsRoundOver = false;
        _endTimer = TickTimer.None;

        IsGameStarted = true;
        RPC_OnGameStarted();
    }

    public void Server_TryAwardPoint(PlayerRef toucher)
    {
        if (!Object.HasStateAuthority) return;
        if (!IsGameStarted) return;
        if (IsRoundOver) return;

        if (!TryGetSlot(toucher, out byte slot))
            return;

        if (slot >= MaxSlots) return;
        if (_connected.Get(slot) == false) return;

        int next = _scores.Get(slot) + 1;
        _scores.Set(slot, next);

        LastScoreSlot = slot;
        ScoreSeq++;

        if (next >= _winScore)
            Server_EndRound(slot, toucher);
    }

    public void ServerEndGame()
    {
        if (!Object.HasStateAuthority) return;
        if (!IsGameStarted && !IsRoundOver) return;

        IsGameStarted = false;
        IsRoundOver = true;
        WinnerSlot = 255;
        WinnerPlayer = PlayerRef.None;

        _endTimer = TickTimer.CreateFromSeconds(Runner, _postWinSeconds);

        RPC_OnRoundEnded(255);
    }

    private void Server_EndRound(byte winnerSlot, PlayerRef winnerPlayer)
    {
        if (!Object.HasStateAuthority) return;
        if (IsRoundOver) return;

        IsGameStarted = false;
        IsRoundOver = true;
        WinnerSlot = winnerSlot;
        WinnerPlayer = winnerPlayer;

        _endTimer = TickTimer.CreateFromSeconds(Runner, _postWinSeconds);

        RPC_OnRoundEnded(winnerSlot);
    }

    private void Server_ResetToLobbyState()
    {
        if (!Object.HasStateAuthority) return;

        IsGameStarted = false;
        IsRoundOver = false;
        WinnerSlot = 255;
        WinnerPlayer = PlayerRef.None;
        _endTimer = TickTimer.None;

        for (int i = 0; i < MaxSlots; i++)
        {
            if (_connected.Get(i))
            {
                _scores.Set(i, 0);
                _ready.Set(i, false);
            }
        }

        LobbyResetSeq++;
        RPC_OnLobbyReset();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnGameStarted()
    {
        Debug.Log("[GameState] Game Started");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnRoundEnded(byte winnerSlot)
    {
        Debug.Log($"[GameState] Round Ended. WinnerSlot={winnerSlot}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnLobbyReset()
    {
        Debug.Log("[GameState] Lobby Reset (back to Ready)");
    }

    public int GetScore(byte slot) => slot < MaxSlots ? _scores.Get(slot) : 0;
    public bool IsSlotConnected(byte slot) => slot < MaxSlots && _connected.Get(slot);
    public bool IsSlotReady(byte slot) => slot < MaxSlots && _ready.Get(slot);

    public string GetWinnerText()
    {
        if (!IsRoundOver) return "";
        if (WinnerSlot >= MaxSlots) return "GAME ENDED";
        return $"Player {WinnerSlot + 1} WIN!";
    }
}