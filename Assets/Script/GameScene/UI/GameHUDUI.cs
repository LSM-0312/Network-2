using Fusion;
using UnityEngine;

public class GameHUDUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private HpUI hpPanel;
    [SerializeField] private RoundInfoUI roundPanel;
    [SerializeField] private CenterMessageUI centerMessagePanel;

    private NetworkRunner runner;
    private PlayerAvatar localAvatar;
    private PlayerHealth localHealth;

    private void Update()
    {
        ResolveLocalPlayer();

        if (hpPanel != null)
        {
            if (localHealth != null)
                hpPanel.SetHp(localHealth.CurrentHp, localHealth.MaxHp, localHealth.IsDead);
            else
                hpPanel.Clear();
        }

        GameStateManager manager = GameStateManager.Instance;

        if (roundPanel != null)
        {
            if (manager != null)
                roundPanel.SetRound(manager);
            else
                roundPanel.Clear();
        }

        if (centerMessagePanel != null)
        {
            if (manager != null)
                centerMessagePanel.SetMessage(manager.GetCenterMessage());
            else
                centerMessagePanel.Clear();
        }
    }

    private void ResolveLocalPlayer()
    {
        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();

        if (runner == null || !runner.IsRunning)
        {
            localAvatar = null;
            localHealth = null;
            return;
        }

        if (localAvatar != null && localHealth != null)
            return;

        if (runner.TryGetPlayerObject(runner.LocalPlayer, out NetworkObject obj) && obj != null)
        {
            localAvatar = obj.GetComponent<PlayerAvatar>();
            localHealth = obj.GetComponent<PlayerHealth>();
        }
    }
}