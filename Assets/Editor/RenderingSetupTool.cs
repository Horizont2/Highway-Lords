using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Налаштування рендерингу URP 2D для всіх сцен проєкту.
///
/// Навіщо: у проєкті встановлено URP + Renderer2D, але пайплайн не був призначений,
/// тому 2D-світло й пост-обробка не працювали, а спрайти з матеріалом
/// URP/Sprite-Lit-Default не могли рендеритись коректно.
///
/// Інструмент працює ТІЛЬКИ в едіторі (edit-time). У рантаймі нічого не створюється:
/// усі об'єкти та компоненти додаються в сцену тут і зберігаються в неї.
/// </summary>
public static class RenderingSetupTool
{
    private const string RenderingGroupName = "Rendering";
    private const string GlobalLightName = "Global Light 2D";
    private const string GlobalVolumeName = "Global Volume";

    private const string RenderingFolder = "Assets/Settings/Rendering";
    private const string ProfilePath = RenderingFolder + "/GlobalPostProcess.asset";

    // ------------------------------------------------------------------
    // 1. Діагностика (нічого не змінює)
    // ------------------------------------------------------------------

    [MenuItem("Tools/Highway Lords/Rendering/1. Перевірити налаштування", false, 100)]
    public static void Check()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== ПЕРЕВІРКА РЕНДЕРИНГУ ===");
        sb.AppendLine();

        var pipeline = GraphicsSettings.defaultRenderPipeline;
        if (pipeline is UniversalRenderPipelineAsset urp)
        {
            sb.AppendLine($"[OK] Активний пайплайн: URP ({urp.name})");
            sb.AppendLine($"     HDR: {urp.supportsHDR}, SRP Batcher: {urp.useSRPBatcher}");
        }
        else if (pipeline == null)
        {
            sb.AppendLine("[ПРОБЛЕМА] Пайплайн не призначено — гра рендериться на Built-in RP.");
            sb.AppendLine("           Project Settings > Graphics > Default Render Pipeline");
        }
        else
        {
            sb.AppendLine($"[УВАГА] Активний пайплайн не URP: {pipeline.GetType().Name}");
        }

