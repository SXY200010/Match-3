using UnityEngine;
using UnityEngine.EventSystems;

public class FruitDrag : MonoBehaviour, IPointerDownHandler
{
    private static GameObject selectedFruit;
    private static Material glowMaterial;
    private static Material defaultMaterial;

    private GameBoard board;
    private Vector2Int myPos;
    private SpriteRenderer visualRenderer;

    void Start()
    {
        board = GetComponentInParent<GameBoard>();

        if (glowMaterial == null)
            glowMaterial = Resources.Load<Material>("GlowMaterial");

        Transform visual = transform.Find("Visual");
        if (visual != null)
        {
            visualRenderer = visual.GetComponent<SpriteRenderer>();
            if (defaultMaterial == null && visualRenderer != null)
                defaultMaterial = visualRenderer.material;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Clicked: " + gameObject.name);

        if (board == null || visualRenderer == null || board.isBusy) return;

        myPos = board.GetGridPosition(transform.position);

        if (selectedFruit == null)
        {
            selectedFruit = this.gameObject;
            ApplyGlow(true);
        }
        else if (selectedFruit == this.gameObject)
        {
            ApplyGlow(false);
            selectedFruit = null;
        }
        else
        {
            Vector2Int selectedPos = board.GetGridPosition(selectedFruit.transform.position);
            FruitDrag otherDrag = selectedFruit.GetComponent<FruitDrag>();

            if (IsAdjacent(selectedPos, myPos))
            {
                otherDrag.ApplyGlow(false);
                board.TrySwapFruits(selectedPos, myPos);
                selectedFruit = null;
            }
            else
            {
                otherDrag.ApplyGlow(false);
                ApplyGlow(true);
                selectedFruit = this.gameObject;
            }
        }
    }

    private void ApplyGlow(bool on)
    {
        if (visualRenderer != null)
        {
            visualRenderer.material = on ? glowMaterial : defaultMaterial;
        }
    }

    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        return (Mathf.Abs(a.x - b.x) == 1 && a.y == b.y) ||
               (Mathf.Abs(a.y - b.y) == 1 && a.x == b.x);
    }
}
