using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using System;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;
#if false

namespace MissionPlanner
{

    public class ExpandableListUI : MonoBehaviour
    {
        [System.Serializable]
        public class ListItem
        {
            public string title;
            public List<ListItem> children = new List<ListItem>();
        }

        [Header("UI References")]
        public Transform contentParent; // The parent transform under which top-level items will be instantiated
        public GameObject itemPrefab; // Prefab for any level item: should have ExpandButton (Button with Image for +/-, positioned left), TitleButton (Button with Text child for title, positioned right), and a child Transform "SubContent" for sub-items
        public GameObject textWindowPrefab; // Prefab for the text window: should have a Canvas, Panel, and Text component to display the item's title

        [Header("Data")]
        public List<ListItem> items = new List<ListItem>();

        [Header("Sprites")]
        public Sprite plusSprite;
        public Sprite minusSprite;

        private bool isPopulated = false; // Track if list has been populated

        private void Start()
        {
            // Defer population until window is shown
        }

        public void PopulateList()
        {
            if (isPopulated) return; // Avoid repopulating

            if (contentParent == null || itemPrefab == null)
            {
                UnityEngine.Debug.LogError("Missing UI references!");
                return;
            }

            ClearContent();

            foreach (var item in items)
            {
                CreateItem(item, contentParent);
            }

            isPopulated = true;
        }

        private void ClearContent()
        {
            if (contentParent == null) return;
            foreach (Transform child in contentParent)
            {
                DestroyImmediate(child.gameObject); // Immediate for editor safety
            }
        }

        private void CreateItem(ListItem data, Transform parent)
        {
            GameObject itemGO = Instantiate(itemPrefab, parent);
            var itemComponent = itemGO.GetComponent<ExpandableItem>();
            if (itemComponent == null)
            {
                itemComponent = itemGO.AddComponent<ExpandableItem>();
            }
            itemComponent.Initialize(data, plusSprite, minusSprite, textWindowPrefab, this); // Pass self for recursive creation
        }

        // Call this to refresh the list if data changes
        public void RefreshList()
        {
            isPopulated = false;
            PopulateList();
        }
    }
}

// Separate script for window management and toolbar
public class KerbalListWindow : MonoBehaviour
{
    [Header("Window UI")]
    [SerializeField] private RectTransform windowRect; // Assign the main window panel's RectTransform (e.g., the draggable/closable panel containing the ScrollView/Content)
    [SerializeField] private ExpandableListUI listManager; // Reference to the ExpandableListUI component (on the same GO or child)

    [Header("Toolbar Settings")]
    [SerializeField] private string modNamespace = "YourModName"; // e.g., "com.yourname.kerballist"
    [SerializeField] private string buttonId = "kerbalListButton";
    [SerializeField] private ApplicationLauncher.AppScenes visibleInScenes = ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;

    [Header("Toolbar Textures (assign in Inspector or load from assets)")]
    [SerializeField] private Texture2D toolbarTexture;
    [SerializeField] private Texture2D toolbarHighlightedTexture;

    private ApplicationLauncherButton toolbarButton;
    private bool isWindowVisible = false;

    private void Start()
    {
        GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
        GameEvents.onGUIApplicationLauncherUnloadEvent.Add(OnAppLauncherUnload);

        // Start with window hidden
        if (windowRect != null)
        {
            windowRect.gameObject.SetActive(false);
            isWindowVisible = false;
        }
    }

    private void OnDestroy()
    {
        GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLauncherReady);
        GameEvents.onGUIApplicationLauncherUnloadEvent.Remove(OnAppLauncherUnload);

        if (toolbarButton != null)
        {
            ApplicationLauncher.Instance.RemoveApplication(toolbarButton);
            toolbarButton = null;
        }
    }

    private void OnAppLauncherReady()
    {
        if (ApplicationLauncher.Instance == null) return;
        if (toolbarButton != null) return; // Already added

        toolbarButton = ApplicationLauncher.Instance.AddModApplication(
            OnToolbarButtonClick, // onTrue (show)
            OnToolbarButtonClick, // onFalse (hide)
            null, // onHover
            null, // onHoverOut
            null, // onEnable
            null, // onDisable
            visibleInScenes,
            modNamespace,
            buttonId,
            toolbarTexture, // Normal texture
            toolbarHighlightedTexture, // Highlighted texture
            toolbarTexture // ? (often same as normal)
        );

        // Initial state: button shows normal (window closed)
        UpdateButtonTexture();
    }

    private void OnAppLauncherUnload()
    {
        if (toolbarButton != null)
        {
            ApplicationLauncher.Instance.RemoveApplication(toolbarButton);
            toolbarButton = null;
        }
    }

    private void OnToolbarButtonClick()
    {
        ToggleWindow();
    }

    public void ToggleWindow()
    {
        if (windowRect == null) return;

        isWindowVisible = !isWindowVisible;
        windowRect.gameObject.SetActive(isWindowVisible);

        if (isWindowVisible && listManager != null && !listManager.isPopulated)
        {
            listManager.PopulateList(); // Populate on first show
        }

        UpdateButtonTexture();
    }

    private void UpdateButtonTexture()
    {
        if (toolbarButton == null) return;

        // Optional: Swap textures based on state (e.g., open/closed icon)
        // For simplicity, use highlighted when open, normal when closed
        Texture2D currentTexture = isWindowVisible ? toolbarHighlightedTexture : toolbarTexture;
        if (currentTexture != null)
        {
            toolbarButton.SetTexture(currentTexture);
        }
    }
}

