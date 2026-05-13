
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using Unity.Netcode;
using System.Text.RegularExpressions;

namespace CustomScoreboard.UI
{
    public partial class CustomBroadcastScoreboard : MonoBehaviour
    {
        private VisualElement scoreboardContainer;
        private VisualElement leagueLogo;
        private VisualElement leagueLogoSection;
        private VisualElement blueSection;
        private VisualElement redSection;
        private VisualElement blueTeamLogo;
        private VisualElement redTeamLogo;
        private Label blueNameLabel;
        private Label redNameLabel;
        private Label blueScoreLabel;
        private Label redScoreLabel;
        private Label blueShotLabel;
        private Label redShotLabel;
        private VisualElement blueStatPopup;
        private Label blueStatLabel;
        private VisualElement redStatPopup;
        private Label redStatLabel;
        private VisualElement goalOverlay;
        private Label goalOverlayLabel;
        private VisualElement winOverlay; // Win/shutout overlay that persists
        private Label winOverlayLabel;
        private VisualElement periodSummaryPopup; // Separate popup for period summaries

        // ScorebugAnchor: positions are scoreboardContainer-local pixel coordinates.
        // All popups are children of scoreboardContainer so they follow its position/scale
        // automatically. Layout (scorebug-local x): blue 0-280, logo 280-300, red 300-580,
        // periodBox 580-660, timeBox 660-780. Total width 780, height 41.
        private static class ScorebugAnchor
        {
            public const float Width = 780f;
            public const float Height = 41f;

            // Stat popups (80px wide) anchored to the RIGHT edge of each team section.
            // blueSection ends at x=280; redSection ends at x=580. Popup right edge sits
            // flush with the section edge under SHOTS column.
            public const float BlueStatPopupLeft = 200f; // right edge at 280 (blueSection end)
            public const float RedStatPopupLeft = 500f;  // right edge at 580 (redSection end)

            // Goal/Win overlay covers the full scorebug during the animation.
            public const float GoalOverlayLeft = 0f;
            public const float GoalOverlayTop = 0f;
            public const float GoalOverlayWidth = Width;
            public const float GoalOverlayHeight = Height;

            // Centered popups span the full scorebug width.
            public const float CenteredPopupWidth = Width;  // 780
            public const float CenteredPopupLeft = 0f;

            // Slide distances (final TOP, in scorebug-local pixels). Initial top is always 0
            // (hidden behind scorebug due to z-order Insert(0)). Final top = Height (41) lands
            // the popup flush below the scorebug, fully visible.
            // 40, not 41 — 1px overlap with scorebug bottom eliminates a tiny gap that
            // appeared during the slide-out animation due to alignItems=Center rounding.
            public const float StatPopupSlideTo = 40f;
            public const float LineupSlideTo = 40f;
            public const float ScoringSummarySlideTo = 40f;
            public const float PeriodSummarySlideTo = 40f;
            public const float GameSummarySlideTo = 40f;
        }
        private Dictionary<ulong, int> playerGoals = new Dictionary<ulong, int>(); // Track goals per player for HAT TRICK
        private Dictionary<ulong, int> playerShots = new Dictionary<ulong, int>(); // Track shots per player for shooting %
        
        // Save percentage from stats mod (per goalie Steam ID)
        private Dictionary<string, (int shots, int saves)> goalieSaveStats = new Dictionary<string, (int, int)>();
        
        // Additional stats from Stats mod (per player Steam ID)
        private Dictionary<string, int> playerHits = new Dictionary<string, int>();
        private Dictionary<string, int> playerPasses = new Dictionary<string, int>();
        private Dictionary<string, int> playerTakeaways = new Dictionary<string, int>();
        private Dictionary<string, int> playerTurnovers = new Dictionary<string, int>();
        private Dictionary<string, int> playerBlocks = new Dictionary<string, int>();
        private Dictionary<string, int> playerSOG = new Dictionary<string, int>(); // Shots on goal per player (by Steam ID)
        
        // Extended stats from Stats mod v0.7.1+
        private Dictionary<string, int> playerStickSaves = new Dictionary<string, int>(); // Goalie stick saves
        private Dictionary<string, int> playerBodySaves = new Dictionary<string, int>(); // Goalie body saves
        private Dictionary<string, int> playerPlusMinus = new Dictionary<string, int>(); // Plus/minus rating
        private Dictionary<string, int> playerPuckBattleWins = new Dictionary<string, int>(); // Puck battle wins
        private Dictionary<string, int> playerPuckBattleLosses = new Dictionary<string, int>(); // Puck battle losses
        private Dictionary<string, double> playerTimeOnIce = new Dictionary<string, double>(); // Time on ice (seconds)
        
