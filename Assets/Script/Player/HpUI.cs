using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HpUI : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;

    private NetworkRunner runner;
    private PlayerHealth localHealth;

    private void Update()
    {
        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();

        if (runner == null || !runner.IsRunning)
        {
            ClearUI();
            return;
        }

        if (localHealth == null)
        {
            if (runner.TryGetPlayerObject(runner.LocalPlayer, out NetworkObject playerObj))
                localHealth = playerObj.GetComponent<PlayerHealth>();
        }

        if (localHealth == null)
        {
            ClearUI();
            return;
        }

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = localHealth.MaxHp;
            hpSlider.value = localHealth.CurrentHp;
        }

        if (hpText != null)
        {
            if (localHealth.IsDead)
                hpText.text = "DEAD";
            else
                hpText.text = $"{localHealth.CurrentHp} / {localHealth.MaxHp}";
        }
    }

    private void ClearUI()
    {
        if (hpText != null)
            hpText.text = "";

        if (hpSlider != null)
            hpSlider.value = 0f;
    }
}