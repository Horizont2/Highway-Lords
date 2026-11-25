using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Один файл збереження замість 53 ключів PlayerPrefs.
///
/// Чому не лишили PlayerPrefs: на Android це XML у пісочниці застосунку, який
/// пишеться по одному ключу й не має жодних гарантій цілісності — обрив на
/// середині лишає половину прогресу. Тут усе лягає одним JSON через
/// тимчасовий файл, тож або зберігається все, або нічого.
///
/// Міграція автоматична: якщо файлу ще немає, а в PlayerPrefs щось лежить,
/// старі значення переносяться при першому зверненні. Прогрес гравців,
/// які вже грали, не втрачається.
///
/// API навмисно повторює PlayerPrefs, щоб перехід був механічним і не
/// тягнув за собою зміни в логіці.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";
    private const string MigratedFlag = "__migrated";

    /// <summary>Ключі, які треба перенести зі старих збережень.</summary>
    private static readonly string[] LegacyIntKeys =
    {
        "SavedGold", "SavedWood", "SavedStone", "SavedWave", "SavedGems",
        "PlayerGlory", "SavedCurrentKills", "SavedKillsToNextGem",
        "SavedKnightLevel", "SavedArcherLevel", "SavedSpearmanLevel",
        "SavedCavalryLevel", "SavedWallArcherLevel",
        "SavedMaxUnits", "SavedBarracksLevel",
        "SavedSpearmanUnlocked", "SavedCavalryUnlocked",
        "SavedMineBuilt", "SavedMineLevel", "SavedCastleLevel", "SavedTowerLevel",
        "SavedCrossbowBuilt", "SavedCrossbowDmgLevel", "SavedCrossbowReloadLevel",
        "MetaFortifiedWalls", "MetaPrecisionBows", "MetaVolleyBarrage",
        "MetaTrophyBounty", "MetaEfficientCarts", "MetaMendingMasonry",
        "FreeKnights", "FreeArchers", "FreeSpearmen", "FreeCavalry",
        "PoolKnights", "PoolArchers", "PoolSpearmen", "PoolCavalry",
        "Merc_Knight_Battles", "Merc_Archer_Battles",
        "Merc_Spearman_Battles", "Merc_Cavalry_Battles",
        "CitiesConquered", "TutorialStepIndex",
        "ConsecutiveDays", "DailiesCount", "TodayLoginClaimed",
    };

    private static readonly string[] LegacyFloatKeys = { "MusicVolume", "SFXVolume" };
    private static readonly string[] LegacyStringKeys = { "SavedArmyData", "LastLoginDate" };

    [System.Serializable]
    private class Store
    {
        // JsonUtility не вміє словники, тож тримаємо паралельні списки.
        public List<string> intKeys = new List<string>();
        public List<int> intValues = new List<int>();

        public List<string> floatKeys = new List<string>();
        public List<float> floatValues = new List<float>();

        public List<string> stringKeys = new List<string>();
        public List<string> stringValues = new List<string>();
    }

    private static Store store;
    private static readonly Dictionary<string, int> ints = new Dictionary<string, int>();
    private static readonly Dictionary<string, float> floats = new Dictionary<string, float>();
    private static readonly Dictionary<string, string> strings = new Dictionary<string, string>();

    private static bool dirty;
    private static bool loaded;

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    // ------------------------------------------------------------------
    // Автозбереження
    // ------------------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        // Скидаємо стан: при вимкненому Domain Reload статика переживає Play.
        loaded = false;
        dirty = false;
        ints.Clear(); floats.Clear(); strings.Clear();

        Application.quitting -= Save;
        Application.quitting += Save;

        Application.focusChanged -= OnFocusChanged;
        Application.focusChanged += OnFocusChanged;
    }

    private static void OnFocusChanged(bool hasFocus)
    {
        // Згортання застосунку на мобільному — остання надійна мить, щоб записати.
        if (!hasFocus) Save();
    }

    // ------------------------------------------------------------------
    // Завантаження
    // ------------------------------------------------------------------

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;

        store = new Store();

        if (File.Exists(FilePath))
        {
            try
            {
                string json = File.ReadAllText(FilePath);
                var parsed = JsonUtility.FromJson<Store>(json);
                if (parsed != null) store = parsed;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Не вдалося прочитати {FilePath}: {e.Message}. Починаємо з чистого.");
                store = new Store();
            }
        }

        Unpack();

        if (!ints.ContainsKey(MigratedFlag)) MigrateFromPlayerPrefs();
    }

    private static void Unpack()
    {
        ints.Clear(); floats.Clear(); strings.Clear();

        for (int i = 0; i < store.intKeys.Count && i < store.intValues.Count; i++)
            ints[store.intKeys[i]] = store.intValues[i];

        for (int i = 0; i < store.floatKeys.Count && i < store.floatValues.Count; i++)
            floats[store.floatKeys[i]] = store.floatValues[i];

        for (int i = 0; i < store.stringKeys.Count && i < store.stringValues.Count; i++)
            strings[store.stringKeys[i]] = store.stringValues[i];
    }

    /// <summary>
    /// Переносить прогрес зі старих PlayerPrefs. Виконується один раз —
    /// далі стоїть позначка, і старі значення більше не читаються.
    /// </summary>
    private static void MigrateFromPlayerPrefs()
    {
        int moved = 0;

        foreach (var key in LegacyIntKeys)
            if (PlayerPrefs.HasKey(key)) { ints[key] = PlayerPrefs.GetInt(key); moved++; }

        foreach (var key in LegacyFloatKeys)
            if (PlayerPrefs.HasKey(key)) { floats[key] = PlayerPrefs.GetFloat(key); moved++; }

        foreach (var key in LegacyStringKeys)
            if (PlayerPrefs.HasKey(key)) { strings[key] = PlayerPrefs.GetString(key); moved++; }

        // Захоплені табори мають динамічні імена (Camp_1_Conquered і далі),
        // тож перебираємо з запасом.
        for (int id = 1; id <= 100; id++)
        {
            string key = "Camp_" + id + "_Conquered";
            if (PlayerPrefs.HasKey(key)) { ints[key] = PlayerPrefs.GetInt(key); moved++; }
        }

        ints[MigratedFlag] = 1;
        dirty = true;

        if (moved > 0)
        {
            Debug.Log($"[SaveSystem] Перенесено зі старих збережень: {moved} значень.");
            Save();
        }
    }

    // ------------------------------------------------------------------
    // Запис
    // ------------------------------------------------------------------

    public static void Save()
    {
        if (!loaded || !dirty) return;

        store.intKeys.Clear(); store.intValues.Clear();
        foreach (var pair in ints) { store.intKeys.Add(pair.Key); store.intValues.Add(pair.Value); }

        store.floatKeys.Clear(); store.floatValues.Clear();
        foreach (var pair in floats) { store.floatKeys.Add(pair.Key); store.floatValues.Add(pair.Value); }

        store.stringKeys.Clear(); store.stringValues.Clear();
        foreach (var pair in strings) { store.stringKeys.Add(pair.Key); store.stringValues.Add(pair.Value); }

        try
        {
            // Пишемо в тимчасовий файл і підміняємо: обрив на середині
            // не псує наявне збереження.
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(store));

            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(temp, FilePath);

            dirty = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Не вдалося записати {FilePath}: {e.Message}");
        }
    }

    // ------------------------------------------------------------------
    // API у стилі PlayerPrefs
    // ------------------------------------------------------------------

    public static int GetInt(string key, int defaultValue = 0)
    {
        EnsureLoaded();
        return ints.TryGetValue(key, out int v) ? v : defaultValue;
    }

    public static void SetInt(string key, int value)
    {
        EnsureLoaded();
        if (ints.TryGetValue(key, out int old) && old == value) return;
        ints[key] = value;
        dirty = true;
    }

    public static float GetFloat(string key, float defaultValue = 0f)
    {
        EnsureLoaded();
        return floats.TryGetValue(key, out float v) ? v : defaultValue;
    }

    public static void SetFloat(string key, float value)
    {
        EnsureLoaded();
        if (floats.TryGetValue(key, out float old) && Mathf.Approximately(old, value)) return;
        floats[key] = value;
        dirty = true;
    }

    public static string GetString(string key, string defaultValue = "")
    {
        EnsureLoaded();
        return strings.TryGetValue(key, out string v) ? v : defaultValue;
    }

    public static void SetString(string key, string value)
    {
        EnsureLoaded();
        if (strings.TryGetValue(key, out string old) && old == value) return;
        strings[key] = value;
        dirty = true;
    }

    public static bool HasKey(string key)
    {
        EnsureLoaded();
        return ints.ContainsKey(key) || floats.ContainsKey(key) || strings.ContainsKey(key);
    }

    public static void DeleteKey(string key)
    {
        EnsureLoaded();
        bool removed = ints.Remove(key) | floats.Remove(key) | strings.Remove(key);
        if (removed) dirty = true;
    }

    public static void DeleteAll()
    {
        EnsureLoaded();
        ints.Clear(); floats.Clear(); strings.Clear();

        // Позначку лишаємо: інакше наступне звернення знову затягне
        // старі PlayerPrefs і "видалене" збереження повернеться.
        ints[MigratedFlag] = 1;

        dirty = true;
        Save();
    }
}
