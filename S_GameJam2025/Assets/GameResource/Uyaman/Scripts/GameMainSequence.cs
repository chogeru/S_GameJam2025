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

        // カーブに失敗した
        if (m_isWaitingCurveInputPress)
        {
            Debug.Log("カーブに失敗した");
            UdonInfoManager.Instance.CurveFailed = true;
            UdonInfoManager.Instance.RemoveParts();
            UdonInfoManager.Instance.RemoveOjama();
        }
        // アイテム取得に失敗した
        if (m_isWaitingItemInputPress)
        {
            Debug.Log("アイテム取得に失敗した");
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
        // このフレームですでにアイテムボタンが押されている
        if (m_isItemInputPressed) return;


        // アイテム取得成功
        if (m_isWaitingItemInputPress)
        {
            // TODO アイテムゲット
            Debug.Log("アイテムゲット");
            UdonInfoManager.Instance.AddParts();
        }
        // アイテムが取得できないのにアイテムをゲットした
        else
        {
            //TODO お邪魔アイテムゲット
            Debug.Log("お邪魔アイテムゲット");
            UdonInfoManager.Instance.AddOjama();
        }

        m_isItemInputPressed = true;
        m_isWaitingItemInputPress = false;
    }

    private void CurveAction_performed(InputAction.CallbackContext obj)
    {
        // このフレームですでにカーブボタンが押されている
        if (m_isCurveInputPressed) return;

        // カーブ成功
        if (m_isWaitingCurveInputPress)
        {
            // TODO ドリフトエフェクト再生
            Debug.Log("ドリフトエフェクト再生");
            StartCoroutine(StartExcellentCorutine());
        }
        // カーブの必要がないのにカーブを押した
        else
        {
            // TODO 最後にゲットしたアイテムを一つ落とす
            Debug.Log("最後にゲットしたアイテムを一つ落とす");
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
        Debug.Log("カーブ中");
    }

    void StartWaitItemInputPress()
    {
        m_isWaitingItemInputPress = true;
        Debug.Log("アイテム取得可能");
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
