using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AbubuResource.Scene
{
    [DisallowMultipleComponent]
    public sealed class SceneDirector : SingletonBehaviour<SceneDirector>, ISceneDirector
    {
        [Title("利用可能なシーン一覧")]
        [TableList(AlwaysExpanded = true, DrawScrollView = true, ShowIndexLabels = true)]
        [InfoBox("Project ビューからシーンアセットをドラッグして登録", InfoMessageType.Info)]
        [SerializeField]
        private List<SceneEntry> scenes = new List<SceneEntry>();

        [Title("スライドトランジション設定")]
        [Required, SerializeField]
        private RectTransform slidePanel;

        [SerializeField, Min(0f)]
        private float slideDuration = 1f;

        [BoxGroup("スライドトランジション設定")]
        [LabelText("補間カーブ"), Tooltip("スライド時のイージングカーブ設定")]
        [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);  // イージングカーブ

        private float panelWidth;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Canvas.ForceUpdateCanvases();
            panelWidth = slidePanel.rect.width;
            slidePanel.anchoredPosition = new Vector2(panelWidth, 0f);
        }

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

        public void Load(string sceneName)
        {
            StartCoroutine(LoadWithTransition(sceneName));
        }

        public void Load(int buildIndex)
        {
            StartCoroutine(LoadWithTransition(buildIndex));
        }

        private IEnumerator LoadWithTransition(string sceneName)
        {
            yield return Slide(panelWidth, 0f);  // 画面外→中央
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
            yield return Slide(0f, -panelWidth);  // 中央→画面外
            ResetPanel();
        }

        private IEnumerator LoadWithTransition(int buildIndex)
        {
            yield return Slide(panelWidth, 0f);
            var op = SceneManager.LoadSceneAsync(buildIndex);
            while (!op.isDone) yield return null;
            yield return Slide(0f, -panelWidth);
            ResetPanel();
        }

        private IEnumerator Slide(float fromX, float toX)
        {
            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                float eval = slideCurve.Evaluate(t);
                float x = Mathf.Lerp(fromX, toX, eval);
                slidePanel.anchoredPosition = new Vector2(x, 0f);
                yield return null;
            }
            slidePanel.anchoredPosition = new Vector2(toX, 0f);
        }

        private void ResetPanel()
        {
            slidePanel.anchoredPosition = new Vector2(panelWidth, 0f);
        }

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
}
