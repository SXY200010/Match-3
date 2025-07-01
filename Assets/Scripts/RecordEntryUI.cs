using TMPro;
using UnityEngine;

public class RecordEntryUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text scoreText;

    public void SetData(string name, int score)
    {
        nameText.text = name;
        scoreText.text = score.ToString();
    }
}
