using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using UITK = UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager : MonoBehaviour
    {
        private VisualElement _configPanel;
        private VisualElement _overlayBackground;
        private UIDocument _uiDocument;
        private bool _isUIVisible = false;
        private CustomBroadcastScoreboard _scoreboardReference;
        private ScoreboardConfig _config;
        private PresetsConfig _presetsConfig;
        private bool _inputsWereDisabled = false;
        
        // Menu button hiding
        private List<UITK.VisualElement> _hiddenMenuElements = new List<UITK.VisualElement>();
        private static FieldInfo _fiMainSettings;
        private static FieldInfo _fiPauseSettings;
        
        // Preset row management
        private class PresetRow 
        { 
            public UITK.TextField nameField; 
            public UITK.VisualElement row; 
            public int presetIndex;
            public bool isBluePreset;
        }
        private readonly List<PresetRow> _presetRows = new List<PresetRow>();
        private UITK.VisualElement _presetRowsRoot;
        
        // Size preset row management
        private class SizePresetRow 
        { 
            public UITK.TextField nameField; 
            public UITK.VisualElement row; 
            public int presetIndex;
        }
        private readonly List<SizePresetRow> _sizePresetRows = new List<SizePresetRow>();
        private UITK.VisualElement _sizePresetRowsRoot;
        
        // Scroll view for preserving position
        private ScrollView _mainScrollView;
        private UITK.VisualElement _teamPresetsContainer;
        private UITK.VisualElement _sizePresetsContainer;

        // Search/filter: a persistent box above the scroll view that hides rows on
        // the active tab whose label does not contain the query (matches CompAdjust).
        private UITK.TextField _searchField;
        private string _searchQuery = "";

        // PoncePlayerInput styling constants for better readability
        private static readonly Color32 TextFieldBg = new Color32(57, 57, 57, 255);
        private static readonly Color32 RowBg = new Color32(61, 61, 61, 255);
        private static readonly Color32 ButtonBg = new Color32(57, 57, 57, 255);
        private static readonly Color32 PanelBg = new Color32(48, 48, 47, 255);
        private static readonly Color32 TabActiveBg = new Color32(80, 80, 80, 255);
        private static readonly Color32 TabInactiveBg = new Color32(66, 66, 66, 255);
        private static readonly Color P_White = new Color(0.93f, 0.93f, 0.93f, 1f);
        private static readonly Color BtnBrightGray = (Color)RowBg;
        private static Font _uiTextFont;
        
        // Tab system
        private enum ScoreboardTab { General, Presets, Advanced, Tests }
        private ScoreboardTab _activeTab = ScoreboardTab.General;
        private UITK.Button _tabGeneral, _tabPresets, _tabAdvanced, _tabTests;
        private UITK.VisualElement _generalContent, _presetsContent, _advancedContent, _testsContent;

        private static Font GetUIFont()
        {
            if (_uiTextFont != null) return _uiTextFont;

            // Try to get the font from the game's PanelSettings (matches base game font exactly)
            try
            {
                var uiManager = MonoBehaviourSingleton<UIManager>.Instance;
                if (uiManager != null && uiManager.PanelSettings != null)
                {
                    var textSettings = uiManager.PanelSettings.textSettings;
                    if (textSettings != null && textSettings.defaultFontAsset != null)
                    {
                        _uiTextFont = textSettings.defaultFontAsset.sourceFontFile;
                        if (_uiTextFont != null) return _uiTextFont;
                    }
                }
            }
            catch { }

            // Fallback to Arial
            try { _uiTextFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            if (_uiTextFont == null)
            {
                try
                {
                    _uiTextFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Arial", "Helvetica Neue", "Segoe UI", "Liberation Sans", "Noto Sans" }, 16);
                }
                catch { }
            }
            return _uiTextFont;
        }

        private static void ForceUIFont(UITK.VisualElement ve)
        {
            var f = GetUIFont(); if (f == null) return;
            ve.style.unityFont = f;
        }

        private static void MakeReadable(UITK.TextField tf)
        {
            tf.style.color = Color.white;
            tf.style.unityFont = GetUIFont();

            // make the field itself the swatch #3 color
            tf.style.backgroundColor = new UITK.StyleColor(TextFieldBg);

            var input = tf.childCount > 0 ? tf.ElementAt(0) : null;
            if (input != null)
            {
                input.style.color = Color.white;
                input.style.unityFont = GetUIFont();
                input.style.backgroundColor = new UITK.StyleColor(TextFieldBg);
            }
        }

        private static void MakeReadable(UITK.Label l)
        {
            l.style.color = Color.white;
            l.style.unityFont = GetUIFont();
        }

        // Professional slider styling to match PoncePlayerInput
        private static void StyleSliderLikeBase(UITK.Slider s)
        {
            s.style.height = 26;
            s.style.marginLeft = 6;
            s.style.marginRight = 6;

            // Cover legacy, current, and base-slider USS class names - Unity
            // renames these between versions and only one will match per build.
            var tracker = s.Q<VisualElement>(className: "unity-base-slider__tracker")
                       ?? s.Q<VisualElement>(className: "unity-slider__tracker")
                       ?? s.Q<VisualElement>(className: "unity-tracker");
            var dragger = s.Q<VisualElement>(className: "unity-base-slider__dragger")
                       ?? s.Q<VisualElement>(className: "unity-slider__dragger")
                       ?? s.Q<VisualElement>(className: "unity-dragger");
            if (tracker != null)
            {
                tracker.style.height = 4;
                tracker.style.marginTop = 11; // vertically center the 4px rail in 26px
                tracker.style.backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 0.35f));
                tracker.style.borderTopLeftRadius = 2;
                tracker.style.borderTopRightRadius = 2;
                tracker.style.borderBottomLeftRadius = 2;
                tracker.style.borderBottomRightRadius = 2;
            }
            if (dragger != null)
            {
                dragger.style.width = 18;
                dragger.style.height = 18;
                dragger.style.marginTop = 4; // center on the rail
                dragger.style.backgroundColor = new StyleColor(Color.white);
                dragger.style.borderTopLeftRadius = 9;
                dragger.style.borderTopRightRadius = 9;
                dragger.style.borderBottomLeftRadius = 9;
                dragger.style.borderBottomRightRadius = 9;
            }
        }

        // Control builder methods moved to ScoreboardUIManager.Controls.cs

        // Event hooks
        private bool _eventsHooked = false;

        private void Start()
        {
            // Ensure logos folder exists and copy default logos if needed
            EnsureDefaultLogosExist();
            
            // Load the current config
            _config = LoadScoreboardConfig();
            _presetsConfig = LoadPresetsConfig();
            
            // Scan workshop mods for additional presets
            ScanWorkshopForPresets();

            // Hook events for client connect/disconnect
            HookEvents();
        }

        private void HookEvents()
        {
            if (_eventsHooked) return;
            try
            {
                EventManager.AddEventListener("Event_Everyone_OnClientConnected", new Action<Dictionary<string, object>>(OnClientConnected));
                EventManager.AddEventListener("Event_OnDisconnected", new Action<Dictionary<string, object>>(OnClientDisconnected));
                EventManager.AddEventListener("Event_OnMainMenuShow", new Action<Dictionary<string, object>>(OnMainMenuShow));
                _eventsHooked = true;
                Debug.Log("[Scoreboard] Event listeners hooked");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Scoreboard] Failed to hook events: {e.Message}");
            }
        }

        private void UnhookEvents()
        {
            if (!_eventsHooked) return;
            try
            {
                EventManager.RemoveEventListener("Event_Everyone_OnClientConnected", new Action<Dictionary<string, object>>(OnClientConnected));
                EventManager.RemoveEventListener("Event_OnDisconnected", new Action<Dictionary<string, object>>(OnClientDisconnected));
                EventManager.RemoveEventListener("Event_OnMainMenuShow", new Action<Dictionary<string, object>>(OnMainMenuShow));
                _eventsHooked = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Scoreboard] UnhookEvents failed: {e.Message}");
            }
        }

        private void OnClientConnected(Dictionary<string, object> message)
        {
            try
            {
                // Reload config when connecting to a server
                _config = LoadScoreboardConfig();
                _presetsConfig = LoadPresetsConfig();
                ScanWorkshopForPresets();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Scoreboard] OnClientConnected error: {e.Message}");
            }
        }

        private void OnClientDisconnected(Dictionary<string, object> message)
        {
            try
            {
                // Hide UI when disconnecting
                if (_isUIVisible)
                {
                    HideUI();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Scoreboard] OnClientDisconnected HideUI failed: {e.Message}");
            }
        }

        private void OnMainMenuShow(Dictionary<string, object> message)
        {
            try
            {
                // Hide UI when returning to main menu
                if (_isUIVisible)
                {
                    HideUI();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Scoreboard] OnMainMenuShow HideUI failed: {e.Message}");
            }
        }
        
        private void EnsureDefaultLogosExist()
        {
            try
            {
                string targetLogosFolder = ScoreboardPaths.LogosDir;
                Directory.CreateDirectory(targetLogosFolder);
                
                int migratedCount = 0;
                int copiedCount = 0;
                
                // First, try to migrate from old locations
                string[] oldLogoLocations = ScoreboardPaths.GetLegacyLogosLocations();
                
                foreach (string oldLocation in oldLogoLocations)
                {
                    if (Directory.Exists(oldLocation))
                    {
                        var logoFiles = Directory.GetFiles(oldLocation, "*.png");
                        foreach (var logoFile in logoFiles)
                        {
                            string fileName = Path.GetFileName(logoFile);
                            string targetPath = Path.Combine(targetLogosFolder, fileName);
                            
                            // Always copy/overwrite to ensure user's old files are preserved
                            File.Copy(logoFile, targetPath, true);
                            migratedCount++;
                        }
                    }
                }
                
                if (migratedCount > 0)
                {
                    Debug.Log($"[Scoreboard] Migrated {migratedCount} logo files from old locations to {targetLogosFolder}");
                }
                
                // Then, copy default logos from DLL directory if needed
                string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string dllDirectory = Path.GetDirectoryName(dllPath);
                string sourceLogosFolder = Path.Combine(dllDirectory, "scorebuglogos");
                
                if (Directory.Exists(sourceLogosFolder))
                {
                    var logoFiles = Directory.GetFiles(sourceLogosFolder, "*.png");
                    foreach (var logoFile in logoFiles)
                    {
                        string fileName = Path.GetFileName(logoFile);
                        string targetPath = Path.Combine(targetLogosFolder, fileName);
                        
                        // Only copy if doesn't exist
                        if (!File.Exists(targetPath))
                        {
                            File.Copy(logoFile, targetPath, false);
                            copiedCount++;
                        }
                    }
                    
                    if (copiedCount > 0)
                    {
                        Debug.Log($"[Scoreboard] Copied {copiedCount} default logo files to {targetLogosFolder}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Scoreboard] Failed to ensure default logos exist: {e.Message}");
            }
        }

        public void SetScoreboardReference(CustomBroadcastScoreboard scoreboard)
        {
            _scoreboardReference = scoreboard;
        }

        public void ToggleUI()
        {
            if (_isUIVisible)
            {
                HideUI();
            }
            else
            {
                ShowUI();
            }
        }

        private void ShowUI()
        {
            try
            {
                if (_configPanel == null)
                {
                    CreateUI();
                }

                if (_overlayBackground != null)
                {
                    _overlayBackground.style.display = DisplayStyle.Flex;
                    _overlayBackground.pickingMode = PickingMode.Position; // Enable mouse event capture when visible
                    _isUIVisible = true;

                    // Enable mouse cursor
                    UnityEngine.Cursor.lockState = CursorLockMode.None;
                    UnityEngine.Cursor.visible = true;

                    // Disable game inputs and block ESC key
                    DisableGameInputs();
                    
                    // Menu buttons are already hidden by hub, no need to hide them again
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Scoreboard] Failed to show UI: " + e);
            }
        }

        private void Update()
        {
            // Continuously enforce cursor state while menu is visible
            if (_isUIVisible && _overlayBackground != null && _overlayBackground.style.display == DisplayStyle.Flex)
            {
                // Keep cursor unlocked and visible (game tries to re-lock it)
                if (UnityEngine.Cursor.lockState != CursorLockMode.None)
                {
                    UnityEngine.Cursor.lockState = CursorLockMode.None;
                }
                if (!UnityEngine.Cursor.visible)
                {
                    UnityEngine.Cursor.visible = true;
                }
                
                // Handle ESC key to fully close panel
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame)
                {
                    FullCloseUI();
                    return;
                }
            }
        }
        
        /// <summary>
        /// Fully close the UI without returning to hub (ESC behavior).
        /// </summary>
        private void FullCloseUI()
        {
            if (_overlayBackground == null) return;
            
            _overlayBackground.style.display = DisplayStyle.None;
            _overlayBackground.pickingMode = PickingMode.Ignore;
            _isUIVisible = false;
            
            // Re-enable game inputs
            EnableGameInputs();
            
            Debug.Log("[Scoreboard] UI fully closed via ESC");
            
            // Use ModMenuHub's FullClose to handle cursor and menu buttons properly
            try
            {
                PonceMods.Shared.ModMenuHub.FullClose();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Scoreboard] Failed to full close: {e}");
                // Fallback: restore state manually
                HideMenuButtons(false);
            }
        }

        private void HideUI()
        {
            if (_overlayBackground != null)
            {
                _overlayBackground.style.display = DisplayStyle.None;
                _overlayBackground.pickingMode = PickingMode.Ignore; // Disable mouse event capture when hidden
                _isUIVisible = false;

                // Re-enable game inputs
                EnableGameInputs();
                
                // Don't restore menu buttons - hub will manage them
                // Return to ModMenuHub
                try
                {
                    PonceMods.Shared.ModMenuHub.OpenPanel();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Scoreboard] Failed to open hub: {e}");
                    // Fallback: restore state manually
                    HideMenuButtons(false);
                }
            }
        }
        
        private void HideMenuButtons(bool hide)
        {
            if (_fiMainSettings == null)
                _fiMainSettings = typeof(UIMainMenu).GetField("settingsButton", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_fiPauseSettings == null)
                _fiPauseSettings = typeof(UIPauseMenu).GetField("settingsButton", BindingFlags.Instance | BindingFlags.NonPublic);
            
            if (hide)
            {
                _hiddenMenuElements.Clear();
                
                // Hide main menu elements
                var main = MonoBehaviourSingleton<UIMainMenu>.Instance;
                if (main != null && _fiMainSettings != null)
                {
                    var refBtn = _fiMainSettings.GetValue(main) as UITK.Button;
                    if (refBtn?.parent != null)
                    {
                        foreach (var child in refBtn.parent.Children())
                        {
                            if (child.style.display != DisplayStyle.None)
                            {
                                _hiddenMenuElements.Add(child);
                                child.style.display = DisplayStyle.None;
                            }
                        }
                    }
                }
                
                // Also hide pause menu elements
                var pause = MonoBehaviourSingleton<UIPauseMenu>.Instance;
                if (pause != null && _fiPauseSettings != null)
                {
                    var refBtn = _fiPauseSettings.GetValue(pause) as UITK.Button;
                    if (refBtn?.parent != null)
                    {
                        foreach (var child in refBtn.parent.Children())
                        {
                            if (child.style.display != DisplayStyle.None)
                            {
                                _hiddenMenuElements.Add(child);
                                child.style.display = DisplayStyle.None;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var elem in _hiddenMenuElements)
                {
                    if (elem != null)
                        elem.style.display = DisplayStyle.Flex;
                }
                _hiddenMenuElements.Clear();
            }
        }

        private void DisableGameInputs()
        {
            try
            {
                // PlayerInput only exists once you've spawned in. In the main menu there's no
                // PlayerInput, but the chat InputActions still exist — disable them either way.
                var playerInput = UnityEngine.Object.FindFirstObjectByType<UnityEngine.InputSystem.PlayerInput>();
                if (playerInput != null)
                {
                    playerInput.DeactivateInput();
                    _inputsWereDisabled = true;
                }

                InputManager.TalkAction?.Disable();
                InputManager.AllChatAction?.Disable();
                InputManager.TeamChatAction?.Disable();
                InputManager.QuickChat1Action?.Disable();
                InputManager.QuickChat2Action?.Disable();
                InputManager.QuickChat3Action?.Disable();
                InputManager.QuickChat4Action?.Disable();
                InputManager.QuickChat5Action?.Disable();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Scoreboard] Could not disable inputs: " + e.Message);
            }
        }

        private void EnableGameInputs()
        {
            try
            {
                if (_inputsWereDisabled)
                {
                    var playerInput = UnityEngine.Object.FindFirstObjectByType<UnityEngine.InputSystem.PlayerInput>();
                    if (playerInput != null)
                    {
                        playerInput.ActivateInput();
                    }
                    _inputsWereDisabled = false;
                }

                // Re-enable chat actions unconditionally — DisableGameInputs disables them
                // regardless of whether a PlayerInput was found, so we must mirror that here
                // or chat keys stay dead after opening the config in the main menu.
                InputManager.TalkAction?.Enable();
                InputManager.AllChatAction?.Enable();
                InputManager.TeamChatAction?.Enable();
                InputManager.QuickChat1Action?.Enable();
                InputManager.QuickChat2Action?.Enable();
                InputManager.QuickChat3Action?.Enable();
                InputManager.QuickChat4Action?.Enable();
                InputManager.QuickChat5Action?.Enable();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Scoreboard] Could not re-enable inputs: " + e.Message);
            }
        }

        private void CreateUI()
        {
            try
            {
                // Find the UI root - try to get it from UIDocument or create our own
                var root = GetUIRoot();
                if (root == null)
                {
                    Debug.LogError("[Scoreboard] Could not get UI root");
                    return;
                }

                // Create full-screen overlay background (click to close)
                var overlay = new VisualElement();
                overlay.style.position = Position.Absolute;
                overlay.style.width = new Length(100, LengthUnit.Percent);
                overlay.style.height = new Length(100, LengthUnit.Percent);
                overlay.style.left = 0;
                overlay.style.top = 0;
                overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.0f);
                overlay.pickingMode = PickingMode.Position; // Enable click detection
                overlay.RegisterCallback<PointerUpEvent>(_ => HideUI()); // Click backdrop to close
                _overlayBackground = overlay;

                // Create main panel with professional readable styling
                _configPanel = new VisualElement();
                _configPanel.style.position = Position.Absolute;
                int targetW = Mathf.Clamp(Mathf.RoundToInt(Screen.width * 0.58f), 680, 980);
                _configPanel.style.width = targetW;
                _configPanel.style.left = new Length(50, LengthUnit.Percent);
                _configPanel.style.top = new Length(50, LengthUnit.Percent);
                _configPanel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
                _configPanel.style.height = new Length(84, LengthUnit.Percent);
                _configPanel.style.minHeight = new Length(56, LengthUnit.Percent);
                _configPanel.style.maxHeight = new Length(56, LengthUnit.Percent);
                _configPanel.style.overflow = Overflow.Hidden;
                _configPanel.style.flexDirection = FlexDirection.Column;
                _configPanel.style.backgroundColor = new StyleColor(PanelBg);
                _configPanel.style.paddingLeft = 8;
                _configPanel.style.paddingRight = 8;
                _configPanel.style.paddingTop = 8;
                _configPanel.style.paddingBottom = 8;

                // Title
                var title = new Label("SCOREBUG");
                MakeReadable(title);
                title.style.fontSize = 50;
                title.style.unityTextAlign = TextAnchor.MiddleLeft;
                title.style.marginBottom = 16;
                _configPanel.Add(title);

                // Tab bar
                var tabBar = new VisualElement();
                tabBar.style.flexDirection = FlexDirection.Row;
                tabBar.style.marginBottom = 8;
                tabBar.style.height = 50;

                _tabGeneral = MakeTabButton("GENERAL", true, () => SwitchToTab(ScoreboardTab.General));
                _tabPresets = MakeTabButton("PRESETS", false, () => SwitchToTab(ScoreboardTab.Presets));
                _tabAdvanced = MakeTabButton("ADVANCED", false, () => SwitchToTab(ScoreboardTab.Advanced));
                _tabTests = MakeTabButton("TESTDEMOS", false, () => SwitchToTab(ScoreboardTab.Tests));

                tabBar.Add(_tabGeneral);
                tabBar.Add(_tabPresets);
                tabBar.Add(_tabAdvanced);
                tabBar.Add(_tabTests);
                // MakeTabButton gives every tab an 8px right margin for spacing; the
                // last tab must not, or it sits 8px off the right edge while the
                // first tab hugs the left. Zero it so both ends are flush.
                _tabTests.style.marginRight = 0;
                _configPanel.Add(tabBar);

                // Search box: filters the rows on the active tab by label text. It
                // sits above the scroll view so it stays put while the list scrolls.
                // Styled as a row (RowBg + 12px inset) so SEARCH lines up with the
                // row labels below it instead of sitting flush against the panel edge.
                var searchRow = new VisualElement();
                searchRow.style.flexDirection = FlexDirection.Row;
                searchRow.style.alignItems = Align.Center;
                searchRow.style.flexShrink = 0;
                searchRow.style.height = 50;
                searchRow.style.marginBottom = 8;
                searchRow.style.paddingLeft = 12;
                searchRow.style.paddingRight = 12;
                searchRow.style.backgroundColor = new StyleColor(RowBg);

                var searchLabel = new Label("SEARCH");
                searchLabel.style.fontSize = 18;
                searchLabel.style.marginRight = 8;
                MakeReadable(searchLabel);
                searchRow.Add(searchLabel);

                _searchField = new TextField();
                _searchField.value = _searchQuery;
                _searchField.style.flexGrow = 1;
                _searchField.style.height = 34;
                _searchField.style.backgroundColor = new StyleColor(TextFieldBg);
                _searchField.style.color = Color.white;
                ForceUIFont(_searchField);
                _searchField.RegisterValueChangedCallback(e =>
                {
                    _searchQuery = e.newValue ?? "";
                    ApplySearchFilter();
                });
                searchRow.Add(_searchField);
                _configPanel.Add(searchRow);

                // Create scrollable content
                _mainScrollView = new ScrollView();
                _mainScrollView.style.flexGrow = 1;
                // A flex item's default min-height is its content size, so a long
                // list keeps the scroll view tall and pushes the footer past the
                // panel's clipped bottom. Pin min-height to 0 so the scroll view
                // shrinks and the footer stays in (matches CompAdjust).
                _mainScrollView.style.flexShrink = 1;
                _mainScrollView.style.minHeight = 0;
                _mainScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
                _mainScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                _configPanel.Add(_mainScrollView);

                // Create content containers for each tab
                _generalContent = new VisualElement();
                _presetsContent = new VisualElement();
                _advancedContent = new VisualElement();
                _testsContent = new VisualElement();

                _mainScrollView.Add(_generalContent);
                _mainScrollView.Add(_presetsContent);
                _mainScrollView.Add(_advancedContent);
                _mainScrollView.Add(_testsContent);

                // Build content for each tab
                BuildGeneralTab(_generalContent);
                BuildPresetsTab(_presetsContent);
                BuildAdvancedTab(_advancedContent);
                BuildTestsTab(_testsContent);

                // Show initial tab
                SwitchToTab(ScoreboardTab.General);

                // Button row at bottom (CompAdjust layout): COFFEE? alone on the
                // left; EXPORT + CLOSE grouped together on the right. No bottom
                // margins on the buttons - the gap under the row comes from the
                // panel's bottom padding.
                var donateBtn = new Button(() => Application.OpenURL("https://buymeacoffee.com/amikiir")) { text = "COFFEE?" };
                styleBottomButton(donateBtn);

                var exportBtn = new Button(() => ShowExportPackDialog()) { text = "EXPORT FOR WORKSHOP" };
                styleBottomButton(exportBtn);
                exportBtn.style.marginLeft = 8;

                var closeButton = new Button(() => HideUI()) { text = "CLOSE" };
                styleBottomButton(closeButton);
                closeButton.style.marginLeft = 8;
                closeButton.style.paddingRight = 182;

                var buttonRow = new VisualElement();
                buttonRow.style.flexDirection = FlexDirection.Row;
                buttonRow.style.justifyContent = Justify.SpaceBetween;
                buttonRow.style.marginTop = 8;
                buttonRow.style.flexShrink = 0;   // footer keeps its size; the list shrinks instead
                buttonRow.Add(donateBtn);
                var rightButtons = new VisualElement();
                rightButtons.style.flexDirection = FlexDirection.Row;
                rightButtons.Add(exportBtn);
                rightButtons.Add(closeButton);
                buttonRow.Add(rightButtons);

                _configPanel.Add(buttonRow);

                // Prevent clicks on panel from closing (both ClickEvent and PointerUpEvent)
                _configPanel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
                _configPanel.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());

                // Add panel to overlay, then overlay to root
                overlay.Add(_configPanel);
                root.Add(overlay);

                overlay.style.display = DisplayStyle.None;
            }
            catch (Exception e)
            {
                Debug.LogError("[Scoreboard] Failed to create UI: " + e);
            }
        }

        private UITK.Button MakeTabButton(string text, bool isActive, Action onClick)
        {
            var btn = new UITK.Button(onClick) { text = text };
            btn.style.height = 50;
            btn.style.flexGrow = 1;
            btn.style.paddingLeft = 8;
            btn.style.paddingRight = 8;
            btn.style.marginRight = 8;
            // Spacing below the tab strip is owned by tabBar.marginBottom; the
            // button keeps no bottom margin of its own (it used to stack a second
            // gap and bleed past the strip into the search row).
            btn.style.marginBottom = 0;
            btn.style.fontSize = 24;
            btn.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            btn.style.borderTopLeftRadius = 6;
            btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 0;
            btn.style.borderBottomRightRadius = 0;
            btn.style.borderBottomWidth = isActive ? 3 : 0;
            btn.style.borderBottomColor = new StyleColor(Color.white);
            btn.style.backgroundColor = new StyleColor(isActive ? TabActiveBg : TabInactiveBg);
            btn.style.color = isActive ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            ForceUIFont(btn);

            // Add hover effect - white background on hover (unless active)
            btn.RegisterCallback<PointerEnterEvent>(_ => {
                float borderWidth = btn.resolvedStyle.borderBottomWidth;
                if (borderWidth < 1)
                {
                    btn.style.backgroundColor = new StyleColor(Color.white);
                    btn.style.color = Color.black;
                }
            });
            btn.RegisterCallback<PointerLeaveEvent>(_ => {
                float borderWidth = btn.resolvedStyle.borderBottomWidth;
                if (borderWidth < 1)
                {
                    btn.style.backgroundColor = new StyleColor(TabInactiveBg);
                    btn.style.color = new Color(0.7f, 0.7f, 0.7f);
                }
            });

            return btn;
        }

        private void SwitchToTab(ScoreboardTab tab)
        {
            _activeTab = tab;
            UpdateTabStyles();

            if (_generalContent != null) _generalContent.style.display = (tab == ScoreboardTab.General) ? DisplayStyle.Flex : DisplayStyle.None;
            if (_presetsContent != null) _presetsContent.style.display = (tab == ScoreboardTab.Presets) ? DisplayStyle.Flex : DisplayStyle.None;
            if (_advancedContent != null) _advancedContent.style.display = (tab == ScoreboardTab.Advanced) ? DisplayStyle.Flex : DisplayStyle.None;
            if (_testsContent != null) _testsContent.style.display = (tab == ScoreboardTab.Tests) ? DisplayStyle.Flex : DisplayStyle.None;

            // Re-apply the active search query to the newly-shown tab's rows.
            // (ActiveTabContainer / MarkSearchable / ApplySearchFilter live in
            // ScoreboardUIManager.Search.cs.)
            ApplySearchFilter();
        }

        private void UpdateTabStyles()
        {
            UpdateSingleTabStyle(_tabGeneral, _activeTab == ScoreboardTab.General);
            UpdateSingleTabStyle(_tabPresets, _activeTab == ScoreboardTab.Presets);
            UpdateSingleTabStyle(_tabAdvanced, _activeTab == ScoreboardTab.Advanced);
            UpdateSingleTabStyle(_tabTests, _activeTab == ScoreboardTab.Tests);
        }

        private void UpdateSingleTabStyle(UITK.Button btn, bool active)
        {
            if (btn == null) return;
            btn.style.backgroundColor = new StyleColor(active ? TabActiveBg : TabInactiveBg);
            btn.style.color = active ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            btn.style.borderBottomWidth = active ? 3 : 0;
        }

        // BuildGeneralTab moved to ScoreboardUIManager.GeneralTab.cs
        // BuildPresetsTab moved to ScoreboardUIManager.PresetsTab.cs
        // BuildAdvancedTab moved to ScoreboardUIManager.AdvancedTab.cs
        // BuildTestsTab moved to ScoreboardUIManager.TestsTab.cs

        // Styling methods moved to ScoreboardUIManager.Styling.cs

        private void SaveAndApplyConfig()
        {
            try
            {
                SaveScoreboardConfig(_config);
                
                // Apply config changes using lightweight method (no UI recreation)
                if (_scoreboardReference != null)
                {
                    _scoreboardReference.ApplyConfigChanges(_config);
                }
                // Don't hide UI - let user continue configuring
            }
            catch (Exception e)
            {
                Debug.LogError("[Scoreboard] Failed to save config: " + e);
            }
        }

        private void ResetToDefaults()
        {
            _config = new ScoreboardConfig();
            SaveScoreboardConfig(_config); // Save the default config
            
            // Refresh the scoreboard with new settings
            if (_scoreboardReference != null)
            {
                _scoreboardReference.RefreshScoreboardUI();
            }
            
            HideUI();
            _configPanel = null; // Force UI recreation with new values
            ShowUI();
        }

        private ScoreboardConfig LoadScoreboardConfig()
        {
            try
            {
                string configDir = ScoreboardPaths.ConfigDir;
                string configPath = ScoreboardPaths.ConfigPath;
                
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    return JsonUtility.FromJson<ScoreboardConfig>(json) ?? new ScoreboardConfig();
                }
                
                // Try to migrate from old location FIRST (config/CustomScoreboard.json)
                string oldConfigPath = Path.Combine(ScoreboardPaths.GameRoot, "config", "CustomScoreboard.json");
                if (File.Exists(oldConfigPath))
                {
                    Directory.CreateDirectory(configDir);
                    File.Copy(oldConfigPath, configPath, true);
                    Debug.Log($"[Scoreboard] Migrated config from old location: {oldConfigPath}");
                    
                    // Rename old file to _old instead of deleting
                    try
                    {
                        string oldConfigBackup = oldConfigPath.Replace(".json", "_old.json");
                        if (File.Exists(oldConfigBackup)) File.Delete(oldConfigBackup);
                        File.Move(oldConfigPath, oldConfigBackup);
                        Debug.Log($"[Scoreboard] Renamed old config file to: {oldConfigBackup}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Scoreboard] Could not rename old config file: {ex.Message}");
                    }
                    
                    string json = File.ReadAllText(configPath);
                    return JsonUtility.FromJson<ScoreboardConfig>(json) ?? new ScoreboardConfig();
                }
                
                // Try to copy default config from the DLL directory (works for both Plugins and Workshop)
                string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string dllDirectory = Path.GetDirectoryName(dllPath);
                string defaultConfigPath = Path.Combine(dllDirectory, "CustomScoreboard.json");
                
                if (File.Exists(defaultConfigPath))
                {
                    Directory.CreateDirectory(configDir);
                    File.Copy(defaultConfigPath, configPath, false);
                    Debug.Log($"[Scoreboard] Copied default config from {defaultConfigPath}");
                    
                    string json = File.ReadAllText(configPath);
                    return JsonUtility.FromJson<ScoreboardConfig>(json) ?? new ScoreboardConfig();
                }
                
                // Fallback to code defaults if file not found
                var defaultConfig = new ScoreboardConfig();
                SaveScoreboardConfig(defaultConfig);
                return defaultConfig;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Scoreboard] Failed to load config: {e}");
                return new ScoreboardConfig();
            }
        }

        private void SaveScoreboardConfig(ScoreboardConfig config)
        {
            try
            {
                Directory.CreateDirectory(ScoreboardPaths.ConfigDir);
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(ScoreboardPaths.ConfigPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Scoreboard] Failed to save config: " + e);
            }
        }

        private PresetsConfig LoadPresetsConfig()
        {
            try
            {
                string configDir = ScoreboardPaths.ConfigDir;
                string configPath = ScoreboardPaths.PresetsPath;
                
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var loadedPresets = new PresetsConfig();
                    
                    // Manual JSON parsing for team presets
                    int presetsStart = json.IndexOf("\"teamPresets\": [");
                    if (presetsStart >= 0)
                    {
                        presetsStart = json.IndexOf('[', presetsStart);
                        int presetsEnd = FindMatchingBracket(json, presetsStart);
                        if (presetsEnd > presetsStart)
                        {
                            ParsePresets(json.Substring(presetsStart, presetsEnd - presetsStart + 1), loadedPresets.teamPresets);
                        }
                    }
                    
                    // Manual JSON parsing for size presets
                    int sizePresetsStart = json.IndexOf("\"sizePresets\": [");
                    if (sizePresetsStart >= 0)
                    {
                        sizePresetsStart = json.IndexOf('[', sizePresetsStart);
                        int sizePresetsEnd = FindMatchingBracket(json, sizePresetsStart);
                        if (sizePresetsEnd > sizePresetsStart)
                        {
                            ParseSizePresets(json.Substring(sizePresetsStart, sizePresetsEnd - sizePresetsStart + 1), loadedPresets.sizePresets);
                        }
                    }
                    
                    Debug.Log($"[Scoreboard] Loaded presets - Team: {loadedPresets.teamPresets.Count}, Size: {loadedPresets.sizePresets.Count}");
                    return loadedPresets;
                }
                
                // Try to migrate from old location FIRST (config/ScoreboardPresets.json)
                string oldPresetsPath = Path.Combine(ScoreboardPaths.GameRoot, "config", "ScoreboardPresets.json");
                if (File.Exists(oldPresetsPath))
                {
                    Directory.CreateDirectory(configDir);
                    File.Copy(oldPresetsPath, configPath, true);
                    Debug.Log($"[Scoreboard] Migrated presets from old location: {oldPresetsPath}");
                    
                    // Rename old file to _old instead of deleting
                    try
                    {
                        string oldPresetsBackup = oldPresetsPath.Replace(".json", "_old.json");
                        if (File.Exists(oldPresetsBackup)) File.Delete(oldPresetsBackup);
                        File.Move(oldPresetsPath, oldPresetsBackup);
                        Debug.Log($"[Scoreboard] Renamed old presets file to: {oldPresetsBackup}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Scoreboard] Could not rename old presets file: {ex.Message}");
                    }
                    
                    string json = File.ReadAllText(configPath);
                    var loadedPresets = new PresetsConfig();
                    
                    int presetsStart = json.IndexOf("\"teamPresets\": [");
                    if (presetsStart >= 0)
                    {
                        presetsStart = json.IndexOf('[', presetsStart);
                        int presetsEnd = FindMatchingBracket(json, presetsStart);
                        if (presetsEnd > presetsStart)
                        {
                            ParsePresets(json.Substring(presetsStart, presetsEnd - presetsStart + 1), loadedPresets.teamPresets);
                        }
                    }
                    
                    int sizePresetsStart = json.IndexOf("\"sizePresets\": [");
                    if (sizePresetsStart >= 0)
                    {
                        sizePresetsStart = json.IndexOf('[', sizePresetsStart);
                        int sizePresetsEnd = FindMatchingBracket(json, sizePresetsStart);
                        if (sizePresetsEnd > sizePresetsStart)
                        {
                            ParseSizePresets(json.Substring(sizePresetsStart, sizePresetsEnd - sizePresetsStart + 1), loadedPresets.sizePresets);
                        }
                    }
                    
                    return loadedPresets;
                }
                
                // Try to copy default presets from the DLL directory (works for both Plugins and Workshop)
                string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string dllDirectory = Path.GetDirectoryName(dllPath);
                string defaultPresetsPath = Path.Combine(dllDirectory, "ScoreboardPresets.json");
                
                if (File.Exists(defaultPresetsPath))
                {
                    Directory.CreateDirectory(configDir);
                    try
                    {
                        File.Copy(defaultPresetsPath, configPath, true);
                        Debug.Log($"[Scoreboard] Copied default presets from {defaultPresetsPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Scoreboard] Failed to copy default presets: {ex.Message}");
                        // Don't return empty - continue to try loading what we have
                    }
                    
                    if (File.Exists(configPath))
                    {
                        string json = File.ReadAllText(configPath);
                        var loadedPresets = new PresetsConfig();
                        
                        int presetsStart = json.IndexOf("\"teamPresets\": [");
                        if (presetsStart >= 0)
                        {
                            presetsStart = json.IndexOf('[', presetsStart);
                            int presetsEnd = FindMatchingBracket(json, presetsStart);
                            if (presetsEnd > presetsStart)
                            {
                                ParsePresets(json.Substring(presetsStart, presetsEnd - presetsStart + 1), loadedPresets.teamPresets);
                            }
                        }
                        
                        int sizePresetsStart = json.IndexOf("\"sizePresets\": [");
                        if (sizePresetsStart >= 0)
                        {
                            sizePresetsStart = json.IndexOf('[', sizePresetsStart);
                            int sizePresetsEnd = FindMatchingBracket(json, sizePresetsStart);
                            if (sizePresetsEnd > sizePresetsStart)
                            {
                                ParseSizePresets(json.Substring(sizePresetsStart, sizePresetsEnd - sizePresetsStart + 1), loadedPresets.sizePresets);
                            }
                        }
                        
                        if (loadedPresets.teamPresets.Count > 0 || loadedPresets.sizePresets.Count > 0)
                        {
                            return loadedPresets;
                        }
                    }
                }
                
                // Fall back to empty presets
                var defaultPresets = new PresetsConfig();
                InitializeDefaultPresets(defaultPresets);
                SavePresetsConfig(defaultPresets);
                return defaultPresets;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Scoreboard] Failed to load presets: {e}");
                var presets = new PresetsConfig();
                InitializeDefaultPresets(presets);
                return presets;
            }
        }
        
        private int FindMatchingBracket(string json, int startPos)
        {
            int depth = 0;
            for (int i = startPos; i < json.Length; i++)
            {
                if (json[i] == '[') depth++;
                else if (json[i] == ']')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        private VisualElement GetUIRoot()
        {
            try
            {
                // Try to find an existing UIDocument first
                var uiDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
                foreach (var doc in uiDocuments)
                {
                    if (doc != null && doc.rootVisualElement != null)
                    {
                        Debug.Log("[Scoreboard] Using existing UIDocument");
                        return doc.rootVisualElement;
                    }
                }

                // If no UIDocument found, create our own
                Debug.Log("[Scoreboard] Creating new UIDocument");
                var uiGameObject = new GameObject("ScoreboardUI");
                DontDestroyOnLoad(uiGameObject);
                
                _uiDocument = uiGameObject.AddComponent<UIDocument>();
                
                // Try to get panel settings from UIManager
                try
                {
                    var uiManager = MonoBehaviourSingleton<UIManager>.Instance;
                    if (uiManager?.PanelSettings != null)
                    {
                        _uiDocument.panelSettings = uiManager.PanelSettings;
                        Debug.Log("[Scoreboard] Panel settings applied from UIManager");
                    }
                }
                catch (Exception panelEx)
                {
                    Debug.LogWarning("[Scoreboard] Could not get UIManager panel settings: " + panelEx.Message);
                }

                return _uiDocument.rootVisualElement;
            }
            catch (Exception e)
            {
                Debug.LogError("[Scoreboard] Failed to get UI root: " + e);
                return null;
            }
        }

        private void OnDestroy()
        {
            UnhookEvents();
            if (_configPanel != null && _configPanel.parent != null)
            {
                _configPanel.RemoveFromHierarchy();
            }
        }
    }
}
