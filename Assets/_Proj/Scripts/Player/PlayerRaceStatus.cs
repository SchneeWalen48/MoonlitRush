using UnityEngine;

public class PlayerRaceStatus : MonoBehaviour
{
  public int currLap = 0; // 현재 랩 횟수
  public int lastCheckpointId = -1; // 마지막으로 통과한 체크포인트 번호
  public float distToNextCP = float.MaxValue; // 다음 체크포인트까지 거리
  public Transform nextCPTrans; // 다음 체크포인트 위치

  private CheckpointManager cpManager;

  void Start()
  {
    cpManager = FindObjectOfType<CheckpointManager>();
  }
  void Update()
  {
    if(nextCPTrans != null)
    {
      distToNextCP = Vector3.Distance(transform.position, nextCPTrans.position);
    }
  }

  void OnTriggerEnter(Collider other)
  {
    Checkpoint cp = other.GetComponent<Checkpoint>();
    if (cp != null)
    {
      UpdateCheckPoint(cp);  
    }
  }

  void UpdateCheckPoint(Checkpoint cp)
  {
    if(cp.checkpointId != lastCheckpointId)
    {
      lastCheckpointId = cp.checkpointId;

      if (cp.isFinalCheckpoint)
      {
        currLap++;
      }
      nextCPTrans = cp.nextCheckpoint.transform;
    }
  }

}
