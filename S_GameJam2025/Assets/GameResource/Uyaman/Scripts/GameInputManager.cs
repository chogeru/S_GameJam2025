using UnityEngine;
using UnityEngine.InputSystem;

public class GameInputManager : MonoBehaviour
{
    TestActions m_inputActions;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_inputActions = new TestActions();
        m_inputActions.RhythmGame.Curve.performed += Curve_performed;
        m_inputActions.RhythmGame.Item.performed += Item_performed;
        m_inputActions.Enable();
    }

    private void Item_performed(InputAction.CallbackContext obj)
    {
    }

    private void Curve_performed(InputAction.CallbackContext obj)
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
