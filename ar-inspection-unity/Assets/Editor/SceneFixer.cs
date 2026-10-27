using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>修复 AR 巡检场景：绿色平面半透明材质 + 关闭按钮 + InputHandling 修正</summary>
public static class SceneFixer
{
    [MenuItem("Tools/AR巡检/修复：材质+关闭按钮+Input")]
    public static void FixAll()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // ===== 1. 材质：PlaneMat → Sprites/Default 半透明 =====
        var matPath = "Assets/Materials/PlaneMat.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        var shader = Shader.Find("Sprites/Default");
        if (mat != null && shader != null)
        {
            mat.shader = shader;
            mat.color = new Color(0.1f, 0.75f, 0.5f, 0.4f);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log("✅ PlaneMat: Shader=" + mat.shader.name + " Color=" + mat.color);
        }

        // ===== 2. 相机 Clear Flags → Depth =====
        var cam = Object.FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.Depth;
            EditorUtility.SetDirty(cam);
            Debug.Log("✅ Camera clearFlags = Depth");
        }

        // ===== 3. Input Handling → Legacy Only (反射改 PlayerSettings.activeInputHandler) =====
        // Unity 2022.3 PlayerSettings 里 activeInputHandler: 2=Both, 1=Legacy, 3=New
        var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        var field = typeof(PlayerSettings).GetField("activeInputHandler", flags);
        if (field != null)
        {
            field.SetValue(null, 1); // Legacy only
            Debug.Log("✅ Input Handling = Legacy Only (via reflection)");
        }

        // ===== 4. 关闭按钮 =====
        var panel = GameObject.Find("FormPanel");
        var closeButtonGO = GameObject.Find("CloseButton");
        if (panel != null && closeButtonGO == null)
        {
            closeButtonGO = new GameObject("CloseButton");
            closeButtonGO.transform.SetParent(panel.transform, false);
            var img = closeButtonGO.AddComponent<Image>();
            img.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            var btn = closeButtonGO.AddComponent<Button>();
            var rt = (RectTransform)closeButtonGO.transform;
            rt.anchorMin = new Vector2(0.92f, 0.92f);
            rt.anchorMax = new Vector2(0.99f, 0.99f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 子物体：文字
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(closeButtonGO.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.text = "X";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 40;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var trt = (RectTransform)txtGO.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            Debug.Log("✅ CloseButton created");
        }

        // ===== 5. IssueFormUI.closeButton 自动连线 =====
        var formUI = Object.FindObjectOfType<IssueFormUI>();
        if (formUI != null && formUI.closeButton == null)
        {
            var cb = GameObject.Find("CloseButton");
            if (cb != null)
            {
                formUI.closeButton = cb.GetComponent<Button>();
                EditorUtility.SetDirty(formUI);
                Debug.Log("✅ IssueFormUI.closeButton wired");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("🎉 全部修复完成，可以重新 Build And Run");
        EditorUtility.DisplayDialog("修复完成", "材质、关闭按钮、InputHandling 都修好了，重新打包即可。", "好的");
    }
}
