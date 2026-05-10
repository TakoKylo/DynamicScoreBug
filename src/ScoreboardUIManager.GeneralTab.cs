using System;
using UnityEngine;
using UnityEngine.UIElements;
using UITK = UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        private void BuildGeneralTab(VisualElement container)
        {
            // ======================
            // MAIN TOGGLES (scoreboard and animation enable/disable)
            // ======================
            AddSection(container, "GENERAL SETTINGS");
            
            container.Add(MakeToggleRow("ENABLE SCOREBOARD", _config.enableCustomScoreboard, (value) => 
            {
                _config.enableCustomScoreboard = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null)
                {
                    var customScoreboard = _scoreboardReference as CustomBroadcastScoreboard;
                    if (customScoreboard != null)
                    {
                        if (value)
                        {
                            customScoreboard.RefreshScoreboardUI();
                        }
                        else
                        {
                            customScoreboard.RemoveScoreboardUI();
                            Scoreboard[] scoreboards = UnityEngine.Object.FindObjectsByType<Scoreboard>(FindObjectsSortMode.None);
                            foreach (Scoreboard sb in scoreboards)
                            {
                                sb.gameObject.SetActive(true);
                            }
                            try
                            {
                                var uiGameState = MonoBehaviourSingleton<UIManager>.Instance?.GameState;
                                if (uiGameState != null) uiGameState.Show();
                            }
                            catch (Exception e) { Debug.LogError($"[ScoreboardUI] Failed to show UIGameState: {e}"); }
                        }
                    }
                }
            }));
            
            container.Add(MakeToggleRow("ENABLE ANIMATIONS", _config.enableAnimations, (value) => 
            {
                _config.enableAnimations = value;
                SaveScoreboardConfig(_config);
            }));
            
            container.Add(MakeToggleRow("ENABLE POPUPS", _config.enablePopups, (value) => 
            {
                _config.enablePopups = value;
                SaveScoreboardConfig(_config);
            }));
            
            container.Add(MakeToggleRow("ENABLE MINIMAP COLORS", _config.enableMinimapColors, (value) => 
            {
                _config.enableMinimapColors = value;
                SaveScoreboardConfig(_config);
            }));
            
            container.Add(MakeToggleRow("ENABLE DEBUG LOGS", _config.enableDebugLogs, (value) => 
            {
                _config.enableDebugLogs = value;
                SaveScoreboardConfig(_config);
            }));
            
            Slider opacitySlider = null;
            TextField opacityField = null;
            container.Add(MakeSliderRow("SCOREBOARD OPACITY", _config.scoreboardOpacity, 0f, 1f, out opacitySlider, out opacityField, (value) => 
            {
                _config.scoreboardOpacity = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.UpdateOpacity(_config);
            }));

            // ======================
            // PRODUCTION STAFF (outside advanced settings for easy access)
            // ======================
            AddSection(container, "PRODUCTION STAFF");
            container.Add(MakeTextFieldRow("PLAY-BY-PLAY", _config.playByPlayName, (value) => {
                _config.playByPlayName = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("COLOR COMMENTATOR", _config.colorCommentatorName, (value) => {
                _config.colorCommentatorName = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("PRODUCER", _config.producerName, (value) => {
                _config.producerName = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
        }
    }
}
