using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestRowUI : MonoBehaviour
{
    [Header("Тексти")]
    public TMP_Text titleText;
    public TMP_Text descText;      // Ось наш текст опису!
    public TMP_Text progressText;
    public TMP_Text rewardText;
    public TMP_Text btnText;

    [Header("Графіка та Інтерактив")]
    public Slider progressSlider;
    public Image questIcon;
    public Image rewardIcon;
    public Button claimBtn;
}