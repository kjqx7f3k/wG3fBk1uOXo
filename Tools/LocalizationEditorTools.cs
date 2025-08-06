#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 本地化編輯器工具 - 提供本地化相關的開發輔助功能
/// </summary>
public class LocalizationEditorTools : EditorWindow
{
    private Vector2 scrollPosition;
    private List<TextMeshProUGUI> sceneTextComponents = new List<TextMeshProUGUI>();
    private string searchFilter = "";
    private bool autoRefresh = true;
    
    [MenuItem("Window/Localization Tools")]
    public static void ShowWindow()
    {
        GetWindow<LocalizationEditorTools>("Localization Tools");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        
        DrawHeader();
        DrawTextComponentScanner();
        DrawFontManagerTools();
        DrawDialogTools();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Localization Tools", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
        EditorGUILayout.Space();
    }
    
    private void DrawTextComponentScanner()
    {
        EditorGUILayout.LabelField("Text Component Scanner", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        searchFilter = EditorGUILayout.TextField("Search Filter", searchFilter);
        if (GUILayout.Button("Scan Scene", GUILayout.Width(100)))
        {
            ScanSceneForTextComponents();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField($"Found {sceneTextComponents.Count} TextMeshProUGUI components");
        
        if (sceneTextComponents.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add LocalizedFontUpdater to All"))
            {
                AddLocalizedFontUpdaterToAll();
            }
            if (GUILayout.Button("Remove LocalizedFontUpdater from All"))
            {
                RemoveLocalizedFontUpdaterFromAll();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        foreach (var textComponent in sceneTextComponents)
        {
            if (textComponent == null) continue;
            
            EditorGUILayout.BeginHorizontal();
            
            // 顯示物件名稱和文字內容
            string displayText = string.IsNullOrEmpty(textComponent.text) ? "[Empty]" : textComponent.text;
            if (displayText.Length > 30)
                displayText = displayText.Substring(0, 30) + "...";
                
            EditorGUILayout.LabelField($"{textComponent.gameObject.name}: {displayText}");
            
            // 檢查是否已有 LocalizedFontUpdater
            bool hasUpdater = textComponent.GetComponent<LocalizedFontUpdater>() != null;
            EditorGUILayout.LabelField(hasUpdater ? "✓" : "✗", GUILayout.Width(20));
            
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeGameObject = textComponent.gameObject;
            }
            
            if (GUILayout.Button(hasUpdater ? "Remove" : "Add", GUILayout.Width(60)))
            {
                if (hasUpdater)
                {
                    RemoveLocalizedFontUpdater(textComponent);
                }
                else
                {
                    AddLocalizedFontUpdater(textComponent);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
    }
    
    private void DrawFontManagerTools()
    {
        EditorGUILayout.LabelField("Font Manager Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Find FontManager in Scene"))
        {
            var fontManager = FindObjectOfType<FontManager>();
            if (fontManager != null)
            {
                Selection.activeGameObject = fontManager.gameObject;
                EditorUtility.DisplayDialog("Font Manager", $"Found FontManager on: {fontManager.gameObject.name}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Font Manager", "No FontManager found in scene.", "OK");
            }
        }
        
        if (GUILayout.Button("Create FontManager"))
        {
            CreateFontManager();
        }
        
        EditorGUILayout.Space();
    }
    
    private void DrawDialogTools()
    {
        EditorGUILayout.LabelField("Dialog Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Find DialogManager in Scene"))
        {
            var dialogManager = FindObjectOfType<DialogManager>();
            if (dialogManager != null)
            {
                Selection.activeGameObject = dialogManager.gameObject;
                EditorUtility.DisplayDialog("Dialog Manager", $"Found DialogManager on: {dialogManager.gameObject.name}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Dialog Manager", "No DialogManager found in scene.", "OK");
            }
        }
        
        if (GUILayout.Button("Create LocalizedDialogManager"))
        {
            Debug.Log("No LocalizedDialogManager...");
        }
        
        EditorGUILayout.Space();
    }
    
    private void ScanSceneForTextComponents()
    {
        sceneTextComponents.Clear();
        var allTextComponents = FindObjectsOfType<TextMeshProUGUI>(true);
        
        foreach (var textComponent in allTextComponents)
        {
            if (string.IsNullOrEmpty(searchFilter) || 
                textComponent.gameObject.name.ToLower().Contains(searchFilter.ToLower()) ||
                textComponent.text.ToLower().Contains(searchFilter.ToLower()))
            {
                sceneTextComponents.Add(textComponent);
            }
        }
        
        Debug.Log($"[LocalizationEditorTools] 掃描完成，找到 {sceneTextComponents.Count} 個 TextMeshProUGUI 組件");
    }
    
    private void AddLocalizedFontUpdaterToAll()
    {
        int count = 0;
        foreach (var textComponent in sceneTextComponents)
        {
            if (textComponent != null && AddLocalizedFontUpdater(textComponent))
            {
                count++;
            }
        }
        
        EditorUtility.DisplayDialog("Add Components", $"已為 {count} 個組件添加 LocalizedFontUpdater", "OK");
        ScanSceneForTextComponents(); // 重新掃描
    }
    
    private void RemoveLocalizedFontUpdaterFromAll()
    {
        int count = 0;
        foreach (var textComponent in sceneTextComponents)
        {
            if (textComponent != null && RemoveLocalizedFontUpdater(textComponent))
            {
                count++;
            }
        }
        
        EditorUtility.DisplayDialog("Remove Components", $"已從 {count} 個組件移除 LocalizedFontUpdater", "OK");
        ScanSceneForTextComponents(); // 重新掃描
    }
    
    private bool AddLocalizedFontUpdater(TextMeshProUGUI textComponent)
    {
        if (textComponent.GetComponent<LocalizedFontUpdater>() == null)
        {
            Undo.AddComponent<LocalizedFontUpdater>(textComponent.gameObject);
            EditorUtility.SetDirty(textComponent.gameObject);
            return true;
        }
        return false;
    }
    
    private bool RemoveLocalizedFontUpdater(TextMeshProUGUI textComponent)
    {
        var updater = textComponent.GetComponent<LocalizedFontUpdater>();
        if (updater != null)
        {
            Undo.DestroyObjectImmediate(updater);
            EditorUtility.SetDirty(textComponent.gameObject);
            return true;
        }
        return false;
    }
    
    private void CreateFontManager()
    {
        var existing = FindObjectOfType<FontManager>();
        if (existing != null)
        {
            if (EditorUtility.DisplayDialog("FontManager Exists", 
                "A FontManager already exists in the scene. Select it?", "Yes", "No"))
            {
                Selection.activeGameObject = existing.gameObject;
            }
            return;
        }
        
        var go = new GameObject("FontManager");
        go.AddComponent<FontManager>();
        Selection.activeGameObject = go;
        
        Undo.RegisterCreatedObjectUndo(go, "Create FontManager");
        EditorUtility.DisplayDialog("FontManager Created", 
            "FontManager has been created. Please configure the language font settings in the inspector.", "OK");
    }
    

    
    private void OnFocus()
    {
        if (autoRefresh)
        {
            ScanSceneForTextComponents();
        }
    }
}

/// <summary>
/// 自定義屬性繪製器，用於在 Inspector 中添加便捷按鈕
/// </summary>
[CustomEditor(typeof(LocalizedFontUpdater))]
public class LocalizedFontUpdaterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        var updater = (LocalizedFontUpdater)target;
        
        if (GUILayout.Button("Update Font Now"))
        {
            updater.UpdateFont();
        }
        
        if (GUILayout.Button("Clear Override Language"))
        {
            updater.ClearOverrideLanguageCode();
        }
    }
}

/// <summary>
/// FontManager 的自定義編輯器
/// </summary>
[CustomEditor(typeof(FontManager))]
public class FontManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        var fontManager = (FontManager)target;
        
        if (GUILayout.Button("Scan and Register All Text Components"))
        {
            var allTextComponents = FindObjectsOfType<TextMeshProUGUI>();
            int count = 0;
            foreach (var textComponent in allTextComponents)
            {
                fontManager.RegisterTextComponent(textComponent);
                count++;
            }
            EditorUtility.DisplayDialog("Registration Complete", 
                $"已註冊 {count} 個 TextMeshProUGUI 組件到 FontManager", "OK");
        }
        
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField($"Current Language: {fontManager.GetCurrentLanguageCode()}");
            
            var availableLanguages = fontManager.GetAvailableLanguages();
            foreach (var lang in availableLanguages)
            {
                if (GUILayout.Button($"Switch to {lang}"))
                {
                    fontManager.ApplyFontForLanguage(lang);
                }
            }
        }
    }
}
#endif