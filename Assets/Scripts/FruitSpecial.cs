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
        if (colorbombIcon != null) colorbombIcon.SetActive(false);
    }

    // Activate as a color bomb (5-match)
    public void ActivateColorBomb()
    {
        isColorBomb = true;
        isRowBlaster = false;
        if (colorbombIcon != null) colorbombIcon.SetActive(true);
        if (bombIcon != null) bombIcon.SetActive(false);
        gameObject.tag = "ColorBomb";
    }

    public void ResetSpecial()
    {
        isRowBlaster = false;
        isColorBomb = false;
        if (bombIcon != null) bombIcon.SetActive(false);
    }
}
