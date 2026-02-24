using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public static class GenerateUIPanelsFromTemplate
{
    [MenuItem("Tools/Generate UI Panels from Construction")]
    public static void Generate()
    {
        var templatePath = "Assets/Objects/Prefabs/ConstructionPanel_New.prefab";
        var template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
        if (template == null)
        {
            Debug.LogError("Template not found: " + templatePath);
            return;
        }

        // Fix cost layout on template before cloning
        ApplyCostFixes(template);

        CreateCopy(template, "Assets/Objects/Prefabs/BarracksPanel_New.prefab", "BARRACKS");
        CreateCopy(template, "Assets/Objects/Prefabs/SettingsPanel_New.prefab", "SETTINGS");
        CreateCopy(template, "Assets/Objects/Prefabs/ShopPanel_New.prefab", "SHOP");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Panels generated from template.");
    }

    static void CreateCopy(GameObject template, string outPath, string headerText)
    {
        var temp = GameObject.Instantiate(template);
        temp.name = System.IO.Path.GetFileNameWithoutExtension(outPath);

        // Update header text if found
        var headers = temp.GetComponentsInChildren<TextMeshProUGUI>(true)
            .Where(t => t.gameObject.name.ToLower().Contains("header") || t.text.ToLower().Contains("construction"));
        foreach (var h in headers) h.text = headerText;

        PrefabUtility.SaveAsPrefabAsset(temp, outPath);
        GameObject.DestroyImmediate(temp);
    }

    static void ApplyCostFixes(GameObject root)
    {
        // Fix CostGroup layout
        foreach (var costGroup in root.GetComponentsInChildren<HorizontalLayoutGroup>(true)
                     .Where(h => h.gameObject.name.ToLower().Contains("cost")))
        {
            costGroup.spacing = 6;
            costGroup.childControlWidth = true;
            costGroup.childControlHeight = true;
            costGroup.childForceExpandWidth = false;
            costGroup.childForceExpandHeight = false;
        }

        // Fix cost icons
        foreach (var img in root.GetComponentsInChildren<Image>(true)
                     .Where(i => i.gameObject.name.ToLower().Contains("costicon")))
        {
            var rt = img.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(28, 28);
            var le = img.GetComponent<LayoutElement>();
            if (le == null) le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 28;
            le.preferredHeight = 28;
            le.flexibleWidth = 0;
            le.flexibleHeight = 0;
        }

        // Fix cost texts
        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true)
                     .Where(t => t.gameObject.name.ToLower().Contains("costtext")))
        {
            tmp.enableAutoSizing = true;
            tmp.fontSize = 18;
            tmp.fontSizeMin = 12;
            tmp.fontSizeMax = 20;
            tmp.alignment = TextAlignmentOptions.Left;
            var rt = tmp.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40, 28);
            var le = tmp.GetComponent<LayoutElement>();
            if (le == null) le = tmp.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 40;
            le.preferredHeight = 28;
            le.flexibleWidth = 0;
            le.flexibleHeight = 0;
        }
    }
}
