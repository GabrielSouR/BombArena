using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;   
using System.Collections;
using System.Collections.Generic;

public class PauseManager : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [SerializeField] private GameObject hudTimer;
    [SerializeField] private Transform PausePanel;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();

        inputActions.UI.Pause.performed += ctx => TogglePause();
    }

    private void OnEnable()
    {
        inputActions.UI.Enable();
    }

    private void OnDisable()
    {
        inputActions.UI.Disable();
    }

    private void TogglePause()
    {
        if (GameIsPaused)
            Resume();
        else
            Pause();
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);

        if (hudTimer != null)
            hudTimer.SetActive(true);

        AudioManager.Instance?.PlayPauseOn();
        Time.timeScale = 1f;
        GameIsPaused = false;

        AudioManager.Instance?.ResumeMusic();
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);

        PausePanel.transform.localScale = Vector3.one * 0.9f;
        StartCoroutine(PopupScale(PausePanel.transform));

        if (hudTimer != null)
            hudTimer.SetActive(false); 
        
        AudioManager.Instance?.PlayPauseOn();
        Time.timeScale = 0f;
        GameIsPaused = true;

        AudioManager.Instance?.PauseMusic();
    }

    private IEnumerator PopupScale(Transform t)
    {
        float duration = 0.15f;
        float time = 0f;
        Vector3 start = t.localScale;
        Vector3 end = Vector3.one;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime; 
            float k = time / duration;
            k = Mathf.SmoothStep(0, 1, k);
            t.localScale = Vector3.Lerp(start, end, k);
            yield return null;
        }

        t.localScale = end;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        GameIsPaused = false;

        AudioManager.Instance?.StopMusic();  

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        GameIsPaused = false;

        AudioManager.Instance?.StopMusic();  

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
