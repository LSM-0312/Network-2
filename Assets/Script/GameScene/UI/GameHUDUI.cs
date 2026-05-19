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
        GameStateManager manager = GameStateManager.Instance;

        if (manager == null)
        {
            ClearAll();
            ClearLocalPlayerCache();
            return;
        }

        ResolveLocalPlayer();

        if (hpPanel != null)
        {
            if (IsLocalHealthUsable())
                hpPanel.SetHp(localHealth.CurrentHp, localHealth.MaxHp, localHealth.IsDead);
            else
                hpPanel.Clear();
        }

        if (roundPanel != null)
            roundPanel.SetRound(manager);

        if (centerMessagePanel != null)
            centerMessagePanel.SetMessage(manager.GetCenterMessage());
    }

    private void ResolveLocalPlayer()
    {
        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();

        if (runner == null || !runner.IsRunning)
        {
            ClearLocalPlayerCache();
            return;
        }

        if (IsLocalPlayerCacheUsable())
            return;

        ClearLocalPlayerCache();

        if (runner.TryGetPlayerObject(runner.LocalPlayer, out NetworkObject obj) && obj != null)
        {
            localAvatar = obj.GetComponent<PlayerAvatar>();
            localHealth = obj.GetComponent<PlayerHealth>();
        }
    }

    private bool IsLocalPlayerCacheUsable()
    {
        if (localAvatar == null || localHealth == null)
            return false;

        if (localAvatar.Object == null || localHealth.Object == null)
            return false;

        return true;
    }

    private bool IsLocalHealthUsable()
    {
        if (localHealth == null)
            return false;

        if (localHealth.Object == null)
            return false;

        return true;
    }

    private void ClearAll()
    {
        if (hpPanel != null)
            hpPanel.Clear();

        if (roundPanel != null)
            roundPanel.Clear();

        if (centerMessagePanel != null)
            centerMessagePanel.Clear();
    }

    private void ClearLocalPlayerCache()
    {
        localAvatar = null;
        localHealth = null;
    }

    private void OnDisable()
    {
        ClearAll();
        ClearLocalPlayerCache();
    }

    private void OnDestroy()
    {
        ClearLocalPlayerCache();
    }
}