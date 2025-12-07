using UnityEngine;
using TMPro;

public class SuddenDeathTimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    void Update()
    {
        if (GameManager.Instance == null || timerText == null)
            return;

        // se a partida acabou, trava em 00:00
        if (GameManager.Instance.MatchOver)
        {
            timerText.text = "00:00";
            return;
        }

        float t = GameManager.Instance.CurrentTimer;

        if (GameManager.Instance.SuddenDeathStarted)
            t = 0f;

        int totalSeconds = Mathf.CeilToInt(t);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
