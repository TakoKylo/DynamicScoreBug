using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace CustomScoreboard.UI
{
    public partial class CustomBroadcastScoreboard
    {
        private void Event_OnClientConnected(Dictionary<string, object> message)
        {
            try
            {
                DebugLog("Client connected to server - ensuring scoreboard is restored");
            
                // Force re-initialization when joining a server
                isInitialized = false;
                _originalScoreboardHidden = false; // Reset so we hide the original scoreboard again
                
                // Reset state and stats when connecting to server
                isInMainMenu = false;
                hasShownWinAnimation = false;
                blueTeamShots = 0;
                redTeamShots = 0;
                playerShots.Clear();
                cachedPlayerStats.Clear();
                
                // Reset shootout state in case we join mid-shootout
                isShootoutActive = false;
                shootoutLabelOverride = false;
                blueShooters.Clear();
                redShooters.Clear();
                blueShootoutResults.Clear();
                redShootoutResults.Clear();
                
                DebugLog("[CustomScoreboard] Reset all stats on server connect");
                
                // Force a poll of game state to update period display
                DOTween.Sequence()
                    .AppendInterval(0.5f)
                    .OnComplete(() => PollGameState());
                
                // The Update() method will handle recreation on next frame
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in Event_OnClientConnected: {ex.Message}");
            }
        }

        private void Event_Client_OnMainMenuShow(Dictionary<string, object> message)
        {
            try
            {
                isInMainMenu = true;
                TurnOff();
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in Event_Client_OnMainMenuShow: {ex.Message}");
            }
        }

        private void Event_Client_OnDisconnected(Dictionary<string, object> message)
        {
            try
            {
                DebugLog("[CustomScoreboard] Client disconnected event received");
                TurnOff();
                _originalScoreboardHidden = false; // Reset so we re-hide on next connect
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in Event_Client_OnDisconnected: {ex.Message}");
            }
        }

        private void Event_OnLevelStarted(Dictionary<string, object> message)
        {
            try
            {
                DebugLog("[CustomScoreboard] Level started event received");
                isInMainMenu = false;
                
                // Delay scoreboard creation to ensure everything is initialized
                DOTween.Sequence()
                    .AppendInterval(0.5f)
                    .OnComplete(() =>
                    {
                        if (!_originalScoreboardHidden)
                        {
                            HideOriginalScoreboard();
                            _originalScoreboardHidden = true;
                        }
                        PollGameState();
                        RefreshScoreboardUI();
                    });
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in Event_OnLevelStarted: {ex.Message}");
            }
        }

        private void Event_OnLevelDestroyed(Dictionary<string, object> message)
        {
            try
            {
                DebugLog("[CustomScoreboard] Level destroyed event received");
                TurnOff();
                _originalScoreboardHidden = false;
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in Event_OnLevelDestroyed: {ex.Message}");
            }
        }

        private void Event_OnPlayerGoalsChanged(Dictionary<string, object> message)
        {
            try
            {
                // Goals are tracked by Event_OnGoalScored and Event_OnGameStateChanged
                // This event is just for individual player stat tracking, no heavy refresh needed
                DebugLog("[CustomScoreboard] Player goals changed event received");
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in Event_OnPlayerGoalsChanged: {ex.Message}");
            }
        }

        private void Event_OnPlayerAssistsChanged(Dictionary<string, object> message)
        {
            try
            {
                // Assists are tracked for summary popups, no heavy refresh needed
                DebugLog("[CustomScoreboard] Player assists changed event received");
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in Event_OnPlayerAssistsChanged: {ex.Message}");
            }
        }
    }
}
