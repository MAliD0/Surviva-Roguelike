using Unity.VisualScripting;
using UnityEngine;

public class ItemDisplay : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] IItemHolder itemHolder;
    [SerializeField] WorldTextDisplay worldTextDisplay;
    bool displayTextIfNoSprite = false; 

    void Start()
    {
        if(spriteRenderer == null){
            if(TryGetComponent(out SpriteRenderer component))
                spriteRenderer = component;
        }

        if(itemHolder == null){
            if(TryGetComponent(out IItemHolder component))
                itemHolder = component;
        }

        worldTextDisplay = GetComponentInChildren<WorldTextDisplay>();
        if(worldTextDisplay != null)
        {
            displayTextIfNoSprite = true;
        }

        itemHolder.OnItemChanged += OnItemChanged; 
    }

    private void OnItemChanged(ItemSlot itemSlot)
    {
        if (itemHolder.IsEmpty)
        {
            if (displayTextIfNoSprite)
            {
                worldTextDisplay.SetText("");
            }
            else
            {
                spriteRenderer.sprite = null;
                spriteRenderer.enabled = false;    
            }
        }
        else
        {
            if(displayTextIfNoSprite && itemSlot.itemData.itemIcon == null)
            {
                worldTextDisplay.SetText(itemSlot.GetDebugDescription());
            }
            else
            {
                spriteRenderer.sprite = itemSlot.itemData.itemIcon;
                spriteRenderer.enabled = true;    
            }
            
        }
    }
}
