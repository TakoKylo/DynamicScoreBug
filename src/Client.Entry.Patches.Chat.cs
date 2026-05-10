using System;
using UnityEngine;

public sealed partial class Scoreboard_ClientMod
{
    public static bool Chat_ScoreboardCommand_Prefix(ChatManager __instance, ref string content, bool isQuickChat, bool isTeamChat)
    {
        try
        {
            if (isQuickChat || string.IsNullOrEmpty(content)) return true;

            var trimmed = content.Trim();
            var lowerCmd = trimmed.ToLower();

            // Check for popup commands
            if (lowerCmd.StartsWith("/"))
            {
                var scoreboard = UnityEngine.Object.FindFirstObjectByType<CustomScoreboard.UI.CustomBroadcastScoreboard>();

                if (scoreboard != null)
                {
                    bool commandHandled = false;

                    // Separate blue/red shot speed commands
                    if (lowerCmd.StartsWith("/shotspeedb"))
                    {
                        Debug.Log("[Scoreboard] /shotspeedb command detected!");
                        scoreboard.ShowShotSpeedBlue();
                        commandHandled = true;
                    }
                    else if (lowerCmd.StartsWith("/shotspeedr"))
                    {
                        Debug.Log("[Scoreboard] /shotspeedr command detected!");
                        scoreboard.ShowShotSpeedRed();
                        commandHandled = true;
                    }
                    else if (lowerCmd.StartsWith("/shotspeed"))
                    {
                        Debug.Log("[Scoreboard] /shotspeed command detected!");
                        scoreboard.TestShotSpeedPopup();
                        commandHandled = true;
                    }
                    // Separate blue/red save% commands
                    else if (lowerCmd.StartsWith("/save%b") || lowerCmd.StartsWith("/savepercentb"))
                    {
                        Debug.Log("[Scoreboard] /save%b command detected!");
                        scoreboard.ShowSavePercentBlue();
                        commandHandled = true;
                    }
                    else if (lowerCmd.StartsWith("/save%r") || lowerCmd.StartsWith("/savepercentr"))
                    {
                        Debug.Log("[Scoreboard] /save%r command detected!");
                        scoreboard.ShowSavePercentRed();
                        commandHandled = true;
                    }
                    else if (lowerCmd.StartsWith("/save%") || lowerCmd.StartsWith("/savepercent"))
                    {
                        Debug.Log("[Scoreboard] /save% command detected!");
                        scoreboard.ShowGoalieStats();
                        commandHandled = true;
                    }
                    else if (lowerCmd.StartsWith("/summarys"))
                    {
                        Debug.Log("[Scoreboard] /summarys command detected!");
                        scoreboard.TestScoringSummary();
                        commandHandled = true;
                    }
                    else if (lowerCmd.StartsWith("/summaryp"))
                    {
                        Debug.Log("[Scoreboard] /summaryp command detected!");
                        scoreboard.TestPeriodSummary();
                        commandHandled = true;
                    }
                    else if (lowerCmd.StartsWith("/summaryg"))
                    {
                        Debug.Log("[Scoreboard] /summaryg command detected!");
                        scoreboard.TestEndOfGameSummary();
                        commandHandled = true;
                    }
                    else if (lowerCmd.StartsWith("/lineup"))
                    {
                        Debug.Log("[Scoreboard] /lineup command detected!");
                        scoreboard.TestLineupPopup();
                        commandHandled = true;
                    }

                    if (commandHandled)
                    {
                        return false; // Block the message from being sent to chat
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Scoreboard] Error in chat command prefix: " + e);
        }

        return true; // Allow other messages through
    }

    public static void Chat_MessageReceived_Postfix(ChatMessage chatMessage)
    {
        try
        {
            string message = chatMessage?.Content.ToString();
            if (string.IsNullOrEmpty(message)) return;

            var scoreboard = UnityEngine.Object.FindFirstObjectByType<CustomScoreboard.UI.CustomBroadcastScoreboard>();

            // Check for warmup/start commands or vote passes (reset stats when game resets)
            if (message.Contains("Starting warmup") || message.Contains("Warmup vote passed") ||
                message.Contains("Starting game") || message.Contains("Start vote passed") ||
                message.Contains("Resetting game"))
            {
                if (scoreboard != null)
                {
                    scoreboard.ResetStatsOnCommand();
                }
            }

            // Check for native shot speed message from server (e.g. "Goal scored! 100.7 KPH across the line, 106.6 KPH from the stick.")
            // This is a standalone check — it fires independently of the other event checks below.
            if (message.ToLower().Contains("from the stick") && scoreboard != null)
            {
                scoreboard.OnShotSpeedChatMessage(message);
            }

            // Commands are now handled in the Prefix, so we only check for shootout messages here

            // Check for shootout detection message (supports both old and new formats)
            if (message.Contains("Shootout begins") || message.Contains("SHOOTOUT BEGINS"))
            {
                // Notify the scoreboard about shootout
                if (scoreboard != null)
                {
                    scoreboard.OnShootoutBegin(message);
                }
            }
            // Check for shooter order message (Blue: ... Red: ...) - check for rich text colors to ensure it's the order message
            else if ((message.Contains("Blue:") || message.Contains("blue:")) && (message.Contains("Red:") || message.Contains("red:")) && message.Contains("color="))
            {
                if (scoreboard != null)
                {
                    scoreboard.OnShootoutShooterOrder(message);
                }
            }
            // Check for shootout shooter messages (for tracking)
            else if (message.ToLower().Contains("shooter:"))
            {
                if (scoreboard != null)
                {
                    scoreboard.OnShootoutShooterMessage(message);
                }
            }
            // Check for shootout result messages (messages with Score during shootout)
            else if (message.Contains("Score ") && (message.Contains("no goal") || message.Contains("GOAL") || message.Contains("scores")))
            {
                if (scoreboard != null)
                {
                    scoreboard.OnShootoutResultMessage(message);
                }
            }
            // Check for shootout win messages (Blue/Red Team wins/won!)
            else if (message.ToLower().Contains("blue team win") || message.ToLower().Contains("red team win"))
            {
                if (scoreboard != null)
                {
                    // Determine which team won
                    if (message.ToLower().Contains("blue team win"))
                    {
                        scoreboard.OnShootoutWin(PlayerTeam.Blue);
                    }
                    else if (message.ToLower().Contains("red team win"))
                    {
                        scoreboard.OnShootoutWin(PlayerTeam.Red);
                    }
                }
            }
            // Check for three stars announcements
            else if (message.Contains("first star is") || message.Contains("First star is") || message.Contains("1st star:") || message.Contains("First star:"))
            {
                if (scoreboard != null)
                {
                    scoreboard.OnFirstStarAnnounced(message);
                }
            }
            else if (message.Contains("second star is") || message.Contains("Second star is") || message.Contains("2nd star:") || message.Contains("Second star:"))
            {
                if (scoreboard != null)
                {
                    scoreboard.OnSecondStarAnnounced(message);
                }
            }
            else if (message.Contains("third star is") || message.Contains("Third star is") || message.Contains("3rd star:") || message.Contains("Third star:"))
            {
                if (scoreboard != null)
                {
                    scoreboard.OnThirdStarAnnounced(message);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Scoreboard] Error in chat message postfix: " + e);
        }
    }

}
