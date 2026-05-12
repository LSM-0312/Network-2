using System.Collections;
using UnityEngine;

public class FlashbangScreenEffect : MonoBehaviour
{
    public static FlashbangScreenEffect Local { get; private set; }

    [SerializeField] private CanvasGroup whiteGroup;
    [SerializeField] private Camera localCamera;
    [SerializeField] private float holdTime = 0.15f;

    private Coroutine flashRoutine;

    private void Awake()
    {
        Local = this;

        if (whiteGroup == null)
            TryGetComponent(out whiteGroup);

        if (whiteGroup != null)
        {
            whiteGroup.alpha = 0f;
            whiteGroup.blocksRaycasts = false;
            whiteGroup.interactable = false;
        }
    }

    private void OnDestroy()
    {
        if (Local == this)
            Local = null;
    }

    public bool TryPlayIfLookingAt(Vector3 flashPosition, float viewAngle, float fadeTime)
    {
        if (localCamera == null)
        {
            Debug.Log("[FlashbangScreenEffect] Local camera is null");
            return false;
        }

        Vector3 toFlash = flashPosition - localCamera.transform.position;

        if (toFlash.sqrMagnitude < 0.0001f)
        {
            Play(fadeTime);
            return true;
        }

        toFlash.Normalize();

        float minDot = Mathf.Cos((viewAngle * 0.5f) * Mathf.Deg2Rad);
        float dot = Vector3.Dot(localCamera.transform.forward, toFlash);

        Debug.Log($"[FlashbangScreenEffect] Camera: {localCamera.name}, Dot: {dot}, MinDot: {minDot}");

        if (dot < minDot)
            return false;

        Play(fadeTime);
        return true;
    }

    public void Play(float fadeTime)
    {
        if (whiteGroup == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(fadeTime));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("[FlashbangScreenEffect] F test pressed");
            Play(2.5f);
        }
    }

    private IEnumerator FlashRoutine(float fadeTime)
    {
        whiteGroup.alpha = 1f;

        yield return new WaitForSeconds(holdTime);

        float time = 0f;

        while (time < fadeTime)
        {
            time += Time.deltaTime;
            whiteGroup.alpha = Mathf.Lerp(1f, 0f, time / fadeTime);
            yield return null;
        }

        whiteGroup.alpha = 0f;
        flashRoutine = null;
    }
}