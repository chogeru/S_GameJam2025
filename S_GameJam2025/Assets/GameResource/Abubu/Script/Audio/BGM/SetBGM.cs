using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Zenject;
using UnityEngine;
namespace AbubuResource.Audio.BGM
{
    public class SetBGM : SerializedMonoBehaviour
    {
        [Inject] private IBgmService _bgm;

        [Title("BGM 再生設定")]
        [LabelText("再生する BGM ID")]
        [ValueDropdown("GetAvailableBgmIds")]
        public string SelectedBgmId;

        private void Start()
        {
            if (_bgm != null)
                _bgm.Play(SelectedBgmId)
                    .Forget(ex => Debug.LogException(ex));
            else
                Debug.LogError("IBgmService が注入されてない");
        }

        private static IEnumerable<string> GetAvailableBgmIds()
        {
            var svc = Object.FindFirstObjectByType<BgmService>();
            if (svc == null) yield break;
            foreach (var id in svc.StrategyIds)
                yield return id;
        }
    }
}