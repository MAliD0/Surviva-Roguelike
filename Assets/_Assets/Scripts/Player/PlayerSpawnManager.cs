using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject offlinePlayerPrefab;

    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;

    private GameObject spawnedOfflinePlayer;

    private void Start()
    {
        if (IsOnlineMode())
            return;

        SpawnOfflinePlayer();
    }

    private bool IsOnlineMode()
    {
        return NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening;
    }

    private void SpawnOfflinePlayer()
    {
        if (offlinePlayerPrefab == null)
        {
            Debug.LogError("[PlayerSpawnManager] Offline player prefab is missing.");
            return;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        spawnedOfflinePlayer = Instantiate(offlinePlayerPrefab, position, rotation);
    }
}