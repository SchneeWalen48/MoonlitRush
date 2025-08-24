using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    // Singleton Instance : Accessible from anywhere
    public static TimeManager Instance;

    [Header("Podium")]
    public GameObject winnerPodiumPrefab;

    public class PlayerTimeData
    {
        public string playerName;
        public float finishTime;
        public bool finished = true;
    }

    public List<PlayerTimeData> data = new List<PlayerTimeData>();
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
        DontDestroyOnLoad(gameObject); // Maintained even where other scenes (Timer Data)
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
        int m = Mathf.FloorToInt(t / 60f);
        float s = t % 60f;
        return $"{m:00}:{s:00.000}";
    }

    public string GetFormatRaceTime() => FormatTime(RaceDuration);

    // Reset Timer
    public void ResetTimer()
    {
        raceStartTime = 0f;
        raceEndTime = 0f;
        isTiming = false;
        isPaused = false;
        pausedTime = 0f;
        totalPausedDuration = 0f;
        winnerPodiumPrefab = null;
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
        string safe = (ri && !string.IsNullOrWhiteSpace(ri.displayName))
            ? ri.displayName
            : PlayerPrefs.GetString("PlayerNickname", "Player");
        RecordFinishTime(safe, fTime);
    }

    public void TrySetWinnerPrefab(RacerInfo ri)
    {
        if (!winnerPodiumPrefab && ri && ri.podiumDisplayPrefab)
            winnerPodiumPrefab = ri.podiumDisplayPrefab;
    }

    public List<PlayerTimeData> GetRanking()
    {
        return data.OrderBy(p => p.finishTime).ToList(); // Sort racing records by fastest time
    }

}
