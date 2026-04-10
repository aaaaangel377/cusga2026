using UnityEngine;
using UnityEngine.UI;

public class HintSystemUI : MonoBehaviour
{
    private const int ExpectedHintCount = 3;

    [Header("Hints")]
    [TextArea(2, 5)]
    [SerializeField] private string firstHint = string.Empty;
    [TextArea(2, 5)]
    [SerializeField] private string secondHint = string.Empty;
    [TextArea(2, 5)]
    [SerializeField] private string thirdHint = string.Empty;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text hintText;
    [SerializeField] private Button mainHintButton;
    [SerializeField] private Button[] hintButtons = new Button[ExpectedHintCount];

    [Header("State")]
    [SerializeField] private bool hidePanelOnStart = true;
    [SerializeField] private bool autoShowFirstHintWhenOpened;
    [SerializeField] private string emptyHintFallbackText = string.Empty;

    private bool[] _viewedHints = new bool[ExpectedHintCount];
    private int _unlockedHintCount = 1;
    private int _currentHintIndex = -1;

    void Awake()
    {
        ResolveReferences();
        BindHintButtons();
        BindControlButtons();
        ApplyStateToUI();
    }

    void Start()
    {
        if (panelRoot != null && hidePanelOnStart)
        {
            panelRoot.SetActive(false);
        }
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveReferences();
        }
    }

    public void TogglePanel()
    {
        if (panelRoot == null)
        {
            return;
        }

        bool isOpening = !panelRoot.activeSelf;
        panelRoot.SetActive(isOpening);

        if (isOpening && autoShowFirstHintWhenOpened && _currentHintIndex < 0)
        {
            RequestHint(0);
        }
    }

    public void RequestFirstHint()
    {
        RequestHint(0);
    }

    public void RequestSecondHint()
    {
        RequestHint(1);
    }

    public void RequestThirdHint()
    {
        RequestHint(2);
    }

    public void RequestHint(int hintIndex)
    {
        if (hintIndex < 0 || hintIndex >= ExpectedHintCount)
        {
            return;
        }

        if (hintIndex >= _unlockedHintCount)
        {
            return;
        }

        _currentHintIndex = hintIndex;

        if (!_viewedHints[hintIndex])
        {
            _viewedHints[hintIndex] = true;
            _unlockedHintCount = Mathf.Max(_unlockedHintCount, Mathf.Min(hintIndex + 2, ExpectedHintCount));
        }

        UpdateHintText();
        ApplyStateToUI();
    }

    public void ResetProgress()
    {
        _viewedHints = new bool[ExpectedHintCount];
        _unlockedHintCount = 1;
        _currentHintIndex = -1;
        UpdateHintText();
        ApplyStateToUI();
    }

    public int GetUnlockedHintCount()
    {
        return _unlockedHintCount;
    }

    public bool HasViewedHint(int index)
    {
        return index >= 0 && index < _viewedHints.Length && _viewedHints[index];
    }

    private string GetHintText(int hintIndex)
    {
        switch (hintIndex)
        {
            case 0:
                return firstHint;
            case 1:
                return secondHint;
            case 2:
                return thirdHint;
            default:
                return string.Empty;
        }
    }

    private void BindHintButtons()
    {
        for (int i = 0; i < hintButtons.Length; i++)
        {
            Button button = hintButtons[i];
            if (button == null || button.onClick.GetPersistentEventCount() > 0)
            {
                continue;
            }

            switch (i)
            {
                case 0:
                    button.onClick.RemoveListener(RequestFirstHint);
                    button.onClick.AddListener(RequestFirstHint);
                    break;
                case 1:
                    button.onClick.RemoveListener(RequestSecondHint);
                    button.onClick.AddListener(RequestSecondHint);
                    break;
                case 2:
                    button.onClick.RemoveListener(RequestThirdHint);
                    button.onClick.AddListener(RequestThirdHint);
                    break;
            }
        }
    }

    private void BindControlButtons()
    {
        if (mainHintButton != null && mainHintButton.onClick.GetPersistentEventCount() == 0)
        {
            mainHintButton.onClick.RemoveListener(TogglePanel);
            mainHintButton.onClick.AddListener(TogglePanel);
        }
    }

    private void UpdateHintText()
    {
        if (hintText == null)
        {
            return;
        }

        if (_currentHintIndex < 0)
        {
            hintText.text = emptyHintFallbackText;
            return;
        }

        hintText.text = GetHintText(_currentHintIndex);
    }

    private void ApplyStateToUI()
    {
        for (int i = 0; i < hintButtons.Length; i++)
        {
            bool isUnlocked = i < _unlockedHintCount;

            if (hintButtons[i] != null)
            {
                hintButtons[i].interactable = isUnlocked;
            }
        }
    }

    private void ResolveReferences()
    {
        EnsureHintButtonArraySize();

        if (panelRoot == null)
        {
            Transform panelTransform = FindDescendantByName(transform, "HintPanel");
            if (panelTransform != null)
            {
                panelRoot = panelTransform.gameObject;
            }
        }

        if (hintText == null)
        {
            Transform hintTextTransform = FindDescendantByName(transform, "HintText");
            if (hintTextTransform != null)
            {
                hintText = hintTextTransform.GetComponent<Text>();
            }
        }

        mainHintButton = ResolveButton(mainHintButton, "MainHintButton");

        hintButtons[0] = ResolveButton(hintButtons[0], "Hint1Button", "Hint1button");
        hintButtons[1] = ResolveButton(hintButtons[1], "Hint2Button", "Hint2button");
        hintButtons[2] = ResolveButton(hintButtons[2], "Hint3Button", "Hint3button");

    }

    private void EnsureHintButtonArraySize()
    {
        if (hintButtons == null || hintButtons.Length != ExpectedHintCount)
        {
            System.Array.Resize(ref hintButtons, ExpectedHintCount);
        }

    }

    private Button ResolveButton(Button currentButton, params string[] objectNames)
    {
        if (currentButton != null)
        {
            return currentButton;
        }

        for (int i = 0; i < objectNames.Length; i++)
        {
            Transform target = FindDescendantByName(transform, objectNames[i]);
            if (target != null)
            {
                return target.GetComponent<Button>();
            }
        }

        return null;
    }


    private Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDescendantByName(root.GetChild(i), targetName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
