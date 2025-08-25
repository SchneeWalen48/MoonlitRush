using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PlayerTimeData = TimeManager.PlayerTimeData;

public class EndingSceneController : MonoBehaviour
{
  [Header("UI References")]
  public RankingUI rankingUI;    // 씬 안의 RankingUI 오브젝트 직접 드래그(없으면 자동 탐색)
  public GameObject buttonsRoot;
  public Button toLobbyButton;
  public string lobbySceneName = "Lobby";
  public Button quitButton;

  [Header("Delay")]
  public float showDelay = 0.6f;

  void Start()
  {
    // 버튼 이벤트 등록
    if (toLobbyButton)
      toLobbyButton.onClick.AddListener(() =>
      {
        if (SceneManagers.Instance) SceneManagers.LoadScene(lobbySceneName);
        else SceneManager.LoadScene(lobbySceneName);
      });

    if (quitButton)
      quitButton.onClick.AddListener(Application.Quit);

    if (!rankingUI) rankingUI = FindObjectOfType<RankingUI>(true);
    if (buttonsRoot) buttonsRoot.SetActive(false);

    StartCoroutine(CoSequence());
  }

  IEnumerator CoSequence()
  {
    if (showDelay > 0f)
      yield return new WaitForSecondsRealtime(showDelay);

    var tm = TimeManager.Instance;
    var rm = RaceManager.Instance;

    // 1) DNF 채우기(완주 못한 사람도 반드시 순위에 나오게)
    if (tm != null && rm != null)
      tm.EnsureDNFsFrom(rm.racers);

    // 2) 결과 가져오기: RaceDataStore → TimeManager → 마지막 수단으로 현재 racers로 빌드
    List<PlayerTimeData> results = RaceDataStore.RankingData;

    if (results == null || results.Count == 0)
      results = tm != null ? tm.GetRanking() : null;

    if ((results == null || results.Count == 0) && rm != null && rm.racers != null)
    {
      // racers에서 즉석 생성 (들어온 순서가 없으면 현재 정렬대로)
      results = new List<PlayerTimeData>(rm.racers.Count);
      foreach (var r in rm.racers)
      {
        if (!r) continue;
        string safeName =
            !string.IsNullOrWhiteSpace(r.displayName) ? r.displayName :
            !string.IsNullOrWhiteSpace(r.racerName) ? r.racerName :
            PlayerPrefs.GetString("PlayerNickname", "Player");

        float f = (r.finished && tm != null) ? tm.RaceDuration : -1f; // DNF는 -1 => "--"
        results.Add(new PlayerTimeData { playerName = safeName, finishTime = f, finished = r.finished, isPlayer = r.isPlayer });
      }
    }

    if (results == null || results.Count == 0)
    {
      //Debug.LogWarning("[Ending] 랭킹 데이터 없음");
      if (buttonsRoot) buttonsRoot.SetActive(true);
      yield break;
    }

    // 3) 랭킹 표시
    if (rankingUI)
    {
      rankingUI.ShowRanking(results);
      //Debug.Log($"[Ending] {results.Count}명 랭킹 표시 완료");
    }
    else
    {
      //Debug.LogError("[Ending] RankingUI가 씬에 없습니다! (씬에 배치하고 인스펙터 연결 필요)");
    }

    // 4) 버튼 표시
    if (buttonsRoot) buttonsRoot.SetActive(true);
  }
}
