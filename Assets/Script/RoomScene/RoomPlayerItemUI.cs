using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomPlayerItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text txtPlayerName;
    [SerializeField] private TMP_Text txtStatus;
    [SerializeField] private Button btnRole;
    [SerializeField] private TMP_Text txtRole;

    private RoomPlayer targetPlayer;

    public RoomPlayer TargetPlayer => targetPlayer;

    public void Setup(RoomPlayer roomPlayer)
    {
        if (targetPlayer != null)
            RoomPlayer.AnyPlayerVisualChanged -= OnAnyPlayerVisualChanged;

        targetPlayer = roomPlayer;

        if (btnRole != null)
        {
            btnRole.onClick.RemoveAllListeners();
            btnRole.onClick.AddListener(OnClickRole);
        }

        RoomPlayer.AnyPlayerVisualChanged += OnAnyPlayerVisualChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        RoomPlayer.AnyPlayerVisualChanged -= OnAnyPlayerVisualChanged;
    }

    private void OnAnyPlayerVisualChanged(RoomPlayer changedPlayer)
    {
        if (changedPlayer == targetPlayer)
            Refresh();
    }

    public void Refresh()
    {
        if (targetPlayer == null)
            return;

        if (txtPlayerName != null)
            txtPlayerName.text = targetPlayer.DisplayName.ToString();

        if (txtRole != null)
            txtRole.text = GetRoleText(targetPlayer.SelectedRole);

        if (txtStatus != null)
        {
            if (targetPlayer.IsHostPlayer)
                txtStatus.text = "HOST";
            else
                txtStatus.text = targetPlayer.IsReady ? "READY" : "WAIT";
        }

        if (btnRole != null)
            btnRole.interactable = targetPlayer.IsLocalPlayer && !targetPlayer.IsReady;
    }

    private void OnClickRole()
    {
        if (targetPlayer == null)
            return;

        targetPlayer.ToggleRole();
    }

    private string GetRoleText(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Cop:
                return "COP";
            case PlayerRole.Robber:
                return "ROBBER";
            default:
                return "-";
        }
    }
}