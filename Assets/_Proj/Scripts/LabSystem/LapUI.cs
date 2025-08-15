
using UnityEngine;
using TMPro;

public class LapUI : MonoBehaviour
{
    public TextMeshProUGUI lapText;
    public RacerInfo playerRacer;

    void Update()
    {
        if (playerRacer == null) return;

        lapText.text = $"{playerRacer.lapCounter.currentLap} / {RaceManager.Instance.totalLaps} Lap";
    }
}
