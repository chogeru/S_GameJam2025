using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace AbubuResource.Scene
{
    [DisallowMultipleComponent]
    public sealed class PanelChildAnimator : MonoBehaviour
    {
        [Header("Swing (éŒÇﬂóhÇÍ)")]
        [SerializeField] List<RectTransform> swingTargets = new();
        [SerializeField, Range(1f, 25f)] float swingAngle = 12f;
        [SerializeField, Range(.1f, 5f)] float swingSec = 1.8f;

        [Header("Pop (èáî‘Ç…ägëÂ)")]
        [SerializeField] List<RectTransform> popTargets = new();
        [SerializeField] float popDelay = .05f;
        [SerializeField] float popSec = .15f;
        [SerializeField] Ease popEase = Ease.OutBack;

        Sequence popSequence;
        readonly List<Tweener> swingTweens = new();
        readonly List<Vector3> defaultAngles = new();

        void Awake()
        {
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(200, 50);

            foreach (var rt in popTargets) rt.localScale = Vector3.zero;
            foreach (var rt in swingTargets) defaultAngles.Add(rt.localEulerAngles);

            BuildPopSequence();
        }

        [Button("Play")]
        public void Play()
        {
            Stop();

            foreach (var rt in popTargets) rt.localScale = Vector3.zero;

            foreach (var rt in swingTargets)
            {
                rt.localEulerAngles = defaultAngles[swingTargets.IndexOf(rt)];
                swingTweens.Add(
                    rt.DORotate(new Vector3(0, 0, swingAngle), swingSec)
                      .SetLoops(-1, LoopType.Yoyo)
                      .SetEase(Ease.InOutSine)
                );
            }

            popSequence.Restart();
        }

        [Button("Stop")]
        public void Stop()
        {
            foreach (var tw in swingTweens) tw?.Kill(false);
            swingTweens.Clear();

            for (int i = 0; i < swingTargets.Count; i++)
                swingTargets[i].localEulerAngles = defaultAngles[i];

            popSequence.Rewind();
            popSequence.Pause();
        }

        void BuildPopSequence()
        {
            popSequence = DOTween.Sequence().SetAutoKill(false).Pause();

            for (int i = 0; i < popTargets.Count; i++)
            {
                float at = i * popDelay;
                popSequence.Insert(at,
                    popTargets[i]
                        .DOScale(1f, popSec)
                        .SetEase(popEase)
                        .From(0f)
                );
            }
        }
    }
}
