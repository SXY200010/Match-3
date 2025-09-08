using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BuffCardUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text desc;
    private BuffDefinition _def;
    private Action<BuffDefinition> _onPick;

    public void Setup(BuffDefinition def, Action<BuffDefinition> onPick)
    {
        _def = def;
        _onPick = onPick;

        if (icon) icon.sprite = def.icon;
        if (title) title.text = def.displayName;
        if (desc) desc.text = def.description;
        gameObject.SetActive(true);
    }

    public void OnClick()
    {
        if (_def != null) _onPick?.Invoke(_def);
    }
}
