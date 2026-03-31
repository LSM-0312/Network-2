using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : NetworkBehaviour
{
    [Header("Move")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private LayerMask groundMask;

    [Header("Rotation")]
    [SerializeField] private float rotateLerp = 16f;
    [SerializeField] private float rotateInputDeadzone = 0.12f;

    [Header("Ground")]
    [SerializeField] private float groundedContactDistance = 0.06f;
    [SerializeField] private float groundedSnapDistance = 0.06f;
    [SerializeField] private float groundedAnimDistance = 0.18f;
    [SerializeField] private float groundedVelClamp = 0.5f;

    [Networked] private float animSpeed { get; set; }
    [Networked] private NetworkBool netGrounded { get; set; }
    [Networked] private float netVerticalVel { get; set; }
    [Networked] private NetworkButtons previousButtons { get; set; }

    private Rigidbody rb;
    private Animator animator;

    private Vector3 lastStableLookDir = Vector3.forward;

    private float localAnimSpeed;
    private bool localGrounded;
    private float localVerticalVel;

    private int animIDSpeed;
    private int animIDGrounded;
    private int animIDJump;
    private int animIDFreeFall;
    private int animIDMotionSpeed;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Start()
    {
        TryGetComponent(out animator);
        AssignAnimationIDs();
    }

    public override void Spawned()
    {
        if (rb != null)
            rb.interpolation = RigidbodyInterpolation.None;
    }

    private void AssignAnimationIDs()
    {
        animIDSpeed = Animator.StringToHash("Speed");
        animIDGrounded = Animator.StringToHash("Grounded");
        animIDJump = Animator.StringToHash("Jump");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    public override void Render()
    {
        if (animator == null || rb == null)
            return;

        float speed = Object.HasInputAuthority ? localAnimSpeed : animSpeed;
        bool grounded = Object.HasInputAuthority ? localGrounded : netGrounded;
        float verticalVelocity = Object.HasInputAuthority ? localVerticalVel : netVerticalVel;

        animator.SetFloat(animIDSpeed, speed, 0.1f, Time.deltaTime);
        animator.SetFloat(animIDMotionSpeed, speed > 0.1f ? 1f : 0f);
        animator.SetBool(animIDGrounded, grounded);
        animator.SetBool(animIDJump, !grounded && verticalVelocity > 0.1f);
        animator.SetBool(animIDFreeFall, !grounded && verticalVelocity < -0.1f);
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input))
            return;

        Vector3 moveWorld = input.direction;
        float inputMag = Mathf.Clamp01(moveWorld.magnitude);

        Vector3 desiredMoveDir = Vector3.zero;
        if (moveWorld.sqrMagnitude > 0.0001f)
            desiredMoveDir = moveWorld.normalized;

        bool groundProbe = IsGrounded(out RaycastHit groundHit);
        float groundDist = groundProbe ? groundHit.distance : float.PositiveInfinity;

        bool groundedContact = groundProbe && groundDist <= groundedContactDistance;
        bool groundedAnim = groundProbe && groundDist <= groundedAnimDistance;

        rb.angularVelocity = Vector3.zero;

        float targetSpeed = input.buttons.IsSet((int)InputButton.Sprint) ? runSpeed : walkSpeed;
        float moveSpeed = targetSpeed * inputMag;

        Vector3 velocity = rb.velocity;
        Vector3 planar = desiredMoveDir * moveSpeed;

        velocity.x = planar.x;
        velocity.z = planar.z;

        if (inputMag > rotateInputDeadzone && desiredMoveDir.sqrMagnitude > 0.0001f)
            lastStableLookDir = desiredMoveDir;

        if (lastStableLookDir.sqrMagnitude > 0.0001f)
        {
            float targetYaw = Mathf.Atan2(lastStableLookDir.x, lastStableLookDir.z) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);

            Quaternion newRot = Quaternion.Slerp(
                rb.rotation,
                targetRot,
                rotateLerp * Runner.DeltaTime
            );

            rb.MoveRotation(newRot);
        }

        bool jumpPressed = input.buttons.WasPressed(previousButtons, (int)InputButton.Jump);

        if (jumpPressed && groundProbe)
        {
            velocity.y = jumpForce;
        }
        else
        {
            if (groundProbe && velocity.y <= 0f && groundDist <= groundedSnapDistance)
            {
                Vector3 pos = rb.position;
                pos.y -= groundDist;
                rb.MovePosition(pos);

                groundedContact = true;
                groundedAnim = true;
            }

            if (groundedContact && velocity.y < -groundedVelClamp)
                velocity.y = -groundedVelClamp;
        }

        rb.velocity = velocity;

        if (Object.HasInputAuthority)
        {
            localAnimSpeed = moveSpeed;
            localGrounded = groundedAnim;
            localVerticalVel = velocity.y;
        }

        if (Object.HasStateAuthority)
        {
            animSpeed = moveSpeed;
            netGrounded = groundedAnim;
            netVerticalVel = velocity.y;
        }

        previousButtons = input.buttons;
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

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips != null && FootstepAudioClips.Length > 0)
            {
                int index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (LandingAudioClip != null)
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, FootstepAudioVolume);
        }
    }
}