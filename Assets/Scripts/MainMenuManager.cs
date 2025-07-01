using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public AudioSource bgmAudioSource;

    void Start()
    {
        float volume = PlayerPrefs.GetFloat("Volume", 1f);
        volumeSlider.value = volume;
        AudioListener.volume = volume;
        if (bgmAudioSource != null) bgmAudioSource.volume = volume;
    }

    public void StartNewGame()
    {
        string savePath = System.IO.Path.Combine(Application.persistentDataPath, "savegame.json");
        if (System.IO.File.Exists(savePath))
            System.IO.File.Delete(savePath); // Delete saved file

        SceneManager.LoadScene("GameScene");
    }


    public void ContinueGame()
    {
        string savePath = System.IO.Path.Combine(Application.persistentDataPath, "savegame.json");
        if (System.IO.File.Exists(savePath))
        {
            SceneManager.LoadScene("GameScene"); 
        }
        else
        {
            Debug.Log("无存档文件可加载！");
        }
    }


    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void OnVolumeChanged(float value)
    {
        value=volumeSlider.value;
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
        PlayerPrefs.Save();
        if (bgmAudioSource != null) bgmAudioSource.volume = value;
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit called (will not quit in editor)");
    }
}
