using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Тіні для світових об'єктів.
///
/// У проєкті вже є своя конвенція тіні: дочірній об'єкт "Shadow" зі
/// SpriteRenderer — чорний спрайт з альфою ~0.39, сплющений у блін і зміщений
/// під ноги. Зараз вона стоїть лише на Knight, BallistaTower і Maincart.
/// Цей інструмент розносить ту саму конвенцію на решту, беручи спрайт і колір
/// з наявної тіні, щоб нічого не розʼїхалось візуально.
///
/// Окремо є додавання ShadowCaster2D — це тіні засобами URP. Вони працюють
/// лише від світла, у якого увімкнено Shadows. У сцені зараз одне глобальне
/// світло, тож самі по собі ShadowCaster2D нічого не покажуть.
///
/// Усе робиться в едіторі й зберігається в префаби.
/// </summary>
public static class ShadowSetupTool
{
    private const string ShadowChildName = "Shadow";
    private const string TemplatePrefab = "Assets/Objects/Prefabs/Knight.prefab";

    /// <summary>Ширина тіні відносно ширини спрайта об'єкта.</summary>
    private const float WidthFactor = 0.62f;

    /// <summary>Висота тіні відносно її ширини — та сама пропорція, що в Knight.</summary>
    private const float HeightRatio = 0.28f;

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Shadows/1. Показати кандидатів", false, 400)]
    public static void ListCandidates()
    {
        var template = LoadTemplate();
        var sb = new StringBuilder();
        sb.AppendLine("=== ТІНІ: КАНДИДАТИ ===");

        if (template == null)
        {
            sb.AppendLine($"[ПРОБЛЕМА] не знайдено зразок тіні у {TemplatePrefab}");
            sb.AppendLine("           потрібен дочірній об'єкт 'Shadow' зі SpriteRenderer");
            Debug.Log(sb.ToString());
            return;
        }

        sb.AppendLine($"Зразок узято з {TemplatePrefab}: спрайт '{template.Value.sprite.name}', " +
                      $"колір a={template.Value.color.a:0.00}");
        sb.AppendLine();

        var with = new List<string>();
        var without = new List<string>();

        foreach (var path in AllPrefabPaths())
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            if (prefab.GetComponent<SpriteRenderer>() == null) continue;

            if (prefab.transform.Find(ShadowChildName) != null) with.Add(path);
            else without.Add(path);
        }

        sb.AppendLine($"--- Уже мають тінь: {with.Count}");
        foreach (var p in with) sb.AppendLine("      " + p);

        sb.AppendLine();
        sb.AppendLine($"--- Без тіні: {without.Count}");
        foreach (var p in without) sb.AppendLine("      " + p);

        sb.AppendLine();
        sb.AppendLine("Познач потрібні префаби у вікні Project і запусти");
        sb.AppendLine("Shadows > 2. Додати тінь обраним.");
        sb.AppendLine("Снаряди, ефекти й UI тіні не потрібні — тому список не");
        sb.AppendLine("застосовується автоматично, вибір за тобою.");

