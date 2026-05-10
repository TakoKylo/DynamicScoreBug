using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomScoreboard.UI
{
    public partial class CustomBroadcastScoreboard
    {
        // Test methods for UI
        public void TestShotSpeedPopup()
        {
            // Puck speed is only available server-side. Show the last speeds received via server chat messages.
            DebugLog("[CustomScoreboard] Testing shot speed popup - showing last received shot speeds");
            string unitLabel = GetSpeedUnitLabel();
            ShowStatPopup(lastBlueShotSpeed > 0 ? $"{lastBlueShotSpeed:F0}" : "--", PlayerTeam.Blue, unitLabel);
            ShowStatPopup(lastRedShotSpeed > 0 ? $"{lastRedShotSpeed:F0}" : "--", PlayerTeam.Red, unitLabel);
        }
        
        public void TestSavePercentagePopup()
        {
            DebugLog("[CustomScoreboard] Testing save percentage popup");
            ShowStatPopup("0.923", PlayerTeam.Blue, "SV%");
            ShowStatPopup("0.875", PlayerTeam.Red, "SV%");
        }
        
        public void TestLineupPopup()
        {
            DebugLog("[CustomScoreboard] Testing lineup popup");
            ShowPlayerLineup();
        }
        
        public void TestScoringSummary()
        {
            DebugLog("[CustomScoreboard] Testing scoring summary - showing last goal");
            
            if (lastGoalMessage != null)
            {
                // Show the actual last goal that was scored
                ShowScoringSummary(lastGoalMessage);
            }
            else
            {
                DebugWarning("[CustomScoreboard] No goal has been scored yet this game");
                // Show a message or do nothing
            }
        }
        
        public void TestPeriodSummary()
        {
            DebugLog("[CustomScoreboard] Testing period summary");
            ShowPeriodSummary();
        }
        
        public void TestEndOfGameSummary()
        {
            DebugLog("[CustomScoreboard] Testing end-of-game summary");
            // Set some test three stars if none exist
            if (string.IsNullOrEmpty(firstStar))
            {
                firstStar = "Test Player 1";
                secondStar = "Test Player 2";
                thirdStar = "Test Player 3";
            }
            ShowEndOfGameSummary();
        }
        
        public void ShowGoalieStats()
        {
            DebugLog("[CustomScoreboard] Showing goalie stats");
            // Calculate and show save percentages for both teams
            // Blue goalie save% = (red shots - blue goals allowed) / red shots
            int blueSavesCalc = redTeamShots - lastRedScore;
            float blueSavePercent = redTeamShots > 0 ? (float)blueSavesCalc / (float)redTeamShots : 0f;
            // Red goalie save% = (blue shots - red goals allowed) / blue shots  
            int redSavesCalc = blueTeamShots - lastBlueScore;
            float redSavePercent = blueTeamShots > 0 ? (float)redSavesCalc / (float)blueTeamShots : 0f;
            
            ShowStatPopup(blueSavePercent.ToString("0.000"), PlayerTeam.Blue, "SV%");
            ShowStatPopup(redSavePercent.ToString("0.000"), PlayerTeam.Red, "SV%");
        }
        
        // Separate blue/red save% commands
        public void ShowSavePercentBlue()
        {
            DebugLog("[CustomScoreboard] Showing blue goalie save %");
            // Blue goalie save% = (red shots - blue goals allowed) / red shots
            int blueSavesCalc = redTeamShots - lastRedScore;
            float blueSavePercent = redTeamShots > 0 ? (float)blueSavesCalc / (float)redTeamShots : 0f;
            ShowStatPopup(blueSavePercent.ToString("0.000"), PlayerTeam.Blue, "SV%");
        }
        
        public void ShowSavePercentRed()
        {
            DebugLog("[CustomScoreboard] Showing red goalie save %");
            // Red goalie save% = (blue shots - red goals allowed) / blue shots
            int redSavesCalc = blueTeamShots - lastBlueScore;
            float redSavePercent = blueTeamShots > 0 ? (float)redSavesCalc / (float)blueTeamShots : 0f;
            ShowStatPopup(redSavePercent.ToString("0.000"), PlayerTeam.Red, "SV%");
        }
        
        // Separate blue/red shot speed commands
        public void ShowShotSpeedBlue()
        {
            string unitLabel = GetSpeedUnitLabel();
            DebugLog("[CustomScoreboard] Showing blue team last shot speed");
            if (lastBlueShotSpeed > 0)
            {
                ShowStatPopup($"{lastBlueShotSpeed:F0}", PlayerTeam.Blue, unitLabel);
            }
            else
            {
                ShowStatPopup("--", PlayerTeam.Blue, unitLabel);
            }
        }
        
        public void ShowShotSpeedRed()
        {
            string unitLabel = GetSpeedUnitLabel();
            DebugLog("[CustomScoreboard] Showing red team last shot speed");
            if (lastRedShotSpeed > 0)
            {
                ShowStatPopup($"{lastRedShotSpeed:F0}", PlayerTeam.Red, unitLabel);
            }
            else
            {
                ShowStatPopup("--", PlayerTeam.Red, unitLabel);
            }
        }
        
        public void TestGoalAnimation(PlayerTeam team = PlayerTeam.Blue)
        {
            ShowGoalAnimation(team, () => {
                DebugLog($"[CustomScoreboard] Test goal animation completed for {team}");
            }, false);
        }
        
        public void TestWinAnimation(PlayerTeam team, bool isShutout)
        {
            ShowWinAnimation(team, isShutout);
        }
        
        public void TestHideWinAnimation()
        {
            HideWinAnimation();
        }

        public void TestOTWin(PlayerTeam team)
        {
            ShowOTWinAnimation(team, null);
        }

        public void TestShootoutStart()
        {
            OnShootoutBegin();
        }

        public void TestShootoutWin(PlayerTeam team)
        {
            isShootoutActive = false;
            ShowShootoutWinAnimation(team);
        }
    }
}
