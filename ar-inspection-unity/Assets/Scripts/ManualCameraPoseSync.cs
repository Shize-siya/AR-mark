using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// 手动把 ARCore camera pose 同步到 Unity Camera。
/// 绕过 TrackedPoseDriver（旧版 SpatialTracking 和新版 InputSystem 在 activeInputHandler=1 下都不工作）。
/// 优先从 Input System XR 设备拿 pose，fallback 到 ARCameraManager 的 camera texture transform。
/// </summary>
public class ManualCameraPoseSync : MonoBehaviour
{
    private ARCameraManager _cameraManager;
    private bool _hasPose;
    private Vector3 _position;
    private Quaternion _rotation;

    void Awake()
    {
        _cameraManager = GetComponent<ARCameraManager>();
        if (_cameraManager != null)
            _cameraManager.frameReceived += OnFrameReceived;
        Debug.Log("[ManualCameraPoseSync] Awake");
    }

    void OnDestroy()
    {
        if (_cameraManager != null)
            _cameraManager.frameReceived -= OnFrameReceived;
    }

    void OnFrameReceived(ARCameraFrameEventArgs args)
    {
        // 方法 1: 从 camera texture 拿 display transform
        // 注意：这是 UV transform，不是 camera world pose...
    }

    void Update()
    {
        // 方法 2: 用 Input System 直接拿 Head pose
        TryInputSystemPose();
        
        if (_hasPose)
        {
            transform.position = _position;
            transform.rotation = _rotation;
        }
    }

    void TryInputSystemPose()
    {
        try
        {
            // Input System 包可能没在这个程序集里暴露... 用反射
            var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.XR.XRDevice, Unity.InputSystem");
            if (inputSystemType == null) return;
            
            var headProp = inputSystemType.GetProperty("head", 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (headProp == null) return;
            
            var headPose = headProp.GetValue(null);
            if (headPose == null) return;
            
            var positionProp = headPose.GetType().GetProperty("position");
            var rotationProp = headPose.GetType().GetProperty("rotation");
            if (positionProp != null && rotationProp != null)
            {
                _position = (Vector3)positionProp.GetValue(headPose);
                _rotation = (Quaternion)rotationProp.GetValue(headPose);
                _hasPose = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ManualCameraPoseSync] InputSystem pose failed: {e.Message}");
        }
    }
}
