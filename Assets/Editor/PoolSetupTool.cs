using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ставить PoolRegistry у сцени й заповнює його префабами.
///
/// Префаби не вгадуються: інструмент читає поля GameManager і BattleManager
/// у кожній сцені, тож бере рівно те, що прив'язане в інспекторі.
///
/// Без реєстру код працює як раніше (Instantiate / Destroy), тож цей крок
/// нічого не ламає, якщо його не робити — він лише вмикає перевикористання.
/// </summary>
public static class PoolSetupTool
{
    private const string RegistryName = "PoolRegistry";
    private const string ManagersGroup = "--- Managers ---";

    /// <summary>Поле менеджера -> скільки створити наперед.</summary>
    private static readonly Dictionary<string, int> PooledFields = new Dictionary<string, int>
    {
        { "damagePopupPrefab", 32 },
        { "resourcePopupPrefab", 16 },
        { "bloodHitPrefab", 24 },
        { "volleyEffectPrefab", 4 },
        { "hitParticlePrefab", 24 },
        { "arrowPrefab", 48 },
        { "projectilePrefab", 16 },
        { "rainArrowPrefab", 24 },
        { "destroyEffect", 4 },
        { "miningEffect", 4 },
    };

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Pools/1. Перевірити пули", false, 800)]
    public static void Check()
    {
        var sb = new StringBuilder("=== ПУЛИ ===\n");

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            sb.AppendLine("Є незбережені зміни — збережи сцену й повтори.");
            Debug.Log(sb.ToString());
            return;
        }

        string openScene = EditorSceneManager.GetActiveScene().path;

        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.path) || !File.Exists(entry.path)) continue;

            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            sb.AppendLine();
            sb.AppendLine($"--- {scene.name}");

            var registry = FindRegistry(scene);
            var found = FindPooledPrefabs(scene);

            if (registry == null)
                sb.AppendLine("    PoolRegistry: немає");
            else
                sb.AppendLine($"    PoolRegistry: є, пулів налаштовано {registry.pools?.Length ?? 0}");

            if (found.Count == 0)
                sb.AppendLine("    префабів для пулу не знайдено");

            foreach (var (field, prefab) in found)
                sb.AppendLine($"      {field,-22} {prefab.name}");
        }

        if (!string.IsNullOrEmpty(openScene))
            EditorSceneManager.OpenScene(openScene, OpenSceneMode.Single);

        Debug.Log(sb.ToString());
    }

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Pools/2. Налаштувати пули", false, 801)]
    public static void Setup()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Налаштувати пули",
            "У кожну сцену додасться об'єкт PoolRegistry з префабами цифр шкоди,\n" +
            "крові та ефектів — узятими з полів GameManager і BattleManager.\n\n" +
            "Екземпляри створюються наперед і перевикористовуються замість\n" +
            "Instantiate/Destroy на кожне влучання.",
            "Налаштувати", "Скасувати");

        if (!ok) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var log = new StringBuilder("=== НАЛАШТОВАНО ПУЛИ ===\n");
        string openScene = EditorSceneManager.GetActiveScene().path;

        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.path) || !File.Exists(entry.path)) continue;

            log.AppendLine();
            log.AppendLine($"--- {Path.GetFileNameWithoutExtension(entry.path)}");

            try
            {
                var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                var found = FindPooledPrefabs(scene);

                if (found.Count == 0)
                {
                    log.AppendLine("    нема що пулити");
                    continue;
                }

                var registry = FindRegistry(scene) ?? CreateRegistry(scene, log);

                var entries = new List<PoolRegistry.Entry>();
                foreach (var (field, prefab) in found)
                {
                    if (entries.Any(e => e.prefab == prefab)) continue;

                    entries.Add(new PoolRegistry.Entry
                    {
                        prefab = prefab,
                        prewarm = PooledFields.TryGetValue(field, out int n) ? n : 8,
                        maxRetained = 128,
                    });

                    log.AppendLine($"    + {prefab.name}  (наперед {entries[entries.Count - 1].prewarm})");
                }

                registry.pools = entries.ToArray();
                EditorUtility.SetDirty(registry);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.AppendLine("    сцену збережено");
            }
            catch (System.Exception e)
            {
                log.AppendLine($"    [ПОМИЛКА] {e.GetType().Name}: {e.Message}");
                Debug.LogException(e);
            }
        }

        if (!string.IsNullOrEmpty(openScene))
            EditorSceneManager.OpenScene(openScene, OpenSceneMode.Single);

        Debug.Log(log.ToString());
    }

    // ==================================================================

    private static PoolRegistry FindRegistry(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var registry = root.GetComponentInChildren<PoolRegistry>(true);
            if (registry != null) return registry;
        }
        return null;
    }

    private static PoolRegistry CreateRegistry(Scene scene, StringBuilder log)
    {
        // Edit-time: об'єкт зберігається в сцену, у рантаймі нічого не створюється.
        var go = new GameObject(RegistryName);
        SceneManager.MoveGameObjectToScene(go, scene);

        // Кладемо до менеджерів, якщо така група вже є.
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != ManagersGroup) continue;
            go.transform.SetParent(root.transform, false);
            break;
        }

        log.AppendLine($"    + {RegistryName}");
        return go.AddComponent<PoolRegistry>();
    }

    /// <summary>
    /// Збирає префаби з полів менеджерів сцени — за іменем поля, а не
    /// за здогадкою про те, який префаб де лежить.
    /// </summary>
    private static List<(string field, GameObject prefab)> FindPooledPrefabs(Scene scene)
    {
        var result = new List<(string, GameObject)>();

        void Scan(MonoBehaviour mb)
        {
            if (mb == null) return;

            var so = new SerializedObject(mb);
            foreach (var field in PooledFields.Keys)
            {
                var prop = so.FindProperty(field);
                if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference) continue;

                var prefab = prop.objectReferenceValue as GameObject;
                if (prefab == null) continue;
                if (result.Any(r => r.Item2 == prefab)) continue;

                result.Add((field, prefab));
            }
        }

        // Об'єкти сцени.
        foreach (var root in scene.GetRootGameObjects())
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                Scan(mb);

        // І префаби проєкту: стріли та ефекти влучання прив'язані саме там,
        // на лучниках і вежах, а не на менеджерах у сцені.
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset == null) continue;

            foreach (var mb in asset.GetComponentsInChildren<MonoBehaviour>(true))
                Scan(mb);
        }

        return result;
    }
}
