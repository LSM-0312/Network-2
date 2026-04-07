using Fusion;
using UnityEngine;

public class MeleeAttack : NetworkBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackRadius = 1.2f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackHeight = 0.9f;
    [SerializeField] private float attackCooldown = 0.45f;
    [SerializeField] private float attackHalfAngle = 50f;

    [Header("Damage")]
    [SerializeField] private int minDamage = 25;
    [SerializeField] private int maxDamage = 35;

    [Header("Target")]
    [SerializeField] private LayerMask playerMask;

    [Networked] private TickTimer cooldownTimer { get; set; }
    [Networked] private NetworkButtons previousButtons { get; set; }

    private readonly Collider[] hitBuffer = new Collider[16];

    private PlayerAvatar avatar;
    private PlayerHealth health;

    private void Awake()
    {
        TryGetComponent(out avatar);
        TryGetComponent(out health);
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input))
            return;

        bool attackPressed = input.buttons.WasPressed(previousButtons, (int)InputButton.Mouse0);
        previousButtons = input.buttons;

        if (!Object.HasStateAuthority)
            return;

        if (!attackPressed)
            return;

        if (health != null && !health.CanControl)
            return;

        if (avatar == null || avatar.Role != PlayerRole.Cop)
            return;

        if (!cooldownTimer.ExpiredOrNotRunning(Runner))
            return;

        TryAttack();
        cooldownTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
    }

    private void TryAttack()
    {
        Vector3 origin = transform.position + Vector3.up * attackHeight + transform.forward * (attackRange * 0.5f);

        int hitCount = Runner.GetPhysicsScene().OverlapSphere(
            origin,
            attackRadius,
            hitBuffer,
            playerMask,
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

            PlayerHealth target = col.GetComponentInParent<PlayerHealth>();
            if (target == null)
                continue;

            if (target == health)
                continue;

            if (target.IsOut)
                continue;

            PlayerAvatar targetAvatar = target.GetComponent<PlayerAvatar>();
            if (targetAvatar == null)
                continue;

            if (targetAvatar.Role != PlayerRole.Robber)
                continue;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;

            float sqr = toTarget.sqrMagnitude;
            if (sqr > attackRange * attackRange)
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
                bestTarget = target;
            }
        }

        if (bestTarget == null)
            return;

        int damage = Random.Range(minDamage, maxDamage + 1);
        bestTarget.ApplyDamage(damage);
    }
}