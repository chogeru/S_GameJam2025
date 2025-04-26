using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using AbubuResource.Audio.SFX;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Button))]
public class ButtonSfx : SerializedMonoBehaviour
{
    [Inject] private PlaySfxChannel _playChannel;

    [LabelText("çƒê∂Ç∑ÇÈ SFX ID")]
    [ValueDropdown(nameof(GetAvailableSfxIds))]
    public string sfxId;

    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClickPlay);
    }

    void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClickPlay);
    }

    private void OnClickPlay()
    {
        if (string.IsNullOrEmpty(sfxId)) return;
        _playChannel.Raise(new SfxRequest(sfxId));
    }


    private IEnumerable<string> GetAvailableSfxIds()
    {
        var svc = FindAnyObjectByType<SfxService>();
        if (svc != null && svc.SeIds != null)
            return svc.SeIds;
        return new List<string> { "<No SFX>" };
    }
}
