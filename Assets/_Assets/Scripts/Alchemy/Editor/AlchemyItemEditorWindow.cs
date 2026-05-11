#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AlchemyItemEditorWindow : EditorWindow
{
    private string itemSearch = "";
    private AspectDefinition requiredAspect;
    private bool showOnlyWithAspect = false;
    private bool showOnlyMatchable = false;

    private Vector2 itemListScroll;
    private Vector2 detailsScroll;

    private List<AlchemyItemData> allItems = new();
    private AlchemyItemData selectedItem;

    private string selectedAssetName = "";
    private string newItemName = "New Alchemy Item";
    private string newItemFolder = "Assets/_Assets/Prefabs/Resources/Alchemy/Items";

    private const float IconSize = 36f;

    [MenuItem("Tools/Alchemy/Item Editor")]
    public static void Open()
    {
        AlchemyItemEditorWindow window = GetWindow<AlchemyItemEditorWindow>();
        window.titleContent = new GUIContent("Alchemy Items");
        window.minSize = new Vector2(900, 540);
        window.RefreshItems();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshItems();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawCreateItemPanel();

        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawItemListPanel();
            DrawSelectedItemPanel();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Search", GUILayout.Width(45));
            itemSearch = GUILayout.TextField(itemSearch, EditorStyles.toolbarSearchField, GUILayout.Width(220));

            GUILayout.Space(8);

            showOnlyWithAspect = GUILayout.Toggle(showOnlyWithAspect, "Has Aspect", EditorStyles.toolbarButton, GUILayout.Width(85));
            using (new EditorGUI.DisabledScope(!showOnlyWithAspect))
            {
                requiredAspect = (AspectDefinition)EditorGUILayout.ObjectField(requiredAspect, typeof(AspectDefinition), false, GUILayout.Width(180));
            }

            GUILayout.Space(8);

            showOnlyMatchable = GUILayout.Toggle(showOnlyMatchable, "Only Matchable", EditorStyles.toolbarButton, GUILayout.Width(110));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshItems();
            }
        }
    }

    private void DrawCreateItemPanel()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Create New Alchemy Item", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                newItemName = EditorGUILayout.TextField("Name", newItemName);

                if (GUILayout.Button("Create", GUILayout.Width(90)))
                {
                    CreateNewAlchemyItem();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                newItemFolder = EditorGUILayout.TextField("Folder", newItemFolder);

                if (GUILayout.Button("Pick", GUILayout.Width(90)))
                {
                    string picked = EditorUtility.OpenFolderPanel("Select item folder", Application.dataPath, "");
                    if (!string.IsNullOrWhiteSpace(picked))
                    {
                        if (picked.StartsWith(Application.dataPath))
                        {
                            newItemFolder = "Assets" + picked.Substring(Application.dataPath.Length);
                        }
                        else
                        {
                            Debug.LogWarning("Selected folder must be inside this Unity project's Assets folder.");
                        }
                    }
                }
            }
        }
    }

    private void DrawItemListPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(340)))
        {
            EditorGUILayout.LabelField($"Items ({GetFilteredItems().Count})", EditorStyles.boldLabel);

            itemListScroll = EditorGUILayout.BeginScrollView(itemListScroll, "box");

            foreach (AlchemyItemData item in GetFilteredItems())
            {
                DrawItemRow(item);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawItemRow(AlchemyItemData item)
    {
        if (item == null)
            return;

        Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(44));

        bool isSelected = selectedItem == item;
        if (isSelected)
        {
            EditorGUI.DrawRect(rowRect, new Color(0.25f, 0.45f, 0.75f, 0.25f));
        }

        Texture iconTexture = item.itemIcon != null ? item.itemIcon.texture : null;
        GUILayout.Label(iconTexture, GUILayout.Width(IconSize), GUILayout.Height(IconSize));

        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField(item.name, EditorStyles.boldLabel);

            string aspectPreview = BuildAspectPreview(item);
            EditorGUILayout.LabelField(aspectPreview, EditorStyles.miniLabel);
        }

        EditorGUILayout.EndHorizontal();

        if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
        {
            SelectItem(item);
            GUI.FocusControl(null);
            Repaint();
        }
    }

    private void SelectItem(AlchemyItemData item)
    {
        selectedItem = item;
        selectedAssetName = item != null ? item.name : "";
    }

    private void DrawSelectedItemPanel()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            if (selectedItem == null)
            {
                EditorGUILayout.HelpBox("Select an alchemy item from the list.", MessageType.Info);
                return;
            }

            detailsScroll = EditorGUILayout.BeginScrollView(detailsScroll, "box");

            SerializedObject serializedItem = new SerializedObject(selectedItem);
            serializedItem.Update();

            DrawHeader(serializedItem);

            EditorGUILayout.Space(8);
            DrawRenamePanel();

            EditorGUILayout.Space(8);
            DrawBasicItemFields(serializedItem);

            EditorGUILayout.Space(8);
            DrawAlchemyFields(serializedItem);

            EditorGUILayout.Space(8);
            DrawAspectProfile(serializedItem);

            EditorGUILayout.Space(8);
            DrawUtilityButtons(serializedItem);

            if (serializedItem.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(selectedItem);
                AssetDatabase.SaveAssetIfDirty(selectedItem);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawHeader(SerializedObject serializedItem)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            Texture iconTexture = selectedItem.itemIcon != null ? selectedItem.itemIcon.texture : null;
            GUILayout.Label(iconTexture, GUILayout.Width(64), GUILayout.Height(64));

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(selectedItem.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(selectedItem), EditorStyles.miniLabel);

                if (GUILayout.Button("Ping Asset", GUILayout.Width(100)))
                {
                    EditorGUIUtility.PingObject(selectedItem);
                    Selection.activeObject = selectedItem;
                }
            }
        }
    }

    private void DrawRenamePanel()
    {
        EditorGUILayout.LabelField("Asset Name", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            selectedAssetName = EditorGUILayout.TextField("Name", selectedAssetName);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(selectedAssetName) || selectedAssetName == selectedItem.name))
            {
                if (GUILayout.Button("Rename Asset", GUILayout.Width(110)))
                {
                    RenameSelectedAsset(selectedAssetName);
                }
            }
        }

        EditorGUILayout.HelpBox("This renames the ScriptableObject asset file and its Unity object name. Editing SerializedObject fields alone does not rename an asset.", MessageType.None);
    }

    private void DrawBasicItemFields(SerializedObject serializedItem)
    {
        EditorGUILayout.LabelField("Base Item Data", EditorStyles.boldLabel);

        DrawPropertyIfExists(serializedItem, "itemIcon");
        DrawPropertyIfExists(serializedItem, "itemType");
        DrawPropertyIfExists(serializedItem, "isStackable");
        DrawPropertyIfExists(serializedItem, "maxStack");
        DrawPropertyIfExists(serializedItem, "description");
    }

    private void DrawAlchemyFields(SerializedObject serializedItem)
    {
        EditorGUILayout.LabelField("Alchemy Data", EditorStyles.boldLabel);

        DrawIntPropertyIfExists(serializedItem, "complexity");
        DrawPropertyIfExists(serializedItem, "excludeFromNormalMatching");
        DrawPropertyIfExists(serializedItem, "category");
        DrawPropertyIfExists(serializedItem, "alchemyDescription");
    }

    private void DrawAspectProfile(SerializedObject serializedItem)
    {
        EditorGUILayout.LabelField("Aspect Profile", EditorStyles.boldLabel);

        SerializedProperty aspectProfile = serializedItem.FindProperty("aspectProfile");
        if (aspectProfile == null)
        {
            EditorGUILayout.HelpBox("No aspectProfile field found.", MessageType.Warning);
            return;
        }

        SerializedProperty aspects = aspectProfile.FindPropertyRelative("aspects");
        if (aspects == null)
        {
            EditorGUILayout.HelpBox("AspectProfile.aspects field is not visible. Make sure the list is serialized.", MessageType.Warning);
            return;
        }

        for (int i = 0; i < aspects.arraySize; i++)
        {
            SerializedProperty stack = aspects.GetArrayElementAtIndex(i);
            SerializedProperty aspect = stack.FindPropertyRelative("aspect");
            SerializedProperty amount = stack.FindPropertyRelative("amount");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(aspect, GUIContent.none, GUILayout.MinWidth(180));
                DrawAmountField(amount, GUILayout.Width(80));

                if (GUILayout.Button("-", GUILayout.Width(24)))
                {
                    aspects.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Aspect"))
            {
                int index = aspects.arraySize;
                aspects.InsertArrayElementAtIndex(index);

                SerializedProperty newStack = aspects.GetArrayElementAtIndex(index);
                SerializedProperty newAspect = newStack.FindPropertyRelative("aspect");
                SerializedProperty newAmount = newStack.FindPropertyRelative("amount");

                newAspect.objectReferenceValue = null;
                newAmount.floatValue = 1f;
            }

            if (GUILayout.Button("Normalize / Merge Duplicates"))
            {
                serializedItem.ApplyModifiedProperties();
                NormalizeSelectedItemAspects();
                serializedItem.Update();
            }
        }
    }

    private void DrawAmountField(SerializedProperty amountProperty, params GUILayoutOption[] options)
    {
        if (amountProperty == null)
            return;

        switch (amountProperty.propertyType)
        {
            case SerializedPropertyType.Integer:
            {
                int value = amountProperty.intValue;
                value = EditorGUILayout.IntField(value, options);
                amountProperty.intValue = Mathf.Max(0, value);
                break;
            }

            case SerializedPropertyType.Float:
            {
                int value = Mathf.RoundToInt(amountProperty.floatValue);
                value = EditorGUILayout.IntField(value, options);
                amountProperty.floatValue = Mathf.Max(0, value);
                break;
            }

            default:
                EditorGUILayout.PropertyField(amountProperty, GUIContent.none, options);
                break;
        }
    }

    private void DrawUtilityButtons(SerializedObject serializedItem)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Save Asset"))
            {
                serializedItem.ApplyModifiedProperties();
                EditorUtility.SetDirty(selectedItem);
                AssetDatabase.SaveAssetIfDirty(selectedItem);
            }

            if (GUILayout.Button("Validate Item"))
            {
                ValidateSelectedItem();
            }
        }
    }

    private void DrawPropertyIfExists(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, true);
        }
    }

    private void DrawIntPropertyIfExists(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            return;

        if (property.propertyType == SerializedPropertyType.Integer)
        {
            property.intValue = EditorGUILayout.IntField(ObjectNames.NicifyVariableName(propertyName), property.intValue);
        }
        else
        {
            EditorGUILayout.PropertyField(property, true);
        }
    }

    private List<AlchemyItemData> GetFilteredItems()
    {
        IEnumerable<AlchemyItemData> query = allItems.Where(item => item != null);

        if (!string.IsNullOrWhiteSpace(itemSearch))
        {
            string lower = itemSearch.ToLowerInvariant();
            query = query.Where(item => item.name.ToLowerInvariant().Contains(lower));
        }

        if (showOnlyWithAspect && requiredAspect != null)
        {
            query = query.Where(item => ItemHasAspect(item, requiredAspect));
        }

        if (showOnlyMatchable)
        {
            query = query.Where(item => !item.excludeFromNormalMatching);
        }

        return query.OrderBy(item => item.name).ToList();
    }

    private bool ItemHasAspect(AlchemyItemData item, AspectDefinition aspect)
    {
        if (item == null || item.aspectProfile == null || aspect == null)
            return false;

        return item.aspectProfile.GetAmount(aspect) > 0f;
    }

    private string BuildAspectPreview(AlchemyItemData item)
    {
        if (item == null || item.aspectProfile == null || item.aspectProfile.Aspects == null)
            return "No aspects";

        List<string> parts = new();

        foreach (AspectStack stack in item.aspectProfile.Aspects)
        {
            if (stack == null || stack.aspect == null || stack.amount <= 0f)
                continue;

            string aspectName = string.IsNullOrWhiteSpace(stack.aspect.displayName)
                ? stack.aspect.name
                : stack.aspect.displayName;

            parts.Add($"{aspectName}: {stack.amount}");
        }

        return parts.Count == 0 ? "No aspects" : string.Join(", ", parts);
    }

    private void RefreshItems()
    {
        allItems.Clear();

        string[] guids = AssetDatabase.FindAssets("t:AlchemyItemData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AlchemyItemData item = AssetDatabase.LoadAssetAtPath<AlchemyItemData>(path);

            if (item != null)
                allItems.Add(item);
        }

        allItems = allItems.OrderBy(item => item.name).ToList();
    }

    private void CreateNewAlchemyItem()
    {
        if (string.IsNullOrWhiteSpace(newItemName))
        {
            Debug.LogWarning("New item name is empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(newItemFolder) || !newItemFolder.StartsWith("Assets"))
        {
            Debug.LogWarning("New item folder must be inside Assets.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(newItemFolder))
        {
            Directory.CreateDirectory(newItemFolder);
            AssetDatabase.Refresh();
        }

        string safeName = SanitizeFileName(newItemName);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{newItemFolder}/{safeName}.asset");

        AlchemyItemData item = CreateInstance<AlchemyItemData>();
        item.name = safeName;
        item.isStackable = true;
        item.maxStack = 24;
        item.complexity = 1;
        item.excludeFromNormalMatching = false;

        AssetDatabase.CreateAsset(item, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshItems();
        SelectItem(item);

        EditorGUIUtility.PingObject(item);
        Selection.activeObject = item;
    }

    private void RenameSelectedAsset(string newName)
    {
        if (selectedItem == null)
            return;

        if (string.IsNullOrWhiteSpace(newName))
            return;

        string safeName = SanitizeFileName(newName);
        string path = AssetDatabase.GetAssetPath(selectedItem);

        if (string.IsNullOrWhiteSpace(path))
        {
            selectedItem.name = safeName;
            EditorUtility.SetDirty(selectedItem);
            return;
        }

        Undo.RecordObject(selectedItem, "Rename Alchemy Item Asset");
        selectedItem.name = safeName;
        EditorUtility.SetDirty(selectedItem);

        string error = AssetDatabase.RenameAsset(path, safeName);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogWarning($"Could not rename asset: {error}", selectedItem);
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshItems();
        SelectItem(selectedItem);
    }

    private string SanitizeFileName(string fileName)
    {
        string safeName = fileName.Trim();

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidChar.ToString(), "");
        }

        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "New Alchemy Item";

        return safeName;
    }

    private void NormalizeSelectedItemAspects()
    {
        if (selectedItem == null || selectedItem.aspectProfile == null)
            return;

        Undo.RecordObject(selectedItem, "Normalize Aspect Profile");

        Dictionary<AspectDefinition, float> merged = new();

        foreach (AspectStack stack in selectedItem.aspectProfile.Aspects)
        {
            if (stack == null || stack.aspect == null || stack.amount <= 0f)
                continue;

            if (!merged.ContainsKey(stack.aspect))
                merged.Add(stack.aspect, 0f);

            merged[stack.aspect] += stack.amount;
        }

        selectedItem.aspectProfile.Clear();

        foreach (KeyValuePair<AspectDefinition, float> pair in merged)
        {
            selectedItem.aspectProfile.AddAspect(pair.Key, (int)pair.Value);
        }

        EditorUtility.SetDirty(selectedItem);
        AssetDatabase.SaveAssetIfDirty(selectedItem);
    }

    private void ValidateSelectedItem()
    {
        if (selectedItem == null)
            return;

        List<string> warnings = new();

        if (selectedItem.aspectProfile == null)
            warnings.Add("Missing aspect profile.");
        else if (selectedItem.aspectProfile.IsEmpty())
            warnings.Add("Aspect profile is empty.");

        if (selectedItem is ResidueItemData && !selectedItem.excludeFromNormalMatching)
            warnings.Add("Residue should be excluded from normal matching.");

        if (selectedItem.maxStack <= 0)
            warnings.Add("Max stack should be greater than 0.");

        if (selectedItem.complexity < 0)
            warnings.Add("Complexity should not be negative.");

        if (warnings.Count == 0)
        {
            Debug.Log($"[Alchemy Item Editor] {selectedItem.name}: OK", selectedItem);
        }
        else
        {
            Debug.LogWarning($"[Alchemy Item Editor] {selectedItem.name}:\n- " + string.Join("\n- ", warnings), selectedItem);
        }
    }
}
#endif
