using UnityEngine;
using AbubuResource.Scene;
using UnityEngine.InputSystem;

public class OnClickMuseButton : MonoBehaviour
{
    private void Update()
    {
        if ((Mouse.current.rightButton.wasPressedThisFrame ||
              Mouse.current.middleButton.wasPressedThisFrame)
             && !SceneDirector.Instance.IsTransitioning)
        {
            SceneDirector.Instance.NextScene();
        }
    }
}
