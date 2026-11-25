using UnityEngine;

/// <summary>
/// Єдине джерело правди для балансу кампанії.
///
/// Навіщо: склад гарнізону, число в "ENEMY POWER", шкала шансу перемоги і те,
/// що реально спавниться в бою, раніше рахувалися в трьох різних місцях
/// різними формулами. Розвідка показувала одне, бій давав інше — на 10-му
/// таборі різниця була в 43 рази.
///
/// Тепер усі троє — MapNode, CampaignManager і BattleManager — беруть числа
/// звідси. Міняєш тут — міняється скрізь однаково.
/// </summary>
public static class CampaignBalance
{
    /// <summary>Скільки фізичних юнітів в одному загоні гравця.</summary>
    public const int UnitsPerSquad = 5;

    /// <summary>Максимум загонів, які гравець бере в похід.</summary>
    public const int MaxSquads = 9;

    // ------------------------------------------------------------------
    // Щільність гарнізону
    // ------------------------------------------------------------------

    /// <summary>
    /// Головна ручка складності кампанії.
    ///
    /// Гарнізон набирається на бюджет MapNode.enemyPowerScore (35 на 1-му
    /// таборі, 320 на 10-му). Що менша вартість юніта, то більше їх влізе
    /// в той самий бюджет.
    ///
    /// 1.0 — вартості як були в MapNode.GenerateEnemyArmy.
    /// Менше 1.0 — гарнізони більші й бій важчий.
    /// </summary>
    public const float GarrisonScale = 1f;

    // Базові вартості юніта в очках enemyPowerScore.
    private const int BaseCostGuard = 15;
    private const int BaseCostArcher = 20;
    private const int BaseCostSpearman = 30;
    private const int BaseCostCavalry = 50;

    private static int Cost(int baseCost) =>
        Mathf.Max(1, Mathf.RoundToInt(baseCost * GarrisonScale));

    // Рівень табору, з якого в гарнізоні з'являється відповідний клас.
    private const int UnlockArcher = 2;
    private const int UnlockSpearman = 4;
    private const int UnlockCavalry = 8;

    // ------------------------------------------------------------------
    // Стати
    // ------------------------------------------------------------------

    /// <summary>Стати ворожого юніта. Ті самі числа, що BattleManager використовував локально.</summary>
    public static void GetEnemyStats(string unitClass, int campLevel,
        out int hp, out int dmg, out float speed, out bool isRanged)
    {
        campLevel = Mathf.Max(1, campLevel);

        switch (unitClass)
        {
            case "Archer":
                hp = 40 + campLevel * 10; dmg = 7 + campLevel * 2;
                speed = 1.3f; isRanged = true; break;

            case "Spearman":
                hp = 50 + campLevel * 12; dmg = 9 + campLevel * 3;
                speed = 1.6f; isRanged = false; break;

            case "Cavalry":
                hp = 90 + campLevel * 20; dmg = 15 + campLevel * 4;
                speed = 3.2f; isRanged = false; break;

            default: // Knight / Guard
                hp = 60 + campLevel * 15; dmg = 10 + campLevel * 3;
                speed = 1.9f; isRanged = false; break;
        }
    }

    /// <summary>Стати юніта гравця за рівнем прокачки.</summary>
    public static void GetPlayerStats(string unitClass, int level,
        out int hp, out int dmg, out float speed, out bool isRanged)
    {
        level = Mathf.Max(1, level);

        switch (unitClass)
        {
            case "Archer":
                hp = 60 + level * 10; dmg = 25 + level * 5;
                speed = 1.3f; isRanged = true; break;

            case "Spearman":
                hp = 90 + level * 15; dmg = 30 + level * 6;
                speed = 1.6f; isRanged = false; break;

            case "Cavalry":
                hp = 150 + level * 25; dmg = 40 + level * 8;
                speed = 3.2f; isRanged = false; break;

            default: // Knight
                hp = 120 + level * 20; dmg = 35 + level * 7;
                speed = 1.9f; isRanged = false; break;
        }
    }

    /// <summary>Стати найманця. Від рівня прокачки не залежать.</summary>
    public static void GetMercenaryStats(string unitClass,
        out int hp, out int dmg, out float speed, out bool isRanged)
    {
        switch (unitClass)
        {
            case "Archer":
                hp = 100; dmg = 35; speed = 1.5f; isRanged = true; break;
            case "Spearman":
                hp = 150; dmg = 40; speed = 1.8f; isRanged = false; break;
            case "Cavalry":
                hp = 250; dmg = 55; speed = 3.5f; isRanged = false; break;
            default:
                hp = 200; dmg = 45; speed = 1.9f; isRanged = false; break;
        }
    }

    // ------------------------------------------------------------------
    // Сила
    // ------------------------------------------------------------------

