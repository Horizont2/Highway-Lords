using UnityEngine;
using UnityEditor;
using TMPro;
using System.Linq;

public static class GeneratePanelsFromExisting
{
    [MenuItem("Tools/Generate New Panels From Existing")]
    public static void Generate()
    {
        var templatePath = "Assets/Panels/ConstructionPanel_New.prefab";
        var template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
        if (template == null)
        {
            Debug.LogError("Template not found: " + templatePath);
            return;
        }

        CreateFrom(template, "Assets/Objects/Prefabs/BarracksPanel_UI.prefab", "Assets/Panels/BarracksPanel_New.prefab", "BARRACKS");
        CreateFrom(template, "Assets/Objects/Prefabs/ShopPanel_UI.prefab", "Assets/Panels/ShopPanel_New.prefab", "SHOP");
        CreateFrom(template, "Assets/Objects/Prefabs/SettingsPanel_UI.prefab", "Assets/Panels/SettingsPanel_New.prefab", "SETTINGS");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("New panels generated from ConstructionPanel_New.");
    }

    static void CreateFrom(GameObject template, string sourcePath, string outPath, string headerText)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
        {
            Debug.LogWarning("Source panel not found: " + sourcePath);
            return;
        }

        var temp = GameObject.Instantiate(template);
        temp.name = System.IO.Path.GetFileNameWithoutExtension(outPath);

        // Set header
        foreach (var h in temp.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (h.text != null && h.text.ToLower().Contains("construct"))
                h.text = headerText;
        }

        // Extract names/descs from source
        var sourceTmps = source.GetComponentsInChildren<TextMeshProUGUI>(true);
        var names = sourceTmps.Where(t => t.gameObject.name.ToLower().Contains("name"))
                              .Select(t => t.text.Trim())
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .ToList();
        var descs = sourceTmps.Where(t => t.gameObject.name.ToLower().Contains("desc"))
                              .Select(t => t.text.Trim())
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .ToList();

        if (names.Count == 0)
            names = sourceTmps.Where(t => !IsMostlyNumeric(t.text))
                              .Select(t => t.text.Trim())
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .ToList();

        var nameFields = temp.GetComponentsInChildren<TextMeshProUGUI>(true)
                             .Where(t => t.gameObject.name.ToLower().Contains("name"))
                             .ToList();
        var descFields = temp.GetComponentsInChildren<TextMeshProUGUI>(true)
                             .Where(t => t.gameObject.name.ToLower().Contains("desc"))
                             .ToList();

        for (int i = 0; i < nameFields.Count && i < names.Count; i++)
            nameFields[i].text = names[i];

        for (int i = 0; i < descFields.Count && i < descs.Count; i++)
            descFields[i].text = descs[i];

        PrefabUtility.SaveAsPrefabAsset(temp, outPath);
        GameObject.DestroyImmediate(temp);
    }

    static bool IsMostlyNumeric(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return true;
        int digits = s.Count(char.IsDigit);
        return digits > 0 && digits >= s.Length / 2;
    }
}
