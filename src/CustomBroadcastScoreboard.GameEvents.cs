using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

namespace CustomScoreboard.UI
{
    /// <summary>
    /// Partial class containing game event handlers.
    /// Handles game phase changes, state updates, goals, and stats triggers.
    /// </summary>
    public partial class CustomBroadcastScoreboard
    {
        private void Event_OnGamePhaseChanged(Dictionary<string, object> message)
        {
            try
            {
                if (config != null && config.enableCustomScoreboard)
                {
                    HideOriginalScoreboard();
                    _originalScoreboardHidden = true;
                }

                if (!TryGetMessageValue(message, out GamePhase phase, "newGamePhase", "gamePhase", "phase"))
                {
                    DebugWarning("[CustomScoreboard] Event_OnGamePhaseChanged missing phase payload");
                    return;
                }

            string phaseName = phase.ToString().ToLowerInvariant();
            string previousPhaseName = previousPhase.ToString().ToLowerInvariant();
            bool isIntermissionPhase = phase == GamePhase.Intermission || phaseName.Contains("intermission") || phaseName.Contains("periodend");
            bool isGameOverPhase = phase == GamePhase.GameOver || phaseName.Contains("gameover") || phaseName.Contains("postgame") || phaseName.Contains("postmatch");
            bool isFaceOffPhase = phase == GamePhase.FaceOff || phaseName.Contains("faceoff") || phaseName.Contains("face-off");
            bool isPlayPhase = phase == GamePhase.Play || phaseName == "play" || phaseName.Contains("ingame");
            bool wasWarmupOrIntermission = previousPhase == GamePhase.Warmup ||
                                           previousPhase == GamePhase.Intermission ||
                                           previousPhaseName.Contains("warmup") ||
                                           previousPhaseName.Contains("intermission") ||
                                           previousPhaseName.Contains("periodend");

            previousPhase = currentPhase;
            currentPhase = phase;
            
            // IMMEDIATELY update phase display for responsive UI
            UpdatePhaseDisplay();
            
            // Hide shootout popup when period ends or game ends
            if (isIntermissionPhase || isGameOverPhase)
            {
                if (isShootoutActive)
                {
                    DebugLog("[CustomScoreboard] Period ended - ending shootout");
                    isShootoutActive = false;
                    shootoutLabelOverride = false;
                }
            }
            
            if (phase == GamePhase.Warmup)
            {
                DebugLog("Warmup phase started");
                
                // Hide win animation when warmup starts (new game)
                HideWinAnimation();
                
                // Show scoreboard in warmup if custom scoreboard is enabled
                isInMainMenu = false;
                if (config != null && config.enableCustomScoreboard)
                {
                    TurnOn();
                }
                // Reset shot counters and stats at start of warmup
                blueTeamShots = 0;
                redTeamShots = 0;
                blueSaves = 0;
                redSaves = 0;
                SetBlueShots(0);
                SetRedShots(0);
                lastBlueScore = 0;
                lastRedScore = 0;
                playerShots.Clear();
                cachedPlayerStats.Clear();
                
                // Clear Stats mod dictionaries for new game
                lock (playerHits)
                {
                    playerHits.Clear();
                    playerPasses.Clear();
                    playerTakeaways.Clear();
                    playerTurnovers.Clear();
                    playerBlocks.Clear();
                    goalieSaveStats.Clear();
                    playerSOG.Clear();
                    playerStickSaves.Clear();
                    playerBodySaves.Clear();
                    playerPlusMinus.Clear();
                    playerPuckBattleWins.Clear();
                    playerPuckBattleLosses.Clear();
                    playerTimeOnIce.Clear();
                }
                
                // Reset win animation flag so summary shows in next game
                hasShownWinAnimation = false;
                
                DebugLog("[CustomScoreboard] Reset all stats on warmup");
            }
            else if (phase == GamePhase.None)
            {
                // None phase means we're not in a game - might be main menu
                if (isInMainMenu)
                {
                    TurnOff();
                }
            }
            else if (isIntermissionPhase)
            {
                // Period just ended - show period summary after a delay
                DOTween.Sequence()
                    .SetTarget(this)
                    .AppendInterval(3f) // Wait 3 seconds after period ends
                    .OnComplete(() => ShowPeriodSummary(false)); // false = not end of game
            }
            else if (isGameOverPhase)
            {
                DebugLog($"[CustomScoreboard] GameOver phase - hasShownWinAnimation: {hasShownWinAnimation}, isShootoutActive: {isShootoutActive}");
                
                // CRITICAL: Update Stats mod data IMMEDIATELY before dictionaries are cleared
                UpdateStatsFromStatsMod();
                DebugLog("*** CRITICAL: Stats captured at GameOver phase start ***");
                
                // Priority: Shootout win > Shutout > Regular win
                // Only show one win animation
                if (hasShownWinAnimation)
                {
                    DebugLog("[CustomScoreboard] Win animation already shown, skipping duplicate");
                    return;
                }
                
                // Determine winner and if shutout occurred
                PlayerTeam? winner = null;
                bool isShutout = false;
                
                if (lastBlueScore > lastRedScore)
                {
                    winner = PlayerTeam.Blue;
                    // Shutout only counts if game ended in regulation (period 3 or less) AND opponent has 0 goals
                    isShutout = (lastRedScore == 0 && currentPeriod <= 3);
                }
                else if (lastRedScore > lastBlueScore)
                {
                    winner = PlayerTeam.Red;
                    // Shutout only counts if game ended in regulation (period 3 or less) AND opponent has 0 goals
                    isShutout = (lastBlueScore == 0 && currentPeriod <= 3);
                }
                
                // Show win animation if there's a winner
                if (winner.HasValue)
                {
                    hasShownWinAnimation = true;

                    DOTween.Sequence()
                        .SetTarget(this)
                        .AppendInterval(1f)
                        .OnComplete(() => {
                            // Priority 1: Shootout win (highest priority)
                            if (isShootoutActive)
                            {
                                isShootoutActive = false;
                                ShowShootoutWinAnimation(winner.Value);
                                DebugLog($"[CustomScoreboard] Showing SHOOTOUT win for {winner.Value}");
                            }
                            // Priority 2: Shutout win
                            else if (isShutout)
                            {
                                ShowWinAnimation(winner.Value, true);
                                DebugLog($"[CustomScoreboard] Showing SHUTOUT win for {winner.Value}");
                            }
                            // Priority 3: Regular win
                            else
                            {
                                ShowWinAnimation(winner.Value, false);
                                DebugLog($"[CustomScoreboard] Showing REGULAR win for {winner.Value}");
                            }
                            
                            // Populate player shots from UIScoreboard before showing summary
                            TryUpdateShotsFromStatsMod();
                            
                            // Update Stats mod data IMMEDIATELY before it gets cleared
                            UpdateStatsFromStatsMod();
                            DebugLog("[CustomScoreboard] Updated Stats mod data before game end");
                            
                            DebugLog("[CustomScoreboard] About to call ShowEndOfGameSummary");

                            // Show end-of-game summary immediately
                            ShowEndOfGameSummary();

                            // Reset shots after showing summary
                            blueTeamShots = 0;
                            redTeamShots = 0;
                            SetBlueShots(0);
                            SetRedShots(0);
                            DebugLog("[CustomScoreboard] Reset shot counters on game end");
                        });
                }
            }
            else if ((isFaceOffPhase || isPlayPhase) && wasWarmupOrIntermission)
            {
                // Period is starting
                isInMainMenu = false;
                TurnOn();
                
                if (currentPeriod == 1)
                {
                    // Clear player stats at start of game (period 1)
                    playerGoals.Clear();
                    playerShots.Clear();
                    cachedPlayerStats.Clear();
                    hasShownWinAnimation = false; // Reset win animation flag for new game
                    DebugLog("[CustomScoreboard] Cleared player stats for new game");
                    
                    // Show player lineup for period 1
                    ShowPlayerLineup();
                }
                else if (currentPeriod >= 2)
                {
                    // Show save percentages for periods 2, 3, and OT
                    ShowSavePercentages();
                }
            }
            else
            {
                // Any other phase means we're in a game
                isInMainMenu = false;
                TurnOn();
            }
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in Event_OnGamePhaseChanged: {ex.Message}");
            }
        }

