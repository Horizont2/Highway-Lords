using UnityEngine;

public static class EconomyConfig
{
    // ==========================================
    // 1. ЦІНИ АПГРЕЙДІВ (FORGE)
    // ==========================================
    public static int GetUpgradeCost(int basePrice, int currentLevel)
    {
        return Mathf.RoundToInt(basePrice + (currentLevel * 50f) + (Mathf.Pow(currentLevel, 2) * 2f));
    }

    // ==========================================
    // 2. НАШІ ЮНІТИ (Залежать від Рівня Кузні/Казарми)
    // ==========================================
    public static int GetUnitMaxHealth(int baseHp, int level)
    {
        return Mathf.RoundToInt(baseHp + (level * 15f) + (Mathf.Pow(level, 2) * 0.1f));
    }

    public static int GetUnitDamage(int baseDmg, int level)
    {
        return Mathf.RoundToInt(baseDmg + (level * 3f) + (Mathf.Pow(level, 2) * 0.02f));
    }

    public static float GetUnitAttackRate(float baseRate, int level)
    {
        float newRate = baseRate / (1f + (level * 0.04f));
        return Mathf.Max(0.2f, newRate); 
    }

    // ==========================================
    // 3. ВОРОГИ (Залежать від Хвилі)
    // ==========================================
    
    // === ФІКС: Більш плавне збільшення ХП ворогів ===
    public static int GetEnemyHealth(int baseHp, int wave)
    {
        // Було: (wave * 12) + (wave^2 * 0.05)
        // Стало: (wave * 8) + (wave^2 * 0.03) -> Вороги "жирнішають" повільніше
        return Mathf.RoundToInt(baseHp + (wave * 9f) + (Mathf.Pow(wave, 2) * 0.03f));
    }

    public static int GetEnemyDamage(int baseDmg, int wave)
    {
        return Mathf.RoundToInt(baseDmg + (wave * 2f) + (Mathf.Pow(wave, 2) * 0.01f));
    }

    public static int GetEnemyGoldDrop(int baseGold, int wave)
    {
        float rawGold = baseGold + (wave * 1.5f);
        return Mathf.RoundToInt(rawGold);
    }
}