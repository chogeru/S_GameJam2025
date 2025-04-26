using UnityEngine;
using UnityEditor;

  public static class RemoveMissingComponentsEditor
    {
        [MenuItem("Abubu/選択したオブジェクトの全ての子から欠損コンポーネントを削除")]
        public static void RemoveMissingComponentsFromChildren()
        {
            var selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0) { Debug.LogWarning("オブジェクトが選択されていない"); return; }
            foreach (var obj in selectedObjects)
                RemoveMissingRecursively(obj);
            Debug.Log("欠損コンポーネントの削除を完了");
        }

        private static void RemoveMissingRecursively(GameObject obj)
        {
            Undo.RegisterCompleteObjectUndo(obj, "欠損コンポーネント削除");
            int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            if (removedCount > 0)
                Debug.Log($"{obj.name}から{removedCount}個の欠損コンポーネントを削除しました");
            foreach (Transform child in obj.transform)
                RemoveMissingRecursively(child.gameObject);
        }
    }