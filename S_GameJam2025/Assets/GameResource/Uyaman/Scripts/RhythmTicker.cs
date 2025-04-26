using System;
using System.Timers;
using UnityEngine;
using UniRx;
using UnityEngine.Events;

public class RhythmTicker : MonoBehaviour
{
    private static RhythmTicker m_instance;
    public static RhythmTicker Instance => m_instance;

    public float TickIntervalSeconds = 0.25f;

    Subject<float> m_tickerSubject = new Subject<float>();
    public IObservable<float> TickerObservable => m_tickerSubject;
    [SerializeField]
    UnityEvent m_tickEvents = new UnityEvent();

    // MonoBehaviour�̊֐��̓��C���X���b�h�ł�����������Ȃ��̂ŁATimer��TickCount�𑝂₵�Ă�����Update��TickCount���̏������s��
    int m_tickCountWhileThisFrame = 0;
    private Timer m_timer;

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
        if (m_timer != null)
        {
            m_timer.Dispose();
        }
        m_timer = new Timer(intervalSeconds * 1000);
        // Hook up the Elapsed event for the timer. 
        m_timer.Elapsed += (sender, e) =>
        {
            m_tickCountWhileThisFrame++;
        };
        m_timer.AutoReset = true;
        m_timer.Enabled = true;
    }

    public void StopTimer()
    {
        m_timer.Stop();
    }

    public void StartTimer()
    {
        // Elapsed�C�x���g�Ƀ^�C�}�[�������̏�����ݒ肷��
        m_timer.Start();
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
        m_timer?.Stop();
        m_timer = null;
    }
}
