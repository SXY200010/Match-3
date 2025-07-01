using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseUI;
    public GameObject nameInputPanel;
    public TMP_InputField nameInputField;
    public Slider volumeSlider;
    public AudioSource bgmAudioSource;

    private void Start()
    {
        pauseUI.SetActive(false);
        nameInputPanel.SetActive(false);
        Time.timeScale = 1;

        float volume = PlayerPrefs.GetFloat("Volume", 1f);
        volumeSlider.value = volume;
        AudioListener.volume = volume;

        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = volume;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        bool isPaused = Time.timeScale == 0.001f;
        Time.timeScale = isPaused ? 1 : 0.001f;
        pauseUI.SetActive(!isPaused);
    }

    public void Resume()
    {
        Time.timeScale = 1;
        pauseUI.SetActive(false);
    }

    public void Replay()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Save()
    {
        GameObject boardObject = GameObject.Find("bg_Board");
        if (boardObject != null)
        {
            GameBoard board = boardObject.GetComponent<GameBoard>();
            if (board != null)
            {
                SaveSystem.SaveGame(board);
                Debug.Log("Game saved!");
            }
        }
    }

    public void Quit()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Mainmenu");
    }

    public void OnVolumeChanged(float value)
    {
        value = volumeSlider.value;

        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
        PlayerPrefs.Save();

        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = value;
        }
    }

    // Called when "End Game" is clicked
    public void ShowEndGameInput()
    {
        pauseUI.SetActive(false);
        nameInputPanel.SetActive(true);
    }

    // Called when "Confirm" is clicked
    public void ConfirmEndGame()
    {
        string playerName = nameInputField != null ? nameInputField.text : "Anonymous";

        if (!string.IsNullOrEmpty(playerName))
        {
            GameObject boardObject = GameObject.Find("bg_Board");
            if (boardObject != null)
            {
                GameBoard board = boardObject.GetComponent<GameBoard>();
                if (board != null)
                {
                    RecordSystem.SaveRecord(playerName, board.score);
                    Debug.Log("Record saved for " + playerName + ": " + board.score);
                }
            }
        }

        Time.timeScale = 1;
        SaveSystem.ClearSaveFile();
        SceneManager.LoadScene("Mainmenu");
    }

    // Called when "Cancel" is clicked
    public void CancelEndGame()
    {
        nameInputPanel.SetActive(false);
        pauseUI.SetActive(true);
    }
}
