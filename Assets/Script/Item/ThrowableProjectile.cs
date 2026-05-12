using Fusion;
using UnityEngine;

public abstract class ThrowableProjectile : NetworkBehaviour
{
    [SerializeField] protected Rigidbody rb;

    protected PlayerRef owner;

    public virtual void Init(PlayerRef ownerRef, float force, float upwardForce)
    {
        owner = ownerRef;

        if (!Object.HasStateAuthority)
            return;

        if (rb == null)
            TryGetComponent(out rb);

        if (rb == null)
            return;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 velocity = transform.forward * force + Vector3.up * upwardForce;
        rb.AddForce(velocity, ForceMode.VelocityChange);
    }
}