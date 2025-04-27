using System;
using UnityEngine;
using UniRx;
using UnityEngine.Events;
using System.Collections;

public class RhythmTicker : MonoBehaviour
{
    private static RhythmTicker m_instance;
    public static RhythmTicker Instance => m_instance;

    public float TickIntervalSeconds = 0.25f;

    Subject<float> m_tickerSubject = new Subject<float>();
    public IObservable<float> TickerObservable => m_tickerSubject;
    [SerializeField]
    UnityEvent m_tickEvents = new UnityEvent();

    // MonoBehaviourの関数はメインスレッドでしか処理されないので、TimerでTickCountを増やしておいてUpdateでTickCount分の処理を行う
    int m_tickCountWhileThisFrame = 0;

    private void Awake()
    {
        if (m_instance == null)
            m_instance = this;
        else
            Destroy(this);
    }

    private void Start()
    {

    }

    public void SetTimerConfig(float intervalSeconds)
    {
        TickIntervalSeconds = intervalSeconds;
    }

    public void StopTimer()
    {
    }

    public void StartTimer()
    {
        StartCoroutine(TimerCoroutine(TickIntervalSeconds));
    }

    private void Update()
    {
        for (int i = 0; i < m_tickCountWhileThisFrame; i++)
        {
            m_tickerSubject.OnNext(TickIntervalSeconds);
            m_tickEvents.Invoke();
            Debug.Log("RhythmTick");
        }
        m_tickCountWhileThisFrame = 0;
    }

    private void OnDestroy()
    {

    }

    private IEnumerator TimerCoroutine(float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            m_tickCountWhileThisFrame++;
        }
    }
}
