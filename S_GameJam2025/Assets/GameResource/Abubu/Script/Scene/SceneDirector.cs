using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class SceneDirector : SingletonBehaviour<SceneDirector>, ISceneDirector
{
    [Title("利用可能なシーン一覧")]
    [TableList(AlwaysExpanded = true, DrawScrollView = true, ShowIndexLabels = true)]
    [InfoBox("Project ビューからシーンアセットをドラッグして登録", InfoMessageType.Info)]
    [SerializeField]
    private List<SceneEntry> scenes = new List<SceneEntry>();

    [Serializable]
    public class SceneEntry : ISceneInfo
    {
#if UNITY_EDITOR
        [HorizontalGroup("Row", 90), HideLabel, PreviewField(64)]
        [Tooltip("ロードしたいシーンアセットをここにドラッグ&ドロップ")]
        public SceneAsset sceneAsset;
#endif
        [HorizontalGroup("Row")]
        [ReadOnly]
        public string sceneName;

        [Button("シーンをロード"), HorizontalGroup("Row", Width = 90)]
        [GUIColor(0.2f, 0.6f, 1f)]
        private void LoadScene() => SceneDirector.Instance.Load(sceneName);

        public string Name => sceneName;
    }

    public void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    public void Load(int buildIndex) => SceneManager.LoadScene(buildIndex);

    /// <summary>現在アクティブなシーンをリロード</summary>
    public void ReloadCurrent()
    {
        var current = SceneManager.GetActiveScene();
        Load(current.buildIndex);
    }

    /// <summary>次のシーンをロード</summary>
    public void NextScene()
    {
        int nextIndex = (SceneManager.GetActiveScene().buildIndex + 1) % SceneManager.sceneCountInBuildSettings;
        Load(nextIndex);
    }

    /// <summary>前のシーンをロード</summary>
    public void PreviousScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int prevIndex = currentIndex == 0 ? SceneManager.sceneCountInBuildSettings - 1 : currentIndex - 1;
        Load(prevIndex);
    }

    [TitleGroup("デバッグ")]
    [Button(ButtonSizes.Large)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    private void DebugReload() => ReloadCurrent();

    [Button(ButtonSizes.Large)]
    [GUIColor(0.2f, 0.6f, 1f)]
    private void DebugNext() => NextScene();

    [Button(ButtonSizes.Large)]
    [GUIColor(1f, 0.4f, 0.4f)]
    private void DebugPrevious() => PreviousScene();

#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var entry in scenes)
        {
            if (entry?.sceneAsset != null)
                entry.sceneName = entry.sceneAsset.name;
        }
    }
#endif
}
