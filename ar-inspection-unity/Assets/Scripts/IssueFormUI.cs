using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 问题上报表单（uGUI）：标题(必填)、描述、优先级、提交按钮、状态提示。
/// 挂在 Canvas 上，在 Inspector 中关联各 UI 控件。
/// </summary>
public class IssueFormUI : MonoBehaviour
{
    [Header("UI 控件")]
    public GameObject formPanel;      // 表单面板（默认隐藏）
    public InputField titleInput;     // 标题输入框（必填）
    public InputField descriptionInput; // 描述输入框（选填）
    public Dropdown priorityDropdown; // 优先级：0=low 1=medium 2=high
    public Button submitButton;
    public Button closeButton;        // 关闭按钮（手动收起表单）
    public Text statusText;           // 提示信息（成功/失败）

    [Header("依赖")]
    public IssueApiClient apiClient;
    public PlaceMarkerOnPlane markerPlacer;

    private static readonly string[] Priorities = { "low", "medium", "high" };
    private CanvasGroup formCanvasGroup;

    // 三个优先级按钮 — 运行时自动查找
    private Button[] _priorityButtons;
    private Image[] _priorityButtonImages;

    // 选中态颜色（更深）和未选中态颜色
    private static readonly Color[] NormalColors =
    {
        new Color(0.30f, 0.75f, 0.35f, 1f), // 低 绿
        new Color(0.90f, 0.75f, 0.20f, 1f), // 中 黄
        new Color(0.85f, 0.25f, 0.25f, 1f), // 高 红
    };
    private static readonly Color[] SelectedColors =
    {
        new Color(0.15f, 0.45f, 0.20f, 1f), // 选中低：深绿
        new Color(0.60f, 0.50f, 0.10f, 1f), // 选中中：深黄
        new Color(0.55f, 0.15f, 0.15f, 1f), // 选中高：深红
    };

    /// <summary>当前选中的优先级索引 0=low 1=medium 2=high</summary>
    private int _selectedPriority = 1;

    /// <summary>供三个优先级按钮调用，设置当前选中的优先级值并更新视觉</summary>
    public void SetPriority(int index)
    {
        _selectedPriority = Mathf.Clamp(index, 0, 2);
        UpdatePriorityButtonHighlight();
        Debug.Log("[IssueFormUI] Priority set to index=" + _selectedPriority + " (" + Priorities[_selectedPriority] + ")");
    }

    private void UpdatePriorityButtonHighlight()
    {
        if (_priorityButtonImages == null || _priorityButtonImages.Length < 3) return;
        for (int i = 0; i < 3; i++)
        {
            if (_priorityButtonImages[i] == null) continue;
            // 选中的：深颜色 + 稍微缩小一点 + 加粗边框效果
            _priorityButtonImages[i].color = (i == _selectedPriority) ? SelectedColors[i] : NormalColors[i];
            // 选中的按钮稍微突出（scale 1.05 vs 0.97）
            var rt = _priorityButtons[i].GetComponent<RectTransform>();
            if (rt != null)
            {
                float scale = (i == _selectedPriority) ? 1.05f : 0.97f;
                rt.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }

    private void FindPriorityButtons()
    {
        _priorityButtons = new Button[3];
        _priorityButtonImages = new Image[3];
        // PriorityDropdown 下面有三个按钮：Btn_低, Btn_中, Btn_高
        var pd = formPanel != null ? formPanel.transform.Find("PriorityDropdown") : null;
        if (pd == null) return;
        string[] names = { "Btn_低", "Btn_中", "Btn_高" };
        for (int i = 0; i < 3; i++)
        {
            var child = pd.Find(names[i]);
            if (child != null)
            {
                _priorityButtons[i] = child.GetComponent<Button>();
                _priorityButtonImages[i] = child.GetComponent<Image>();
            }
        }
        UpdatePriorityButtonHighlight();
    }

    public bool IsFormOpen => formPanel != null && formPanel.activeSelf;

    private void Start()
    {
        // 确保 formPanel 上有 CanvasGroup（用于过渡动画）
        formCanvasGroup = formPanel.GetComponent<CanvasGroup>();
        if (formCanvasGroup == null)
            formCanvasGroup = formPanel.AddComponent<CanvasGroup>();
        formCanvasGroup.alpha = 0f;
        formCanvasGroup.blocksRaycasts = false;
        formCanvasGroup.interactable = false;
        formPanel.SetActive(false);

        submitButton.onClick.AddListener(OnSubmitClicked);
        if (closeButton != null) closeButton.onClick.AddListener(CloseForm);
        SetStatus("点击画面中的平面放置标记");

        // 自动查找三个优先级按钮并初始化视觉
        FindPriorityButtons();
    }

    /// <summary>标记放置成功后由 PlaceMarkerOnPlane 调用</summary>
    public void OnMarkerPlaced()
    {
        StopAllCoroutines();
        StartCoroutine(FadeInForm());
        SetStatus("已放置标记，请填写问题信息");
    }

    private IEnumerator FadeInForm()
    {
        formPanel.SetActive(true);
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.unscaledDeltaTime;
            formCanvasGroup.alpha = Mathf.Clamp01(t / 0.12f);
            yield return null;
        }
        formCanvasGroup.alpha = 1f;
        formCanvasGroup.blocksRaycasts = true;
        formCanvasGroup.interactable = true;
    }

    private IEnumerator FadeOutForm(System.Action onDone = null)
    {
        float t = 0f;
        formCanvasGroup.blocksRaycasts = false;
        formCanvasGroup.interactable = false;
        while (t < 0.12f)
        {
            t += Time.unscaledDeltaTime;
            formCanvasGroup.alpha = 1f - Mathf.Clamp01(t / 0.12f);
            yield return null;
        }
        formCanvasGroup.alpha = 0f;
        formPanel.SetActive(false);
        onDone?.Invoke();
    }

    private void OnSubmitClicked()
    {
        string title = titleInput.text.Trim();

        if (string.IsNullOrEmpty(title))
        {
            SetStatus("标题不能为空");
            return;
        }
        if (markerPlacer == null || markerPlacer.CurrentMarkerPosition == null)
        {
            SetStatus("请先点击平面放置标记");
            return;
        }
        if (apiClient == null)
        {
            Debug.LogError("[IssueFormUI] apiClient is NOT ASSIGNED in Inspector!");
            SetStatus("API 客户端未配置，请检查场景");
            return;
        }

        Debug.Log($"[IssueFormUI] Submitting: title={title}, priority={Priorities[_selectedPriority]}, pos={markerPlacer.CurrentMarkerPosition.Value}");
        submitButton.interactable = false;
        SetStatus("提交中...");

        apiClient.SubmitIssue(
            title,
            descriptionInput.text.Trim(),
            Priorities[_selectedPriority],
            markerPlacer.CurrentMarkerPosition.Value,
            OnSubmitDone);
    }

    private void OnSubmitDone(bool success, string message)
    {
        submitButton.interactable = true;
        SetStatus(message);

        if (success)
        {
            // 清空表单并淡出收起
            titleInput.text = "";
            descriptionInput.text = "";
            priorityDropdown.value = 1;
            StopAllCoroutines();
            StartCoroutine(FadeOutForm());
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    /// <summary>手动关闭表单，不清空标记（允许重新放置）</summary>
    public void CloseForm()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutForm(() => SetStatus("已关闭表单，点击平面重新放置标记")));
    }
}
