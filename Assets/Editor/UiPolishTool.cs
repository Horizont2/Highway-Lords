using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Аудит і дрібний ремонт інтерфейсу.
///
/// Перший пункт лише звітує: текст, що вилазить за свою рамку, плейсхолдери,
/// які забули замінити, порожні посилання та тексти в чужій розкладці.
///
/// Другий додає CanvasGroup панелям, яким його бракує. Раніше код створював
/// цей компонент у рантаймі через AddComponent — проти правил проєкту й з
/// ризиком, що перший показ панелі буде без плавності. Тепер компонент
/// кладеться заздалегідь, а код його лише читає.
/// </summary>
public static class UiPolishTool
{
    private static readonly string[] PanelNameHints = { "panel", "popup", "window", "dialog", "confirm" };

    // "Line 1".."Line 4" свідомо НЕ тут: BattleManager.UpdateRowUI формує
    // з них підпис "Line 1\nKnights", тобто це робоче значення, а не забутий
    // плейсхолдер.
    private static readonly string[] PlaceholderTexts =
    {
        "new text", "text (tmp)", "sample", "lorem", "todo", "label", "???", "xxx",
    };

    // ==================================================================

    [MenuItem("Tools/Highway Lords/UI/1. Аудит інтерфейсу", false, 900)]
    public static void Audit()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var sb = new StringBuilder("=== АУДИТ ІНТЕРФЕЙСУ ===\n");
        string openScene = EditorSceneManager.GetActiveScene().path;

        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.path) || !File.Exists(entry.path)) continue;

            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            sb.AppendLine();
            sb.AppendLine($"--- {scene.name}");

            AuditTexts(scene, sb);
            AuditPanels(scene, sb);
            AuditReferences(scene, sb);
        }

        if (!string.IsNullOrEmpty(openScene))
            EditorSceneManager.OpenScene(openScene, OpenSceneMode.Single);

        Debug.Log(sb.ToString());
    }

    private static void AuditTexts(Scene scene, StringBuilder sb)
    {
        var overflowing = new List<string>();
        var placeholders = new List<string>();
        var mixedScript = new List<string>();
        var emptyStatic = new List<string>();
        int total = 0;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                total++;
                string path = PathOf(text.transform);
                string value = text.text ?? "";

                // Рахуємо лише переповнення ПО ШИРИНІ: саме воно призводить
                // до налізання на сусідні елементи.
                //
                // По висоті не рахуємо навмисно: GetPreferredValues повертає
                // висоту з урахуванням інтерліньяжу й виносних елементів, тож
                // однорядковий підпис майже завжди "не вміщається" на кілька
                // пікселів, нічим це не проявляючи. Перша версія цієї перевірки
                // саме через це видала 49 попереджень, з яких жодне не було
                // справжньою проблемою.
                var rect = text.rectTransform.rect;
                if (rect.width > 1f && !string.IsNullOrEmpty(value))
                {
                    Vector2 needed = text.GetPreferredValues(value, 0f, 0f);
                    if (needed.x > rect.width + 2f)
                    {
                        overflowing.Add($"{path}  ширина рамки {rect.width:0}, " +
                                        $"тексту треба {needed.x:0}  {Short(value)}");
                    }
                }

                if (PlaceholderTexts.Contains(value.Trim().ToLowerInvariant()))
                    placeholders.Add($"{path}  = {Short(value)}");

                bool hasCyr = value.Any(c => c >= 'Ѐ' && c <= 'ӿ');
                bool hasLat = value.Any(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));
                if (hasCyr && hasLat)
                    mixedScript.Add($"{path}  = {Short(value)}");

                if (string.IsNullOrWhiteSpace(value))
                    emptyStatic.Add(path);
            }
        }

        sb.AppendLine($"    текстів: {total}");
        Report(sb, "текст не вміщається у свою рамку", overflowing, 12);
        Report(sb, "плейсхолдери, які забули замінити", placeholders, 12);
        Report(sb, "мішані алфавіти (латиниця + кирилиця в одному слові)", mixedScript, 12);
        Report(sb, "порожній текст (заповнюється кодом — перевір, чи точно)", emptyStatic, 8);
    }

    private static void AuditPanels(Scene scene, StringBuilder sb)
    {
        var missing = new List<string>();

        foreach (var go in PanelCandidates(scene))
            if (go.GetComponent<CanvasGroup>() == null)
                missing.Add(PathOf(go.transform));

        Report(sb, "панелі без CanvasGroup (відкриються без плавності)", missing, 20);
    }

    private static void AuditReferences(Scene scene, StringBuilder sb)
    {
        var empty = new List<string>();

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;

                var so = new SerializedObject(mb);
                var prop = so.GetIterator();
                bool enterChildren = true;

                while (prop.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (prop.objectReferenceValue != null) continue;

                    // Поля з префіксом m_ — внутрішні для Unity й TMP. Саме
                    // через них перша версія видала 253 "порожніх посилання":
                    // майже кожен TMP_Text має незаповнений
                    // m_linkedTextComponent, і це цілком нормально.
                    if (prop.name.StartsWith("m_")) continue;

                    // Порожнє посилання саме по собі нормальне; цікавлять
                    // лише ті, що явно щось мають тримати.
                    string n = prop.name.ToLowerInvariant();
                    if (n.Contains("text") || n.Contains("button") || n.Contains("panel") || n.Contains("prefab"))
                        empty.Add($"{PathOf(mb.transform)} :: {mb.GetType().Name}.{prop.name}");
                }
            }
        }

        Report(sb, "порожні посилання на текст / кнопку / панель / префаб", empty, 25);
    }

    // ==================================================================

    [MenuItem("Tools/Highway Lords/UI/2. Додати CanvasGroup панелям", false, 901)]
    public static void AddCanvasGroups()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var log = new StringBuilder("=== CanvasGroup ДЛЯ ПАНЕЛЕЙ ===\n");
        string openScene = EditorSceneManager.GetActiveScene().path;
        int added = 0;

        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.path) || !File.Exists(entry.path)) continue;

            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            log.AppendLine();
            log.AppendLine($"--- {scene.name}");

            bool changed = false;
            foreach (var go in PanelCandidates(scene))
            {
                if (go.GetComponent<CanvasGroup>() != null) continue;

                // Edit-time: компонент лягає в сцену, у рантаймі нічого
                // не створюється. Типові значення нічого не міняють.
                go.AddComponent<CanvasGroup>();
                log.AppendLine($"    + {PathOf(go.transform)}");
                added++;
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.AppendLine("    сцену збережено");
            }
            else log.AppendLine("    усе вже на місці");
        }

        if (!string.IsNullOrEmpty(openScene))
            EditorSceneManager.OpenScene(openScene, OpenSceneMode.Single);

        log.AppendLine($"\nДодано: {added}.");
        Debug.Log(log.ToString());
    }

    // ==================================================================

    [MenuItem("Tools/Highway Lords/UI/3. Додати відгук натискання кнопкам", false, 902)]
    public static void AddButtonFeedback()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var log = new StringBuilder("=== ВІДГУК НАТИСКАННЯ ===\n");
        string openScene = EditorSceneManager.GetActiveScene().path;
        int added = 0, skipped = 0;

        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.path) || !File.Exists(entry.path)) continue;

            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            log.AppendLine();
            log.AppendLine($"--- {scene.name}");

            bool changed = false;
            int here = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    if (button.GetComponent<ButtonPressFeedback>() != null) { skipped++; continue; }

                    // Edit-time: компонент лягає в сцену.
                    button.gameObject.AddComponent<ButtonPressFeedback>();
                    added++; here++; changed = true;
                }
            }

            log.AppendLine($"    додано: {here}");

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.AppendLine("    сцену збережено");
            }
        }

        if (!string.IsNullOrEmpty(openScene))
            EditorSceneManager.OpenScene(openScene, OpenSceneMode.Single);

        // Кнопки всередині префабів панелей теж треба охопити — частина
        // інтерфейсу живе саме там, а не в сцені.
        int inPrefabs = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool touched = false;
                foreach (var button in contents.GetComponentsInChildren<Button>(true))
                {
                    if (button.GetComponent<ButtonPressFeedback>() != null) continue;
                    button.gameObject.AddComponent<ButtonPressFeedback>();
                    inPrefabs++; touched = true;
                }

                if (touched)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    log.AppendLine($"    префаб: {Path.GetFileNameWithoutExtension(path)}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        AssetDatabase.Refresh();
        log.AppendLine($"\nДодано у сценах: {added}, у префабах: {inPrefabs}, вже було: {skipped}.");
        Debug.Log(log.ToString());
    }

    // ==================================================================

    /// <summary>
    /// Що вважаємо панеллю: або ім'я натякає, або на об'єкт посилається
    /// поле з "panel" у назві. Другий шлях надійніший — він бере саме ті
    /// об'єкти, які код показує й ховає.
    /// </summary>
    private static IEnumerable<GameObject> PanelCandidates(Scene scene)
    {
        var result = new HashSet<GameObject>();

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                string lower = rect.gameObject.name.ToLowerInvariant();
                if (PanelNameHints.Any(h => lower.Contains(h))) result.Add(rect.gameObject);
            }

            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;

                var so = new SerializedObject(mb);
                var prop = so.GetIterator();
                bool enterChildren = true;

                while (prop.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (!prop.name.ToLowerInvariant().Contains("panel")) continue;

                    var obj = prop.objectReferenceValue;
                    if (obj is GameObject go && go.scene == scene) result.Add(go);
                    else if (obj is Component comp && comp != null && comp.gameObject.scene == scene)
                        result.Add(comp.gameObject);
                }
            }
        }

        return result;
    }

    private static void Report(StringBuilder sb, string title, List<string> items, int limit)
    {
        if (items.Count == 0) return;

        sb.AppendLine($"    [{items.Count}] {title}:");
        foreach (var i in items.Take(limit)) sb.AppendLine($"        {i}");
        if (items.Count > limit) sb.AppendLine($"        ... і ще {items.Count - limit}");
    }

    private static string Short(string value)
    {
        value = value.Replace('\n', ' ');
        return value.Length > 40 ? $"\"{value.Substring(0, 40)}…\"" : $"\"{value}\"";
    }

    private static string PathOf(Transform t)
    {
        var parts = new List<string>();
        while (t != null && parts.Count < 5)
        {
            parts.Insert(0, t.name);
            t = t.parent;
        }
        return string.Join("/", parts);
    }
}