        // Cached player stats (so they appear in summary even after leaving) - includes position name
        private Dictionary<ulong, (string name, int number, PlayerTeam team, PlayerRole role, int goals, int assists, int shots, string steamId, string position)> cachedPlayerStats = 
            new Dictionary<ulong, (string, int, PlayerTeam, PlayerRole, int, int, int, string, string)>();
        
        // Last goal information for scoring summary command
        private Dictionary<string, object> lastGoalMessage = null;
        
        // Three stars tracking (from chat messages)
        private string firstStar = null;
        private string secondStar = null;
        private string thirdStar = null;

        private Label periodLabel;
        private Label timeLabel;

        // Shootout tracking
        private bool isShootoutActive = false;
        private bool shootoutLabelOverride = false; // Prevent period label from being overwritten
        private int shootoutMaxRounds = 5; // Default to best-of-5
        private int blueShootoutGoals = 0;
        private int redShootoutGoals = 0;
        private int blueShootoutAttempts = 0;
        private int redShootoutAttempts = 0;
        private List<string> blueShooters = new List<string>();
        private List<string> redShooters = new List<string>();
        private string currentShooter = "";
        private PlayerTeam currentShooterTeam = PlayerTeam.None;
        
        // Track which specific attempts were goals (true = goal, false = miss)
        private List<bool> blueShootoutResults = new List<bool>();
        private List<bool> redShootoutResults = new List<bool>();

        private int lastBlueScore = 0;
        private int lastRedScore = 0;
        private int blueTeamShots = 0;
        private int redTeamShots = 0;
        private int blueSaves = 0;
        private int redSaves = 0;
        
        // Track last shot speeds for each team (for /shotspeed commands)
        private float lastBlueShotSpeed = 0f;
        private float lastRedShotSpeed = 0f;

        // Correlates the scoring team from Event_OnGoalScored with the shot speed chat message
        private PlayerTeam? _pendingShotSpeedTeam = null;
        
        // Track pending win animations to prevent overlaps
        private bool hasShownWinAnimation = false;
        private bool isAnimationPlaying = false; // Lock to prevent multiple animations at once
        
        // Periodic Stats mod capture interval
        private const float STATS_MOD_CAPTURE_INTERVAL = 5f;
        
        // Log file monitoring for Stats mod (fallback when reflection fails)
        private System.Threading.Thread logMonitorThread;
        private bool isMonitoringLog = false;
        private long lastLogPosition = 0;

        private bool isInitialized = false;
        private bool isInMainMenu = false;
        private GamePhase currentPhase = GamePhase.None;
        private int currentPeriod = 0;
        private GamePhase previousPhase = GamePhase.None;
        private ScoreboardConfig config;
        
        // Team logo textures for animations
        private Texture2D blueTeamLogoTexture;
        private Texture2D redTeamLogoTexture;
        
        // Texture cache to avoid reloading images from disk
        private Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();
        
        // Debug logging helper - only logs if debug is enabled in config
        private void DebugLog(string message)
        {
            if (config != null && config.enableDebugLogs)
            {
                Debug.Log(message);
            }
        }
        
        private void DebugWarning(string message)
        {
            if (config != null && config.enableDebugLogs)
            {
                Debug.LogWarning(message);
            }
        }
        
        // Always log important shot data even if debug is off
        private void LogShotData(string message)
        {
            if (config != null && config.enableDebugLogs)
            {
                Debug.Log($"[SHOTS] {message}");
            }
        }

        // Coroutine references for cleanup
        private Coroutine _periodicChecksCoroutine;
        private Coroutine _initializationCoroutine;
        
        // Cached WaitForSeconds to reduce GC pressure
        private static readonly WaitForSeconds _waitHalfSecond = new WaitForSeconds(0.5f);
        private static readonly WaitForSeconds _waitThreeSeconds = new WaitForSeconds(3f);
        
        // Tracking for optimized periodic checks
        private bool _originalScoreboardHidden = false;
        private int _pollGameStateCounter = 0;
        private float _lastGoalAnimationTriggerTime = -10f;

