using UnityEngine;

// Check Point에 스크립트 적용
public class Checkpoint : MonoBehaviour
{
  public int checkpointId;
  public bool isFinalCheckpoint = false;
  public Checkpoint nextCheckpoint;

  void Reset()
  {
    var col = GetComponent<Collider>();
    if (col) col.isTrigger = true;
  }

  void OnTriggerEnter(Collider other)
  {
    var lap = other.GetComponentInParent<LapCounter>() ?? other.GetComponent<LapCounter>();
    if (lap == null) return;

    if (lap.nextCheckpoint != this) return;

    lap.PassCheckpoint(this);
  }
  public void SetNextCheckpoint(Checkpoint next)
  {
    nextCheckpoint = next;
  }
}