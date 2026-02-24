using UnityEngine;
using UnityEditor;

public static class ExtractPanelsFromCanvas
{
    [MenuItem("Tools/Extract UI Panels from Canvas")]
    public static void Extract()
    {
        var canvasPrefabPath = "Assets/Objects/Prefabs/Canvas.prefab";
        var canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(canvasPrefabPath);
        if (canvasPrefab == null)
        {
            Debug.LogError("Canvas prefab not found: " + canvasPrefabPath);
            return;
        }

        ExtractPanel(canvasPrefab, "BarracksPanel", "Assets/Objects/Prefabs/BarracksPanel_UI.prefab");
        ExtractPanel(canvasPrefab, "SettingsPanel", "Assets/Objects/Prefabs/SettingsPanel_UI.prefab");
        ExtractPanel(canvasPrefab, "ShopPanel", "Assets/Objects/Prefabs/ShopPanel_UI.prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Panels extracted from Canvas prefab.");
    }

    static void ExtractPanel(GameObject canvasPrefab, string panelName, string outPath)
    {
        var root = GameObject.Instantiate(canvasPrefab);
        var panel = FindChildByName(root.transform, panelName);
        if (panel == null)
        {
            Debug.LogWarning($"Panel not found: {panelName}");
            GameObject.DestroyImmediate(root);
            return;
        }

        var panelGO = panel.gameObject;
        panel.SetParent(null, true);
        PrefabUtility.SaveAsPrefabAsset(panelGO, outPath);
        GameObject.DestroyImmediate(root);
    }

    static Transform FindChildByName(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }
}