        Debug.Log(sb.ToString());
    }

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Shadows/2. Додати тінь обраним", false, 401)]
    public static void AddShadowsToSelection()
    {
        var template = LoadTemplate();
        if (template == null)
        {
            EditorUtility.DisplayDialog("Тіні",
                $"Не знайдено зразок тіні у {TemplatePrefab}.\n" +
                "Потрібен дочірній об'єкт 'Shadow' зі SpriteRenderer.", "OK");
            return;
        }

        var paths = SelectedPrefabPaths();
        if (paths.Count == 0)
        {
            EditorUtility.DisplayDialog("Тіні",
                "Познач префаби у вікні Project.\n\n" +
                "Список кандидатів: Shadows > 1. Показати кандидатів.", "OK");
            return;
        }

        bool ok = EditorUtility.DisplayDialog(
            "Додати тінь",
            $"Обрано префабів: {paths.Count}\n\n" +
            "До кожного додасться дочірній 'Shadow' за зразком з Knight.\n" +
            "Розмір і зміщення рахуються з меж спрайта самого префаба.\n\n" +
            "Префаби, які вже мають 'Shadow', пропускаються.",
            "Додати", "Скасувати");

        if (!ok) return;

        var log = new StringBuilder("=== ДОДАНО ТІНІ ===\n");
        int added = 0, skipped = 0;

        foreach (var path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var existing = root.transform.Find(ShadowChildName);
                if (existing != null)
                {
                    // Тінь уже є, але її порядок міг лишитись не підв'язаним
                    // до YSorter — тоді вона провалюється під землю.
                    if (LinkShadowToSorter(root, existing.GetComponent<SpriteRenderer>()))
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        log.AppendLine($"  ~ тінь була, підв'язано до YSorter: {path}");
                        added++;
                    }
                    else
                    {
                        log.AppendLine($"  пропущено (вже є тінь): {path}");
                        skipped++;
                    }
                    continue;
                }

                var sr = root.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null)
                {
                    log.AppendLine($"  пропущено (немає спрайта в корені): {path}");
                    skipped++;
                    continue;
                }

                CreateShadow(root, sr, template.Value, log);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                added++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.Refresh();
        log.AppendLine($"\nДодано: {added}, пропущено: {skipped}.");
        Debug.Log(log.ToString());
    }

    private static void CreateShadow(GameObject root, SpriteRenderer ownerSr, ShadowTemplate template, StringBuilder log)
    {
        // Edit-time створення: об'єкт зберігається в префаб, у рантаймі нічого
        // не створюється.
        var shadow = new GameObject(ShadowChildName);
        shadow.transform.SetParent(root.transform, false);

        var sr = shadow.AddComponent<SpriteRenderer>();
        sr.sprite = template.sprite;
        sr.color = template.color;
        sr.sortingLayerID = ownerSr.sortingLayerID;
        sr.sortingOrder = ownerSr.sortingOrder - 1;

        // Ширина тіні — від ширини спрайта власника, у локальних одиницях.
        Bounds ownerBounds = ownerSr.sprite.bounds;
        float shadowNativeWidth = Mathf.Max(0.0001f, template.sprite.bounds.size.x);

        float targetWidth = ownerBounds.size.x * WidthFactor;
        float scaleX = targetWidth / shadowNativeWidth;
        float scaleY = scaleX * HeightRatio;

        shadow.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        // Під ноги: низ меж спрайта власника.
        shadow.transform.localPosition = new Vector3(ownerBounds.center.x, ownerBounds.min.y, 0f);

        bool linked = LinkShadowToSorter(root, sr);

        log.AppendLine($"  + тінь {targetWidth:0.00} ширини  {root.name}" +
                       (linked ? "  (підв'язано до YSorter)" : ""));
    }

    /// <summary>
    /// Віддає тінь під керування YSorter. Без цього тіло щокадру стрибає на
    /// ~5000, а тінь лишається на фіксованому порядку з префаба.
    /// Повертає true, якщо посилання справді проставили.
    /// </summary>
    private static bool LinkShadowToSorter(GameObject root, SpriteRenderer shadowRenderer)
    {
        if (shadowRenderer == null) return false;

        var sorter = root.GetComponent<YSorter>();
        if (sorter == null || sorter.shadowRenderer == shadowRenderer) return false;

        sorter.shadowRenderer = shadowRenderer;
        return true;
    }

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Shadows/3. Додати ShadowCaster2D обраним", false, 402)]
    public static void AddShadowCasters()
    {
        var paths = SelectedPrefabPaths();
        if (paths.Count == 0)
        {
            EditorUtility.DisplayDialog("ShadowCaster2D",
                "Познач префаби у вікні Project.", "OK");
            return;
        }

        bool ok = EditorUtility.DisplayDialog(
            "Додати ShadowCaster2D",
            $"Обрано префабів: {paths.Count}\n\n" +
            "УВАГА: тіні URP малюються лише від Light2D, у якого увімкнено\n" +
            "Shadows. У сцені зараз лише глобальне світло, тож видимого\n" +
            "результату може не бути, доки не з'явиться джерело світла\n" +
            "з позицією (Point або Freeform) і ввімкненими тінями.\n\n" +
            "Сам компонент нічого не ламає — він просто мовчить.",
            "Додати", "Скасувати");

        if (!ok) return;

        var log = new StringBuilder("=== ДОДАНО ShadowCaster2D ===\n");
        int added = 0, skipped = 0;

        foreach (var path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<ShadowCaster2D>() != null)
                {
                    log.AppendLine($"  пропущено (вже є): {path}");
                    skipped++;
                    continue;
                }

                if (root.GetComponent<SpriteRenderer>() == null)
                {
                    log.AppendLine($"  пропущено (немає SpriteRenderer): {path}");
                    skipped++;
                    continue;
                }

                // Додаємо з типовими налаштуваннями — так само, як це робить
                // інспектор: форму URP бере зі спрайта.
                root.AddComponent<ShadowCaster2D>();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                log.AppendLine($"  + {root.name}");
                added++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.Refresh();
        log.AppendLine($"\nДодано: {added}, пропущено: {skipped}.");
        log.AppendLine("Щоб побачити результат, постав у сцену Light2D типу Point");
        log.AppendLine("або Freeform і ввімкни в нього Shadows.");
        Debug.Log(log.ToString());
    }

    // ==================================================================

    private struct ShadowTemplate
    {
        public Sprite sprite;
        public Color color;
    }

    /// <summary>
    /// Беремо спрайт і колір тіні з наявного префаба, щоб нові тіні
    /// виглядали так само, як ті три, що вже є.
    /// </summary>
    private static ShadowTemplate? LoadTemplate()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefab);
        if (prefab == null) return null;

        var shadow = prefab.transform.Find(ShadowChildName);
        if (shadow == null) return null;

        var sr = shadow.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return null;

        return new ShadowTemplate { sprite = sr.sprite, color = sr.color };
    }

    private static IEnumerable<string> AllPrefabPaths()
    {
        return AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p);
    }

    private static List<string> SelectedPrefabPaths()
    {
        return Selection.objects
            .Select(AssetDatabase.GetAssetPath)
            .Where(p => !string.IsNullOrEmpty(p) && p.EndsWith(".prefab"))
            .Distinct()
            .ToList();
    }
}
