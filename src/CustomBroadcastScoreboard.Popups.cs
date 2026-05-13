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
        // STAT POPUP METHODS
        // ============================================
        
        private void ShowShotSpeedPopup(float speed, PlayerTeam team, string unitLabel)
        {
            try
            {
                ShowStatPopup($"{speed:F0}", team, unitLabel);
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error showing shot speed popup: {ex.Message}");
            }
        }
        
        private string GetSpeedUnitLabel()
        {
            bool useMetric = SettingsManager.Units == Units.Metric;
            return useMetric ? "KPH" : "MPH";
        }
        
        private float ConvertSpeedToDisplayUnits(float speedMps)
        {
            bool useMetric = SettingsManager.Units == Units.Metric;
            return useMetric ? speedMps * 3.6f : speedMps * 2.23694f;
        }

        private void ShowSavePercentages()
        {
            // Read save percentages directly from the Stats mod display on UIScoreboard
            DebugLog("[CustomScoreboard] Reading save percentages from UIScoreboard for goalies");
            
            try
            {
                var uiScoreboard = FindFirstObjectByType<UIScoreboard>();
                if (uiScoreboard != null && MonoBehaviourSingleton<PlayerManager>.Instance != null)
                {
                    // Access the playerVisualElementMap using reflection
                    var playerMapField = GetPrivateInstanceField(typeof(UIScoreboard), "playerVisualElementMap");
                    
                    if (playerMapField != null)
                    {
                        var playerMap = playerMapField.GetValue(uiScoreboard) as Dictionary<Player, VisualElement>;
                        
                        if (playerMap != null)
                        {
                            string blueSavePercent = null;
                            string redSavePercent = null;
                            
                            foreach (var kvp in playerMap)
                            {
                                Player player = kvp.Key;
                                VisualElement playerRow = kvp.Value;
                                
                                // Check if player is a goalie
                                bool isGoalie = player.PlayerPosition != null && player.PlayerPosition.Role == PlayerRole.Goalie;
                                
                                if (isGoalie)
                                {
                                    // Get all labels from the player row
                                    var allLabels = playerRow.Query<Label>().ToList();
                                    
                                    // Look for save percentage - it's a decimal number like "0.923"
                                    foreach (var label in allLabels)
                                    {
                                        if (!string.IsNullOrEmpty(label.text))
                                        {
                                            string text = label.text.Trim();
                                            
                                            // Check if it's a decimal number (contains '.' and is between 0 and 1)
                                            if (text.Contains(".") && float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float savePercent))
                                            {
                                                // Save percentages are typically between 0.000 and 1.000
                                                if (savePercent >= 0f && savePercent <= 1f)
                                                {
                                                    if (player.Team == PlayerTeam.Blue)
                                                    {
                                                        blueSavePercent = text;
                                                        DebugLog($"[CustomScoreboard] Found Blue goalie {player.Username.Value} with SV%: {text}");
                                                    }
                                                    else if (player.Team == PlayerTeam.Red)
                                                    {
                                                        redSavePercent = text;
                                                        DebugLog($"[CustomScoreboard] Found Red goalie {player.Username.Value} with SV%: {text}");
                                                    }
                                                    break; // Found the save percentage for this goalie
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            
                            // Show the save percentages
                            if (blueSavePercent != null)
                            {
                                ShowStatPopup(blueSavePercent, PlayerTeam.Blue, "SV%");
                            }
                            
                            if (redSavePercent != null)
                            {
                                ShowStatPopup(redSavePercent, PlayerTeam.Red, "SV%");
                            }
                            
                            if (blueSavePercent == null && redSavePercent == null)
                            {
                                DebugWarning("[CustomScoreboard] No goalie save percentages found on scoreboard");
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error reading save percentages: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ShowStatPopup(string text, PlayerTeam team, string suffix = "")
        {
            try
            {
                if (config == null || !config.enablePopups)
                {
                    DebugWarning("[CustomScoreboard] Popups disabled or config null");
                    return;
                }

                VisualElement popup = team == PlayerTeam.Blue ? blueStatPopup : redStatPopup;
                Label label = team == PlayerTeam.Blue ? blueStatLabel : redStatLabel;

                if (popup == null || label == null)
                {
                    DebugWarning($"[CustomScoreboard] Popup or label is null for team {team}");
                    return;
                }

                // Kill any in-flight animation on this popup so back-to-back calls don't race.
                DOTween.Kill(popup);

                label.text = suffix != "" ? $"{text} {suffix}" : text;
                popup.style.backgroundColor = GetTeamColor(team);
                label.style.color = GetTeamTextColor(team);

                // Reset to hidden-behind-scorebug position. Popup is a child of scoreboardContainer
                // so left/top are scoreboard-local; it follows the scorebug automatically.
                popup.style.top = 0;
                popup.style.display = DisplayStyle.Flex;

                DOTween.Sequence()
                    .SetTarget(popup)
                    .Append(DOTween.To(() => popup.style.top.value.value,
                        y => popup.style.top = y, ScorebugAnchor.StatPopupSlideTo, 0.3f).SetEase(Ease.OutCubic))
                    .AppendInterval(3f)
                    .Append(DOTween.To(() => popup.style.top.value.value,
                        y => popup.style.top = y, 0f, 0.4f).SetEase(Ease.InCubic))
                    .OnComplete(() => popup.style.display = DisplayStyle.None)
                    .OnKill(() => popup.style.display = DisplayStyle.None);
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error showing stat popup: {ex.Message}");
            }
        }

        // ============================================
        // LINEUP POPUP METHODS
        // ============================================
        
        private void ShowPlayerLineup()
        {
            try
            {
                if (config == null || !config.enablePopups)
                {
                    DebugWarning("[CustomScoreboard] Config is null or popups disabled, cannot show lineup");
                    return;
                }
            
            DebugLog("[CustomScoreboard] Showing player lineup for period 1");
            
            // Get all players
            var playerManager = GetPlayerManager();
            if (playerManager == null)
            {
                DebugWarning("[CustomScoreboard] PlayerManager is null");
                return;
            }
            
            var players = playerManager.GetPlayers(false);
            DebugLog($"[CustomScoreboard] Found {players.Count} total players");
            
            // Separate players by team
            var bluePlayers = players.Where(p => p.Team == PlayerTeam.Blue).ToList();
            var redPlayers = players.Where(p => p.Team == PlayerTeam.Red).ToList();
            
            DebugLog($"[CustomScoreboard] Blue: {bluePlayers.Count}, Red: {redPlayers.Count}");
            
            if (bluePlayers.Count == 0 && redPlayers.Count == 0)
            {
                DebugWarning("[CustomScoreboard] No players on either team");
                return;
            }
            
            // Lineup popup is anchored to scoreboardContainer so it follows the scorebug
            // automatically (no scoreboardX/Y/scale math needed).
            if (scoreboardContainer == null) return;

            VisualElement lineupPopup = new VisualElement();
            lineupPopup.style.position = Position.Absolute;
            lineupPopup.style.width = ScorebugAnchor.CenteredPopupWidth;
            lineupPopup.style.height = config.lineupPopupHeight;
            lineupPopup.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            // No horizontal padding: child sections are absolutely positioned in scorebug-local
            // pixels so they sit exactly under the matching scorebug sections.
            lineupPopup.style.paddingTop = 5;
            lineupPopup.style.paddingBottom = 5;
            lineupPopup.pickingMode = PickingMode.Ignore;

            // Anchored to scorebug; top:0 hides behind scorebug initially.
            float startY = 0f;
            float endY = ScorebugAnchor.LineupSlideTo;
            lineupPopup.style.top = startY;
            lineupPopup.style.left = ScorebugAnchor.CenteredPopupLeft;

            UnityEngine.Font uiFont = GetUIFont();

            // Sections are absolutely positioned to align exactly with the scorebug layout:
            //   blue section:   x=0   to 280  (matches scorebug blue 0-280)
            //   logo gap:       x=280 to 300  (empty, matches league logo strip)
            //   red section:    x=300 to 580  (matches scorebug red 300-580)
            //   production:     x=580 to 780  (matches scorebug period 580-660 + time 660-780)
            VisualElement mainRow = new VisualElement();
            mainRow.style.position = Position.Absolute;
            mainRow.style.left = 0;
            mainRow.style.right = 0;
            mainRow.style.top = 5;
            mainRow.style.bottom = 5;

            VisualElement blueSection = new VisualElement();
            blueSection.style.position = Position.Absolute;
            blueSection.style.left = 0;
            blueSection.style.width = 280;
            blueSection.style.top = 0;
            blueSection.style.bottom = 0;
            blueSection.style.flexDirection = FlexDirection.Column;
            
            // Blue team name
            Label blueTeamName = new Label(config.blueTeamName);
            blueTeamName.style.fontSize = 10;
            blueTeamName.style.unityFontStyleAndWeight = FontStyle.Bold;
            blueTeamName.style.color = GetBlueTeamColor();
            blueTeamName.style.unityTextAlign = TextAnchor.MiddleCenter;
            blueTeamName.style.marginBottom = 3;
            if (uiFont != null) blueTeamName.style.unityFont = uiFont;
            blueSection.Add(blueTeamName);
            
            // Blue players in formation: LW/C/RW, LD/RD, G
            AddPlayerFormation(blueSection, bluePlayers, uiFont);
            
            mainRow.Add(blueSection);
            
            VisualElement redSection = new VisualElement();
            redSection.style.position = Position.Absolute;
            redSection.style.left = 300;
            redSection.style.width = 280;
            redSection.style.top = 0;
            redSection.style.bottom = 0;
            redSection.style.flexDirection = FlexDirection.Column;
            
            // Red team name
            Label redTeamName = new Label(config.redTeamName);
            redTeamName.style.fontSize = 10;
            redTeamName.style.unityFontStyleAndWeight = FontStyle.Bold;
            redTeamName.style.color = GetRedTeamColor();
            redTeamName.style.unityTextAlign = TextAnchor.MiddleCenter;
            redTeamName.style.marginBottom = 3;
            if (uiFont != null) redTeamName.style.unityFont = uiFont;
            redSection.Add(redTeamName);
            
            // Red players in formation: LW/C/RW, LD/RD, G
            AddPlayerFormation(redSection, redPlayers, uiFont);
            
            mainRow.Add(redSection);
            
            VisualElement productionSection = new VisualElement();
            productionSection.style.position = Position.Absolute;
            productionSection.style.left = 580;
            productionSection.style.width = 200;
            productionSection.style.top = 0;
            productionSection.style.bottom = 0;
            productionSection.style.flexDirection = FlexDirection.Column;
            productionSection.style.justifyContent = Justify.Center;
            productionSection.style.paddingLeft = 10;
            productionSection.style.borderLeftWidth = 1;
            productionSection.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            
            // Play-by-Play (title LEFT, name RIGHT)
            VisualElement pbpRow = new VisualElement();
            pbpRow.style.flexDirection = FlexDirection.Row;
            pbpRow.style.marginBottom = 2;
            pbpRow.style.justifyContent = Justify.SpaceBetween;
            
            Label pbpLabel = new Label("Play-by-Play");
            pbpLabel.style.fontSize = 7;
            pbpLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            if (uiFont != null) pbpLabel.style.unityFont = uiFont;
            pbpRow.Add(pbpLabel);
            
            Label pbpName = new Label(config.playByPlayName);
            pbpName.style.fontSize = 8;
            pbpName.style.color = Color.white;
            pbpName.style.marginLeft = 5;
            if (uiFont != null) pbpName.style.unityFont = uiFont;
            pbpRow.Add(pbpName);
            
            productionSection.Add(pbpRow);
            
            // Color Commentator (title LEFT, name RIGHT)
            VisualElement colorRow = new VisualElement();
            colorRow.style.flexDirection = FlexDirection.Row;
            colorRow.style.marginBottom = 2;
            colorRow.style.justifyContent = Justify.SpaceBetween;
            
            Label colorLabel = new Label("Color");
            colorLabel.style.fontSize = 7;
            colorLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            if (uiFont != null) colorLabel.style.unityFont = uiFont;
            colorRow.Add(colorLabel);
            
            Label colorName = new Label(config.colorCommentatorName);
            colorName.style.fontSize = 8;
            colorName.style.color = Color.white;
            colorName.style.marginLeft = 5;
            if (uiFont != null) colorName.style.unityFont = uiFont;
            colorRow.Add(colorName);
            
            productionSection.Add(colorRow);
            
            // Producer (title LEFT, name RIGHT)
            VisualElement producerRow = new VisualElement();
            producerRow.style.flexDirection = FlexDirection.Row;
            producerRow.style.justifyContent = Justify.SpaceBetween;
            
            Label producerLabel = new Label("Production");
            producerLabel.style.fontSize = 7;
            producerLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            if (uiFont != null) producerLabel.style.unityFont = uiFont;
            producerRow.Add(producerLabel);
            
            Label producerName = new Label(config.producerName);
            producerName.style.fontSize = 8;
            producerName.style.color = Color.white;
            producerName.style.marginLeft = 5;
            if (uiFont != null) producerName.style.unityFont = uiFont;
            producerRow.Add(producerName);
            
            productionSection.Add(producerRow);
            
            mainRow.Add(productionSection);
            lineupPopup.Add(mainRow);
            
            // Insert at index 0 so the popup renders BEHIND the scorebug's flex children,
            // giving the slide-from-behind look as it animates downward.
            scoreboardContainer.Insert(0, lineupPopup);
            
            // Animate slide down, stay visible, then slide back up and remove.
            // SetTarget+OnKill ensures the popup is removed even if the tween is killed mid-flight
            // (mod disable, scene change, etc.) — otherwise it would leak in the DOM.
            DOTween.Sequence()
                .SetTarget(lineupPopup)
                .Append(DOTween.To(() => lineupPopup.style.top.value.value,
                    y => lineupPopup.style.top = y, endY, 0.9f))
                .AppendInterval(5f) // Show longer for roster
                .Append(DOTween.To(() => lineupPopup.style.top.value.value,
                    y => lineupPopup.style.top = y, startY, 0.9f))
                .OnComplete(() => { if (lineupPopup.parent != null) lineupPopup.RemoveFromHierarchy(); })
                .OnKill(() => { if (lineupPopup.parent != null) lineupPopup.RemoveFromHierarchy(); });
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in ShowPlayerLineup: {ex.Message}");
            }
        }
        
        private string GetPlayerPosition(Player player)
        {
            try
            {
                // Access the PlayerPosition property which contains Name (LD, RD, LW, RW, C, G, etc.)
                if (player.PlayerPosition != null)
                {
                    string positionName = player.PlayerPosition.Name;
                    if (!string.IsNullOrEmpty(positionName))
                    {
                        return positionName.ToUpper();
                    }
                    
                    // Fallback to Role if Name is empty
                    if (player.PlayerPosition.Role == PlayerRole.Goalie)
                    {
                        return "G";
                    }
                }
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error getting player position: {ex.Message}");
            }
            
            // Default fallback
            return "F"; // Forward
        }
        
        private string GetPlayerPositionName(Player player)
        {
            try
            {
                if (player != null && player.PlayerPosition != null)
                {
                    string positionName = player.PlayerPosition.Name;
                    if (!string.IsNullOrEmpty(positionName))
                    {
                        // Map abbreviations to full names
                        switch (positionName.ToUpper())
                        {
                            case "LW": return "LeftWing";
                            case "C": return "Center";
                            case "RW": return "RightWing";
                            case "LD": return "LeftDefense";
                            case "RD": return "RightDefense";
                            case "G": return "Goalie";
                            default: return positionName;
                        }
                    }
                    
                    // Fallback to Role if Name is empty
                    if (player.PlayerPosition.Role == PlayerRole.Goalie)
                    {
                        return "Goalie";
                    }
                }
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error getting player position name: {ex.Message}");
            }
            
            // Default fallback
            return "Forward";
        }
        
        private void AddPlayerFormation(VisualElement container, List<Player> players, UnityEngine.Font uiFont)
        {
            // Group players by position
            var forwards = new Dictionary<string, Player>
            {
                { "LW", players.FirstOrDefault(p => GetPlayerPosition(p) == "LW") },
                { "C", players.FirstOrDefault(p => GetPlayerPosition(p) == "C") },
                { "RW", players.FirstOrDefault(p => GetPlayerPosition(p) == "RW") }
            };
            
            var defense = new Dictionary<string, Player>
            {
                { "LD", players.FirstOrDefault(p => GetPlayerPosition(p) == "LD") },
                { "RD", players.FirstOrDefault(p => GetPlayerPosition(p) == "RD") }
            };
            
            var goalie = players.FirstOrDefault(p => GetPlayerPosition(p) == "G");
            
            // Forwards row (LW, C, RW)
            VisualElement forwardRow = new VisualElement();
            forwardRow.style.flexDirection = FlexDirection.Row;
            forwardRow.style.justifyContent = Justify.SpaceAround;
            forwardRow.style.marginBottom = 2;
            forwardRow.style.alignSelf = Align.Center;
            forwardRow.style.width = new StyleLength(new Length(80, LengthUnit.Percent));
            
            foreach (var kvp in forwards)
            {
                Label playerLabel = new Label(kvp.Value != null ? $"{kvp.Key}: {kvp.Value.Username.Value}" : $"{kvp.Key}: --");
                playerLabel.style.fontSize = 7;
                playerLabel.style.color = kvp.Value != null ? Color.white : new Color(0.5f, 0.5f, 0.5f);
                if (uiFont != null) playerLabel.style.unityFont = uiFont;
                forwardRow.Add(playerLabel);
            }
            container.Add(forwardRow);
            
            // Defense row (LD, RD)
            VisualElement defenseRow = new VisualElement();
            defenseRow.style.flexDirection = FlexDirection.Row;
            defenseRow.style.justifyContent = Justify.SpaceAround;
            defenseRow.style.marginBottom = 2;
            defenseRow.style.alignSelf = Align.Center;
            defenseRow.style.width = new StyleLength(new Length(60, LengthUnit.Percent));
            
            foreach (var kvp in defense)
            {
                Label playerLabel = new Label(kvp.Value != null ? $"{kvp.Key}: {kvp.Value.Username.Value}" : $"{kvp.Key}: --");
                playerLabel.style.fontSize = 7;
                playerLabel.style.color = kvp.Value != null ? Color.white : new Color(0.5f, 0.5f, 0.5f);
                if (uiFont != null) playerLabel.style.unityFont = uiFont;
                defenseRow.Add(playerLabel);
            }
            container.Add(defenseRow);
            
            // Goalie row
            VisualElement goalieRow = new VisualElement();
            goalieRow.style.flexDirection = FlexDirection.Row;
            goalieRow.style.justifyContent = Justify.SpaceAround;
            goalieRow.style.alignSelf = Align.Center;
            goalieRow.style.width = new StyleLength(new Length(40, LengthUnit.Percent));
            
            Label goalieLabel = new Label(goalie != null ? $"G: {goalie.Username.Value}" : "G: --");
            goalieLabel.style.fontSize = 7;
            goalieLabel.style.color = goalie != null ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            if (uiFont != null) goalieLabel.style.unityFont = uiFont;
            goalieRow.Add(goalieLabel);
            container.Add(goalieRow);
        }
        
        private UnityEngine.Font GetUIFont()
        {
            try
            {
                var panelSettings = MonoBehaviourSingleton<UIManager>.Instance.PanelSettings;
                if (panelSettings != null && panelSettings.textSettings != null)
                {
                    var fontAsset = panelSettings.textSettings.defaultFontAsset;
                    if (fontAsset != null)
                    {
                        return fontAsset.sourceFontFile;
                    }
                }
            }
            catch { }
            
            // Fallback to Arial
            return UnityEngine.Font.CreateDynamicFontFromOSFont("Arial", 14);
        }
    }
}
