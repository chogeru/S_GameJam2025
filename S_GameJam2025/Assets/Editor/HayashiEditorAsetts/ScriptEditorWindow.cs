using System;
using System.IO;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(MonoScript))]
public class ScriptEditorWindow : Editor
{
    private string originalScriptContent;
    private string editedScriptContent;
    private Vector2 scrollPosition;
    private bool isModified;

    private void OnEnable()
    {
        MonoScript script = (MonoScript)target;
        originalScriptContent = script.text;
        editedScriptContent = originalScriptContent;
        isModified = false;
    }

    private void OnDisable()
    {
        originalScriptContent = null;
        editedScriptContent = null;
        isModified = false;
    }

    public override void OnInspectorGUI()
    {
        MonoScript script = (MonoScript)target;
        GUILayout.Label("スクリプト内容", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(500));
        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.gray;
        string newContent = EditorGUILayout.TextArea(editedScriptContent, GUILayout.ExpandHeight(true));
        GUI.backgroundColor = oldColor;
        EditorGUILayout.EndScrollView();
        if (newContent != editedScriptContent)
        {
            Undo.RecordObject(script, "編集 " + script.name);
            editedScriptContent = newContent;
            isModified = !editedScriptContent.Equals(originalScriptContent);
        }
        EditorGUILayout.Space();
        GUI.enabled = isModified;
        if (GUILayout.Button("スクリプトを保存"))
        {
            if (EditorUtility.DisplayDialog("スクリプトの保存", "変更を保存しますか？", "はい", "いいえ"))
                SaveScript(script, editedScriptContent);
        }
        GUI.enabled = true;
        Repaint();
    }

    private void SaveScript(MonoScript script, string content)
    {
        string path = AssetDatabase.GetAssetPath(script);
        if (!AssetDatabase.IsOpenForEdit(path))
        {
            EditorUtility.DisplayDialog("読み取り専用スクリプト", "このスクリプトは読み取り専用のため、編集できません", "はい");
            return;
        }
        try
        {
            System.IO.File.WriteAllText(path, content);
            AssetDatabase.Refresh();
            originalScriptContent = content;
            isModified = false;
            Debug.Log("スクリプトを保存しました");
        }
        catch (UnauthorizedAccessException e) { HandleSaveError("アクセス権限がない", e); }
        catch (System.IO.IOException e) { HandleSaveError("I/Oエラーが発生しました", e); }
        catch (Exception e) { HandleSaveError(e.Message, e); }
    }

    private void HandleSaveError(string message, Exception e)
    {
        Debug.LogError($"スクリプトの保存に失敗しました: {message}");
        EditorUtility.DisplayDialog("エラー", $"スクリプトの保存に失敗しました: {message}", "はい");
    }
}
