using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using UnityEngine.WSA;
using ProceduralGeneration;

public class TileCreator : MonoBehaviour
{
    [FoldoutGroup("Settings")][SerializeField] private string tileAssetPath = "Assets/_Assets/Prefabs/Resources/Tiles";
    [FoldoutGroup("Settings")][SerializeField] private string itemAssetPath = "Assets/_Assets/Prefabs/Resources/Items";
    [FoldoutGroup("Settings")][SerializeField] private ItemLibrary itemLibrary;
    [FoldoutGroup("Settings")][SerializeField] private MapBlockDataLibrary mapBlockDataLibrary;

    [FoldoutGroup("Settings")][SerializeField] GameObject basicPrefab;

    [SerializeField] private AssetType assetType = AssetType.MapBlock;
    
    #region New Map Block Data Fields
    [FoldoutGroup("New Map Block Data")][SerializeField][PreviewField][HideLabel] Sprite tileSprite;
    [FoldoutGroup("New Map Block Data")][SerializeField] MapBlockType mapBlockType = MapBlockType.Tile;
    [FoldoutGroup("New Map Block Data")][SerializeField] MapLayerType mapLayerType;
    [FoldoutGroup("New Map Block Data")][SerializeField] string tileDataName = "NewTileData";
    #endregion
    
    #region New Item Data Fields
    [FoldoutGroup("New Item Data")][SerializeField] string itemDataName = "NewItemData";
    [FoldoutGroup("New Item Data")][SerializeField][PreviewField][HideLabel] Sprite itemSprite;
    #endregion
    [Button("Create new Asset")]


    public void CreateTileData()
    {
        string path = "";

        switch (assetType)
        {
            case AssetType.MapBlock:
                path = tileAssetPath;
                break;
            case AssetType.Item:
                path = itemAssetPath;
                break;
        }

        // 1) Ensure Tiles folder exists
        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogWarning($"Tiles folder is not existing, create path {path}");
        }

        // 2) Check if target folder already exists
        string newFolderPath = $"{path}/{tileDataName}";
        if (AssetDatabase.IsValidFolder(newFolderPath))
        {
            // Folder already exists → do nothing
            Debug.LogWarning("Folder with this name already exists!");
            return;
        }

        // 3) Create new folder
        AssetDatabase.CreateFolder(path, tileDataName);
        AssetDatabase.Refresh();

        // 4) Create TileData asset inside the new folder

        switch (assetType)
        {
            case AssetType.MapBlock:
                MapBlockData newTileData = CreateAsset<MapBlockData>(newFolderPath, tileDataName);

                newTileData.name = tileDataName;
                newTileData.mapBlockType = mapBlockType;
                newTileData.mapLayerType = mapLayerType;
                newTileData.itemIcon = tileSprite;
                newTileData.itemType = ItemType.Placeable;

                switch(mapBlockType)
                {
                    case MapBlockType.Tile:
                        RuleTile ruleTile = ScriptableObject.CreateInstance<RuleTile>();
                        ruleTile.name = $"{tileDataName}RuleTile";
                        ruleTile.m_DefaultSprite = tileSprite;
                        AssetDatabase.CreateAsset(ruleTile, $"{newFolderPath}/{tileDataName}RuleTile.asset");

                        newTileData.tile = ruleTile;
                        break;
                    case MapBlockType.GameObject:
                        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(basicPrefab, $"{newFolderPath}/{tileDataName}Prefab.prefab");

                        SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sprite = tileSprite;
                            sr.sortingLayerName = mapLayerType.ToString();
                        }
                        
                        newTileData.gameObject = prefab;
                        break;
                    }

                    mapBlockDataLibrary.AddData(newTileData);
                break;
            case AssetType.Item:
                ItemData newItemData = CreateAsset<ItemData>(newFolderPath, itemDataName);
                newItemData.name = itemDataName;
                newItemData.itemIcon = itemSprite;

                itemLibrary.AddData(newItemData);
                break;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    
    public static T CreateAsset<T>(string path, string fileName) where T : ScriptableObject
    {
        // Ensure path starts with Assets/
        if (!path.StartsWith("Assets"))
            path = "Assets";

        // Create instance
        T asset = ScriptableObject.CreateInstance<T>();

        string fullPath = $"{path}/{fileName}.asset";

        // Create asset
        AssetDatabase.CreateAsset(asset, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Focus it in Project window
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        return asset;
    }

    public enum AssetType
    {
        Item,
        MapBlock
    }
}
