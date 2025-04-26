using System;
using UnityEngine;
namespace AbubuResource.Audio.SFX
{
    [CreateAssetMenu(menuName = "Audio/PlaySfxChannel")]
    public class PlaySfxChannel : ScriptableObject
    {
        public event Action<SfxRequest> OnRaised;
        public void Raise(in SfxRequest req) => OnRaised?.Invoke(req);
    }

    public readonly struct SfxRequest
    {
        public readonly string Id;
        public readonly Vector3? Position;

        public SfxRequest(string id, Vector3? pos = null)
        {
            Id = id;
            Position = pos;
        }
    }
}