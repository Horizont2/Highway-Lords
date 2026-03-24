using UnityEditor;
using UnityEngine;
using UnityEngine.UI; // Обов'язково для роботи з LayoutGroup!

public class AnchorSnapper : MonoBehaviour
{
    [MenuItem("UI/Snap Anchors %q")]
    static void SnapAnchors()
    {
        SnapList(Selection.gameObjects);
    }

    [MenuItem("UI/Snap Anchors (Include Children) %#q")]
    static void SnapAnchorsRecursively()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            RectTransform[] allRects = go.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rt in allRects)
            {
                SnapSingle(rt);
            }
        }
    }

    static void SnapList(GameObject[] objects)
    {
        foreach (GameObject go in objects)
        {
            SnapSingle(go.GetComponent<RectTransform>());
        }
    }

    static void SnapSingle(RectTransform t)
    {
        if (t == null) return;

        RectTransform pt = t.parent as RectTransform;
        if (pt == null) return;

        // === РОЗУМНА ПЕРЕВІРКА ===
        // Якщо батько керує цим об'єктом через Layout Group - не чіпаємо його якорі!
        if (pt.GetComponent<LayoutGroup>() != null)
        {
            Debug.Log($"[AnchorSnapper] Пропущено '{t.name}', бо він підпорядковується LayoutGroup.");
            return;
        }
        // =========================

        Undo.RecordObject(t, "Snap Anchors"); 

        Vector2 newAnchorsMin = new Vector2(t.anchorMin.x + t.offsetMin.x / pt.rect.width,
                                            t.anchorMin.y + t.offsetMin.y / pt.rect.height);
        Vector2 newAnchorsMax = new Vector2(t.anchorMax.x + t.offsetMax.x / pt.rect.width,
                                            t.anchorMax.y + t.offsetMax.y / pt.rect.height);

        t.anchorMin = newAnchorsMin;
        t.anchorMax = newAnchorsMax;
        t.offsetMin = t.offsetMax = new Vector2(0, 0);
    }
}