using System.Collections.Generic;
using UnityEngine;

public class DialogueLocalizationManager : MonoBehaviour
{
    public static DialogueLocalizationManager Instance;

    [Header("Dialogue TSV Files")]
    [SerializeField] private TextAsset[] dialogueTSVs;

    private readonly Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadTSVs();
            return;
        }

        Destroy(gameObject);
    }

    private void LoadTSVs()
    {
        dictionary.Clear();

        if (dialogueTSVs == null || dialogueTSVs.Length == 0)
            return;

        foreach (TextAsset dialogueTSV in dialogueTSVs)
        {
            if (dialogueTSV == null)
                continue;

            string[] rows = dialogueTSV.text.Split('\n');
            for (int i = 1; i < rows.Length; i++)
            {
                string row = rows[i].TrimEnd('\r', '\n');
                if (string.IsNullOrWhiteSpace(row))
                    continue;

                string[] columns = row.Split('\t');
                if (columns.Length < 3)
                    continue;

                string key = columns[0].Trim();
                if (string.IsNullOrEmpty(key))
                    continue;

                dictionary[key] = new[] { columns[1], columns[2] };
            }
        }
    }

    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "";

        if (!dictionary.TryGetValue(key, out string[] texts))
            return key;

        if (LocalizationManager.Instance == null)
            return texts[0];

        return LocalizationManager.Instance.currentLanguage == LocalizationManager.Language.Korean ? texts[0] : texts[1];
    }
}
