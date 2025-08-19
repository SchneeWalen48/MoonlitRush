using UnityEngine;

// Check Point에 스크립트 적용
public class CheckPoint : MonoBehaviour
{
  public int checkpointId;
  public bool isFinalCheckpoint = false;
  public CheckPoint nextCheckpoint;

  public void SetNextCheckpoint(CheckPoint next)
  {
    nextCheckpoint = next;
  }
}