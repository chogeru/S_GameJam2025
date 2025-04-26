using System.Collections.Generic;
using UnityEngine;

public struct UdonPart
{
    GameObject PartData;
}

public class UdonInfoManager : MonoBehaviour
{
    static private UdonInfoManager m_instance;

    public Stack<UdonPart> UdonParts = new Stack<UdonPart>();

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

    // Update is called once per frame
    void Update()
    {
        
    }
}
