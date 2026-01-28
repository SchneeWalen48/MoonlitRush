using UnityEngine;

public class RacerInfo : MonoBehaviour
{
  public string racerName;
  public string displayName;
  public bool isPlayer;

  public LapCounter lapCounter;
  public int currentRank;
  public bool finished;
  public int finishOrder;

  public float finishClock = -1f; // 결승선 통과 순산의 RaceDuration
  [Header("Podium")]
  public GameObject podiumDisplayPrefab;

  void Awake()
  {
    if (!lapCounter) lapCounter = GetComponent<LapCounter>();

    if (isPlayer && PlayerPrefs.HasKey("PlayerNickname"))
    {
      var nick = PlayerPrefs.GetString("PlayerNickname").Trim();
      if (!string.IsNullOrWhiteSpace(nick)) displayName = nick;
    }
    if (string.IsNullOrEmpty(displayName)) displayName = racerName;
    if (string.IsNullOrEmpty(racerName)) racerName = displayName;
  }

  void Start()
  {
    if (RaceManager.Instance) RaceManager.Instance.RegisterRacer(this);
    //else Debug.LogWarning("RaceManager Instance not found. Racer not registered.");
  }
}