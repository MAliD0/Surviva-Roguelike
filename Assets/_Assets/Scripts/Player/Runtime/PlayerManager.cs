using Cinemachine;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public class PlayerManager : NetworkBehaviour
{
    [SerializeField] private PlayerController playerMovement;
    [SerializeField] private PlayerAnimationController playerAnimationController;
    [SerializeField] private PlayerInputManager playerInputManager;
    [SerializeField] private PlayerInteractionManager playerInteractionManager;

    [SerializeField] private Inventory inventory;
    [SerializeField] private DSManager dsManager;
    [SerializeField] private Rigidbody2D rb;

    [Header("Test Fields")]
    [SerializeField] private int damage;
    [SerializeField] private ItemSlot itemHeld;

    [Header("Settings")]
    [SerializeField] private int maxMessageLength = 36;
    [SerializeField] private float castRadius;

    private void Start()
    {
        if (!HasLocalControl)
            return;

        InitLocalPlayer();
    }
    private void OnDestroy()
    {
        if (!HasLocalControl)
            return;

        if (MessageMenu.instance != null)
            MessageMenu.instance.onMessageSent -= OnMessageSent;

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.onItemIndexSelected -= OnItemIndexSelected;

        if (playerInputManager != null)
        {
            playerInputManager.onMouseLeftPress -= OnLeftClick;
            playerInputManager.onMouseRightPress -= OnRightClick;
            playerInputManager.onInteractPress -= OnInteractPressed;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!HasLocalControl)
            return;

        SetupCamera();
    }
    
    private void InitLocalPlayer()
    {
        playerMovement.rb = rb;

        if (MessageMenu.instance != null)
            MessageMenu.instance.onMessageSent += OnMessageSent;

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ConnectInventory(inventory);
            InventoryUI.Instance.onItemIndexSelected += OnItemIndexSelected;
        }

        playerInputManager.onMouseLeftPress += OnLeftClick;
        playerInputManager.onMouseRightPress += OnRightClick;
        playerInputManager.onInteractPress += OnInteractPressed;

        SetupCamera();
    }

    private void SetupCamera()
    {
        var vcam = FindObjectOfType<Cinemachine.CinemachineVirtualCamera>();

        if (vcam != null)
        {
            vcam.Follow = transform;
            vcam.LookAt = transform;
        }
    }
    private void OnItemIndexSelected(int index)
    {
        ItemSlot itemSlot = inventory.GetInventoryItems()[index];
        itemHeld = itemSlot;
    }

    private void OnLeftClick(Vector2 worldPosition)
    {
        if (itemHeld.itemData == null)
            return;

        MapBlockData blockData = itemHeld.itemData as MapBlockData;

        if (blockData == null)
            return;

        if (WorldMapManager.Instance == null)
            return;

        bool placementStarted = WorldMapManager.Instance.TryPlaceBlock(
            worldPosition,
            blockData.GetItemID()
        );

        if (placementStarted)
        {
            // For online clients this is still optimistic.
            // Long-term: remove item only after server confirms placement.
            inventory.RemoveItem(blockData, 1);
        }
    }

    private void OnRightClick(Vector2 worldPosition)
    {
        if (WorldMapManager.Instance == null)
            return;

        WorldMapManager.Instance.TryDamageBlock(worldPosition, damage);
    }

    private void OnInteractPressed()
    {
        IInteractable[] interactables =
            playerInteractionManager.CastForInteractables(transform.position, castRadius);

        Debug.Log($"Interactables found: {interactables.Length}");

        for (int i = 0; i < interactables.Length; i++)
        {
            Debug.Log($"Interactable[{i}] = {interactables[i].GetType().Name}");
        }

        if (interactables.Length <= 0)
            return;

        interactables[0].OnInteract(gameObject, OwnerClientId);
    }

    private void OnMessageSent(string text)
    {
        if (dsManager.dsStarted)
            return;

        MessageServerRpc(text);
    }

    private void Update()
    {
        if (!HasLocalControl)
            return;

        HandleMovement();
        HandleGhostBuilding();
    }

    private void HandleMovement()
    {
        float inputX = playerInputManager.movementDirection.x;
        float inputY = playerInputManager.movementDirection.y;

        if (inputX != 0)
            playerAnimationController.FlipSprite(inputX > 0);

        playerAnimationController.SetBool("IsWalking", inputX != 0 || inputY != 0);
        playerMovement.Move(inputX, inputY);
    }

    private void HandleGhostBuilding()
    {
        if (itemHeld.itemData is MapBlockData blockData)
        {
            if (GhostBuildManager.Instance != null)
            {
                GhostBuildManager.Instance.PlaceGhost(
                    playerInputManager.mouseWorldPosition,
                    blockData
                );
            }

            return;
        }

        if (GhostBuildManager.Instance != null)
            GhostBuildManager.Instance.ClearGhost();
    }

    [ClientRpc]
    private void MessageClientRpc(string text)
    {
        Debug.Log("Enter Client RPC: " + text);
        dsManager.SayText(text);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MessageServerRpc(string text)
    {
        string limitedText = text.Substring(0, Mathf.Min(text.Length, maxMessageLength));
        MessageClientRpc(limitedText);
    }

    [ClientRpc]
    public void AddItemClientRpc(string itemId, int number, ClientRpcParams rpcParams)
    {
        inventory.AddItem(itemId, number);
    }
    public void AddItem(ItemData itemData, int number)
    {
        inventory.AddItem(itemData, number);
    }
    public void RemoveItem(ItemData itemData, int number)
    {
        inventory.RemoveItem(itemData, number);
    }

    public ItemSlot GetCurrentHeldItem()
    {
        return itemHeld;
    }

    private bool HasLocalControl
    {
        get
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return true;

            return IsOwner;
        }
    }
}