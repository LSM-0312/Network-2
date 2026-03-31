using Fusion;
using UnityEngine;

public class PlayerAvatar : NetworkBehaviour
{
    [Header("Visual")]
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color copColor = Color.blue;
    [SerializeField] private Color robberColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Catch")]
    [SerializeField] private float catchInvulnerableDuration = 3f;

    [Networked, OnChangedRender(nameof(OnRoleChanged))]
    public PlayerRole Role { get; set; }

    [Networked] private TickTimer CatchInvulnTimer { get; set; }

    private PlayerRole initialRole = PlayerRole.None;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);

        mpb = new MaterialPropertyBlock();
    }

    public void SetInitialRole(PlayerRole role)
    {
        initialRole = role;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            Role = initialRole;

        ApplyRoleVisual();
    }

    private void OnRoleChanged()
    {
        ApplyRoleVisual();
    }

    public bool IsCatchInvulnerable()
    {
        return !CatchInvulnTimer.ExpiredOrNotRunning(Runner);
    }

    public void StartCatchInvulnerability()
    {
        if (!Object.HasStateAuthority)
            return;

        CatchInvulnTimer = TickTimer.CreateFromSeconds(Runner, catchInvulnerableDuration);
    }

    public void StartCatchInvulnerability(float duration)
    {
        if (!Object.HasStateAuthority)
            return;

        CatchInvulnTimer = TickTimer.CreateFromSeconds(Runner, duration);
    }

    private void ApplyRoleVisual()
    {
        Color color = defaultColor;

        switch (Role)
        {
            case PlayerRole.Cop:
                color = copColor;
                break;

            case PlayerRole.Robber:
                color = robberColor;
                break;
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null)
                continue;

            targetRenderers[i].GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);
            targetRenderers[i].SetPropertyBlock(mpb);
        }
    }
}