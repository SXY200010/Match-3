using System.Collections.Generic;
using UnityEngine;

public class BuffPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private BuffCardUI card0;
    [SerializeField] private BuffCardUI card1;
    [SerializeField] private BuffCardUI card2;

    [Tooltip("Candidate buffs for random offering.")]
    public List<BuffDefinition> pool = new List<BuffDefinition>();

    private System.Random _rng = new System.Random();
    private bool _active;

    private void Awake()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }

    public void ShowChoices()
    {
        if (_active) return;
        var picks = PickThree(pool);
        if (picks.Count == 0) return;

        _active = true;
        if (panelRoot) panelRoot.SetActive(true);
        Time.timeScale = 0f;

        card0.Setup(picks[0], OnPick);
        if (picks.Count > 1) card1.Setup(picks[1], OnPick); else card1.gameObject.SetActive(false);
        if (picks.Count > 2) card2.Setup(picks[2], OnPick); else card2.gameObject.SetActive(false);
    }

    private void OnPick(BuffDefinition def)
    {
        BuffManager.Instance.AddBuff(def);
        Hide();
    }

    public void Hide()
    {
        if (!_active) return;
        _active = false;
        if (panelRoot) panelRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    private List<BuffDefinition> PickThree(List<BuffDefinition> src)
    {
        var list = new List<BuffDefinition>();
        if (src == null || src.Count == 0) return list;

        // Simple unique picks
        var indices = new List<int>();
        for (int i = 0; i < src.Count; i++) indices.Add(i);

        // Shuffle
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        int count = Mathf.Min(3, indices.Count);
        for (int k = 0; k < count; k++)
        {
            var def = src[indices[k]];
            if (def) list.Add(def);
        }
        return list;
    }
}
