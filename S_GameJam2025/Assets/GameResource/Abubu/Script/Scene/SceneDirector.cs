using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using AbubuResource.Audio.SFX;
using Zenject;

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

        [Title("シーン切替時の SFX 設定")]
        [TableList(AlwaysExpanded = true, DrawScrollView = true, ShowIndexLabels = true)]
        [InfoBox("シーン名ごとに再生する SFX を設定します", InfoMessageType.Info)]
        [SerializeField]
        private List<SceneSfxEntry> sceneSfxList = new List<SceneSfxEntry>();

        [Title("スライドトランジション設定")]
        [Required, SerializeField]
        private RectTransform slidePanel;

        [SerializeField, Min(0f)]
        private float slideDuration = 1f;

        [BoxGroup("スライドトランジション設定")]
        [LabelText("補間カーブ"), Tooltip("スライド時のイージングカーブ設定")]
        [SerializeField]
        private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Required, SerializeField, BoxGroup("スライドトランジション設定")]
        private PanelChildAnimator panelAnimator;

        [SerializeField, Tooltip("中央で静止する秒数"), BoxGroup("スライドトランジション設定")]
        private float centerPause = 0.6f;

        [SerializeField, Tooltip("子画像アニメ開始までの遅延秒数"), BoxGroup("スライドトランジション設定")]
        private float childAnimDelay = 0.3f;

        [Inject]
        private PlaySfxChannel _playChannel;

        private float panelWidth;

        protected override void Awake()
        {
            base.Awake();
            Canvas.ForceUpdateCanvases();
            panelWidth = slidePanel.rect.width;
            slidePanel.anchoredPosition = new Vector2(panelWidth, 0f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (var e in scenes)
                if (e?.sceneAsset != null)
                    e.sceneName = e.sceneAsset.name;
        }
#endif

        public void Load(string sceneName)
        {
            StartCoroutine(LoadWithTransition(sceneName));
        }

        public void Load(int buildIndex)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            StartCoroutine(LoadWithTransition(name));
        }

        public void ReloadCurrent()
        {
            var cur = SceneManager.GetActiveScene();
            Load(cur.buildIndex);
        }

        public void NextScene()
        {
            int next = (SceneManager.GetActiveScene().buildIndex + 1) % SceneManager.sceneCountInBuildSettings;
            Load(next);
        }

        public void PreviousScene()
        {
            int idx = SceneManager.GetActiveScene().buildIndex;
            int prev = idx == 0 ? SceneManager.sceneCountInBuildSettings - 1 : idx - 1;
            Load(prev);
        }

        private IEnumerator LoadWithTransition(string sceneName)
        {
            Application.backgroundLoadingPriority = ThreadPriority.Low;
            PlaySfxForScene(sceneName);

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            op.allowSceneActivation = false;

            StartCoroutine(PlayAfterDelay());
            yield return Slide(panelWidth, 0f);

            while (op.progress < 0.9f) yield return null;
            if (centerPause > 0f) yield return new WaitForSeconds(centerPause);
            yield return Slide(0f, -panelWidth);

            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            Application.backgroundLoadingPriority = ThreadPriority.Normal;
            ResetPanel();
            panelAnimator?.Stop();
        }

        private IEnumerator PlayAfterDelay()
        {
            yield return new WaitForSeconds(childAnimDelay);
            panelAnimator?.Play();
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

        private void PlaySfxForScene(string sceneName)
        {
            var entry = sceneSfxList.FirstOrDefault(x => x.sceneName == sceneName);
            if (entry != null && !string.IsNullOrEmpty(entry.sfxId) && entry.sfxId != "<No SFX>")
                _playChannel.Raise(new SfxRequest(entry.sfxId));
        }

        public IEnumerable<string> GetAvailableSfxIds()
        {
            var svc = FindAnyObjectByType<SfxService>();
            return (svc != null && svc.SeIds != null && svc.SeIds.Count > 0)
                ? svc.SeIds
                : new[] { "<No SFX>" };
        }

        public IEnumerable<string> GetSceneNames()
        {
            return scenes != null
                ? scenes.Select(e => e.sceneName)
                : Enumerable.Empty<string>();
        }

        [TitleGroup("デバッグ")]
        [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        private void DebugReload() => ReloadCurrent();

        [Button(ButtonSizes.Large), GUIColor(0.2f, 0.6f, 1f)]
        private void DebugNext() => NextScene();

        [Button(ButtonSizes.Large), GUIColor(1f, 0.4f, 0.4f)]
        private void DebugPrevious() => PreviousScene();


        [Serializable]
        public class SceneEntry : ISceneInfo
        {
#if UNITY_EDITOR
            [HorizontalGroup("Row", 90), HideLabel, PreviewField(64)]
            public SceneAsset sceneAsset;
#endif
            [HorizontalGroup("Row"), ReadOnly]
            public string sceneName;

            [LabelText("再生する SFX ID")]
            [ValueDropdown("@( SceneDirector.Instance != null ? SceneDirector.Instance.GetAvailableSfxIds() : (IEnumerable<string>)new string[0] )")]
            public string sfxId;

            [Button("シーンをロード"), HorizontalGroup("Row", Width = 90), GUIColor(0.2f, 0.6f, 1f)]
            private void LoadScene() => SceneDirector.Instance.Load(sceneName);

            public string Name => sceneName;
        }
        [Serializable]
        private class SceneSfxEntry
        {
            [LabelText("シーン名")]
            public string sceneName;

            [LabelText("SFX ID")]
            [ValueDropdown("@( SceneDirector.Instance != null ? SceneDirector.Instance.GetAvailableSfxIds() : (IEnumerable<string>)new string[0] )")]
            public string sfxId;
        }
    }
}
