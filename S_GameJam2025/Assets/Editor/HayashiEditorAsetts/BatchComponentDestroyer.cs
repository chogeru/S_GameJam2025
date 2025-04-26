using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
namespace AbubuResource.Editor
{
    public class BatchComponentDestroyer : EditorWindow
    {
        private GameObject m_SelectedObject;
        private Component m_SelectedComponent;
        private List<Component> m_AllComponents = new List<Component>();
        private int m_SelectedComponentIndex = 0;
        private string[] m_ComponentNames;

        [MenuItem("Abubu/コンポーネント/一括コンポーネント破壊ツール")]
        public static void ShowWindow() => GetWindow<BatchComponentDestroyer>("一括コンポーネント破壊ツール");

        private void OnEnable() => UpdateSelectedObject();

        private void OnGUI()
        {
            if (GUILayout.Button("選択されたオブジェクトを更新")) UpdateSelectedObject();
            if (m_SelectedObject != null)
            {
                EditorGUILayout.LabelField("選択されたオブジェクト: " + m_SelectedObject.name);
                if (m_AllComponents.Any())
                {
                    m_SelectedComponentIndex = EditorGUILayout.Popup("コンポーネント選択", m_SelectedComponentIndex, m_ComponentNames);
                    m_SelectedComponent = m_AllComponents[Mathf.Clamp(m_SelectedComponentIndex, 0, m_AllComponents.Count - 1)];
                    if (GUILayout.Button("選択したコンポーネントと同名の全コンポーネントを削除"))
                        RemoveComponentsWithSameName();
                }
                else EditorGUILayout.LabelField("コンポーネントが見つからないですわ");
            }
            else EditorGUILayout.LabelField("オブジェクトが選択されてませんこと");
        }

        private void UpdateSelectedObject()
        {
            m_SelectedObject = Selection.activeGameObject;
            UpdateComponentList();
        }

        private void UpdateComponentList()
        {
            if (m_SelectedObject != null)
            {
                m_AllComponents = m_SelectedObject.GetComponentsInChildren<Component>().ToList();
                var uniqueTypes = m_AllComponents.Select(c => c.GetType()).Distinct().Where(t => t != typeof(Transform)).ToList();
                m_ComponentNames = uniqueTypes.Select(t => t.Name).ToArray();
                m_AllComponents = uniqueTypes.Select(t => m_SelectedObject.GetComponentInChildren(t)).ToList();
            }
        }

        private void RemoveComponentsWithSameName()
        {
            if (m_SelectedComponent != null)
            {
                string componentName = m_SelectedComponent.GetType().Name;
                var comps = m_SelectedObject.GetComponentsInChildren<Component>()
                    .Where(c => c.GetType().Name == componentName && !(c is Transform)).ToArray();
                foreach (var comp in comps) DestroyImmediate(comp);
                UpdateComponentList();
                EditorUtility.SetDirty(m_SelectedObject);
                m_SelectedComponentIndex = m_AllComponents.Count == 0 ? 0 : Mathf.Min(m_SelectedComponentIndex, m_AllComponents.Count - 1);
            }
        }
    }
}