        private void AddGameEventListener(string eventName, Action<Dictionary<string, object>> handler)
        {
            try
            {
                EventManager.AddEventListener(eventName, handler);
                DebugLog($"[CustomScoreboard] Listening for event: {eventName}");
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Failed to add listener for {eventName}: {ex.Message}");
            }
        }

        private void RemoveGameEventListener(string eventName, Action<Dictionary<string, object>> handler)
        {
            try
            {
                EventManager.RemoveEventListener(eventName, handler);
            }
            catch { }
        }

        private void Start()
        {
            DebugLog("[CustomScoreboard] Start() called - registering event listeners");
            
            // Core game state — phase changes are derived from GameState (Event_OnGamePhaseChanged was removed in 310).
            AddGameEventListener("Event_Everyone_OnGameStateChanged", new Action<Dictionary<string, object>>(Event_OnGameStateChanged));
            AddGameEventListener("Event_Everyone_OnGoalScored", new Action<Dictionary<string, object>>(Event_OnGoalScored));

            // Connection events — Event_OnClientConnected (no payload) and Event_Everyone_OnClientConnected (with clientId) both fire.
            AddGameEventListener("Event_Everyone_OnClientConnected", new Action<Dictionary<string, object>>(Event_OnClientConnected));
            AddGameEventListener("Event_OnClientConnected", new Action<Dictionary<string, object>>(Event_OnClientConnected));
            AddGameEventListener("Event_OnDisconnected", new Action<Dictionary<string, object>>(Event_Client_OnDisconnected));
            AddGameEventListener("Event_OnMainMenuShow", new Action<Dictionary<string, object>>(Event_Client_OnMainMenuShow));

            // Level events — renamed to Spawned/Despawned in 310.
            AddGameEventListener("Event_Everyone_OnLevelSpawned", new Action<Dictionary<string, object>>(Event_OnLevelStarted));
            AddGameEventListener("Event_Everyone_OnLevelDespawned", new Action<Dictionary<string, object>>(Event_OnLevelDestroyed));

            // Player stat events.
            AddGameEventListener("Event_Everyone_OnPlayerGoalsChanged", new Action<Dictionary<string, object>>(Event_OnPlayerGoalsChanged));
            AddGameEventListener("Event_Everyone_OnPlayerAssistsChanged", new Action<Dictionary<string, object>>(Event_OnPlayerAssistsChanged));

            // Stats mod integration (oomtm450_stats fires this).
            AddGameEventListener("Event_OnStatsTrigger", new Action<Dictionary<string, object>>(Event_OnStatsTrigger));
            
            // Start log file monitoring in background thread
            StartLogMonitoring();
            
            // Start initialization coroutine instead of polling in Update
            _initializationCoroutine = StartCoroutine(InitializationCoroutine());
            
            // Start periodic checks coroutine (replaces Update polling)
            _periodicChecksCoroutine = StartCoroutine(PeriodicChecksCoroutine());
        }

        private System.Collections.IEnumerator InitializationCoroutine()
        {
            // Wait for UIManager to be available
            while (GetUIManager() == null)
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            // UIManager is ready, initialize
            try
            {
                // Remove any existing UI elements before recreating
                if (scoreboardContainer != null && scoreboardContainer.parent != null)
                {
                    scoreboardContainer.RemoveFromHierarchy();
                }
                if (leagueLogo != null && leagueLogo.parent != null)
                {
                    leagueLogo.RemoveFromHierarchy();
                }
                
                CreateCustomScoreboard();
                
                // Check if custom scoreboard is enabled
                if (config != null && config.enableCustomScoreboard)
                {
                    HideOriginalScoreboard();
                    // Only show if we're not in main menu and in a game phase
                    if (!isInMainMenu && currentPhase != GamePhase.None)
                    {
                        TurnOn();
                    }
                    else
                    {
                        TurnOff();
                    }
                }
                else
                {
                    // Custom scoreboard disabled - show original
                    ShowOriginalScoreboard();
                    TurnOff();
                }
                isInitialized = true;

                DebugLog("[CustomScoreboard] Initialization complete via coroutine");
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error during initialization: {ex.Message}");
            }
        }

