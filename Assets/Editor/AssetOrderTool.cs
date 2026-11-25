using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Розкладає асети по папках за призначенням.
///
/// Переміщення через AssetDatabase.MoveAsset зберігає GUID, тож усі посилання
/// зі сцен, префабів і ProjectSettings лишаються цілими. Саме тому це
/// безпечно робити інструментом, а не руками через файлову систему.
///
/// Що робиться:
///   * 8 файлів з кореня Assets/ розкладаються по папках;
///   * 94 скрипти з однієї купи — по підпапках за призначенням.
///
/// Що лише повідомляється, без дій: три папки анімацій (animations/, anim2/,
/// archer/) і залишок _Recovery/. Їх злиття вимагає рішень про колізії імен
/// і про те, що взагалі треба зберегти — це не автоматизується наосліп.
/// </summary>
public static class AssetOrderTool
{
    // Куди переносимо файли з кореня Assets/
    private static readonly Dictionary<string, string> RootStrays = new Dictionary<string, string>
    {
        { "Flag.anim",                                "Assets/Art/Animations" },
        { "Idle.anim",                                "Assets/Art/Animations" },
        { "flag_1 (1).controller",                    "Assets/Art/Animations" },
        { "maincart_0.controller",                    "Assets/Art/Animations" },
        { "ZeroFriction.physicsMaterial2D",           "Assets/Settings" },
        { "DefaultVolumeProfile.asset",               "Assets/Settings" },
        { "UniversalRenderPipelineGlobalSettings.asset", "Assets/Settings" },
        { "InputSystem_Actions.inputactions",         "Assets/Settings" },
    };

    // Розкладка скриптів. Підпапка -> імена файлів без розширення.
    private static readonly Dictionary<string, string[]> ScriptFolders = new Dictionary<string, string[]>
    {
        ["Core"] = new[]
        {
            "GameManager", "GameSaveData", "EconomyConfig", "GameStarter",
            "InitialLoader", "ResolutionFixer", "CrossSceneData", "PoolRegistry",
        },
        ["Units"] = new[]
        {
            "Knight", "Archer", "Spearman", "Cavalry",
            "Guard", "EnemyArcher", "EnemySpearman", "EnemyHorse", "EnemyElephant",
            "Boss", "BatteringRam", "WallArcher", "CrossbowTower",
            "UnitStats", "EnemyStats", "Enemy", "EnemySelector",
            "Projectile", "EnemyProjectile", "Arrow", "RainArrow",
        },
        ["Buildings"] = new[]
        {
            "Wall", "Spikes", "GoldMine", "Barracks", "BuildingSpot", "Cart",
        },
        ["Campaign"] = new[]
        {
            "CampaignBalance", "CampaignManager", "CampaignUnit", "BattleManager",
            "MapNode", "MapMarkerVisuals", "GlobalMapController",
            "FogOfWarCloud", "FogRegion", "ManualMapScroll",
            "AnimatedBattleResult", "BattleCameraScroll",
        },
        ["Managers"] = new[]
        {
            "SoundManager", "QuestManager", "ChestManager", "CodexManager",
            "TavernManager", "SkillManager", "TutorialManager", "WeatherManager",
            "LoadingManager", "EnemySpawner", "Spawner", "CartSpawner", "CartManager",
            "WallArcherManager", "CallLegionCooldownManager",
        },
        ["UI"] = new[]
        {
            "CastleUpgradeUI", "SettingsMenu", "PanelSlider", "LivingButton",
            "UIPulseEffect", "BonusPanelBlinker", "WaveInfoPanel", "QuestRowUI",
            "HealthBar", "HealthBarSegments", "DamagePopup", "UI_ForceScrollToTop",
            "LoadingSpinner", "ForgeLock", "FloatingIcon", "TargetIndicator",
            "AudioSettingsSync", "HornController",
        },
        ["Camera"] = new[]
        {
            "CameraController", "CameraShake",
        },
        ["VFX"] = new[]
        {
            "CloudShadow", "CloudScroller", "UniversalDust", "MovementVFX",
            "SimpleShine", "YSorter", "DepthSorter", "FlickeringLight2D",
        },
    };

    private const string ScriptsRoot = "Assets/scripts";