public class ExpandableItem : MonoBehaviour, IPointerClickHandler
{
    [Header("Components")]
    [SerializeField] private Button expandButton;
    [SerializeField] private Button titleButton; // Button wrapping the title Text
    [SerializeField] private Text titleText;
    [SerializeField] private Transform subContentParent; // Child transform where sub-items go

    private ListItem data;
    private Sprite plusSprite;
    private Sprite minusSprite;
    private GameObject textWindowPrefab;
    private ExpandableListUI listManager; // For recursive creation
    private bool isExpanded = false;
    private float lastClickTime = 0f;
    private const float doubleClickTime = 0.3f; // Time window for double-click detection

    public void Initialize(ListItem itemData, Sprite plus, Sprite minus, GameObject textWindow, ExpandableListUI manager)
    {
        data = itemData;
        titleText.text = data.title;
        plusSprite = plus;
        minusSprite = minus;
        textWindowPrefab = textWindow;
        listManager = manager;

        // Initially collapsed
        if (subContentParent != null)
        {
            subContentParent.gameObject.SetActive(false);
        }

        // Set initial button sprite
        SetExpandIcon(false);

        // Add listener to the expand button (single click to toggle expand)
        if (expandButton != null)
        {
            expandButton.onClick.RemoveAllListeners(); // Clear any existing
            expandButton.onClick.AddListener(ToggleExpand);
        }

        // The titleButton will handle clicks via IPointerClickHandler for double-click detection
        // Ensure titleButton exists (fallback if not assigned)
        if (titleButton == null)
        {
            titleButton = GetComponent<Button>();
            if (titleButton == null)
            {
                titleButton = gameObject.AddComponent<Button>();
                var graphic = titleText?.GetComponent<Graphic>();
                if (graphic != null) titleButton.targetGraphic = graphic;
            }
        }

        // Populate sub-items but keep inactive
        PopulateSubItems();
    }

    private void PopulateSubItems()
    {
        if (subContentParent == null || data.children == null || listManager == null) return;

        // Clear existing sub-items
        foreach (Transform child in subContentParent)
        {
            DestroyImmediate(child.gameObject); // Immediate for UI consistency
        }

        // Create recursive sub-items
        foreach (var childData in data.children)
        {
            listManager.CreateItem(childData, subContentParent);
        }
    }

    private void SetExpandIcon(bool expanded)
    {
        if (expandButton != null && expandButton.GetComponent<Image>() != null)
        {
            expandButton.GetComponent<Image>().sprite = expanded ? minusSprite : plusSprite;
        }
    }

    public void ToggleExpand()
    {
        isExpanded = !isExpanded;

        if (subContentParent != null)
        {
            subContentParent.gameObject.SetActive(isExpanded);
        }

        SetExpandIcon(isExpanded);
    }

    // IPointerClickHandler for title clicks (detects double-click)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        float currentTime = Time.unscaledTime;
        if (currentTime - lastClickTime <= doubleClickTime)
        {
            // Double-click detected: Open text window
            OpenTextWindow();
        }
        lastClickTime = currentTime;
    }

    private void OpenTextWindow()
    {
        if (textWindowPrefab == null) return;

        GameObject windowGO = Instantiate(textWindowPrefab, (transform.root as RectTransform)); // Instantiate under root Canvas/RectTransform
        Text windowText = windowGO.GetComponentInChildren<Text>();
        if (windowText != null)
        {
            windowText.text = data.title; // Display the item's title in the window
        }

        // Optional: Add a close button logic here, e.g., find a Button in the prefab and add onClick to Destroy(windowGO)
        Button closeButton = windowGO.GetComponentInChildren<Button>();
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => Destroy(windowGO));
        }
    }
}
#endif