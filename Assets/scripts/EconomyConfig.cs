using UnityEngine;

public static class EconomyConfig
{
    // ==========================================
    // 1. ЦІНИ АПГРЕЙДІВ (FORGE)
    // Формула: База + (Рівень * 50) + (Рівень^2 * 2)
    // ==========================================
    public static int GetUpgradeCost(int basePrice, int currentLevel)
    {
        return Mathf.RoundToInt(basePrice + (currentLevel * 50f) + (Mathf.Pow(currentLevel, 2) * 2f));
    }

    // ==========================================
    // 2. НАШІ ЮНІТИ (Залежать від Рівня Кузні/Казарми)
    // ==========================================
    // Здоров'я: База + (Рівень * 15) + (Рівень^2 * 0.1)
    public static int GetUnitMaxHealth(int baseHp, int level)
    {
        return Mathf.RoundToInt(baseHp + (level * 15f) + (Mathf.Pow(level, 2) * 0.1f));
    }

    // Урон: База + (Рівень * 3) + (Рівень^2 * 0.02)
    public static int GetUnitDamage(int baseDmg, int level)
    {
        return Mathf.RoundToInt(baseDmg + (level * 3f) + (Mathf.Pow(level, 2) * 0.02f));
    }

    // Швидкість атаки (Soft Cap: не швидше ніж 0.2 сек)
    public static float GetUnitAttackRate(float baseRate, int level)
    {
        float newRate = baseRate / (1f + (level * 0.03f));
        return Mathf.Max(0.2f, newRate); 
    }

    // ==========================================
    // 3. ВОРОГИ (Залежать від Хвилі)
    // ==========================================
    // Здоров'я: База + (Хвиля * 12) + (Хвиля^2 * 0.05)
    public static int GetEnemyHealth(int baseHp, int wave)
    {
        return Mathf.RoundToInt(baseHp + (wave * 12f) + (Mathf.Pow(wave, 2) * 0.05f));
    }

    // Урон: База + (Хвиля * 2) + (Хвиля^2 * 0.01)
    public static int GetEnemyDamage(int baseDmg, int wave)
    {
        return Mathf.RoundToInt(baseDmg + (wave * 2f) + (Mathf.Pow(wave, 2) * 0.01f));
    }

    // Золото: (База + Хвиля * 1.5) * Бонус
    public static int GetEnemyGoldDrop(int baseGold, int wave)
    {
        float rawGold = baseGold + (wave * 1.5f);
        
        // Якщо у вас вже є BonusManager для кристалів
        /*
        if (BonusManager.Instance != null) {
            rawGold *= BonusManager.Instance.GetGoldPerKillMultiplier();
        }
        */
        
        return Mathf.RoundToInt(rawGold);
    }
}