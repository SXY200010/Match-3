using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    public int width = 8;
    public int height = 8;
    public float cellSize = 1.0f;

    // Only normal fruit prefabs here (no special prefabs)
    public GameObject[] fruitPrefabs;

    private GameObject[] availableFruits;
    private const int fruitLimit = 5;

    public GameObject[,] grid;
    public int score = 0;
    public bool isBusy = false;

    private AudioSource audioSource;
    public AudioClip swapSound;
    public AudioClip matchSound;

    public GameObject matchParticlePrefab;
    public PauseMenu pauseMenu;

    public BuffPanelUI buffPanelUI;     // Assign in inspector
    private int currentChainDepth = 0;  // Tracks cascading depth for buff triggers

    void Start()
    {
        grid = new GameObject[width, height];

        SaveSystem.LoadGame(this);

        if (IsBoardEmpty())
        {
            availableFruits = PickRandomFruits(fruitPrefabs, fruitLimit);
            GenerateBoard();
            StartCoroutine(CheckAndCollapse());
        }

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    private bool IsBoardEmpty()
    {
        foreach (var cell in grid)
        {
            if (cell != null) return false;
        }
        return true;
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

                // ensure FruitSpecial exists and visuals are reset
                var fs = fruit.GetComponent<FruitSpecial>();
                if (fs != null) fs.ResetSpecial();

                grid[x, y] = fruit;
            }
        }
    }

    IEnumerator CheckAndCollapse()
    {
        yield return new WaitForSeconds(0.2f);

        while (true)
        {
            yield return new WaitForSeconds(0.25f);

            var groups = FindMatchGroups();
            if (groups.Count == 0)
                break;

            // increase chain depth for this cascade wave
            currentChainDepth++;

            ProcessMatchGroups(groups); // scoring happens inside

            // --- chain-based offer (>=3) ---
            if (BuffManager.Instance.ShouldOfferOnChain(currentChainDepth, 3) && buffPanelUI != null)
            {
                buffPanelUI.ShowChoices();
            }

            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(CollapseColumns());
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(FillBoard());
        }

        // reset depth after all cascades end
        currentChainDepth = 0;

        if (!HasPossibleMoves())
        {
            Debug.Log("No more possible moves. Game Over!");
            if (pauseMenu != null)
            {
                pauseMenu.ShowEndGameInput();
            }
        }
        Debug.Log("Final Score: " + score);
    }

    // ---------- Matching as groups (handles separate groups and T/L shapes) ----------
    private List<HashSet<GameObject>> FindMatchGroups()
    {
        var groups = new List<HashSet<GameObject>>();

        // Horizontal runs
        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (x < width - 2)
            {
                if (grid[x, y] == null) { x++; continue; }
                string tag = grid[x, y].tag;
                int run = 1;
                int k = x + 1;
                while (k < width && grid[k, y] != null && grid[k, y].tag == tag) { run++; k++; }
                if (run >= 3)
                {
                    var set = new HashSet<GameObject>();
                    for (int c = x; c < x + run; c++) set.Add(grid[c, y]);
                    MergeOrAddGroup(groups, set);
                }
                x = (run > 1) ? (x + run) : (x + 1);
            }
        }

        // Vertical runs
        for (int x = 0; x < width; x++)
        {
            int y = 0;
            while (y < height - 2)
            {
                if (grid[x, y] == null) { y++; continue; }
                string tag = grid[x, y].tag;
                int run = 1;
                int k = y + 1;
                while (k < height && grid[x, k] != null && grid[x, k].tag == tag) { run++; k++; }
                if (run >= 3)
                {
                    var set = new HashSet<GameObject>();
                    for (int r = y; r < y + run; r++) set.Add(grid[x, r]);
                    MergeOrAddGroup(groups, set);
                }
                y = (run > 1) ? (y + run) : (y + 1);
            }
        }

        return groups;
    }

    private void MergeOrAddGroup(List<HashSet<GameObject>> groups, HashSet<GameObject> newSet)
    {
        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i].Overlaps(newSet))
            {
                groups[i].UnionWith(newSet);
                return;
            }
        }
        groups.Add(newSet);
    }

    // ---------- Process groups: spawn/trigger specials ----------
    private void ProcessMatchGroups(List<HashSet<GameObject>> groups)
    {
        if (groups.Count == 0) return;
        if (matchSound != null) audioSource.PlayOneShot(matchSound, 2.0f);

        foreach (var group in groups)
        {
            // If a row blaster is inside the group -> trigger row clear instead of normal removal
            var blaster = group.Select(g => g ? g.GetComponent<FruitSpecial>() : null)
                               .FirstOrDefault(fs => fs != null && fs.isRowBlaster);
            if (blaster != null)
            {
                // --- apply row blaster with width buffs ---
                int baseRowWidth = 1; // 1 row by default
                int dummyScore = 60;  // your original bonus for activating blaster
                string fruitNameForBuff = blaster.tag; // use tag for fruit type

                // ask BuffManager to modify score & width
                int rowWidth = baseRowWidth;
                int finalScore = BuffManager.Instance.ApplyScoreMods(
                    dummyScore,
                    fruitNameForBuff,
                    createdRowBomb: true,          // created/used a row bomb
                    triggeredColorBomb: false,
                    chainDepth: Mathf.Max(1, currentChainDepth),
                    rowBombWidth: ref rowWidth
                );

                // Clear multiple rows according to width (center +/- extra)
                Vector2Int p = GetGridPosition(blaster.transform.position);
                ClearRowWithWidth(p.y, rowWidth);

                Destroy(blaster.gameObject);
                grid[p.x, p.y] = null;

                // add modified score
                score += finalScore;

                // score-based offer
                if (BuffManager.Instance.ShouldOfferOnScore(score) && buffPanelUI != null)
                    buffPanelUI.ShowChoices();

                continue;
            }

            // --- normal groups: compute base & upgrades ---
            int size = group.Count;
            int baseScore = 0;
            bool willCreateRowBomb = false;
            bool willCreateColorBomb = false;

            if (size >= 5)
            {
                baseScore = 100;
                willCreateColorBomb = true;
            }
            else if (size == 4)
            {
                baseScore = 60;
                willCreateRowBomb = true;
            }
            else
            {
                baseScore = 30;
            }

            // representative fruit type for FruitScorePercent buff (use tag)
            string fruitName = group.FirstOrDefault(g => g != null)?.tag ?? "Unknown";

            // row bomb width to be possibly increased by buffs (only used if willCreateRowBomb)
            int rowWidthForCreation = 1;

            // ask BuffManager for final score and maybe modified width
            int finalStepScore = BuffManager.Instance.ApplyScoreMods(
                baseScore,
                fruitName,
                createdRowBomb: willCreateRowBomb,
                triggeredColorBomb: willCreateColorBomb,
                chainDepth: Mathf.Max(1, currentChainDepth),
                rowBombWidth: ref rowWidthForCreation
            );

            // apply special upgrades and removals
            if (willCreateColorBomb)
            {
                UpgradeOneToColorBomb(group);
                RemoveOthersInGroup(group);
            }
            else if (willCreateRowBomb)
            {
                // visually mark ONE fruit as row blaster as before
                UpgradeOneToRowBlaster(group);
                RemoveOthersInGroup(group);
                // note: the extra width is consumed when the blaster is actually triggered later
                // (handled by the 'blaster != null' branch above)
            }
            else
            {
                RemoveGroup(group);
            }

            // add modified score
            score += finalStepScore;

            // score-based offer
            if (BuffManager.Instance.ShouldOfferOnScore(score) && buffPanelUI != null)
                buffPanelUI.ShowChoices();
        }

        Debug.Log("Score: " + score);
    }
    private void RemoveGroup(HashSet<GameObject> group)
    {
        foreach (var fruit in group)
        {
            if (fruit == null) continue;
            Vector2Int pos = GetGridPosition(fruit.transform.position);
            grid[pos.x, pos.y] = null;

            if (matchParticlePrefab != null)
                Instantiate(matchParticlePrefab, fruit.transform.position, Quaternion.identity);
            Destroy(fruit);
        }
    }

    private void RemoveOthersInGroup(HashSet<GameObject> group)
    {
        // Keep the upgraded one (it has been marked), remove others
        foreach (var fruit in group)
        {
            if (fruit == null) continue;
            var fs = fruit.GetComponent<FruitSpecial>();
            if (fs != null && (fs.isRowBlaster || fs.isColorBomb))
                continue;

            Vector2Int pos = GetGridPosition(fruit.transform.position);
            grid[pos.x, pos.y] = null;

            if (matchParticlePrefab != null)
                Instantiate(matchParticlePrefab, fruit.transform.position, Quaternion.identity);
            Destroy(fruit);
        }
    }

    private void UpgradeOneToRowBlaster(HashSet<GameObject> group)
    {
        GameObject keeper = group.FirstOrDefault(g => g != null);
        if (keeper == null) return;

        var fs = keeper.GetComponent<FruitSpecial>();
        if (fs != null) fs.ActivateRowBlaster();
        // grid stays the same; visual icon is shown
    }

    private void UpgradeOneToColorBomb(HashSet<GameObject> group)
    {
        GameObject keeper = group.FirstOrDefault(g => g != null);
        if (keeper == null) return;

        var fs = keeper.GetComponent<FruitSpecial>();
        if (fs != null) fs.ActivateColorBomb();
        // grid stays the same; icon is shown (you can use a different icon if preferred)
    }

    private void ClearRow(int y)
    {
        for (int x = 0; x < width; x++)
        {
            if (grid[x, y] == null) continue;
            var go = grid[x, y];
            grid[x, y] = null;

            if (matchParticlePrefab != null)
                Instantiate(matchParticlePrefab, go.transform.position, Quaternion.identity);
            Destroy(go);
        }
    }

    private void ClearAllOfTag(string tagToClear)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null) continue;
                if (grid[x, y].tag == tagToClear)
                {
                    var go = grid[x, y];
                    grid[x, y] = null;

                    if (matchParticlePrefab != null)
                        Instantiate(matchParticlePrefab, go.transform.position, Quaternion.identity);
                    Destroy(go);
                }
            }
        }
    }

    // ---------- Grid maintenance ----------
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
                        dropTo--;

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
            yield return co;
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

                    var fs = fruit.GetComponent<FruitSpecial>();
                    if (fs != null) fs.ResetSpecial();

                    Coroutine move = StartCoroutine(MoveDown(fruit, position));
                    coroutines.Add(move);

                    grid[x, y] = fruit;
                }
            }
        }

        foreach (var co in coroutines)
            yield return co;
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

    // ---------- Swap & special interactions ----------
    public void TrySwapFruits(Vector2Int posA, Vector2Int posB)
    {
        GameObject fruitA = grid[posA.x, posA.y];
        GameObject fruitB = grid[posB.x, posB.y];

        if (fruitA == null || fruitB == null) return;

        StartCoroutine(SwapAndCheck(fruitA, fruitB, posA, posB));
    }

    private IEnumerator SwapAndCheck(GameObject fruitA, GameObject fruitB, Vector2Int posA, Vector2Int posB)
    {
        isBusy = true;
        if (swapSound != null) audioSource.PlayOneShot(swapSound, 2.0f);

        grid[posA.x, posA.y] = fruitB;
        grid[posB.x, posB.y] = fruitA;

        yield return StartCoroutine(SwapAnimation(fruitA, fruitB));

        // Color bomb immediate activation when swapped with a normal fruit
        bool activated = TryActivateColorBombSwap(fruitA, fruitB, posA, posB);
        if (activated)
        {
            yield return new WaitForSeconds(0.15f);
            yield return StartCoroutine(CollapseColumns());
            yield return new WaitForSeconds(0.15f);
            yield return StartCoroutine(FillBoard());
            yield return new WaitForSeconds(0.15f);

            while (true)
            {
                var groups2 = FindMatchGroups();
                if (groups2.Count == 0) break;
                ProcessMatchGroups(groups2);
                yield return new WaitForSeconds(0.15f);
                yield return StartCoroutine(CollapseColumns());
                yield return new WaitForSeconds(0.15f);
                yield return StartCoroutine(FillBoard());
                yield return new WaitForSeconds(0.15f);
            }
            isBusy = false;
            yield break;
        }

        yield return StartCoroutine(CheckSwapResult(posA, posB));
        isBusy = false;
    }

    private bool TryActivateColorBombSwap(GameObject a, GameObject b, Vector2Int posA, Vector2Int posB)
    {
        var sa = a.GetComponent<FruitSpecial>();
        var sb = b.GetComponent<FruitSpecial>();

        bool aIsBomb = sa != null && sa.isColorBomb;
        bool bIsBomb = sb != null && sb.isColorBomb;

        // ColorBomb + normal fruit
        if (aIsBomb && !bIsBomb)
        {
            string targetTag = b.tag;
            Destroy(a); Destroy(b);
            grid[posA.x, posA.y] = null;
            grid[posB.x, posB.y] = null;

            ClearAllOfTag(targetTag);
            score += 120;
            return true;
        }
        if (bIsBomb && !aIsBomb)
        {
            string targetTag = a.tag;
            Destroy(a); Destroy(b);
            grid[posA.x, posA.y] = null;
            grid[posB.x, posB.y] = null;

            ClearAllOfTag(targetTag);
            score += 120;
            return true;
        }

        // Optional: ColorBomb + ColorBomb (clear everything)
        if (aIsBomb && bIsBomb)
        {
            Destroy(a); Destroy(b);
            grid[posA.x, posA.y] = null;
            grid[posB.x, posB.y] = null;

            // Clear the whole board
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (grid[x, y] != null) { Destroy(grid[x, y]); grid[x, y] = null; }
            score += 300;
            return true;
        }

        return false;
    }

    private IEnumerator CheckSwapResult(Vector2Int posA, Vector2Int posB)
    {
        yield return new WaitForSeconds(0.25f);

        var groups = FindMatchGroups();
        if (groups.Count > 0)
        {
            do
            {
                ProcessMatchGroups(groups);
                yield return new WaitForSeconds(0.2f);

                yield return StartCoroutine(CollapseColumns());
                yield return new WaitForSeconds(0.2f);

                yield return StartCoroutine(FillBoard());
                yield return new WaitForSeconds(0.2f);

                groups = FindMatchGroups();
            }
            while (groups.Count > 0);
        }
        else
        {
            // revert swap
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

    public GameObject GetFruitAt(int x, int y) => grid[x, y];

    public void ClearBoard()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        grid = new GameObject[width, height];
    }

    public void SpawnFruit(int x, int y, string fruitName)
    {
        GameObject prefab = fruitPrefabs.FirstOrDefault(f => f.name == fruitName);
        if (prefab != null)
        {
            Vector3 pos = GetWorldPosition(x, y);
            GameObject fruit = Instantiate(prefab, pos, Quaternion.identity, transform);

            var fs = fruit.GetComponent<FruitSpecial>();
            if (fs != null) fs.ResetSpecial();

            grid[x, y] = fruit;
        }
    }

    public void SpawnFruit(int x, int y, string fruitName, bool isRowBlaster, bool isColorBomb)
    {
        GameObject prefab = fruitPrefabs.FirstOrDefault(f => f.name == fruitName);
        if (prefab != null)
        {
            Vector3 pos = GetWorldPosition(x, y);
            GameObject fruit = Instantiate(prefab, pos, Quaternion.identity, transform);

            var fs = fruit.GetComponent<FruitSpecial>();
            if (fs != null)
            {
                fs.ResetSpecial(); // ensure clean before applying
                if (isRowBlaster)
                    fs.ActivateRowBlaster();
                else if (isColorBomb)
                    fs.ActivateColorBomb();
            }

            grid[x, y] = fruit;
        }
    }


    public List<string> GetAvailableFruitNames()
    {
        return availableFruits.Select(f => f.name.Replace("(Clone)", "")).ToList();
    }

    public void SetAvailableFruits(List<string> names)
    {
        List<GameObject> result = new List<GameObject>();
        foreach (string name in names)
        {
            GameObject prefab = fruitPrefabs.FirstOrDefault(f => f.name == name);
            if (prefab != null)
                result.Add(prefab);
        }
        availableFruits = result.ToArray();
    }

    // ---------- Possible move check ----------
    private bool HasPossibleMoves()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null) continue;

                // right swap
                if (x < width - 1 && grid[x + 1, y] != null)
                {
                    SwapInGrid(x, y, x + 1, y);
                    if (FindMatchGroups().Count > 0)
                    {
                        SwapInGrid(x, y, x + 1, y);
                        return true;
                    }
                    SwapInGrid(x, y, x + 1, y);
                }

                // up swap
                if (y < height - 1 && grid[x, y + 1] != null)
                {
                    SwapInGrid(x, y, x, y + 1);
                    if (FindMatchGroups().Count > 0)
                    {
                        SwapInGrid(x, y, x, y + 1);
                        return true;
                    }
                    SwapInGrid(x, y, x, y + 1);
                }
            }
        }
        return false;
    }

    private void SwapInGrid(int x1, int y1, int x2, int y2)
    {
        GameObject temp = grid[x1, y1];
        grid[x1, y1] = grid[x2, y2];
        grid[x2, y2] = temp;
    }

    private void ClearRowWithWidth(int centerY, int widthRows)
    {
        widthRows = Mathf.Max(1, widthRows);
        // collect rows: center, then +/-1, +/-2 ...
        List<int> rows = new List<int> { centerY };
        for (int i = 1; i < widthRows; i++)
        {
            rows.Add(centerY - i);
            rows.Add(centerY + i);
        }

        foreach (int y in rows)
        {
            if (y < 0 || y >= height) continue;
            ClearRow(y);
        }
    }
}
