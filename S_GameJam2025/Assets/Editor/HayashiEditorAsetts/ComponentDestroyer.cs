using UnityEngine;
using UnityEditor; 
using System.Collections.Generic;
using System.Linq;

public class ComponentDestroyer : EditorWindow
{
    private GameObject m_SelectedObject;
    private Dictionary<Component, bool> m_ComponentsToDestroy = new Dictionary<Component, bool>();

    [MenuItem("Abubu/コンポーネント/コンポーネントの破壊ヤー")] 
    public static void ShowWindow()
    {
        GetWindow<ComponentDestroyer>("コンポーネントの破壊ヤー"); 
    }

    private void OnGUI()
    {
        if (GUILayout.Button("選択されたオブジェクトを更新"))
        {
            UpdateSelectedObject();
        }

        if (m_SelectedObject != null)
        {
            EditorGUILayout.LabelField("選択されたオブジェクト: " + m_SelectedObject.name);
            if (GUILayout.Button("コンポーネントを取得"))
            {
                GetComponentsFromSelected();
            }

            //登録されたコンポーネントをリストで表示し、チェックボックスで選択
            foreach (var pair in new List<KeyValuePair<Component, bool>>(m_ComponentsToDestroy))
            {
                string label = pair.Key.gameObject.name + "/" + pair.Key.GetType().Name;
                bool newValue = EditorGUILayout.ToggleLeft(label, pair.Value);
                if (newValue != pair.Value)
                {
                    //チェック状態を更新
                    m_ComponentsToDestroy[pair.Key] = newValue;
                }
            }

            //チェックされたコンポーネントを破壊するボタン
            if (GUILayout.Button("チェックされたコンポーネントを破壊"))
            {
                DestroyCheckedComponents();
            }

        }
        else
        {
            EditorGUILayout.LabelField("オブジェクトが選択されていません。");
        }
    }

    private void UpdateSelectedObject()
    {
        //現在エディタで選択されているGameObjectを更新
        m_SelectedObject = Selection.activeGameObject; 
    }

    private void GetComponentsFromSelected()
    {
        //既存のリストをクリア
        m_ComponentsToDestroy.Clear(); 
        if (m_SelectedObject != null)
        {
            //選択されたオブジェクトのコンポーネントを追加
            AddComponents(m_SelectedObject);
            //子オブジェクトも同様に処理
            foreach (Transform child in m_SelectedObject.transform) 
            {
                AddComponents(child.gameObject);
            }
        }
    }

    private void AddComponents(GameObject obj)
    {
        foreach (Component component in obj.GetComponents<Component>())
        {
            //Transformコンポーネントは削除不可としてスキップ
            if (!(component is Transform))
            {
                //新しいコンポーネントを辞書に追加
                m_ComponentsToDestroy[component] = false; 
            }
        }
    }

    private void DestroyCheckedComponents()
    {
        foreach (var pair in m_ComponentsToDestroy)
        {
            //チェックされたコンポーネントを削除
            if (pair.Value) 
            {
                //エディタ中でコンポーネントを破壊する
                DestroyImmediate(pair.Key);
            }
        }
        // 削除後、辞書をクリア
        m_ComponentsToDestroy.Clear();
    }
}
