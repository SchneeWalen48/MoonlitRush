using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalCount : MonoBehaviour
{
    public static FinalCount Instance;

    [Header("UI")]
    public TextMeshProUGUI finalCountText;

    [Header("Timing")]
    [Min(0)] public int defaultSeconds = 10;
    [Min(0)] public float winnerSlowDuration = 1.0f;
    [Min(0)] public float othersSlowDuration = 0.8f;

    [Header("Fade to Ending")]
    public CanvasGroup endFade;
    [Min(0)] public float endFadeDuration = 0.6f;

    [Header("Scene")]
    public string endingSceneName = "Ending";

    bool isGameEnding;

    void Awake()
    {
        Instance = this;
        if (finalCountText) finalCountText.gameObject.SetActive(false);
    }

    public void StartCountdown(int seconds, RacerInfo winner = null)
    {
        if (isGameEnding) return;
        isGameEnding = true;

        //TimeManager.Instance?.PauseTimer();

        if (winner) StartCoroutine(SlowdownOne(winner, winnerSlowDuration, true));
        StartCoroutine(CoFinal(seconds > 0 ? seconds : defaultSeconds, winner));
    }

    public void Finish() => StartCountdown(defaultSeconds);

    IEnumerator CoFinal(int sec, RacerInfo winner)
    {
        if (finalCountText) finalCountText.gameObject.SetActive(true);

        for (int i = sec; i > 0; i--)
        {
            if (finalCountText) finalCountText.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        if (finalCountText) finalCountText.text = "Finish!";
        yield return new WaitForSecondsRealtime(1f);

        var everyone = FindObjectsOfType<RacerInfo>(true);
        foreach (var r in everyone)
        {
            if (!r || (winner != null && r == winner)) continue;
            StartCoroutine(SlowdownOne(r, othersSlowDuration, true));
        }
        yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, othersSlowDuration * 0.6f));

    //    TimeManager.Instance?.StopTimer();
    //if (TimeManager.Instance != null)
    //{
    //  RaceDataStore.RankingData = TimeManager.Instance.GetRanking();
    //}
    //else
    //{
    //  Debug.LogWarning("TimeManager 인스턴스를 찾을 수 없어 랭킹을 저장할 수 없습니다.");
    //}

    //if (endFade) yield return FadeTo(endFade, 1f, endFadeDuration);
    OnFinalCountdownDone();
        LoadEndingSceneSafe();
    }

    IEnumerator SlowdownOne(RacerInfo racer, float duration, bool lockControl)
    {
        if (!racer) yield break;

        var tf = racer.transform;
        tf.TryGetComponent<Rigidbody>(out var rb);
        tf.TryGetComponent<CarController>(out var car);
        tf.TryGetComponent<AICarController>(out var ai);

        float t = 0f;
        while (t < duration)
        {
            if (rb)
            {
                float k = (duration <= 0f) ? 1f : t / duration;
                rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, k);
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, k);
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        if (lockControl)
        {
            if (car) car.enabled = false;
            if (ai) { ai.moveStart = false; ai.enabled = false; }
            if (rb) rb.isKinematic = true;
        }
    }

    IEnumerator FadeTo(CanvasGroup cg, float target, float dur)
    {
        if (!cg) yield break;
        if (dur <= 0f) { cg.alpha = target; yield break; }

        float start = cg.alpha, t = 0f;
        cg.blocksRaycasts = true;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            cg.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }
        cg.alpha = target;
    }
  void OnFinalCountdownDone()
  {
    var rm = RaceManager.Instance;
    var tm = TimeManager.Instance;

    if (tm != null && rm != null)
    {
      // 1) DNF 채우기
      tm.EnsureDNFsFrom(rm.racers);

      // 2) 최종 순위 생성 (완주자 → 미완주자)
      var final = BuildFinalResults(rm.racers, tm);

      // 3) 엔딩에 넘길 데이터 확정
      RaceDataStore.RankingData = final;

      // 4) 최종적으로 타이머 정지
      tm.StopTimer();
    }
  }
  void LoadEndingSceneSafe()
    {
        var name = string.IsNullOrWhiteSpace(endingSceneName) ? "Ending" : endingSceneName;
        if (SceneManagers.Instance) SceneManagers.LoadScene(name);
        else SceneManager.LoadScene(name);
    }
  static string NameOf(RacerInfo r)
  {
    if (!r) return "Unknown";
    if (!string.IsNullOrWhiteSpace(r.displayName)) return r.displayName;
    if (!string.IsNullOrWhiteSpace(r.racerName)) return r.racerName;
    return PlayerPrefs.GetString("PlayerNickname", "Player");
  }
  static float DistToNext(RacerInfo r)
  {
    var lc = r?.lapCounter;
    if (lc?.nextCheckpoint == null) return float.MaxValue;
    return Vector3.Distance(r.transform.position, lc.nextCheckpoint.transform.position);
  }

  static List<TimeManager.PlayerTimeData> BuildFinalResults(List<RacerInfo> racers, TimeManager tm)
  {
    var dict = tm.data.ToDictionary(x => x.playerName, x => x);

    // 1) 완주자: finishOrder 순으로 고정
    var finished = racers
        .Where(r => r && r.finished)
        .OrderBy(r => r.finishOrder)
        .Select(r =>
        {
          var name = NameOf(r);
          if (!dict.TryGetValue(name, out var p))
          {
            p = new TimeManager.PlayerTimeData
            {
              playerName = name,
              finishTime = tm.RaceDuration,
              finished = true,
              isPlayer = r.isPlayer
            };
          }
          return p;
        });

    // 2) 미완주자: 진행도(랩↓, 체크포인트 인덱스↓, 다음 체크포인트까지 거리↑)
    var notFinished = racers
        .Where(r => r && !r.finished && r.lapCounter && r.lapCounter.checkpointManager)
        .OrderByDescending(r => r.lapCounter.currentLap)
        .ThenByDescending(r => r.lapCounter.nextCheckpoint ? r.lapCounter.nextCheckpoint.checkpointId : 0)
        .ThenBy(r => DistToNext(r))
        .Select(r =>
        {
          var name = NameOf(r);
          if (!dict.TryGetValue(name, out var p))
          {
            p = new TimeManager.PlayerTimeData
            {
              playerName = name,
              finishTime = -1f, // DNF
              finished = false,
              isPlayer = r.isPlayer
            };
          }
          return p;
        });

    return finished.Concat(notFinished).ToList();
  }
}