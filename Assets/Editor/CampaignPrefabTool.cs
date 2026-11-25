using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Готує префаби, які використовує бій за аванпости.
///
/// Навіщо: BattleManager раніше на кожному заспавненому юніті зривав скрипти
/// через DestroyImmediate і додавав CampaignUnit у рантаймі. Тепер CampaignUnit
/// має лежати на префабі заздалегідь — як і вимагають правила проєкту.
///
/// Інструмент не вгадує, які це префаби: він читає поля самого BattleManager
/// у сцені SiegeBattleScene, тож бере рівно те, що прив'язане в інспекторі.
/// </summary>
public static class CampaignPrefabTool
{
    private const string BattleScenePath = "Assets/Scenes/SiegeBattleScene.unity";

    private static readonly string[] PrefabFields =
    {
        "playerKnightPrefab", "playerArcherPrefab", "playerSpearmanPrefab", "playerCavalryPrefab",
        "mercKnightPrefab", "mercArcherPrefab", "mercSpearmanPrefab", "mercCavalryPrefab",
        "enemyGuardPrefab", "enemyArcherPrefab", "enemySpearmanPrefab", "enemyCavalryPrefab",
    };

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Campaign/1. Перевірити префаби бою", false, 500)]
    public static void Check()
    {
        var found = CollectPrefabs(out string error);
        var sb = new StringBuilder();
        sb.AppendLine("=== ПРЕФАБИ БОЮ ЗА АВАНПОСТИ ===");

        if (error != null)
        {
            sb.AppendLine($"[ПРОБЛЕМА] {error}");
            Debug.Log(sb.ToString());
            return;
        }

        var missing = new List<string>();

        foreach (var (field, path) in found)
        {
            if (path == null)
            {
                sb.AppendLine($"  [не прив'язано] {field}");
                continue;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            bool has = prefab != null && prefab.GetComponent<CampaignUnit>() != null;

            sb.AppendLine($"  {(has ? "OK  " : "НЕМА")} {field,-22} {path}");
            if (!has && path != null && !missing.Contains(path)) missing.Add(path);
        }

        sb.AppendLine();
        if (missing.Count == 0)
        {
            sb.AppendLine("Усі прив'язані префаби мають CampaignUnit — у рантаймі нічого не додається.");
        }
        else
        {
            sb.AppendLine($"Без CampaignUnit: {missing.Count} префабів.");
            sb.AppendLine("Запусти Campaign > 2. Додати CampaignUnit на префаби бою.");
        }

        Debug.Log(sb.ToString());
    }

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Campaign/2. Додати CampaignUnit на префаби бою", false, 501)]
    public static void AddCampaignUnit()
    {
        var found = CollectPrefabs(out string error);

        if (error != null)
        {
            EditorUtility.DisplayDialog("Префаби бою", error, "OK");
            return;
        }

        var paths = found
            .Select(f => f.path)
            .Where(p => p != null)
            .Distinct()
            .Where(p =>
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                return prefab != null && prefab.GetComponent<CampaignUnit>() == null;
            })
            .ToList();

        if (paths.Count == 0)
        {
            EditorUtility.DisplayDialog("Префаби бою",
                "Усі прив'язані префаби вже мають CampaignUnit.", "OK");
            return;
        }

        bool ok = EditorUtility.DisplayDialog(
            "Додати CampaignUnit",
            $"Префабів: {paths.Count}\n\n" +
            string.Join("\n", paths.Select(System.IO.Path.GetFileNameWithoutExtension)) +
            "\n\nСкрипти AI оборони на них лишаються, але CampaignUnit вимикає\n" +
            "їх сам у Setup() — нічого не знищується.",
            "Додати", "Скасувати");

        if (!ok) return;

        var log = new StringBuilder("=== ДОДАНО CampaignUnit ===\n");
        int added = 0;

        foreach (var path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<CampaignUnit>() != null)
                {
                    log.AppendLine($"  вже було: {path}");
                    continue;
                }

                root.AddComponent<CampaignUnit>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                log.AppendLine($"  + {path}");
                added++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.Refresh();
        log.AppendLine($"\nДодано: {added}.");
        Debug.Log(log.ToString());
    }

    // ==================================================================

    /// <summary>
    /// Читає поля префабів із BattleManager у сцені бою.
    /// Повертає пари (назва поля, шлях до префаба або null).
    /// </summary>
    private static List<(string field, string path)> CollectPrefabs(out string error)
    {
        error = null;
        var result = new List<(string, string)>();

        if (!System.IO.File.Exists(BattleScenePath))
        {
            error = $"Не знайдено сцену {BattleScenePath}";
            return result;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            error = "Є незбережені зміни — збережи сцену й повтори.";
            return result;
        }

        string openScenePath = EditorSceneManager.GetActiveScene().path;

        try
        {
            var scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);

            BattleManager manager = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                manager = root.GetComponentInChildren<BattleManager>(true);
                if (manager != null) break;
            }

            if (manager == null)
            {
                error = $"У {BattleScenePath} немає BattleManager";
                return result;
            }

            var so = new SerializedObject(manager);
            foreach (var field in PrefabFields)
            {
                var prop = so.FindProperty(field);
                var obj = prop != null ? prop.objectReferenceValue : null;
                string path = obj != null ? AssetDatabase.GetAssetPath(obj) : null;
                result.Add((field, string.IsNullOrEmpty(path) ? null : path));
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(openScenePath))
                EditorSceneManager.OpenScene(openScenePath, OpenSceneMode.Single);
        }

        return result;
    }
}
