using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverSceneManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text resultText;

    [Header("Background Images")]
    [SerializeField] private Sprite player1WinSprite;
    [SerializeField] private Sprite player2WinSprite;
    [SerializeField] private Sprite drawSprite;

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Game Over Music")]
    [SerializeField] private bool playMusicOnGameOver = true;

    private void Start()
    {
        SetupScreen();
        PlayGameOverMusic();
    }

    private void SetupScreen()
    {
        switch (GameManager.LastResult)
        {
            case GameResult.Player1Win:
                backgroundImage.sprite = player1WinSprite;
                resultText.text = "Player 1 venceu!";
                break;

            case GameResult.Player2Win:
                backgroundImage.sprite = player2WinSprite;
                resultText.text = "Player 2 venceu!";
                break;

            case GameResult.Draw:
                backgroundImage.sprite = drawSprite;
                resultText.text = "Empate!";
                break;
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void PlayGameOverMusic()
    {
        if (!playMusicOnGameOver || AudioManager.Instance == null)
            return;

        AudioClip clip = null;

        switch (GameManager.LastResult)
        {
            case GameResult.Player1Win:
            case GameResult.Player2Win:
                clip = AudioManager.Instance.bgmGameOverWin;
                break;

            case GameResult.Draw:
                clip = AudioManager.Instance.bgmGameOverDraw;
                break;
        }

        if (clip != null)
        {
            AudioManager.Instance.PlayBGM(clip);
        }
    }
}
