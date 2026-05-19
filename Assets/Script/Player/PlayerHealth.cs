using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHp = 100;

    [Networked] public int CurrentHp { get; private set; }
    [Networked] public NetworkBool IsDead { get; private set; }

    private PlayerAvatar avatar;

    public int MaxHp => maxHp;
    public bool CanControl => !IsDead;
    public bool IsOut => IsDead;

    private void Awake()
    {
        TryGetComponent(out avatar);
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            ResetRound();
    }

    public void ApplyDamage(int damage)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsDead)
            return;

        damage = Mathf.Max(0, damage);
        CurrentHp = Mathf.Max(0, CurrentHp - damage);

        if (CurrentHp <= 0)
            Death();
    }

    public void Death()
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsDead)
            return;

        CurrentHp = 0;
        IsDead = true;

        if (GameStateManager.Instance != null && avatar != null)
            GameStateManager.Instance.ServerNotifyPlayerDied(avatar.Role);
    }

    public void ResetRound()
    {
        if (!Object.HasStateAuthority)
            return;

        CurrentHp = maxHp;
        IsDead = false;
    }
}