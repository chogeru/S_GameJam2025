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
        // カーブに失敗した
        if (m_isWaitingCurveInputPress)
        {
            Debug.Log("カーブに失敗した");
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
            case StageNodeType.Item:
                StartWaitItemInputPress();
                break;
            default:
                break;
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
        }
        // アイテムが取得できないのにアイテムをゲットした
        else
        {
            //TODO お邪魔アイテムゲット
            Debug.Log("お邪魔アイテムゲット");
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
        }
        // カーブの必要がないのにカーブを押した
        else
        {
            // TODO 最後にゲットしたアイテムを一つ落とす
            Debug.Log("最後にゲットしたアイテムを一つ落とす");
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
        RhythmTicker.Instance.SetTimerConfig(1.0f);
        RhythmTicker.Instance.StartTimer();
    }
}
