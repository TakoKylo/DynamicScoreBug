using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomScoreboard.UI
{
    /// <summary>
    /// Partial class containing shootout detection and tracking methods.
    /// Called from Client.Entry.cs chat monitoring for shootout functionality.
    /// </summary>
    public partial class CustomBroadcastScoreboard
    {
        // Shootout detection and tracking methods (called from Client.Entry.cs chat monitoring)
        public void OnShootoutBegin()
        {
            DebugLog($"[CustomScoreboard] OnShootoutBegin called - setting isShootoutActive to TRUE");
            isShootoutActive = true;
            shootoutLabelOverride = true;
            blueShootoutGoals = 0;
            redShootoutGoals = 0;
            blueShootoutAttempts = 0;
            redShootoutAttempts = 0;
            
            // Don't clear shooter lists here - they're populated by OnShootoutShooterOrder which comes BEFORE this
            // Clearing results only
            blueShootoutResults.Clear();
            redShootoutResults.Clear();
            shootoutMaxRounds = 5; // Default to 5
            
            // Change period label to SHOOTOUT (with override flag to prevent it being changed)
            if (periodLabel != null)
            {
                periodLabel.text = "SHOOTOUT";
                DebugLog("[CustomScoreboard] Period label set to SHOOTOUT");
            }
            else
            {
                DebugWarning("[CustomScoreboard] periodLabel is NULL, cannot set SHOOTOUT text");
            }
        }
        
        public void OnShootoutBegin(string message)
        {
            OnShootoutBegin();
            
            // Try to parse "Best-of-X rounds" to determine max rounds
            try
            {
                string cleanMsg = System.Text.RegularExpressions.Regex.Replace(message, "<.*?>", string.Empty);
                if (cleanMsg.Contains("Best-of-"))
                {
                    int start = cleanMsg.IndexOf("Best-of-") + 8;
                    int end = cleanMsg.IndexOf(" ", start);
                    if (end > start)
                    {
                        string numStr = cleanMsg.Substring(start, end - start);
                        if (int.TryParse(numStr, out int maxRounds))
                        {
                            shootoutMaxRounds = maxRounds;
                            DebugLog($"[CustomScoreboard] Shootout is Best-of-{shootoutMaxRounds}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                DebugWarning($"[CustomScoreboard] Error parsing best-of number: {e}");
            }
        }
        
        public void OnShootoutShooterOrder(string message)
        {
            // Parse the shooter order message like "<b><color=#0066CCFF><b>Blue:</b></color> (none)\n<color=#FF0000FF><b>Red:</b></color> #62 ✧ Ami</b>"
            try
            {
                // Clear shooter lists at the START of parsing to prevent accumulation between shootouts
                blueShooters.Clear();
                redShooters.Clear();
                
                string cleanMsg = System.Text.RegularExpressions.Regex.Replace(message, "<.*?>", string.Empty);
                DebugLog($"[CustomScoreboard] Clean shooter order message: '{cleanMsg}'");
                
                // Split by Blue: and Red:
                if (cleanMsg.Contains("Blue:") && cleanMsg.Contains("Red:"))
                {
                    int blueStart = cleanMsg.IndexOf("Blue:") + 5;
                    int redStart = cleanMsg.IndexOf("Red:");
                    int redEnd = cleanMsg.Length;
                    
                    string blueShootersStr = cleanMsg.Substring(blueStart, redStart - blueStart).Trim();
                    string redShootersStr = cleanMsg.Substring(cleanMsg.IndexOf("Red:") + 4, redEnd - (cleanMsg.IndexOf("Red:") + 4)).Trim();
                    
                    DebugLog($"[CustomScoreboard] Blue shooters string: '{blueShootersStr}'");
                    DebugLog($"[CustomScoreboard] Red shooters string: '{redShootersStr}'");
                    
                    // Parse blue shooters
                    if (!blueShootersStr.Contains("(none)") && !string.IsNullOrEmpty(blueShootersStr))
                    {
                        // Could be comma-separated or single shooter
                        var shooters = blueShootersStr.Split(',');
                        foreach (var shooter in shooters)
                        {
                            string trimmed = shooter.Trim();
                            if (!string.IsNullOrEmpty(trimmed))
                            {
                                blueShooters.Add(trimmed);
                                DebugLog($"[CustomScoreboard] Added blue shooter: '{trimmed}'");
                            }
                        }
                    }
                    
                    // Parse red shooters
                    if (!redShootersStr.Contains("(none)") && !string.IsNullOrEmpty(redShootersStr))
                    {
                        var shooters = redShootersStr.Split(',');
                        foreach (var shooter in shooters)
                        {
                            string trimmed = shooter.Trim();
                            if (!string.IsNullOrEmpty(trimmed))
                            {
                                redShooters.Add(trimmed);
                                DebugLog($"[CustomScoreboard] Added red shooter: '{trimmed}'");
                            }
                        }
                    }
                    
                    DebugLog($"[CustomScoreboard] Parsed shooter order - Blue: {blueShooters.Count}, Red: {redShooters.Count}");
                    UpdateShootoutScoreboard();
                }
            }
            catch (Exception e)
            {
                DebugWarning($"[CustomScoreboard] Error parsing shooter order: {e}");
            }
        }

        public void OnShootoutShooterMessage(string message)
        {
            DebugLog($"[CustomScoreboard] Processing shooter message: {message}");
            
            // Parse messages like "<b><b>Shooter:</b> <color=#FF0000FF>#62 ✧ Ami</color></b>"
            // or "<b><b>Shooter:</b> <color=#0066CCFF>#62 ✧ Ami</color></b>"
            try
            {
                // Strip HTML tags
                string cleanMsg = System.Text.RegularExpressions.Regex.Replace(message, "<.*?>", string.Empty);
                
                // Determine team by color in original message
                PlayerTeam team = PlayerTeam.None;
                if (message.Contains("#0066CC") || message.Contains("Blue"))
                {
                    team = PlayerTeam.Blue;
                }
                else if (message.Contains("#FF0000") || message.Contains("Red"))
                {
                    team = PlayerTeam.Red;
                }
                
                if (team != PlayerTeam.None && cleanMsg.Contains("Shooter:"))
                {
                    string shooterName = cleanMsg.Substring(cleanMsg.IndexOf(":") + 1).Trim();
                    
                    // Just set the current shooter - don't add to lists (already populated from shooter order)
                    currentShooter = shooterName;
                    currentShooterTeam = team;
                    DebugLog($"[CustomScoreboard] Current shooter: {shooterName} ({team})");
                    
                    UpdateShootoutScoreboard();
                }
            }
            catch (Exception e)
            {
                DebugWarning($"[CustomScoreboard] Error parsing shooter message: {e}");
            }
        }

        public void OnShootoutResultMessage(string message)
        {
            DebugLog($"[CustomScoreboard] Processing shootout result: {message}");
            
            // Parse messages like "<b><color=#0066CCFF><b>DET</color> no goal <size=70%>(No shooter)</size></b>  Score <color=#0066CCFF><b>0</b></color>–<color=#FF0000FF><b>0</b></color></b>"
            try
            {
                // Strip HTML tags
                string cleanMsg = System.Text.RegularExpressions.Regex.Replace(message, "<.*?>", string.Empty);
                
                // Shootout shots ALWAYS alternate: Blue, Red, Blue, Red, etc.
                // Total attempts tells us which team is shooting
                int totalAttempts = blueShootoutAttempts + redShootoutAttempts;
                PlayerTeam shotTeam = (totalAttempts % 2 == 0) ? PlayerTeam.Blue : PlayerTeam.Red;
                
                // Extract score from "Score X–Y" format (using en-dash)
                if (cleanMsg.Contains("Score "))
                {
                    string scoreStr = cleanMsg.Substring(cleanMsg.IndexOf("Score ") + 6).Trim();
                    // Handle en-dash (–), em-dash (—), and regular hyphen (-)
                    scoreStr = scoreStr.Replace('–', '-').Replace('—', '-').Replace('?', '-');
                    string[] scores = scoreStr.Split('-');
                    if (scores.Length == 2)
                    {
                        int newBlueGoals = 0;
                        int newRedGoals = 0;
                        int.TryParse(scores[0].Trim(), out newBlueGoals);
                        int.TryParse(scores[1].Trim(), out newRedGoals);
                        
                        // Determine if it was a goal by checking for "scores" or "goal!" but NOT "no goal"
                        bool wasGoal = false;
                        if (cleanMsg.Contains("no goal") || cleanMsg.Contains("No goal"))
                        {
                            wasGoal = false;
                        }
                        else if (cleanMsg.Contains("scores") || cleanMsg.Contains("goal!") || cleanMsg.Contains("GOAL!"))
                        {
                            wasGoal = true;
                        }
                        
                        // Update attempts based on which team just shot (alternating pattern)
                        if (shotTeam == PlayerTeam.Blue)
                        {
                            blueShootoutAttempts++;
                            blueShootoutResults.Add(wasGoal);
                            blueShootoutGoals = newBlueGoals;
                        }
                        else if (shotTeam == PlayerTeam.Red)
                        {
                            redShootoutAttempts++;
                            redShootoutResults.Add(wasGoal);
                            redShootoutGoals = newRedGoals;
                        }
                        
                        DebugLog($"[CustomScoreboard] Shootout state: Blue {blueShootoutGoals}/{blueShootoutAttempts}, Red {redShootoutGoals}/{redShootoutAttempts}");
                    }
                }
                
                // Update shootout UI
                UpdateShootoutScoreboard();
                
                // Check for winner (best of 5 = first to 3, or ahead after 5 rounds)
                CheckShootoutWinner();
            }
            catch (Exception e)
            {
                DebugWarning($"[CustomScoreboard] Error parsing shootout result: {e}");
            }
        }
    }
}
