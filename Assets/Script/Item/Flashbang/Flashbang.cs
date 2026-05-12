using Fusion;
using UnityEngine;

public class Flashbang : ThrowableProjectile
{
    [Header("Flashbang")]
    [SerializeField] private float fuseTime = 2f;
    [SerializeField] private float flashRadius = 8f;
    [SerializeField] private float flashFadeTime = 2.5f;
    [SerializeField] private float despawnDelay = 0.5f;
    [SerializeField, Range(0f, 180f)] private float flashViewAngle = 120f;

    [Header("Layer Names")]
    [SerializeField] private string playerLayerName = "Player";
    [SerializeField] private string[] obstacleLayerNames = { "Default", "Ground", "Wall" };

    [Header("Effect")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private GameObject explosionEffectPrefab;

    private int playerMask;
    private int obstacleMask;

    [Networked] private TickTimer fuseTimer { get; set; }
    [Networked] private TickTimer despawnTimer { get; set; }
    [Networked] private NetworkBool exploded { get; set; }

    private readonly Collider[] hitBuffer = new Collider[16];

    public override void Spawned()
    {
        CacheLayerMasks();
    }

    private void CacheLayerMasks()
    {
        playerMask = LayerMask.GetMask(playerLayerName);
        obstacleMask = LayerMask.GetMask(obstacleLayerNames);

        if (playerMask == 0)
            Debug.LogWarning($"Player layer mask is empty. Check layer name: {playerLayerName}");

        if (obstacleMask == 0)
            Debug.LogWarning("Obstacle layer mask is empty. Check obstacle layer names.");
    }

    public override void Init(PlayerRef ownerRef, float force, float upwardForce)
    {
        base.Init(ownerRef, force, upwardForce);

        if (!Object.HasStateAuthority)
            return;

        fuseTimer = TickTimer.CreateFromSeconds(Runner, fuseTime);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!exploded && fuseTimer.Expired(Runner))
            Explode();

        if (exploded && despawnTimer.Expired(Runner))
            Runner.Despawn(Object);
    }

    private void Explode()
    {
        exploded = true;

        Vector3 origin = transform.position;

        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            flashRadius,
            hitBuffer,
            playerMask,
            QueryTriggerInteraction.Ignore
        );

        Debug.Log($"[Flashbang] Explode / HitCount: {hitCount}, Origin: {origin}");

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitBuffer[i];
            if (col == null)
                continue;

            Debug.Log($"[Flashbang] Hit Collider: {col.name}, Layer: {LayerMask.LayerToName(col.gameObject.layer)}");

            NetworkObject targetObject = col.GetComponentInParent<NetworkObject>();
            if (targetObject == null)
            {
                Debug.Log("[Flashbang] Failed: NetworkObject not found");
                continue;
            }

            if (targetObject.InputAuthority == PlayerRef.None)
            {
                Debug.Log("[Flashbang] Failed: InputAuthority is None");
                continue;
            }

            Vector3 targetPos = targetObject.transform.position + Vector3.up * 1.5f;

            if (!HasLineOfSight(origin, targetPos))
            {
                Debug.Log("[Flashbang] Failed: Line of sight blocked");
                continue;
            }

            Debug.Log($"[Flashbang] RPC sent to: {targetObject.InputAuthority}");
            RPC_PlayFlash(targetObject.InputAuthority, origin, flashFadeTime);
        }

        StopPhysics();
        HideVisual();
        RPC_PlayExplosionEffect();

        despawnTimer = TickTimer.CreateFromSeconds(Runner, despawnDelay);
    }

    private bool HasLineOfSight(Vector3 origin, Vector3 target)
    {
        Vector3 dir = target - origin;

        if (dir.sqrMagnitude < 0.0001f)
            return true;

        bool blocked = Physics.Raycast(
            origin,
            dir.normalized,
            dir.magnitude,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        return !blocked;
    }

    private bool IsLookingAtFlashbang(NetworkObject targetObject, Vector3 flashOrigin)
    {
        Transform targetTr = targetObject.transform;

        // 눈높이 보정
        Vector3 eyePos = targetTr.position + Vector3.up * 1.5f;

        // 플레이어 → 섬광탄 방향
        Vector3 toFlash = flashOrigin - eyePos;

        // y축 영향 줄이고 싶으면 아래처럼 평면 기준으로 볼 수도 있음
        toFlash.y = 0f;

        if (toFlash.sqrMagnitude < 0.0001f)
            return true;

        toFlash.Normalize();

        // 플레이어가 바라보는 방향
        Vector3 forward = targetTr.forward;
        forward.y = 0f;
        forward.Normalize();

        // 시야각 절반을 코사인 값으로 변환
        float minDot = Mathf.Cos((flashViewAngle * 0.5f) * Mathf.Deg2Rad);

        // Dot이 minDot 이상이면 시야각 안에 있음
        float dot = Vector3.Dot(forward, toFlash);

        return dot >= minDot;
    }
    private bool IsLocalCameraLookingAtFlashbang(Vector3 flashPosition)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return false;

        Vector3 toFlash = flashPosition - cam.transform.position;

        if (toFlash.sqrMagnitude < 0.0001f)
            return true;

        toFlash.Normalize();

        float minDot = Mathf.Cos((flashViewAngle * 0.5f) * Mathf.Deg2Rad);
        float dot = Vector3.Dot(cam.transform.forward, toFlash);

        return dot >= minDot;
    }

    private void StopPhysics()
    {
        if (rb == null)
            TryGetComponent(out rb);

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();

        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    private void HideVisual()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayFlash(PlayerRef targetPlayer, Vector3 flashPosition, float fadeTime)
    {
        Debug.Log($"[Flashbang] RPC arrived / Local: {Runner.LocalPlayer}, Target: {targetPlayer}");

        if (Runner.LocalPlayer != targetPlayer)
            return;

        if (FlashbangScreenEffect.Local == null)
        {
            Debug.Log("[Flashbang] FlashbangScreenEffect.Local is null");
            return;
        }

        bool played = FlashbangScreenEffect.Local.TryPlayIfLookingAt(
            flashPosition,
            flashViewAngle,
            fadeTime
        );

        Debug.Log($"[Flashbang] TryPlayIfLookingAt result: {played}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayExplosionEffect()
    {
        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

        if (audioSource != null && explosionSound != null)
            audioSource.PlayOneShot(explosionSound);
    }
}