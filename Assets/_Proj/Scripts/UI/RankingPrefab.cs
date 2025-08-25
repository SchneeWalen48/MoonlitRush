using TMPro;
using UnityEngine;

public class RankingPrefab : MonoBehaviour
{
    public TMP_Text rankText;
    public TMP_Text nameText;
    public TMP_Text timeText;

    public void Set(int rank, string name, string time)
    {
        rankText.text = rank.ToString();
        nameText.text = name;
        timeText.text = time;
    }

}
