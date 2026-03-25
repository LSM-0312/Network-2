using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomUIManager : MonoBehaviour
{
    [Header("Top")]
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private Button btnSettings;
    [SerializeField] private GameObject settingsPanel;

    [Header("Left Buttons")]
    [SerializeField] private Button btnMainAction;
    [SerializeField] private TextMeshProUGUI btnMainActionText;
    [SerializeField] private Button btnReturn;

    [Header("Player List")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject itemTemplate;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();
    private float refreshTimer;
    private const float RefreshInterval = 0.2f;

    private void Awake()
    {
        if (btnMainAction != null)
            btnMainAction.onClick.AddListener(OnClickMainAction);

        if (btnReturn != null)
            btnReturn.onClick.AddListener(OnClickReturn);

        if (btnSettings != null)
            btnSettings.onClick.AddListener(OnClickSettings);

        if (itemTemplate != null)
            itemTemplate.SetActive(false);
    }

    private void Update()
    {
        refreshTimer += Time.deltaTime;

        if (refreshTimer >= RefreshInterval)
        {
            refreshTimer = 0f;
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        RoomManager roomManager = RoomManager.Instance;
        if (roomManager == null)
            return;

        bool isHost = roomManager.IsLocalHost;
        RoomPlayer localPlayer = roomManager.GetLocalRoomPlayer();

        if (roomNameText != null)
            roomNameText.text = roomManager.RoomName;

        if (btnSettings != null)
            btnSettings.gameObject.SetActive(isHost);

        if (settingsPanel != null && !isHost)
            settingsPanel.SetActive(false);

        if (btnMainActionText != null)
        {
            if (isHost)
            {
                btnMainActionText.text = "Start";
            }
            else
            {
                bool isReady = localPlayer != null && localPlayer.IsReady;
                btnMainActionText.text = isReady ? "Cancel Ready" : "Ready";
            }
        }

        if (btnMainAction != null)
        {
            if (isHost)
                btnMainAction.interactable = roomManager.CanStartMatch();
            else
                btnMainAction.interactable = localPlayer != null;
        }

        RefreshPlayerList();
    }

    private void RefreshPlayerList()
    {
        ClearPlayerList();

        for (int i = 0; i < RoomPlayer.ActivePlayers.Count; i++)
        {
            RoomPlayer player = RoomPlayer.ActivePlayers[i];
            if (player == null)
                continue;

            GameObject item = Instantiate(itemTemplate, contentParent);
            item.SetActive(true);

            RoomPlayerItemUI itemUI = item.GetComponent<RoomPlayerItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(
                    player.DisplayName.ToString(),
                    player.IsReady,
                    player.IsHostPlayer,
                    player.IsLocalPlayer
                );
            }

            spawnedItems.Add(item);
        }
    }

    private void ClearPlayerList()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i]);
        }

        spawnedItems.Clear();
    }

    private void OnClickMainAction()
    {
        RoomManager roomManager = RoomManager.Instance;
        if (roomManager == null)
            return;

        if (roomManager.IsLocalHost)
        {
            roomManager.StartMatch();
        }
        else
        {
            RoomPlayer localPlayer = roomManager.GetLocalRoomPlayer();
            if (localPlayer != null)
                localPlayer.ToggleReady();
        }
    }

    private void OnClickReturn()
    {
        RoomManager roomManager = RoomManager.Instance;
        if (roomManager != null)
            roomManager.LeaveRoom();
    }

    private void OnClickSettings()
    {
        RoomManager roomManager = RoomManager.Instance;
        if (roomManager == null || !roomManager.IsLocalHost)
            return;

        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
}