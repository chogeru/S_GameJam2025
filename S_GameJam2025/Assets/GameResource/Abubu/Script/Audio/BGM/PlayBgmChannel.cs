using System;
using UnityEngine;
namespace AbubuResource.Audio.BGM
{
    [CreateAssetMenu(menuName = "Audio/PlayBgmChannel")]
    public class PlayBgmChannel : ScriptableObject
    {
        public event Action<string> OnRaised;

        public void Raise(string id) => OnRaised?.Invoke(id);
    }
}