        sb.AppendLine();
        sb.AppendLine($"Профіль пост-обробки: {(File.Exists(ProfilePath) ? ProfilePath : "ще не створено")}");
        sb.AppendLine();

        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled) continue;

            sb.AppendLine($"--- {Path.GetFileNameWithoutExtension(entry.path)}");

            var scene = EditorSceneManager.GetSceneByPath(entry.path);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                sb.AppendLine("    (не відкрита — відкрий сцену, щоб побачити деталі)");
                continue;
            }

            int cameras = 0, camerasWithUrpData = 0, camerasWithPostFx = 0;
            int globalLights = 0, otherLights = 0, volumes = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                {
                    cameras++;
                    var data = cam.GetComponent<UniversalAdditionalCameraData>();
                    if (data == null) continue;
                    camerasWithUrpData++;
                    if (data.renderPostProcessing) camerasWithPostFx++;
                }

                foreach (var light in root.GetComponentsInChildren<Light2D>(true))
                {
                    if (light.lightType == Light2D.LightType.Global) globalLights++;
                    else otherLights++;
                }

                volumes += root.GetComponentsInChildren<Volume>(true).Length;
            }

            sb.AppendLine($"    камери: {cameras}, з URP-даними: {camerasWithUrpData}, з пост-обробкою: {camerasWithPostFx}");
            sb.AppendLine($"    2D-світло: глобальне {globalLights}, інше {otherLights}");
            sb.AppendLine($"    Volume: {volumes}");

            if (globalLights == 0)
                sb.AppendLine("    [ПРОБЛЕМА] немає глобального 2D-світла — lit-спрайти будуть чорні");
            if (cameras > 0 && camerasWithPostFx == 0)
                sb.AppendLine("    [ПРОБЛЕМА] пост-обробка вимкнена на всіх камерах");
        }

        Debug.Log(sb.ToString());
    }

    // ------------------------------------------------------------------
    // 2. Налаштування всіх сцен
    // ------------------------------------------------------------------

    [MenuItem("Tools/Highway Lords/Rendering/2. Налаштувати всі сцени", false, 101)]
    public static void SetupAllScenes()
    {
        if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
        {
            EditorUtility.DisplayDialog(
                "URP не активний",
                "Спершу призначи URP:\nProject Settings > Graphics > Default Render Pipeline = UniversalRP\n\n" +
                "Без цього 2D-світло й пост-обробка не працюють.",
                "Зрозуміло");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var profile = GetOrCreateProfile();
        var log = new StringBuilder();
        log.AppendLine("=== НАЛАШТУВАННЯ РЕНДЕРИНГУ ===");

        string openScenePath = EditorSceneManager.GetActiveScene().path;

        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.path)) continue;

            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            log.AppendLine();
            log.AppendLine($"--- {scene.name}");

            bool changed = false;
            changed |= SetupCameras(scene, log);
            changed |= SetupRenderingGroup(scene, profile, log);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.AppendLine("    сцену збережено");
            }
            else
            {
                log.AppendLine("    змін не потрібно");
            }
        }

        if (!string.IsNullOrEmpty(openScenePath))
            EditorSceneManager.OpenScene(openScenePath, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        Debug.Log(log.ToString());
    }

    // ------------------------------------------------------------------

    private static bool SetupCameras(Scene scene, StringBuilder log)
    {
        bool changed = false;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data == null)
                {
                    data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    log.AppendLine($"    + UniversalAdditionalCameraData на '{cam.name}'");
                    changed = true;
                }

                if (data.renderType != CameraRenderType.Base)
                {
                    data.renderType = CameraRenderType.Base;
                    changed = true;
                }

                if (!data.renderPostProcessing)
                {
                    data.renderPostProcessing = true;
                    log.AppendLine($"    > пост-обробка увімкнена на '{cam.name}'");
                    changed = true;
                }

                // Для 2D-пікселів MSAA/FXAA зайві — вони милять спрайти.
                if (data.antialiasing != AntialiasingMode.None)
                {
                    data.antialiasing = AntialiasingMode.None;
                    changed = true;
                }

                if (changed) EditorUtility.SetDirty(cam.gameObject);
            }
        }

        return changed;
    }

    private static bool SetupRenderingGroup(Scene scene, VolumeProfile profile, StringBuilder log)
    {
        bool changed = false;

        // Група в ієрархії, щоб рендеринг не валявся серед кореневих об'єктів.
        GameObject group = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == RenderingGroupName) { group = root; break; }
        }

        if (group == null)
        {
            group = new GameObject(RenderingGroupName);
            SceneManager.MoveGameObjectToScene(group, scene);
            group.transform.position = Vector3.zero;
            log.AppendLine($"    + група '{RenderingGroupName}'");
            changed = true;
        }

        // --- глобальне 2D-світло ---
        Light2D globalLight = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var light in root.GetComponentsInChildren<Light2D>(true))
            {
                if (light.lightType != Light2D.LightType.Global) continue;
                globalLight = light;
                break;
            }
            if (globalLight != null) break;
        }

        if (globalLight == null)
        {
            var lightGO = new GameObject(GlobalLightName);
            SceneManager.MoveGameObjectToScene(lightGO, scene);
            lightGO.transform.SetParent(group.transform, false);

            globalLight = lightGO.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.color = Color.white;
            globalLight.intensity = 1f;
            globalLight.blendStyleIndex = 0; // Multiply — нейтрально при intensity 1

            log.AppendLine($"    + '{GlobalLightName}' (без нього lit-спрайти чорні)");
            changed = true;
        }
        else if (globalLight.transform.parent != group.transform)
        {
            globalLight.transform.SetParent(group.transform, true);
            log.AppendLine($"    > '{globalLight.name}' перенесено в '{RenderingGroupName}'");
            changed = true;
        }

        // --- глобальний Volume для пост-обробки ---
        Volume volume = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var v in root.GetComponentsInChildren<Volume>(true))
            {
                if (!v.isGlobal) continue;
                volume = v;
                break;
            }
            if (volume != null) break;
        }

        if (volume == null)
        {
            var volumeGO = new GameObject(GlobalVolumeName);
            SceneManager.MoveGameObjectToScene(volumeGO, scene);
            volumeGO.transform.SetParent(group.transform, false);

            volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = profile;

            log.AppendLine($"    + '{GlobalVolumeName}' (профіль: {profile.name})");
            changed = true;
        }
        else if (volume.sharedProfile == null)
        {
            volume.sharedProfile = profile;
            log.AppendLine($"    > профіль призначено на '{volume.name}'");
            changed = true;
        }

        return changed;
    }

    // ------------------------------------------------------------------

    private static VolumeProfile GetOrCreateProfile()
    {
        var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder(RenderingFolder))
            AssetDatabase.CreateFolder("Assets/Settings", "Rendering");

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);

        // Стартова база — стримана. Точне налаштування — на етапі візуалу.
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.overrideState = true; bloom.threshold.value = 0.9f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 0.5f;
        bloom.scatter.overrideState = true; bloom.scatter.value = 0.6f;

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.overrideState = true; vignette.intensity.value = 0.22f;
        vignette.smoothness.overrideState = true; vignette.smoothness.value = 0.45f;

        var color = profile.Add<ColorAdjustments>(true);
        color.contrast.overrideState = true; color.contrast.value = 5f;
        color.saturation.overrideState = true; color.saturation.value = 4f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Створено профіль пост-обробки: {ProfilePath}");
        return profile;
    }
}
