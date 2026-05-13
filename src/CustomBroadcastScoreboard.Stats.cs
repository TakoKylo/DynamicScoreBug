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
                                    if (text.Contains(".") && float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float savePercent))
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
        // THREE STARS TRACKING METHODS
        // ============================================
        
        // Extract player name from either a bare name ("PuckistaniSniper") or a chat-formatted
        // announcement like "The first star is... #67 PuckistaniSniper !" / "1st star: PlayerName".
        private static string ExtractStarName(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            string cleanMsg = System.Text.RegularExpressions.Regex.Replace(message, "<.*?>", string.Empty).Trim();

            if (cleanMsg.Contains("is..."))
            {
                int isIndex = cleanMsg.IndexOf("is...");
                return cleanMsg.Substring(isIndex + 5).TrimEnd('!', ' ', '.').Trim();
            }

            int colonIndex = cleanMsg.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < cleanMsg.Length - 1)
            {
                return cleanMsg.Substring(colonIndex + 1).Trim();
            }

            // Bare name (e.g. from oomtm450_statsSTAR network handler)
            return cleanMsg;
        }

        public void OnFirstStarAnnounced(string message)
        {
            try
            {
                string name = ExtractStarName(message);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    firstStar = name;
                    DebugLog($"[CustomScoreboard] First star set to: {firstStar}");
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
                string name = ExtractStarName(message);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    secondStar = name;
                    DebugLog($"[CustomScoreboard] Second star set to: {secondStar}");
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
                string name = ExtractStarName(message);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    thirdStar = name;
                    DebugLog($"[CustomScoreboard] Third star set to: {thirdStar}");
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
                .SetTarget(this)
                .AppendInterval(1f)
                .OnComplete(() => {
                    ShowShootoutWinAnimation(winningTeam);
                });
        }
    }
}
