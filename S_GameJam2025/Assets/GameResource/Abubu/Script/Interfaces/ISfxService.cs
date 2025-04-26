using UnityEngine;
public interface ISfxService
{
    void Play(string id);
    void PlayAt(string id, Vector3 position);
}

