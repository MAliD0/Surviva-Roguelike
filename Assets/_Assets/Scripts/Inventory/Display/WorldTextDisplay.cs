using TMPro;
using UnityEngine;

public class WorldTextDisplay : MonoBehaviour
{
    [SerializeField] TextMeshPro textPrefab;

    void Start()
    {
        if(textPrefab == null){
            if(TryGetComponent(out TextMeshPro component))
                textPrefab = component;
        }
    }

    public void SetText(string text)
    {
        if(text == "")
        {
            textPrefab.text = "";
            textPrefab.enabled = false;
        }
        else
        {
            textPrefab.text = text;
            textPrefab.enabled = true;    
        }
    }
}
