using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

   public class HierarchySyncEditor : EditorWindow
    {
        private GameObject sourceObject;
        private GameObject targetObject;
        private Vector2 scrollPos;

        [MenuItem("Abubu/階層ツール/階層同期")]
        private static void OpenWindow() => GetWindow<HierarchySyncEditor>("階層同期エディタ");

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            sourceObject = (GameObject)EditorGUILayout.ObjectField("ソースオブジェクト (A)", sourceObject, typeof(GameObject), true);
            targetObject = (GameObject)EditorGUILayout.ObjectField("ターゲットオブジェクト (B)", targetObject, typeof(GameObject), true);
            EditorGUILayout.Space();
            if (GUILayout.Button("AからBへ欠損オブジェクトをコピー"))
            {
                if (sourceObject == null || targetObject == null)
                    Debug.LogWarning("ソースとターゲットの両方のオブジェクトを選択してください。");
                else
                {
                    CopyMissingHierarchy(sourceObject.transform, targetObject.transform);
                    Debug.Log("欠損オブジェクトを正常にコピーしました。");
                    ShowNotification(new GUIContent("コピー完了"));
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void CopyMissingHierarchy(Transform source, Transform target)
        {
            foreach (Transform sourceChild in source)
            {
                Transform targetChild = target.Find(sourceChild.name);
                if (targetChild == null)
                {
                    GameObject newChild = Instantiate(sourceChild.gameObject);
                    Undo.RegisterCreatedObjectUndo(newChild, "Copy Missing Object");
                    newChild.name = sourceChild.name;
                    newChild.transform.SetParent(target, false);
                    newChild.transform.localPosition = sourceChild.localPosition;
                    newChild.transform.localRotation = sourceChild.localRotation;
                    newChild.transform.localScale = sourceChild.localScale;
                    targetChild = newChild.transform;
                }
                CopyMissingHierarchy(sourceChild, targetChild);
            }
        }
    }