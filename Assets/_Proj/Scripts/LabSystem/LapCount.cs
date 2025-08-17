
using TMPro;
using UnityEngine;

public class LapCounter : MonoBehaviour
{
    public TextMeshProUGUI timeText;
    private float lapStartTime;

    public int currentLap = 0;
   // public int nextCheckpointIndex = 0;
    private bool raceFinished = false;
    public Checkpoint nextCheckpoint;  // 다음 체크포인트의 오브젝트를 직접 참조

    public ChekcpointManager chekcpointManager;

    private void Start()
    {
        if(chekcpointManager == null)
        {
            chekcpointManager = FindObjectOfType<ChekcpointManager>();
        }

        lapStartTime = Time.time;
    }

    private void Update()
    {
        if (timeText != null && !raceFinished)
        {
            float currentLapTime = Time.time - lapStartTime;
            int minutes = Mathf.FloorToInt(currentLapTime / 60);  //분
            float seconds = currentLapTime % 60; //초
            timeText.text = $"Lap Time: {minutes:00}:{seconds:00.00}";
        }
    }

    //public void PassCheckpoint(int checkpointIndex)
    //{
    //    if (raceFinished) return;

    //    if (checkpointIndex == nextCheckpointIndex)
    //    {
    //        nextCheckpointIndex++;

    //        if (nextCheckpointIndex >= RaceManager.Instance.checkpoints.Count)
    //        {
    //            nextCheckpointIndex = 0;
    //            currentLap = 1;                
    //            raceFinished = true;              

    //            Debug.Log(name + " Finished Race!");                
    //        }
    //    }
    //}


    public void PassCheckpoint(Checkpoint passedCheckpoint)
    {
        if (raceFinished) return;

        // 통과한 체크포인트가 다음 체크포인트와 일치하는지 확인
        if (passedCheckpoint == nextCheckpoint)
        {
            // 다음 체크포인트를 설정
            nextCheckpoint = passedCheckpoint.nextCheckpoint;

            // 마지막 체크포인트를 통과했는지 확인
            if (passedCheckpoint.isFinalCheckpoint)
            {
                TimeManager.Instance.StopTimer(); 
                //최종 기록 전달
                TimeManager.Instance.RecordFinishTime(
                    gameObject.name,
                    TimeManager.Instance.RaceDuration);

                currentLap++;
                // 랩이 특정 횟수를 채웠는지 확인
                if (currentLap >= RaceManager.Instance.totalLaps)
                {
                    raceFinished = true;
                    Debug.Log(name + " Finished Race!");

                    //체크포인트 다 돌면 활성화
                    RaceManager.Instance.ActivateEndTrigger();
                }
            }
        }
    }
}
