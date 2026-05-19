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

        if (avatar == null || avatar.Role != PlayerRole.Cop)
            return false;

        if (health == null || !health.CanControl)
            return false;

        if (!cooldownTimer.ExpiredOrNotRunning(Runner))
            return false;

        cooldownTimer = TickTimer.CreateFromSeconds(Runner, item.cooldown);

        PlayerHealth target = FindBestTarget(item);
        if (target == null)
            return true;

        target.ApplyDamage(item.damage);
        return true;
    }

    private PlayerHealth FindBestTarget(MeleeItemDefinition item)
    {
        Vector3 origin = transform.position + Vector3.up * attackHeight + transform.forward * (item.attackRange * 0.5f);

        int hitCount = Runner.GetPhysicsScene().OverlapSphere(
            origin,
            item.attackRadius,
            hitBuffer,
            item.targetMask,
            QueryTriggerInteraction.Ignore
        );

        PlayerHealth bestTarget = null;
        float bestSqr = float.MaxValue;
        float minDot = Mathf.Cos(attackHalfAngle * Mathf.Deg2Rad);

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitBuffer[i];
            if (col == null)
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

            if (targetAvatar.Role != PlayerRole.Robber)
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
                bestTarget = targetHealth;
            }
        }

        return bestTarget;
    }
}