    /// <summary>
    /// Папки, яким невдалий запуск дав ім'я з іншим регістром. Unity так
    /// обходить колізію на файловій системі, нечутливій до регістру:
    /// запит на "Units" при наявній "Units N" дав "UnitS".
    /// </summary>
    private static readonly Dictionary<string, string> FolderRenames = new Dictionary<string, string>
    {
        { ScriptsRoot + "/UnitS", "Units" },
        { ScriptsRoot + "/BuildingS", "Buildings" },
    };

    /// <summary>
    /// Перейменування лише за регістром файлова система не приймає,
    /// тому йдемо через тимчасове ім'я.
    /// </summary>
    private static bool RenameFolderCaseSafe(string path, string targetName, StringBuilder log)
    {
        if (!AssetDatabase.IsValidFolder(path)) return false;
        if (Path.GetFileName(path) == targetName) return false;

        string temp = targetName + "__tmp";

        string error = AssetDatabase.RenameAsset(path, temp);
        if (!string.IsNullOrEmpty(error))
        {
            log.AppendLine($"  [ПОМИЛКА] {path} -> {temp}: {error}");
            return false;
        }

        string tempPath = Path.GetDirectoryName(path).Replace('\\', '/') + "/" + temp;
        error = AssetDatabase.RenameAsset(tempPath, targetName);
        if (!string.IsNullOrEmpty(error))
        {
            log.AppendLine($"  [ПОМИЛКА] {tempPath} -> {targetName}: {error}");
            return false;
        }

        log.AppendLine($"  {Path.GetFileName(path)} -> {targetName}");
        return true;
    }

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Assets/1. План наведення порядку", false, 700)]
    public static void Preview() => Run(apply: false);

