using Fusion;
using UnityEngine;

public abstract class ThrowableProjectile : NetworkBehaviour
{
    [SerializeField] protected Rigidbody rb;

    protected PlayerRef owner;

    public void Init(PlayerRef ownerRef, float force, float upwardForce, Vector3 visualScale)
    {
        owner = ownerRef;

        transform.localScale = visualScale;

        if (rb == null)
            TryGetComponent(out rb);

        if (rb == null)
            return;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 dir = transform.forward * force + Vector3.up * upwardForce;
        rb.AddForce(dir, ForceMode.VelocityChange);
    }
}