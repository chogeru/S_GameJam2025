using Zenject;
using Sirenix.OdinInspector;
using UnityEngine;

public class AudioInstaller : MonoInstaller
{
    [InfoBox("SFX �Đ��`�����l��")]
    [SerializeField] private PlaySfxChannel _playSfxChannelAsset;

    [InfoBox("BGM �Đ��`�����l��")]
    [SerializeField] private PlayBgmChannel _playBgmChannelAsset;

    [InfoBox("�ˑ��������o�C���h��o�^")]
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