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
        private AudioSourcePlus audioSync;

        [SerializeField, ListDrawerSettings(Expanded = true, ShowIndexLabels = true)]
        private List<RectTransform> targets = new List<RectTransform>();

        [BoxGroup("�X�P�[���ݒ�"), MinValue(0f)]
        public float minScale = 1f;
        [BoxGroup("�X�P�[���ݒ�"), MinValue(0f)]
        public float maxScale = 1.3f;
        [BoxGroup("�X�P�[���ݒ�")]
        public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [BoxGroup("�g�D�C�[���ݒ�"), MinValue(0f)]
        public float tweenDuration = 0.05f;

        [BoxGroup("���x"), Range(0f, 5f)]
        public float sensitivity = 1f;

        [BoxGroup("�r�[�g�ݒ�"), MinValue(0.01f)]
        public float pollInterval = 0.1f;
        [BoxGroup("�r�[�g�ݒ�"), Range(0f, 1f)]
        public float beatThreshold = 0.25f;

        [BoxGroup("�p���`�ݒ�"), MinValue(0f)]
        public float punchScaleStrength = 0.1f;
        [BoxGroup("�p���`�ݒ�"), MinValue(0f)]
        public float punchPosStrength = 10f;
        [BoxGroup("�p���`�ݒ�"), MinValue(0f)]
        public float punchRotStrength = 5f;
        [BoxGroup("�p���`�ݒ�"), MinValue(0f)]
        public float punchDuration = 0.15f;
        [BoxGroup("�p���`�ݒ�"), MinValue(1)]
        public int punchVibrato = 3;
        [BoxGroup("�p���`�ݒ�"), Range(0f, 1f)]
        public float punchElasticity = 0.2f;

        [BoxGroup("�J���[�ݒ�")]
        public Image uiImage;
        [BoxGroup("�J���[�ݒ�")]
        public Color beatColor = Color.white;

        private Tween idleTween;
        private void Start()
        {
            if (bgmSource == null)
            {
                Debug.LogError("[UIImageBeatScaler] BGMManager �����蓖�Ă��Ă��܂���B");
                enabled = false;
                return;
            }

            audioSync = bgmSource.GetComponent<AudioSourcePlus>();
            if (audioSync == null)
            {
                Debug.LogError("[UIImageBeatScaler] ���蓖�Ă� BGMManager �� AudioSourcePlus ��������܂���B");
                enabled = false;
                return;
            }

            StartIdleBreathing();
            ObserveBeatAsync().Forget();
        }

        private void StartIdleBreathing()
        {
            idleTween = DOTween.To(
                () => 0f,
                t =>
                {
                    float s = Mathf.Lerp(minScale, minScale + (maxScale - minScale) * 0.1f, t);
                    foreach (var rt in targets)
                    {
                        rt.localScale = new Vector3(s, s, 1f);
                    }
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
                await UniTask.Delay(TimeSpan.FromSeconds(pollInterval),
                    cancellationToken: this.GetCancellationTokenOnDestroy());

                if (!audioSync.isPlaying)
                    continue;

                float rms = Mathf.Clamp01(audioSync.rmsValue * sensitivity);

                float eval = scaleCurve.Evaluate(rms);
                float targetScale = Mathf.Lerp(minScale, maxScale, eval);

                bool isBeat = rms > beatThreshold;
                if (isBeat)
                {
                    idleTween.Pause();
                }
                else
                {
                    if (!idleTween.IsPlaying())
                        idleTween.Play();
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    var rt = targets[i];
                    float delay = i * 0.00f;

                    rt.DOScale(new Vector3(targetScale, targetScale, 1f), tweenDuration)
                      .SetDelay(delay)
                      .SetEase(Ease.OutSine);

                    if (isBeat)
                    {
                        var seq = DOTween.Sequence()
                            .SetDelay(delay)
                            .Join(rt.DOPunchScale(
                                new Vector3(punchScaleStrength, punchScaleStrength, 0f),
                                punchDuration, punchVibrato, punchElasticity
                            ).SetEase(Ease.OutSine))
                            .Join(rt.DOPunchAnchorPos(
                                Vector2.up * punchPosStrength,
                                punchDuration, punchVibrato, punchElasticity
                            ).SetEase(Ease.OutSine))
                            .Join(rt.DOPunchRotation(
                                new Vector3(0f, 0f, punchRotStrength),
                                punchDuration, punchVibrato, punchElasticity
                            ).SetEase(Ease.OutSine))
                            .OnComplete(() =>
                            {
                                idleTween.Restart();
                            });
                    }

                    if (uiImage != null && isBeat)
                    {
                        uiImage
                            .DOColor(beatColor, punchDuration * 0.5f)
                            .SetLoops(2, LoopType.Yoyo)
                            .SetEase(Ease.OutSine)
                            .SetDelay(delay);
                    }
                }
            }
        }
    }
}
