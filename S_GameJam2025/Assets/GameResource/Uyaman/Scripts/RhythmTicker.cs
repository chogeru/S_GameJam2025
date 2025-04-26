using System;
using System.Timers;
using UnityEngine;
using UniRx;
using UnityEngine.Events;

public class RhythmTicker : MonoBehaviour
{
    private static RhythmTicker m_instance;
    public static RhythmTicker Instance { get { return m_instance; } }

    public float TickIntervalSeconds = 0.25f;
    Subject<float> tickerSubject = new Subject<float>();
    [SerializeField]
    UnityEvent TickEvents = new UnityEvent();
    public IObservable<float> TickerObservable => tickerSubject;

    private Timer m_timer;
    private void Start()
    {
        // Create a timer with a two second interval.
        m_timer = new Timer(TickIntervalSeconds * 1000);
        // Hook up the Elapsed event for the timer. 
        m_timer.Elapsed += (sender, e) =>
        {
            // 何らかの処理
            tickerSubject.OnNext(TickIntervalSeconds);
            TickEvents.Invoke();
            Debug.Log("Timer");
        };
        m_timer.AutoReset = true;
        m_timer.Enabled = true;
        // Elapsedイベントにタイマー発生時の処理を設定する
        m_timer.Start();
    }

    private void Update()
    {

    }

    private void OnDestroy()
    {
        m_timer?.Stop();
        m_timer = null;
    }
}
