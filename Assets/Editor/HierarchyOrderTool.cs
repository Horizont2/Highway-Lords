using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Групує кореневі об'єкти сцен за призначенням.
///
/// У Main було 25 кореневих об'єктів одним списком: менеджери, точки спавну,
/// межі, земля й UI упереміш.
///
/// Що НЕ чіпається, свідомо:
///   * Canvas — вкладений Canvas поводиться інакше, а CanvasScaler діє лише
///     на кореневому;
///   * об'єкти з DontDestroyOnLoad (SoundManager, LoadingManager) — цей виклик
///     працює тільки на кореневих об'єктах, інакше Unity кидає помилку
///     й об'єкт гине при зміні сцени;
///   * камери та EventSystem — на них зав'язано надто багато.
///
/// Діти Canvas теж не чіпаються: порядок малювання UI — це порядок в
/// ієрархії, тож перегрупування там змінило б те, що поверх чого лежить.
/// </summary>
public static class HierarchyOrderTool
{
    private const string GroupManagers = "--- Managers ---";
    private const string GroupSpawnPoints = "--- Spawn Points ---";
    private const string GroupEnvironment = "--- Environment ---";

    /// <summary>Об'єкти, які мають лишитися в корені (за іменем).</summary>
    private static readonly HashSet<string> KeepAtRoot = new HashSet<string>
    {
        "SoundManager", "LoadingManager",
    };

    private static readonly string[] SpawnPointNames =
    {
        "MineSpawnPoint", "SpikesSpawnPoint", "BarracksSpawnPoint", "CartSpawnPoint",
        "PlayerSpawnPoint", "EnemySpawnPoint",
        "FormationStart", "LimitLeft", "LimitRight", "Engagement_Line",
    };

    private static readonly string[] EnvironmentNames =
    {
        "Ground", "Map", "Wall", "Square", "BuilderBase",
    };

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Hierarchy/1. План перегрупування", false, 600)]
    public static void Preview()
    {
        Run(apply: false);
    }

    [MenuItem("Tools/Highway Lords/Hierarchy/2. Перегрупувати сцени", false, 601)]
    public static void Apply()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Перегрупувати ієрархію",
            "Кореневі об'єкти сцен розкладуться по групах Managers / Spawn Points / Environment.\n\n" +
            "Canvas, камери, EventSystem і об'єкти з DontDestroyOnLoad лишаються\n" +
            "в корені — їх переносити не можна.\n\n" +
            "Світові позиції зберігаються. Спершу подивись план (пункт 1).",
            "Перегрупувати", "Скасувати");

        if (ok) Run(apply: true);
    }

    // ==================================================================

    private static void Run(bool apply)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var log = new StringBuilder(apply ? "=== ПЕРЕГРУПОВАНО ===\n" : "=== ПЛАН ПЕРЕГРУПУВАННЯ ===\n");
        string openScenePath = EditorSceneManager.GetActiveScene().path;

        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.path)) continue;
            if (!File.Exists(entry.path)) continue;

            log.AppendLine();
            log.AppendLine($"--- {Path.GetFileNameWithoutExtension(entry.path)}");

            try
            {
                var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                bool changed = ProcessScene(scene, apply, log);

                if (apply && changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    log.AppendLine("    сцену збережено");
                }
            }
            catch (System.Exception e)
            {
                log.AppendLine($"    [ПОМИЛКА] {e.GetType().Name}: {e.Message}");
                Debug.LogException(e);
            }
        }

        if (!string.IsNullOrEmpty(openScenePath))
            EditorSceneManager.OpenScene(openScenePath, OpenSceneMode.Single);

        if (!apply)
            log.AppendLine("\nЦе лише план. Застосувати: Hierarchy > 2. Перегрупувати сцени.");

        Debug.Log(log.ToString());
    }

    private static bool ProcessScene(Scene scene, bool apply, StringBuilder log)
    {
        var plan = new List<(GameObject obj, string group)>();
        var kept = new List<string>();
        var duplicates = new List<string>();

        var roots = scene.GetRootGameObjects();
        var seenNames = new HashSet<string>();

        foreach (var root in roots)
        {
            if (!seenNames.Add(root.name)) duplicates.Add(root.name);

            // Самі групи пропускаємо, інакше вони поїдуть одна в одну.
            if (IsGroup(root.name)) continue;

            string reason = WhyKeepAtRoot(root);
            if (reason != null)
            {
                kept.Add($"{root.name}  ({reason})");
                continue;
            }

            string group = Classify(root);
            if (group == null)
            {
                kept.Add($"{root.name}  (не класифіковано)");
                continue;
            }

            plan.Add((root, group));
        }

        foreach (var name in duplicates.Distinct())
            log.AppendLine($"    [УВАГА] кілька кореневих об'єктів з іменем '{name}'");

        if (plan.Count == 0)
        {
            log.AppendLine("    нема що переносити");
            foreach (var k in kept) log.AppendLine($"      лишається: {k}");
            return false;
        }

        foreach (var group in plan.Select(p => p.group).Distinct())
        {
            var members = plan.Where(p => p.group == group).Select(p => p.obj.name);
            log.AppendLine($"    {group}");
            foreach (var m in members) log.AppendLine($"      - {m}");
        }

        foreach (var k in kept) log.AppendLine($"      лишається в корені: {k}");

        if (!apply) return false;

        foreach (var (obj, groupName) in plan)
        {
            var group = FindOrCreateGroup(scene, groupName);
            // worldPositionStays: позиції в світі не зсуваються.
            obj.transform.SetParent(group.transform, true);
        }

        return true;
    }

    // ==================================================================

    private static bool IsGroup(string name) =>
        name == GroupManagers || name == GroupSpawnPoints || name == GroupEnvironment
        || name == "Rendering";

    /// <summary>Чому об'єкт не можна переносити. null — можна.</summary>
    private static string WhyKeepAtRoot(GameObject obj)
    {
        if (KeepAtRoot.Contains(obj.name))
            return "DontDestroyOnLoad працює лише в корені";

        if (obj.GetComponent<Canvas>() != null)
            return "Canvas";

        if (obj.GetComponentInChildren<Camera>(true) != null)
            return "камера";

        if (obj.GetComponent<EventSystem>() != null)
            return "EventSystem";

        return null;
    }

    private static string Classify(GameObject obj)
    {
        if (SpawnPointNames.Contains(obj.name) || obj.name.Contains("SpawnPoint"))
            return GroupSpawnPoints;

        if (EnvironmentNames.Contains(obj.name))
            return GroupEnvironment;

        // Менеджер — за наявним MonoBehaviour, а не за іменем.
        foreach (var mb in obj.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            string typeName = mb.GetType().Name;
            if (typeName.EndsWith("Manager") || typeName.EndsWith("Spawner") || typeName.EndsWith("System"))
                return GroupManagers;
        }

        return null;
    }

    private static GameObject FindOrCreateGroup(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name) return root;

        // Edit-time: об'єкт зберігається в сцену, у рантаймі нічого не створюється.
        var group = new GameObject(name);
        SceneManager.MoveGameObjectToScene(group, scene);
        group.transform.position = Vector3.zero;
        return group;
    }
}
