using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    public int width = 8;
    public int height = 8;
    public float cellSize = 1.0f;
    public GameObject[] fruitPrefabs;

    private GameObject[] availableFruits; 
    private const int fruitLimit = 5;

    private GameObject[,] grid;
    public int score = 0;
    public bool isBusy = false;

    private AudioSource audioSource;
    public AudioClip swapSound;
    public AudioClip matchSound;


    void Start()
    {
        grid = new GameObject[width, height];

        availableFruits = PickRandomFruits(fruitPrefabs, fruitLimit);

        GenerateBoard();
        StartCoroutine(CheckAndCollapse());

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    private GameObject[] PickRandomFruits(GameObject[] source, int count)
    {
        List<GameObject> list = new List<GameObject>(source);
        List<GameObject> result = new List<GameObject>();

        for (int i = 0; i < count && list.Count > 0; i++)
        {
            int index = Random.Range(0, list.Count);
            result.Add(list[index]);
            list.RemoveAt(index);
        }

        return result.ToArray();
    }


    void GenerateBoard()
    {
        float xOffset = -(width - 1) * cellSize / 2f;
        float yOffset = -(height - 1) * cellSize / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isCorner = (x == 0 && y == 0) || (x == 0 && y == height - 1) || (x == width - 1 && y == 0) || (x == width - 1 && y == height - 1);
                if (isCorner) continue;

                Vector3 position = new Vector3(x * cellSize + xOffset, y * cellSize + yOffset, 0);
                int randomIndex = Random.Range(0, availableFruits.Length);
                GameObject fruit = Instantiate(availableFruits[randomIndex], position, Quaternion.identity);
                fruit.transform.parent = this.transform;
                grid[x, y] = fruit;
            }
        }
    }

    IEnumerator CheckAndCollapse()
    {
        yield return new WaitForSeconds(0.2f);

        while (true)
        {
            yield return new WaitForSeconds(0.3f);

            var matches = FindMatches();
            if (matches.Count == 0)
            {
                break;
            }

            RemoveMatches(matches);
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(CollapseColumns());
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(FillBoard());
        }

        Debug.Log("Final Score: " + score);
    }

    HashSet<GameObject> FindMatches()
    {
        HashSet<GameObject> fruitsToDestroy = new HashSet<GameObject>();

        // Check horizontal
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                if (grid[x, y] && grid[x + 1, y] && grid[x + 2, y])
                {
                    if (grid[x, y].tag == grid[x + 1, y].tag && grid[x, y].tag == grid[x + 2, y].tag)
                    {
                        fruitsToDestroy.Add(grid[x, y]);
                        fruitsToDestroy.Add(grid[x + 1, y]);
                        fruitsToDestroy.Add(grid[x + 2, y]);
                    }
                }
            }
        }

        // Check vertical
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                if (grid[x, y] && grid[x, y + 1] && grid[x, y + 2])
                {
                    if (grid[x, y].tag == grid[x, y + 1].tag && grid[x, y].tag == grid[x, y + 2].tag)
                    {
                        fruitsToDestroy.Add(grid[x, y]);
                        fruitsToDestroy.Add(grid[x, y + 1]);
                        fruitsToDestroy.Add(grid[x, y + 2]);
                    }
                }
            }
        }
        return fruitsToDestroy;
    }

    void RemoveMatches(HashSet<GameObject> matches)
    {
        int count = matches.Count;
        if (count == 0) return;

        if (matchSound != null) audioSource.PlayOneShot(matchSound,2.0f);

        foreach (var fruit in matches)
        {
            Vector2Int pos = GetGridPosition(fruit.transform.position);
            grid[pos.x, pos.y] = null;
            Destroy(fruit);
        }

        if (count == 3) score += 30;
        else if (count == 4) score += 60;
        else if (count >= 5) score += 100;

        Debug.Log("Score: " + score);
    }

    private IEnumerator CollapseColumns()
    {
        List<Coroutine> coroutines = new List<Coroutine>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 1; y < height; y++)
            {
                if (grid[x, y] != null && grid[x, y - 1] == null)
                {
                    int dropTo = y - 1;
                    while (dropTo > 0 && grid[x, dropTo - 1] == null)
                    {
                        dropTo--;
                    }

                    if ((x == 0 && dropTo == 0) || (x == width - 1 && dropTo == 0)) continue;

                    GameObject fruit = grid[x, y];
                    grid[x, dropTo] = fruit;
                    grid[x, y] = null;

                    Vector3 targetPos = GetWorldPosition(x, dropTo);
                    Coroutine move = StartCoroutine(AnimateMove(fruit, targetPos));
                    coroutines.Add(move);
                }
            }
        }

        foreach (var co in coroutines)
        {
            yield return co;
        }
    }

    IEnumerator FillBoard()
    {
        List<Coroutine> coroutines = new List<Coroutine>();

        float xOffset = -(width - 1) * cellSize / 2f;
        float yOffset = -(height - 1) * cellSize / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isCorner =
                    (x == 0 && y == 0) ||
                    (x == 0 && y == height - 1) ||
                    (x == width - 1 && y == 0) ||
                    (x == width - 1 && y == height - 1);

                if (grid[x, y] == null && !isCorner)
                {
                    Vector3 position = new Vector3(x * cellSize + xOffset, y * cellSize + yOffset, 0);
                    int randomIndex = Random.Range(0, availableFruits.Length);
                    GameObject fruit = Instantiate(availableFruits[randomIndex], position + new Vector3(0, cellSize * 2, 0), Quaternion.identity);
                    fruit.transform.parent = this.transform;

                    Coroutine move = StartCoroutine(MoveDown(fruit, position));
                    coroutines.Add(move);

                    grid[x, y] = fruit;
                }
            }
        }

        foreach (var co in coroutines)
        {
            yield return co;
        }
    }

    IEnumerator MoveDown(GameObject fruit, Vector3 targetPos)
    {
        while (Vector3.Distance(fruit.transform.position, targetPos) > 0.01f)
        {
            fruit.transform.position = Vector3.MoveTowards(fruit.transform.position, targetPos, 10f * Time.deltaTime);
            yield return null;
        }
        fruit.transform.position = targetPos;
    }

    public Vector2Int GetGridPosition(Vector3 worldPos)
    {
        float xOffset = -(width - 1) * cellSize / 2f;
        float yOffset = -(height - 1) * cellSize / 2f;

        int x = Mathf.RoundToInt((worldPos.x - xOffset) / cellSize);
        int y = Mathf.RoundToInt((worldPos.y - yOffset) / cellSize);

        return new Vector2Int(x, y);
    }

    public void TrySwapFruits(Vector2Int posA, Vector2Int posB)
    {
        GameObject fruitA = grid[posA.x, posA.y];
        GameObject fruitB = grid[posB.x, posB.y];

        if (fruitA == null || fruitB == null) return;

        StartCoroutine(SwapAndCheck(fruitA, fruitB, posA, posB));
    }

    private IEnumerator CheckSwapResult(Vector2Int posA, Vector2Int posB)
    {
        yield return new WaitForSeconds(0.3f);

        var matches = FindMatches();
        if (matches.Count > 0)
        {
            yield return StartCoroutine(CollapseColumns());
            yield return new WaitForSeconds(0.3f);

            do
            {
                RemoveMatches(matches);
                yield return new WaitForSeconds(0.3f);

                yield return StartCoroutine(CollapseColumns());
                yield return new WaitForSeconds(0.3f);

                yield return FillBoard();
                yield return new WaitForSeconds(0.3f);

                matches = FindMatches();
            }
            while (matches.Count > 0);
        }
        else
        {
            GameObject fruitA = grid[posA.x, posA.y];
            GameObject fruitB = grid[posB.x, posB.y];

            grid[posA.x, posA.y] = fruitB;
            grid[posB.x, posB.y] = fruitA;

            yield return StartCoroutine(SwapAnimation(fruitA, fruitB));
        }
    }

    private IEnumerator SwapAnimation(GameObject a, GameObject b)
    {
        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;
        float time = 0f;
        float duration = 0.2f;

        while (time < duration)
        {
            a.transform.position = Vector3.Lerp(posA, posB, time / duration);
            b.transform.position = Vector3.Lerp(posB, posA, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        a.transform.position = posB;
        b.transform.position = posA;
    }

    private IEnumerator SwapAndCheck(GameObject fruitA, GameObject fruitB, Vector2Int posA, Vector2Int posB)
    {
        isBusy = true;
        if (swapSound != null) audioSource.PlayOneShot(swapSound,2.0f);

        grid[posA.x, posA.y] = fruitB;
        grid[posB.x, posB.y] = fruitA;

        yield return StartCoroutine(SwapAnimation(fruitA, fruitB));
        yield return StartCoroutine(CheckSwapResult(posA, posB));

        isBusy = false;
    }

    private IEnumerator AnimateMove(GameObject fruit, Vector3 targetPos)
    {
        while (Vector3.Distance(fruit.transform.position, targetPos) > 0.01f)
        {
            fruit.transform.position = Vector3.MoveTowards(fruit.transform.position, targetPos, 10f * Time.deltaTime);
            yield return null;
        }
        fruit.transform.position = targetPos;
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        float xOffset = -(width - 1) * cellSize / 2f;
        float yOffset = -(height - 1) * cellSize / 2f;
        return new Vector3(x * cellSize + xOffset, y * cellSize + yOffset, 0);
    }

}
