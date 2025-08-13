// GameMetrics.cs
public static class GameMetrics
{
    public const int GLOBAL_MAX_ACTIVE_ENEMIES = 30; // Total enemies all towers combined can have active at once
    public static int totalActiveEnemies = 0; // Tracks total active enemies in the scene

    // This static method is called by GroundedEnemy when it dies
    public static void DecrementActiveEnemies()
    {
        if (totalActiveEnemies > 0)
        {
            totalActiveEnemies--;
            // Debug.Log($"Enemy died. Total active enemies: {totalActiveEnemies}"); // Uncomment for debugging
        }
    }

    // Call this at the start of your game or scene to ensure it's reset
    public static void ResetMetrics()
    {
        totalActiveEnemies = 0;
        // Debug.Log("GameMetrics reset: totalActiveEnemies = 0."); // Uncomment for debugging
    }
}