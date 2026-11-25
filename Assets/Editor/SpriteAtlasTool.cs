using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Збирає UI-спрайти в атласи, щоб скоротити кількість draw call.
///
/// ВАЖЛИВО про порядок: атлас пакує спрайти в тій роздільності, у якій вони
/// імпортовані. Поки великі листи імпортуються в 2048, окремий спрайт
/// усередині може займати пів-атласа — тоді атласи не зменшують draw call,
/// а лише дублюють памʼять. Тому спершу:
///   Textures > 2. Увімкнути стиск
///   Textures > 3. Підігнати розміри під використання
/// і лише потім збирати атласи.
///
/// Текстури, більші за MaxSideForAtlas, свідомо лишаються поза атласами:
/// фони й великі панелі й так малюються поодинці, а в атласі вони б
/// роздували сторінки.
/// </summary>
public static class SpriteAtlasTool
{
    private const string AtlasFolder = "Assets/Settings/Atlases";

    /// <summary>Спрайти, довша сторона яких більша за це, в атлас не йдуть.</summary>
    private const int MaxSideForAtlas = 1024;

    /// <summary>Розмір сторінки атласа.</summary>
    private const int AtlasPageSize = 2048;

    private const int Padding = 4;

    /// <summary>Папки, з яких збираємо атласи. Один рядок — один атлас.</summary>
    private static readonly string[] Groups =
    {
        "Assets/NewStyle",
        "Assets/Units/Panelimgs",
        "Assets/GeneralMap1",
        "Assets/GlobalMap",
        "Assets/CodexPanel",
        "Assets/ForgePanel",
        "Assets/ChestPanel",
        "Assets/Achievements_Panel",
        "Assets/PreparePanel",
        "Assets/ExitConfirmationPanel",
        "Assets/Objects/Icons",
        "Assets/Objects/Buttons",
    };

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Atlases/1. Що потрапить в атласи", false, 300)]
    public static void Preview()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== ПЛАН АТЛАСІВ ===");
        sb.AppendLine($"Поріг розміру: спрайти з довшою стороною > {MaxSideForAtlas} px лишаються окремо");
        sb.AppendLine($"Сторінка атласа: {AtlasPageSize}x{AtlasPageSize}");
        sb.AppendLine();

        int totalIn = 0, totalOut = 0;

