using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    private void Start()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.bgmMainMenu != null)
        {
            AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmMainMenu);
        }
    }

    [SerializeField] private string gameSceneName = "Game"; 

    public void PlayGame()
    {
        AudioManager.Instance?.StopBGM();
        Time.timeScale = 1f; 
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
