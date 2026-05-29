using Fusion;
using Fusion.Addons.Physics;
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

    [Header("Step")]
    [SerializeField] private float maxStepHeight = 0.28f;          // 수정: 자동으로 올라갈 수 있는 턱 높이
    [SerializeField] private float stepCheckDistance = 0.35f;     // 수정: 발 앞 턱 검사 거리
    [SerializeField] private float stepSkin = 0.03f;              // 수정: 턱 위로 살짝 올리는 여유값
    [SerializeField] private float stepSphereRadius = 0.18f;      // 수정: 턱 검사 구 반지름
    [SerializeField] private float minStepNormalY = 0.65f;        // 수정: 너무 가파른 벽은 턱으로 보지 않음

    [Networked] private float animSpeed { get; set; }
    [Networked] private NetworkBool netGrounded { get; set; }
    [Networked] private float netVerticalVel { get; set; }
    [Networked] private NetworkButtons previousButtons { get; set; }

    private PlayerHealth health;
    private Rigidbody rb;
    private NetworkRigidbody3D nrb;
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
        nrb = GetComponent<NetworkRigidbody3D>();

        /* nrb를 불러오는 순간 동기화를 다 해주기 때문에 rb에선 필요 없음
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        */

        TryGetComponent(out health);
    }

    private void Start()
    {
        TryGetComponent(out animator);
        AssignAnimationIDs();
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
        animator.SetBool(animIDJump, !grounded && verticalVelocity > 4f);
        animator.SetBool(animIDFreeFall, !grounded && verticalVelocity < -1f);
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input))
            return;

        Vector3 moveWorld = input.direction;
        moveWorld.y = 0f;

        float inputMag = Mathf.Clamp01(moveWorld.magnitude);

        Vector3 desiredMoveDir = Vector3.zero;
        if (moveWorld.sqrMagnitude > 0.0001f)
            desiredMoveDir = moveWorld.normalized;

        bool groundProbe = IsGrounded(out RaycastHit groundHit);
        float groundDist = groundProbe ? groundHit.distance : float.PositiveInfinity;

        bool groundedContact = groundProbe && groundDist <= groundedContactDistance;
        bool groundedSnap = groundProbe && groundDist <= groundedSnapDistance;
        bool groundedAnim = groundProbe && groundDist <= groundedAnimDistance;
        bool groundedForPhysics = groundedContact || groundedSnap;

        bool canControl = health == null || health.CanControl;

        float targetSpeed = input.buttons.IsSet((int)InputButton.Sprint) ? runSpeed : walkSpeed;
        float moveSpeed = canControl ? targetSpeed * inputMag : 0f;

        // 로컬 표시값만 먼저 갱신
        if (Object.HasInputAuthority)
        {
            localAnimSpeed = moveSpeed;
            localGrounded = groundedAnim;
            localVerticalVel = rb.velocity.y;
        }

        // 로컬 회전 반응만 먼저 처리
        if (canControl && inputMag > rotateInputDeadzone && desiredMoveDir.sqrMagnitude > 0.0001f)
        {
            lastStableLookDir = desiredMoveDir;

            float targetYaw = Mathf.Atan2(lastStableLookDir.x, lastStableLookDir.z) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);

            Quaternion newRot = Quaternion.Slerp(
                rb.rotation,
                targetRot,
                rotateLerp * Runner.DeltaTime
            );

            rb.MoveRotation(newRot);
        }

        // 실제 Rigidbody 속도 제어는 State Authority만 실행
        if (!Object.HasStateAuthority)
            return;

        rb.angularVelocity = Vector3.zero;

        if (!canControl)
        {
            Vector3 stop = rb.velocity;
            stop.x = 0f;
            stop.z = 0f;
            rb.velocity = stop;

            animSpeed = 0f;
            netGrounded = groundedAnim;
            netVerticalVel = rb.velocity.y;
            previousButtons = input.buttons;

            return;
        }

        Vector3 velocity = rb.velocity;
        Vector3 planar = desiredMoveDir * moveSpeed;

        velocity.x = planar.x;
        velocity.z = planar.z;

        bool jumpPressed = input.buttons.WasPressed(previousButtons, (int)InputButton.Jump);
        bool groundedForJump = groundedContact || groundedSnap || groundedAnim;
        bool wantsJump = jumpPressed && groundedForJump;

        bool steppedUp = false;

        // 점프하려는 순간에는 턱 보정 금지
        if (!wantsJump &&
            velocity.y <= 0.05f &&
            groundProbe &&
            groundedAnim &&
            inputMag > rotateInputDeadzone &&
            desiredMoveDir.sqrMagnitude > 0.0001f)
        {
            steppedUp = TryStepUp(desiredMoveDir, groundHit);

            if (steppedUp)
            {
                groundedContact = true;
                groundedSnap = true;
                groundedAnim = true;
                groundedForPhysics = true;

                if (velocity.y > 0f)
                    velocity.y = 0f;

                // 턱 보정 중에는 점프/낙하 애니메이션 방지
                if (Object.HasInputAuthority)
                {
                    localGrounded = true;
                    localVerticalVel = 0f;
                }
            }
        }

        if (wantsJump)
        {
            velocity.y = jumpForce;

            groundedContact = false;
            groundedSnap = false;
            groundedAnim = false;
            groundedForPhysics = false;

            // 점프 입력 직후 로컬 애니메이션도 바로 반응
            if (Object.HasInputAuthority)
            {
                localGrounded = false;
                localVerticalVel = velocity.y;
            }
        }
        else
        {
            if (!steppedUp && groundedForPhysics && velocity.y <= 0f && groundDist <= groundedSnapDistance)
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

        animSpeed = moveSpeed;
        netGrounded = groundedAnim;

        // 점프한 틱에는 steppedUp보다 점프 속도를 우선 적용
        if (wantsJump)
            netVerticalVel = velocity.y;
        else if (steppedUp)
            netVerticalVel = 0f;
        else
            netVerticalVel = velocity.y;

        previousButtons = input.buttons;
    }

    private bool TryStepUp(Vector3 moveDir, RaycastHit currentGroundHit)
    {
        // 수정: 턱 보정은 서버/호스트 권위에서만 처리
        if (!Object.HasStateAuthority)
            return false;

        if (moveDir.sqrMagnitude < 0.0001f)
            return false;

        var scene = Runner.GetPhysicsScene();

        float groundY = currentGroundHit.point.y;

        Vector3 lowerOrigin = rb.position;
        lowerOrigin.y = groundY + stepSphereRadius + 0.03f;

        Vector3 upperOrigin = rb.position;
        upperOrigin.y = groundY + maxStepHeight + stepSphereRadius + 0.08f;

        // 수정: 낮은 위치에서 앞이 막히면 작은 턱 후보
        bool hasLowObstacle = scene.SphereCast(
            lowerOrigin,
            stepSphereRadius,
            moveDir,
            out _,
            stepCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (!hasLowObstacle)
            return false;

        // 수정: 위쪽도 막혀 있으면 벽으로 보고 올라가지 않음
        bool upperBlocked = scene.SphereCast(
            upperOrigin,
            stepSphereRadius,
            moveDir,
            out _,
            stepCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (upperBlocked)
            return false;

        Vector3 downOrigin =
            rb.position +
            moveDir * (stepCheckDistance + 0.05f) +
            Vector3.up * (maxStepHeight + 0.4f);

        // 수정: 턱 위에 실제로 밟을 수 있는 바닥이 있는지 확인
        bool foundStepTop = scene.Raycast(
            downOrigin,
            Vector3.down,
            out RaycastHit stepHit,
            maxStepHeight + 0.8f,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (!foundStepTop)
            return false;

        if (stepHit.normal.y < minStepNormalY)
            return false;

        float stepHeight = stepHit.point.y - groundY;

        if (stepHeight <= 0f)
            return false;

        if (stepHeight > maxStepHeight)
            return false;

        Vector3 pos = rb.position;
        pos.y += stepHeight + stepSkin;

        rb.MovePosition(pos);
        return true;
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