using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public static class GenerateConstructionPanel
{
    [MenuItem("Tools/Generate Construction Panel (Leather)")]
    public static void Generate()
    {
        // Try to grab sprites from existing ConstructionPanel_UI prefab
        var existingPrefabPath = "Assets/Objects/Prefabs/ConstructionPanel_UI.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(existingPrefabPath);
        Sprite panelSprite = null;
        Sprite buttonSprite = null;
        if (existing != null)
        {
            var images = existing.GetComponentsInChildren<Image>(true);
            panelSprite = images.FirstOrDefault(i => i.sprite != null)?.sprite;
            buttonSprite = images.FirstOrDefault(i => i.sprite != null && (i.gameObject.name.ToLower().Contains("btn") || i.gameObject.name.ToLower().Contains("build")))?.sprite;
        }

        // Resource icons
        Sprite gold = FindSprite("resourceicons_0");
        Sprite wood = FindSprite("resourceicons_1");
        Sprite stone = FindSprite("resourceicons_2");

        var root = new GameObject("ConstructionPanel_New", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900, 600);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        var bg = root.GetComponent<Image>();
        bg.sprite = panelSprite;
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = true;

        // Header
        var header = CreateText(root.transform, "Header", "CONSTRUCTIONS", 28, FontStyles.Bold, new Vector2(0.5f, 1f));
        var hrt = header.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0.5f, 1f);
        hrt.anchorMax = new Vector2(0.5f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.anchoredPosition = new Vector2(0, -25);
        hrt.sizeDelta = new Vector2(600, 40);

        // List
        var list = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
        list.transform.SetParent(root.transform, false);
        var lrt = list.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0, 0);
        lrt.anchorMax = new Vector2(1, 1);
        lrt.offsetMin = new Vector2(24, 20);
        lrt.offsetMax = new Vector2(-24, -70);

        var vlg = list.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        CreateRow(list.transform, "Barracks", "+Unlock units / +cap", "200", "100", gold, wood, buttonSprite);
        CreateRow(list.transform, "Spikes", "Stop carts / delay", "100", "", gold, null, buttonSprite);
        CreateRow(list.transform, "Mine", "+Gold income", "150", "50", gold, wood, buttonSprite);
        CreateRow(list.transform, "Tower Upgrade", "+DPS", "50", "20", gold, wood, buttonSprite, true);

        var path = "Assets/Objects/Prefabs/ConstructionPanel_New.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("ConstructionPanel_New prefab created at: " + path);
    }

    static Sprite FindSprite(string name)
    {
        var guids = AssetDatabase.FindAssets(name + " t:sprite");
        if (guids.Length == 0) return null;
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static GameObject CreateRow(Transform parent, string title, string desc, string costA, string costB, Sprite iconA, Sprite iconB, Sprite btnSprite, bool upgrade = false)
    {
        var row = new GameObject(title + "_Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        var rrt = row.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(760, 110);

        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 0, 0);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var le = row.GetComponent<LayoutElement>();
        le.preferredHeight = 110;

        var left = new GameObject("LeftGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        left.transform.SetParent(row.transform, false);
        var leftLE = left.GetComponent<LayoutElement>();
        leftLE.preferredWidth = 260;

        var leftHLG = left.GetComponent<HorizontalLayoutGroup>();
        leftHLG.spacing = 8;
        leftHLG.childAlignment = TextAnchor.MiddleLeft;
        leftHLG.childControlWidth = true;
        leftHLG.childControlHeight = true;
        leftHLG.childForceExpandWidth = false;
        leftHLG.childForceExpandHeight = false;

        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        icon.transform.SetParent(left.transform, false);
        var irt = icon.GetComponent<RectTransform>();
        irt.sizeDelta = new Vector2(60, 60);
        icon.GetComponent<Image>().sprite = iconA;
        var ile = icon.GetComponent<LayoutElement>();
        ile.preferredWidth = 60; ile.preferredHeight = 60; ile.flexibleWidth = 0; ile.flexibleHeight = 0;

        var textGroup = new GameObject("TextGroup", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        textGroup.transform.SetParent(left.transform, false);
        var tgl = textGroup.GetComponent<LayoutElement>();
        tgl.preferredWidth = 200;

        var vlg = textGroup.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        CreateText(textGroup.transform, "Name", title, 26, FontStyles.Bold, new Vector2(0, 0.5f));
        CreateText(textGroup.transform, "Desc", desc, 16, FontStyles.Normal, new Vector2(0, 0.5f));

        var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(row.transform, false);
        var sle = spacer.GetComponent<LayoutElement>();
        sle.preferredWidth = 20;
        sle.flexibleWidth = 0;

        var right = new GameObject("RightGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        right.transform.SetParent(row.transform, false);
        var rightLE = right.GetComponent<LayoutElement>();
        rightLE.preferredWidth = 220;

        var rightHLG = right.GetComponent<HorizontalLayoutGroup>();
        rightHLG.spacing = 6;
        rightHLG.childAlignment = TextAnchor.MiddleLeft;
        rightHLG.childControlWidth = true;
        rightHLG.childControlHeight = true;
        rightHLG.childForceExpandWidth = false;
        rightHLG.childForceExpandHeight = false;

        var cost = new GameObject("CostGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        cost.transform.SetParent(right.transform, false);
        var costLE = cost.GetComponent<LayoutElement>();
        costLE.preferredWidth = 120;

        var costHLG = cost.GetComponent<HorizontalLayoutGroup>();
        costHLG.spacing = 4;
        costHLG.childAlignment = TextAnchor.MiddleLeft;
        costHLG.childControlWidth = true;
        costHLG.childControlHeight = true;
        costHLG.childForceExpandWidth = false;
        costHLG.childForceExpandHeight = false;

        AddCost(cost.transform, iconA, costA);
        if (!string.IsNullOrEmpty(costB)) AddCost(cost.transform, iconB, costB);

        var btn = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        btn.transform.SetParent(right.transform, false);
        var brt = btn.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(150, 38);
        var bi = btn.GetComponent<Image>();
        if (btnSprite != null) bi.sprite = btnSprite;
        var ble = btn.GetComponent<LayoutElement>();
        ble.preferredWidth = 150; ble.preferredHeight = 38;
        var btnText = CreateText(btn.transform, "BtnText", upgrade ? "UPGRADE" : "BUILD", 16, FontStyles.Bold, new Vector2(0.5f, 0.5f));
        btnText.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 38);

        return row;
    }

    static void AddCost(Transform parent, Sprite icon, string amount)
    {
        var iconObj = new GameObject("CostIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconObj.transform.SetParent(parent, false);
        var irt = iconObj.GetComponent<RectTransform>();
        irt.sizeDelta = new Vector2(28, 28);
        iconObj.GetComponent<Image>().sprite = icon;
        var ile = iconObj.GetComponent<LayoutElement>();
        ile.preferredWidth = 28; ile.preferredHeight = 28;

        var text = CreateText(parent, "CostText", amount, 16, FontStyles.Normal, new Vector2(0, 0.5f));
        var trt = text.GetComponent<RectTransform>();
        trt.sizeDelta = new Vector2(32, 28);
    }

    static GameObject CreateText(Transform parent, string name, string text, int size, FontStyles style, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = new Color(0.95f, 0.9f, 0.82f, 1f);
        tmp.alignment = TextAlignmentOptions.Left;
        var rt = go.GetComponent<RectTransform>();
        rt.pivot = pivot;
        return go;
    }
}
