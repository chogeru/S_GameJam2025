using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

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
        }
        Debug.Log(GetCurrentNode.NodeType);
    }
}
