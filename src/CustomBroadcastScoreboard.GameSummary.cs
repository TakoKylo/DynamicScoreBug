using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

namespace CustomScoreboard.UI
{
    public partial class CustomBroadcastScoreboard
    {
        private static readonly Color SummaryButtonBg = new Color(57f / 255f, 57f / 255f, 57f / 255f, 1f);

        // ============================================
        // END OF GAME SUMMARY METHODS
        // ============================================
        
        private void ShowEndOfGameSummary()
        {
            LogShotData("ShowEndOfGameSummary called");
            DebugLog("[CustomScoreboard] ===== ShowEndOfGameSummary START =====");
            
            if (config == null || !config.enablePopups)
            {
                LogShotData($"Cannot show summary: config={config}, enablePopups={(config == null ? "null" : config.enablePopups.ToString())}");
                DebugWarning($"[CustomScoreboard] Cannot show summary - config={(config==null?"NULL":"exists")}, enablePopups={(config==null?"N/A":config.enablePopups.ToString())}");
                if (config == null) DebugWarning("[CustomScoreboard] Config is null");
                return;
            }
            
            DebugLog("[CustomScoreboard] Config check passed, creating summary UI");
            
            try
            {
                VisualElement root = GetRootVisualElement();
                if (root == null)
                {
                    DebugWarning("[CustomScoreboard] UIManager.RootVisualElement is NULL - cannot display summary");
                    return;
                }
                DebugLog("[CustomScoreboard] UIManager.RootVisualElement obtained successfully");
                
                // Create summary popup
                VisualElement gameSummary = new VisualElement();
                gameSummary.style.position = Position.Absolute;
                gameSummary.style.width = config.gameSummaryWidth;
                gameSummary.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.98f);
                gameSummary.style.borderBottomWidth = 3;
                gameSummary.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                gameSummary.style.paddingTop = 15;
                gameSummary.style.paddingBottom = 15;
                gameSummary.style.paddingLeft = 20;
                gameSummary.style.paddingRight = 20;
                gameSummary.style.flexDirection = FlexDirection.Column;
                
                // Calculate position based on scoreboard position and config offsets
                float scoreboardCenterX = config.scoreboardX + (740 / 2); // 740 is scoreboard width
                float startX = scoreboardCenterX + config.gameSummaryOffsetX;
                float startY = config.scoreboardY - (config.gameSummarySlideDistance); // Start above final position
                float finalY = config.scoreboardY + config.gameSummaryOffsetY;
                
                // Position at starting point (above final position)
                gameSummary.style.top = startY;
                gameSummary.style.left = startX;
                gameSummary.style.translate = new StyleTranslate(new Translate(new Length(-(config.gameSummaryWidth) / 2, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel)));
                
                UnityEngine.Font uiFont = GetUIFont();
                
                // Title
                // Title with close button header
                VisualElement titleRow = new VisualElement();
                titleRow.style.flexDirection = FlexDirection.Row;
                titleRow.style.justifyContent = Justify.SpaceBetween;
                titleRow.style.alignItems = Align.Center;
                titleRow.style.marginBottom = 15;
                
                Label title = new Label("GAME SUMMARY");
                title.style.fontSize = 18;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.color = Color.white;
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
                title.style.flexGrow = 1;
                if (uiFont != null) title.style.unityFont = uiFont;
                titleRow.Add(title);
                
                // Close button (X)
                Button closeBtn = new Button(() => gameSummary.RemoveFromHierarchy());
                closeBtn.text = "CLOSE";
                closeBtn.style.width = 70;
                closeBtn.style.height = 30;
                closeBtn.style.fontSize = 12;
                closeBtn.style.color = Color.white;
                closeBtn.style.backgroundColor = SummaryButtonBg;
                closeBtn.style.paddingTop = 0;
                closeBtn.style.paddingBottom = 0;
                closeBtn.style.paddingLeft = 0;
                closeBtn.style.paddingRight = 0;
                closeBtn.style.marginLeft = 10;
                closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
                closeBtn.style.whiteSpace = WhiteSpace.NoWrap;
                closeBtn.style.borderTopWidth = 0;
                closeBtn.style.borderBottomWidth = 0;
                closeBtn.style.borderLeftWidth = 0;
                closeBtn.style.borderRightWidth = 0;
                closeBtn.pickingMode = PickingMode.Position; // Ensure the entire button area is clickable
                if (uiFont != null) closeBtn.style.unityFont = uiFont;
                
                // Add hover effect to close button
                closeBtn.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    closeBtn.style.backgroundColor = Color.white;
                    closeBtn.style.color = Color.black;
                });
                closeBtn.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    closeBtn.style.backgroundColor = SummaryButtonBg;
                    closeBtn.style.color = Color.white;
                });
                
                titleRow.Add(closeBtn);
                
                gameSummary.Add(titleRow);
                
                // Stats section
                VisualElement statsRow = new VisualElement();
                statsRow.style.flexDirection = FlexDirection.Row;
                statsRow.style.justifyContent = Justify.SpaceAround;
                statsRow.style.marginBottom = 15;
                
                // Blue team stats
                VisualElement blueStats = CreateTeamStatsColumn(PlayerTeam.Blue, uiFont);
                statsRow.Add(blueStats);
                
                // Divider
                VisualElement divider = new VisualElement();
                divider.style.width = 2;
                divider.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                statsRow.Add(divider);
                
                // Red team stats
                VisualElement redStats = CreateTeamStatsColumn(PlayerTeam.Red, uiFont);
                statsRow.Add(redStats);
                
                gameSummary.Add(statsRow);
                
                // Advanced Stats button (opens full-screen UI)
                Button advancedBtn = new Button();
                advancedBtn.text = "ADVANCED STATS";
                advancedBtn.style.position = Position.Absolute;
                advancedBtn.style.left = 15;
                advancedBtn.style.bottom = 15;
                advancedBtn.style.width = 120;
                advancedBtn.style.height = 28;
                advancedBtn.style.paddingTop = 4;
                advancedBtn.style.paddingBottom = 4;
                advancedBtn.style.paddingLeft = 8;
                advancedBtn.style.paddingRight = 8;
                advancedBtn.style.fontSize = 11;
                advancedBtn.style.color = Color.white;
                advancedBtn.style.backgroundColor = SummaryButtonBg;
                advancedBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
                advancedBtn.style.whiteSpace = WhiteSpace.NoWrap;
                advancedBtn.pickingMode = PickingMode.Position;
                if (uiFont != null) advancedBtn.style.unityFont = uiFont;
                
                // Open advanced stats panel
                advancedBtn.clicked += () =>
                {
                    ShowAdvancedStatsPanel(uiFont);
                };
                
                // Add hover effect to advanced stats button
                advancedBtn.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    advancedBtn.style.backgroundColor = Color.white;
                    advancedBtn.style.color = Color.black;
                });
                advancedBtn.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    advancedBtn.style.backgroundColor = SummaryButtonBg;
                    advancedBtn.style.color = Color.white;
                });
                
                gameSummary.Add(advancedBtn);

                // Three Stars section
                if (!string.IsNullOrEmpty(firstStar) || !string.IsNullOrEmpty(secondStar) || !string.IsNullOrEmpty(thirdStar))
                {
                    Label starsTitle = new Label("☆ THREE STARS ☆");
                    starsTitle.style.fontSize = 14;
                    starsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                    starsTitle.style.color = new Color(1f, 0.8f, 0f);
                    starsTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
                    starsTitle.style.marginTop = 10;
                    starsTitle.style.marginBottom = 10;
                    if (uiFont != null) starsTitle.style.unityFont = uiFont;
                    gameSummary.Add(starsTitle);
                    
                    // Stars list
                    if (!string.IsNullOrEmpty(firstStar))
                    {
                        Label star1 = new Label($"1st: {firstStar}");
                        star1.style.fontSize = 12;
                        star1.style.color = Color.white;
                        star1.style.unityTextAlign = TextAnchor.MiddleCenter;
                        star1.style.marginBottom = 3;
                        if (uiFont != null) star1.style.unityFont = uiFont;
                        gameSummary.Add(star1);
                    }
                    
                    if (!string.IsNullOrEmpty(secondStar))
                    {
                        Label star2 = new Label($"2nd: {secondStar}");
                        star2.style.fontSize = 12;
                        star2.style.color = Color.white;
                        star2.style.unityTextAlign = TextAnchor.MiddleCenter;
                        star2.style.marginBottom = 3;
                        if (uiFont != null) star2.style.unityFont = uiFont;
                        gameSummary.Add(star2);
                    }
                    
                    if (!string.IsNullOrEmpty(thirdStar))
                    {
                        Label star3 = new Label($"3rd: {thirdStar}");
                        star3.style.fontSize = 12;
                        star3.style.color = Color.white;
                        star3.style.unityTextAlign = TextAnchor.MiddleCenter;
                        if (uiFont != null) star3.style.unityFont = uiFont;
                        gameSummary.Add(star3);
                    }
                }
                
                root.Add(gameSummary);
                
                // Animate slide down
                DOTween.Sequence()
                    .Append(DOTween.To(() => gameSummary.style.top.value.value,
                        y => gameSummary.style.top = y, finalY, 0.8f).SetEase(Ease.OutCubic))
                    .AppendInterval(10f) // Stay visible for 10 seconds
                    .Append(DOTween.To(() => gameSummary.style.top.value.value,
                        y => gameSummary.style.top = y, startY, 0.6f).SetEase(Ease.InCubic))
                    .OnComplete(() => gameSummary.RemoveFromHierarchy());
                    
                DebugLog("[CustomScoreboard] End-of-game summary displayed");
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error creating end-of-game summary: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void ShowAdvancedStatsPanel(UnityEngine.Font uiFont)
        {
            // Get root element
            VisualElement root = GetRootVisualElement();
            if (root == null)
            {
                DebugWarning("[CustomScoreboard] UIManager.RootVisualElement is NULL - cannot display advanced stats panel");
                return;
            }
            
            // Close existing panel if any
            if (advancedStatsPanel != null)
            {
                advancedStatsPanel.RemoveFromHierarchy();
                advancedStatsPanel = null;
            }
            
            // Create full-screen overlay panel
            advancedStatsPanel = new VisualElement();
            advancedStatsPanel.name = "advancedStatsPanel";
            advancedStatsPanel.style.position = Position.Absolute;
            advancedStatsPanel.style.left = 0;
            advancedStatsPanel.style.top = 0;
            advancedStatsPanel.style.width = new Length(100, LengthUnit.Percent);
            advancedStatsPanel.style.height = new Length(100, LengthUnit.Percent);
            advancedStatsPanel.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0f); // Transparent, no darkening
            advancedStatsPanel.style.justifyContent = Justify.Center;
            advancedStatsPanel.style.alignItems = Align.Center;
            
            // Content container
            VisualElement content = new VisualElement();
            content.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            content.style.paddingTop = 20;
            content.style.paddingBottom = 20;
            content.style.paddingLeft = 30;
            content.style.paddingRight = 30;
            content.style.borderLeftWidth = 2;
            content.style.borderRightWidth = 2;
            content.style.borderTopWidth = 2;
            content.style.borderBottomWidth = 2;
            content.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            content.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            content.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            content.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            content.style.maxHeight = new Length(95, LengthUnit.Percent);
            content.style.maxWidth = 2000;
            
            // Title with close button
            VisualElement titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.justifyContent = Justify.SpaceBetween;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.marginBottom = 15;
            
            Label title = new Label("ADVANCED PLAYER STATISTICS");
            title.style.fontSize = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            if (uiFont != null) title.style.unityFont = uiFont;
            titleRow.Add(title);
            
            // Close button
            Button closeBtn = new Button();
            closeBtn.text = "CLOSE";
            closeBtn.style.width = 80;
            closeBtn.style.height = 30;
            closeBtn.style.fontSize = 12;
            closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            closeBtn.style.color = Color.white;
            closeBtn.style.backgroundColor = SummaryButtonBg;
            closeBtn.style.borderLeftWidth = 0;
            closeBtn.style.borderRightWidth = 0;
            closeBtn.style.borderTopWidth = 0;
            closeBtn.style.borderBottomWidth = 0;
            if (uiFont != null) closeBtn.style.unityFont = uiFont;
            
            // Add hover effect
            closeBtn.RegisterCallback<PointerEnterEvent>(_ =>
            {
                closeBtn.style.backgroundColor = Color.white;
                closeBtn.style.color = Color.black;
            });
            closeBtn.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                closeBtn.style.backgroundColor = SummaryButtonBg;
                closeBtn.style.color = Color.white;
            });
            
            closeBtn.clicked += () =>
            {
                advancedStatsPanel.RemoveFromHierarchy();
                advancedStatsPanel = null;
            };
            titleRow.Add(closeBtn);
            
            content.Add(titleRow);
            
            // Scrollable text content showing stats in same format as txt file
            ScrollView scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            scrollView.style.maxHeight = 700;
            
            Label statsText = new Label(FormatPlayerStatsSection());
            statsText.style.fontSize = 13;
            statsText.style.color = Color.white;
            statsText.style.whiteSpace = WhiteSpace.PreWrap; // Preserve formatting
            if (uiFont != null) statsText.style.unityFont = uiFont;
            
            scrollView.Add(statsText);
            content.Add(scrollView);

            advancedStatsPanel.Add(content);
            root.Add(advancedStatsPanel);
        }
        
        // ============================================
        // TEAM STATS COLUMN FOR GAME SUMMARY
        // ============================================
        
        private VisualElement CreateTeamStatsColumn(PlayerTeam team, UnityEngine.Font uiFont)
        {
            VisualElement column = new VisualElement();
            column.style.flexDirection = FlexDirection.Column;
            column.style.alignItems = Align.Center; // Center everything
            column.style.width = 380;
            column.style.paddingLeft = 10;
            column.style.paddingRight = 10;
            
            // Team name
            string teamName = team == PlayerTeam.Blue ? config.blueTeamName : config.redTeamName;
            Color teamColor = team == PlayerTeam.Blue ?
                ParseHexColor(config.blueTeamColorHex, new Color(0.4f, 0.6f, 1f)) :
                ParseHexColor(config.redTeamColorHex, new Color(1f, 0.4f, 0.4f));
            
            Label nameLabel = new Label(teamName);
            nameLabel.style.fontSize = 20;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.color = teamColor;
            nameLabel.style.marginBottom = 10;
            if (uiFont != null) nameLabel.style.unityFont = uiFont;
            column.Add(nameLabel);
            
            // Calculate team stats
            int teamGoals = team == PlayerTeam.Blue ? lastBlueScore : lastRedScore;
            int teamShots = team == PlayerTeam.Blue ? blueTeamShots : redTeamShots;
            int opponentShots = team == PlayerTeam.Blue ? redTeamShots : blueTeamShots;
            int opponentGoals = team == PlayerTeam.Blue ? lastRedScore : lastBlueScore;
            
            // Calculate actual saves: opponent shots minus goals they scored
            int actualSaves = opponentShots - opponentGoals;
            float savePercent = opponentShots > 0 ? (float)actualSaves / (float)opponentShots : 0f;
            float shotPercent = teamShots > 0 ? (float)teamGoals / (float)teamShots * 100f : 0f;
            
            // Team summary stats - centered
            Label scoreLabel = new Label($"Final Score: {teamGoals}");
            scoreLabel.style.fontSize = 14;
            scoreLabel.style.color = Color.white;
            scoreLabel.style.marginBottom = 3;
            if (uiFont != null) scoreLabel.style.unityFont = uiFont;
            column.Add(scoreLabel);
            
            Label shotsLabel = new Label($"Shots: {teamShots} | Shot%: {shotPercent:0.0} %");
            shotsLabel.style.fontSize = 14;
            shotsLabel.style.color = Color.white;
            shotsLabel.style.marginBottom = 3;
            if (uiFont != null) shotsLabel.style.unityFont = uiFont;
            column.Add(shotsLabel);
            
            Label savesLabel = new Label($"Saves: {actualSaves}/{opponentShots} | Save%: {savePercent:0.000}");
            savesLabel.style.fontSize = 14;
            savesLabel.style.color = Color.white;
            savesLabel.style.marginBottom = 15;
            if (uiFont != null) savesLabel.style.unityFont = uiFont;
            column.Add(savesLabel);
            
            // Individual player statistics section - use cached stats so disconnected players still show
            List<(string name, int number, int goals, int assists, int shots, ulong clientId, PlayerRole role, string steamId)> allTeamPlayers = 
                new List<(string, int, int, int, int, ulong, PlayerRole, string)>();
            
            // Get current players ONLY from this team
            if (MonoBehaviourSingleton<PlayerManager>.Instance != null)
            {
                var players = MonoBehaviourSingleton<PlayerManager>.Instance.GetPlayers(false);
                foreach (var player in players)
                {
                    // FIXED: Only add players that match THIS team
                    if (player.Team != team) continue;
                    
                    ulong clientId = player.OwnerClientId;
                    string steamId = player.SteamId.Value.ToString();
                    
                    // Get shots from playerSOG (by Steam ID) first, fallback to playerShots (by client ID)
                    int shots = 0;
                    if (playerSOG.ContainsKey(steamId))
                        shots = playerSOG[steamId];
                    else if (playerShots.ContainsKey(clientId))
                        shots = playerShots[clientId];
                    
                    DebugLog($"[CustomScoreboard] Game Summary: {player.Username.Value} (clientId={clientId}, steamId={steamId}) has {shots} shots");
                    
                    // Update cache with current stats
                    string positionName = GetPlayerPositionName(player);
                    cachedPlayerStats[clientId] = (
                        player.Username.Value.ToString(),
                        player.Number.Value,
                        player.Team,
                        player.Role,
                        player.Goals.Value,
                        player.Assists.Value,
                        shots,
                        steamId,
                        positionName
                    );
                    
                    allTeamPlayers.Add((
                        player.Username.Value.ToString(),
                        player.Number.Value,
                        player.Goals.Value,
                        player.Assists.Value,
                        shots,
                        clientId,
                        player.Role,
                        steamId
                    ));
                }
            }
            
            // Add any cached players from this team who disconnected
            foreach (var kvp in cachedPlayerStats)
            {
                if (kvp.Value.team == team && !allTeamPlayers.Any(p => p.clientId == kvp.Key))
                {
                    // Check if playerSOG has more recent shot data
                    int cachedShots = kvp.Value.shots;
                    if (!string.IsNullOrEmpty(kvp.Value.steamId) && playerSOG.ContainsKey(kvp.Value.steamId))
                        cachedShots = playerSOG[kvp.Value.steamId];
                    
                    allTeamPlayers.Add((
                        kvp.Value.name,
                        kvp.Value.number,
                        kvp.Value.goals,
                        kvp.Value.assists,
                        cachedShots,
                        kvp.Key,
                        kvp.Value.role,
                        kvp.Value.steamId // Include Steam ID for disconnected players
                    ));
                }
            }
            
            if (allTeamPlayers.Count > 0)
            {
                // Separate skaters and goalies by Role
                var skaters = allTeamPlayers.Where(p => p.role == PlayerRole.Attacker).ToList();
                var goalies = allTeamPlayers.Where(p => p.role == PlayerRole.Goalie).ToList();
                
                // Show skater stats header
                if (skaters.Count > 0)
                {
                    Label skatersTitle = new Label("SKATERS:");
                    skatersTitle.style.fontSize = 13;
                    skatersTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                    skatersTitle.style.color = new Color(0.9f, 0.9f, 0.9f);
                    skatersTitle.style.marginBottom = 5;
                    if (uiFont != null) skatersTitle.style.unityFont = uiFont;
                    column.Add(skatersTitle);
                    
                    // Sort skaters by points (goals + assists) descending
                    skaters = skaters.OrderByDescending(p => p.goals + p.assists).ToList();
                    
                    foreach (var player in skaters)
                    {
                        int points = player.goals + player.assists;
                        float playerShotPercent = player.shots > 0 ? (float)player.goals / (float)player.shots * 100f : 0f;

                        // Basic stats only: Goals, Assists, Points, Shots, Shot%
                        string statsText = $"#{player.number} {player.name}: {player.goals}G-{player.assists}A ({points}P) {player.shots}S | {playerShotPercent:0.0}%";
                        
                        Label playerStatLabel = new Label(statsText);
                        playerStatLabel.style.fontSize = 12;
                        playerStatLabel.style.color = new Color(0.95f, 0.95f, 0.95f);
                        playerStatLabel.style.marginBottom = 2;
                        playerStatLabel.style.whiteSpace = WhiteSpace.NoWrap;
                        if (uiFont != null) playerStatLabel.style.unityFont = uiFont;
                        column.Add(playerStatLabel);
                    }
                }
                
                // Show goalie stats
                if (goalies.Count > 0)
                {
                    Label goaliesTitle = new Label("GOALIES:");
                    goaliesTitle.style.fontSize = 13;
                    goaliesTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                    goaliesTitle.style.color = new Color(0.9f, 0.9f, 0.9f);
                    goaliesTitle.style.marginTop = 8;
                    goaliesTitle.style.marginBottom = 5;
                    if (uiFont != null) goaliesTitle.style.unityFont = uiFont;
                    column.Add(goaliesTitle);
                    
                    foreach (var goalie in goalies)
                    {
                        // Calculate goalie saves based on actual game data
                        string saveStatsText = "";
                        
                        // Try to use Stats mod data first if available
                        if (!string.IsNullOrEmpty(goalie.steamId) && goalieSaveStats.ContainsKey(goalie.steamId))
                        {
                            var (saves, shots) = goalieSaveStats[goalie.steamId];
                            float savePct = shots > 0 ? (float)saves / (float)shots : 0f;
                            saveStatsText = $"{saves}/{shots} saves ({savePct:0.000})";
                        }
                        // Otherwise calculate from game stats (opponent shots - goals)
                        else if (opponentShots > 0)
                        {
                            float savePct = actualSaves > 0 ? (float)actualSaves / (float)opponentShots : 0f;
                            saveStatsText = $"{actualSaves}/{opponentShots} saves ({savePct:0.000})";
                        }
                        else
                        {
                            saveStatsText = "0/0 saves (0.000)";
                        }
                        
                        // Format: "#30 Name: 15/17 saves (0.882) | 0G-0A"
                        string goalieStats = $"#{goalie.number} {goalie.name}: {saveStatsText}";
                        if (goalie.goals > 0 || goalie.assists > 0)
                            goalieStats += $" | {goalie.goals}G-{goalie.assists}A";
                        
                        Label goalieStatLabel = new Label(goalieStats);
                        goalieStatLabel.style.fontSize = 12;
                        goalieStatLabel.style.color = new Color(0.95f, 0.95f, 0.95f);
                        goalieStatLabel.style.marginBottom = 2;
                        if (uiFont != null) goalieStatLabel.style.unityFont = uiFont;
                        column.Add(goalieStatLabel);
                    }
                }
            }
            
            return column;
        }
    }
}
