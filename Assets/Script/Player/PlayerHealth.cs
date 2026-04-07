using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHp = 100;

    [Networked] public int CurrentHp { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }

    public int MaxHp => maxHp;
    public bool CanControl => !IsDead;
    public bool IsOut => IsDead;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHp = maxHp;
            IsDead = false;
        }
    }

    public void ApplyDamage(int damage)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsDead)
            return;

        damage = Mathf.Max(0, damage);
        CurrentHp = Mathf.Max(0, CurrentHp - damage);

        if (CurrentHp == 0)
            IsDead = true;
    }

    public void Death()
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsDead)
            return;

        CurrentHp = 0;
        IsDead = true;
    }
}