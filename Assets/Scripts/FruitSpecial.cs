using UnityEngine;

public class FruitSpecial : MonoBehaviour
{
    [Header("Special flags")]
    public bool isRowBlaster = false;
    public bool isColorBomb = false;

    [Header("Visual hook")]
    public GameObject bombIcon;
    public GameObject colorbombIcon;// Assign the BombIcon child in the inspector

    // Activate as a row blaster (4-match)
    public void ActivateRowBlaster()
    {
        isRowBlaster = true;
        isColorBomb = false;
        if (bombIcon != null) bombIcon.SetActive(true);
    }

    // Activate as a color bomb (5-match)
    public void ActivateColorBomb()
    {
        isColorBomb = true;
        isRowBlaster = false;
        if (colorbombIcon != null) colorbombIcon.SetActive(true);
    }

    public void ResetSpecial()
    {
        isRowBlaster = false;
        isColorBomb = false;
        if (bombIcon != null) bombIcon.SetActive(false);
    }
}
