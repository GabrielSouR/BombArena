using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("BGM Clips")]
    public AudioClip bgmNormal;
    public AudioClip bgmSuddenDeath;
    public AudioClip suddenDeathSiren;
    public AudioClip bgmMainMenu;
    public AudioClip bgmGameOverWin;
    public AudioClip bgmGameOverDraw;

    [Header("SFX Clips")]
    public AudioClip bombPlace; 
    public AudioClip bombExplosion;
    public AudioClip playerDeath;
    public AudioClip itemPickup;
    public AudioClip blockFall;
    public AudioClip gameStart;
    public AudioClip pause;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ======================
    // BGM
    // ======================
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.time = 0f;      
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    // ======================
    // SFX
    // ======================
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // atalhos
    public void PlayBombPlace()       => PlaySFX(bombPlace);
    public void PlayExplosion()       => PlaySFX(bombExplosion);
    public void PlayPlayerDeath()     => PlaySFX(playerDeath);
    public void PlayItemPickup()      => PlaySFX(itemPickup);
    public void PlayBlockFall()       => PlaySFX(blockFall);

    public void PlaySiren()
    {
        sfxSource.PlayOneShot(suddenDeathSiren, 0.3f);
    }

    // ======================
    // Controle de música p/ Pause / Menu
    // ======================
    public void PauseMusic()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Pause();
    }

    public void ResumeMusic()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
            bgmSource.UnPause();
    }

    public void StopMusic()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void PlayGameStart()
    {
        PlaySFX(gameStart);
    }

    public void PlayPauseOn()
    {
        PlaySFX(pause);
    }

}
