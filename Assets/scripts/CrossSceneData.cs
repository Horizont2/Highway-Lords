using UnityEngine;

public static class CrossSceneData
{
    // Армія гравця
    public static int knightsCount = 0;
    public static int archersCount = 0;
    public static int spearmenCount = 0;
    public static int cavalryCount = 0;

    // Армія ворога (згенерована розвідкою)
    public static int enemyGuards = 0;
    public static int enemyArchers = 0;
    public static int enemySpearmen = 0;
    public static int enemyCavalry = 0;
    
    // Дані про похід
    public static int spentGold = 0;
    public static int campId = -1;
    public static int campLevel = 1;
    public static string campName = "";

    // Нагороди
    public static int rewardGold = 0;
    public static int rewardWood = 0;
    public static int rewardStone = 0;

    public static void ResetData()
    {
        knightsCount = 0; archersCount = 0; spearmenCount = 0; cavalryCount = 0;
        enemyGuards = 0; enemyArchers = 0; enemySpearmen = 0; enemyCavalry = 0;
        spentGold = 0; campId = -1; rewardGold = 0; rewardWood = 0; rewardStone = 0;
    }
}