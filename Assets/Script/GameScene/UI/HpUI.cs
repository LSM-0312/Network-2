using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HpUI : MonoBehaviour
{
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider hpSlider;

    public void SetHp(int currentHp, int maxHp, bool isDead)
    {
        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }

        if (hpText != null)
        {
            if (isDead)
                hpText.text = "DEAD";
            else
                hpText.text = $"{currentHp} / {maxHp}";
        }
    }

    public void Clear()
    {
        if (hpText != null)
            hpText.text = "";

        if (hpSlider != null)
            hpSlider.value = 0f;
    }
}