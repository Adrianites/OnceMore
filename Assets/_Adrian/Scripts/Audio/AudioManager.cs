using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public Slider masterVol;
    public Slider musicVol;
    public Slider ambienceVol;
    public Slider sfxVol;
    public AudioMixer am;


    public void Start()
    {
        if (PlayerPrefs.HasKey("MasterVol"))
        {
            masterVol.value = PlayerPrefs.GetFloat("MasterVol");
            am.SetFloat("MasterVol", masterVol.value);
        }
        else
        {
            masterVol.value = 0;
            am.SetFloat("MasterVol", 0);
        }

        if (PlayerPrefs.HasKey("MusicVol"))
        {
            musicVol.value = PlayerPrefs.GetFloat("MusicVol");
            am.SetFloat("MusicVol", musicVol.value);
        }
        else
        {
            musicVol.value = -25;
            am.SetFloat("MusicVol", -25);
        }

        if (PlayerPrefs.HasKey("AmbienceVol"))
        {
            ambienceVol.value = PlayerPrefs.GetFloat("AmbienceVol");
            am.SetFloat("AmbienceVol", ambienceVol.value);
        }
        else
        {
            ambienceVol.value = -25;
            am.SetFloat("AmbienceVol", -25);
        }
        
        if (PlayerPrefs.HasKey("SFXVol"))
        {
            sfxVol.value = PlayerPrefs.GetFloat("SFXVol");
            am.SetFloat("SFXVol", sfxVol.value);
        }
        else
        {
            sfxVol.value = -25;
            am.SetFloat("SFXVol", -25);
        }
    }

    #region Volume
    public void ChangeMasterVolume()
    {
        am.SetFloat("MasterVol", masterVol.value);
        PlayerPrefs.SetFloat("MasterVol", masterVol.value);
        PlayerPrefs.Save();
    }

    public void ChangeMusicVolume()
    {
        am.SetFloat("MusicVol", musicVol.value);
        PlayerPrefs.SetFloat("MusicVol", musicVol.value);
        PlayerPrefs.Save();
    }

    public void ChangeAmbienceVolume()
    {
        am.SetFloat("AmbienceVol", ambienceVol.value);
        PlayerPrefs.SetFloat("AmbienceVol", ambienceVol.value);
        PlayerPrefs.Save();
    }

    public void ChangeSFXVolume()
    {
        am.SetFloat("SFXVol", sfxVol.value);
        PlayerPrefs.SetFloat("SFXVol", sfxVol.value);
        PlayerPrefs.Save();
    }
    #endregion
}