        private System.Collections.IEnumerator PeriodicChecksCoroutine()
        {
            // Wait for initialization first
            while (!isInitialized)
            {
                yield return _waitHalfSecond;
            }
            
            float lastStatsCapture = 0f;
            
            while (true)
            {
                yield return _waitThreeSeconds; // Check every 3 seconds instead of 1 to reduce overhead
                
                try
                {
                    // Only hide original scoreboard once per session (or when it becomes visible again)
                    if (config != null && config.enableCustomScoreboard && !_originalScoreboardHidden)
                    {
                        HideOriginalScoreboard();
                        _originalScoreboardHidden = true;
                    }
                    
                    // Periodically capture Stats mod data during gameplay (every 5 seconds)
                    if (Time.time >= lastStatsCapture + STATS_MOD_CAPTURE_INTERVAL)
                    {
                        lastStatsCapture = Time.time;
                        if (currentPhase == GamePhase.Play || currentPhase == GamePhase.FaceOff)
                        {
                            UpdateStatsFromStatsMod();
                        }
                    }
                    
                    // Poll game state less frequently (every 9 seconds = every 3rd iteration)
                    _pollGameStateCounter++;
                    if (_pollGameStateCounter >= 3 && config != null && config.enableCustomScoreboard)
                    {
                        _pollGameStateCounter = 0;
                        PollGameState();
                    }
                    
                    // Hide scoreboard if we're in main menu
                    if (isInMainMenu && scoreboardContainer != null && scoreboardContainer.style.display == DisplayStyle.Flex)
                    {
                        TurnOff();
                        DebugLog("[CustomScoreboard] Hiding scoreboard - returned to main menu");
                    }

                    // Update period label to reflect current phase (lightweight operation)
                    UpdatePhaseDisplay();
                }
                catch (Exception ex)
                {
                    DebugWarning($"[CustomScoreboard] Error in periodic checks: {ex.Message}");
                }
            }
        }
        
        // Log monitoring and Stats mod integration methods moved to CustomBroadcastScoreboard.StatsIntegration.cs

        // RefreshScoreboardUI, ApplyConfigChanges, UpdateMinimapColors, GetConfig, HotswapLogos, UpdatePositionAndScale moved to CustomBroadcastScoreboard.ConfigHotswap.cs

