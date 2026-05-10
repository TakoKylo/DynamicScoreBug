using System;
using UnityEngine;
using UnityEngine.UIElements;
using UITK = UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        private void BuildTestsTab(VisualElement container)
        {
            // Helper to create uniform button rows
            VisualElement CreateButtonRow(string marginTop = "5")
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.flexWrap = Wrap.Wrap;
                row.style.justifyContent = Justify.FlexStart;
                row.style.marginTop = float.Parse(marginTop);
                row.style.marginBottom = 5;
                row.style.paddingLeft = 10;
                row.style.paddingRight = 10;
                return row;
            }
            
            // Section label helper
            Label CreateSectionLabel(string text)
            {
                var label = new Label(text);
                label.style.fontSize = 24;
                label.style.color = new Color(0.8f, 0.8f, 0.8f);
                label.style.marginTop = 10;
                label.style.marginBottom = 3;
                label.style.marginLeft = 10;
                return label;
            }
            
            // POPUPS SECTION
            container.Add(CreateSectionLabel("POPUP TESTS"));
            var popupsRow = CreateButtonRow("0");
            
            var testShotSpeedBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestShotSpeedPopup(); }) { text = "SHOT SPEED" };
            styleButton(testShotSpeedBtn);
            popupsRow.Add(testShotSpeedBtn);
            
            var testSavePercentBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestSavePercentagePopup(); }) { text = "SAVE %" };
            styleButton(testSavePercentBtn);
            popupsRow.Add(testSavePercentBtn);
            
            var testLineupBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestLineupPopup(); }) { text = "LINEUP" };
            styleButton(testLineupBtn);
            popupsRow.Add(testLineupBtn);
            
            var testScoringSummaryBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestScoringSummary(); }) { text = "SCORING SUMMARY" };
            styleButton(testScoringSummaryBtn);
            popupsRow.Add(testScoringSummaryBtn);
            
            var testPeriodSummaryBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestPeriodSummary(); }) { text = "PERIOD SUMMARY" };
            styleButton(testPeriodSummaryBtn);
            popupsRow.Add(testPeriodSummaryBtn);
            
            container.Add(popupsRow);
            
            // GOAL ANIMATIONS SECTION
            container.Add(CreateSectionLabel("GOAL ANIMATIONS"));
            var goalsRow = CreateButtonRow("0");
            
            var testGoalAnimBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestGoalAnimation(PlayerTeam.Blue); }) { text = "GOAL (BLUE)" };
            styleButton(testGoalAnimBtn);
            goalsRow.Add(testGoalAnimBtn);
            
            var testGoalAnimRedBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestGoalAnimation(PlayerTeam.Red); }) { text = "GOAL (RED)" };
            styleButton(testGoalAnimRedBtn);
            goalsRow.Add(testGoalAnimRedBtn);
            
            container.Add(goalsRow);
            
            // WIN ANIMATIONS SECTION
            container.Add(CreateSectionLabel("WIN ANIMATIONS"));
            var winsRow = CreateButtonRow("0");
            
            var testBlueWinBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestWinAnimation(PlayerTeam.Blue, false); }) { text = "BLUE WIN" };
            styleButton(testBlueWinBtn);
            winsRow.Add(testBlueWinBtn);
            
            var testBlueShutoutBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestWinAnimation(PlayerTeam.Blue, true); }) { text = "BLUE SHUTOUT" };
            styleButton(testBlueShutoutBtn);
            winsRow.Add(testBlueShutoutBtn);
            
            var testRedWinBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestWinAnimation(PlayerTeam.Red, false); }) { text = "RED WIN" };
            styleButton(testRedWinBtn);
            winsRow.Add(testRedWinBtn);
            
            var testRedShutoutBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestWinAnimation(PlayerTeam.Red, true); }) { text = "RED SHUTOUT" };
            styleButton(testRedShutoutBtn);
            winsRow.Add(testRedShutoutBtn);
            
            var hideWinBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestHideWinAnimation(); }) { text = "HIDE WIN" };
            styleButton(hideWinBtn);
            winsRow.Add(hideWinBtn);
            
            container.Add(winsRow);
            
            // OT/SHOOTOUT SECTION
            container.Add(CreateSectionLabel("OVERTIME & SHOOTOUT"));
            var otShootoutRow = CreateButtonRow("0");
            
            var testBlueOTWinBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestOTWin(PlayerTeam.Blue); }) { text = "BLUE OT WIN" };
            styleButton(testBlueOTWinBtn);
            otShootoutRow.Add(testBlueOTWinBtn);
            
            var testRedOTWinBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestOTWin(PlayerTeam.Red); }) { text = "RED OT WIN" };
            styleButton(testRedOTWinBtn);
            otShootoutRow.Add(testRedOTWinBtn);
            
            var testBlueSOWinBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestShootoutWin(PlayerTeam.Blue); }) { text = "BLUE SO WIN" };
            styleButton(testBlueSOWinBtn);
            otShootoutRow.Add(testBlueSOWinBtn);
            
            var testRedSOWinBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestShootoutWin(PlayerTeam.Red); }) { text = "RED SO WIN" };
            styleButton(testRedSOWinBtn);
            otShootoutRow.Add(testRedSOWinBtn);
            
            container.Add(otShootoutRow);
            
            // GAME SUMMARY SECTION
            container.Add(CreateSectionLabel("GAME SUMMARY"));
            var summaryRow = CreateButtonRow("0");
            
            var testGameSummaryBtn = new Button(() => { if (_scoreboardReference != null) _scoreboardReference.TestEndOfGameSummary(); }) { text = "GAME SUMMARY" };
            styleButton(testGameSummaryBtn);
            summaryRow.Add(testGameSummaryBtn);
            
            container.Add(summaryRow);

            // HELPFUL TIPS
            AddSection(container, "HELPFUL TIPS");
            var helpText = new Label("• /shotspeedb     - Blue team puck speed only\n" +
                                   "• /shotspeedr     - Red team puck speed only\n" +
                                   "• /shotspeed      - Both teams puck speed\n" +
                                   "• /save%b         - Blue team save percentage only\n" +
                                   "• /save%r         - Red team save percentage only\n" +
                                   "• /save%          - Both teams save percentage\n" +
                                   "• /lineup         - Lineup popup\n" +
                                   "• /summarys       - Scoring summary popup\n" +
                                   "• /summaryp       - Period summary popup\n" +
                                   "• /summaryg       - Game summary with stats and 3 stars");
            MakeReadable(helpText);
            helpText.style.whiteSpace = WhiteSpace.Normal;
            helpText.style.marginBottom = 10;
            helpText.style.paddingLeft = 10;
            container.Add(helpText);
        }
    }
}
