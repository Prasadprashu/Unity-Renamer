using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PrashuRenameTool
{
    public enum NumberedMethod
    {
        BySelection = 0,
        ByHierarchy = 1
    }

    public enum CaseMethod
    {
        None = 0,
        Uppercase = 1,
        Lowercase = 2,
        TitleCase = 3,
        CamelCase = 4,
        PascalCase = 5
    }

    public enum SortMethod
    {
        None = 0,
        Alphabetical = 1,
        ReverseAlphabetical = 2,
        Length = 3
    }

    public enum TrimPosition
    {
        Beginning = 0,
        End = 1
    }

    [Serializable]
    public class PrashuRenameToolSettings
    {
        public bool useBasename = false;
        public string basename = "";
        public bool usePrefix = false;
        public string prefix = "";
        public bool useSuffix = false;
        public string suffix = "";
        
        public bool useNumbered = false;
        public int baseNumbered = 1;
        public int stepNumbered = 1;
        public NumberedMethod numberMethod = NumberedMethod.BySelection;
        public int numberPadding = 0;
        
        public bool useReplace = false;
        public string replace = "";
        public string replaceWith = "";
        public bool useRegex = false;
        
        public bool useRemove = false;
        public string remove = "";
        
        public bool useCase = false;
        public CaseMethod caseMethod = CaseMethod.None;
        
        public bool useTrim = false;
        public bool useSort = false;
        public SortMethod sortMethod = SortMethod.None;
        
        public bool removeSpaces = false;
        public bool replaceSpacesWithUnderscore = false;
        
        // New trim letters feature
        public bool useTrimLetters = false;
        public int trimLettersCount = 1;
        public TrimPosition trimPosition = TrimPosition.Beginning;
    }

    public class PrashuRenameToolWindow : EditorWindow
    {
        private UnityEngine.Object[] selectedObjects = new UnityEngine.Object[0];
        private GameObject[] selectedGameObjects = new GameObject[0];
        private string[] previewNames = new string[0];
        
        private PrashuRenameToolSettings settings = new PrashuRenameToolSettings();
        
        private bool showPreview = true;
        private bool showAdvanced = false;
        private Vector2 scrollPosition;
        private Vector2 previewScrollPosition;
        
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private GUIStyle toggleStyle;
        
        // Preset management
        private string presetName = "";
        private Dictionary<string, PrashuRenameToolSettings> presets = new Dictionary<string, PrashuRenameToolSettings>();
        
        [MenuItem("Tools/Prashu Rename Tool %#&r", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<PrashuRenameToolWindow>("Prashu Rename Tool");
            window.minSize = new Vector2(400, 600);
            window.maxSize = new Vector2(800, 1200);
            window.Show();
        }
        
        private void OnEnable()
        {
            LoadPresets();
            RefreshSelection();
        }
        
        private void OnDisable()
        {
            SavePresets();
        }
        
        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle("Box")
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
            
            if (toggleStyle == null)
            {
                toggleStyle = new GUIStyle(EditorStyles.toggle)
                {
                    fontStyle = FontStyle.Bold
                };
            }
        }
        
        private void OnGUI()
        {
            InitializeStyles();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawHeader();
            DrawMainSettings();
            DrawAdvancedSettings();
            DrawActionButtons();
            DrawPreview();
            DrawPresetManagement();
            
            EditorGUILayout.EndScrollView();
            
            // Update preview in real-time
            if (GUI.changed)
            {
                UpdatePreview();
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🔧 Prashu Rename Tool", headerStyle);
            EditorGUILayout.Space(5);
            
            if (selectedObjects.Length > 0)
            {
                EditorGUILayout.HelpBox($"Selected: {selectedObjects.Length} object(s)", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No objects selected. Select objects in the Hierarchy or Project to rename them.", MessageType.Warning);
            }
            
            EditorGUILayout.Space(10);
        }
        
        private void DrawMainSettings()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("📝 Basic Settings", EditorStyles.boldLabel);
            
            // Base Name
            EditorGUILayout.BeginHorizontal();
            settings.useBasename = EditorGUILayout.Toggle(settings.useBasename, GUILayout.Width(20));
            GUI.enabled = settings.useBasename;
            settings.basename = EditorGUILayout.TextField("Base Name:", settings.basename);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            // Prefix
            EditorGUILayout.BeginHorizontal();
            settings.usePrefix = EditorGUILayout.Toggle(settings.usePrefix, GUILayout.Width(20));
            GUI.enabled = settings.usePrefix;
            settings.prefix = EditorGUILayout.TextField("Prefix:", settings.prefix);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            // Suffix
            EditorGUILayout.BeginHorizontal();
            settings.useSuffix = EditorGUILayout.Toggle(settings.useSuffix, GUILayout.Width(20));
            GUI.enabled = settings.useSuffix;
            settings.suffix = EditorGUILayout.TextField("Suffix:", settings.suffix);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Numbering
            settings.useNumbered = EditorGUILayout.Toggle("Add Numbers", settings.useNumbered, toggleStyle);
            if (settings.useNumbered)
            {
                EditorGUI.indentLevel++;
                settings.baseNumbered = EditorGUILayout.IntField("Start Number:", settings.baseNumbered);
                settings.stepNumbered = EditorGUILayout.IntField("Step:", Mathf.Max(1, settings.stepNumbered));
                settings.numberPadding = EditorGUILayout.IntField("Padding (zeros):", Mathf.Max(0, settings.numberPadding));
                settings.numberMethod = (NumberedMethod)EditorGUILayout.EnumPopup("Method:", settings.numberMethod);
                
                if (settings.numberMethod == NumberedMethod.ByHierarchy && HasProjectAssets())
                {
                    EditorGUILayout.HelpBox("Warning: Project assets cannot use hierarchy numbering. They will use selection order instead.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAdvancedSettings()
        {
            EditorGUILayout.Space(5);
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "⚙️ Advanced Settings", true, EditorStyles.foldoutHeader);
            
            if (showAdvanced)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                // Replace
                settings.useReplace = EditorGUILayout.Toggle("Find & Replace", settings.useReplace, toggleStyle);
                if (settings.useReplace)
                {
                    EditorGUI.indentLevel++;
                    settings.replace = EditorGUILayout.TextField("Find:", settings.replace);
                    settings.replaceWith = EditorGUILayout.TextField("Replace with:", settings.replaceWith);
                    settings.useRegex = EditorGUILayout.Toggle("Use Regex", settings.useRegex);
                    EditorGUI.indentLevel--;
                }
                
                // Remove
                EditorGUILayout.BeginHorizontal();
                settings.useRemove = EditorGUILayout.Toggle(settings.useRemove, GUILayout.Width(20));
                GUI.enabled = settings.useRemove;
                settings.remove = EditorGUILayout.TextField("Remove Text:", settings.remove);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                
                // NEW: Trim Letters Feature
                settings.useTrimLetters = EditorGUILayout.Toggle("Trim Letters", settings.useTrimLetters, toggleStyle);
                if (settings.useTrimLetters)
                {
                    EditorGUI.indentLevel++;
                    settings.trimLettersCount = EditorGUILayout.IntField("Number of Letters:", Mathf.Max(1, settings.trimLettersCount));
                    settings.trimPosition = (TrimPosition)EditorGUILayout.EnumPopup("Position:", settings.trimPosition);
                    
                    // Show helpful info
                    string positionText = settings.trimPosition == TrimPosition.Beginning ? "beginning" : "end";
                    EditorGUILayout.HelpBox($"Will remove {settings.trimLettersCount} letter(s) from the {positionText} of each name.", MessageType.Info);
                    EditorGUI.indentLevel--;
                }
                
                // Case Conversion
                settings.useCase = EditorGUILayout.Toggle("Change Case", settings.useCase, toggleStyle);
                if (settings.useCase)
                {
                    EditorGUI.indentLevel++;
                    settings.caseMethod = (CaseMethod)EditorGUILayout.EnumPopup("Case Type:", settings.caseMethod);
                    EditorGUI.indentLevel--;
                }
                
                // Spacing Options
                EditorGUILayout.LabelField("Spacing Options:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                settings.useTrim = EditorGUILayout.Toggle("Trim Whitespace", settings.useTrim);
                settings.removeSpaces = EditorGUILayout.Toggle("Remove All Spaces", settings.removeSpaces);
                settings.replaceSpacesWithUnderscore = EditorGUILayout.Toggle("Spaces to Underscores", settings.replaceSpacesWithUnderscore);
                EditorGUI.indentLevel--;
                
                // Sorting
                settings.useSort = EditorGUILayout.Toggle("Sort Objects", settings.useSort, toggleStyle);
                if (settings.useSort)
                {
                    EditorGUI.indentLevel++;
                    settings.sortMethod = (SortMethod)EditorGUILayout.EnumPopup("Sort Method:", settings.sortMethod);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawActionButtons()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = selectedObjects.Length > 0;
            if (GUILayout.Button("🔄 Rename Selected", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm Rename", 
                    $"Are you sure you want to rename {selectedObjects.Length} object(s)?", 
                    "Yes", "No"))
                {
                    PerformRename();
                }
            }
            GUI.enabled = true;
            
            if (GUILayout.Button("🧹 Clear Settings", GUILayout.Height(30)))
            {
                ClearSettings();
            }
            
            if (GUILayout.Button("🔄 Refresh", GUILayout.Height(30)))
            {
                RefreshSelection();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawPreview()
        {
            if (selectedObjects.Length == 0) return;
            
            EditorGUILayout.Space(5);
            showPreview = EditorGUILayout.Foldout(showPreview, $"👁️ Preview ({selectedObjects.Length} objects)", true, EditorStyles.foldoutHeader);
            
            if (showPreview)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                previewScrollPosition = EditorGUILayout.BeginScrollView(previewScrollPosition, GUILayout.MaxHeight(300));
                
                EditorGUILayout.BeginHorizontal();
                
                // Original names column
                EditorGUILayout.BeginVertical("Box", GUILayout.Width(200));
                EditorGUILayout.LabelField("Original", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);
                for (int i = 0; i < selectedObjects.Length; i++)
                {
                    EditorGUILayout.LabelField($"{i + 1}. {selectedObjects[i].name}", EditorStyles.wordWrappedLabel);
                }
                EditorGUILayout.EndVertical();
                
                // Arrow
                EditorGUILayout.BeginVertical(GUILayout.Width(30));
                EditorGUILayout.LabelField("", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);
                for (int i = 0; i < selectedObjects.Length; i++)
                {
                    EditorGUILayout.LabelField("→", EditorStyles.centeredGreyMiniLabel);
                }
                EditorGUILayout.EndVertical();
                
                // Preview names column
                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);
                for (int i = 0; i < previewNames.Length; i++)
                {
                    bool hasChanged = previewNames[i] != selectedObjects[i].name;
                    var style = hasChanged ? EditorStyles.boldLabel : EditorStyles.wordWrappedLabel;
                    EditorGUILayout.LabelField($"{i + 1}. {previewNames[i]}", style);
                }
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawPresetManagement()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("💾 Presets", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            presetName = EditorGUILayout.TextField("Preset Name:", presetName);
            
            GUI.enabled = !string.IsNullOrEmpty(presetName);
            if (GUILayout.Button("Save", GUILayout.Width(60)))
            {
                SavePreset(presetName);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            if (presets.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Saved Presets:");
                foreach (var preset in presets.Keys.ToList())
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(preset);
                    
                    if (GUILayout.Button("Load", GUILayout.Width(50)))
                    {
                        LoadPreset(preset);
                    }
                    
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Preset", $"Delete preset '{preset}'?", "Yes", "No"))
                        {
                            presets.Remove(preset);
                            SavePresets();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void Update()
        {
            // Refresh selection if it has changed
            if (!ArraysEqual(selectedObjects, Selection.objects))
            {
                RefreshSelection();
                Repaint();
            }
        }
        
        private void RefreshSelection()
        {
            selectedObjects = Selection.objects;
            selectedGameObjects = Selection.gameObjects;
            UpdatePreview();
        }
        
        private void UpdatePreview()
        {
            previewNames = new string[selectedObjects.Length];
            
            var objectsToProcess = selectedObjects.ToList();
            
            // Apply sorting if enabled
            if (settings.useSort && settings.sortMethod != SortMethod.None)
            {
                objectsToProcess = SortObjects(objectsToProcess);
            }
            
            for (int i = 0; i < objectsToProcess.Count; i++)
            {
                previewNames[i] = GenerateNewName(objectsToProcess[i], i, objectsToProcess.ToArray());
            }
        }
        
        private List<UnityEngine.Object> SortObjects(List<UnityEngine.Object> objects)
        {
            switch (settings.sortMethod)
            {
                case SortMethod.Alphabetical:
                    return objects.OrderBy(o => o.name).ToList();
                case SortMethod.ReverseAlphabetical:
                    return objects.OrderByDescending(o => o.name).ToList();
                case SortMethod.Length:
                    return objects.OrderBy(o => o.name.Length).ToList();
                default:
                    return objects;
            }
        }
        
        private string GenerateNewName(UnityEngine.Object obj, int index, UnityEngine.Object[] allObjects)
        {
            string newName = obj.name;
            
            // Base name
            if (settings.useBasename && !string.IsNullOrEmpty(settings.basename))
            {
                newName = settings.basename;
            }
            
            // NEW: Trim Letters (applied early in the process)
            if (settings.useTrimLetters)
            {
                newName = ApplyTrimLetters(newName, settings.trimLettersCount, settings.trimPosition);
            }
            
            // Prefix
            if (settings.usePrefix && !string.IsNullOrEmpty(settings.prefix))
            {
                newName = settings.prefix + newName;
            }
            
            // Suffix
            if (settings.useSuffix && !string.IsNullOrEmpty(settings.suffix))
            {
                newName = newName + settings.suffix;
            }
            
            // Numbering
            if (settings.useNumbered)
            {
                int number = CalculateNumber(obj, index, allObjects);
                string numberStr = settings.numberPadding > 0 ? 
                    number.ToString($"D{settings.numberPadding}") : 
                    number.ToString();
                newName = newName + numberStr;
            }
            
            // Remove text
            if (settings.useRemove && !string.IsNullOrEmpty(settings.remove))
            {
                newName = newName.Replace(settings.remove, "");
            }
            
            // Replace text
            if (settings.useReplace && !string.IsNullOrEmpty(settings.replace))
            {
                if (settings.useRegex)
                {
                    try
                    {
                        newName = Regex.Replace(newName, settings.replace, settings.replaceWith ?? "");
                    }
                    catch
                    {
                        // If regex fails, fall back to normal replace
                        newName = newName.Replace(settings.replace, settings.replaceWith ?? "");
                    }
                }
                else
                {
                    newName = newName.Replace(settings.replace, settings.replaceWith ?? "");
                }
            }
            
            // Case conversion
            if (settings.useCase)
            {
                newName = ApplyCaseConversion(newName, settings.caseMethod);
            }
            
            // Spacing options
            if (settings.useTrim)
            {
                newName = newName.Trim();
            }
            
            if (settings.removeSpaces)
            {
                newName = newName.Replace(" ", "");
            }
            else if (settings.replaceSpacesWithUnderscore)
            {
                newName = newName.Replace(" ", "_");
            }
            
            // Ensure name is not empty
            if (string.IsNullOrEmpty(newName))
            {
                newName = "Object";
            }
            
            return newName;
        }
        
        // NEW: Method to trim letters from beginning or end
        private string ApplyTrimLetters(string text, int count, TrimPosition position)
        {
            if (string.IsNullOrEmpty(text) || count <= 0)
                return text;
            
            // Ensure we don't trim more characters than exist
            count = Mathf.Min(count, text.Length);
            
            switch (position)
            {
                case TrimPosition.Beginning:
                    return text.Length > count ? text.Substring(count) : "";
                case TrimPosition.End:
                    return text.Length > count ? text.Substring(0, text.Length - count) : "";
                default:
                    return text;
            }
        }
        
        private int CalculateNumber(UnityEngine.Object obj, int index, UnityEngine.Object[] allObjects)
        {
            if (settings.numberMethod == NumberedMethod.BySelection)
            {
                return settings.baseNumbered + (settings.stepNumbered * index);
            }
            else // ByHierarchy
            {
                GameObject go = obj as GameObject;
                if (go != null && go.transform.parent != null)
                {
                    return settings.baseNumbered + (settings.stepNumbered * go.transform.GetSiblingIndex());
                }
                else
                {
                    // Fallback to selection order for project assets or root objects
                    return settings.baseNumbered + (settings.stepNumbered * index);
                }
            }
        }
        
        private string ApplyCaseConversion(string text, CaseMethod method)
        {
            switch (method)
            {
                case CaseMethod.Uppercase:
                    return text.ToUpper();
                case CaseMethod.Lowercase:
                    return text.ToLower();
                case CaseMethod.TitleCase:
                    return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
                case CaseMethod.CamelCase:
                    return ToCamelCase(text);
                case CaseMethod.PascalCase:
                    return ToPascalCase(text);
                default:
                    return text;
            }
        }
        
        private string ToCamelCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string[] words = text.Split(' ', '_', '-');
            string result = words[0].ToLower();
            for (int i = 1; i < words.Length; i++)
            {
                if (!string.IsNullOrEmpty(words[i]))
                {
                    result += char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return result;
        }
        
        private string ToPascalCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string[] words = text.Split(' ', '_', '-');
            string result = "";
            foreach (string word in words)
            {
                if (!string.IsNullOrEmpty(word))
                {
                    result += char.ToUpper(word[0]) + word.Substring(1).ToLower();
                }
            }
            return result;
        }
        
        private void PerformRename()
        {
            var objectsToProcess = selectedObjects.ToList();
            
            // Apply sorting if enabled
            if (settings.useSort && settings.sortMethod != SortMethod.None)
            {
                objectsToProcess = SortObjects(objectsToProcess);
            }
            
            Undo.SetCurrentGroupName("Prashu Rename Tool");
            int undoGroup = Undo.GetCurrentGroup();
            
            for (int i = 0; i < objectsToProcess.Count; i++)
            {
                var obj = objectsToProcess[i];
                string newName = GenerateNewName(obj, i, objectsToProcess.ToArray());
                
                if (newName != obj.name)
                {
                    Undo.RecordObject(obj, "Rename Object");
                    
                    // Handle asset renaming
                    string assetPath = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        AssetDatabase.RenameAsset(assetPath, newName);
                    }
                    else
                    {
                        obj.name = newName;
                    }
                }
            }
            
            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssets();
            
            RefreshSelection();
            EditorUtility.DisplayDialog("Rename Complete", $"Successfully renamed {objectsToProcess.Count} object(s)!", "OK");
        }
        
        private void ClearSettings()
        {
            settings = new PrashuRenameToolSettings();
            presetName = "";
            UpdatePreview();
        }
        
        private bool HasProjectAssets()
        {
            return selectedObjects.Any(obj => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj)));
        }
        
        private bool ArraysEqual<T>(T[] array1, T[] array2)
        {
            if (array1.Length != array2.Length) return false;
            for (int i = 0; i < array1.Length; i++)
            {
                if (!array1[i].Equals(array2[i])) return false;
            }
            return true;
        }
        
        #region Preset Management
        
        private void SavePreset(string name)
        {
            presets[name] = CloneSettings(settings);
            SavePresets();
            presetName = "";
            EditorUtility.DisplayDialog("Preset Saved", $"Preset '{name}' has been saved!", "OK");
        }
        
        private void LoadPreset(string name)
        {
            if (presets.ContainsKey(name))
            {
                settings = CloneSettings(presets[name]);
                UpdatePreview();
            }
        }
        
        private PrashuRenameToolSettings CloneSettings(PrashuRenameToolSettings original)
        {
            return JsonUtility.FromJson<PrashuRenameToolSettings>(JsonUtility.ToJson(original));
        }
        
        private void SavePresets()
        {
            string json = JsonUtility.ToJson(new SerializablePresets { presets = presets.Keys.ToArray(), settings = presets.Values.ToArray() });
            EditorPrefs.SetString("PrashuRenameTool_Presets", json);
        }
        
        private void LoadPresets()
        {
            string json = EditorPrefs.GetString("PrashuRenameTool_Presets", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<SerializablePresets>(json);
                    presets.Clear();
                    for (int i = 0; i < data.presets.Length && i < data.settings.Length; i++)
                    {
                        presets[data.presets[i]] = data.settings[i];
                    }
                }
                catch
                {
                    presets.Clear();
                }
            }
        }
        
        [Serializable]
        private class SerializablePresets
        {
            public string[] presets;
            public PrashuRenameToolSettings[] settings;
        }
        
        #endregion
    }
}