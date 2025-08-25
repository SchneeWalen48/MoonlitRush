using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using PlayerTimeData = TimeManager.PlayerTimeData;


public class PodiumWinnerController : MonoBehaviour
{
    [Header("Winner Spot")]
    public Transform winnerSpot;      // 우승 차량 자리
    public Vector3 offset;            // 필요시 보정
    public Vector3 rotation;
    public float scale = 1f;
    

    [Header("UI")]
    public TMP_Text nameText;
    public TMP_Text timeText;
    public GameObject confetti;

    [Header("Move (optional)")]
    public float moveDuration = 1.0f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float yOffset = 0f;

    bool isRunning;
    Coroutine moveCo;

    void Start()
    {
    //var tm = TimeManager.Instance;
    //if (tm == null)
    //{
    //    Debug.LogError("[Podium] TimeManager.Instance is NULL. Make sure TimeManager exists in the race scene and uses DontDestroyOnLoad.");
    //    return;
    //}

        List<PlayerTimeData> results = RaceDataStore.RankingData;
        if (results == null || results.Count == 0)
        {
            Debug.LogWarning("[Podium] Ranking data not found.");
            return;
        }

        // 우승 차량 스폰
        //if (tm.winnerPodiumPrefab && winnerSpot)
        //{
        //    var car = Instantiate(tm.winnerPodiumPrefab, winnerSpot);
        //    car.transform.localPosition = offset;
        //    car.transform.localEulerAngles = rotation;
        //    car.transform.localScale = Vector3.one * scale;
        //}

        // 1등 텍스트 세팅 (Inspector 연결 필수)
        var winner = results[0];
        if (nameText) nameText.text = winner.playerName;
        if (timeText) timeText.text = TimeManager.FormatTime(winner.finishTime);

        if (confetti) confetti.SetActive(true);
    }
    public void ShowWinner()
    {
        if (isRunning) return;

        

        string nameFromRanking = null;
        float timeFromRanking = 0f;
        var ranks = TimeManager.Instance ? TimeManager.Instance.GetRanking() : null;
        if (ranks != null && ranks.Count > 0)
        {
            nameFromRanking = ranks[0].playerName;
            timeFromRanking = ranks[0].finishTime;
        }

        var winner = FindWinnerRacer();
        if (winner != null)
        {
            var display = string.IsNullOrWhiteSpace(winner.displayName) ? winner.racerName : winner.displayName;
            if (string.IsNullOrWhiteSpace(nameFromRanking)) nameFromRanking = display;
            if (timeFromRanking <= 0f) timeFromRanking = GetWinnerTimeFor(winner);

            if (moveCo != null) StopCoroutine(moveCo);
            moveCo = StartCoroutine(MoveWinnerCar(winner.transform));
        }
        else Debug.Log("[Podium] Winner car not found; show text only.");

        if (nameText) nameText.text = string.IsNullOrWhiteSpace(nameFromRanking) ? "Winner" : nameFromRanking;
        if (timeText) timeText.text = TimeManager.FormatTime(timeFromRanking);

        var ps = confetti ? confetti.GetComponent<ParticleSystem>() : null;
        if (ps) { if (!ps.isPlaying) ps.Play(); }
        else if (confetti && !confetti.activeSelf) confetti.SetActive(true);

        isRunning = true;
    }


    RacerInfo FindWinnerRacer()
    {
        var ranks = TimeManager.Instance ? TimeManager.Instance.GetRanking() : null;
        if (ranks != null && ranks.Count > 0)
        {
            var topName = ranks[0].playerName;
            var allRacers = FindObjectsOfType<RacerInfo>();
            var match = allRacers.FirstOrDefault(r =>
                (!string.IsNullOrWhiteSpace(r.displayName) && r.displayName == topName) ||
                (!string.IsNullOrWhiteSpace(r.racerName) && r.racerName == topName));
            if (match) return match;
        }
        return null;
    }

    float GetWinnerTimeFor(RacerInfo winner)
    {
        if (!TimeManager.Instance) return 0f;
        var ranks = TimeManager.Instance.GetRanking();
        if (ranks == null || ranks.Count == 0) return 0f;

        foreach (var key in new[] { winner.displayName, winner.racerName })
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            var row = ranks.FirstOrDefault(r => r.playerName == key);
            if (row != null) return row.finishTime;
        }
        return 0f;
    }

    IEnumerator MoveWinnerCar(Transform car)
    {
        if (!car || !winnerSpot) yield break;
        var rb = car.GetComponent<Rigidbody>();
        if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }

        Vector3 startPos = car.position; Quaternion startRot = car.rotation;
        Vector3 endPos = winnerSpot.position + Vector3.up * yOffset;
        Quaternion endRot = winnerSpot.rotation;

        float t = 0f;
        while (t < moveDuration)
        {
            float k = ease.Evaluate(t / Mathf.Max(0.0001f, moveDuration));
            car.position = Vector3.Lerp(startPos, endPos, k);
            car.rotation = Quaternion.Slerp(startRot, endRot, k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        car.position = endPos; car.rotation = endRot;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!winnerSpot) Debug.LogWarning("[Podium] winnerSpot is not assigned.", this);
    }
#endif
}