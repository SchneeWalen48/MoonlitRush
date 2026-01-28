using TMPro;
using UnityEngine;

public class LapCounter : MonoBehaviour
{
  public TextMeshProUGUI lapText;
  public TextMeshProUGUI timeText;

  public int currentLap = 0;
  bool raceFinished = false;

  public Checkpoint nextCheckpoint;
  public CheckpointManager checkpointManager;

  void Start()
  {
    if (!checkpointManager) checkpointManager = FindObjectOfType<CheckpointManager>();
    if (checkpointManager && checkpointManager.allCheckPoints.Count > 0)
      nextCheckpoint = checkpointManager.allCheckPoints[0];

    RefreshLapUI();
  }

  void Update()
  {
    if (raceFinished) return;
    if (timeText && TimeManager.Instance != null)
      timeText.text = TimeManager.Instance.GetFormatRaceTime();
  }

  public void PassCheckpoint(Checkpoint passed)
  {
    if (raceFinished) return;
    if (passed != nextCheckpoint) return;

    nextCheckpoint = passed.nextCheckpoint;

    if (passed.isFinalCheckpoint)
    {
      int total = RaceManager.Instance ? RaceManager.Instance.totalLaps : 1;
      if (currentLap < total) currentLap++;

      if (currentLap >= total)
      {
        var tm = TimeManager.Instance;
        if (tm != null)
        {
          var ri = GetComponent<RacerInfo>() ?? GetComponentInChildren<RacerInfo>();

          if (ri != null)
          {
            float captured = tm.RaceDuration;// 결승선 통과한 '지금' 시간
            tm.RecordFinishTime(ri, captured); // 엔딩 기록 저장
            if (timeText) timeText.text = TimeManager.FormatTime(captured);
          }
        }
        raceFinished = true;
        //TimeManager.Instance?.StopTimer(); // 다른 플레이어 기록 해야하기 때문에 호출 X
        RaceManager.Instance?.ActivateEndTrigger();
      }
      nextCheckpoint = checkpointManager.allCheckPoints[0];
    }
    RefreshLapUI();
  }

  public void RefreshLapUI()
  {
    if (!lapText) return;
    int total = RaceManager.Instance ? RaceManager.Instance.totalLaps : 1;
    int displayLap = Mathf.Min(currentLap + 1, total);
    lapText.text = $"{displayLap} / {total}";
    lapText.gameObject.SetActive(true);
  }
}