using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AbubuResource.Editor
{
    public class SelectableCopyPasteEditor : EditorWindow
    {
        private List<GameObject> selectedSourceRoots = new List<GameObject>();
        private List<CopyPasteData.SourceObjectData> sourceObjects = new List<CopyPasteData.SourceObjectData>();
        private List<CopyPasteData.TargetObjectData> targetObjects = new List<CopyPasteData.TargetObjectData>();
        private GameObject pasteRoot;
        private Vector2 scrollPos;
        private bool showCopySection = true;
        private bool showTargetSection = true;
        private bool showPasteSection = true;
        private bool showCopiedSources = false;
        private string sourceSearchText = "";
        private string targetSearchText = "";
        private Dictionary<int, bool> sourceFoldoutStates = new Dictionary<int, bool>();
        private bool sortAlphabetically = false;
        private List<Type> excludedComponentTypes = new List<Type> { typeof(Transform) };
        private bool overwriteExistingComponents = false;
        private bool removeBeforePaste = false;
        private bool enableUndo = true;

        [MenuItem("Abubu/階層ツール/選択可能コピー＆ペーストエディタ")]
        private static void OpenWindow()
        {
            GetWindow<SelectableCopyPasteEditor>("SelectableCopyPaste");
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.Space();

            showCopySection = EditorGUILayout.Foldout(showCopySection, "1) ソースからコピー", true);
            if (showCopySection)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("ソースのコピー処理。1つ以上の GameObject を選択し、ボタンをクリックしてください。", MessageType.Info);
                enableUndo = EditorGUILayout.Toggle("Undo を有効にする（実験的）", enableUndo);
                if (GUILayout.Button("選択からコピー（子を含む）[複数]"))
                {
                    CopyFromSelectedMultiple();
                }
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("ソース検索（オプション）：");
                sourceSearchText = EditorGUILayout.TextField(sourceSearchText);
                sortAlphabetically = EditorGUILayout.Toggle("ソースをアルファベット順にソート", sortAlphabetically);
                EditorGUILayout.Space();
                showCopiedSources = EditorGUILayout.Toggle("コピーされたソースを表示", showCopiedSources);
                if (showCopiedSources)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("すべて展開"))
                        CopyPasteUIHelper.SetAllFoldoutStates(sourceFoldoutStates, sourceObjects, true);
                    if (GUILayout.Button("すべて折りたたむ"))
                        CopyPasteUIHelper.SetAllFoldoutStates(sourceFoldoutStates, sourceObjects, false);
                    EditorGUILayout.EndHorizontal();
                    CopyPasteUIHelper.DrawSourceObjectInfo(sourceObjects, sourceSearchText, sourceFoldoutStates, sortAlphabetically);
                }
                else
                {
                    EditorGUILayout.HelpBox("コピーされたソースオブジェクトは非表示です。「コピーされたソースを表示」を切り替えて表示してください。", MessageType.Info);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            showTargetSection = EditorGUILayout.Foldout(showTargetSection, "2) ターゲットの設定", true);
            if (showTargetSection)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("ターゲット設定。ターゲットルートを割り当て、ターゲットリストを生成してください。", MessageType.Info);
                pasteRoot = (GameObject)EditorGUILayout.ObjectField("ターゲットルート", pasteRoot, typeof(GameObject), true);
                if (GUILayout.Button("ターゲットリストを生成（子を含む）"))
                    GenerateTargetList();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("ターゲット検索（オプション）：");
                targetSearchText = EditorGUILayout.TextField(targetSearchText);
                EditorGUILayout.LabelField("ペーストオプション：", EditorStyles.boldLabel);
                overwriteExistingComponents = EditorGUILayout.Toggle("既存のコンポーネントを上書き", overwriteExistingComponents);
                if (overwriteExistingComponents)
                    removeBeforePaste = EditorGUILayout.Toggle("上書き時に事前に削除", removeBeforePaste);
                CopyPasteUIHelper.DrawTargetObjectList(targetObjects, sourceObjects, targetSearchText, sortAlphabetically, Repaint);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            showPasteSection = EditorGUILayout.Foldout(showPasteSection, "3) ペースト", true);
            if (showPasteSection)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("ペースト処理。各ターゲットに対してソースとコンポーネントを選択し、ペーストしてください。", MessageType.Info);
                if (GUILayout.Button("ターゲットオブジェクトにペースト"))
                    PasteToTarget();
                if (GUILayout.Button("マッチするソースを自動選択"))
                    AutoSelectSourcesByName();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("コピー済みデータの保存／読み込み（オプション）：", EditorStyles.boldLabel);
                if (GUILayout.Button("コピー済みデータをアセットに保存"))
                    SaveCopiedDataToAsset();
                if (GUILayout.Button("アセットからコピー済みデータを読み込み"))
                    LoadCopiedDataFromAsset();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void CopyFromSelectedMultiple()
        {
            var selections = Selection.gameObjects;
            if (selections == null || selections.Length == 0)
            {
                Debug.LogWarning("少なくとも1つのソースGameObjectを選択してください。");
                return;
            }
            sourceObjects.Clear();
            selectedSourceRoots.Clear();
            selectedSourceRoots.AddRange(selections);
            foreach (var sel in selections)
                if (sel != null)
                    CopyPasteProcessor.CollectSourceDataRecursive(sel.transform, "", sourceObjects, excludedComponentTypes);
            string msg = $"{selections.Length}つのルートをコピーしました。合計{sourceObjects.Count}のソースオブジェクトを取得しました。";
            Debug.Log(msg);
            ShowNotification(new GUIContent(msg));
        }

        private void AutoSelectSourcesByName()
        {
            foreach (var tgt in targetObjects)
            {
                int foundIndex = -1;
                for (int i = 0; i < sourceObjects.Count; i++)
                {
                    if (sourceObjects[i].objectName.Equals(tgt.objectName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundIndex = i;
                        break;
                    }
                }
                if (foundIndex < 0)
                {
                    for (int i = 0; i < sourceObjects.Count; i++)
                        if (sourceObjects[i].objectPath.ToLower().Contains(tgt.objectName.ToLower()))
                        {
                            foundIndex = i;
                            break;
                        }
                }
                if (foundIndex >= 0)
                {
                    tgt.selectedSourceIndex = foundIndex;
                    int compCount = sourceObjects[foundIndex].components.Count;
                    tgt.componentToggles = new bool[compCount];
                    for (int j = 0; j < compCount; j++)
                        tgt.componentToggles[j] = true;
                }
                else
                {
                    tgt.selectedSourceIndex = -1;
                    tgt.componentToggles = null;
                }
            }
            Repaint();
        }

        private void GenerateTargetList()
        {
            targetObjects.Clear();
            if (pasteRoot == null)
            {
                Debug.LogWarning("ターゲットルートが設定されていません。");
                return;
            }
            CopyPasteProcessor.CollectTargetDataRecursive(pasteRoot.transform, "", targetObjects);
            Debug.Log($"{pasteRoot.name}からターゲットリストを生成しました。{targetObjects.Count}個のオブジェクトが見つかりました。");
        }

        private void PasteToTarget()
        {
            if (sourceObjects.Count == 0)
            {
                Debug.LogWarning("ペーストするソースオブジェクトがありません。");
                return;
            }
            if (targetObjects.Count == 0)
            {
                Debug.LogWarning("ターゲットオブジェクトがありません。まずターゲットリストを生成してください。");
                return;
            }
            int pastedCount = 0;
            foreach (var tgt in targetObjects)
            {
                int idx = tgt.selectedSourceIndex;
                if (idx < 0 || idx >= sourceObjects.Count) continue;
                var srcObj = sourceObjects[idx];
                if (tgt.componentToggles == null || tgt.componentToggles.Length != srcObj.components.Count)
                    continue;
                if (enableUndo)
                    Undo.RegisterFullObjectHierarchyUndo(tgt.targetTransform.gameObject, $"Paste Components {tgt.objectName}");
                for (int c = 0; c < srcObj.components.Count; c++)
                {
                    if (!tgt.componentToggles[c]) continue;
                    CopyPasteProcessor.PasteOneComponent(tgt.targetTransform, srcObj.components[c], overwriteExistingComponents, removeBeforePaste, enableUndo);
                    pastedCount++;
                }
            }
            Debug.Log($"ペースト完了！{pastedCount}件のコンポーネントをペーストしました。");
        }

        private void SaveCopiedDataToAsset()
        {
            if (sourceObjects.Count == 0)
            {
                Debug.LogWarning("保存するソースデータがありません。");
                return;
            }
            var path = EditorUtility.SaveFilePanelInProject("Save Copied Data", "CopiedData", "asset", "Save Copied Data to Asset");
            if (string.IsNullOrEmpty(path)) return;
            var so = ScriptableObject.CreateInstance<CopyPasteDataAsset>();
            so.sourceObjects = new List<CopyPasteData.SourceObjectData>(sourceObjects);
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"{path}にソースデータを保存しました。");
        }

        private void LoadCopiedDataFromAsset()
        {
            var path = EditorUtility.OpenFilePanel("Load Copied Data", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;
            var projectRelativePath = "Assets" + path.Substring(Application.dataPath.Length);
            var so = AssetDatabase.LoadAssetAtPath<CopyPasteDataAsset>(projectRelativePath);
            if (so == null)
            {
                Debug.LogWarning("無効なアセット、またはCopyPasteDataAssetではありません。");
                return;
            }
            sourceObjects = new List<CopyPasteData.SourceObjectData>(so.sourceObjects);
            Debug.Log($"{projectRelativePath}からソースデータを読み込みました。{sourceObjects.Count}個のソースオブジェクト。");
        }
    }

    public static class CopyPasteProcessor
    {
        public static void CollectSourceDataRecursive(Transform t, string parentPath, List<CopyPasteData.SourceObjectData> sourceList, List<Type> excludedTypes)
        {
            string path = string.IsNullOrEmpty(parentPath) ? t.name : (parentPath + "/" + t.name);
            var data = new CopyPasteData.SourceObjectData
            {
                objectPath = path,
                objectName = t.name,
                components = new List<CopyPasteData.SourceComponentData>()
            };
            var comps = t.GetComponents<Component>();
            foreach (var comp in comps)
            {
                if (comp == null || excludedTypes.Contains(comp.GetType())) continue;
                var tempGO = new GameObject("TempForCopy") { hideFlags = HideFlags.HideAndDontSave };
                TryAddDependencies(tempGO, comp.GetType());

                Component tempComp = null;
                try { tempComp = tempGO.AddComponent(comp.GetType()); }
                catch (Exception e) { Debug.LogWarning($"AddComponent({comp.GetType().Name})に失敗しました: {e.Message}"); }
                if (tempComp == null) { UnityEngine.Object.DestroyImmediate(tempGO); continue; }
                try
                {
                    EditorUtility.CopySerialized(comp, tempComp);
                    string json = EditorJsonUtility.ToJson(tempComp);
                    data.components.Add(new CopyPasteData.SourceComponentData { typeName = comp.GetType().AssemblyQualifiedName, json = json });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"CopySerialized({comp.GetType().Name})に失敗しました: {ex.Message}");
                }
                UnityEngine.Object.DestroyImmediate(tempGO);
            }
            sourceList.Add(data);
            for (int i = 0; i < t.childCount; i++)
                CollectSourceDataRecursive(t.GetChild(i), path, sourceList, excludedTypes);
        }

        public static void CollectTargetDataRecursive(Transform t, string parentPath, List<CopyPasteData.TargetObjectData> targetList)
        {
            string path = string.IsNullOrEmpty(parentPath) ? t.name : (parentPath + "/" + t.name);
            var tgt = new CopyPasteData.TargetObjectData { targetTransform = t, objectPath = path, objectName = t.name, selectedSourceIndex = -1, componentToggles = null };
            targetList.Add(tgt);
            for (int i = 0; i < t.childCount; i++)
                CollectTargetDataRecursive(t.GetChild(i), path, targetList);
        }

        public static void PasteOneComponent(Transform targetTransform, CopyPasteData.SourceComponentData compData, bool overwriteExisting, bool removeBefore, bool enableUndo)
        {
            var compType = FindTypeByName(compData.typeName);
            if (compType == null) { Debug.LogWarning($"型を取得できませんでした: {compData.typeName}"); return; }
            var existingComp = targetTransform.GetComponent(compType);
            if (existingComp != null)
            {
                if (overwriteExisting)
                {
                    if (removeBefore)
                    {
                        if (enableUndo) Undo.DestroyObjectImmediate(existingComp);
                        else UnityEngine.Object.DestroyImmediate(existingComp);
                        existingComp = null;
                    }
                    else
                    {
                        var tempGO2 = new GameObject("TempForPaste") { hideFlags = HideFlags.HideAndDontSave };
                        var tempComp2 = tempGO2.AddComponent(compType);
                        EditorJsonUtility.FromJsonOverwrite(compData.json, tempComp2);
                        if (enableUndo) Undo.RecordObject(existingComp, $"Paste {compType.Name}");
                        EditorUtility.CopySerialized(tempComp2, existingComp);
                        UnityEngine.Object.DestroyImmediate(tempGO2);
                        return;
                    }
                }
                else return;
            }
            Component newComp = enableUndo ? Undo.AddComponent(targetTransform.gameObject, compType) : targetTransform.gameObject.AddComponent(compType);
            var tempGO = new GameObject("TempForPaste") { hideFlags = HideFlags.HideAndDontSave };
            var tempComp = tempGO.AddComponent(compType);
            EditorJsonUtility.FromJsonOverwrite(compData.json, tempComp);
            if (enableUndo) Undo.RecordObject(newComp, $"Paste {compType.Name}");
            EditorUtility.CopySerialized(tempComp, newComp);
            UnityEngine.Object.DestroyImmediate(tempGO);
        }

        private static void TryAddDependencies(GameObject go, Type compType)
        {
            var requireAttrs = compType.GetCustomAttributes(typeof(RequireComponent), true);
            foreach (RequireComponent req in requireAttrs)
            {
                AddRequiredComponent(go, req.m_Type0);
                AddRequiredComponent(go, req.m_Type1);
                AddRequiredComponent(go, req.m_Type2);
            }
        }

        private static void AddRequiredComponent(GameObject go, Type requiredType)
        {
            if (requiredType == null || requiredType == typeof(Transform)) return;
            if (requiredType.IsAbstract)
            {
                if (requiredType == typeof(Collider))
                {
                    if (go.GetComponent<SphereCollider>() == null)
                        go.AddComponent<SphereCollider>();
                }
                else Debug.LogWarning($"抽象型{requiredType.Name}を追加できません");
                return;
            }
            if (go.GetComponent(requiredType) == null)
            {
                try { go.AddComponent(requiredType); }
                catch (Exception e) { Debug.LogWarning($"{requiredType.Name}の追加に失敗しました: {e.Message}"); }
            }
        }

        private static Type FindTypeByName(string assemblyQualifiedName)
        {
            var t = Type.GetType(assemblyQualifiedName);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(assemblyQualifiedName);
                if (t != null) return t;
            }
            return null;
        }
    }

    [Serializable]
    public static class CopyPasteData
    {
        public class SourceObjectData { public string objectPath; public string objectName; public List<SourceComponentData> components; }
        public class SourceComponentData { public string typeName; public string json; }
        public class TargetObjectData { public Transform targetTransform; public string objectPath; public string objectName; public int selectedSourceIndex = -1; public bool[] componentToggles; }
    }

    public class CopyPasteDataAsset : ScriptableObject
    {
        public List<CopyPasteData.SourceObjectData> sourceObjects;
    }

    public static class CopyPasteUIHelper
    {
        public static void DrawSourceObjectInfo(List<CopyPasteData.SourceObjectData> sourceObjects, string sourceSearchText, Dictionary<int, bool> foldoutStates, bool sortAlphabetically)
        {
            if (sourceObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("No source copied yet.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("Copied Source Objects:", EditorStyles.boldLabel);
            IEnumerable<CopyPasteData.SourceObjectData> displayList = sourceObjects;
            if (sortAlphabetically) displayList = displayList.OrderBy(s => s.objectPath);
            int i = 0;
            foreach (var srcObj in displayList)
            {
                if (!string.IsNullOrEmpty(sourceSearchText) && !srcObj.objectPath.ToLower().Contains(sourceSearchText.ToLower())) { i++; continue; }
                if (!foldoutStates.ContainsKey(i)) foldoutStates[i] = false;
                int level = srcObj.objectPath.Count(c => c == '/');
                EditorGUI.indentLevel = level;
                foldoutStates[i] = EditorGUILayout.Foldout(foldoutStates[i], $"{srcObj.objectPath} (Components: {srcObj.components.Count})", true);
                if (foldoutStates[i])
                {
                    EditorGUI.indentLevel++;
                    foreach (var compData in srcObj.components)
                        EditorGUILayout.LabelField($"Type: {compData.typeName}");
                    EditorGUI.indentLevel--;
                }
                i++;
            }
            EditorGUI.indentLevel = 0;
        }

        public static void DrawTargetObjectList(List<CopyPasteData.TargetObjectData> targetObjects, List<CopyPasteData.SourceObjectData> sourceObjects, string targetSearchText, bool sortAlphabetically, Action repaintCallback)
        {
            if (targetObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("No target generated yet. Please assign the Target Root and press \"Generate Target List\".", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("Target Objects (select source & components):", EditorStyles.boldLabel);
            IEnumerable<CopyPasteData.TargetObjectData> displayList = targetObjects;
            if (sortAlphabetically) displayList = displayList.OrderBy(t => t.objectPath);
            int i = 0;
            foreach (var tgt in displayList)
            {
                if (!string.IsNullOrEmpty(targetSearchText) && !tgt.objectPath.ToLower().Contains(targetSearchText.ToLower())) { i++; continue; }
                int level = tgt.objectPath.Count(c => c == '/');
                EditorGUI.indentLevel = level;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"[{i}] {tgt.objectPath}");
                if (GUILayout.Button("Select Source Object", GUILayout.MaxWidth(200)))
                    ShowSourceSelectionMenu(i, targetObjects, sourceObjects, repaintCallback);
                if (tgt.selectedSourceIndex >= 0 && tgt.selectedSourceIndex < sourceObjects.Count)
                {
                    var chosen = sourceObjects[tgt.selectedSourceIndex];
                    EditorGUILayout.LabelField($"Selected: {chosen.objectPath}");
                    if (tgt.componentToggles == null || tgt.componentToggles.Length != chosen.components.Count)
                    {
                        tgt.componentToggles = new bool[chosen.components.Count];
                        for (int k = 0; k < chosen.components.Count; k++) tgt.componentToggles[k] = true;
                    }
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select All", GUILayout.MaxWidth(100))) for (int c = 0; c < tgt.componentToggles.Length; c++) tgt.componentToggles[c] = true;
                    if (GUILayout.Button("Deselect All", GUILayout.MaxWidth(100))) for (int c = 0; c < tgt.componentToggles.Length; c++) tgt.componentToggles[c] = false;
                    EditorGUILayout.EndHorizontal();
                    for (int c = 0; c < chosen.components.Count; c++)
                        tgt.componentToggles[c] = EditorGUILayout.ToggleLeft($"[{chosen.components[c].typeName}]", tgt.componentToggles[c]);
                    EditorGUI.indentLevel--;
                }
                else EditorGUILayout.LabelField("Selected: (None)");
                EditorGUILayout.EndVertical();
                i++;
            }
            EditorGUI.indentLevel = 0;
        }

        private static void ShowSourceSelectionMenu(int targetIndex, List<CopyPasteData.TargetObjectData> targetObjects, List<CopyPasteData.SourceObjectData> sourceObjects, Action repaintCallback)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("(None)"), targetObjects[targetIndex].selectedSourceIndex == -1, () =>
            {
                targetObjects[targetIndex].selectedSourceIndex = -1;
                targetObjects[targetIndex].componentToggles = null;
                repaintCallback();
            });
            for (int s = 0; s < sourceObjects.Count; s++)
            {
                var src = sourceObjects[s];
                string[] split = src.objectPath.Split('/');
                string shortName = split[split.Length - 1];
                int indexCopy = s;
                menu.AddItem(new GUIContent($"[{s}] {shortName}"), targetObjects[targetIndex].selectedSourceIndex == s, () =>
                {
                    targetObjects[targetIndex].selectedSourceIndex = indexCopy;
                    targetObjects[targetIndex].componentToggles = new bool[sourceObjects[indexCopy].components.Count];
                    for (int k = 0; k < targetObjects[targetIndex].componentToggles.Length; k++)
                        targetObjects[targetIndex].componentToggles[k] = true;
                    repaintCallback();
                });
            }
            menu.ShowAsContext();
        }

        public static void SetAllFoldoutStates(Dictionary<int, bool> foldoutStates, List<CopyPasteData.SourceObjectData> sourceObjects, bool state)
        {
            for (int i = 0; i < sourceObjects.Count; i++) foldoutStates[i] = state;
        }
    }

    public class ComponentDestroyer : EditorWindow
    {
        private GameObject m_SelectedObject;
        private Dictionary<Component, bool> m_ComponentsToDestroy = new Dictionary<Component, bool>();

        [MenuItem("Abubu/コンポーネント破壊ツール")]
        public static void ShowWindow() => GetWindow<ComponentDestroyer>("コンポーネント破壊ツール");

        private void OnGUI()
        {
            if (GUILayout.Button("選択されたオブジェクトを更新"))
                UpdateSelectedObject();
            if (m_SelectedObject != null)
            {
                EditorGUILayout.LabelField("選択されたオブジェクト: " + m_SelectedObject.name);
                if (GUILayout.Button("コンポーネントを取得"))
                    GetComponentsFromSelected();
                foreach (var pair in new List<KeyValuePair<Component, bool>>(m_ComponentsToDestroy))
                {
                    bool newValue = EditorGUILayout.ToggleLeft(pair.Key.gameObject.name + "/" + pair.Key.GetType().Name, pair.Value);
                    if (newValue != pair.Value) m_ComponentsToDestroy[pair.Key] = newValue;
                }
                if (GUILayout.Button("チェックされたコンポーネントを破壊"))
                    DestroyCheckedComponents();
            }
            else EditorGUILayout.LabelField("オブジェクトが選択されていません。");
        }

        private void UpdateSelectedObject() => m_SelectedObject = Selection.activeGameObject;

        private void GetComponentsFromSelected()
        {
            m_ComponentsToDestroy.Clear();
            if (m_SelectedObject != null)
            {
                AddComponents(m_SelectedObject);
                foreach (Transform child in m_SelectedObject.transform) AddComponents(child.gameObject);
            }
        }

        private void AddComponents(GameObject obj)
        {
            foreach (Component component in obj.GetComponents<Component>())
                if (!(component is Transform))
                    m_ComponentsToDestroy[component] = false;
        }

        private void DestroyCheckedComponents()
        {
            foreach (var pair in m_ComponentsToDestroy) if (pair.Value) DestroyImmediate(pair.Key);
            m_ComponentsToDestroy.Clear();
        }
    }
}
