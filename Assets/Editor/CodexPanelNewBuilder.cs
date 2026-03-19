#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Створює готовий префаб CodexPanel_New.prefab з повною ієрархією CodexPanel по промту.
/// Після патча: Tools → Build CodexPanel_New Prefab
/// </summary>
public static class CodexPanelNewBuilder
{
    [MenuItem("Tools/Build CodexPanel_New Prefab")]
    public static void BuildCodexPanelPrefab()
    {
        // Кореневий об'єкт префаба
        GameObject root = new GameObject("CodexPanel", typeof(RectTransform), typeof(Image), typeof(CodexManager));
        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.sizeDelta = new Vector2(900f, 600f);
        rootRT.anchoredPosition = Vector2.zero;

        Image rootImg = root.GetComponent<Image>();
        rootImg.color = Color.white;

        CodexManager codex = root.GetComponent<CodexManager>();
        // CodexManager очікує GameObject для codexPanel
        codex.codexPanel = root;

        // 1) CodexContentPanel (пергамент усередині)
        GameObject contentGO = CreateUIObj("CodexContentPanel", rootRT);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0.1f, 0.1f);
        contentRT.anchorMax = new Vector2(0.9f, 0.9f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        Image contentImg = contentGO.AddComponent<Image>();
        contentImg.color = new Color(0.93f, 0.84f, 0.66f, 1f);
        codex.codexContentPanel = contentRT;

        // CloseButton у правому верхньому куті CodexPanel
        GameObject closeBtnGO = CreateUIObj("CloseButton", rootRT);
        RectTransform closeRT = closeBtnGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1f, 1f);
        closeRT.anchorMax = new Vector2(1f, 1f);
        closeRT.pivot = new Vector2(1f, 1f);
        closeRT.sizeDelta = new Vector2(40, 40);
        closeRT.anchoredPosition = new Vector2(-15, -15);
        Image closeImg = closeBtnGO.AddComponent<Image>();
        closeImg.color = Color.white;
        Button closeBtn = closeBtnGO.AddComponent<Button>();

