using Fusion;
using UnityEngine;

public class MeleeAttack : NetworkBehaviour
{
    [Header("Shape")]
    [SerializeField] private float attackHeight = 0.9f;
    [SerializeField] private float attackHalfAngle = 50f;

    [Networked] private TickTimer cooldownTimer { get; set; }

    private readonly Collider[] hitBuffer = new Collider[16];

    private PlayerAvatar avatar;
    private PlayerHealth health;

    private void Awake()
    {
        TryGetComponent(out avatar);
        TryGetComponent(out health);
    }

    public bool TryAttack(MeleeItemDefinition item)
    {
        if (!Object.HasStateAuthority)
            return false;

        if (item == null)
            return false;

        if (health != null && !health.CanControl)
            return false;

        if (!cooldownTimer.ExpiredOrNotRunning(Runner))
            return false;

        IDamageable target = FindBestTarget(item);
        if (target == null)
            return false;

        target.ApplyDamage(item.damage);
        cooldownTimer = TickTimer.CreateFromSeconds(Runner, item.cooldown);
        return true;
    }

    private IDamageable FindBestTarget(MeleeItemDefinition item)
    {
        Vector3 origin = transform.position + Vector3.up * attackHeight + transform.forward * (item.attackRange * 0.5f);

        int hitCount = Runner.GetPhysicsScene().OverlapSphere(
            origin,
            item.attackRadius,
            hitBuffer,
            item.targetMask,
            QueryTriggerInteraction.Ignore
        );

        IDamageable bestTarget = null;
        float bestSqr = float.MaxValue;
        float minDot = Mathf.Cos(attackHalfAngle * Mathf.Deg2Rad);

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitBuffer[i];
            if (col == null)
                continue;

            IDamageable damageable = FindDamageable(col);
            if (damageable == null)
                continue;

            PlayerHealth targetHealth = col.GetComponentInParent<PlayerHealth>();
            if (targetHealth == null)
                continue;

            if (targetHealth == health)
                continue;

            if (targetHealth.IsOut)
                continue;

            PlayerAvatar targetAvatar = targetHealth.GetComponent<PlayerAvatar>();
            if (targetAvatar == null)
                continue;

            if (!IsEnemyRole(avatar != null ? avatar.Role : PlayerRole.None, targetAvatar.Role))
                continue;

            Vector3 toTarget = targetHealth.transform.position - transform.position;
            toTarget.y = 0f;

            float sqr = toTarget.sqrMagnitude;
            if (sqr > item.attackRange * item.attackRange)
                continue;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float dot = Vector3.Dot(transform.forward, toTarget.normalized);
                if (dot < minDot)
                    continue;
            }

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestTarget = damageable;
            }
        }

        return bestTarget;
    }

    private IDamageable FindDamageable(Collider col)
    {
        MonoBehaviour[] behaviours = col.GetComponentsInParent<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IDamageable damageable)
                return damageable;
        }

        return null;
    }

    private bool IsEnemyRole(PlayerRole attackerRole, PlayerRole targetRole)
    {
        if (attackerRole == PlayerRole.Cop && targetRole == PlayerRole.Robber)
            return true;

        if (attackerRole == PlayerRole.Robber && targetRole == PlayerRole.Cop)
            return true;

        return false;
    }
}