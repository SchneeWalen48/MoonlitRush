using TMPro;
using UnityEngine;

public class RankingPrefab : MonoBehaviour
{
  public TextMeshProUGUI rankTxt;
  public TextMeshProUGUI nameTxt;
  public TextMeshProUGUI timeTxt;

  public void SetUI(int rank, string pName, float time)
  {
    rankTxt.text = $"{rank}.";
    nameTxt.text = pName;
    timeTxt.text = FormatTime(time);
  }

  string FormatTime(float time)
  {
    int minutes = Mathf.FloorToInt(time / 60);
    float seconds = time % 60;
    return $"{minutes:00}:{seconds:00.00}";
  }
}
