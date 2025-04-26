using FunkyCode;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using VInspector.Libs;

[Serializable]
public enum StageNodeType
{
    None,
    Left,
    Right,
    Item,
    Goal
}

[Serializable]
public struct StageNode
{
    [SerializeField]
    public StageNodeType NodeType;
}

public class StageDataManager : MonoBehaviour
{
    private static StageDataManager m_instance;
    public static StageDataManager Instance => m_instance;

    [SerializeField]
    private List<GameObject> m_straightImages;
    private int m_straightImageIndex;
    [SerializeField]
    private GameObject m_curveLeft;
    [SerializeField]
    private GameObject m_curveRight;

    [SerializeField]
    private List<RectTransform> m_leftBGTransformsStraight;
    [SerializeField]
    private List<RectTransform> m_rightBGTransformsStraight;

    [SerializeField]
    private List<RectTransform> m_leftBGTransformsLeftCurce;
    [SerializeField]
    private List<RectTransform> m_rightBGTransformsLeftCurve;

    [SerializeField]
    private List<RectTransform> m_leftBGTransformsRightCurve;
    [SerializeField]
    private List<RectTransform> m_rightBGTransformsRightCurve;

    private List<RectTransform> m_currentLeftBGTransforms;
    private List<RectTransform> m_currentrightBGTransforms;

    [SerializeField]
    private List<Image> m_BGImages;
    private List<Image> m_currentRightBGs;
    private List<Image> m_currentLeftBGs;

#if UNITY_EDITOR
    [SerializeField]
    private List<StageNode> m_debugNodes;
#endif

    private List<StageNode> m_nodes = new List<StageNode>();
    private int m_currentNodePosition;
    public StageNode GetCurrentNode
    {
        get
        {
            if (m_currentNodePosition < m_nodes.Count)
                return m_nodes[m_currentNodePosition];
            else
                return new StageNode() { NodeType = StageNodeType.Goal };
        }
    }
    public List<StageNode> GetStageNodes => m_nodes;

    private void Awake()
    {
        if (m_instance == null)
            m_instance = this;
        else
            Destroy(this);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
#if UNITY_EDITOR
        m_nodes = new List<StageNode>(m_debugNodes);
#endif
        RhythmTicker.Instance.TickerObservable.Subscribe(time => OnRhythmTick(time));

        m_currentRightBGs = new List<Image>();
        m_currentLeftBGs = new List<Image>();

        for (int i = 0; i < 3; i++)
        {
            var obj = m_BGImages[UnityEngine.Random.Range(0, m_BGImages.Count)];
            m_BGImages.Remove(obj);
            m_currentLeftBGs.Add(obj);
            obj = m_BGImages[UnityEngine.Random.Range(0, m_BGImages.Count)];
            m_BGImages.Remove(obj);
            m_currentRightBGs.Add(obj);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnRhythmTick(float DeltaTime)
    {
        if (m_currentNodePosition < m_nodes.Count && GetCurrentNode.NodeType != StageNodeType.Goal)
        {
            m_currentNodePosition++;

            for (int i = 0; i < m_straightImages.Count; i++)
            {
                m_straightImages[i].SetActive(false);
            }
            m_curveLeft.SetActive(false);
            m_curveRight.SetActive(false);

            switch (GetCurrentNode.NodeType)
            {
                case StageNodeType.None:
                    GoStraight();
                    break;
                case StageNodeType.Left:
                    m_curveLeft.SetActive(true);
                    m_currentLeftBGTransforms = m_leftBGTransformsLeftCurce;
                    m_currentrightBGTransforms = m_rightBGTransformsLeftCurve;
                    break;
                case StageNodeType.Right:
                    m_curveRight.SetActive(true);
                    m_currentLeftBGTransforms = m_leftBGTransformsRightCurve;
                    m_currentrightBGTransforms = m_rightBGTransformsRightCurve;
                    break;
                case StageNodeType.Item:
                    GoStraight();
                    break;
                case StageNodeType.Goal:
                    break;
                default:
                    break;
            }
            MoveBG();
        }
        Debug.Log(GetCurrentNode.NodeType);
    }

    void GoStraight()
    {
        m_straightImageIndex = (m_straightImageIndex + 1) % m_straightImages.Count;
        m_straightImages[m_straightImageIndex].SetActive(true);
        m_currentLeftBGTransforms = m_leftBGTransformsStraight;
        m_currentrightBGTransforms = m_rightBGTransformsStraight;
    }

    void MoveBG()
    {
        m_BGImages.Add(m_currentLeftBGs[0]);
        m_currentLeftBGs[0].gameObject.SetActive(false);
        m_currentLeftBGs.RemoveAt(0);
        m_BGImages.Add(m_currentRightBGs[0]);
        m_currentRightBGs[0].gameObject.SetActive(false);
        m_currentRightBGs.RemoveAt(0);

        var obj = m_BGImages[UnityEngine.Random.Range(0, m_BGImages.Count)];
        obj.gameObject.SetActive(true);
        m_BGImages.Remove(obj);
        m_currentLeftBGs.Add(obj);
        obj = m_BGImages[UnityEngine.Random.Range(0, m_BGImages.Count)];
        obj.gameObject.SetActive(true);
        m_BGImages.Remove(obj);
        m_currentRightBGs.Add(obj);

        for (int i = 0; i < 3; i++)
        {
            m_currentLeftBGs[i].rectTransform.position = m_currentLeftBGTransforms[i].position;
            m_currentLeftBGs[i].rectTransform.localScale = m_currentLeftBGTransforms[i].localScale;
            m_currentRightBGs[i].rectTransform.position = m_currentrightBGTransforms[i].position;
            m_currentRightBGs[i].rectTransform.localScale = m_currentrightBGTransforms[i].localScale;
        }
    }
}

