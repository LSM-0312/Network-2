using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button btnReturn;
    public Button btnCreate;
    public Button btnRefresh;
    public Button btnQuickJoin;

    [Header("Room List")]
    public Transform contentParent;
    public GameObject itemTemplate;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();
    private LobbyFusionManager fusionManager;

    private void Awake()
    {
        fusionManager = FindObjectOfType<LobbyFusionManager>();

        if (btnReturn != null)
            btnReturn.onClick.AddListener(OnClickReturn);

        if (btnCreate != null)
            btnCreate.onClick.AddListener(OnClickCreate);

        if (btnRefresh != null)
            btnRefresh.onClick.AddListener(OnClickRefresh);

        if (btnQuickJoin != null)
            btnQuickJoin.onClick.AddListener(OnClickQuickJoin);

        if (itemTemplate != null)
            itemTemplate.SetActive(false);
    }

    public void RefreshRoomList(List<RoomInfoData> rooms)
    {
        ClearRoomList();

        foreach (RoomInfoData room in rooms)
        {
            GameObject item = Instantiate(itemTemplate, contentParent);
            item.SetActive(true);

            LobbyRoomItemUI itemUI = item.GetComponent<LobbyRoomItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(
                    room.roomName,
                    room.currentPlayers,
                    room.maxPlayers,
                    room.isOpen
                );
            }

            spawnedItems.Add(item);
        }
    }

    public void ClearRoomList()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i]);
        }

        spawnedItems.Clear();
    }

    private void OnClickReturn()
    {
        if (fusionManager != null)
            fusionManager.ReturnToLobby1();
        else
            SceneManager.LoadScene("Lobby1");
    }

    private void OnClickCreate()
    {
        if (fusionManager != null)
            fusionManager.CreateRoom();
    }

    private void OnClickRefresh()
    {
        if (fusionManager != null)
            fusionManager.RefreshLobby();
    }

    private void OnClickQuickJoin()
    {
        if (fusionManager != null)
            fusionManager.QuickJoin();
    }
}