using System.Collections.Generic;
using UnityEngine;

public class EndTrigger : MonoBehaviour
{
    BoxCollider col;
    FinalCount final;
    bool finalStarted = false;

  readonly HashSet<RacerInfo> recorded = new HashSet<RacerInfo>();
    void Awake() => col = GetComponent<BoxCollider>();

    void Start()
    {
        final = FindObjectOfType<FinalCount>();
        if (col) col.enabled = false;
    }

    public void ActiveTrigger()
    {
        if (!col) return;
        col.enabled = true;
        col.isTrigger = true;
    }

  void OnTriggerEnter(Collider other)
  {
    var ri = other.GetComponentInParent<RacerInfo>() ?? other.GetComponent<RacerInfo>();
    if (!ri || !ri.lapCounter) return;

    int total = RaceManager.Instance ? RaceManager.Instance.totalLaps : 2;
    if (ri.lapCounter.currentLap < total) return;

    // 완주 순서
    if (!ri.finished)
    {
      ri.finished = true;
      //if(RaceDataStore.Instance != null)
        ri.finishOrder = ++RaceManager.Instance.finishCounter;
    }

    // 그 순간의 정확한 시간 캡처
    if (ri.finishClock < 0 && TimeManager.Instance != null)
      ri.finishClock = TimeManager.Instance.RaceDuration;

    // 기록(중복 방지) - AI/Player 모두
    if (TimeManager.Instance != null && !recorded.Contains(ri))
    {
      TimeManager.Instance.RecordFinishTime(ri, ri.finishClock); //위에서 캡처된 값으로
      recorded.Add(ri);
    }
    if (!finalStarted)
    {
      finalStarted = true;
      final?.StartCountdown(final.defaultSeconds, ri);
    }
  }
}