    [MenuItem("Tools/Highway Lords/Assets/2. Навести порядок", false, 701)]
    public static void Apply()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Навести порядок в асетах",
            "Файли з кореня Assets/ і скрипти розкладуться по папках.\n\n" +
            "Переміщення зберігає GUID, тож посилання зі сцен і префабів\n" +
            "не ламаються.\n\n" +
            "Спершу подивись план (пункт 1).",
            "Навести порядок", "Скасувати");

        if (ok) Run(apply: true);
    }

    // ==================================================================

    private static void Run(bool apply)
    {
        var log = new StringBuilder(apply ? "=== НАВЕДЕНО ПОРЯДОК ===\n" : "=== ПЛАН ===\n");
        var moves = new List<(string from, string to)>();

        // --- файли з кореня ---
        foreach (var pair in RootStrays)
        {
            string from = "Assets/" + pair.Key;
            if (!File.Exists(from)) continue;
            moves.Add((from, pair.Value + "/" + pair.Key));
        }

        // --- скрипти ---
        if (AssetDatabase.IsValidFolder(ScriptsRoot))
        {
            foreach (var path in Directory.GetFiles(ScriptsRoot, "*.cs"))
            {
                string normalized = path.Replace('\\', '/');
                string name = Path.GetFileNameWithoutExtension(normalized);

                string folder = ScriptFolders
                    .Where(kv => kv.Value.Contains(name))
                    .Select(kv => kv.Key)
                    .FirstOrDefault();

                if (folder == null) continue;
                moves.Add((normalized, $"{ScriptsRoot}/{folder}/{name}.cs"));
            }
        }

        if (moves.Count == 0)
        {
            log.AppendLine("Нема що переносити — усе вже на місці.");
        }
        else
        {
            foreach (var group in moves.GroupBy(m => Path.GetDirectoryName(m.to).Replace('\\', '/')))
            {
                log.AppendLine($"  {group.Key}");
                foreach (var m in group) log.AppendLine($"      {Path.GetFileName(m.from)}");
            }
        }

        // --- непереміщені скрипти ---
        if (AssetDatabase.IsValidFolder(ScriptsRoot))
        {
            var unclassified = Directory.GetFiles(ScriptsRoot, "*.cs")
                .Select(p => Path.GetFileNameWithoutExtension(p))
                .Where(n => !ScriptFolders.Any(kv => kv.Value.Contains(n)))
                .ToList();

            if (unclassified.Count > 0)
            {
                log.AppendLine();
                log.AppendLine($"  Не класифіковано ({unclassified.Count}) — лишаються на місці:");
                foreach (var n in unclassified) log.AppendLine($"      {n}");
            }
        }

        // --- те, що треба вирішити вручну ---
        log.AppendLine();
        log.AppendLine("  Потребує твого рішення (інструмент не чіпає):");

        foreach (var folder in new[] { "Assets/animations", "Assets/anim2", "Assets/archer" })
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            int count = AssetDatabase.FindAssets("", new[] { folder }).Length;
            log.AppendLine($"      {folder} — {count} асетів");
        }
        log.AppendLine("      три папки під анімації та спрайти; злиття може дати колізії імен");

        if (AssetDatabase.IsValidFolder("Assets/_Recovery"))
            log.AppendLine("      Assets/_Recovery — залишок від аварійного відновлення редактора");

        if (!apply)
        {
            log.AppendLine();
            log.AppendLine("Це лише план. Застосувати: Assets > 2. Навести порядок.");
            Debug.Log(log.ToString());
            return;
        }

        // --- спершу нормалізуємо покручені імена папок ---
        foreach (var pair in FolderRenames)
            RenameFolderCaseSafe(pair.Key, pair.Value, log);

        // --- потім папки, і ТІЛЬКИ поза пакетним режимом ---
        // Під StartAssetEditing база асетів не оновлюється, тож IsValidFolder
        // не бачить щойно створену папку, і CreateFolder робить дубль з
        // номером — "Units 1", "Units 2" і далі на кожен файл.
        foreach (var folder in moves
                     .Select(m => Path.GetDirectoryName(m.to).Replace('\\', '/'))
                     .Distinct())
        {
            EnsureFolder(folder);
        }
        AssetDatabase.Refresh();

        // --- переносимо ---
        int moved = 0, failed = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (var (from, to) in moves)
            {
                string error = AssetDatabase.MoveAsset(from, to);
                if (string.IsNullOrEmpty(error)) moved++;
                else
                {
                    failed++;
                    log.AppendLine($"  [ПОМИЛКА] {from}: {error}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        log.AppendLine();
        log.AppendLine($"Перенесено: {moved}, з помилками: {failed}.");
        Debug.Log(log.ToString());

        // Мовчазний провал — найгірше, що може зробити такий інструмент.
        if (failed > 0)
        {
            EditorUtility.DisplayDialog("Навести порядок",
                $"Перенесено: {moved}\nНе вдалося: {failed}\n\nДеталі в консолі.", "OK");
        }
    }

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Assets/3. Прибрати порожні папки", false, 702)]
    public static void CleanEmptyFolders()
    {
        var empties = FindEmptyFolders().ToList();

        if (empties.Count == 0)
        {
            EditorUtility.DisplayDialog("Порожні папки", "Порожніх папок не знайдено.", "OK");
            return;
        }

        bool ok = EditorUtility.DisplayDialog(
            "Прибрати порожні папки",
            $"Знайдено порожніх папок: {empties.Count}\n\n" +
            string.Join("\n", empties.Take(15)) +
            (empties.Count > 15 ? $"\n... і ще {empties.Count - 15}" : "") +
            "\n\nУ них немає жодного асета.",
            "Видалити", "Скасувати");

        if (!ok) return;

        int deleted = 0;
        foreach (var folder in empties)
            if (AssetDatabase.DeleteAsset(folder)) deleted++;

        AssetDatabase.Refresh();
        Debug.Log($"Видалено порожніх папок: {deleted} з {empties.Count}.");
    }

    /// <summary>
    /// Порожні папки всередині scripts/ та Art/ — здебільшого сміття від
    /// невдалого запуску. Глибші рівні йдуть першими, щоб папка-батько
    /// теж стала порожньою і потрапила під видалення.
    /// </summary>
    private static IEnumerable<string> FindEmptyFolders()
    {
        var roots = new[] { ScriptsRoot, "Assets/Art" }
            .Where(AssetDatabase.IsValidFolder);

        var result = new List<string>();

        foreach (var root in roots)
        {
            foreach (var dir in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
            {
                string path = dir.Replace('\\', '/');

                // Папка порожня, якщо в ній немає жодного файлу, крім .meta.
                bool hasAssets = Directory
                    .GetFiles(path, "*", SearchOption.AllDirectories)
                    .Any(f => !f.EndsWith(".meta"));

                if (!hasAssets) result.Add(path);
            }
        }

        return result.OrderByDescending(p => p.Count(c => c == '/'));
    }

    /// <summary>Створює папку з усіма проміжними рівнями.</summary>
    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf = Path.GetFileName(folder);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
