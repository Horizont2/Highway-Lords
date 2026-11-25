using UnityEngine;
using UnityEngine.UI;

public class CloudScroller : MonoBehaviour
{
    [Header("Налаштування руху (Для UI)")]
    public float speed = 50f; // Швидкість в пікселях
    public bool moveRight = true;

    [Header("Межі екрану (Телепортація)")]
    // Для UI зазвичай межі більші (наприклад, -1200 і 1200)
    public float despawnX = 1500f; 
    public float respawnX = -1500f;

    [Header("Рандомна висота")]
    public float minY = -500f; 
    public float maxY = 500f;

    private RectTransform rect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (rect == null) return;

        float moveDir = moveRight ? 1f : -1f;
        rect.anchoredPosition += new Vector2(moveDir * speed * Time.deltaTime, 0);

        if (moveRight && rect.anchoredPosition.x > despawnX)
        {
            RespawnCloud();
        }
        else if (!moveRight && rect.anchoredPosition.x < despawnX)
        {
            RespawnCloud();
        }
    }

    void RespawnCloud()
    {
        float randomY = Random.Range(minY, maxY);
        rect.anchoredPosition = new Vector2(respawnX, randomY);
    }
}