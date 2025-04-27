using UnityEngine;
using UniRx;
using UnityEngine.InputSystem;
using System.Collections;

public class GameMainSequence : MonoBehaviour
{
    private bool m_isWaitingItemInputPress;
    private bool m_isWaitingCurveInputPress;
    private bool m_isItemInputPressed;
    private bool m_isCurveInputPressed;
    TestActions m_inputActions;

    [SerializeField]
    GameObject m_ExcellentObj;

    [SerializeField]
    private float m_tickInterval;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RhythmTicker.Instance.TickerObservable.Subscribe(delta => OnRhythmTick(delta));
        m_inputActions = new TestActions();
        m_inputActions.RhythmGame.Curve.performed += CurveAction_performed;
        m_inputActions.RhythmGame.Item.performed += ItemAction_performed;
        m_inputActions.Enable();
        StartCoroutine(StartGameTimer(3));
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnRhythmTick(float deltaTime)
    {
        UdonInfoManager.Instance.CurveFailed = false;

        // �J�[�u�Ɏ��s����
        if (m_isWaitingCurveInputPress)
        {
            Debug.Log("�J�[�u�Ɏ��s����");
            UdonInfoManager.Instance.CurveFailed = true;
            UdonInfoManager.Instance.RemoveParts();
            UdonInfoManager.Instance.RemoveOjama();
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
            default:
                break;
        }
        if (StageDataManager.Instance.GetCurrentNode.IsItem)
        {
            StartWaitItemInputPress();
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
            UdonInfoManager.Instance.AddParts();
        }
        // �A�C�e�����擾�ł��Ȃ��̂ɃA�C�e�����Q�b�g����
        else
        {
            //TODO ���ז��A�C�e���Q�b�g
            Debug.Log("���ז��A�C�e���Q�b�g");
            UdonInfoManager.Instance.AddOjama();
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
            StartCoroutine(StartExcellentCorutine());
        }
        // �J�[�u�̕K�v���Ȃ��̂ɃJ�[�u��������
        else
        {
            // TODO �Ō�ɃQ�b�g�����A�C�e��������Ƃ�
            Debug.Log("�Ō�ɃQ�b�g�����A�C�e��������Ƃ�");
            UdonInfoManager.Instance.CurveFailed = true;
            UdonInfoManager.Instance.RemoveParts();
            UdonInfoManager.Instance.RemoveOjama();
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
        RhythmTicker.Instance.SetTimerConfig(m_tickInterval);
        RhythmTicker.Instance.StartTimer();
    }

    IEnumerator StartExcellentCorutine()
    {
        m_ExcellentObj.SetActive(true);
        yield return new WaitForSeconds(m_tickInterval * 2);
        m_ExcellentObj.SetActive(false);
    }

    IEnumerator StartGameTimer(int seconds)
    {
        for (int i = 0; i < seconds; i++)
        {
            yield return new WaitForSeconds(1f);
        }
        StartGame();
    }
}