        // 2) LeftColumn
        GameObject leftColGO = CreateUIObj("LeftColumn", contentRT);
        RectTransform leftRT = leftColGO.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0f, 0f);
        // трохи ширше, як у мокапі (≈35%)
        leftRT.anchorMax = new Vector2(0.35f, 1f);
        leftRT.offsetMin = Vector2.zero;
        leftRT.offsetMax = Vector2.zero;

        // TabsContainer
        GameObject tabsGO = CreateUIObj("TabsContainer", leftRT);
        RectTransform tabsRT = tabsGO.GetComponent<RectTransform>();
        tabsRT.anchorMin = new Vector2(0f, 1f);
        tabsRT.anchorMax = new Vector2(1f, 1f);
        tabsRT.pivot = new Vector2(0.5f, 1f);
        tabsRT.sizeDelta = new Vector2(0f, 40f);
        tabsRT.anchoredPosition = new Vector2(0f, -10f);
        HorizontalLayoutGroup tabsHLG = tabsGO.AddComponent<HorizontalLayoutGroup>();
        tabsHLG.childControlWidth = true;
        tabsHLG.childControlHeight = true;
        tabsHLG.childForceExpandWidth = true;
        tabsHLG.childForceExpandHeight = true;
        tabsHLG.spacing = 5f;
        tabsHLG.padding = new RectOffset(5, 5, 0, 0);

        Button unitsBtn = CreateTabButton(tabsGO.transform, "UnitsTabBtn", "Units");
        Button buildingsBtn = CreateTabButton(tabsGO.transform, "BuildingsTabBtn", "Buildings");
        Button enemiesBtn = CreateTabButton(tabsGO.transform, "EnemiesTabBtn", "Enemies");
        codex.unitsTabBtn = unitsBtn;
        codex.buildingsTabBtn = buildingsBtn;
        codex.enemiesTabBtn = enemiesBtn;

        // Scroll View
        GameObject scrollGO = CreateUIObj("Scroll View", leftRT);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0f, 0f);
        scrollRT.anchorMax = new Vector2(1f, 1f);
        scrollRT.offsetMin = new Vector2(10f, 10f);
        scrollRT.offsetMax = new Vector2(-10f, -60f);
        Image scrollImg = scrollGO.AddComponent<Image>();
        scrollImg.color = new Color(0f, 0f, 0f, 0.15f);
        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();

        GameObject viewportGO = CreateUIObj("Viewport", scrollRT);
        RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        Image viewportImg = viewportGO.AddComponent<Image>();
        viewportImg.color = new Color(1f, 1f, 1f, 0.01f);
        Mask viewportMask = viewportGO.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject entryListGO = CreateUIObj("EntryListContainer", viewportRT);
        RectTransform entryListRT = entryListGO.GetComponent<RectTransform>();
        entryListRT.anchorMin = new Vector2(0f, 1f);
        entryListRT.anchorMax = new Vector2(1f, 1f);
        entryListRT.pivot = new Vector2(0.5f, 1f);
        entryListRT.offsetMin = Vector2.zero;
        entryListRT.offsetMax = Vector2.zero;
        VerticalLayoutGroup vlg = entryListGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        ContentSizeFitter csf = entryListGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.viewport = viewportRT;
        scrollRect.content = entryListRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        codex.entryListContainer = entryListRT;

        // 3) RightColumn
        GameObject rightColGO = CreateUIObj("RightColumn", contentRT);
        RectTransform rightRT = rightColGO.GetComponent<RectTransform>();
        // права колонка займає решту (від 35%)
        rightRT.anchorMin = new Vector2(0.35f, 0f);
        rightRT.anchorMax = new Vector2(1f, 1f);
        rightRT.offsetMin = Vector2.zero;
        rightRT.offsetMax = Vector2.zero;

        // Title
        TextMeshProUGUI title = CreateTMP("TitleText", rightRT);
        RectTransform titleRT = title.rectTransform;
        // заголовок вирівняний ліворуч, як у мокапі
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0f, 1f);
        titleRT.sizeDelta = new Vector2(0f, 36f);
        titleRT.anchoredPosition = new Vector2(20f, -20f);
        title.alignment = TextAlignmentOptions.Left;
        title.fontSize = 28f;
        title.text = "Unit Name";
        codex.titleText = title;

        // Subtitle
        TextMeshProUGUI subtitle = CreateTMP("SubtitleText", rightRT);
        RectTransform subtitleRT = subtitle.rectTransform;
        subtitleRT.anchorMin = new Vector2(0f, 1f);
        subtitleRT.anchorMax = new Vector2(1f, 1f);
        subtitleRT.pivot = new Vector2(0f, 1f);
        subtitleRT.sizeDelta = new Vector2(0f, 24f);
        subtitleRT.anchoredPosition = new Vector2(20f, -56f);
        subtitle.alignment = TextAlignmentOptions.Left;
        subtitle.fontSize = 20f;
        subtitle.text = "Role / Faction";
        codex.subtitleText = subtitle;

        // HeaderContainer (портрет + стати)
        GameObject headerGO = CreateUIObj("HeaderContainer", rightRT);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0f, 1f);
        headerRT.sizeDelta = new Vector2(0f, 180f);
        headerRT.anchoredPosition = new Vector2(20f, -90f);
        HorizontalLayoutGroup headerHLG = headerGO.AddComponent<HorizontalLayoutGroup>();
        headerHLG.spacing = 10f;
        headerHLG.padding = new RectOffset(10, 10, 10, 10);
        headerHLG.childControlWidth = false;
        headerHLG.childControlHeight = true;
        headerHLG.childForceExpandWidth = false;
        headerHLG.childForceExpandHeight = true;

        // PortraitImage
        GameObject portraitGO = CreateUIObj("PortraitImage", headerRT);
        RectTransform portraitRT = portraitGO.GetComponent<RectTransform>();
        portraitRT.sizeDelta = new Vector2(150f, 150f);
        Image portraitImg = portraitGO.AddComponent<Image>();
        portraitImg.color = new Color(0f, 0f, 0f, 0.25f);
        LayoutElement portraitLE = portraitGO.AddComponent<LayoutElement>();
        portraitLE.minWidth = 150f;
        portraitLE.minHeight = 150f;
        portraitLE.preferredWidth = 150f;
        portraitLE.preferredHeight = 150f;
        codex.portraitImage = portraitImg;

        // StatsContainer
        GameObject statsGO = CreateUIObj("StatsContainer", headerRT);
        VerticalLayoutGroup statsVLG = statsGO.AddComponent<VerticalLayoutGroup>();
        statsVLG.spacing = 4f;
        statsVLG.childControlWidth = true;
        statsVLG.childControlHeight = true;
        statsVLG.childForceExpandWidth = true;
        statsVLG.childForceExpandHeight = false;

        TextMeshProUGUI hp = CreateTMP("HPText", statsGO.transform);
        hp.alignment = TextAlignmentOptions.Left;
        hp.fontSize = 20f;
        hp.text = "HP: 100";
        codex.hpText = hp;

        TextMeshProUGUI dmg = CreateTMP("DMGText", statsGO.transform);
        dmg.alignment = TextAlignmentOptions.Left;
        dmg.fontSize = 20f;
        dmg.text = "DMG: 10";
        codex.dmgText = dmg;

        // DescriptionText (середина)
        TextMeshProUGUI desc = CreateTMP("DescriptionText", rightRT);
        RectTransform descRT = desc.rectTransform;
        // опис займає праву частину під статами
        descRT.anchorMin = new Vector2(0f, 0.32f);
        descRT.anchorMax = new Vector2(1f, 0.82f);
        descRT.offsetMin = new Vector2(20f, 0f);
        descRT.offsetMax = new Vector2(-20f, 0f);
        desc.alignment = TextAlignmentOptions.TopLeft;
        desc.enableWordWrapping = true;
        desc.text = "Short description of the selected entry.";
        codex.descriptionText = desc;

        // TacticsContainer (низ)
        GameObject tacticsGO = CreateUIObj("TacticsContainer", rightRT);
        RectTransform tacticsRT = tacticsGO.GetComponent<RectTransform>();
        tacticsRT.anchorMin = new Vector2(0f, 0f);
        tacticsRT.anchorMax = new Vector2(1f, 0.28f);
        tacticsRT.offsetMin = new Vector2(20f, 10f);
        tacticsRT.offsetMax = new Vector2(-20f, 10f);
        HorizontalLayoutGroup tacticsHLG = tacticsGO.AddComponent<HorizontalLayoutGroup>();
        tacticsHLG.spacing = 10f;
        tacticsHLG.childControlWidth = true;
        tacticsHLG.childControlHeight = true;
        tacticsHLG.childForceExpandWidth = true;
        tacticsHLG.childForceExpandHeight = true;

        TextMeshProUGUI pros = CreateTMP("ProsText", tacticsGO.transform);
        pros.richText = true;
        pros.enableWordWrapping = true;
        pros.alignment = TextAlignmentOptions.TopLeft;
        pros.text = "<b>Pros:</b> durable, good vs cavalry";
        codex.prosText = pros;

        TextMeshProUGUI cons = CreateTMP("ConsText", tacticsGO.transform);
        cons.richText = true;
        cons.enableWordWrapping = true;
        cons.alignment = TextAlignmentOptions.TopLeft;
        cons.text = "<b>Cons:</b> slow, weak vs archers";
        codex.consText = cons;

        // Префаб кнопки CodexEntryBtn (вже є в Assets/Prefabs)
        codex.entryButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CodexEntryBtn.prefab");
        if (codex.entryButtonPrefab == null)
        {
            Debug.LogWarning("CodexPanelNewBuilder: Assets/Prefabs/CodexEntryBtn.prefab не знайдено. Перевір шлях або створи префаб вручну.");
        }

        // Зберігаємо префаб
        string path = "Assets/Panels/CodexPanel_New.prefab";
        System.IO.Directory.CreateDirectory("Assets/Panels");
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log("CodexPanelNewBuilder: створено/оновлено префаб " + path);
    }

    private static GameObject CreateUIObj(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI CreateTMP(string name, Transform parent)
    {
        GameObject go = CreateUIObj(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = name;
        return tmp;
    }

    private static Button CreateTabButton(Transform parent, string name, string label)
    {
        GameObject go = CreateUIObj(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.15f, 0.05f, 0.9f);
        Button btn = go.AddComponent<Button>();
        TextMeshProUGUI txt = CreateTMP("Label", go.transform);
        txt.text = label;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 18f;
        return btn;
    }
}
#endif
