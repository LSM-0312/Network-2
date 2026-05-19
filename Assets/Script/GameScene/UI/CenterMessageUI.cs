using TMPro;
using UnityEngine;

public class CenterMessageUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;

    public void SetMessage(string message)
    {
        if (messageText == null)
            return;

        messageText.text = message;
    }

    public void Clear()
    {
        if (messageText != null)
            messageText.text = "";
    }
}