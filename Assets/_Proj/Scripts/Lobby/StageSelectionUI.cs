using UnityEngine;
using UnityEngine.UI;

public class StageSelectManager : MonoBehaviour
{
  public Outline[] stageOutlines; //스테이지 썸네일 테두리 배열
  public Color selectedColor = Color.yellow;
  public Color defaultColor = Color.clear;

  public Button StartButton; //게임 시작 버튼

  int selectedStageIndex = -1; //현재 선택된 스테이지 인덱스 ; -1로 한 이유는 처음 시작 시 스테이지를 선택한 상태가 아니기 때문
  public AudioSource source;
  public AudioClip clip;

  private void Start()
  {
    //모든 스테이지 썸네일을 기본 색상으로
    for (int i = 0; i < stageOutlines.Length; i++)
    {
      stageOutlines[i].effectColor = defaultColor;

      //각 썸네일에 클릭 이벤트 추가
      int index = i; //클로저 문제 방지 ; 클로저: 
      stageOutlines[i].GetComponent<Button>().onClick.AddListener(() => OnStageSelected(index));
    }

    if (StartButton != null)
    {
      StartButton.interactable = false;
    }
  }

  public void OnStageSelected(int index)
  {
    //이전에 선택된 스테이지의 색상을 원래대로
    if (selectedStageIndex != -1)
    {
      stageOutlines[selectedStageIndex].effectColor = defaultColor;
    }

    //새롭게 선택된 스테이지의 색상 변경
    selectedStageIndex = index;
    stageOutlines[selectedStageIndex].effectColor = selectedColor;

    //선택된 스테이지 인덱스를 저장
    PlayerPrefs.SetInt("SelectedStage", selectedStageIndex);
    //Debug.Log("Selected Stage: " + (selectedStageIndex));

    //스테이지 선택 시 시작 버튼 활성화
    if (StartButton != null)
    {

      StartButton.interactable = true;
    }
    source.PlayOneShot(clip);
  }

  public void StartGame()
  {
    //스테이지 선택 확인
    if (selectedStageIndex != -1)
    {
      //선택된 스테이지 인덱스를 기반으로 scene 로드
      UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
      source.PlayOneShot(clip);
    }
    else
    {
      //Debug.LogWarning("게임 시작 전 스테이지 선택 확인");
    }
  }
}
