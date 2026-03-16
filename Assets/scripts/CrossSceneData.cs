using UnityEngine;

public static class CrossSceneData
{
    // Кількість юнітів гравця
    public static int knightsCount;
    public static int archersCount;
    public static int spearmenCount;
    public static int cavalryCount;

    // СКІНИ ЮНІТІВ ДЛЯ БОЮ (Нове!)
    public static Sprite knightSkin;
    public static Sprite archerSkin;
    public static Sprite spearmanSkin;
    public static Sprite cavalrySkin;

    // Вороги
    public static int enemyGuards;
    public static int enemyArchers;
    public static int enemySpearmen;
    public static int enemyCavalry;

    // Дані табору
    public static int spentGold;
    public static string campId;
    public static int campLevel;
    public static string campName;

    // Нагороди
    public static int rewardGold;
    public static int rewardWood;
    public static int rewardStone;

    // СТАТУС ПІСЛЯ БОЮ
    public static bool isReturningFromBattle = false;
    public static bool lastBattleWon = false;
}