        foreach (string folder in Groups)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                sb.AppendLine($"--- {folder}  (немає такої папки)");
                continue;
            }

            var (included, excluded, area) = Collect(folder);
            totalIn += included.Count;
            totalOut += excluded.Count;

            float pages = area / (float)(AtlasPageSize * AtlasPageSize);

            sb.AppendLine($"--- {folder}");
            sb.AppendLine($"    в атлас: {included.Count} текстур, ~{pages:0.00} сторінки");
            sb.AppendLine($"    поза атласом (завеликі): {excluded.Count}");

            if (included.Count == 0 && excluded.Count > 0)
                sb.AppendLine("    [УВАГА] усе завелике — спершу піджени розміри (Textures > 3)");
            else if (pages > 4f)
                sb.AppendLine("    [УВАГА] більше 4 сторінок — виграш у draw call буде малий");

            foreach (var t in excluded.Take(6))
                sb.AppendLine($"      поза: {t.Width}x{t.Height}  {t.Path}");
            if (excluded.Count > 6)
                sb.AppendLine($"      ... і ще {excluded.Count - 6}");
        }

        sb.AppendLine();
        sb.AppendLine($"Разом в атласи: {totalIn}, поза атласами: {totalOut}");

        if (totalIn == 0)
        {
            sb.AppendLine();
            sb.AppendLine("Зараз в атласи не потрапляє нічого. Це очікувано, поки");
            sb.AppendLine("великі листи імпортуються в 2048. Запусти спершу");
            sb.AppendLine("Textures > 2 і Textures > 3, потім повтори цей звіт.");
        }

        Debug.Log(sb.ToString());
    }

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Atlases/2. Створити атласи", false, 301)]
    public static void CreateAtlases()
    {
        var mode = EditorSettings.spritePackerMode;
        bool v2 = mode == SpritePackerMode.SpriteAtlasV2
                  || mode == SpritePackerMode.SpriteAtlasV2Build;

        if (!v2)
        {
            EditorUtility.DisplayDialog(
                "Режим атласів",
                $"Зараз Sprite Packer Mode = {mode}.\n\n" +
                "Цей інструмент робить атласи версії 2.\n" +
                "Увімкни Project Settings > Editor > Sprite Packer > Sprite Atlas V2.",
                "Зрозуміло");
            return;
        }

        var plan = new List<(string folder, List<TexRef> textures)>();
        int total = 0;

        foreach (string folder in Groups)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;

            var (included, _, _) = Collect(folder);
            if (included.Count == 0) continue;

            plan.Add((folder, included));
            total += included.Count;
        }

        if (total == 0)
        {
            EditorUtility.DisplayDialog(
                "Атласи",
                "Жодна текстура не проходить поріг розміру.\n\n" +
                "Спершу запусти Textures > 2 (стиск) і Textures > 3 (розміри),\n" +
                "інакше атласи не дадуть виграшу.",
                "OK");
            return;
        }

        bool ok = EditorUtility.DisplayDialog(
            "Створити атласи",
            $"Атласів: {plan.Count}\nТекстур у них: {total}\n\n" +
            $"Створюються у {AtlasFolder}.\n" +
            "Вихідні текстури не змінюються — атлас лише посилається на них.",
            "Створити", "Скасувати");

        if (!ok) return;

        if (!AssetDatabase.IsValidFolder(AtlasFolder))
            AssetDatabase.CreateFolder("Assets/Settings", "Atlases");

        var log = new StringBuilder("=== СТВОРЕНО АТЛАСИ ===\n");

        foreach (var (folder, textures) in plan)
        {
            string name = folder.Replace("Assets/", "").Replace("/", "_");
            string path = $"{AtlasFolder}/{name}.spriteatlasv2";

            var atlas = new SpriteAtlasAsset();
            atlas.SetIncludeInBuild(true);

            atlas.SetPackingSettings(new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableRotation = false,
                enableTightPacking = false,
                padding = Padding,
            });

            atlas.SetTextureSettings(new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
            });

            atlas.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "DefaultTexturePlatform",
                maxTextureSize = AtlasPageSize,
                format = TextureImporterFormat.Automatic,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 50,
                overridden = true,
            });

            var objects = textures
                .Select(t => AssetDatabase.LoadAssetAtPath<Texture2D>(t.Path))
                .Where(o => o != null)
                .Cast<Object>()
                .ToArray();

            atlas.Add(objects);
            SpriteAtlasAsset.Save(atlas, path);

            log.AppendLine($"  {objects.Length,3} текстур  {path}");
        }

        AssetDatabase.Refresh();
        log.AppendLine($"\nГотово: {plan.Count} атласів.");
        log.AppendLine("Перевір результат: Window > 2D > Sprite Atlas, або натисни Pack Preview.");
        Debug.Log(log.ToString());
    }

    // ==================================================================

    [MenuItem("Tools/Highway Lords/Atlases/3. Видалити створені атласи", false, 302)]
    public static void DeleteAtlases()
    {
        if (!AssetDatabase.IsValidFolder(AtlasFolder))
        {
            EditorUtility.DisplayDialog("Атласи", "Папки з атласами немає.", "OK");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:SpriteAtlasAsset", new[] { AtlasFolder });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Атласи", $"У {AtlasFolder} атласів немає.", "OK");
            return;
        }

        bool ok = EditorUtility.DisplayDialog(
            "Видалити атласи",
            $"Буде видалено {guids.Length} атласів у {AtlasFolder}.\n\n" +
            "Вихідні текстури й спрайти не постраждають.",
            "Видалити", "Скасувати");

        if (!ok) return;

        foreach (var guid in guids)
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

        AssetDatabase.Refresh();
        Debug.Log($"Видалено атласів: {guids.Length}");
    }

    // ==================================================================

    private struct TexRef
    {
        public string Path;
        public int Width, Height;
    }

    /// <summary>
    /// Розбирає папку на те, що йде в атлас, і те, що завелике.
    /// Третє значення — сумарна площа в пікселях для оцінки кількості сторінок.
    /// </summary>
    private static (List<TexRef> included, List<TexRef> excluded, long area) Collect(string folder)
    {
        var included = new List<TexRef>();
        var excluded = new List<TexRef>();
        long area = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite) continue;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;

            var entry = new TexRef { Path = path, Width = tex.width, Height = tex.height };

            if (Mathf.Max(tex.width, tex.height) > MaxSideForAtlas)
            {
                excluded.Add(entry);
                continue;
            }

            included.Add(entry);

            // Груба оцінка: площа текстури плюс поля по периметру кожного спрайта.
            area += (long)(tex.width + Padding) * (tex.height + Padding);
        }

        return (included, excluded, area);
    }
}
