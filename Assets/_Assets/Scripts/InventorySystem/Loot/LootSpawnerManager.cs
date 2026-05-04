using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class LootSpawnerManager : NetworkBehaviour
{
    public static LootSpawnerManager Instance { get; private set; }

    public GameObject lootPrefab;

    [Header("Scatter")]
    [SerializeField, Tooltip("Maximum scatter radius from block center")]
    private float scatterRadius = 0.45f;

    [SerializeField, Tooltip("Random loot rotation on spawn")]
    private bool randomRotation = true;

    private bool IsOnlineMode
    {
        get
        {
            return NetworkManager.Singleton != null &&
                   NetworkManager.Singleton.IsListening;
        }
    }

    private bool CanSpawnAuthoritatively
    {
        get
        {
            if (!IsOnlineMode)
                return true;

            return IsServer;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnLootForBlock(MapBlockData data, Vector2 position, int? seed = null)
    {
        if (!CanSpawnAuthoritatively)
            return;

        if (data == null || data.loot == null || data.loot.Count == 0)
            return;

        if (lootPrefab == null)
        {
            Debug.LogError("[LootSpawner] lootPrefab is missing.");
            return;
        }

        Vector3 basePos = position;
        var rng = seed.HasValue
            ? new System.Random(seed.Value)
            : new System.Random(CombineHash(position, Time.frameCount));

        foreach (var rule in data.loot)
        {
            if (rule.item == null)
                continue;

            if (rng.NextDouble() * 100 > rule.chance)
                continue;

            int count = Mathf.Clamp(Random.Range(rule.min, rule.max + 1), 1, 100);

            float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float golden = 2.39996323f;

            for (int i = 0; i < count; i++)
            {
                angle += golden + (float)rng.NextDouble() * 0.15f;

                float r = Mathf.Sqrt((float)rng.NextDouble()) * scatterRadius;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;

                Vector3 spawnPos = basePos + new Vector3(offset.x, offset.y, 0f);
                Quaternion rotation = randomRotation ? Random.rotationUniform : Quaternion.identity;

                GameObject lootObject = SpawnOne(lootPrefab, spawnPos, rotation);

                if (lootObject == null)
                    continue;

                LootObject loot = lootObject.GetComponent<LootObject>();

                if (loot == null)
                {
                    Debug.LogError("[LootSpawner] Spawned loot object has no LootObject component.");
                    continue;
                }

                if (IsOnlineMode)
                    loot.InitServer(rule.item.GetItemID(), 1);
                else
                    loot.InitOffline(rule.item.GetItemID(), 1);
            }
        }
    }

    private GameObject SpawnOne(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        GameObject go = Instantiate(prefab, pos, rot);

        if (IsOnlineMode)
        {
            NetworkObject networkObject = go.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError($"[LootSpawner] Online loot prefab '{prefab.name}' needs NetworkObject.");
                Destroy(go);
                return null;
            }

            networkObject.Spawn(true);
        }

        return go;
    }

    private static int CombineHash(Vector2 a, int b)
    {
        unchecked
        {
            int hx = a.x.GetHashCode();
            int hy = a.y.GetHashCode();

            return (hx * 73856093) ^ (hy * 19349663) ^ (b * 83492791);
        }
    }
}