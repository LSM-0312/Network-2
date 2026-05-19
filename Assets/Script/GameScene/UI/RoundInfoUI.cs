using TMPro;
using UnityEngine;

public class RoundInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text aliveText;

    public void SetRound(GameStateManager manager)
    {
        if (roundText != null)
            roundText.text = $"ROUND {manager.RoundNumber}";

        if (scoreText != null)
            scoreText.text = $"COP {manager.CopScore} : {manager.RobberScore} ROBBER";

        if (aliveText != null)
            aliveText.text = $"ALIVE  COP {manager.AliveCopCount} / ROBBER {manager.AliveRobberCount}";

        if (timerText != null)
        {
            if (manager.State == GameState.Playing)
            {
                int remain = Mathf.CeilToInt(manager.RemainingRoundTime);
                int min = remain / 60;
                int sec = remain % 60;

                timerText.text = $"{min:00}:{sec:00}";
            }
            else
            {
                timerText.text = "";
            }
        }
    }

    public void Clear()
    {
        if (roundText != null)
            roundText.text = "";

        if (timerText != null)
            timerText.text = "";

        if (scoreText != null)
            scoreText.text = "";

        if (aliveText != null)
            aliveText.text = "";
    }
}