        private void OnDestroy()
        {
            try
            {
                // Stop coroutines
                if (_periodicChecksCoroutine != null)
                {
                    StopCoroutine(_periodicChecksCoroutine);
                }
                if (_initializationCoroutine != null)
                {
                    StopCoroutine(_initializationCoroutine);
                }
                
                // Stop log monitoring
                isMonitoringLog = false;
                if (logMonitorThread != null && logMonitorThread.IsAlive)
                {
                    logMonitorThread.Join(1000); // Wait up to 1 second for thread to stop
                }
                
                // Kill all DOTween animations on scoreboard elements
                // DOTween.Kill(this) sweeps any schedule-only sequences (delayed callbacks
                // for win animations, scoring summaries, polls) that target this MonoBehaviour.
                DOTween.Kill(this);
                if (scoreboardContainer != null) DOTween.Kill(scoreboardContainer);
                if (leagueLogo != null) DOTween.Kill(leagueLogo);
                if (goalOverlay != null) DOTween.Kill(goalOverlay);
                if (winOverlay != null) DOTween.Kill(winOverlay);
                if (winOverlayLabel != null) DOTween.Kill(winOverlayLabel);
                if (periodSummaryPopup != null) DOTween.Kill(periodSummaryPopup);
                if (blueStatPopup != null) DOTween.Kill(blueStatPopup);
                if (redStatPopup != null) DOTween.Kill(redStatPopup);
                
                if (scoreboardContainer != null && scoreboardContainer.parent != null)
                {
                    scoreboardContainer.RemoveFromHierarchy();
                }
                if (leagueLogo != null && leagueLogo.parent != null)
                {
                    leagueLogo.RemoveFromHierarchy();
                }
                
                // Restore base game scoreboard UI
                RestoreBaseGameUI();
                
                // Remove all event listeners (must mirror the registration set in Start()).
                RemoveGameEventListener("Event_Everyone_OnGameStateChanged", new Action<Dictionary<string, object>>(Event_OnGameStateChanged));
                RemoveGameEventListener("Event_Everyone_OnGoalScored", new Action<Dictionary<string, object>>(Event_OnGoalScored));
                RemoveGameEventListener("Event_Everyone_OnClientConnected", new Action<Dictionary<string, object>>(Event_OnClientConnected));
                RemoveGameEventListener("Event_OnClientConnected", new Action<Dictionary<string, object>>(Event_OnClientConnected));
                RemoveGameEventListener("Event_OnDisconnected", new Action<Dictionary<string, object>>(Event_Client_OnDisconnected));
                RemoveGameEventListener("Event_OnMainMenuShow", new Action<Dictionary<string, object>>(Event_Client_OnMainMenuShow));
                RemoveGameEventListener("Event_Everyone_OnLevelSpawned", new Action<Dictionary<string, object>>(Event_OnLevelStarted));
                RemoveGameEventListener("Event_Everyone_OnLevelDespawned", new Action<Dictionary<string, object>>(Event_OnLevelDestroyed));
                RemoveGameEventListener("Event_Everyone_OnPlayerGoalsChanged", new Action<Dictionary<string, object>>(Event_OnPlayerGoalsChanged));
                RemoveGameEventListener("Event_Everyone_OnPlayerAssistsChanged", new Action<Dictionary<string, object>>(Event_OnPlayerAssistsChanged));
                RemoveGameEventListener("Event_OnStatsTrigger", new Action<Dictionary<string, object>>(Event_OnStatsTrigger));
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in OnDestroy: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Restores the base game scoreboard UI and minimap colors when mod is disabled
        /// </summary>
        private void RestoreBaseGameUI()
        {
            try
            {
                // Show the UIGameState component (the actual scoreboard UI at the top).
                // UIGameState isn't its own singleton in 310 — reach it through UIManager.GameState.
                var uiGameState = MonoBehaviourSingleton<UIManager>.Instance?.GameState;
                if (uiGameState != null)
                {
                    uiGameState.Show();
                    DebugLog("[CustomScoreboard] Restored UIGameState");
                }
                
                // Show all base game Scoreboard objects
                Scoreboard[] scoreboards = UnityEngine.Object.FindObjectsByType<Scoreboard>(FindObjectsSortMode.None);
                foreach (Scoreboard sb in scoreboards)
                {
                    sb.gameObject.SetActive(true);
                }
                if (scoreboards.Length > 0)
                {
                    DebugLog($"[CustomScoreboard] Restored {scoreboards.Length} base game scoreboard(s)");
                }
                
                // Show the GameStateContainer in the UI root
                var uiManager = GetUIManager();
                if (uiManager != null)
                {
                    var root = uiManager.RootVisualElement;
                    if (root != null)
                    {
                        var gameStateContainer = root.Q("GameStateContainer");
                        if (gameStateContainer != null)
                        {
                            gameStateContainer.style.display = DisplayStyle.Flex;
                            gameStateContainer.style.visibility = Visibility.Visible;
                            gameStateContainer.style.opacity = 1;
                            DebugLog("[CustomScoreboard] Restored GameStateContainer visibility");
                        }
                    }
                }
                
                // Reset minimap player colors to default (white)
                RestoreMinimapColors();
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error restoring base game UI: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Restores default minimap number colors
        /// </summary>
        private void RestoreMinimapColors()
        {
            try
            {
                var minimap = UnityEngine.Object.FindFirstObjectByType<UIMinimap>();
                if (minimap == null) return;
                
                // Get the playerBodyVisualElementMap field
                var minimapType = typeof(UIMinimap);
                var mapField = minimapType.GetField("playerBodyVisualElementMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (mapField == null) return;
                
                var playerMap = mapField.GetValue(minimap) as System.Collections.IDictionary;
                if (playerMap == null) return;
                
                // Iterate through all players and reset their number colors to white
                foreach (System.Collections.DictionaryEntry entry in playerMap)
                {
                    var visualElement = entry.Value as VisualElement;
                    if (visualElement == null) continue;
                    
                    Label numberLabel = visualElement.Query<Label>("Number");
                    if (numberLabel != null)
                    {
                        numberLabel.style.color = new StyleColor(Color.white);
                    }
                }
                
                DebugLog("[CustomScoreboard] Restored minimap colors to default");
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error restoring minimap colors: {ex.Message}");
            }
        }

        // Event handlers moved to CustomBroadcastScoreboard.Events.cs

        private void HideOriginalScoreboard()
        {
            try
            {
                // Hide the UIGameState component (the actual scoreboard UI at the top)
                var uiManager = GetUIManager();
                if (uiManager != null)
                {
                    // Directly query and hide the GameStateContainer from root
                    var root = uiManager.RootVisualElement;
                    if (root != null)
                    {
                        var gameStateContainer = root.Q("GameStateContainer");
                        if (gameStateContainer != null && gameStateContainer.style.display != DisplayStyle.None)
                        {
                            gameStateContainer.style.display = DisplayStyle.None;
                            gameStateContainer.style.visibility = Visibility.Hidden;
                            gameStateContainer.style.opacity = 0;
                            DebugLog("[CustomScoreboard] Hidden GameStateContainer");
                        }
                    }
                    
                    // Also hide via UIGameState component
                    var uiGameState = MonoBehaviourSingleton<UIManager>.Instance.GameState;
                    if (uiGameState != null && uiGameState.IsVisible)
                    {
                        uiGameState.Hide();
                        DebugLog("[CustomScoreboard] Hidden UIGameState");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error hiding original scoreboard: {ex.Message}");
            }
        }
        
        private void ShowOriginalScoreboard()
        {
            _originalScoreboardHidden = false; // Reset flag so it will be hidden again if needed
            try
            {
                // Show the UIGameState component (the actual scoreboard at the top)
                var uiManager = GetUIManager();
                if (uiManager != null)
                {
                    // Directly query and show the GameStateContainer from root
                    var root = uiManager.RootVisualElement;
                    if (root != null)
                    {
                        var gameStateContainer = root.Q("GameStateContainer");
                        if (gameStateContainer != null)
                        {
                            gameStateContainer.style.display = DisplayStyle.Flex;
                            gameStateContainer.style.visibility = Visibility.Visible;
                            gameStateContainer.style.opacity = 1;
                            DebugLog("[CustomScoreboard] Shown GameStateContainer");
                        }
                    }
                    
                    var uiGameState = uiManager.GameState;
                    if (uiGameState != null)
                    {
                        // Access the container field using reflection and show it
                        var containerField = GetPrivateInstanceField(typeof(UIGameState), "container");
                        
                        if (containerField != null)
                        {
                            var container = containerField.GetValue(uiGameState) as VisualElement;
                            if (container != null)
                            {
                                container.style.display = DisplayStyle.Flex;
                                container.style.visibility = Visibility.Visible;
                                container.style.opacity = 1;
                                container.style.width = StyleKeyword.Auto;
                                container.style.height = StyleKeyword.Auto;
                                DebugLog("[CustomScoreboard] Shown original UIGameState container");
                            }
                        }
                        
                        // Also call Show() to properly activate it
                        if (!uiGameState.IsVisible)
                        {
                            uiGameState.Show();
                        }
                    }
                }
                
                // Also show the old 3D Scoreboard component (if it exists)
                Scoreboard[] scoreboards = FindObjectsByType<Scoreboard>(FindObjectsSortMode.None);
                foreach (Scoreboard sb in scoreboards)
                {
                    sb.gameObject.SetActive(true);
                    
                    // Re-enable the ScoreboardController
                    var controller = sb.GetComponent<ScoreboardController>();
                    if (controller != null)
                    {
                        controller.enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error showing original scoreboard: {ex.Message}");
            }
        }

        // Event handlers moved to CustomBroadcastScoreboard.GameEvents.cs


        // ShowShotSpeedPopup, GetSpeedUnitLabel, ConvertSpeedToDisplayUnits moved to CustomBroadcastScoreboard.Popups.cs

        // ShowSavePercentages moved to CustomBroadcastScoreboard.Popups.cs

        // ShowPlayerLineup moved to CustomBroadcastScoreboard.Popups.cs
        
        // GetPlayerPositionName, AddPlayerFormation, GetUIFont, ShowStatPopup moved to CustomBroadcastScoreboard.Popups.cs
        
        // Test animation methods moved to CustomBroadcastScoreboard.Tests.cs
        
        // ShowWinAnimation, StartWinTextLoop, HideWinAnimation, ShowGoalAnimation, ShowOTWinAnimation moved to CustomBroadcastScoreboard.Animations.cs

        // GetGoalieSavePercentage moved to CustomBroadcastScoreboard.Stats.cs

        // ShowPeriodSummary, AddLeaderCategory moved to CustomBroadcastScoreboard.Summaries.cs

        public void TurnOn()
        {
            if (scoreboardContainer != null)
            {
                scoreboardContainer.style.display = DisplayStyle.Flex;
            }
            if (leagueLogo != null)
            {
                leagueLogo.style.display = DisplayStyle.Flex;
            }
        }

        public void TurnOff()
        {
            if (scoreboardContainer != null)
            {
                scoreboardContainer.style.display = DisplayStyle.None;
            }
            if (leagueLogo != null)
            {
                leagueLogo.style.display = DisplayStyle.None;
            }
        }
        
        public void RemoveScoreboardUI()
        {
            // Completely remove UI elements from hierarchy
            if (scoreboardContainer != null && scoreboardContainer.parent != null)
            {
                scoreboardContainer.RemoveFromHierarchy();
                DebugLog("[CustomScoreboard] Removed scoreboard container from hierarchy");
            }
            if (leagueLogo != null && leagueLogo.parent != null)
            {
                leagueLogo.RemoveFromHierarchy();
                DebugLog("[CustomScoreboard] Removed league logo from hierarchy");
            }
            scoreboardContainer = null;
            leagueLogo = null;
            isInitialized = false;
        }
        
        // Test methods moved to CustomBroadcastScoreboard.Tests.cs

        public void SetTime(int time)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds((double)time);
            if (timeLabel != null)
            {
                timeLabel.text = timeSpan.ToString("mm") + ":" + timeSpan.ToString("ss");
            }
        }

        public void SetPeriod(int period)
        {
            if (periodLabel != null && !shootoutLabelOverride)
            {
                string phaseName = currentPhase.ToString();
                bool isPreGame = phaseName.IndexOf("pregame", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 phaseName.IndexOf("prematch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 phaseName.IndexOf("lobby", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isPostGame = phaseName.IndexOf("postgame", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  phaseName.IndexOf("postmatch", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isPreGame)
                {
                    periodLabel.text = "PRE-GAME";
                    return;
                }

                if (isPostGame)
                {
                    periodLabel.text = "POST-GAME";
                    return;
                }

                // Don't override if we're in Warmup or other special phases
                if (currentPhase == GamePhase.Play || currentPhase == GamePhase.None)
                {
                    string periodText = "";
                    if (period == 1) periodText = "1ST";
                    else if (period == 2) periodText = "2ND";
                    else if (period == 3) periodText = "3RD";
                    else if (period > 3) periodText = "OT";
                    periodLabel.text = periodText;
                }
                // If we're in Warmup, make sure the label shows WARM-UP
                else if (currentPhase == GamePhase.Warmup)
                {
                    periodLabel.text = "WARM-UP";
                }
            }
        }

        private void UpdatePhaseDisplay()
        {
            if (periodLabel == null) return;

            string phaseName = currentPhase.ToString();
            bool isPreGame = phaseName.IndexOf("pregame", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             phaseName.IndexOf("prematch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             phaseName.IndexOf("lobby", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isPostGame = phaseName.IndexOf("postgame", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              phaseName.IndexOf("postmatch", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isPreGame)
            {
                periodLabel.text = "PRE-GAME";
                return;
            }

            if (isPostGame)
            {
                periodLabel.text = "POST-GAME";
                return;
            }

            // Reset shootout override on warmup
            if (currentPhase == GamePhase.Warmup)
            {
                shootoutLabelOverride = false;
            }

            // Don't override if shootout is active
            if (shootoutLabelOverride) return;

            string phaseText = "";
            switch (currentPhase)
            {
                case GamePhase.Warmup:
                    phaseText = "WARM-UP";
                    break;
                case GamePhase.FaceOff:
                    phaseText = "FACE-OFF";
                    break;
                case GamePhase.Replay:
                    phaseText = "REPLAY";
                    break;
                case GamePhase.BlueScore:
                    phaseText = "GOAL";
                    break;
                case GamePhase.RedScore:
                    phaseText = "GOAL";
                    break;
                case GamePhase.Intermission:
                    phaseText = "END";
                    break;
                case GamePhase.GameOver:
                    phaseText = "FINAL";
                    break;
                default:
                    return; // Keep the period number for Playing phase
            }

            if (!string.IsNullOrEmpty(phaseText))
            {
                periodLabel.text = phaseText;
            }
        }

        public void SetBlueScore(int score)
        {
            if (blueScoreLabel != null)
            {
                blueScoreLabel.text = score.ToString();
            }
        }

        public void SetRedScore(int score)
        {
            if (redScoreLabel != null)
            {
                redScoreLabel.text = score.ToString();
            }
        }
        
        // Reset stats when /warmup or /start command is triggered (called from chat message detection)
        public void ResetStatsOnCommand()
        {
            DebugLog("[CustomScoreboard] Resetting stats due to game reset command/vote");
            blueTeamShots = 0;
            redTeamShots = 0;
            blueSaves = 0;
            redSaves = 0;
            SetBlueShots(0);
            SetRedShots(0);
            lastBlueScore = 0;
            lastRedScore = 0;
            playerShots.Clear();
            playerGoals.Clear();
            cachedPlayerStats.Clear();
            hasShownWinAnimation = false;
            
            // Reset shootout data
            blueShootoutGoals = 0;
            redShootoutGoals = 0;
            blueShootoutAttempts = 0;
            redShootoutAttempts = 0;
            blueShooters.Clear();
            redShooters.Clear();
            currentShooter = null;
            blueShootoutResults.Clear();
            redShootoutResults.Clear();
        }

        public void SetBlueShots(int shots)
        {
            if (blueShotLabel != null)
            {
                blueShotLabel.text = shots.ToString();
                DebugLog($"[CustomScoreboard] SetBlueShots called with {shots}");
            }
            else
            {
                DebugWarning("[CustomScoreboard] SetBlueShots called but blueShotLabel is null");
            }
        }

        public void SetRedShots(int shots)
        {
            if (redShotLabel != null)
            {
                redShotLabel.text = shots.ToString();
                DebugLog($"[CustomScoreboard] SetRedShots called with {shots}");
            }
            else
            {
                DebugWarning("[CustomScoreboard] SetRedShots called but redShotLabel is null");
            }
        }

        private Texture2D LoadLogoImage(string filename)
        {
            try
            {
                if (string.IsNullOrEmpty(filename)) return null;
                
                // Check texture cache first
                if (_textureCache.TryGetValue(filename, out Texture2D cachedTexture) && cachedTexture != null)
                {
                    return cachedTexture;
                }
                
                // Get the directory where this DLL is located
                string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string dllDirectory = Path.GetDirectoryName(dllPath);
                
                // Try multiple possible paths (new ModHub location first)
                string[] possiblePaths = new string[]
                {
                    Path.Combine(ScoreboardPaths.LogosDir, filename), // ModHub location (preferred)
                    Path.Combine(dllDirectory, "scorebuglogos", filename), // Next to DLL
                    Path.Combine(Application.dataPath, "..", "config", "Scoreboard", "scorebuglogos", filename), // Legacy location
                    Path.Combine(Application.dataPath, "..", "Plugins", "Scoreboard", "scorebuglogos", filename), // In mod folder
                    Path.Combine(Application.dataPath, "..", "scorebuglogos", filename), // Game root
                    Path.Combine(Application.dataPath, "scorebuglogos", filename) // Data folder
                };

                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        byte[] fileData = File.ReadAllBytes(path);
                        Texture2D texture = new Texture2D(2, 2);
                        texture.LoadImage(fileData);
                        
                        // Enable better filtering to reduce pixelation when scaling
                        texture.filterMode = FilterMode.Bilinear;
                        texture.anisoLevel = 4;
                        
                        // Cache the texture
                        _textureCache[filename] = texture;
                        
                        DebugLog($"[CustomScoreboard] Loaded {filename} from {path}");
                        return texture;
                    }
                }
                
                DebugWarning($"[CustomScoreboard] Could not find {filename} in any expected location");
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Failed to load {filename}: {ex.Message}");
            }
            return null;
        }
        
        /// <summary>
        /// Clears the texture cache to force reloading images from disk.
        /// Call this if user has updated logo files and wants to see changes.
        /// </summary>
        public void ClearTextureCache()
        {
            _textureCache.Clear();
            DebugLog("[CustomScoreboard] Texture cache cleared");
        }

        public Color ParseHexColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            if (!hex.StartsWith("#")) hex = "#" + hex;
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;
        }

        // OnShootoutBegin, OnShootoutShooterOrder, OnShootoutShooterMessage, OnShootoutResultMessage moved to CustomBroadcastScoreboard.Shootout.cs

        // UpdateShootoutScoreboard, CheckShootoutWinner, OnShootoutWin, ShowShootoutWinAnimation moved to CustomBroadcastScoreboard.Stats.cs

        // OnFirstStarAnnounced, OnSecondStarAnnounced, OnThirdStarAnnounced moved to CustomBroadcastScoreboard.Stats.cs
        
        // UpdatePlayerShots, UpdateGoalieSaves moved to CustomBroadcastScoreboard.Stats.cs
        
        // ShowEndOfGameSummary, CreateTeamStatsColumn moved to CustomBroadcastScoreboard.GameSummary.cs

        // OnShootoutAttempt moved to CustomBroadcastScoreboard.Stats.cs

        // Helper methods moved to CustomBroadcastScoreboard.Helpers.cs

    }
}