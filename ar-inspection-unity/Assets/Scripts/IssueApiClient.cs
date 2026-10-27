using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 与 Go 后端通信的 HTTP 客户端。
/// 挂在场景任意常驻物体上（如 AppManager），在 Inspector 中配置后端地址。
/// 真机使用时把 serverBaseUrl 改为电脑局域网 IP，例如 http://192.168.1.100:8080
/// </summary>
public class IssueApiClient : MonoBehaviour
{
    [Tooltip("Go 后端地址，真机填电脑局域网 IP，例如 http://192.168.1.100:8080")]
    public string serverBaseUrl = "http://localhost:8080";

    [System.Serializable]
    private class PositionData
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    private class CreateIssueBody
    {
        public string title;
        public string description;
        public string priority;
        public PositionData position;
    }

    /// <summary>上报一条问题到后端，回调参数依次表示：是否成功、提示信息</summary>
    public void SubmitIssue(string title, string description, string priority, Vector3 position,
        System.Action<bool, string> onDone)
    {
        if (onDone == null) { Debug.LogError("[IssueApiClient] onDone callback is null!"); return; }
        Debug.Log($"[IssueApiClient] SubmitIssue called. URL={serverBaseUrl}, title={title}, pos={position}");
        StartCoroutine(PostIssue(title, description, priority, position, onDone));
    }

    private IEnumerator PostIssue(string title, string description, string priority, Vector3 position,
        System.Action<bool, string> onDone)
    {
        var body = new CreateIssueBody
        {
            title = title,
            description = description,
            priority = priority,
            position = new PositionData { x = position.x, y = position.y, z = position.z },
        };
        string json = JsonUtility.ToJson(body);
        string url = serverBaseUrl + "/api/issues";
        Debug.Log($"[IssueApiClient] POST {url}\nBody: {json}");

        using (var request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            Debug.Log("[IssueApiClient] Sending web request...");
            yield return request.SendWebRequest();

            Debug.Log($"[IssueApiClient] Done. result={request.result}, responseCode={request.responseCode}, error={request.error}");

            if (request.result == UnityWebRequest.Result.Success &&
                request.responseCode >= 200 && request.responseCode < 300)
            {
                Debug.Log($"[IssueApiClient] SUCCESS: {request.downloadHandler.text}");
                onDone?.Invoke(true, "提交成功");
            }
            else if (request.responseCode == 400)
            {
                string msg = ExtractMessage(request.downloadHandler.text);
                Debug.LogWarning($"[IssueApiClient] 400 error: {msg}");
                onDone?.Invoke(false, "提交失败: " + msg);
            }
            else
            {
                Debug.LogError($"[IssueApiClient] FAIL: result={request.result}, code={request.responseCode}, err={request.error}");
                onDone?.Invoke(false, $"网络错误({request.responseCode})，请检查后端地址与网络");
            }
        }
    }

    /// <summary>从后端错误 JSON 中提取 message 字段（简单解析，避免引入第三方库）</summary>
    private static string ExtractMessage(string json)
    {
        const string key = "\"message\":\"";
        int start = json.IndexOf(key, System.StringComparison.Ordinal);
        if (start < 0) return "参数错误";
        start += key.Length;
        int end = json.IndexOf('"', start);
        return end > start ? json.Substring(start, end - start) : "参数错误";
    }
}
