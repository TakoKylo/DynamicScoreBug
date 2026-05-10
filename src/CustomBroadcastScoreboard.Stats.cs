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
        // PLAYER STATS RETRIEVAL METHODS
        // ============================================
        
        private (int goals, int assists, int points) GetPlayerStats(Player player)
        {
            try
            {
                var uiScoreboard = FindFirstObjectByType<UIScoreboard>();
                if (uiScoreboard != null)
                {
                    var playerMapField = GetPrivateInstanceField(typeof(UIScoreboard), "playerVisualElementMap");
                    
                    if (playerMapField != null)
                    {
                        var playerMap = playerMapField.GetValue(uiScoreboard) as Dictionary<Player, VisualElement>;
                        
                        if (playerMap != null && playerMap.ContainsKey(player))
                        {
                            VisualElement playerRow = playerMap[player];
                            var allLabels = playerRow.Query<Label>().ToList();
                            
                            // Debug: Log all label texts to find correct indices
                            DebugLog($"[CustomScoreboard] Player {player.Username.Value} has {allLabels.Count} labels");
                            for (int i = 0; i < allLabels.Count; i++)
                            {
                                DebugLog($"[CustomScoreboard]   Label[{i}]: {allLabels[i].text}");
                            }
                            
                            // Find Goals, Assists, Points by searching through all labels
                            int goals = 0, assists = 0, points = 0;
                            
                            // Try to find them in order: Number, Name, G, A, P, +/-, Shots, etc.
                            // Skip first 2 (number and name), then parse numeric values
                            for (int i = 2; i < allLabels.Count && i < 7; i++)
                            {
                                if (int.TryParse(allLabels[i].text, out int value))
                                {
                                    if (i == 2) goals = value;       // Goals is usually index 2
                                    else if (i == 3) assists = value; // Assists is usually index 3
                                    else if (i == 4) points = value;  // Points is usually index 4
                                }
                            }
                            
                            // If points wasn't found or is 0, calculate it
                            if (points == 0 || points != goals + assists)
                            {
                                points = goals + assists;
                            }
                            
                            DebugLog($"[CustomScoreboard] Player {player.Username.Value} stats: {goals}G {assists}A {points}P");
                            
                            return (goals, assists, points);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error getting player stats: {ex.Message}");
            }
            
            return (0, 0, 0);
        }
        
        private float GetGoalieSavePercentage(Player goalie)
        {
            try
            {
                var uiScoreboard = FindFirstObjectByType<UIScoreboard>();
                if (uiScoreboard != null)
                {
                    var playerMapField = GetPrivateInstanceField(typeof(UIScoreboard), "playerVisualElementMap");
                    
                    if (playerMapField != null)
                    {
                        var playerMap = playerMapField.GetValue(uiScoreboard) as Dictionary<Player, VisualElement>;
                        
                        if (playerMap != null && playerMap.ContainsKey(goalie))
                        {
                            VisualElement playerRow = playerMap[goalie];
                            var allLabels = playerRow.Query<Label>().ToList();
                            
                            // Look for save percentage - it's a decimal number like "0.923"
                            foreach (var label in allLabels)
                            {
                                if (!string.IsNullOrEmpty(label.text))
                                {
                                    string text = label.text.Trim();
                                    
                                    // Check if it's a decimal number between 0 and 1
                                    if (text.Contains(".") && float.TryParse(text, out float savePercent))
                                    {
                                        if (savePercent >= 0f && savePercent <= 1f)
                                        {
                                            return savePercent;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            
            return 0f;
        }

        // ============================================
        // STATS DISPLAY HELPERS
        // ============================================

        private string FormatPlayerStatsSection()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== PLAYER STATISTICS ===");
            
            // Check if advanced stats are available (any player has non-zero values)
            bool hasAdvancedStats = playerHits.Values.Any(v => v > 0) || 
                                     playerPasses.Values.Any(v => v > 0) || 
                                     playerBlocks.Values.Any(v => v > 0) || 
                                     playerTakeaways.Values.Any(v => v > 0) || 
                                     playerTurnovers.Values.Any(v => v > 0);
            
            // Check if extended stats are available
            bool hasExtendedStats = playerPlusMinus.Count > 0 || 
                                    playerTimeOnIce.Values.Any(v => v > 0) ||
                                    playerPuckBattleWins.Values.Any(v => v > 0);
            
            if (MonoBehaviourSingleton<PlayerManager>.Instance != null)
            {
                var players = MonoBehaviourSingleton<PlayerManager>.Instance.GetPlayers(false);
                
                // Build combined lists (current + disconnected) for each team
                var allBluePlayers = new List<(string name, int number, PlayerRole role, int goals, int assists, int shots, string steamId, string position, bool isDisconnected)>();
                var allRedPlayers = new List<(string name, int number, PlayerRole role, int goals, int assists, int shots, string steamId, string position, bool isDisconnected)>();
                
                // Add current players
                foreach (var player in players)
                {
                    ulong clientId = player.OwnerClientId;
                    string steamId = player.SteamId.Value.ToString();
                    string position = GetPlayerPosition(player);
                    
                    // Get shots from playerSOG (by Steam ID) first, fallback to playerShots (by client ID)
                    int shots = 0;
                    if (playerSOG.ContainsKey(steamId))
                        shots = playerSOG[steamId];
                    else if (playerShots.ContainsKey(clientId))
                        shots = playerShots[clientId];
                    
                    if (player.Team == PlayerTeam.Blue)
                        allBluePlayers.Add((player.Username.Value.ToString(), player.Number.Value, player.Role, player.Goals.Value, player.Assists.Value, shots, steamId, position, false));
                    else if (player.Team == PlayerTeam.Red)
                        allRedPlayers.Add((player.Username.Value.ToString(), player.Number.Value, player.Role, player.Goals.Value, player.Assists.Value, shots, steamId, position, false));
                }
                
                // Add disconnected players from cache - also check playerSOG for shots
                foreach (var kvp in cachedPlayerStats)
                {
                    var (name, number, team, role, goals, assists, shots, steamId, position) = kvp.Value;
                    
                    // Check if playerSOG has more recent shot data
                    int actualShots = shots;
                    if (!string.IsNullOrEmpty(steamId) && playerSOG.ContainsKey(steamId))
                        actualShots = playerSOG[steamId];
                    
                    // Only add if not already in current players list
                    if (team == PlayerTeam.Blue && !allBluePlayers.Any(p => p.name == name))
                        allBluePlayers.Add((name, number, role, goals, assists, actualShots, steamId, position, true));
                    else if (team == PlayerTeam.Red && !allRedPlayers.Any(p => p.name == name))
                        allRedPlayers.Add((name, number, role, goals, assists, actualShots, steamId, position, true));
                }
                
                // Blue team - separate skaters and goalies
                var blueSkaters = allBluePlayers.Where(p => p.role == PlayerRole.Attacker).ToList();
                var blueGoalies = allBluePlayers.Where(p => p.role == PlayerRole.Goalie).ToList();
                
                // Blue team skaters
                if (blueSkaters.Count > 0)
                {
                    sb.AppendLine($"{config.blueTeamName} Players:");
                    foreach (var playerData in blueSkaters)
                    {
                        int points = playerData.goals + playerData.assists;
                        float shotPercent = playerData.shots > 0 ? (float)playerData.goals / playerData.shots * 100f : 0f;
                        
                        // Get additional stats from Stats mod
                        int hits = !string.IsNullOrEmpty(playerData.steamId) && playerHits.ContainsKey(playerData.steamId) ? playerHits[playerData.steamId] : 0;
                        int passes = !string.IsNullOrEmpty(playerData.steamId) && playerPasses.ContainsKey(playerData.steamId) ? playerPasses[playerData.steamId] : 0;
                        int blocks = !string.IsNullOrEmpty(playerData.steamId) && playerBlocks.ContainsKey(playerData.steamId) ? playerBlocks[playerData.steamId] : 0;
                        int takeaways = !string.IsNullOrEmpty(playerData.steamId) && playerTakeaways.ContainsKey(playerData.steamId) ? playerTakeaways[playerData.steamId] : 0;
                        int turnovers = !string.IsNullOrEmpty(playerData.steamId) && playerTurnovers.ContainsKey(playerData.steamId) ? playerTurnovers[playerData.steamId] : 0;
                        
                        // Get extended stats
                        int plusMinus = !string.IsNullOrEmpty(playerData.steamId) && playerPlusMinus.ContainsKey(playerData.steamId) ? playerPlusMinus[playerData.steamId] : 0;
                        double toi = !string.IsNullOrEmpty(playerData.steamId) && playerTimeOnIce.ContainsKey(playerData.steamId) ? playerTimeOnIce[playerData.steamId] : 0;
                        int puckBattleWins = !string.IsNullOrEmpty(playerData.steamId) && playerPuckBattleWins.ContainsKey(playerData.steamId) ? playerPuckBattleWins[playerData.steamId] : 0;
                        int puckBattleLosses = !string.IsNullOrEmpty(playerData.steamId) && playerPuckBattleLosses.ContainsKey(playerData.steamId) ? playerPuckBattleLosses[playerData.steamId] : 0;
                        
                        // Format with Steam ID and abbreviated position (DC for disconnected)
                        string displayPosition = playerData.isDisconnected ? "DC" : playerData.position;
                        string playerInfo = $"#{playerData.number.ToString().PadLeft(2)} {playerData.name.PadRight(20)} ({displayPosition.PadRight(2)}) [{playerData.steamId}]";
                        
                        // Format +/- with sign
                        string plusMinusStr = plusMinus >= 0 ? $"+{plusMinus}" : plusMinus.ToString();
                        string scoring = $"{playerData.goals}G {playerData.assists}A {points}P {plusMinusStr} {playerData.shots}S {shotPercent:0.0}%".PadRight(30);
                        
                        if (hasAdvancedStats || hasExtendedStats)
                        {
                            var statParts = new List<string>();
                            if (hasAdvancedStats)
                            {
                                statParts.Add($"{hits}H {passes}Pas {blocks}Blk {takeaways}TA {turnovers}TO");
                            }
                            if (hasExtendedStats && toi > 0)
                            {
                                int toiMinutes = (int)(toi / 60);
                                int toiSeconds = (int)(toi % 60);
                                statParts.Add($"TOI:{toiMinutes}:{toiSeconds:D2}");
                            }
                            if (hasExtendedStats && (puckBattleWins > 0 || puckBattleLosses > 0))
                            {
                                statParts.Add($"PB:{puckBattleWins}-{puckBattleLosses}");
                            }
                            sb.AppendLine($"  {playerInfo} | {scoring} | {string.Join(" ", statParts)}");
                        }
                        else
                        {
                            sb.AppendLine($"  {playerInfo} | {scoring}");
                        }
                    }
                }
                
                // Blue team goalies
                if (blueGoalies.Count > 0)
                {
                    sb.AppendLine($"{config.blueTeamName} Goalies:");
                    foreach (var playerData in blueGoalies)
                    {
                        // Get passes from Stats mod
                        int passes = !string.IsNullOrEmpty(playerData.steamId) && playerPasses.ContainsKey(playerData.steamId) ? playerPasses[playerData.steamId] : 0;
                        
                        // Get goalie-specific extended stats
                        int stickSaves = !string.IsNullOrEmpty(playerData.steamId) && playerStickSaves.ContainsKey(playerData.steamId) ? playerStickSaves[playerData.steamId] : 0;
                        int bodySaves = !string.IsNullOrEmpty(playerData.steamId) && playerBodySaves.ContainsKey(playerData.steamId) ? playerBodySaves[playerData.steamId] : 0;
                        
                        // Get saves and save% from goalieSaveStats (same as game summary)
                        int saves = 0;
                        int shotsAgainst = 0;
                        float savePercent = 0f;
                        if (!string.IsNullOrEmpty(playerData.steamId) && goalieSaveStats.ContainsKey(playerData.steamId))
                        {
                            var (savesCount, shots) = goalieSaveStats[playerData.steamId];
                            saves = savesCount;
                            shotsAgainst = shots;
                            savePercent = shots > 0 ? (float)savesCount / (float)shots : 0f;
                        }
                        
                        // Format with Steam ID (DC for disconnected)
                        string displayPosition = playerData.isDisconnected ? "DC" : "G";
                        string playerInfo = $"#{playerData.number.ToString().PadLeft(2)} {playerData.name.PadRight(20)} ({displayPosition} ) [{playerData.steamId}]";
                        
                        var statParts = new List<string>();
                        statParts.Add($"{saves}/{shotsAgainst} Saves {savePercent:0.000}");
                        
                        // Show stick/body save breakdown if available
                        if (stickSaves > 0 || bodySaves > 0)
                        {
                            statParts.Add($"({stickSaves}Stk/{bodySaves}Bdy)");
                        }
                        
                        if (hasAdvancedStats && passes > 0)
                        {
                            statParts.Add($"{passes}Pas");
                        }
                        
                        // Add goals/assists/points if any are non-zero
                        if (playerData.goals > 0 || playerData.assists > 0)
                        {
                            int points = playerData.goals + playerData.assists;
                            statParts.Add($"{playerData.goals}G {playerData.assists}A {points}P");
                        }
                        
                        sb.AppendLine($"  {playerInfo} | {string.Join(" | ", statParts)}");
                    }
                }
                
                sb.AppendLine();;
                
                // Red team - separate skaters and goalies
                var redSkaters = allRedPlayers.Where(p => p.role == PlayerRole.Attacker).ToList();
                var redGoalies = allRedPlayers.Where(p => p.role == PlayerRole.Goalie).ToList();
                
                // Red team skaters
                if (redSkaters.Count > 0)
                {
                    sb.AppendLine($"{config.redTeamName} Players:");
                    foreach (var playerData in redSkaters)
                    {
                        int points = playerData.goals + playerData.assists;
                        float shotPercent = playerData.shots > 0 ? (float)playerData.goals / playerData.shots * 100f : 0f;
                        
                        // Get additional stats from Stats mod
                        int hits = !string.IsNullOrEmpty(playerData.steamId) && playerHits.ContainsKey(playerData.steamId) ? playerHits[playerData.steamId] : 0;
                        int passes = !string.IsNullOrEmpty(playerData.steamId) && playerPasses.ContainsKey(playerData.steamId) ? playerPasses[playerData.steamId] : 0;
                        int blocks = !string.IsNullOrEmpty(playerData.steamId) && playerBlocks.ContainsKey(playerData.steamId) ? playerBlocks[playerData.steamId] : 0;
                        int takeaways = !string.IsNullOrEmpty(playerData.steamId) && playerTakeaways.ContainsKey(playerData.steamId) ? playerTakeaways[playerData.steamId] : 0;
                        int turnovers = !string.IsNullOrEmpty(playerData.steamId) && playerTurnovers.ContainsKey(playerData.steamId) ? playerTurnovers[playerData.steamId] : 0;
                        
                        // Get extended stats
                        int plusMinus = !string.IsNullOrEmpty(playerData.steamId) && playerPlusMinus.ContainsKey(playerData.steamId) ? playerPlusMinus[playerData.steamId] : 0;
                        double toi = !string.IsNullOrEmpty(playerData.steamId) && playerTimeOnIce.ContainsKey(playerData.steamId) ? playerTimeOnIce[playerData.steamId] : 0;
                        int puckBattleWins = !string.IsNullOrEmpty(playerData.steamId) && playerPuckBattleWins.ContainsKey(playerData.steamId) ? playerPuckBattleWins[playerData.steamId] : 0;
                        int puckBattleLosses = !string.IsNullOrEmpty(playerData.steamId) && playerPuckBattleLosses.ContainsKey(playerData.steamId) ? playerPuckBattleLosses[playerData.steamId] : 0;
                        
                        // Format with Steam ID and abbreviated position (DC for disconnected)
                        string displayPosition = playerData.isDisconnected ? "DC" : playerData.position;
                        string playerInfo = $"#{playerData.number.ToString().PadLeft(2)} {playerData.name.PadRight(20)} ({displayPosition.PadRight(2)}) [{playerData.steamId}]";
                        
                        // Format +/- with sign
                        string plusMinusStr = plusMinus >= 0 ? $"+{plusMinus}" : plusMinus.ToString();
                        string scoring = $"{playerData.goals}G {playerData.assists}A {points}P {plusMinusStr} {playerData.shots}S {shotPercent:0.0}%".PadRight(30);
                        
                        if (hasAdvancedStats || hasExtendedStats)
                        {
                            var statParts = new List<string>();
                            if (hasAdvancedStats)
                            {
                                statParts.Add($"{hits}H {passes}Pas {blocks}Blk {takeaways}TA {turnovers}TO");
                            }
                            if (hasExtendedStats && toi > 0)
                            {
                                int toiMinutes = (int)(toi / 60);
                                int toiSeconds = (int)(toi % 60);
                                statParts.Add($"TOI:{toiMinutes}:{toiSeconds:D2}");
                            }
                            if (hasExtendedStats && (puckBattleWins > 0 || puckBattleLosses > 0))
                            {
                                statParts.Add($"PB:{puckBattleWins}-{puckBattleLosses}");
                            }
                            sb.AppendLine($"  {playerInfo} | {scoring} | {string.Join(" ", statParts)}");
                        }
                        else
                        {
                            sb.AppendLine($"  {playerInfo} | {scoring}");
                        }
                    }
                }
                
                // Red team goalies
                if (redGoalies.Count > 0)
                {
                    sb.AppendLine($"{config.redTeamName} Goalies:");
                    foreach (var playerData in redGoalies)
                    {
                        // Get passes from Stats mod
                        int passes = !string.IsNullOrEmpty(playerData.steamId) && playerPasses.ContainsKey(playerData.steamId) ? playerPasses[playerData.steamId] : 0;
                        
                        // Get goalie-specific extended stats
                        int stickSaves = !string.IsNullOrEmpty(playerData.steamId) && playerStickSaves.ContainsKey(playerData.steamId) ? playerStickSaves[playerData.steamId] : 0;
                        int bodySaves = !string.IsNullOrEmpty(playerData.steamId) && playerBodySaves.ContainsKey(playerData.steamId) ? playerBodySaves[playerData.steamId] : 0;
                        
                        // Get saves and save% from goalieSaveStats (same as game summary)
                        int saves = 0;
                        int shotsAgainst = 0;
                        float savePercent = 0f;
                        if (!string.IsNullOrEmpty(playerData.steamId) && goalieSaveStats.ContainsKey(playerData.steamId))
                        {
                            var (savesCount, shots) = goalieSaveStats[playerData.steamId];
                            saves = savesCount;
                            shotsAgainst = shots;
                            savePercent = shots > 0 ? (float)savesCount / (float)shots : 0f;
                        }
                        
                        // Format with Steam ID (DC for disconnected)
                        string displayPosition = playerData.isDisconnected ? "DC" : "G";
                        string playerInfo = $"#{playerData.number.ToString().PadLeft(2)} {playerData.name.PadRight(20)} ({displayPosition} ) [{playerData.steamId}]";
                        
                        var statParts = new List<string>();
                        statParts.Add($"{saves}/{shotsAgainst} Saves {savePercent:0.000}");
                        
                        // Show stick/body save breakdown if available
                        if (stickSaves > 0 || bodySaves > 0)
                        {
                            statParts.Add($"({stickSaves}Stk/{bodySaves}Bdy)");
                        }
                        
                        if (hasAdvancedStats && passes > 0)
                        {
                            statParts.Add($"{passes}Pas");
                        }
                        
                        // Add goals/assists/points if any are non-zero
                        if (playerData.goals > 0 || playerData.assists > 0)
                        {
                            int points = playerData.goals + playerData.assists;
                            statParts.Add($"{playerData.goals}G {playerData.assists}A {points}P");
                        }
                        
                        sb.AppendLine($"  {playerInfo} | {string.Join(" | ", statParts)}");
                    }
                }
            }
            
            return sb.ToString();
        }
        
        // ============================================
        // THREE STARS TRACKING METHODS
        // ============================================
        
        public void OnFirstStarAnnounced(string message)
        {
            try
            {
                // Parse message like "The first star is... #67 PuckistaniSniper !" or "1st star: PlayerName"
                string cleanMsg = System.Text.RegularExpressions.Regex.Replace(message, "<.*?>", string.Empty);
                
                // Try "is..." format first
                if (cleanMsg.Contains("is..."))
                {
                    int isIndex = cleanMsg.IndexOf("is...");
                    string remaining = cleanMsg.Substring(isIndex + 5).Trim();
                    // Remove trailing exclamation and extra spaces
                    firstStar = remaining.TrimEnd('!', ' ', '.').Trim();
                    DebugLog($"[CustomScoreboard] First star set to: {firstStar}");
                }
                // Fallback to colon format
                else
                {
                    int colonIndex = cleanMsg.IndexOf(":");
                    if (colonIndex >= 0 && colonIndex < cleanMsg.Length - 1)
                    {
                        firstStar = cleanMsg.Substring(colonIndex + 1).Trim();
                        DebugLog($"[CustomScoreboard] First star set to: {firstStar}");
                    }
                }
            }
            catch (Exception e)
            {
                DebugWarning($"[CustomScoreboard] Error parsing first star: {e}");
            }
        }
        
        public void OnSecondStarAnnounced(string message)
        {
            try
            {
                // Parse message like "The second star is... #28 DrSouls !" or "2nd star: PlayerName"
                string cleanMsg = System.Text.RegularExpressions.Regex.Replace(message, "<.*?>", string.Empty);
                
                // Try "is..." format first
                if (cleanMsg.Contains("is..."))
                {
                    int isIndex = cleanMsg.IndexOf("is...");
                    string remaining = cleanMsg.Substring(isIndex + 5).Trim();
                    // Remove trailing exclamation and extra spaces
                    secondStar = remaining.TrimEnd('!', ' ', '.').Trim();
                    DebugLog($"[CustomScoreboard] Second star set to: {secondStar}");
                }
                // Fallback to colon format
                else
                {
                    int colonIndex = cleanMsg.IndexOf(":");
                    if (colonIndex >= 0 && colonIndex < cleanMsg.Length - 1)
                    {
                        secondStar = cleanMsg.Substring(colonIndex + 1).Trim();
                        DebugLog($"[CustomScoreboard] Second star set to: {secondStar}");
                    }
                }
            }
            catch (Exception e)
            {
                DebugWarning($"[CustomScoreboard] Error parsing second star: {e}");
            }
        }
        
        public void OnThirdStarAnnounced(string message)
        {
            try
            {
                // Parse message like "The third star is... #24 SnackTheWall !" or "3rd star: PlayerName"
                string cleanMsg = System.Text.RegularExpressions.Regex.Replace(message, "<.*?>", string.Empty);
                
                // Try "is..." format first
                if (cleanMsg.Contains("is..."))
                {
                    int isIndex = cleanMsg.IndexOf("is...");
                    string remaining = cleanMsg.Substring(isIndex + 5).Trim();
                    // Remove trailing exclamation and extra spaces
                    thirdStar = remaining.TrimEnd('!', ' ', '.').Trim();
                    DebugLog($"[CustomScoreboard] Third star set to: {thirdStar}");
                }
                // Fallback to colon format
                else
                {
                    int colonIndex = cleanMsg.IndexOf(":");
                    if (colonIndex >= 0 && colonIndex < cleanMsg.Length - 1)
                    {
                        thirdStar = cleanMsg.Substring(colonIndex + 1).Trim();
                        DebugLog($"[CustomScoreboard] Third star set to: {thirdStar}");
                    }
                }
            }
            catch (Exception e)
            {
                DebugWarning($"[CustomScoreboard] Error parsing third star: {e}");
            }
        }
        
        // ============================================
        // PLAYER STAT UPDATE METHODS
        // ============================================
        
        public void UpdatePlayerShots(ulong steamId, int shotCount)
        {
            try
            {
                // Find the player by Steam ID
                if (MonoBehaviourSingleton<PlayerManager>.Instance != null)
                {
                    var players = MonoBehaviourSingleton<PlayerManager>.Instance.GetPlayers(false);
                    
                    // SteamId is stored as a string, so convert to compare
                    string steamIdStr = steamId.ToString();
                    var player = players.FirstOrDefault(p => p.SteamId.Value.ToString() == steamIdStr);
                    
                    if (player != null)
                    {
                        ulong clientId = player.OwnerClientId;
                        playerShots[clientId] = shotCount;
                        DebugLog($"[CustomScoreboard] Updated shots for {player.Username.Value} (clientId={clientId}): {shotCount} shots");
                        
                        // Cache player stats with position
                        string positionName = GetPlayerPositionName(player);
                        cachedPlayerStats[clientId] = (
                            player.Username.Value.ToString(),
                            player.Number.Value,
                            player.Team,
                            player.Role,
                            player.Goals.Value,
                            player.Assists.Value,
                            shotCount,
                            player.SteamId.Value.ToString(),
                            positionName
                        );
                        
                        DebugLog($"[CustomScoreboard] Updated shots for player {player.Username.Value} (ClientId: {clientId}): {shotCount}");
                    }
                }
            }
            catch (Exception e)
            {
                DebugWarning($"[CustomScoreboard] Error updating player shots: {e}");
            }
        }
        
        public void UpdateGoalieSaves(ulong steamId, int shots, int saves)
        {
            try
            {
                string steamIdStr = steamId.ToString();
                goalieSaveStats[steamIdStr] = (shots, saves);
                DebugLog($"[CustomScoreboard] Updated goalie saves for Steam ID {steamId}: {saves}/{shots}");
            }
            catch (Exception e)
            {
                DebugWarning($"[CustomScoreboard] Error updating goalie saves: {e}");
            }
        }

        // ============================================
        // SHOOTOUT TRACKING METHODS
        // ============================================
        
        private void OnShootoutAttempt(PlayerTeam team, bool scored)
        {
            if (team == PlayerTeam.Blue)
            {
                blueShootoutResults.Add(scored);
                blueShootoutAttempts++;
                if (scored) blueShootoutGoals++;
            }
            else if (team == PlayerTeam.Red)
            {
                redShootoutResults.Add(scored);
                redShootoutAttempts++;
                if (scored) redShootoutGoals++;
            }
            
            UpdateShootoutScoreboard();
        }
        
        private void UpdateShootoutScoreboard()
        {
            // Shootout tracking still active - just logging for now
            // The shootout mod will handle the UI
            DebugLog($"[CustomScoreboard] Shootout update: Blue {blueShootoutGoals}/{blueShootoutAttempts}, Red {redShootoutGoals}/{redShootoutAttempts}");
        }

        private void CheckShootoutWinner()
        {
            // Just update the UI - don't show win animation here
            // The game will naturally end and trigger GamePhase.GameOver when someone wins
            // At that point, we'll show the shootout win animation
            
            DebugLog($"[CustomScoreboard] Shootout update: Blue {blueShootoutGoals}/{blueShootoutAttempts}, Red {redShootoutGoals}/{redShootoutAttempts}");
        }

        public void OnShootoutWin(PlayerTeam winningTeam)
        {
            DebugLog($"[CustomScoreboard] Shootout win detected for {winningTeam}!");
            
            // Hide the shootout popup
            if (isShootoutActive)
            {
                isShootoutActive = false;
            }
            
            // Show the shootout win animation immediately
            DOTween.Sequence()
                .AppendInterval(1f)
                .OnComplete(() => {
                    ShowShootoutWinAnimation(winningTeam);
                });
        }
    }
}
