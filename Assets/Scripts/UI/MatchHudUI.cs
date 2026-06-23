using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchHudUI : MonoBehaviour
{
    private TextMeshProUGUI _statusLabel;
    private TextMeshProUGUI _timerLabel;
    private TextMeshProUGUI _roleLabel;
    private TextMeshProUGUI _playersLabel;
    private Button _startButton;
    private Button _menuButton;
    private RectTransform _winnerPanel;
    private TextMeshProUGUI _winnerLabel;

    public static MatchHudUI EnsureExists()
    {
        MatchHudUI existing = FindAnyObjectByType<MatchHudUI>();
        if (existing != null)
        {
            return existing;
        }

        GameObject hudObject = new GameObject("MatchHudUI");
        return hudObject.AddComponent<MatchHudUI>();
    }

    private void Awake()
    {
        BuildInterface();
    }

    private void Update()
    {
        NetworkMatchController controller = NetworkMatchController.Instance;
        if (controller == null)
        {
            _statusLabel.text = "Conectando...";
            return;
        }

        List<PlayerMovement> players = controller.GetPlayersSnapshot(false);
        PlayerMovement localPlayer = controller.GetLocalPlayer();
        NetworkMatchPhase phase = controller.GetPhase();

        _playersLabel.text = $"Jugadores {players.Count}/{NetworkSessionRequest.MaxPlayers}";
        _startButton.gameObject.SetActive(controller.ActiveRunner != null && controller.ActiveRunner.IsServer && phase == NetworkMatchPhase.Waiting);
        _startButton.interactable = controller.CanHostStart();

        if (phase == NetworkMatchPhase.Waiting)
        {
            _statusLabel.text = players.Count < 2 ? "Esperando jugadores" : "Listo para iniciar";
            _timerLabel.text = "--";
            _roleLabel.text = "Sala";
            _winnerPanel.gameObject.SetActive(false);
            return;
        }

        if (phase == NetworkMatchPhase.Playing)
        {
            PlayerMovement human = controller.GetCurrentHuman();
            _statusLabel.text = human != null ? $"Humano: {human.GetDisplayName()}" : "Buscando humano";
            _timerLabel.text = Mathf.CeilToInt(controller.GetCurrentHumanTime()).ToString();
            _roleLabel.text = GetRoleText(localPlayer);
            _winnerPanel.gameObject.SetActive(false);
            return;
        }

        PlayerMovement winner = controller.GetWinner();
        _statusLabel.text = "Partida terminada";
        _timerLabel.text = "--";
        _roleLabel.text = GetRoleText(localPlayer);
        _winnerLabel.text = winner != null ? $"Ganador: {winner.GetDisplayName()}" : "Sin ganador";
        _winnerPanel.gameObject.SetActive(true);
    }

    private string GetRoleText(PlayerMovement localPlayer)
    {
        if (localPlayer == null)
        {
            return "Espectador";
        }

        if (localPlayer.IsEliminated)
        {
            return "Eliminado";
        }

        return localPlayer.IsHuman ? "Humano" : "Monstruo";
    }

    private void StartMatch()
    {
        if (NetworkMatchController.Instance != null)
        {
            NetworkMatchController.Instance.StartMatchByHost();
        }
    }

    private void ReturnToMenu()
    {
        if (NetworkSessionManager.Instance != null)
        {
            NetworkSessionManager.Instance.ReturnToMenu();
            return;
        }

        NetworkRunner runner = FindAnyObjectByType<NetworkRunner>();
        if (runner != null)
        {
            runner.Shutdown();
        }

        SceneManager.LoadScene(NetworkSessionRequest.MainMenuSceneIndex);
    }

    private void BuildInterface()
    {
        EnsureEventSystem();

        Canvas canvas = CreateCanvas("MatchHudCanvas");

        RectTransform topBar = CreatePanel(canvas.transform, "TopBar", new Color(0.03f, 0.035f, 0.04f, 0.86f));
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(0f, 82f);
        topBar.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup topLayout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        topLayout.padding = new RectOffset(24, 24, 12, 12);
        topLayout.spacing = 18f;
        topLayout.childControlHeight = true;
        topLayout.childControlWidth = true;
        topLayout.childForceExpandHeight = true;
        topLayout.childForceExpandWidth = false;
        topLayout.childAlignment = TextAnchor.MiddleCenter;

        _playersLabel = CreateText("Jugadores 0/4", topBar, 20, FontStyles.Bold, TextAlignmentOptions.Left);
        SetLayout(_playersLabel.gameObject, 220f, 56f);

        _statusLabel = CreateText("Conectando...", topBar, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        SetLayout(_statusLabel.gameObject, 420f, 56f);

        _timerLabel = CreateText("--", topBar, 36, FontStyles.Bold, TextAlignmentOptions.Center);
        _timerLabel.color = new Color(1f, 0.84f, 0.45f, 1f);
        SetLayout(_timerLabel.gameObject, 120f, 56f);

        _roleLabel = CreateText("Sala", topBar, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        SetLayout(_roleLabel.gameObject, 180f, 56f);

        _startButton = CreateButton("Iniciar", topBar, new Color(0.62f, 0.13f, 0.16f, 1f));
        _startButton.onClick.AddListener(StartMatch);
        SetLayout(_startButton.gameObject, 150f, 52f);

        _menuButton = CreateButton("Menu", topBar, new Color(0.12f, 0.33f, 0.38f, 1f));
        _menuButton.onClick.AddListener(ReturnToMenu);
        SetLayout(_menuButton.gameObject, 130f, 52f);

        _winnerPanel = CreatePanel(canvas.transform, "WinnerPanel", new Color(0.04f, 0.045f, 0.055f, 0.94f));
        _winnerPanel.anchorMin = new Vector2(0.5f, 0.5f);
        _winnerPanel.anchorMax = new Vector2(0.5f, 0.5f);
        _winnerPanel.pivot = new Vector2(0.5f, 0.5f);
        _winnerPanel.sizeDelta = new Vector2(520f, 240f);
        _winnerPanel.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup winnerLayout = _winnerPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        winnerLayout.padding = new RectOffset(24, 24, 24, 24);
        winnerLayout.spacing = 18f;
        winnerLayout.childAlignment = TextAnchor.MiddleCenter;
        winnerLayout.childControlHeight = false;
        winnerLayout.childControlWidth = true;

        _winnerLabel = CreateText("Ganador", _winnerPanel, 34, FontStyles.Bold, TextAlignmentOptions.Center);
        _winnerLabel.color = new Color(0.96f, 0.92f, 0.84f, 1f);
        SetLayout(_winnerLabel.gameObject, 460f, 90f);

        Button winnerMenuButton = CreateButton("Volver al menu", _winnerPanel, new Color(0.12f, 0.36f, 0.44f, 1f));
        winnerMenuButton.onClick.AddListener(ReturnToMenu);
        SetLayout(winnerMenuButton.gameObject, 360f, 58f);

        _winnerPanel.gameObject.SetActive(false);
    }

    private Canvas CreateCanvas(string objectName)
    {
        GameObject canvasObject = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private Button CreateButton(string label, Transform parent, Color color)
    {
        RectTransform rect = CreatePanel(parent, label, color);
        Button button = rect.gameObject.AddComponent<Button>();

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.25f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TextMeshProUGUI text = CreateText(label, rect, 20, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);

        return button;
    }

    private RectTransform CreatePanel(Transform parent, string objectName, Color color)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        return panelObject.GetComponent<RectTransform>();
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
        label.color = Color.white;
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

    private void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
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
