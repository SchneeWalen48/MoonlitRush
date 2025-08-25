using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
  // Singleton Instance : Accessible from anywhere
  public static TimeManager Instance;

  //[Header("Podium")]
  //public GameObject winnerPodiumPrefab;
  private float playerFinishTime = 0f;
  public class PlayerTimeData
  {
    public string playerName;
    public float finishTime;
    public bool finished = true;
    public bool isPlayer;
  }

  public readonly List<PlayerTimeData> data = new List<PlayerTimeData>();
  public List<PlayerTimeData> Results => GetRanking();


  private float raceStartTime; // (Time.time)
  private float raceEndTime;
  private bool isTiming;
  private float pausedTime;
  private float totalPausedDuration; // Total time paused so far
  private bool isPaused;

  // Time.time keeps passing, but the paused time needs to be subtracted from the record. if paused.
  public float RaceDuration => isTiming ? Time.time - raceStartTime - totalPausedDuration : raceEndTime - raceStartTime - totalPausedDuration;


  void Awake()
  {
    // Prevent duplication in other scenes
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }

    Instance = this;
  }

  void Start()
  {
    // 에디터에서 빈 씬에서 테스트 할때 Play 누르자마자 실행되게 만들었음. 씬 여러개 연결되면 시작 타임 정해주고(카운트다운 이후 등) 삭제.
#if UNITY_EDITOR
    //StartTimer();
#endif
  }

  // Start Timer: Record Time + Write
  public void StartTimer()
  {
    raceStartTime = Time.time;
    isTiming = true;
    isPaused = false;
    totalPausedDuration = 0;
  }

  // Stop Timer : Save current time
  public void StopTimer()
  {
    if (!isTiming) return;
    if (isPaused) ResumeTimer();
    raceEndTime = Time.time;
    isTiming = false;
    playerFinishTime = RaceDuration;
  }

  // Pause: Saves the current time and sets it to stopped state
  public void PauseTimer()
  {
    if (!isTiming || isPaused) return;
    pausedTime = Time.time;
    isPaused = true;
  }

  // Unpause: Accumulates as much as the pause time
  public void ResumeTimer()
  {
    if (!isPaused) return;
    totalPausedDuration += Time.time - pausedTime;
    isPaused = false;
  }

  // Returns elapsed time as a string (minutes:seconds.milliseconds)
  public static string FormatTime(float t)
  {
    if (t < 0 || float.IsNaN(t) || float.IsInfinity(t)) return "Time Over";
    int m = Mathf.FloorToInt(t / 60f);
    float s = t % 60f;
    return $"{m:00}:{s:00.000}";
  }

  public string GetFormatRaceTime()
  {
    if (isTiming) return FormatTime(RaceDuration);
    else return FormatTime(playerFinishTime);

  }

  // Reset Timer
  public void ResetTimer()
  {
    raceStartTime = 0f;
    raceEndTime = 0f;
    isTiming = false;
    isPaused = false;
    pausedTime = 0f;
    totalPausedDuration = 0f;
    //winnerPodiumPrefab = null;
  }

  public void RecordFinishTime(string name, float fTime)
  {

    string safe = string.IsNullOrWhiteSpace(name)
        ? PlayerPrefs.GetString("PlayerNickname", "Player")
        : name;

    var exist = data.FirstOrDefault(d => d.playerName == safe);
    if (exist != null) { exist.finishTime = Mathf.Min(exist.finishTime, fTime); return; }

    data.Add(new PlayerTimeData { playerName = safe, finishTime = fTime, finished = true });
  }

  public void RecordFinishTime(RacerInfo ri, float fTime)
  {
    if (ri == null) return;

    string safeName = SafeNameOf(ri);

    if (data.Any(x => x.playerName == safeName)) return;

    data.Add(new PlayerTimeData
    {
      playerName = safeName,
      finishTime = fTime,
      isPlayer = ri.isPlayer,
      finished = (fTime >= 0f)
    });
  }

  public static string SafeNameOf(RacerInfo ri)
  {
    return !string.IsNullOrWhiteSpace(ri.displayName) ? ri.displayName :
           !string.IsNullOrWhiteSpace(ri.racerName) ? ri.racerName :
           PlayerPrefs.GetString("PlayerNickname", "Player");
  }
  public void EnsureDNFsFrom(List<RacerInfo> racers)
  {
    if (racers == null) return;

    foreach (var r in racers)
    {
      if (!r) continue;

      string safeName =
          !string.IsNullOrWhiteSpace(r.displayName) ? r.displayName :
          !string.IsNullOrWhiteSpace(r.racerName) ? r.racerName :
          PlayerPrefs.GetString("PlayerNickname", "Player");

      // 이미 기록된(완주한) 이름은 스킵
      if (data.Any(d => d.playerName == safeName)) continue;

      // DNF 추가
      data.Add(new PlayerTimeData
      {
        playerName = safeName,
        finishTime = -1f,
        finished = false,
        isPlayer = r.isPlayer
      });
    }
  }

  // 편의: UI에서 "--" 찍기 쉽게
  public static string FormatOrDash(PlayerTimeData p)
  {
    return (p != null && p.finished && p.finishTime >= 0f)
        ? FormatTime(p.finishTime)
        : "Time Over";
  }
  //public void TrySetWinnerPrefab(RacerInfo ri)
  //{
  //    if (!winnerPodiumPrefab && ri && ri.podiumDisplayPrefab)
  //        winnerPodiumPrefab = ri.podiumDisplayPrefab;
  //}

  public List<PlayerTimeData> GetRanking()
  {
    return new List<PlayerTimeData>(data);
    //return data.OrderBy(p => p.finishTime).ToList(); // Sort racing records by fastest time
  }

}
