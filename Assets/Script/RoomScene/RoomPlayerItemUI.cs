using TMPro;
using UnityEngine;

public class RoomPlayerItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI statusText;

    public void Setup(string playerName, bool isReady, bool isHost, bool isLocal)
    {
        if (playerNameText != null)
            playerNameText.text = isLocal ? $"{playerName} (You)" : playerName;

        if (statusText != null)
        {
            if (isHost)
                statusText.text = "Host";
            else
                statusText.text = isReady ? "Ready" : "Not Ready";
        }
    }
}