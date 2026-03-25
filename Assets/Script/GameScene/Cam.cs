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
    [SerializeField] private Transform playerCameraRoot;   // PlayerCameraRoot 넣기
    [SerializeField] private Vector3 anchorOffset = Vector3.zero;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 0.06f;
    [SerializeField] private float topClamp = 70f;
    [SerializeField] private float bottomClamp = -30f;
    [SerializeField] private bool invertY = false;

    private Transform _pivot;
    private float _yaw;
    private float _pitch;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority) return;

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

        _pivot = new GameObject($"CamPivot_{Object.Id}").transform;
        _pivot.position = playerCameraRoot.position + anchorOffset;

        _yaw = _pivot.eulerAngles.y;
        _pitch = 0f;

        freeLook.Follow = _pivot;
        freeLook.LookAt = _pivot;
        freeLook.Priority = 100;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void LateUpdate()
    {
        if (!Object || !Object.HasInputAuthority) return;
        if (_pivot == null || playerCameraRoot == null) return;

        _pivot.position = playerCameraRoot.position + anchorOffset;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        _yaw += delta.x * mouseSensitivity;
        float ySign = invertY ? 1f : -1f;
        _pitch += delta.y * mouseSensitivity * ySign;

        _pitch = ClampAngle(_pitch, bottomClamp, topClamp);

        _pivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
#endif
    }

    private void OnDestroy()
    {
        if (_pivot != null)
            Destroy(_pivot.gameObject);
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        while (angle < -360f) angle += 360f;
        while (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}
