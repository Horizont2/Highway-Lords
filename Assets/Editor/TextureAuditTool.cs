using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Аудит текстур: скільки памʼяті вони займають і наскільки їхня роздільність
/// відповідає тому, як вони реально виглядають на екрані.
///
/// Чому це важливо саме тут: цільова платформа — Android, а платформних
/// оверайдів у текстур немає, тому Android бере налаштування
/// DefaultTexturePlatform. У частини текстур там стоїть Uncompressed,
/// тобто 4 байти на піксель замість ~1.
///
/// Інструмент 1 — тільки звіт, нічого не змінює.
/// Інструмент 2 — вмикає стиск там, де його немає (змінює лише налаштування
///                імпорту, GUID і посилання не торкаються).
/// </summary>
public static class TextureAuditTool
{
    /// <summary>Роздільність, під яку рахуємо потрібну деталізацію спрайтів.</summary>
    private const int TargetScreenHeight = 1440;

    /// <summary>Запас на зум камери / анімації масштабу.</summary>
    private const float SafetyMargin = 1.25f;

    private const float BytesUncompressed = 4f;
    private const float BytesCompressed = 1f;

    // ==================================================================
    // 1. Звіт
    // ==================================================================

    [MenuItem("Tools/Highway Lords/Textures/1. Звіт по памʼяті та різкості", false, 200)]
    public static void Report()
    {
        var textures = CollectTextures();
        var usage = CollectUsage();

        float currentTotal = 0f, compressedTotal = 0f, rightSizedTotal = 0f;
        var lines = new List<(float waste, string line)>();

        foreach (var info in textures)
        {
            float cur = EstimateBytes(info, info.ImportedWidth, info.ImportedHeight,
                                      info.Compressed ? BytesCompressed : BytesUncompressed);
            float comp = EstimateBytes(info, info.ImportedWidth, info.ImportedHeight, BytesCompressed);

            int recommended = RecommendMaxSize(info, usage);
            var (rw, rh) = ClampToLongestSide(info.SourceWidth, info.SourceHeight, recommended);
            float right = EstimateBytes(info, rw, rh, BytesCompressed);

            currentTotal += cur;
            compressedTotal += comp;
            rightSizedTotal += right;

            string flag = "";
            if (!info.Compressed) flag += " [БЕЗ СТИСКУ]";
            if (recommended < info.MaxTextureSize) flag += $" [завелика: {info.MaxTextureSize}→{recommended}]";
            else if (recommended > info.MaxTextureSize) flag += $" [мила: {info.MaxTextureSize}→{recommended}]";

            if (string.IsNullOrEmpty(flag)) continue;

            lines.Add((cur - right,
                $"{cur / 1048576f,7:0.0} MB → {right / 1048576f,6:0.0} MB  " +
                $"джерело {info.SourceWidth}x{info.SourceHeight}, у грі max {FormatUsage(info, usage)}" +
                $"{flag}\n              {info.Path}"));
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== АУДИТ ТЕКСТУР ===");
        sb.AppendLine($"Текстур: {textures.Count}   цільова роздільність розрахунку: {TargetScreenHeight}p");
        sb.AppendLine();
        sb.AppendLine($"Зараз (налаштування, які бере Android):   {currentTotal / 1048576f,7:0} MB");
        sb.AppendLine($"Якщо просто увімкнути стиск:              {compressedTotal / 1048576f,7:0} MB");
        sb.AppendLine($"Стиск + розмір під фактичне використання: {rightSizedTotal / 1048576f,7:0} MB");
        sb.AppendLine();
        sb.AppendLine($"Текстур без стиску: {textures.Count(t => !t.Compressed)}");
        sb.AppendLine($"Обрізаються при імпорті: {textures.Count(t => System.Math.Max(t.SourceWidth, t.SourceHeight) > t.MaxTextureSize)}");

        int unmeasured = textures.Count(t => FormatUsage(t, usage) == "?");
        sb.AppendLine($"Не вдалося виміряти (спрайт призначається з коду або масиву): {unmeasured}");
        sb.AppendLine("     для них розмір не чіпаємо — потрібне ручне рішення");
        sb.AppendLine();
        sb.AppendLine("--- Що варто правити (найбільша економія вгорі) ---");

        foreach (var (_, line) in lines.OrderByDescending(l => l.waste).Take(45))
            sb.AppendLine("  " + line);

        if (lines.Count > 45) sb.AppendLine($"  ... і ще {lines.Count - 45}");

        sb.AppendLine();
        sb.AppendLine("'у грі max' — найбільший розмір у пікселях, у якому спрайт із цієї");
        sb.AppendLine("текстури зустрічається в сценах і префабах. '?' означає, що спрайт");
        sb.AppendLine("ніде не знайдено статично (може призначатися з коду).");

        Debug.Log(sb.ToString());
    }

    // ==================================================================
    // 2. Увімкнути стиск
    // ==================================================================

    [MenuItem("Tools/Highway Lords/Textures/2. Увімкнути стиск, де його немає", false, 201)]
    public static void EnableCompression()
    {
        var textures = CollectTextures().Where(t => !t.Compressed).ToList();

        if (textures.Count == 0)
        {
            EditorUtility.DisplayDialog("Стиск", "Усі текстури вже стиснені.", "OK");
            return;
        }

        float before = textures.Sum(t => EstimateBytes(t, t.ImportedWidth, t.ImportedHeight, BytesUncompressed));
        float after = textures.Sum(t => EstimateBytes(t, t.ImportedWidth, t.ImportedHeight, BytesCompressed));

        bool ok = EditorUtility.DisplayDialog(
            "Увімкнути стиск текстур",
            $"Текстур без стиску: {textures.Count}\n\n" +
            $"Зараз у памʼяті: ~{before / 1048576f:0} MB\n" +
            $"Стане: ~{after / 1048576f:0} MB\n\n" +
            "Змінюються лише налаштування імпорту (Default > Compression = Compressed).\n" +
            "Формат вибирає Unity під кожну платформу.\n\n" +
            "Стиск може дати легкий бандинг на плавних градієнтах — після\n" +
            "переімпорту перевір великі панелі й іконки.",
            "Застосувати", "Скасувати");

        if (!ok) return;

        var log = new StringBuilder("=== УВІМКНЕНО СТИСК ===\n");
        int done = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < textures.Count; i++)
            {
                var info = textures[i];
                EditorUtility.DisplayProgressBar("Стиск текстур", info.Path, (float)i / textures.Count);

                var importer = AssetImporter.GetAtPath(info.Path) as TextureImporter;
                if (importer == null) continue;

                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.compressionQuality = 50;
                importer.SaveAndReimport();

                log.AppendLine($"  {info.Path}");
                done++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        log.AppendLine($"Готово: {done} текстур.");
        Debug.Log(log.ToString());
    }

    // ==================================================================
    // Збір даних
    // ==================================================================

    private class TexInfo
    {
        public string Path;
        public int SourceWidth, SourceHeight;
        public int ImportedWidth, ImportedHeight;
        public int MaxTextureSize;
        public bool Compressed;
        public bool HasAlpha;
        public List<SpriteEntry> Sprites = new List<SpriteEntry>();
    }

    private struct SpriteEntry
    {
        public string Name;
        public Vector2 Size;
    }

    private static List<TexInfo> CollectTextures()
    {
        var result = new List<TexInfo>();

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;

            // Розмір джерела, а не імпортованого — щоб побачити, що саме обрізається.
            // Читаємо заголовок файлу: публічного API для розміру джерела немає.
            var (srcW, srcH) = ReadSourceSize(path, tex.width, tex.height);

            var settings = importer.GetDefaultPlatformTextureSettings();

            var info = new TexInfo
            {
                Path = path,
                SourceWidth = srcW,
                SourceHeight = srcH,
                ImportedWidth = tex.width,
                ImportedHeight = tex.height,
                MaxTextureSize = settings.maxTextureSize,
                Compressed = settings.textureCompression != TextureImporterCompression.Uncompressed,
                HasAlpha = importer.DoesSourceTextureHaveAlpha(),
            };

            foreach (var s in importer.spritesheet)
                info.Sprites.Add(new SpriteEntry { Name = s.name, Size = s.rect.size });

            result.Add(info);
        }

        return result;
    }

