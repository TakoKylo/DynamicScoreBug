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
                                            if (text.Contains(".") && float.TryParse(text, out float savePercent))
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
                
                if (popupClipContainer == null)
                {
                    DebugWarning("[CustomScoreboard] popupClipContainer is NULL!");
                    return;
                }
                
                DebugLog("[CustomScoreboard] ShowStatPopup called: team=" + team + ", text=" + text + ", suffix=" + suffix);
                DebugLog("[CustomScoreboard] Container childCount=" + popupClipContainer.childCount + ", container parent=" + (popupClipContainer.parent?.name ?? "null"));
            
            // Select the appropriate popup and label for the team
            VisualElement popup = team == PlayerTeam.Blue ? blueStatPopup : redStatPopup;
            Label label = team == PlayerTeam.Blue ? blueStatLabel : redStatLabel;
            
            if (popup == null || label == null) 
            {
                DebugWarning($"[CustomScoreboard] Popup or label is null for team {team}. popup={popup != null}, label={label != null}");
                return;
            }
            
            DebugLog("[CustomScoreboard] Found popup and label, popup is in container children=" + popupClipContainer.Contains(popup));
            DebugLog("[CustomScoreboard] Popup parent name=" + (popup.parent?.name ?? "null") + ", parent is container=" + (popup.parent == popupClipContainer));
            DebugLog("[CustomScoreboard] Container dimensions - width=" + popupClipContainer.style.width.value.value + "%, height=" + popupClipContainer.style.height.value.value + "%");
            DebugLog("[CustomScoreboard] Popup size - width=" + popup.style.width.value.value + ", height=" + popup.style.height.value.value);
            DebugLog("[CustomScoreboard] Popup INITIAL display style=" + popup.style.display.value + ", visible=" + popup.visible);
            
            // Update the label text
            label.text = suffix != "" ? $"{text} {suffix}" : text;
            
            // Set background color based on team using config colors
            Color teamColor = GetTeamColor(team);
            popup.style.backgroundColor = teamColor;
            
            // Set text color based on team using config text colors
            Color textColor = GetTeamTextColor(team);
            label.style.color = textColor;
            
            // Position directly under the shots section for the team using config
            float horizontalOffset = team == PlayerTeam.Blue ? 
                config.scoreboardX + config.blueStatPopupOffsetX : 
                config.scoreboardX + config.redStatPopupOffsetX;
            
            float startY = config.scoreboardY + config.statPopupOffsetY;
            
            // Set initial position
            popup.style.top = startY;
            popup.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            popup.style.translate = new StyleTranslate(new Translate(
                new Length(horizontalOffset, LengthUnit.Pixel), 
                new Length(0, LengthUnit.Pixel)
            ));
            popup.style.scale = new StyleScale(new Scale(new Vector2(config.scoreboardScale, config.scoreboardScale)));
            
            // Show the popup immediately
            popup.style.display = DisplayStyle.Flex;
            DebugLog("[CustomScoreboard] Popup AFTER setting to Flex - display=" + popup.style.display.value + ", visible=" + popup.visible + ", positioned at X=" + horizontalOffset + ", Y=" + startY);
            DebugLog("[CustomScoreboard] Popup will animate from Y=" + startY + " to Y=" + (startY + config.statPopupSlideDistance) + " (distance=" + config.statPopupSlideDistance + ")");
            
            // Animate slide down using configurable distance, stay visible, then slide back up
            DOTween.Sequence()
                .Append(DOTween.To(() => popup.style.top.value.value, 
                    y => popup.style.top = y, startY + config.statPopupSlideDistance, 0.3f))
                .AppendInterval(3f)
                .Append(DOTween.To(() => popup.style.top.value.value, 
                    y => popup.style.top = y, startY, 0.9f))
                .OnComplete(() => 
                {
                    popup.style.display = DisplayStyle.None;
                    DebugLog("[CustomScoreboard] Popup animation complete, display set back to None");
                });
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
            
            // Create lineup popup container spanning full scoreboard width
            VisualElement root = MonoBehaviourSingleton<UIManager>.Instance.RootVisualElement;
            if (root == null) return;
            
            VisualElement lineupPopup = new VisualElement();
            lineupPopup.style.position = Position.Absolute;
            lineupPopup.style.width = config.lineupPopupWidth;
            lineupPopup.style.height = config.lineupPopupHeight;
            lineupPopup.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            lineupPopup.style.flexDirection = FlexDirection.Column;
            lineupPopup.style.paddingTop = 5;
            lineupPopup.style.paddingBottom = 5;
            lineupPopup.style.paddingLeft = 10;
            lineupPopup.style.paddingRight = 10;
            lineupPopup.pickingMode = PickingMode.Ignore;
            
            // Position under scoreboard using config
            float startY = config.scoreboardY;
            float endY = config.scoreboardY + config.lineupPopupSlideDistance;
            
            lineupPopup.style.top = startY;
            lineupPopup.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            lineupPopup.style.translate = new StyleTranslate(new Translate(
                new Length(config.scoreboardX + config.lineupPopupOffsetX - 290f, LengthUnit.Pixel), 
                new Length(0, LengthUnit.Pixel)
            ));
            lineupPopup.style.scale = new StyleScale(new Scale(new Vector2(config.scoreboardScale, config.scoreboardScale)));
            
            // Get font
            UnityEngine.Font uiFont = GetUIFont();
            
            // Main container row: Blue team (left), Red team (center), Production (right)
            // Scoreboard proportions: Blue 280px + Logo 20px + Red 280px = 580px, Time 160px = Total 740px
            // Adjusting to account for borders and padding in the lineup popup
            VisualElement mainRow = new VisualElement();
            mainRow.style.flexDirection = FlexDirection.Row;
            mainRow.style.flexGrow = 1;
            
            // Blue team section (left - under blue side of scoreboard)
            // 280px out of 740px = 37.8%
            VisualElement blueSection = new VisualElement();
            blueSection.style.flexDirection = FlexDirection.Column;
            blueSection.style.flexBasis = new StyleLength(new Length(37.5f, LengthUnit.Percent));
            blueSection.style.flexShrink = 0;
            
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
            
            // Red team section (center - under red side of scoreboard)
            // 280px + 20px (logo space) = 300px out of 740px = 40.5%
            VisualElement redSection = new VisualElement();
            redSection.style.flexDirection = FlexDirection.Column;
            redSection.style.flexBasis = new StyleLength(new Length(41, LengthUnit.Percent));
            redSection.style.flexShrink = 0;
            
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
            
            // Production section (right - under period/time)
            // 160px out of 740px = 21.6%
            VisualElement productionSection = new VisualElement();
            productionSection.style.flexDirection = FlexDirection.Column;
            productionSection.style.flexBasis = new StyleLength(new Length(21.5f, LengthUnit.Percent));
            productionSection.style.flexShrink = 0;
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
            
            // Add to root (before scoreboardContainer so it renders underneath)
            int scoreboardIndex = root.IndexOf(scoreboardContainer);
            if (scoreboardIndex >= 0)
            {
                root.Insert(scoreboardIndex, lineupPopup);
            }
            else
            {
                root.Add(lineupPopup);
            }
            
            // Animate slide down, stay visible, then slide back up and remove
            DOTween.Sequence()
                .Append(DOTween.To(() => lineupPopup.style.top.value.value, 
                    y => lineupPopup.style.top = y, endY, 0.9f))
                .AppendInterval(5f) // Show longer for roster
                .Append(DOTween.To(() => lineupPopup.style.top.value.value, 
                    y => lineupPopup.style.top = y, startY, 0.9f))
                .OnComplete(() => lineupPopup.RemoveFromHierarchy());
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
