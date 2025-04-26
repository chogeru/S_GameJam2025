using System.Collections.Generic;
using UnityEngine;
using uPools;
using Cysharp.Threading.Tasks;
using Zenject;
using Sirenix.OdinInspector;
using UnityEngine.Audio;
namespace AbubuResource.Audio.SFX
{
    public class SfxService : MonoBehaviour, ISfxService
    {
        [InfoBox("SFX効果音をプールで管理し、再生を制御するサービス")]
        [Inject] readonly PlaySfxChannel _playChannel;

        [Title("設定一覧")]
        [InfoBox("各効果音のIDとAudioClipとプールサイズを定義")]
        [SerializeField] private List<SeConfig> _configs;
        [Title("SFX用ミキサーグループ")]
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;

        private Dictionary<string, ObjectPool<AudioSource>> _pools;
        [ShowInInspector, ReadOnly]
        public List<string> SeIds => _configs.ConvertAll(cfg => cfg.Id);


        [InfoBox("On Awake: イベント登録 → プール生成")]
        private void Awake()
        {
            if (_playChannel != null)
                _playChannel.OnRaised += HandlePlay;
            else
                Debug.LogError("PlaySfxChannelがnull");

            InitPools();
        }


        [InfoBox("On Destroy: イベント解除")]
        private void OnDestroy()
        {
            if (_playChannel != null)
                _playChannel.OnRaised -= HandlePlay;
        }

        [InfoBox("各SeConfigからObjectPoolを作成し、事前生成")]
        private void InitPools()
        {
            _pools = new Dictionary<string, ObjectPool<AudioSource>>();
            foreach (var cfg in _configs)
            {
                var pool = new ObjectPool<AudioSource>(
                    createFunc: () => CreateSource(cfg.Id),
                    onRent: src => src.gameObject.SetActive(true),
                    onReturn: src => src.gameObject.SetActive(false),
                    onDestroy: src => Destroy(src.gameObject)
                );
                pool.Prewarm(cfg.PoolSize);
                _pools[cfg.Id] = pool;
            }
        }

        [InfoBox("イベント発火時にSFXを再生して再生完了後にプールへ返却")]
        private void HandlePlay(SfxRequest req)
        {
            if (!_pools.TryGetValue(req.Id, out var pool)) return;

            var cfg = _configs.Find(x => x.Id == req.Id);
            var src = pool.Rent();

            if (req.Position.HasValue)
            {
                src.transform.position = req.Position.Value;
                src.spatialBlend = 1f;
            }
            else
            {
                src.spatialBlend = 0f;
            }

            src.clip = cfg.Clip;
            src.Play();

            ReturnAfter(src, cfg.Clip.length, pool).Forget();
        }

        [InfoBox("効果音を再生")]
        public void Play(string id) => _playChannel.Raise(new SfxRequest(id));


        [InfoBox("IDと座標指定で再生")]
        public void PlayAt(string id, Vector3 position) => _playChannel.Raise(new SfxRequest(id, position));

        [InfoBox("指定秒後にプール返却")]
        private async UniTaskVoid ReturnAfter(AudioSource src, float delay, ObjectPool<AudioSource> pool)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(delay));
            pool.Return(src);
        }

        /// <summary>
        /// 新規GameObjectとAudioSourceを生成して返却用に準備
        /// </summary>
        [InfoBox("AudioSourceを生成")]
        private AudioSource CreateSource(string id)
        {
            var go = new GameObject($"SFX_{id}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            if (_sfxMixerGroup != null)
                src.outputAudioMixerGroup = _sfxMixerGroup;
            return src;
        }
    }

    [System.Serializable]
    [InfoBox("サウンド設定")]
    public class SeConfig
    {
        public string Id;

        [Tooltip("再生するClip")]
        public AudioClip Clip;

        [Tooltip("プールサイズ")]
        public int PoolSize = 10;
    }
}