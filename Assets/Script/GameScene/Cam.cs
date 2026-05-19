using Fusion;
using UnityEngine;
using Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Cam : NetworkBehaviour
{
    [Header("Scene FreeLook")]
    [SerializeField] private CinemachineFreeLook freeLook;
    [SerializeField] private UnityEngine.Behaviour inputProvider;

    [Header("Player Anchor")]
    [SerializeField] private Transform playerCameraRoot;
    [SerializeField] private Vector3 anchorOffset = Vector3.zero;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 0.06f;
    [SerializeField] private float topClamp = 70f;
    [SerializeField] private float bottomClamp = -30f;
    [SerializeField] private bool invertY = false;

    private Transform pivot;
    private float yaw;
    private float pitch;
    private bool isLocalCameraOwner;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;

        isLocalCameraOwner = true;

        if (freeLook == null)
            freeLook = UnityEngine.Object.FindFirstObjectByType<CinemachineFreeLook>();

        if (freeLook == null)
        {
            Debug.LogError("씬에서 CinemachineFreeLook를 찾지 못했습니다.");
            return;
        }

        if (playerCameraRoot == null)
        {
            Debug.LogError("playerCameraRoot가 비어있습니다.");
            return;
        }

        if (inputProvider != null)
            inputProvider.enabled = false;

        freeLook.m_XAxis.m_InputAxisName = string.Empty;
        freeLook.m_YAxis.m_InputAxisName = string.Empty;

        pivot = new GameObject($"CamPivot_{Object.Id}").transform;
        pivot.position = playerCameraRoot.position + anchorOffset;

        yaw = pivot.eulerAngles.y;
        pitch = 0f;

        freeLook.Follow = pivot;
        freeLook.LookAt = pivot;
        freeLook.Priority = 100;

        LockCursor();
    }

    private void LateUpdate()
    {
        if (!Object || !Object.HasInputAuthority)
            return;

        if (pivot == null || playerCameraRoot == null)
            return;

        pivot.position = playerCameraRoot.position + anchorOffset;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        yaw += delta.x * mouseSensitivity;

        float ySign = invertY ? 1f : -1f;
        pitch += delta.y * mouseSensitivity * ySign;

        pitch = ClampAngle(pitch, bottomClamp, topClamp);

        pivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
#else
        yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity * 10f;

        float ySign = invertY ? 1f : -1f;
        pitch += Input.GetAxisRaw("Mouse Y") * mouseSensitivity * 10f * ySign;

        pitch = ClampAngle(pitch, bottomClamp, topClamp);

        pivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
#endif
    }

    private void OnDisable()
    {
        CleanupLocalCamera();
    }

    private void OnDestroy()
    {
        CleanupLocalCamera();
    }

    private void CleanupLocalCamera()
    {
        if (isLocalCameraOwner)
            UnlockCursor();

        if (pivot != null)
        {
            Destroy(pivot.gameObject);
            pivot = null;
        }

        isLocalCameraOwner = false;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        while (angle < -360f)
            angle += 360f;

        while (angle > 360f)
            angle -= 360f;

        return Mathf.Clamp(angle, min, max);
    }
}