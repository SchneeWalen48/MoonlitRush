
using UnityEngine;

public class RacerInfo : MonoBehaviour
{
    public string racerName;
    public bool isPlayer;
    public LapCounter lapCounter;
    public int currentRank;

    private void Awake()
    {
        if(lapCounter == null)
            lapCounter = GetComponent<LapCounter>();
    }

    private void Start()
    {
        RaceManager.Instance.RegisterRacer(this);
    }
    //
}
