using Fusion;
using UnityEngine;

public class SmokeGrenade : ThrowableProjectile
{
    [Header("Smoke")]
    [SerializeField] private float fuseTime = 3f;
    [SerializeField] private float smokeEmitDuration = 10f;
    [SerializeField] private float smokeFadeDelay = 4f;

    [Header("View")]
    [SerializeField] private Transform throwModelRoot;

    [Header("Effect")]
    [SerializeField] private ParticleSystem smokeBurstParticle;
    [SerializeField] private ParticleSystem smokeLoopParticle;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip smokeStartSound;

    [Networked] private TickTimer fuseTimer { get; set; }
    [Networked] private TickTimer stopEmitTimer { get; set; }
    [Networked] private TickTimer despawnTimer { get; set; }
    [Networked] private NetworkBool smoked { get; set; }
    [Networked] private NetworkBool smokeEmitStopped { get; set; }

    private bool smokeViewPlayed;
    private bool smokeViewStopped;

    public override void Spawned()
    {
        smokeViewPlayed = false;
        smokeViewStopped = false;

        if (smokeBurstParticle != null)
            smokeBurstParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (smokeLoopParticle != null)
            smokeLoopParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

        if (!smoked && fuseTimer.Expired(Runner))
            StartSmoke();

        if (smoked && !smokeEmitStopped && stopEmitTimer.Expired(Runner))
            StopSmokeEmission();

        if (smokeEmitStopped && despawnTimer.Expired(Runner))
            Runner.Despawn(Object);
    }

    public override void Render()
    {
        if (smoked)
            PlaySmokeView();

        if (smokeEmitStopped)
            StopSmokeView();
    }

    private void StartSmoke()
    {
        smoked = true;

        StopPhysics();
        PlaySmokeView();

        stopEmitTimer = TickTimer.CreateFromSeconds(Runner, smokeEmitDuration);
    }

    private void StopSmokeEmission()
    {
        smokeEmitStopped = true;

        StopSmokeView();

        despawnTimer = TickTimer.CreateFromSeconds(Runner, smokeFadeDelay);
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

    private void PlaySmokeView()
    {
        if (smokeViewPlayed)
            return;

        smokeViewPlayed = true;

        HideThrowModel();

        if (smokeBurstParticle != null)
            smokeBurstParticle.Play(true);

        if (smokeLoopParticle != null)
            smokeLoopParticle.Play(true);

        if (audioSource != null && smokeStartSound != null)
            audioSource.PlayOneShot(smokeStartSound);
    }

    private void StopSmokeView()
    {
        if (smokeViewStopped)
            return;

        smokeViewStopped = true;

        if (smokeLoopParticle != null)
            smokeLoopParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void HideThrowModel()
    {
        if (throwModelRoot == null)
            return;

        Renderer[] renderers = throwModelRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;
    }
}