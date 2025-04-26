using Zenject;
using Sirenix.OdinInspector;
using UnityEngine;

public class AudioInstaller : MonoInstaller
{
    [InfoBox("SFX 再生チャンネル")]
    [SerializeField] private PlaySfxChannel _playSfxChannelAsset;

    [InfoBox("BGM 再生チャンネル")]
    [SerializeField] private PlayBgmChannel _playBgmChannelAsset;

    [InfoBox("依存性注入バインドを登録")]
    public override void InstallBindings()
    {
        Container.Bind<PlayBgmChannel>()
                   .FromInstance(_playBgmChannelAsset)
                   .AsSingle();
        Container.Bind<PlaySfxChannel>()
                 .FromInstance(_playSfxChannelAsset)
                 .AsSingle();

        Container.Bind<IBgmService>()
                 .To<BgmService>()
                 .FromComponentInHierarchy()
                 .AsSingle();
        Container.Bind<ISfxService>()
                 .To<SfxService>()
                 .FromComponentInHierarchy()
                 .AsSingle();
    }
}