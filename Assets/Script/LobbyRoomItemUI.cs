using TMPro;
using UnityEngine;

public class LobbyRoomItemUI : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI statusText;

    public void Setup(string roomName, int currentPlayers, int maxPlayers, bool isOpen)
    {
        roomNameText.text = roomName;
        playerCountText.text = $"{currentPlayers} / {maxPlayers}";
        statusText.text = isOpen ? "Open" : "Closed";
    }
}