using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class FogRegion : MonoBehaviour
{
    [Tooltip("Введи ID табору (campId), перемога над яким розсіює цей туман")]
    public string unlockCampId; 

    private CanvasGroup cg;

    void Start()
    {
        cg = GetComponent<CanvasGroup>();
        
        // Перевіряємо, чи цей табір вже був захоплений раніше
        bool isConquered = PlayerPrefs.GetInt("Camp_" + unlockCampId + "_Conquered", 0) == 1;

        if (isConquered)
        {
            // Якщо ми ЩОЙНО повернулися з битви саме за цей табір і перемогли -> граємо красиву анімацію
            if (CrossSceneData.isReturningFromBattle && CrossSceneData.campId == unlockCampId && CrossSceneData.lastBattleWon)
            {
                StartCoroutine(PartingFogAnimation());
            }
            else
            {
                // Якщо ми давно його захопили (просто завантажили гру) -> ховаємо миттєво
                gameObject.SetActive(false);
            }
        }
    }

    IEnumerator PartingFogAnimation()
    {
        float duration = 2.0f; // Туман розсіюється 2 секунди
        float t = 0;
        
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 1.3f; // Туман ніби "розповзається" в сторони

        while (t < duration)
        {
            t += Time.deltaTime;
            
            // Плавно робимо прозорим
            cg.alpha = Mathf.Lerp(1f, 0f, t / duration);
            
            // Плавно збільшуємо
            transform.localScale = Vector3.Lerp(startScale, endScale, t / duration);
            
            yield return null;
        }

        gameObject.SetActive(false);
    }
}