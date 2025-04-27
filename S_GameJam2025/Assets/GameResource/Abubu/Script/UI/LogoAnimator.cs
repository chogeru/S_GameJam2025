using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Zenject;
using AbubuResource.Audio.SFX;
using Sirenix.OdinInspector;

[RequireComponent(typeof(RectTransform))]
public class LogoAnimator : MonoBehaviour
{
    [Inject] private PlaySfxChannel _playChannel;

    [LabelText("SubLogo SFX ID")]
    [ValueDropdown(nameof(GetAvailableSfxIds))]
    public string subLogoSfxId;

    [Header("ロゴ設定")]
    public RectTransform titleLogo;
    public RectTransform subLogo;

    [Header("アニメーション設定")]
    public Vector2 titleTargetPos = new Vector2(345f, 213f);
    public float dropDuration = 0.6f;
    public float squashDuration = 0.2f;
    public float bounceHeight = 15f;
    public float bounceDuration = 0.4f;
    public float subDelay = 0.3f;
    public float subPopDuration = 0.5f;

    private Vector3 initialTitleScale;
    private Vector3 initialSubScale;

    void Start()
    {
        initialTitleScale = titleLogo.localScale;
        initialSubScale = subLogo.localScale;

        float canvasH = ((RectTransform)titleLogo.parent).rect.height;
        titleLogo.anchoredPosition = new Vector2(titleTargetPos.x, canvasH + titleLogo.rect.height);
        titleLogo.localScale = initialTitleScale;

        subLogo.localScale = Vector3.zero;

        var seq = DOTween.Sequence();

        seq.Append(titleLogo
            .DOAnchorPos(titleTargetPos, dropDuration)
            .SetEase(Ease.InQuad)
        );

        seq.Append(titleLogo
            .DOScaleY(initialTitleScale.y * 0.8f, squashDuration)
            .SetEase(Ease.OutQuad)
        );
        seq.Join(titleLogo
            .DOScaleX(initialTitleScale.x * 1.2f, squashDuration)
            .SetEase(Ease.OutQuad)
        );

        seq.Append(titleLogo
            .DOScale(initialTitleScale, squashDuration)
            .SetEase(Ease.InQuad)
        );

        seq.Append(titleLogo
            .DOAnchorPosY(titleTargetPos.y + bounceHeight, bounceDuration)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutQuad)
        );

        seq.AppendInterval(subDelay);

        seq.Append(subLogo
            .DOScale(initialSubScale, subPopDuration)
            .SetEase(Ease.OutBack)
            .OnStart(() => {
                if (!string.IsNullOrEmpty(subLogoSfxId))
                    _playChannel.Raise(new SfxRequest(subLogoSfxId));
            })
        );
        seq.Join(subLogo
            .DORotate(new Vector3(0, 0, 15f), subPopDuration / 2)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutBack)
        );
    }

    private IEnumerable<string> GetAvailableSfxIds()
    {
        var svc = FindAnyObjectByType<SfxService>();
        if (svc != null && svc.SeIds != null)
            return svc.SeIds;
        return new List<string> { "<No SFX>" };
    }
}
