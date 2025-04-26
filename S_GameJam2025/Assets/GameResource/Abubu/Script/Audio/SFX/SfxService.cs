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
        [InfoBox("SFX���ʉ����v�[���ŊǗ����A�Đ��𐧌䂷��T�[�r�X")]
        [Inject] readonly PlaySfxChannel _playChannel;

        [Title("�ݒ�ꗗ")]
        [InfoBox("�e���ʉ���ID��AudioClip�ƃv�[���T�C�Y���`")]
        [SerializeField] private List<SeConfig> _configs;
        [Title("SFX�p�~�L�T�[�O���[�v")]
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;

        private Dictionary<string, ObjectPool<AudioSource>> _pools;
        [ShowInInspector, ReadOnly]
        public List<string> SeIds => _configs.ConvertAll(cfg => cfg.Id);


        [InfoBox("On Awake: �C�x���g�o�^ �� �v�[������")]
        private void Awake()
        {
            if (_playChannel != null)
                _playChannel.OnRaised += HandlePlay;
            else
                Debug.LogError("PlaySfxChannel��null");

            InitPools();
        }


        [InfoBox("On Destroy: �C�x���g����")]
        private void OnDestroy()
        {
            if (_playChannel != null)
                _playChannel.OnRaised -= HandlePlay;
        }

        [InfoBox("�eSeConfig����ObjectPool���쐬���A���O����")]
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

        [InfoBox("�C�x���g���Ύ���SFX���Đ����čĐ�������Ƀv�[���֕ԋp")]
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

        [InfoBox("���ʉ����Đ�")]
        public void Play(string id) => _playChannel.Raise(new SfxRequest(id));


        [InfoBox("ID�ƍ��W�w��ōĐ�")]
        public void PlayAt(string id, Vector3 position) => _playChannel.Raise(new SfxRequest(id, position));

        [InfoBox("�w��b��Ƀv�[���ԋp")]
        private async UniTaskVoid ReturnAfter(AudioSource src, float delay, ObjectPool<AudioSource> pool)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(delay));
            pool.Return(src);
        }

        /// <summary>
        /// �V�KGameObject��AudioSource�𐶐����ĕԋp�p�ɏ���
        /// </summary>
        [InfoBox("AudioSource�𐶐�")]
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
    [InfoBox("�T�E���h�ݒ�")]
    public class SeConfig
    {
        public string Id;

        [Tooltip("�Đ�����Clip")]
        public AudioClip Clip;

        [Tooltip("�v�[���T�C�Y")]
        public int PoolSize = 10;
    }
}