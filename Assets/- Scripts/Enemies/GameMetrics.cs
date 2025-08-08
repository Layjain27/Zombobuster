// Filename: GameMetrics.cs
using UnityEngine;

public static class GameMetrics
{
    // You can adjust this global cap in your code.
    public const int GLOBAL_MAX_ACTIVE_ENEMIES = 50;

    public static int totalActiveEnemies = 0;

    // --- NEW: Tracks the health reduction percentage for newly spawned enemies. ---
    public static float enemyHealthDebuffPercentage = 0f;

    public static void ResetMetrics()
    {
        totalActiveEnemies = 0;
        // --- NEW: Ensure the debuff is reset when the game starts. ---
        enemyHealthDebuffPercentage = 0f;
    }
}