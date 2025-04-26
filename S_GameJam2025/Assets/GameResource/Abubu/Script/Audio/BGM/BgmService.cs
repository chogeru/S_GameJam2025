using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Zenject;
using UnityEngine.Audio;
using Sirenix.OdinInspector;
using System;

public class BgmService : MonoBehaviour, IBgmService
{
    [InfoBox("BGM再生用チャンネル登録")]
    [Inject] readonly PlayBgmChannel _playChannel;

    [Title("AudioMixerとストラテジー設定")]
    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private List<BgmStrategy> _strategies;

    private AudioSource _source;
    private Dictionary<string, BgmStrategy> _map;
    [ShowInInspector, ReadOnly]
    public List<string> StrategyIds => _strategies.ConvertAll(s => s.Id);
    [InfoBox("On Awake: イベント登録 → Source 生成 → マップ構築")]
    void Awake()
    {
        _playChannel.OnRaised += id => { _ = Play(id); };

        _source = gameObject.AddComponent<AudioSource>();
        _source.loop = true;
        var groups = _mixer.FindMatchingGroups("Master/BGM");
        if (groups != null && groups.Length > 0)
            _source.outputAudioMixerGroup = groups[0];
        else
            Debug.LogWarning("[BgmService] ‘Master/BGM’グループがMixer見つからない");
        _map = new Dictionary<string, BgmStrategy>();
        foreach (var s in _strategies) _map[s.Id] = s;
    }

    public async UniTask Play(string id)
    {
        if (!_map.TryGetValue(id, out var strat))
        {
            Debug.LogWarning($"[BgmService] ID '{id}' が見つかりません");
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
            Debug.LogError("[BgmService] 'BgmOn' スナップショットが見つかりません");
        }

        _source.PlayDelayed(strat.FadeDuration);
        await UniTask.Delay(TimeSpan.FromSeconds(strat.FadeDuration));
    }


    [InfoBox("BGM の停止：フェードアウト → Stop")]
    public async UniTask Stop()
    {
        if (!_source.isPlaying) return;
        _mixer.FindSnapshot("BgmOff").TransitionTo(1f);
        await UniTask.Delay(System.TimeSpan.FromSeconds(1f));
        _source.Stop();
    }
}

[System.Serializable]
[InfoBox("BGM再生設定")]
public class BgmStrategy
{
    public string Id;

    [Tooltip("再生するClip")]
    public AudioClip Clip;

    [Tooltip("フェードの時間")]
    public float FadeDuration = 1f;
}
