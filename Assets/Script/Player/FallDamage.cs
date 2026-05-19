using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallDamage : NetworkBehaviour
{
    [Header("Ground")]
    [SerializeField] private LayerMask groundMask;

    [Header("Fall")]
    [SerializeField] private float safeHeight = 3f;
    [SerializeField] private float lethalHeight = 12f;
    [SerializeField] private int minDamage = 10;
    [SerializeField] private int maxDamage = 100;

    private Rigidbody rb;
    private PlayerHealth health;

    private bool wasGrounded;
    private float fallStartY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        TryGetComponent(out health);
    }

    public override void Spawned()
    {
        wasGrounded = IsGrounded(out _);
        fallStartY = rb.position.y;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (health == null || health.IsOut)
            return;

        bool grounded = IsGrounded(out _);

        if (wasGrounded && !grounded)
        {
            fallStartY = rb.position.y;
        }
        else if (!wasGrounded && grounded)
        {
            float fallDistance = fallStartY - rb.position.y;
            ApplyFallDamage(fallDistance);
        }

        wasGrounded = grounded;
    }

    private void ApplyFallDamage(float fallDistance)
    {
        if (fallDistance <= safeHeight)
            return;

        if (fallDistance >= lethalHeight)
        {
            health.Death();
            return;
        }

        float t = Mathf.InverseLerp(safeHeight, lethalHeight, fallDistance);
        int damage = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, t));
        health.ApplyDamage(damage);
    }

    private bool IsGrounded(out RaycastHit hit)
    {
        float radius = 0.28f;
        float castDistance = 0.65f;
        Vector3 origin = rb.position + Vector3.up * 0.35f;

        var scene = Runner.GetPhysicsScene();
        return scene.SphereCast(
            origin,
            radius,
            Vector3.down,
            out hit,
            castDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }
    public void ResetRound()
    {
        if (!Object.HasStateAuthority)
            return;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        wasGrounded = IsGrounded(out _);
        fallStartY = rb.position.y;
    }
}