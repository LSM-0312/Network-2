using Fusion;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class CatchDetector : NetworkBehaviour
{
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private float catchCooldownSeconds = 3f;

    private CapsuleCollider _capsule;
    private readonly Collider[] _overlapResults = new Collider[16];

    private void Awake()
    {
        _capsule = GetComponent<CapsuleCollider>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        PlayerAvatar myAvatar = GetComponent<PlayerAvatar>();
        if (myAvatar == null || myAvatar.Role != PlayerRole.Cop)
            return;

        int hitCount = OverlapOwnCapsule(_overlapResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _overlapResults[i];
            if (hit == null)
                continue;

            if (hit.transform == transform)
                continue;

            PlayerAvatar otherAvatar = hit.GetComponentInParent<PlayerAvatar>();
            if (otherAvatar == null)
                continue;

            if (otherAvatar == myAvatar)
                continue;

            if (otherAvatar.Role != PlayerRole.Robber)
                continue;

            if (otherAvatar.IsCatchInvulnerable())
                continue;

            otherAvatar.StartCatchInvulnerability(catchCooldownSeconds);
            Debug.Log("Robber Caught!");
            break;
        }
    }

    private int OverlapOwnCapsule(Collider[] results)
    {
        GetCapsuleWorldPoints(out Vector3 point0, out Vector3 point1, out float radius);

        var scene = Runner.GetPhysicsScene();
        return scene.OverlapCapsule(
            point0,
            point1,
            radius,
            results,
            playerMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void GetCapsuleWorldPoints(out Vector3 point0, out Vector3 point1, out float radius)
    {
        Vector3 center = transform.TransformPoint(_capsule.center);

        float scaleX = Mathf.Abs(transform.lossyScale.x);
        float scaleY = Mathf.Abs(transform.lossyScale.y);
        float scaleZ = Mathf.Abs(transform.lossyScale.z);

        radius = _capsule.radius * Mathf.Max(scaleX, scaleZ);
        float height = Mathf.Max(_capsule.height * scaleY, radius * 2f);

        float cylinderHalf = Mathf.Max(0f, (height * 0.5f) - radius);

        point0 = center + Vector3.up * cylinderHalf;
        point1 = center - Vector3.up * cylinderHalf;
    }
}