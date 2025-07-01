using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class RecordPanel : MonoBehaviour
{
    public GameObject recordEntryPrefab;
    public Transform recordContainer;
    public GameObject panel;

    private void OnEnable()
    {
        RefreshRecords();
    }

    public void RefreshRecords()
    {
        foreach (Transform child in recordContainer)
        {
            Destroy(child.gameObject);
        }

        List<GameRecord> records = RecordSystem.LoadRecords();
        records = records.OrderByDescending(r => r.score).ToList();

        if (records.Count == 0)
        {
            GameObject empty = Instantiate(recordEntryPrefab, recordContainer);
            empty.GetComponentInChildren<TextMeshProUGUI>().text = "No records yet.";
            return;
        }

        float spacing = 80f;
        float topOffset = 150f;
        float totalHeight = records.Count * spacing + topOffset;

        RectTransform contentRT = (RectTransform)recordContainer;
        contentRT.sizeDelta = new Vector2(contentRT.sizeDelta.x, totalHeight);

        for (int i = 0; i < records.Count; i++)
        {
            GameRecord record = records[i];
            GameObject entry = Instantiate(recordEntryPrefab, recordContainer);
            RectTransform rt = entry.GetComponent<RectTransform>();

            rt.offsetMin = new Vector2(0, -spacing);
            rt.offsetMax = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(0, -topOffset - i * spacing);

            RecordEntryUI ui = entry.GetComponent<RecordEntryUI>();
            ui.SetData(record.playerName, record.score);
        }

    }


    public void ShowPanel()
    {
        panel.SetActive(true);
        RefreshRecords();
    }

    public void HidePanel()
    {
        panel.SetActive(false);
    }
}
