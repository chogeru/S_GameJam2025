using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace TelePresent.AudioSyncPro.UI
{
    public class UIImageBeatScaler : MonoBehaviour
    {
        [SerializeField] private GameObject bgmSource;
        private UnityEngine.AudioSource audioSync;
        public float audioInitDelay = 0.5f;

        [SerializeField, ListDrawerSettings(Expanded = true, ShowIndexLabels = true)]
        private List<RectTransform> targets = new List<RectTransform>();

        [BoxGroup("スケール設定"), MinValue(0f)]
        public float minScale = 1f;
        [BoxGroup("スケール設定"), MinValue(0f)]
        public float maxScale = 1.3f;
        [BoxGroup("スケール設定")]
        public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [BoxGroup("トゥイーン設定"), MinValue(0f)]
        public float tweenDuration = 0.05f;

        [BoxGroup("感度"), Range(0f, 5f)]
        public float sensitivity = 1f;

        [BoxGroup("ビート設定"), MinValue(0.01f)]
        public float pollInterval = 0.1f;
        [BoxGroup("ビート設定"), Range(0f, 1f)]
        public float beatThreshold = 0.25f;

        [BoxGroup("パンチ設定"), MinValue(0f)]
        public float punchScaleStrength = 0.1f;
        [BoxGroup("パンチ設定"), MinValue(0f)]
        public float punchPosStrength = 10f;
        [BoxGroup("パンチ設定"), MinValue(0f)]
        public float punchRotStrength = 5f;
        [BoxGroup("パンチ設定"), MinValue(0f)]
        public float punchDuration = 0.15f;
        [BoxGroup("パンチ設定"), MinValue(1)]
        public int punchVibrato = 3;
        [BoxGroup("パンチ設定"), Range(0f, 1f)]
        public float punchElasticity = 0.2f;

        [BoxGroup("カラー設定")]
        public Image uiImage;
        [BoxGroup("カラー設定")]
        public Color beatColor = Color.white;

        private Tween idleTween;
        private List<Vector3> baseScales = new List<Vector3>();

        [BoxGroup("オーディオクリップ設定"), MinValue(128)]
        public int sampleWindowSize = 1024;
        private float[] sampleBuffer;

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(audioInitDelay), cancellationToken: this.GetCancellationTokenOnDestroy());

            audioSync = bgmSource.GetComponent<UnityEngine.AudioSource>();
            if (audioSync == null || audioSync.clip == null)
            {
                Debug.LogError("AudioSource または AudioClip が見つかりません。");
                return;
            }

            sampleBuffer = new float[sampleWindowSize];

            baseScales = new List<Vector3>(targets.Count);
            foreach (var rt in targets)
                baseScales.Add(rt.localScale);

            StageDataManager.OnBGTransformsUpdated += UpdateBaseScales;

            StartIdleBreathing();
            ObserveBeatAsync().Forget();
        }

        private void OnDestroy()
        {
            StageDataManager.OnBGTransformsUpdated -= UpdateBaseScales;
        }

        private void UpdateBaseScales()
        {
            baseScales.Clear();
            foreach (var rt in targets)
                baseScales.Add(rt.localScale);
        }

        private void StartIdleBreathing()
        {
            idleTween = DOTween.To(
                () => 0f,
                t =>
                {
                    float sNorm = Mathf.Lerp(minScale, minScale + (maxScale - minScale) * 0.1f, t);
                    for (int i = 0; i < targets.Count; i++)
                        targets[i].localScale = baseScales[i] * sNorm;
                },
                1f,
                2f
            )
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
        }

        private async UniTaskVoid ObserveBeatAsync()
        {
            while (true)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(pollInterval), cancellationToken: this.GetCancellationTokenOnDestroy());

                if (!audioSync.isPlaying) continue;

                int samplePos = audioSync.timeSamples;
                int startSample = Mathf.Clamp(samplePos - sampleWindowSize / 2, 0, audioSync.clip.samples - sampleWindowSize);
                audioSync.clip.GetData(sampleBuffer, startSample);

                float sum = 0f;
                for (int i = 0; i < sampleWindowSize; i++)
                    sum += sampleBuffer[i] * sampleBuffer[i];
                float rms = Mathf.Clamp01(Mathf.Sqrt(sum / sampleWindowSize) * sensitivity);

                float eval = scaleCurve.Evaluate(rms);
                float targetScaleNorm = Mathf.Lerp(minScale, maxScale, eval);
                bool isBeat = rms > beatThreshold;

                if (isBeat) idleTween.Pause();
                else if (!idleTween.IsPlaying()) idleTween.Play();

                for (int i = 0; i < targets.Count; i++)
                {
                    var rt = targets[i];
                    var baseScale = baseScales[i];

                    rt.DOScale(baseScale * targetScaleNorm, tweenDuration).SetEase(Ease.OutSine);

                    if (isBeat)
                    {
                        DOTween.Sequence()
                            .Join(rt.DOPunchScale(baseScale * punchScaleStrength, punchDuration, punchVibrato, punchElasticity).SetEase(Ease.OutSine))
                            .Join(rt.DOPunchAnchorPos(Vector2.up * punchPosStrength, punchDuration, punchVibrato, punchElasticity).SetEase(Ease.OutSine))
                            .Join(rt.DOPunchRotation(new Vector3(0, 0, punchRotStrength), punchDuration, punchVibrato, punchElasticity).SetEase(Ease.OutSine))
                            .OnComplete(() => idleTween.Restart());
                    }
                }

            }
        }
    }
}
