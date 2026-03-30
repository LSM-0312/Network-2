using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomUIManager : MonoBehaviour
{
    [Header("Top")]
    [SerializeField] private TMP_Text txtRoomTitle;

    [Header("Buttons")]
    [SerializeField] private Button btnAction;
    [SerializeField] private Button btnReturn;

    [Header("Player List")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject itemTemplate;

    private readonly List<RoomPlayerItemUI> spawnedItems = new List<RoomPlayerItemUI>();

    private void Awake()
    {
        if (itemTemplate != null)
            itemTemplate.SetActive(false);

        if (btnAction != null)
            btnAction.onClick.AddListener(OnClickAction);

        if (btnReturn != null)
            btnReturn.onClick.AddListener(OnClickReturn);
    }

    private void OnEnable()
    {
        RoomPlayer.ActivePlayersChanged += HandlePlayersChanged;
        RoomPlayer.AnyPlayerVisualChanged += HandleAnyPlayerVisualChanged;
        RoomManager.RoomVisualChanged += HandleRoomVisualChanged;

        RefreshAll();
    }

    private void OnDisable()
    {
        RoomPlayer.ActivePlayersChanged -= HandlePlayersChanged;
        RoomPlayer.AnyPlayerVisualChanged -= HandleAnyPlayerVisualChanged;
        RoomManager.RoomVisualChanged -= HandleRoomVisualChanged;
    }

    private void HandlePlayersChanged()
    {
        RebuildPlayerList();
        RefreshActionButton();
    }

    private void HandleAnyPlayerVisualChanged(RoomPlayer _)
    {
        RefreshActionButton();
    }

    private void HandleRoomVisualChanged()
    {
        RefreshRoomTitle();
        RefreshActionButton();
    }

    private void RefreshAll()
    {
        RefreshRoomTitle();
        RebuildPlayerList();
        RefreshActionButton();
    }

    private void RefreshRoomTitle()
    {
        if (txtRoomTitle == null)
            return;

        if (RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.RoomName))
            txtRoomTitle.text = RoomManager.Instance.RoomName;
        else
            txtRoomTitle.text = "ROOM";
    }

    private void RebuildPlayerList()
    {
        ClearPlayerList();

        List<RoomPlayer> players = new List<RoomPlayer>(RoomPlayer.ActivePlayers);

        players.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            if (a.IsHostPlayer && !b.IsHostPlayer) return -1;
            if (!a.IsHostPlayer && b.IsHostPlayer) return 1;

            return string.Compare(a.DisplayName.ToString(), b.DisplayName.ToString(), System.StringComparison.Ordinal);
        });

        for (int i = 0; i < players.Count; i++)
        {
            RoomPlayer player = players[i];
            if (player == null)
                continue;

            GameObject item = Instantiate(itemTemplate, contentParent);
            item.SetActive(true);

            RoomPlayerItemUI itemUI = item.GetComponent<RoomPlayerItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(player);
                spawnedItems.Add(itemUI);
            }
        }

        RectTransform rect = contentParent as RectTransform;
        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void ClearPlayerList()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
    }

    private void RefreshActionButton()
    {
        if (btnAction == null)
            return;

        bool isHostLocal = RoomManager.Instance != null && RoomManager.Instance.IsLocalHost;
        RoomPlayer localPlayer = RoomManager.Instance != null
            ? RoomManager.Instance.GetLocalRoomPlayer()
            : null;

        TMP_Text actionText = btnAction.GetComponentInChildren<TMP_Text>(true);

        btnAction.gameObject.SetActive(true);

        if (isHostLocal)
        {
            if (actionText != null)
                actionText.text = "Start";

            btnAction.interactable = RoomManager.Instance != null && RoomManager.Instance.CanStartMatch();
            return;
        }

        if (actionText != null)
            actionText.text = localPlayer != null && localPlayer.IsReady ? "Cancel" : "Ready";

        btnAction.interactable = localPlayer != null;
    }

    private void OnClickAction()
    {
        bool isHostLocal = RoomManager.Instance != null && RoomManager.Instance.IsLocalHost;

        if (isHostLocal)
        {
            if (RoomManager.Instance != null)
                RoomManager.Instance.StartMatch();

            return;
        }

        RoomPlayer localPlayer = RoomManager.Instance != null
            ? RoomManager.Instance.GetLocalRoomPlayer()
            : null;

        if (localPlayer != null)
            localPlayer.ToggleReady();
    }

    private void OnClickReturn()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.LeaveRoom();
    }
}