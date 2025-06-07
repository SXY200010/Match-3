using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
   public GameBoard board;
    // Start is called before the first frame update

    public TMP_Text score;
    void Start()
    {
     
    }

    // Update is called once per frame
    void Update()
    {
        if (board != null)
        {
            score.text = board.score.ToString();
        }
    }
}