    /// <summary>
    /// Сила одного юніта. Одна формула для обох сторін — інакше порівнювати
    /// "YOUR POWER" з "ENEMY POWER" немає сенсу.
    /// </summary>
    public static int UnitPower(int hp, int dmg) => dmg + Mathf.RoundToInt(hp * 0.2f);

    /// <summary>Сила загону гравця — це сила юніта, помножена на їх кількість у загоні.</summary>
    public static int SquadPower(string unitClass, int level, bool isMercenary)
    {
        int hp, dmg; float speed; bool ranged;

        if (isMercenary) GetMercenaryStats(unitClass, out hp, out dmg, out speed, out ranged);
        else GetPlayerStats(unitClass, level, out hp, out dmg, out speed, out ranged);

        return UnitPower(hp, dmg) * UnitsPerSquad;
    }

    /// <summary>Сумарна сила гарнізону — так само, по окремих юнітах.</summary>
    public static int GarrisonPower(int guards, int archers, int spearmen, int cavalry, int campLevel)
    {
        int total = 0;
        total += guards * ClassPower("Knight", campLevel);
        total += archers * ClassPower("Archer", campLevel);
        total += spearmen * ClassPower("Spearman", campLevel);
        total += cavalry * ClassPower("Cavalry", campLevel);
        return total;
    }

    private static int ClassPower(string unitClass, int campLevel)
    {
        GetEnemyStats(unitClass, campLevel, out int hp, out int dmg, out _, out _);
        return UnitPower(hp, dmg);
    }

    // ------------------------------------------------------------------
    // Гарнізон
    // ------------------------------------------------------------------

    /// <summary>
    /// Склад гарнізону табору.
    ///
    /// Детермінований: той самий campId завжди дає той самий склад. Раніше
    /// це був UnityEngine.Random у Start(), тож розвідка показувала новий
    /// склад при кожному заході на мапу.
    ///
    /// Використовує власний System.Random, щоб не збивати глобальний
    /// генератор Unity, на який спирається решта гри.
    /// </summary>
    public static void GenerateGarrison(int campId, int campLevel, int powerBudget,
        out int guards, out int archers, out int spearmen, out int cavalry)
    {
        guards = archers = spearmen = cavalry = 0;
        campLevel = Mathf.Max(1, campLevel);

        var rng = new System.Random(campId * 7919 + campLevel);

        int costGuard = Cost(BaseCostGuard);
        int costArcher = Cost(BaseCostArcher);
        int costSpearman = Cost(BaseCostSpearman);
        int costCavalry = Cost(BaseCostCavalry);

        int left = Mathf.Max(0, powerBudget);
        int guardAttempts = 0;

        while (left >= costGuard && guardAttempts < 10000)
        {
            guardAttempts++;

            if (campLevel >= UnlockCavalry && left >= costCavalry && rng.NextDouble() > 0.7)
            {
                cavalry++; left -= costCavalry;
            }
            else if (campLevel >= UnlockSpearman && left >= costSpearman && rng.NextDouble() > 0.6)
            {
                spearmen++; left -= costSpearman;
            }
            else if (campLevel >= UnlockArcher && left >= costArcher && rng.NextDouble() > 0.5)
            {
                archers++; left -= costArcher;
            }
            else
            {
                guards++; left -= costGuard;
            }
        }

        // Табір без жодного захисника виглядав би зламаним.
        if (guards + archers + spearmen + cavalry == 0) guards = 3;
    }

    // ------------------------------------------------------------------
    // Хвилі
    // ------------------------------------------------------------------

    /// <summary>Скільки ворогів приблизно виходить в одній хвилі.</summary>
    private const int EnemiesPerWave = 10;

    /// <summary>Більше хвиль, ніж це, розтягує бій без користі.</summary>
    private const int MaxWaves = 4;

    /// <summary>
    /// Гарнізон виходить хвилями, але сумарно це рівно ті юніти, яких
    /// показала розвідка — не більше й не менше.
    /// </summary>
    public static int WaveCount(int totalGarrison)
    {
        if (totalGarrison <= 0) return 1;
        return Mathf.Clamp(Mathf.CeilToInt(totalGarrison / (float)EnemiesPerWave), 1, MaxWaves);
    }

    /// <summary>
    /// Скільки юнітів цього класу виходить у хвилі waveIndex (з нуля).
    /// Залишок від ділення віддається останнім хвилям, тож сума по всіх
    /// хвилях завжди дорівнює total.
    /// </summary>
    public static int UnitsInWave(int total, int waveIndex, int waveCount)
    {
        if (waveCount <= 1) return waveIndex == 0 ? total : 0;
        if (waveIndex < 0 || waveIndex >= waveCount) return 0;

        int spawnedBefore = total * waveIndex / waveCount;
        int spawnedThrough = total * (waveIndex + 1) / waveCount;
        return spawnedThrough - spawnedBefore;
    }
}
