using UnityEngine;
using AbubuResource.Scene;
using UnityEngine.InputSystem;

public class OnClickMuseButton : MonoBehaviour
{
    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!SceneDirector.Instance.IsTransitioning)
                SceneDirector.Instance.NextScene();
        }
    }
}
