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

    if (!ri.finished)
    {
      ri.finished = true;
      if(RaceDataStore.Instance != null)
        ri.finishOrder = ++RaceManager.Instance.finishCounter;
    }

    if (TimeManager.Instance != null && !recorded.Contains(ri))
    {
      TimeManager.Instance.RecordFinishTime(ri, TimeManager.Instance.RaceDuration);
      recorded.Add(ri);
    }
    if (!finalStarted)
    {
      finalStarted = true;
      final?.StartCountdown(final.defaultSeconds, ri);
    }
  }
}