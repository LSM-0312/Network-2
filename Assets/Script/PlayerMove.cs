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

    [Networked] private float _animSpeed { get; set; }
    [Networked] private NetworkBool _netGrounded { get; set; }
    [Networked] private float _netVerticalVel { get; set; }

    private Rigidbody _rb;
    private Animator _animator;

    private Vector3 _lastStableLookDir = Vector3.forward;

    private float _localAnimSpeed;
    private bool _localGrounded;
    private float _localVerticalVel;

    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.interpolation = RigidbodyInterpolation.None;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Start()
    {
        TryGetComponent(out _animator);
        AssignAnimationIDs();
    }

    public override void Spawned()
    {
        if (_rb != null)
            _rb.interpolation = RigidbodyInterpolation.None;
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    public override void Render()
    {
        if (_animator == null || _rb == null) return;

        float speed = Object.HasInputAuthority ? _localAnimSpeed : _animSpeed;
        bool grounded = Object.HasInputAuthority ? _localGrounded : _netGrounded;
        float verticalVelocity = Object.HasInputAuthority ? _localVerticalVel : _netVerticalVel;

        _animator.SetFloat(_animIDSpeed, speed, 0.1f, Time.deltaTime);
        _animator.SetFloat(_animIDMotionSpeed, speed > 0.1f ? 1f : 0f);
        _animator.SetBool(_animIDGrounded, grounded);
        _animator.SetBool(_animIDJump, !grounded && verticalVelocity > 0.1f);
        _animator.SetBool(_animIDFreeFall, !grounded && verticalVelocity < -0.1f);
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

        if (Object.HasInputAuthority)
        {
            float localTargetSpeed = input.sprint.IsSet(0) ? runSpeed : walkSpeed;
            _localAnimSpeed = localTargetSpeed * inputMag;
            _localGrounded = groundedAnim;
            _localVerticalVel = _rb.velocity.y;
        }

        if (!Object.HasStateAuthority)
            return;

        _rb.angularVelocity = Vector3.zero;

        float targetSpeed = input.sprint.IsSet(0) ? runSpeed : walkSpeed;
        float moveSpeed = targetSpeed * inputMag;

        _animSpeed = moveSpeed;

        Vector3 v = _rb.velocity;
        Vector3 playerPlanar = desiredMoveDir * moveSpeed;

        v.x = playerPlanar.x;
        v.z = playerPlanar.z;

        if (inputMag > rotateInputDeadzone && desiredMoveDir.sqrMagnitude > 0.0001f)
        {
            _lastStableLookDir = desiredMoveDir;
        }

        if (_lastStableLookDir.sqrMagnitude > 0.0001f)
        {
            float targetYaw = Mathf.Atan2(_lastStableLookDir.x, _lastStableLookDir.z) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);

            Quaternion newRot = Quaternion.Slerp(
                _rb.rotation,
                targetRot,
                rotateLerp * Runner.DeltaTime
            );

            _rb.MoveRotation(newRot);
        }

        bool jumpPressed = input.jump.IsSet(0);

        if (jumpPressed && groundProbe)
        {
            v.y = jumpForce;
        }
        else
        {
            if (groundProbe && v.y <= 0f && groundDist <= groundedSnapDistance)
            {
                Vector3 p = _rb.position;
                p.y -= groundDist;
                _rb.MovePosition(p);

                groundedContact = true;
                groundedAnim = true;
            }

            if (groundedContact)
            {
                if (v.y < -groundedVelClamp)
                    v.y = -groundedVelClamp;
            }
        }

        _rb.velocity = v;

        _netGrounded = groundedAnim;
        _netVerticalVel = v.y;
    }

    private bool IsGrounded(out RaycastHit hit)
    {
        float radius = 0.28f;
        float castDistance = 0.65f;
        Vector3 origin = _rb.position + Vector3.up * 0.35f;

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