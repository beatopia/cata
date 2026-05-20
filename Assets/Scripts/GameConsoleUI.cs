using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(GameConsole))]
public class GameConsoleUI : MonoBehaviour
{
    private void Start()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("ConsoleCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create Panel
        GameObject panelObj = new GameObject("ConsolePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panel = panelObj.AddComponent<Image>();
        panel.color = new Color(0, 0, 0, 0.8f);
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.75f, 0);
        panelRect.anchorMax = new Vector2(1f, 0.15f);
        panelRect.offsetMin = new Vector2(10, 10);
        panelRect.offsetMax = new Vector2(-10, -10);

        // Create ScrollView
        GameObject scrollViewObj = new GameObject("ScrollView");
        scrollViewObj.transform.SetParent(panelObj.transform, false);
        ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        
        RectTransform scrollRectTransform = scrollViewObj.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(5, 5);
        scrollRectTransform.offsetMax = new Vector2(-5, -5);

        // Create Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);
        Image viewport = viewportObj.AddComponent<Image>();
        viewport.color = new Color(0, 0, 0, 0.5f);
        
        RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        // Create Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);
        contentRect.anchoredPosition = Vector2.zero;

        // Create Text
        GameObject textObj = new GameObject("ConsoleText");
        textObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI consoleText = textObj.AddComponent<TextMeshProUGUI>();
        consoleText.color = Color.white;
        consoleText.fontSize = 12;
        consoleText.alignment = TextAlignmentOptions.TopLeft;
        consoleText.textWrappingMode = TextWrappingModes.Normal;
        consoleText.overflowMode = TextOverflowModes.Overflow;
        consoleText.autoSizeTextContainer = true;
        consoleText.extraPadding = true;
        
        // Add Content Size Fitter to Text
        ContentSizeFitter textSizeFitter = textObj.AddComponent<ContentSizeFitter>();
        textSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        textSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5, 5);
        textRect.offsetMax = new Vector2(-5, -5);

        // Setup ScrollRect
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 20;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.elasticity = 0.1f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;

        // Add Layout Group to content
        VerticalLayoutGroup layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.spacing = 2;
        layoutGroup.padding = new RectOffset(5, 5, 5, 5);

        // Add Content Size Fitter
        ContentSizeFitter sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Setup GameConsole component
        GameConsole gameConsole = GetComponent<GameConsole>();
        gameConsole.consoleText = consoleText;
        gameConsole.scrollRect = scrollRect;

        // Add a test message
        gameConsole.AddMessage("Console initialized!");
    }
}