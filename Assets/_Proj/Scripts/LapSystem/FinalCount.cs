using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

  // ====== Audio ======
  [Header("Audio")]
  public AudioSource sfxSource;          // 카운트다운 동안 계속 울릴 SFX 재생기
  public AudioClip finalLoopClip;        // 카운트다운 10초 동안 루프로 재생할 클립
  public AudioSource bgmSource;          // 카메라(또는 BGM용) AudioSource
  [Range(0f, 1f)] public float bgmDimVolume = 0.35f; // 파이널 중에 줄일 볼륨
  [Min(0f)] public float bgmFadeTime = 0.3f;        // 페이드 시간
  public AudioClip finishStinger;        // ("Finish!")에 한 번만 틀 간단한 스팅어

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
    // BGM 볼륨 살짝 낮추기
    StartCoroutine(FadeAudio(bgmSource, bgmDimVolume, bgmFadeTime));

    // 카운트다운 동안 오디오 루프
    if (sfxSource && finalLoopClip)
    {
      sfxSource.loop = true;
      sfxSource.clip = finalLoopClip;
      sfxSource.Play();
    }
    if (finalCountText) finalCountText.gameObject.SetActive(true);

    for (int i = sec; i > 0; i--)
    {
      if (finalCountText) finalCountText.text = i.ToString();
      yield return new WaitForSecondsRealtime(1f);
    }

    if (finalCountText) finalCountText.text = "Finish!";
    if (sfxSource && sfxSource.isPlaying) sfxSource.Stop(); // 루프 종료
    if (sfxSource && finishStinger) sfxSource.PlayOneShot(finishStinger);

    yield return new WaitForSecondsRealtime(1f);

    if (endFade) StartCoroutine(FadeTo(endFade, 0f, 0.25f));
    var everyone = FindObjectsOfType<RacerInfo>(true);
    foreach (var r in everyone)
    {
      if (!r || (winner != null && r == winner)) continue;
      StartCoroutine(SlowdownOne(r, othersSlowDuration, true));
    }
    yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, othersSlowDuration * 0.6f));

    if (endFade) endFade.gameObject.SetActive(true);
    if (endFade) yield return FadeTo(endFade, 1f, endFadeDuration);
    OnFinalCountdownDone();
    LoadEndingSceneSafe();
  }

  IEnumerator SlowdownOne(RacerInfo racer, float duration, bool lockControl)
  {
    if (!racer) yield break;

    var tf = racer.transform;
    var rb = tf.GetComponentInParent<Rigidbody>() ?? tf.GetComponentInChildren<Rigidbody>();
    var car = tf.GetComponentInParent<CarController>() ?? tf.GetComponentInChildren<CarController>();
    var ai = tf.GetComponentInParent<AICarController>() ?? tf.GetComponentInChildren<AICarController>();

    if (car){ car.isFinished = true; car.moveInput = 0f;}
    if (ai) { ai.isFinished = true; ai.moveInput = 0f; }
    if (car)
    {
      car.BeginFinishSequence(duration, lockControl);
      yield break;
    }
    if (ai)
    {
      yield return ai.SmoothStop(duration);
      if (lockControl)
      {
        ai.moveStart = false;
        ai.enabled = false;
        if (rb) rb.isKinematic = true;
      }
      yield break;
    }
    // 시작값 캐시해서 매 프레임 부드럽게 Lerp
    float t = 0f;
    Vector3 v0 = rb ? rb.velocity : Vector3.zero;
    Vector3 w0 = rb ? rb.angularVelocity : Vector3.zero;
    while (t < duration)
    {
      if (!rb) break;
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

    bool visible = target > 0.001f;
    cg.blocksRaycasts = visible;
    cg.interactable = visible;

    if (dur <= 0f) { cg.alpha = target; yield break; }

    float start = cg.alpha, t = 0f;
    while (t < 1f)
    {
      t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
      cg.alpha = Mathf.Lerp(start, target, t);
      yield return null;
    }
    cg.alpha = target;
    cg.blocksRaycasts = visible;
    cg.interactable = visible;
  }

  IEnumerator FadeAudio(AudioSource src, float target, float dur)
  {
    if (!src) yield break;
    if (dur <= 0f) { src.volume = target; yield break; }

    float start = src.volume, t = 0f;
    while (t < 1f)
    {
      t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
      src.volume = Mathf.Lerp(start, target, t);
      yield return null;
    }
    src.volume = target;
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
      var dict = tm.data.ToDictionary(x => x.playerName, x => x);
      var final = new List<TimeManager.PlayerTimeData>();

      foreach (var r in rm.racers.Where(x => x && x.finished).OrderBy(x => x.finishOrder))
      {
        string name = TimeManager.SafeNameOf(r);
        if (dict.TryGetValue(name, out var p))
          final.Add(p);
        else
        {
          float f = (r.finishClock >= 0) ? r.finishClock : -1f;
          final.Add(new TimeManager.PlayerTimeData
          {
            playerName = name,
            finishTime = f,
            finished = (f >= 0),
            isPlayer = r.isPlayer
          });
        }
      }
      var notFinished = rm.racers.Where(x => x && !x.finished && x.lapCounter && x.lapCounter.checkpointManager)
        .OrderByDescending(x => x.lapCounter.currentLap)
        .ThenByDescending(x => x.lapCounter.nextCheckpoint ? x.lapCounter.nextCheckpoint.checkpointId : 0)
        .ThenBy(x =>
        {
          var lc = x.lapCounter;
          return lc?.nextCheckpoint ? Vector3.Distance(x.transform.position, lc.nextCheckpoint.transform.position) : float.MaxValue;
        });

      foreach (var r in notFinished)
      {
        string name = TimeManager.SafeNameOf(r);
        if (dict.TryGetValue(name, out var p)) final.Add(p);
        else final.Add(new TimeManager.PlayerTimeData
        {
          playerName = name,
          finishTime = -1f,
          finished = false,
          isPlayer = r.isPlayer
        });
      }
      //var final = BuildFinalResults(rm.racers, tm);

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
}