using UnityEngine;

public class WorldLootService
{
    public void SpawnLootForBlock(MapBlockData blockData, Vector2 worldPosition)
    {
        if (blockData == null)
            return;

        if (LootSpawnerManager.Instance == null)
            return;

        LootSpawnerManager.Instance.SpawnLootForBlock(
            blockData,
            worldPosition
        );
    }
}