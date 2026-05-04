using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LootObject : NetworkBehaviour, IInteractable
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer sr;

    private string offlineItemId;
    private int offlineAmount = 1;
    private bool initializedOffline = false;

    private readonly NetworkVariable<FixedString64Bytes> itemIdNet =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<int> amountNet =
        new NetworkVariable<int>(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private bool IsOnlineMode
    {
        get
        {
            return NetworkManager.Singleton != null &&
                   NetworkManager.Singleton.IsListening;
        }
    }

    private string CurrentItemId
    {
        get
        {
            if (IsOnlineMode)
                return itemIdNet.Value.ToString();

            return offlineItemId;
        }
    }

    private int CurrentAmount
    {
        get
        {
            if (IsOnlineMode)
                return amountNet.Value;

            return offlineAmount;
        }
    }

    private void Awake()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        itemIdNet.OnValueChanged += OnNetworkDataChanged;
        amountNet.OnValueChanged += OnNetworkDataChanged;

        RefreshVisual();
    }

    public override void OnNetworkDespawn()
    {
        itemIdNet.OnValueChanged -= OnNetworkDataChanged;
        amountNet.OnValueChanged -= OnNetworkDataChanged;
    }

    private void OnNetworkDataChanged<T>(T oldValue, T newValue)
    {
        RefreshVisual();
    }

    public void InitServer(string itemId, int amount)
    {
        if (!IsServer)
            return;

        itemIdNet.Value = itemId;
        amountNet.Value = Mathf.Max(1, amount);

        RefreshVisual();
    }

    public void InitOffline(string itemId, int amount)
    {
        offlineItemId = itemId;
        offlineAmount = Mathf.Max(1, amount);
        initializedOffline = true;

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        if (sr == null)
            return;

        string itemId = CurrentItemId;

        if (string.IsNullOrEmpty(itemId))
        {
            sr.sprite = null;
            return;
        }

        if (ItemDatabase.instance == null)
        {
            Debug.LogWarning("[LootObject] ItemDatabase.instance is missing.");
            return;
        }

        ItemData itemData = ItemDatabase.instance.GetItem(itemId);

        sr.sprite = itemData != null ? itemData.itemIcon : null;
    }

    public void OnInteract(GameObject interactor, ulong interacterId)
    {
        if (IsOnlineMode)
        {
            OnInteractOnline(interactor, interacterId);
            return;
        }

        OnInteractOffline(interactor);
    }

    private void OnInteractOffline(GameObject interactor)
    {
        if (!initializedOffline)
        {
            Debug.LogWarning("[LootObject] Offline loot was not initialized.");
            return;
        }

        if (interactor == null)
            return;

        Inventory inventory = interactor.GetComponent<Inventory>();

        if (inventory == null)
            inventory = interactor.GetComponentInChildren<Inventory>();

        if (inventory == null)
        {
            Debug.LogWarning("[LootObject] Interactor has no Inventory.");
            return;
        }

        inventory.AddItem(offlineItemId, offlineAmount);

        Destroy(gameObject);
    }

    private void OnInteractOnline(GameObject interactor, ulong interacterId)
    {
        if (interactor == null)
            return;

        if (!interactor.TryGetComponent<NetworkBehaviour>(out NetworkBehaviour networkBehaviour))
            return;

        if (networkBehaviour.NetworkObject == null)
            return;

        InteractRequestServerRpc(networkBehaviour.NetworkObjectId, interacterId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractRequestServerRpc(ulong interactorNetworkObjectId, ulong clientId)
    {
        if (!IsServer)
            return;

        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                interactorNetworkObjectId,
                out NetworkObject interactorNetworkObject
            ))
        {
            Debug.LogWarning($"[LootObject] Interactor {interactorNetworkObjectId} was not found.");
            return;
        }

        PlayerManager playerManager = interactorNetworkObject.GetComponent<PlayerManager>();

        if (playerManager == null)
        {
            Debug.LogWarning("[LootObject] Interactor has no PlayerManager.");
            return;
        }

        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };

        playerManager.AddItemClientRpc(CurrentItemId, CurrentAmount, target);

        NetworkObject.Despawn(true);
    }
}