    /// <summary>Найбільший розмір у пікселях, у якому кожен спрайт реально виводиться.</summary>
    private static Dictionary<string, Vector2> CollectUsage()
    {
        var usage = new Dictionary<string, Vector2>();

        void Record(Sprite sprite, Vector2 pixels)
        {
            if (sprite == null) return;

            // Нульовий розмір = RectTransform керується лейаутом і в едіторі
            // ще не порахований. Це "не виміряли", а не "виводиться 0 пікселів".
            if (pixels.x < 1f || pixels.y < 1f) return;

            string key = SpriteKey(sprite);
            if (key == null) return;

            if (usage.TryGetValue(key, out var existing))
                usage[key] = Vector2.Max(existing, pixels);
            else
                usage[key] = pixels;
        }

        void ScanRoots(GameObject[] roots)
        {
            foreach (var root in roots)
            {
                foreach (var img in root.GetComponentsInChildren<Image>(true))
                    Record(img.sprite, UiPixels(img));

                foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                    Record(sr.sprite, WorldPixels(sr));
            }
        }

        // Префаби
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) ScanRoots(new[] { prefab });
        }

        // Сцени з білда
        string openScene = EditorSceneManager.GetActiveScene().path;
        bool reopened = false;

        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.path)) continue;
            if (!File.Exists(entry.path)) continue;

            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            reopened = true;
            ScanRoots(scene.GetRootGameObjects());
        }

        if (reopened && !string.IsNullOrEmpty(openScene))
            EditorSceneManager.OpenScene(openScene, OpenSceneMode.Single);

        return usage;
    }

    private static string SpriteKey(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return null;
        string path = AssetDatabase.GetAssetPath(sprite.texture);
        return string.IsNullOrEmpty(path) ? null : path + "|" + sprite.name;
    }

    private static Vector2 UiPixels(Image img)
    {
        Vector2 size = img.rectTransform.rect.size;

        // Canvas у режимі ScaleWithScreenSize: розмір у referenceResolution-одиницях.
        var canvas = img.canvas;
        float scale = 1f;

        if (canvas != null)
        {
            var scaler = canvas.rootCanvas != null
                ? canvas.rootCanvas.GetComponent<CanvasScaler>()
                : canvas.GetComponent<CanvasScaler>();

            if (scaler != null
                && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                && scaler.referenceResolution.y > 0f)
            {
                scale = TargetScreenHeight / scaler.referenceResolution.y;
            }
        }

        Vector3 lossy = img.rectTransform.lossyScale;
        size.x *= Mathf.Abs(lossy.x);
        size.y *= Mathf.Abs(lossy.y);

        return size * scale * SafetyMargin;
    }

    private static Vector2 WorldPixels(SpriteRenderer sr)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cams.Length > 0) cam = cams[0];
        }

        // Без камери (наприклад, скануємо префаб) беремо піксельний розмір спрайта.
        if (cam == null || !cam.orthographic || cam.orthographicSize <= 0f)
            return sr.sprite != null ? sr.sprite.rect.size : Vector2.zero;

        float pixelsPerUnit = TargetScreenHeight / (2f * cam.orthographicSize);
        Vector3 world = sr.bounds.size;
        return new Vector2(world.x, world.y) * pixelsPerUnit * SafetyMargin;
    }

    // ==================================================================
    // Розрахунки
    // ==================================================================

    private static int RecommendMaxSize(TexInfo info, Dictionary<string, Vector2> usage)
    {
        float worstRatio = 0f;
        bool found = false;

        if (info.Sprites.Count > 0)
        {
            foreach (var s in info.Sprites)
            {
                if (!usage.TryGetValue(info.Path + "|" + s.Name, out var shown)) continue;
                if (s.Size.x <= 0f || s.Size.y <= 0f) continue;

                found = true;
                worstRatio = Mathf.Max(worstRatio,
                    Mathf.Max(shown.x / s.Size.x, shown.y / s.Size.y));
            }
        }
        else
        {
            // Single-sprite або звичайна текстура: ключ — імʼя ассета.
            string name = Path.GetFileNameWithoutExtension(info.Path);
            if (usage.TryGetValue(info.Path + "|" + name, out var shown)
                && info.SourceWidth > 0 && info.SourceHeight > 0)
            {
                found = true;
                worstRatio = Mathf.Max(shown.x / info.SourceWidth, shown.y / info.SourceHeight);
            }
        }

        // Нічого не знайшли — не тягнемо ковдру, лишаємо як є.
        if (!found || worstRatio <= 0f) return info.MaxTextureSize;

        // Вище джерела піднімати нема сенсу — там більше немає даних.
        float factor = Mathf.Min(worstRatio, 1f);
        int longest = Mathf.Max(info.SourceWidth, info.SourceHeight);
        int needed = Mathf.CeilToInt(longest * factor);

        // Округлення до НАЙБЛИЖЧОГО степеня двійки, не вгору: інакше потреба
        // в 2264 px перетворюється на 4096 і подвоює памʼять заради 10% різкості.
        return Mathf.Clamp(NearestPowerOfTwo(needed), 64, 4096);
    }

    private static string FormatUsage(TexInfo info, Dictionary<string, Vector2> usage)
    {
        float w = 0f, h = 0f;
        bool found = false;

        if (info.Sprites.Count > 0)
        {
            foreach (var s in info.Sprites)
            {
                if (!usage.TryGetValue(info.Path + "|" + s.Name, out var shown)) continue;
                found = true;
                w = Mathf.Max(w, shown.x);
                h = Mathf.Max(h, shown.y);
            }
        }
        else
        {
            string name = Path.GetFileNameWithoutExtension(info.Path);
            if (usage.TryGetValue(info.Path + "|" + name, out var shown))
            {
                found = true; w = shown.x; h = shown.y;
            }
        }

        return found ? $"{Mathf.RoundToInt(w)}x{Mathf.RoundToInt(h)}" : "?";
    }

    private static (int, int) ClampToLongestSide(int w, int h, int max)
    {
        int longest = Mathf.Max(w, h);
        if (longest <= max || longest == 0) return (w, h);

        float s = (float)max / longest;
        return (Mathf.Max(1, Mathf.RoundToInt(w * s)), Mathf.Max(1, Mathf.RoundToInt(h * s)));
    }

    /// <summary>
    /// Оцінка памʼяті. w/h — це вже остаточні розміри після обрізання,
    /// обидва виклики передають саме такі.
    /// </summary>
    private static float EstimateBytes(TexInfo info, int w, int h, float bytesPerPixel)
    {
        return (float)w * h * bytesPerPixel;
    }

    /// <summary>
    /// Розмір вихідного файлу з PNG-заголовка (всі текстури проєкту — PNG).
    /// Для інших форматів або при помилці повертає імпортований розмір.
    /// </summary>
    private static (int, int) ReadSourceSize(string path, int fallbackW, int fallbackH)
    {
        try
        {
            var head = new byte[24];
            using (var fs = File.OpenRead(path))
            {
                if (fs.Read(head, 0, head.Length) < head.Length) return (fallbackW, fallbackH);
            }

            // Підпис PNG: 89 'P' 'N' 'G'
            if (head[0] != 0x89 || head[1] != 0x50 || head[2] != 0x4E || head[3] != 0x47)
                return (fallbackW, fallbackH);

            // IHDR: ширина — байти 16..19, висота — 20..23, big-endian
            int w = (head[16] << 24) | (head[17] << 16) | (head[18] << 8) | head[19];
            int h = (head[20] << 24) | (head[21] << 16) | (head[22] << 8) | head[23];

            return (w > 0 && h > 0) ? (w, h) : (fallbackW, fallbackH);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Не вдалося прочитати розмір {path}: {e.Message}");
            return (fallbackW, fallbackH);
        }
    }

    /// <summary>Найближчий степінь двійки — вниз або вгору, що ближче.</summary>
    private static int NearestPowerOfTwo(int v)
    {
        if (v <= 32) return 32;

        int lower = 32;
        while (lower * 2 <= v && lower < 4096) lower <<= 1;

        int upper = Mathf.Min(lower * 2, 4096);
        return (v - lower) <= (upper - v) ? lower : upper;
    }
}
