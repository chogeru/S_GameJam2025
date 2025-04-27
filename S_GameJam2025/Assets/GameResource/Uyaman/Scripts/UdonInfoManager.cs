using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class UdonInfoManager : MonoBehaviour
{
    static private UdonInfoManager m_instance;
    static public UdonInfoManager Instance => m_instance;

    public int PartsCount;
    public int OjamaCount;

    [SerializeField]
    List<GameObject> PartsItemObjs;
    [SerializeField]
    List<GameObject> OjamaItemObjs;
    [SerializeField]
    GameObject m_normalCar;
    [SerializeField]
    GameObject m_failedCar;
    [SerializeField]
    GameObject m_leftCar;
    [SerializeField]
    GameObject m_rightCar;

    public bool CurveFailed;
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

    }

    public void AddParts()
    {
        PartsCount = Mathf.Clamp(PartsCount + 1, 0, PartsItemObjs.Count - 1);
    }

    public void RemoveParts()
    {
        PartsCount = Mathf.Clamp(PartsCount - 1, 0, PartsItemObjs.Count - 1);
    }

    public void AddOjama()
    {
        OjamaCount = Mathf.Clamp(OjamaCount + 1, 0, OjamaItemObjs.Count - 1);
    }

    public void RemoveOjama()
    {
        OjamaCount = Mathf.Clamp(OjamaCount - 1, 0, OjamaItemObjs.Count - 1);
    }


    // Update is called once per frame
    void Update()
    {
        m_normalCar.SetActive(false);
        m_failedCar.SetActive(false);
        m_rightCar.SetActive(false);
        m_leftCar.SetActive(false);

        for (int i = 0; i < PartsItemObjs.Count; i++)
        {
            PartsItemObjs[i].SetActive(false);
        }
        for (int i = 0; i < OjamaItemObjs.Count; i++)
        {
            OjamaItemObjs[i].SetActive(false);
        }

        for (int i = 0; i < PartsCount; i++)
        {
            if (i < PartsItemObjs.Count)
                PartsItemObjs[i].SetActive(true);
        }
        for (int i = 0; i < OjamaCount; i++)
        {
            if (i < OjamaItemObjs.Count)
                OjamaItemObjs[i].SetActive(true);
        }
        if (CurveFailed)
        {
            m_failedCar.SetActive(true);
        }
        else
        {
            switch (StageDataManager.Instance.GetCurrentNode.NodeType)
            {
                case StageNodeType.None:
                    m_normalCar.SetActive(true);
                    break;
                case StageNodeType.Left:
                    m_leftCar.SetActive(true);
                    break;
                case StageNodeType.Right:
                    m_rightCar.SetActive(true);
                    break;
                case StageNodeType.Goal:
                    m_normalCar.SetActive(true);
                    break;
                default:
                    break;
            }
        }
    }
}
