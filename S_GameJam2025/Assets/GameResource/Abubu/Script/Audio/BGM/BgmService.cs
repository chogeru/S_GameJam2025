using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Zenject;
using UnityEngine.Audio;
using Sirenix.OdinInspector;
using System;

public class BgmService : MonoBehaviour, IBgmService
{
    [InfoBox("BGM�Đ��p�`�����l���o�^")]
    [Inject] readonly PlayBgmChannel _playChannel;

    [Title("AudioMixer�ƃX�g���e�W�[�ݒ�")]
    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private List<BgmStrategy> _strategies;

    private AudioSource _source;
    private Dictionary<string, BgmStrategy> _map;
    [ShowInInspector, ReadOnly]
    public List<string> StrategyIds => _strategies.ConvertAll(s => s.Id);
    [InfoBox("On Awake: �C�x���g�o�^ �� Source ���� �� �}�b�v�\�z")]
    void Awake()
    {
        _playChannel.OnRaised += id => { _ = Play(id); };

        _source = gameObject.AddComponent<AudioSource>();
        _source.loop = true;
        var groups = _mixer.FindMatchingGroups("Master/BGM");
        if (groups != null && groups.Length > 0)
            _source.outputAudioMixerGroup = groups[0];
        else
            Debug.LogWarning("[BgmService] �eMaster/BGM�f�O���[�v��Mixer������Ȃ�");
        _map = new Dictionary<string, BgmStrategy>();
        foreach (var s in _strategies) _map[s.Id] = s;
    }

    public async UniTask Play(string id)
    {
        if (!_map.TryGetValue(id, out var strat))
        {
            Debug.LogWarning($"[BgmService] ID '{id}' ��������܂���");
            return;
        }

        var offSnap = _mixer.FindSnapshot("BgmOff");
        if (_source.isPlaying && offSnap != null)
        {
            offSnap.TransitionTo(strat.FadeDuration);
        }

        _source.clip = strat.Clip;

        var onSnap = _mixer.FindSnapshot("BgmOn");
        if (onSnap != null)
        {
            onSnap.TransitionTo(strat.FadeDuration);
        }
        else
        {
            Debug.LogError("[BgmService] 'BgmOn' �X�i�b�v�V���b�g��������܂���");
        }

        _source.PlayDelayed(strat.FadeDuration);
        await UniTask.Delay(TimeSpan.FromSeconds(strat.FadeDuration));
    }


    [InfoBox("BGM �̒�~�F�t�F�[�h�A�E�g �� Stop")]
    public async UniTask Stop()
    {
        if (!_source.isPlaying) return;
        _mixer.FindSnapshot("BgmOff").TransitionTo(1f);
        await UniTask.Delay(System.TimeSpan.FromSeconds(1f));
        _source.Stop();
    }
}

[System.Serializable]
[InfoBox("BGM�Đ��ݒ�")]
public class BgmStrategy
{
    public string Id;

    [Tooltip("�Đ�����Clip")]
    public AudioClip Clip;

    [Tooltip("�t�F�[�h�̎���")]
    public float FadeDuration = 1f;
}
