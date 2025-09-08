using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    [Tooltip("Optional: Initial pool to display in the panel if not provided there.")]
    public List<BuffDefinition> defaultPool = new List<BuffDefinition>();

    // Active buffs and their stacks
    private readonly Dictionary<string, (BuffDefinition def, int stacks)> _active = new();

    // Threshold offering state
    [Header("Offer Settings")]
    public int scoreStep = 2000;
    private int _nextScoreMilestone = 2000;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetRun()
    {
        _active.Clear();
        _nextScoreMilestone = scoreStep;
    }

    public IReadOnlyDictionary<string, (BuffDefinition def, int stacks)> ActiveBuffs => _active;

    public bool AddBuff(BuffDefinition def)
    {
        if (def == null) return false;
        if (_active.TryGetValue(def.id, out var val))
        {
            if (!def.stackable) return false;
            int newStacks = Mathf.Min(val.stacks + 1, Mathf.Max(1, def.maxStacks));
            _active[def.id] = (def, newStacks);
        }
        else
        {
            _active.Add(def.id, (def, 1));
        }
        return true;
    }

    /// <summary>
    /// Call this right after totalScore increases, to see if a choice should be offered.
    /// </summary>
    public bool ShouldOfferOnScore(int totalScore)
    {
        if (totalScore >= _nextScoreMilestone)
        {
            // push milestone forward
            _nextScoreMilestone += scoreStep;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Call this when a large chain happens (>= minChain).
    /// </summary>
    public bool ShouldOfferOnChain(int chainDepth, int minChain = 3) => chainDepth >= minChain;

    /// <summary>
    /// Entry point for scoring/special effects. Call from your existing Remove/Score logic.
    /// </summary>
    /// <param name="baseScore">Base score for this match step</param>
    /// <param name="fruitName">Matched fruit prefab name or type name</param>
    /// <param name="createdRowBomb">Was a 4-match row bomb created this step</param>
    /// <param name="triggeredColorBomb">Was a color bomb triggered this step</param>
    /// <param name="chainDepth">Current chain depth (1 for first, 2 for second, ...)</param>
    /// <param name="rowBombWidth">IN/OUT: row bomb width (tiles)</param>
    /// <returns>Final score after buffs</returns>
    public int ApplyScoreMods(
        int baseScore,
        string fruitName,
        bool createdRowBomb,
        bool triggeredColorBomb,
        int chainDepth,
        ref int rowBombWidth)
    {
        if (_active.Count == 0) return baseScore;

        float totalMultiplier = 1f;
        int extraWidthAcc = 0;

        foreach (var kv in _active.Values)
        {
            var def = kv.def;
            int stacks = Mathf.Max(1, kv.stacks);

            switch (def.kind)
            {
                case BuffKind.FruitScorePercent:
                    if (!string.IsNullOrEmpty(def.targetFruitName) &&
                        string.Equals(def.targetFruitName, fruitName))
                    {
                        totalMultiplier += (def.percentValue / 100f) * stacks;
                    }
                    break;

                case BuffKind.RowBombExtraWidth:
                    if (createdRowBomb)
                    {
                        extraWidthAcc += def.extraWidth * stacks;
                    }
                    break;

                case BuffKind.ColorBombTotalBonus:
                    if (triggeredColorBomb)
                    {
                        totalMultiplier += (def.percentValue / 100f) * stacks;
                    }
                    break;

                case BuffKind.ChainLayerBonusPercent:
                    // Apply only to extra layers beyond first
                    if (chainDepth > 1)
                    {
                        int extraLayers = chainDepth - 1;
                        totalMultiplier += (def.percentValue / 100f) * extraLayers * stacks;
                    }
                    break;
            }
        }

        if (extraWidthAcc != 0)
        {
            rowBombWidth = Mathf.Max(1, rowBombWidth + extraWidthAcc);
        }

        int finalScore = Mathf.RoundToInt(baseScore * totalMultiplier);
        return Mathf.Max(0, finalScore);
    }

    // -------- Optional Save/Load helpers --------
    [System.Serializable]
    public class BuffSaveEntry { public string id; public int stacks; }

    public List<BuffSaveEntry> GetSaveData()
    {
        var list = new List<BuffSaveEntry>();
        foreach (var kv in _active)
            list.Add(new BuffSaveEntry { id = kv.Key, stacks = kv.Value.stacks });
        return list;
    }

    public void LoadFrom(List<BuffSaveEntry> saved, IEnumerable<BuffDefinition> poolForLookup)
    {
        _active.Clear();
        if (saved == null) return;

        // Build quick dict for lookup
        var dict = new Dictionary<string, BuffDefinition>();
        if (defaultPool != null)
            foreach (var d in defaultPool) if (d && !dict.ContainsKey(d.id)) dict.Add(d.id, d);
        if (poolForLookup != null)
            foreach (var d in poolForLookup) if (d && !dict.ContainsKey(d.id)) dict.Add(d.id, d);

        foreach (var e in saved)
        {
            if (e == null || string.IsNullOrEmpty(e.id)) continue;
            if (dict.TryGetValue(e.id, out var def))
                _active[e.id] = (def, Mathf.Max(1, e.stacks));
        }
    }
}
