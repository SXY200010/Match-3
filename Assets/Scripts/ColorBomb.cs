using UnityEngine;

public class ColorBomb : MonoBehaviour
{
    //private GameBoard board;

    //private void Awake()
    //{
    //    board = GetComponentInParent<GameBoard>();
    //    if (board == null)
    //        Debug.LogError("ColorBomb: GameBoard not found in parent hierarchy.");
    //}

    //public bool TryHandleSwap(GameObject other, Vector2Int posSelf, Vector2Int posOther)
    //{
    //    if (board == null || other == null) return false;

    //    bool otherIsColorBomb = other.GetComponent<ColorBomb>() != null || other.CompareTag("ColorBomb");

    //    if (otherIsColorBomb)
    //    {
    //        board.PublicRemoveAt(posSelf, destroy: true);
    //        board.PublicRemoveAt(posOther, destroy: true);
    //        board.PublicClearWholeBoard(additionalScore: 300);
    //        return true;
    //    }

    //    string targetTag = other.tag;
    //    board.PublicRemoveAt(posSelf, destroy: true);
    //    board.PublicRemoveAt(posOther, destroy: true);
    //    board.PublicClearAllOfTag(targetTag, additionalScore: 120);
    //    return true;
    //}
}
