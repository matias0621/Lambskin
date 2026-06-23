using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    private TMP_InputField _roomInput;
    private TextMeshProUGUI _statusLabel;

    private void Awake()
    {
        BuildInterface();
    }

    private void Start()
    {
        if (!string.IsNullOrWhiteSpace(NetworkSessionRequest.LastStatus))
        {
            _statusLabel.text = NetworkSessionRequest.LastStatus;
        }
    }

    private void BuildInterface()
    {
        EnsureEventSystem();

        Canvas canvas = CreateCanvas("MainMenuCanvas");
        RectTransform root = CreatePanel(canvas.transform, "Root", new Color(0.04f, 0.045f, 0.055f, 1f));
        Stretch(root);

        RectTransform content = CreateRect("Content", root);
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(520f, 420f);
        content.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        TextMeshProUGUI title = CreateText("Lambskin", content, 48, FontStyles.Bold, TextAlignmentOptions.Center);
        title.color = new Color(0.96f, 0.92f, 0.84f, 1f);
        SetLayout(title.gameObject, 520f, 70f);

        TextMeshProUGUI subtitle = CreateText("Partidas online", content, 21, FontStyles.Normal, TextAlignmentOptions.Center);
        subtitle.color = new Color(0.75f, 0.82f, 0.86f, 1f);
        SetLayout(subtitle.gameObject, 520f, 38f);

        _roomInput = CreateInput(content);
        _roomInput.text = GenerateRoomCode();

        Button createButton = CreateButton("Crear partida", content, new Color(0.63f, 0.15f, 0.18f, 1f));
        createButton.onClick.AddListener(CreateSession);

        Button joinButton = CreateButton("Unirse a partida", content, new Color(0.12f, 0.36f, 0.44f, 1f));
        joinButton.onClick.AddListener(JoinSession);

        _statusLabel = CreateText(string.Empty, content, 18, FontStyles.Normal, TextAlignmentOptions.Center);
        _statusLabel.color = new Color(0.9f, 0.86f, 0.76f, 1f);
        SetLayout(_statusLabel.gameObject, 520f, 58f);
    }

    private void CreateSession()
    {
        string roomName = NetworkSessionRequest.NormalizeSessionName(_roomInput.text);
        NetworkSessionRequest.Set(GameMode.Host, roomName);
        _statusLabel.text = $"Creando sala {roomName}...";
        SceneManager.LoadScene(NetworkSessionRequest.GameSceneIndex);
    }

    private void JoinSession()
    {
        string roomName = NetworkSessionRequest.NormalizeSessionName(_roomInput.text);
        if (string.IsNullOrWhiteSpace(roomName))
        {
            _statusLabel.text = "Escribe el codigo de sala.";
            return;
        }

        NetworkSessionRequest.Set(GameMode.Client, roomName);
        _statusLabel.text = $"Uniendose a {roomName}...";
        SceneManager.LoadScene(NetworkSessionRequest.GameSceneIndex);
    }

    private string GenerateRoomCode()
    {
        return $"LS{Random.Range(1000, 9999)}";
    }

    private Canvas CreateCanvas(string objectName)
    {
        GameObject canvasObject = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private TMP_InputField CreateInput(Transform parent)
    {
        RectTransform wrapper = CreatePanel(parent, "RoomInput", new Color(0.12f, 0.13f, 0.15f, 1f));
        SetLayout(wrapper.gameObject, 520f, 62f);

        TMP_InputField input = wrapper.gameObject.AddComponent<TMP_InputField>();
        Image image = wrapper.GetComponent<Image>();
        image.color = new Color(0.12f, 0.13f, 0.15f, 1f);

        TextMeshProUGUI text = CreateText(string.Empty, wrapper, 24, FontStyles.Bold, TextAlignmentOptions.Center);
        text.color = Color.white;
        Stretch(text.rectTransform, 18f);

        TextMeshProUGUI placeholder = CreateText("Codigo de sala", wrapper, 22, FontStyles.Normal, TextAlignmentOptions.Center);
        placeholder.color = new Color(0.55f, 0.58f, 0.62f, 1f);
        Stretch(placeholder.rectTransform, 18f);

        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 20;
        input.contentType = TMP_InputField.ContentType.Alphanumeric;
        input.lineType = TMP_InputField.LineType.SingleLine;

        return input;
    }

    private Button CreateButton(string label, Transform parent, Color color)
    {
        RectTransform buttonRect = CreatePanel(parent, label, color);
        SetLayout(buttonRect.gameObject, 520f, 58f);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.24f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TextMeshProUGUI text = CreateText(label, buttonRect, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        text.color = Color.white;
        Stretch(text.rectTransform);

        return button;
    }

    private RectTransform CreatePanel(Transform parent, string objectName, Color color)
    {
        RectTransform rect = CreateRect(objectName, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private TextMeshProUGUI CreateText(string text, Transform parent, int size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private void Stretch(RectTransform rectTransform, float padding = 0f)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(padding, padding);
        rectTransform.offsetMax = new Vector2(-padding, -padding);
    }

    private void SetLayout(GameObject target, float preferredWidth, float preferredHeight)
    {
        LayoutElement element = target.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = target.AddComponent<LayoutElement>();
        }

        element.preferredWidth = preferredWidth;
        element.preferredHeight = preferredHeight;
    }
}