        private void Event_OnGameStateChanged(Dictionary<string, object> message)
        {
            try
            {
            if (config != null && config.enableCustomScoreboard)
            {
                HideOriginalScoreboard();
                _originalScoreboardHidden = true;
            }

            if (!TryGetMessageValue(message, out GameState gameState, "newGameState", "gameState", "state"))
            {
                DebugWarning("[CustomScoreboard] Event_OnGameStateChanged missing game state payload");
                return;
            }

            int previousBlueScore = lastBlueScore;
            int previousRedScore = lastRedScore;
            
            // Check if ShootoutUI exists and is active (PoncePuck.Shootout mod integration)
            try
            {
                System.Type shootoutUIType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    shootoutUIType = assembly.GetType("PoncePuck.Shootout.ShootoutUI");
                    if (shootoutUIType != null) break;
                }
                
                if (shootoutUIType != null)
                {
                    var shootoutUI = FindFirstObjectByType(shootoutUIType);
                    if (shootoutUI != null)
                    {
                        var activeField = shootoutUI.GetType().GetField("isShootoutActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        if (activeField != null)
                        {
                            bool shootoutActive = (bool)activeField.GetValue(shootoutUI);
                            
                            if (shootoutActive && !isShootoutActive)
                            {
                                DebugLog("[CustomScoreboard] Shootout started - changing period label to SHOOTOUT");
                                isShootoutActive = true;
                                shootoutLabelOverride = true;
                                if (periodLabel != null)
                                {
                                    periodLabel.text = "SHOOTOUT";
                                }
                            }
                            else if (!shootoutActive && isShootoutActive)
                            {
                                DebugLog("[CustomScoreboard] Shootout ended");
                                isShootoutActive = false;
                                shootoutLabelOverride = false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error checking ShootoutUI: {ex.Message}");
            }
            
            SetTime(gameState.Tick);

            // Detect phase changes and trigger phase change handling (Event_OnGamePhaseChanged removed in 310)
            if (gameState.Phase != currentPhase)
            {
                var phaseMessage = new Dictionary<string, object> { { "newGamePhase", gameState.Phase } };
                Event_OnGamePhaseChanged(phaseMessage);
            }

            // Update the game state values
            currentPeriod = gameState.Period;
            
            // Check if score went to 0-0 (new game starting mid-game)
            bool scoresWereNonZero = (lastBlueScore > 0 || lastRedScore > 0);
            bool scoresNowZero = (gameState.BlueScore == 0 && gameState.RedScore == 0);
            if (scoresWereNonZero && scoresNowZero && gameState.Period == 1)
            {
                // New game starting mid-game - reset all stats
                blueTeamShots = 0;
                redTeamShots = 0;
                blueSaves = 0;
                redSaves = 0;
                SetBlueShots(0);
                SetRedShots(0);
                playerShots.Clear();
                cachedPlayerStats.Clear();
                DebugLog("[CustomScoreboard] New game detected (0-0 score, period 1) - reset all stats");
            }
            
            // Set period AFTER checking phase, so warmup can override
            if (gameState.Phase == GamePhase.Warmup)
            {
                currentPhase = GamePhase.Warmup;
                if (periodLabel != null && !shootoutLabelOverride)
                {
                    periodLabel.text = "WARM-UP";
                }
            }
            else if (gameState.Phase == GamePhase.Play)
            {
                // Only update to Play if not in shootout
                if (!isShootoutActive)
                {
                    currentPhase = GamePhase.Play;
                }
                SetPeriod(gameState.Period);
            }
            else
            {
                // Update phase for other states
                if (!shootoutLabelOverride)
                {
                    currentPhase = gameState.Phase;
                }
                SetPeriod(gameState.Period);
            }
            
            SetBlueScore(gameState.BlueScore);
            SetRedScore(gameState.RedScore);
            
            // Show scoreboard if we're in a game phase and custom scoreboard is enabled
            if (config != null && config.enableCustomScoreboard && 
                !isInMainMenu && gameState.Phase != GamePhase.None)
            {
                TurnOn();
                DebugLog($"[CustomScoreboard] Showing scoreboard for phase: {gameState.Phase}");
            }

            if (gameState.BlueScore > lastBlueScore)
            {
                TriggerGoalFallbackFromGameState(PlayerTeam.Blue, gameState.BlueScore, previousBlueScore);
            }
            if (gameState.RedScore > lastRedScore)
            {
                TriggerGoalFallbackFromGameState(PlayerTeam.Red, gameState.RedScore, previousRedScore);
            }

            lastBlueScore = gameState.BlueScore;
            lastRedScore = gameState.RedScore;

            // Try to manually trigger shot update by checking Stats mod
            TryUpdateShotsFromStatsMod();
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in Event_OnGameStateChanged: {ex.Message}");
            }
        }

        private void TriggerGoalFallbackFromGameState(PlayerTeam team, int newScore, int oldScore)
        {
            try
            {
                if (newScore <= oldScore)
                {
                    return;
                }

                if (Time.time - _lastGoalAnimationTriggerTime < 0.8f)
                {
                    return;
                }

                _lastGoalAnimationTriggerTime = Time.time;

                bool useMetric = SettingsManager.Units == Units.Metric;
                string unitLabel = useMetric ? "KPH" : "MPH";

                DebugLog($"[CustomScoreboard] Fallback goal trigger from GameState for {team}");

                if (isShootoutActive)
                {
                    ShowShotSpeedPopup(0f, team, unitLabel);
                    return;
                }

                ShowGoalAnimation(team, () =>
                {
                    ShowShotSpeedPopup(0f, team, unitLabel);
                }, false);
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in fallback goal trigger: {ex.Message}");
            }
        }
        
        // Force a sync from the live GameManager.GameState (important when joining mid-game).
        // Replaces the old text-scraping of native UIScoreboard labels — we have the real
        // NetworkVariable<GameState> right here, so just feed it through the same handler the
        // event would have called.
        private void PollGameState()
        {
            try
            {
                var gm = NetworkBehaviourSingleton<GameManager>.Instance;
                if (gm == null || gm.GameState == null) return;

                Event_OnGameStateChanged(new Dictionary<string, object>
                {
                    { "newGameState", gm.GameState.Value }
                });
            }
            catch (Exception e)
            {
                DebugWarning($"[CustomScoreboard] PollGameState error: {e.Message}");
            }
        }

        private void Event_OnGoalScored(Dictionary<string, object> message)
        {
            try
            {
                Debug.Log($"[Scoreboard] Event_OnGoalScored fired. Keys: {string.Join(", ", message.Keys)}");

                // Defensive team extraction — B310 uses "byTeam", older builds used "team"
                PlayerTeam team = PlayerTeam.Blue;
                if (message.ContainsKey("byTeam"))
                    team = (PlayerTeam)message["byTeam"];
                else if (message.ContainsKey("team"))
                    team = (PlayerTeam)message["team"];
                else
                    Debug.LogWarning("[Scoreboard] Goal event missing team key — defaulting to Blue");

                _lastGoalAnimationTriggerTime = Time.time;

                // Store the scoring team so the shot speed chat message handler knows which team to credit
                _pendingShotSpeedTeam = team;

                // Prevent multiple animations from playing at once
                if (isAnimationPlaying)
                {
                    DebugLog("[CustomScoreboard] Animation already in progress - skipping this goal animation");
                    return;
                }

                // Cache the last goal for /summaryS command
                lastGoalMessage = new Dictionary<string, object>(message);
                lastGoalMessage["team"] = team;
                DebugLog($"[CustomScoreboard] Cached lastGoalMessage for team {team}");

                // Track player goals for HAT TRICK detection
                bool isHatTrick = false;
                if (message.ContainsKey("goalPlayer") && message["goalPlayer"] is Player goalPlayer)
                {
                    ulong goalPlayerClientId = goalPlayer.OwnerClientId;
                    if (!playerGoals.ContainsKey(goalPlayerClientId))
                        playerGoals[goalPlayerClientId] = 0;
                    playerGoals[goalPlayerClientId]++;

                    if (playerGoals[goalPlayerClientId] == 3)
                    {
                        isHatTrick = true;
                        DebugLog($"[CustomScoreboard] HAT TRICK! Player {goalPlayer.Username.Value} has scored 3 goals!");
                    }
                }

                // During shootouts, skip goal animation (shot speed popup comes from chat message handler)
                if (isShootoutActive)
                {
                    DebugLog($"[CustomScoreboard] Shootout goal scored by {team}");
                    return;
                }

                bool isOvertime = (currentPeriod > 3);

                ShowGoalAnimation(team, () => {
                    if (isOvertime)
                    {
                        ShowOTWinAnimation(team, () => {
                            ShowScoringSummary(lastGoalMessage);
                        });
                    }
                    else
                    {
                        // Shot speed popup is shown by OnShotSpeedChatMessage when the server
                        // sends "Goal scored! X KPH from the stick." via UIAnnouncements.
                        // Fall back to a no-speed popup after 3s if the announcement never arrives.
                        DOTween.Sequence()
                            .SetTarget(this)
                            .AppendInterval(3f)
                            .OnComplete(() => {
                                // Only show fallback if shot speed popup hasn't already been shown
                                // (OnShotSpeedChatMessage clears _pendingShotSpeedTeam when it fires)
                                if (_pendingShotSpeedTeam.HasValue)
                                {
                                    string unitLabel = GetSpeedUnitLabel();
                                    ShowShotSpeedPopup(0f, team, unitLabel);
                                    _pendingShotSpeedTeam = null;
                                }
                            });

                        DOTween.Sequence()
                            .SetTarget(this)
                            .AppendInterval(15f)
                            .OnComplete(() => ShowScoringSummary(lastGoalMessage));
                    }
                }, isHatTrick);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scoreboard] Error in Event_OnGoalScored: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Called when the server sends the goal speed chat message.
        /// 310 format: "Goal scored! &lt;b&gt;&lt;united&gt;X&lt;/united&gt; &amp;units&lt;/b&gt; across the line, &lt;b&gt;&lt;united&gt;Y&lt;/united&gt; &amp;units&lt;/b&gt; from the stick."
        /// The numeric value is in game units (m/s) and must be converted to the player's preferred unit.
        /// </summary>
        public void OnShotSpeedChatMessage(string chatMessage)
        {
            try
            {
                // Grab the LAST <united>...</united> before "from the stick".
                // The format puts "</b> " (and possibly other rich-text tags) between the value and the phrase,
                // so we can't restrict to [^<]* — match any chars non-greedily up to "from the stick".
                var match = System.Text.RegularExpressions.Regex.Match(
                    chatMessage,
                    @"<united>(\d+(?:\.\d+)?)</united>(?:(?!<united>).)*?from the stick",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

                if (!match.Success)
                {
                    DebugLog($"[CustomScoreboard] OnShotSpeedChatMessage: no speed match in: {chatMessage}");
                    return;
                }

                if (!float.TryParse(match.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float rawMs) || rawMs <= 0f)
                    return;

                // GameUnitsToMetric multiplies by 3.6, GameUnitsToImperial by 2.2369363.
                bool useMetric = SettingsManager.Units == Units.Metric;
                float speed = useMetric ? rawMs * 3.6f : rawMs * 2.2369363f;
                string unitLabel = useMetric ? "KPH" : "MPH";

                PlayerTeam team = _pendingShotSpeedTeam ?? PlayerTeam.Blue;
                _pendingShotSpeedTeam = null;

                if (team == PlayerTeam.Blue) lastBlueShotSpeed = speed;
                else lastRedShotSpeed = speed;

                DebugLog($"[CustomScoreboard] Shot speed: {rawMs:F2} m/s → {speed:F0} {unitLabel} for {team}");
                ShowShotSpeedPopup(speed, team, unitLabel);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scoreboard] Error in OnShotSpeedChatMessage: {ex.Message}");
            }
        }

        private void Event_OnStatsTrigger(Dictionary<string, object> message)
        {
            try
            {
            // The Stats mod triggers this event with shot and save data
            // Log all keys to see what data is available
            DebugLog("[CustomScoreboard] Event_OnStatsTrigger received with keys: " + string.Join(", ", message.Keys));
            
            foreach (var key in message.Keys)
            {
                DebugLog($"[CustomScoreboard] Event_OnStatsTrigger - {key}: {message[key]}");
            }
            
            // Try to extract team shot data - try various possible key names
            if (message.ContainsKey("blueShots"))
            {
                int blueShots = (int)message["blueShots"];
                SetBlueShots(blueShots);
                DebugLog($"[CustomScoreboard] Blue shots updated: {blueShots}");
            }
            
            if (message.ContainsKey("redShots"))
            {
                int redShots = (int)message["redShots"];
                SetRedShots(redShots);
                DebugLog($"[CustomScoreboard] Red shots updated: {redShots}");
            }
            
            // Alternative: try accessing sog array
            if (message.ContainsKey("sog"))
            {
                UpdateTeamShotsFromSOG(message["sog"]);
            }
            
            // Process individual player stats from Stats mod
            // Stats mod sends data with keys like: "playerSteamId", "sog", "hit", "pass", "takeaway", "turnover", "sticksv"
            if (message.ContainsKey("playerSteamId"))
            {
                string steamId = message["playerSteamId"].ToString();
                
                // Hits
                if (message.ContainsKey("hit"))
                {
                    int hits = Convert.ToInt32(message["hit"]);
                    playerHits[steamId] = hits;
                    DebugLog($"[CustomScoreboard] Updated hits for {steamId}: {hits}");
                }
                
                // Passes
                if (message.ContainsKey("pass"))
                {
                    int passes = Convert.ToInt32(message["pass"]);
                    playerPasses[steamId] = passes;
                    DebugLog($"[CustomScoreboard] Updated passes for {steamId}: {passes}");
                }
                
                // Takeaways
                if (message.ContainsKey("takeaway"))
                {
                    int takeaways = Convert.ToInt32(message["takeaway"]);
                    playerTakeaways[steamId] = takeaways;
                    DebugLog($"[CustomScoreboard] Updated takeaways for {steamId}: {takeaways}");
                }
                
                // Turnovers
                if (message.ContainsKey("turnover"))
                {
                    int turnovers = Convert.ToInt32(message["turnover"]);
                    playerTurnovers[steamId] = turnovers;
                    DebugLog($"[CustomScoreboard] Updated turnovers for {steamId}: {turnovers}");
                }
                
                // Stick saves (for goalies)
                // No stick saves data - Stats mod doesn't track this
            }
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in Event_OnStatsTrigger: {ex.Message}");
            }
        }
        
        private void UpdateTeamShotsFromSOG(object sogData)
        {
            // Try to parse the SOG data and sum by team
            try
            {
                DebugLog($"[CustomScoreboard] SOG data type: {sogData.GetType()}");
                // The sog data might be a list of {Key: steamId, Value: shotCount}
                // We need to match each player's steamId to their team and sum
                
                int blueShotsTotal = 0;
                int redShotsTotal = 0;
                
                // Try to access PlayerManager to get player teams
                var playerManager = GetPlayerManager();
                if (playerManager != null)
                {
                    var players = playerManager.GetPlayers(false);
                    
                    // sogData should be iterable - try to cast it
                    if (sogData is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            // Each item should have Key (steamId) and Value (shots)
                            var itemType = item.GetType();
                            var keyProp = itemType.GetProperty("Key");
                            var valueProp = itemType.GetProperty("Value");
                            
                            if (keyProp != null && valueProp != null)
                            {
                                string steamId = keyProp.GetValue(item)?.ToString();
                                int shots = Convert.ToInt32(valueProp.GetValue(item));
                                
                                // Find player with this steam ID and add to their team's total
                                var player = players.FirstOrDefault(p => p.SteamId.Value.ToString() == steamId);
                                if (player != null)
                                {
                                    if (player.Team == PlayerTeam.Blue)
                                        blueShotsTotal += shots;
                                    else if (player.Team == PlayerTeam.Red)
                                        redShotsTotal += shots;
                                }
                            }
                        }
                        
                        SetBlueShots(blueShotsTotal);
                        SetRedShots(redShotsTotal);
                        DebugLog($"[CustomScoreboard] Team shots calculated - Blue: {blueShotsTotal}, Red: {redShotsTotal}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error parsing SOG data: {ex.Message}");
            }
        }

        private void TryUpdateShotsFromStatsMod()
        {
            // Read shot data directly from the UIScoreboard where Stats mod displays it
            LogShotData("Starting TryUpdateShotsFromStatsMod - looking for shots in UIScoreboard");
            
            try
            {
                var uiScoreboard = FindFirstObjectByType<UIScoreboard>();
                var playerManager = GetPlayerManager();
                if (uiScoreboard != null && playerManager != null)
                {
                    var players = playerManager.GetPlayers(false);
                    int blueShots = 0;
                    int redShots = 0;
                    int blueSavesTotal = 0;
                    int redSavesTotal = 0;
                    
                    LogShotData($"Found {players.Count} players in PlayerManager");
                    
                    // Access the playerVisualElementMap using reflection
                    var playerMapField = GetPrivateInstanceField(typeof(UIScoreboard), "playerVisualElementMap");
                    
                    if (playerMapField != null)
                    {
                        var playerMap = playerMapField.GetValue(uiScoreboard) as Dictionary<Player, VisualElement>;
                        
                        if (playerMap != null)
                        {
                            LogShotData($"playerVisualElementMap found with {playerMap.Count} entries");
                            
                            foreach (var kvp in playerMap)
                            {
                                Player player = kvp.Key;
                                VisualElement playerRow = kvp.Value;
                                
                                // Try to find the SOG label that Stats mod adds
                                // It might be in a container with class or name containing "SOG" or "Shots"
                                var allLabels = playerRow.Query<Label>().ToList();
                                var sogLabel = allLabels.Where(l => 
                                    l.name != null && (l.name.Contains("SOG") || l.name.Contains("Shot"))).FirstOrDefault();
                                
                                // If no named label, try to find by position (Stats mod adds columns)
                                if (sogLabel == null)
                                {
                                    // The SOG/S% column is after Goals, Assists, Points, Ping columns
                                    // SOG should be around index 7-8 (after standard columns)
                                    if (allLabels.Count > 7)
                                    {
                                        sogLabel = allLabels[7]; // Adjust index if needed
                                    }
                                }
                                
                                if (sogLabel != null && !string.IsNullOrEmpty(sogLabel.text))
                                {
                                    // Parse the SOG value (might be "5" or "5/100%" format)
                                    string sogText = sogLabel.text.Split('/')[0].Trim();
                                    if (int.TryParse(sogText, out int shots))
                                    {
                                        if (player.Team == PlayerTeam.Blue)
                                            blueShots += shots;
                                        else if (player.Team == PlayerTeam.Red)
                                            redShots += shots;
                                        
                                        LogShotData($"Player {player.Username.Value} ({player.Team}): {shots} shots, clientId={player.OwnerClientId}");
                                        playerShots[player.OwnerClientId] = shots;
                                    }
                                }
                                
                                // Try to find stick saves label/column
                                var savesLabels = playerRow.Query<Label>().ToList();
                                var savesLabel = savesLabels.Where(l => 
                                    l.name != null && (l.name.Contains("save") || l.name.Contains("Save"))).FirstOrDefault();
                                
                                if (savesLabel == null)
                                {
                                    // Try to find by position (saves might be in next column after SOG)
                                    if (savesLabels.Count > 8)
                                    {
                                        savesLabel = savesLabels[8]; // Adjust index if needed
                                    }
                                }
                                
                                if (savesLabel != null && !string.IsNullOrEmpty(savesLabel.text))
                                {
                                    // Parse save count
                                    string savesText = savesLabel.text.Trim();
                                    if (int.TryParse(savesText, out int saves))
                                    {
                                        if (player.Team == PlayerTeam.Blue)
                                            blueSavesTotal += saves;
                                        else if (player.Team == PlayerTeam.Red)
                                            redSavesTotal += saves;
                                        
                                        DebugLog($"[CustomScoreboard] Player {player.Username.Value} ({player.Team}): {saves} saves");
                                    }
                                }
                            }
                            
                            LogShotData($"Total shots from scoreboard - Blue: {blueShots}, Red: {redShots}");
                            LogShotData($"Total saves from scoreboard - Blue: {blueSavesTotal}, Red: {redSavesTotal}");
                            
                            // Only update if the new value is higher (don't decrease when players leave)
                            if (blueShots > blueTeamShots)
                            {
                                blueTeamShots = blueShots;
                                SetBlueShots(blueShots);
                            }
                            if (redShots > redTeamShots)
                            {
                                redTeamShots = redShots;
                                SetRedShots(redShots);
                            }
                            if (blueSavesTotal > blueSaves)
                            {
                                blueSaves = blueSavesTotal;
                            }
                            if (redSavesTotal > redSaves)
                            {
                                redSaves = redSavesTotal;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogShotData($"ERROR reading shots from scoreboard: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Tries to read a value from the event message dictionary using one or more candidate keys.
        /// Returns true and sets <paramref name="value"/> when the first matching key is found.
        /// </summary>
        private static bool TryGetMessageValue<T>(Dictionary<string, object> message, out T value, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (message.ContainsKey(key) && message[key] is T typedValue)
                {
                    value = typedValue;
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
}
