using UnityEngine;

public class ResolutionFixer : MonoBehaviour
{
    // Цей тег змушує функцію спрацювати до появи Splash Screen (логотипу)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void FixResolution()
    {
        // Беремо рідну роздільну здатність поточного монітора гравця
        Resolution currentRes = Screen.currentResolution;
        
        // Встановлюємо безпечний режим "FullScreenWindow" (безрамкове вікно).
        // Це найкращий режим для сучасних ігор, який НІКОЛИ не викликає помилок DX11
        Screen.SetResolution(currentRes.width, currentRes.height, FullScreenMode.FullScreenWindow);
        
        Debug.Log($"[ResolutionFixer] Set safe resolution: {currentRes.width}x{currentRes.height}");
    }
}