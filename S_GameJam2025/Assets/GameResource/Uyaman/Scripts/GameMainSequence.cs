using UnityEngine;
using UniRx;
using UnityEngine.InputSystem;

public class GameMainSequence : MonoBehaviour
{
    private bool m_isWaitingItemInputPress;
    private bool m_isWaitingCurveInputPress;
    private bool m_isItemInputPressed;
    private bool m_isCurveInputPressed;
    TestActions m_inputActions;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RhythmTicker.Instance.TickerObservable.Subscribe(delta => OnRhythmTick(delta));
        m_inputActions = new TestActions();
        m_inputActions.RhythmGame.Curve.performed += CurveAction_performed;
        m_inputActions.RhythmGame.Item.performed += ItemAction_performed;
        m_inputActions.Enable();
        StartGame();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnRhythmTick(float deltaTime)
    {
        // �J�[�u�Ɏ��s����
        if (m_isWaitingCurveInputPress)
        {
            Debug.Log("�J�[�u�Ɏ��s����");
        }
        // �A�C�e���擾�Ɏ��s����
        if (m_isWaitingItemInputPress)
        {
            Debug.Log("�A�C�e���擾�Ɏ��s����");
        }
        m_isCurveInputPressed = false;
        m_isItemInputPressed = false;
        m_isWaitingCurveInputPress = false;
        m_isWaitingItemInputPress = false;

        switch (StageDataManager.Instance.GetCurrentNode.NodeType)
        {
            case StageNodeType.None:
                break;
            case StageNodeType.Left:
                StartWaitCurveInputPress();
                break;
            case StageNodeType.Right:
                StartWaitCurveInputPress();
                break;
            case StageNodeType.Item:
                StartWaitItemInputPress();
                break;
            default:
                break;
        }
    }

    private void ItemAction_performed(InputAction.CallbackContext obj)
    {
        // ���̃t���[���ł��łɃA�C�e���{�^����������Ă���
        if (m_isItemInputPressed) return;


        // �A�C�e���擾����
        if (m_isWaitingItemInputPress)
        {
            // TODO �A�C�e���Q�b�g
            Debug.Log("�A�C�e���Q�b�g");
        }
        // �A�C�e�����擾�ł��Ȃ��̂ɃA�C�e�����Q�b�g����
        else
        {
            //TODO ���ז��A�C�e���Q�b�g
            Debug.Log("���ז��A�C�e���Q�b�g");
        }

        m_isItemInputPressed = true;
        m_isWaitingItemInputPress = false;
    }

    private void CurveAction_performed(InputAction.CallbackContext obj)
    {
        // ���̃t���[���ł��łɃJ�[�u�{�^����������Ă���
        if (m_isCurveInputPressed) return;

        // �J�[�u����
        if (m_isWaitingCurveInputPress)
        {
            // TODO �h���t�g�G�t�F�N�g�Đ�
            Debug.Log("�h���t�g�G�t�F�N�g�Đ�");
        }
        // �J�[�u�̕K�v���Ȃ��̂ɃJ�[�u��������
        else
        {
            // TODO �Ō�ɃQ�b�g�����A�C�e��������Ƃ�
            Debug.Log("�Ō�ɃQ�b�g�����A�C�e��������Ƃ�");
        }

        m_isCurveInputPressed = true;
        m_isWaitingCurveInputPress = false;
    }

    void StartWaitCurveInputPress()
    {
        m_isWaitingCurveInputPress = true;
        Debug.Log("�J�[�u��");
    }

    void StartWaitItemInputPress()
    {
        m_isWaitingItemInputPress = true;
        Debug.Log("�A�C�e���擾�\");
    }

    public void StartGame()
    {
        RhythmTicker.Instance.SetTimerConfig(1.0f);
        RhythmTicker.Instance.StartTimer();
    }
}
