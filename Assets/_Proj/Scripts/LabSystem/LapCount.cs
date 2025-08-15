
using UnityEngine;

public class LapCounter : MonoBehaviour
{
    public int currentLap = 0;
    public int nextCheckpointIndex = 0;
    private bool raceFinished = false;

    public void PassCheckpoint(int checkpointIndex)
    {
        if (raceFinished) return;

        if (checkpointIndex == nextCheckpointIndex)
        {
            nextCheckpointIndex++;

            if (nextCheckpointIndex >= RaceManager.Instance.checkpoints.Count)
            {
                nextCheckpointIndex = 0;
                currentLap = 1;                
                raceFinished = true;              

                Debug.Log(name + " Finished Race!");                
            }
        }
    }
}
