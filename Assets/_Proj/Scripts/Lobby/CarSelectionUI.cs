using UnityEngine;

public class CarSelectionUI : MonoBehaviour
{
  public GameObject[] carPrefabs;  //현재 배열에 넣은 car는 임시로 후에 수정
  int selectedCarIndex = 0;

  [SerializeField] private AudioSource source;
  public AudioClip selectedCarClip;

  private void Start()
  {
    // 게임 시작 시 첫 번째 차량만 활성화
    for (int i = 0; i < carPrefabs.Length; i++)
    {
      carPrefabs[i].SetActive(i == selectedCarIndex);
    }
  }

  public void SelectNextCar() //다음 차량으로 변경
  {
    selectedCarIndex++;
    if (selectedCarIndex >= carPrefabs.Length)
    {
      selectedCarIndex = 0;
    }
    UpdateCarDisplay();
    source.PlayOneShot(selectedCarClip);
  }

  public void SelectPreviousCar() //이전 차량으로 변경
  {
    selectedCarIndex--;
    if (selectedCarIndex < 0)
    {
      selectedCarIndex = carPrefabs.Length - 1;
    }
    UpdateCarDisplay();
    source.PlayOneShot(selectedCarClip);
  }

  void UpdateCarDisplay()
  {
    //모든 차량 비활성화
    foreach (var car in carPrefabs)
    {
      car.SetActive(false);
    }
    //선택된 차량만 활성화
    carPrefabs[selectedCarIndex].SetActive(true);
  }


  // 차량 선택 정보 저장만
  public void SaveSelection()
  {
    PlayerPrefs.SetInt("SelectedCarIndex", selectedCarIndex);
    PlayerPrefs.SetInt("CarCount", carPrefabs.Length); // 배열 길이 동기화
  }
}
