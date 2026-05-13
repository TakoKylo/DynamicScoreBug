using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

namespace CustomScoreboard.UI
{
    public partial class CustomBroadcastScoreboard
    {
        // ============================================
        // SCORING SUMMARY METHODS
        // ============================================
        
        private void ShowScoringSummary(Dictionary<string, object> goalMessage)
        {
            // Don't show goal summaries during shootouts
            if (isShootoutActive)
            {
                return;
            }
            
            if (config == null || !config.enablePopups)
            {
                DebugWarning("[CustomScoreboard] Config is null or popups disabled");
                return;
            }
            
            DebugLog("[CustomScoreboard] Showing scoring summary");
            
            try
            {
                // Try to get the goal scorer from the event
                Player scorer = null;
                PlayerTeam team = (PlayerTeam)goalMessage["team"];
                
                // goalPlayer is a Player object directly in the message
                if (goalMessage.ContainsKey("goalPlayer") && goalMessage["goalPlayer"] is Player goalPlayerObj)
                {
                    scorer = goalPlayerObj;
                    DebugLog("[CustomScoreboard] ShowScoringSummary - found scorer from goalPlayer: " + scorer.Username.Value);
                }
                else
                {
                    // Log why we couldn't get the scorer
                    if (goalMessage.ContainsKey("goalPlayer"))
                    {
                        var gpObj = goalMessage["goalPlayer"];
                        DebugWarning($"[CustomScoreboard] ShowScoringSummary - goalPlayer exists but is type {gpObj?.GetType()?.Name ?? "null"}, not Player");
                    }
                    else
                    {
                        DebugWarning("[CustomScoreboard] ShowScoringSummary - no goalPlayer key in message. Keys: " + string.Join(", ", goalMessage.Keys));
                    }
                }
                
                if (scorer == null)
                {
                    DebugWarning("[CustomScoreboard] Could not find scorer player - summary not showing");
                    return;
                }
                
                var stats = GetPlayerStats(scorer);
                DebugLog($"[CustomScoreboard] Scorer: {scorer.Username.Value} - Stats: {stats.goals}G {stats.assists}A {stats.points}P");
                
                // Try to get assist player(s) with stats
                string assistText = "Unassisted";
                List<string> assistNames = new List<string>();
                
                // assistPlayer and secondAssistPlayer are Player objects directly in the message
                if (goalMessage.ContainsKey("assistPlayer") && goalMessage["assistPlayer"] is Player assistPlayerObj)
                {
                    var assistStats = GetPlayerStats(assistPlayerObj);
                    assistNames.Add($"{assistPlayerObj.Username.Value} ({assistStats.goals}G {assistStats.assists}A {assistStats.points}P)");
                }
                
                if (goalMessage.ContainsKey("secondAssistPlayer") && goalMessage["secondAssistPlayer"] is Player secondAssistPlayerObj)
                {
                    var assistStats = GetPlayerStats(secondAssistPlayerObj);
                    assistNames.Add($"{secondAssistPlayerObj.Username.Value} ({assistStats.goals}G {assistStats.assists}A {assistStats.points}P)");
                }
                
                // Format assist text
                if (assistNames.Count == 2)
                {
                    assistText = $"Assists: {assistNames[0]}, {assistNames[1]}";
                }
                else if (assistNames.Count == 1)
                {
                    assistText = $"Assist: {assistNames[0]}";
                }
                
                // Anchored to scoreboardContainer so it follows scorebug position/scale.
                if (scoreboardContainer == null) return;

                VisualElement summaryPopup = new VisualElement();
                summaryPopup.style.position = Position.Absolute;
                summaryPopup.style.width = ScorebugAnchor.CenteredPopupWidth;
                summaryPopup.style.height = config.scoringSummaryHeight;
                summaryPopup.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
                summaryPopup.style.flexDirection = FlexDirection.Row;
                summaryPopup.style.justifyContent = Justify.Center;
                summaryPopup.style.alignItems = Align.Center;
                summaryPopup.style.paddingLeft = 10;
                summaryPopup.style.paddingRight = 10;
                summaryPopup.pickingMode = PickingMode.Ignore;

                float startY = 0f;
                float endY = ScorebugAnchor.ScoringSummarySlideTo;
                summaryPopup.style.top = startY;
                summaryPopup.style.left = ScorebugAnchor.CenteredPopupLeft;
                
                UnityEngine.Font uiFont = GetUIFont();
                
                // Use white color for all text
                Label goalLabel = new Label($"GOAL: {scorer.Username.Value} ({stats.goals}G {stats.assists}A {stats.points}P) - {assistText}");
                goalLabel.style.fontSize = 10;
                goalLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                goalLabel.style.color = Color.white;
                if (uiFont != null) goalLabel.style.unityFont = uiFont;
                summaryPopup.Add(goalLabel);
                
                // Insert at index 0 so popup renders behind scorebug (slide-from-behind effect).
                scoreboardContainer.Insert(0, summaryPopup);

                // Animate: slide down, hold, slide up and remove
                DOTween.Sequence()
                    .SetTarget(summaryPopup)
                    .Append(DOTween.To(() => summaryPopup.style.top.value.value,
                        y => summaryPopup.style.top = y, endY, 0.5f))
                    .AppendInterval(5f)
                    .Append(DOTween.To(() => summaryPopup.style.top.value.value,
                        y => summaryPopup.style.top = y, startY, 0.5f))
                    .OnComplete(() => { if (summaryPopup.parent != null) summaryPopup.RemoveFromHierarchy(); })
                    .OnKill(() => { if (summaryPopup.parent != null) summaryPopup.RemoveFromHierarchy(); });
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error showing scoring summary: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // ============================================
        // PERIOD SUMMARY METHODS
        // ============================================
        
        private void ShowPeriodSummary(bool isEndOfGame = false)
        {
            if (config == null || !config.enablePopups)
            {
                DebugWarning("[CustomScoreboard] Config is null or popups disabled");
                return;
            }
            
            DebugLog($"[CustomScoreboard] Showing {(isEndOfGame ? "game" : "period")} summary");
            
            try
            {
                if (MonoBehaviourSingleton<PlayerManager>.Instance == null)
                {
                    DebugWarning("[CustomScoreboard] PlayerManager is null");
                    return;
                }
                
                var players = MonoBehaviourSingleton<PlayerManager>.Instance.GetPlayers(false);
                
                // Get stats for all players with shooting percentage
                var playerStats = new List<(Player player, int goals, int assists, int points, float shootPct)>();
                
                foreach (var player in players)
                {
                    var stats = GetPlayerStats(player);
                    ulong clientId = player.OwnerClientId;
                    string steamId = player.SteamId.Value.ToString();
                    
                    // Get shots from playerSOG (by Steam ID) first, fallback to playerShots (by client ID)
                    int shots = 0;
                    if (playerSOG.ContainsKey(steamId))
                        shots = playerSOG[steamId];
                    else if (playerShots.ContainsKey(clientId))
                        shots = playerShots[clientId];
                    
                    // Calculate shooting percentage (only if we have shot data)
                    float shootPct = 0f;
                    if (shots > 0)
                    {
                        shootPct = (stats.goals / (float)shots) * 100f;
                    }
                    // If we don't have shot data, shootPct stays 0 and won't be displayed
                    
                    playerStats.Add((player, stats.goals, stats.assists, stats.points, shootPct));
                }
                
                // Find top 3 in each category
                var pointLeaders = playerStats.OrderByDescending(p => p.points).ThenByDescending(p => p.goals).Take(3).ToList();
                var goalLeaders = playerStats.OrderByDescending(p => p.goals).ThenByDescending(p => p.points).Take(3).ToList();
                var assistLeaders = playerStats.OrderByDescending(p => p.assists).ThenByDescending(p => p.points).Take(3).ToList();
                
                // Anchored to scoreboardContainer.
                if (scoreboardContainer == null) return;

                if (periodSummaryPopup != null)
                {
                    DOTween.Kill(periodSummaryPopup);
                    if (periodSummaryPopup.parent != null)
                    {
                        periodSummaryPopup.RemoveFromHierarchy();
                    }
                }

                float startY = 0f;
                float endY = ScorebugAnchor.PeriodSummarySlideTo;

                periodSummaryPopup = new VisualElement();
                periodSummaryPopup.style.position = Position.Absolute;
                periodSummaryPopup.style.width = ScorebugAnchor.CenteredPopupWidth;
                periodSummaryPopup.style.height = config.periodSummaryHeight;
                periodSummaryPopup.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
                periodSummaryPopup.style.flexDirection = FlexDirection.Column;
                periodSummaryPopup.style.paddingTop = 4;
                periodSummaryPopup.style.paddingBottom = 4;
                periodSummaryPopup.style.paddingLeft = 8;
                periodSummaryPopup.style.paddingRight = 8;
                periodSummaryPopup.style.top = startY;
                periodSummaryPopup.style.left = ScorebugAnchor.CenteredPopupLeft;
                periodSummaryPopup.pickingMode = PickingMode.Ignore;
                
                UnityEngine.Font uiFont = GetUIFont();
                
                // Categories row (no title - just GOALS, ASSISTS, POINTS as headers)
                VisualElement categoriesRow = new VisualElement();
                categoriesRow.style.flexDirection = FlexDirection.Row;
                categoriesRow.style.justifyContent = Justify.SpaceAround;
                categoriesRow.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                
                // Points leaders
                AddLeaderCategory(categoriesRow, "POINTS", pointLeaders.Select(p => $"{p.player.Username.Value} ({p.points}P)").ToList(), uiFont);
                
                // Goals leaders
                AddLeaderCategory(categoriesRow, "GOALS", goalLeaders.Select(p => {
                    string shootPctStr = p.shootPct > 0 ? $" {p.shootPct:F0}%" : "";
                    return $"{p.player.Username.Value} ({p.goals}G{shootPctStr})";
                }).ToList(), uiFont);
                
                // Assists leaders
                AddLeaderCategory(categoriesRow, "ASSISTS", assistLeaders.Select(p => $"{p.player.Username.Value} ({p.assists}A)").ToList(), uiFont);
                
                periodSummaryPopup.Add(categoriesRow);
                
                // If end of game, add goalie stats row
                if (isEndOfGame)
                {
                    VisualElement goalieRow = new VisualElement();
                    goalieRow.style.flexDirection = FlexDirection.Row;
                    goalieRow.style.justifyContent = Justify.SpaceAround;
                    goalieRow.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                    goalieRow.style.marginTop = 5;
                    
                    // Get goalie stats
                    var goalies = players.Where(p => p.Role == PlayerRole.Goalie).ToList();
                    List<string> goalieStats = new List<string>();
                    
                    foreach (var goalie in goalies)
                    {
                        // Try to get save percentage from Stats mod
                        float savePct = GetGoalieSavePercentage(goalie);
                        if (savePct > 0)
                        {
                            goalieStats.Add($"{goalie.Username.Value} ({savePct:F3} SV%)");
                        }
                    }
                    
                    if (goalieStats.Count > 0)
                    {
                        AddLeaderCategory(goalieRow, "GOALIES", goalieStats, uiFont);
                        periodSummaryPopup.Add(goalieRow);
                    }
                }
                
                // Insert behind scorebug for slide-from-behind effect.
                scoreboardContainer.Insert(0, periodSummaryPopup);

                // Animate: slide down, hold 10 seconds, slide up and remove
                DOTween.Sequence()
                    .SetTarget(periodSummaryPopup) // Set target for proper cleanup
                    .Append(DOTween.To(() => periodSummaryPopup.style.top.value.value,
                        y => periodSummaryPopup.style.top = y, endY, 0.7f))
                    .AppendInterval(10f)
                    .Append(DOTween.To(() => periodSummaryPopup.style.top.value.value,
                        y => periodSummaryPopup.style.top = y, startY, 0.7f))
                    .OnComplete(() => {
                        if (periodSummaryPopup != null)
                        {
                            DOTween.Kill(periodSummaryPopup); // Ensure all animations are killed
                            if (periodSummaryPopup.parent != null)
                            {
                                periodSummaryPopup.RemoveFromHierarchy();
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error showing period summary: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void AddLeaderCategory(VisualElement parent, string title, List<string> leaders, UnityEngine.Font uiFont)
        {
            VisualElement category = new VisualElement();
            category.style.flexDirection = FlexDirection.Column;
            category.style.alignItems = Align.Center;
            category.style.flexGrow = 1;
            category.style.flexBasis = 0;
            category.style.minWidth = 0;
            
            Label categoryTitle = new Label(title);
            categoryTitle.style.fontSize = 8;
            categoryTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            categoryTitle.style.color = new Color(0.7f, 0.7f, 0.7f);
            categoryTitle.style.marginBottom = 2;
            if (uiFont != null) categoryTitle.style.unityFont = uiFont;
            category.Add(categoryTitle);
            
            foreach (var leader in leaders)
            {
                if (string.IsNullOrEmpty(leader) || leader.Contains("(0")) continue; // Skip empty or 0 stats
                
                Label leaderLabel = new Label(leader);
                leaderLabel.style.fontSize = 6;
                leaderLabel.style.color = Color.white;
                leaderLabel.style.marginBottom = 0.5f;
                leaderLabel.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                leaderLabel.style.whiteSpace = WhiteSpace.Normal;
                leaderLabel.style.flexShrink = 1;
                leaderLabel.style.unityTextAlign = TextAnchor.UpperCenter;
                if (uiFont != null) leaderLabel.style.unityFont = uiFont;
                category.Add(leaderLabel);
            }
            
            parent.Add(category);
        }
    }
}
