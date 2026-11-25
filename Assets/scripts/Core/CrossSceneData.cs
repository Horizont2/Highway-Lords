using UnityEngine;

public static class CrossSceneData
{
    public static int[] squadSlots = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1 };
    
    public static int knightsCount;
    public static int archersCount;
    public static int spearmenCount;
    public static int cavalryCount;

    public static Sprite knightSkin;
    public static Sprite archerSkin;
    public static Sprite spearmanSkin;
    public static Sprite cavalrySkin;

    public static int enemyGuards;
    public static int enemyArchers;
    public static int enemySpearmen;
    public static int enemyCavalry;

    public static int spentGold;
    public static string campId;
    public static int campLevel;
    public static string campName;

    public static int rewardGold;
    public static int rewardWood;
    public static int rewardStone;

    public static bool isReturningFromBattle = false;
    public static bool lastBattleWon = false;

    // Прапорці використання найманців у конкретному бою
    public static bool useMercKnights = false;
    public static bool useMercArchers = false;
    public static bool useMercSpearmen = false;
    public static bool useMercCavalry = false;
}