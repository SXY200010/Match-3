using UnityEngine;

public enum BuffKind
{
    FruitScorePercent,        // +x% score for a specific fruit (by name)
    RowBombExtraWidth,        // +x tiles width for 4-match row bomb
    ColorBombTotalBonus,      // +x% total score when color bomb triggers
    ChainLayerBonusPercent    // +x% per extra chain layer (beyond the first)
}

[CreateAssetMenu(fileName = "BuffDefinition", menuName = "Match3/Buff Definition", order = 0)]
public class BuffDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;                    // unique ID for save/load
    public string displayName;           // UI title
    [TextArea] public string description;// UI description
    public Sprite icon;

    [Header("Effect")]
    public BuffKind kind;
    public string targetFruitName;       // used when kind == FruitScorePercent
    public float percentValue = 10f;     // generic percentage value
    public int extraWidth = 1;           // for RowBombExtraWidth
    public bool stackable = true;
    public int maxStacks = 99;
}
