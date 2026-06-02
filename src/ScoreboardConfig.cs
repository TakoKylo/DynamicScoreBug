using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CustomScoreboard
{
    /// <summary>
    /// Centralized config path helper for Scoreboard - handles ModHub migration
    /// </summary>
    public static class ScoreboardPaths
    {
        private static string _gameRoot;
        private static string _configDir;
        private static bool _initialized = false;
        
        public static string GameRoot
        {
            get
            {
                EnsureInitialized();
                return _gameRoot;
            }
        }
        
        /// <summary>
        /// Returns config/ModHub/Scoreboard path (new location)
        /// </summary>
        public static string ConfigDir
        {
            get
            {
                EnsureInitialized();
                return _configDir;
            }
        }
        
        public static string LogosDir => Path.Combine(ConfigDir, "scorebuglogos");
        public static string PresetPacksDir => Path.Combine(ConfigDir, "PresetPacks");
        public static string ConfigPath => Path.Combine(ConfigDir, "CustomScoreboard.json");
        public static string PresetsPath => Path.Combine(ConfigDir, "ScoreboardPresets.json");
        
        private static void EnsureInitialized()
        {
            if (_initialized) return;
            
            // Set initialized FIRST to prevent re-entry from property getters
            _initialized = true;
            
            try
            {
                _gameRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string configRoot = Path.Combine(_gameRoot, "config");
                string modHubDir = Path.Combine(configRoot, "ModHub");
                _configDir = Path.Combine(modHubDir, "Scoreboard");
                
                // Migrate from old location (config/Scoreboard) if needed
                string legacyDir = Path.Combine(configRoot, "Scoreboard");
                string legacyMigratedDir = legacyDir + "_migrated";
                
                if (Directory.Exists(legacyDir) && !Directory.Exists(legacyMigratedDir))
                {
                    try
                    {
                        Directory.CreateDirectory(modHubDir);
                        
                        // Copy all contents to new location (don't move, so we can mark as migrated)
                        CopyDirectory(legacyDir, _configDir);
                        Debug.Log($"[Scoreboard] Copied config folder from {legacyDir} to {_configDir}");
                        
                        // Rename old folder to mark as migrated
                        try
                        {
                            Directory.Move(legacyDir, legacyMigratedDir);
                            Debug.Log($"[Scoreboard] Renamed old folder to {legacyMigratedDir}");
                        }
                        catch (Exception renameEx)
                        {
                            Debug.LogWarning($"[Scoreboard] Could not rename old folder: {renameEx.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Scoreboard] Failed to migrate config folder: {ex.Message}");
                    }
                }
                
                // Ensure directories exist - use local paths to avoid property re-entry
                Directory.CreateDirectory(_configDir);
                Directory.CreateDirectory(Path.Combine(_configDir, "scorebuglogos"));
                Directory.CreateDirectory(Path.Combine(_configDir, "Stats"));
                Directory.CreateDirectory(Path.Combine(_configDir, "PresetPacks"));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scoreboard] Failed to initialize paths: {ex.Message}");
                _initialized = false; // Allow retry on failure
            }
        }
        
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
        
        /// <summary>
        /// Get old config locations for migration checking
        /// </summary>
        public static string[] GetLegacyLogosLocations()
        {
            EnsureInitialized();
            return new[]
            {
                Path.Combine(_gameRoot, "scorebuglogos"),
                Path.Combine(_gameRoot, "Plugins", "Scoreboard", "scorebuglogos"),
                Path.Combine(_gameRoot, "config", "scorebuglogos"),
                Path.Combine(_gameRoot, "config", "Scoreboard", "scorebuglogos")
            };
        }
    }

    [Serializable]
    public class TeamPreset
    {
        public string presetName = "PPHL Away";
        public string packName = ""; // Workshop pack name, empty for built-in presets
        public string teamName = " Away";
        public string teamColorHex = "#FFFFFF";
        public string teamTextColorHex = "#00205b";
        public string borderColorHex = "#00205b";
        public string gradientLeftColorHex = "#FFFFFF";
        public string gradientRightColorHex = "#000000";
        public string logoFile = "leaguelogo.png";
        public float logoWidth = 100f;
        public float logoHeight = 100f;
        public float logoOffsetX = 93f;
        public float logoOffsetY = -37f;
        public string minimapPlayerColorHex = ""; // If empty, uses teamColorHex
        public string minimapNumberColorHex = ""; // If empty, uses teamTextColorHex
        public string trlPresetName = ""; // Optional: name of a ToasterReskinLoader preset to apply (to this side) when this team preset is applied. Empty = don't touch TRL.
    }

    [Serializable]
    public class SizePreset
    {
        public string presetName = "Default";
        public string packName = ""; // Workshop pack name, empty for built-in presets
        
        // Scoreboard positioning
        public float scoreboardX = -300f;
        public float scoreboardY = 35f;
        public float scoreboardScale = 2.03f;
        
        // League logo file, positioning and size
        public string leagueLogoFile = "NHL.png";
        public float leagueLogoOffsetX = -135f;
        public float leagueLogoOffsetY = -4f;
        public float leagueLogoWidth = 45f;
        public float leagueLogoHeight = 50f;
        
        // Popup positioning
        public float lineupPopupOffsetY = 80f;
        public float lineupPopupOffsetX = 269f;
        public float blueStatPopupOffsetX = 45f;
        public float redStatPopupOffsetX = 650f;
        public float statPopupOffsetY = 70f;
        
        // Scoring summary popup positioning
        public float scoringSummaryX = 269f;
        public float scoringSummaryY = 70f;
        public float scoringSummaryScale = 1f;
        
        // Period summary popup positioning
        public float periodSummaryX = 269f;
        public float periodSummaryY = 90f;
        public float periodSummaryScale = 1f;
        
        // Goal overlay positioning and size
        public float goalOverlayOffsetX = 350f;
        public float goalOverlayWidth = 741f;
        public float goalOverlayHeight = 42f;
        
        // Animation logo overlay settings
        public float animationLogoWidth = 500f;
        public float animationLogoHeight = 500f;
        
        // Section sizes and colors
        public float leagueLogoSectionWidth = 20f;
        public string leagueLogoSectionColorHex = "#262626";
        public string leagueLogoSectionBorderColorHex = "#1A1A1A";
        public float periodBoxWidth = 80f;
        public string periodBoxColorHex = "#1A1A1A";
        public string periodBoxBorderColorHex = "#0D0D0D";
        public string periodTextColorHex = "#FFFFFF";
        public float timeBoxWidth = 120f;
        public string timeBoxColorHex = "#4D4D4D";
        public string timeBoxBorderColorHex = "#333333";
        public string timeTextColorHex = "#FFFFFF";
        
        // Popup animation settings - individual distances for each popup type
        public float statPopupSlideDistance = 18f;
        public float lineupPopupSlideDistance = 66f;
        public float scoringSummarySlideDistance = 60f;
        public float periodSummarySlideDistance = 66f;
        
        // Popup size settings - individual width/height for each popup type
        public float statPopupWidth = 80f;
        public float statPopupHeight = 20f;
        public float lineupPopupWidth = 740f;
        public float lineupPopupHeight = 40f;
        public float scoringSummaryWidth = 740f;
        public float scoringSummaryHeight = 20f;
        public float periodSummaryWidth = 740f;
        public float periodSummaryHeight = 40f;
        
        // Game summary popup positioning and size
        public float gameSummaryOffsetX = 940f;
        public float gameSummaryOffsetY = 100f;
        public float gameSummarySlideDistance = 160f;
        public float gameSummaryWidth = 800f;
        public float gameSummaryHeight = 300f;
    }

    [Serializable]
    public class PresetsConfig
    {
        public List<TeamPreset> teamPresets = new List<TeamPreset>();
        public List<SizePreset> sizePresets = new List<SizePreset>();
    }

    [Serializable]
    public class ScoreboardConfig
    {
        // Debug settings
        public bool enableDebugLogs = false; // Toggle debug logging on/off (default off)
        
        // Scoreboard settings
        public bool enableCustomScoreboard = true;
        public bool enableAnimations = true; // Controls all popups and animations (goal, win, stat popups, etc)
        public bool enablePopups = true; // Controls all popup overlays (goal, game summary, etc)
        public bool enableMinimapColors = true; // Enable/disable custom minimap colors
        public bool syncTeamColorsToTRL = true; // If true, push team colors into ToasterReskinLoader (equipment + minimap player icons). If false, the scorebug keeps using its own team colors but leaves TRL's team colors untouched, so TRL/minimap colors can be controlled independently.
        public bool enableCompactLayout = false; // Compact layout: teams stacked vertically, period/time on right, league logo on left
        public float scoreboardX = -350f;
        public float scoreboardY = 40f;
        public float scoreboardScale = 1.68f;
        public float scoreboardOpacity = 1.0f; // Global opacity for all scoreboard sections (0-1, excludes text)
        
        // Currently selected size preset name
        public string selectedSizePreset = "Normal PPHL";
        
        // Team colors
        public string blueTeamColorHex = "#FFFFFF";
        public string redTeamColorHex = "#ce1126";
        
        // Team text colors (for names, scores, shots on scoreboard and goal animation)
        public string blueTeamTextColorHex = "#00205b";
        public string redTeamTextColorHex = "#FFFFFF";
        
        // Minimap player colors (if empty, uses team colors)
        public string blueMinimapPlayerColorHex = "";
        public string redMinimapPlayerColorHex = "";
        
        // Minimap number colors (if empty, uses team text colors)
        public string blueMinimapNumberColorHex = "";
        public string redMinimapNumberColorHex = "";
        
        // Team names
        public string blueTeamName = " Away";
        public string redTeamName = " Home";
        
        // Logo settings
        public string leagueLogoFile = "PPHL.png";
        public string blueTeamLogoFile = "leaguelogo.png"; 
        public string redTeamLogoFile = "leaguelogo.png";
        
        // Individual logo sizes
        public float leagueLogoWidth = 50f;
        public float leagueLogoHeight = 55f;
        public float blueLogoWidth = 100f;
        public float blueLogoHeight = 100f;
        public float redLogoWidth = 100f;
        public float redLogoHeight = 100f;
        
        // Logo positions (X and Y offsets)
        public float leagueLogoOffsetX = -101f;
        public float leagueLogoOffsetY = -4f;
        public float blueLogoOffsetX = 93f;
        public float blueLogoOffsetY = -37f;
        public float redLogoOffsetX = 93f;
        public float redLogoOffsetY = -37f;
        
        // Border settings
        public float borderWidth = 2f;
        public string blueBorderColorHex = "#00205b";
        public string redBorderColorHex = "#FFFFFF";
        
        // Gradient fade colors (left and right edges)
        public bool enableGradients = false;
        public string blueGradientLeftColorHex = "#FFFFFF";
        public string blueGradientRightColorHex = "#000000";
        public string redGradientLeftColorHex = "#FFFFFF";
        public string redGradientRightColorHex = "#000000";
        
        // Section sizes and colors
        public float leagueLogoSectionWidth = 20f;
        public string leagueLogoSectionColorHex = "#262626";
        public string leagueLogoSectionBorderColorHex = "#1A1A1A";
        public float periodBoxWidth = 80f;
        public string periodBoxColorHex = "#1A1A1A";
        public string periodBoxBorderColorHex = "#0D0D0D";
        public string periodTextColorHex = "#FFFFFF";
        public float timeBoxWidth = 120f;
        public string timeBoxColorHex = "#4D4D4D";
        public string timeBoxBorderColorHex = "#333333";
        public string timeTextColorHex = "#FFFFFF";

        // Popup positioning
        public float lineupPopupOffsetY = 80f; // Distance below scoreboard
        public float lineupPopupOffsetX = 276f; // X offset from scoreboard center for lineup popup
        public float blueStatPopupOffsetX = 97f; // X offset from scoreboard center for blue popup
        public float redStatPopupOffsetX = 597f; // X offset from scoreboard center for red popup
        public float statPopupOffsetY = 11f; // Distance below scoreboard for stat popups
        
        // Scoring summary popup positioning (follows scoreboard with offsets)
        public float scoringSummaryX = 276f; // X offset from scoreboard center
        public float scoringSummaryY = 30f; // Distance below scoreboard
        public float scoringSummaryScale = 1f; // Scale multiplier
        
        // Period summary popup positioning (follows scoreboard with offsets)
        public float periodSummaryX = 276f; // X offset from scoreboard center
        public float periodSummaryY = 367f; // Distance below scoreboard
        public float periodSummaryScale = 1f; // Scale multiplier
        
        // Goal overlay positioning and size
        public float goalOverlayOffsetX = 355f; // X offset from center position
        public float goalOverlayWidth = 741f; // Goal animation width
        public float goalOverlayHeight = 42f; // Goal animation height
        
        // Animation logo overlay settings
        public float animationLogoOpacity = 0.8f; // Opacity of team logo behind goal/win animations (0-1)
        public float animationLogoWidth = 500f; // Width of team logo behind animations
        public float animationLogoHeight = 500f; // Height of team logo behind animations
        public bool showAnimationLogo = true; // Whether to show team logo behind animations
        
        // Popup animation settings - individual distances for each popup type
        public float statPopupSlideDistance = 50f; // How far down stat popups (shot speed, save %) slide
        public float lineupPopupSlideDistance = 66f; // How far down lineup popup slides
        public float scoringSummarySlideDistance = 30f; // How far down scoring summary slides
        public float periodSummarySlideDistance = 66f; // How far down period summary slides
        
        // Popup size settings - individual width/height for each popup type
        public float statPopupWidth = 80f;
        public float statPopupHeight = 20f;
        public float lineupPopupWidth = 735f;
        public float lineupPopupHeight = 40f;
        public float scoringSummaryWidth = 740f;
        public float scoringSummaryHeight = 20f;
        public float periodSummaryWidth = 740f;
        public float periodSummaryHeight = 40f;
        
        // Game summary popup positioning and size
        public float gameSummaryOffsetX = 940f; // X offset from scoreboard center
        public float gameSummaryOffsetY = 100f; // Distance below scoreboard
        public float gameSummarySlideDistance = 160f; // How far down game summary slides
        public float gameSummaryWidth = 800f; // Game summary width
        public float gameSummaryHeight = 300f; // Game summary height (not used, content-based)
        
        // Production staff names for lineup display
        public string playByPlayName = "";
        public string colorCommentatorName = "";
        public string producerName = "Amikiir, Dog, Bleh";

        // Default constructor with default values
        public ScoreboardConfig()
        {
            // All defaults are set above in field declarations
        }
        
        public TeamPreset CreateBluePresetFromCurrent(string presetName)
        {
            return new TeamPreset
            {
                presetName = presetName,
                teamName = blueTeamName,
                teamColorHex = blueTeamColorHex,
                teamTextColorHex = blueTeamTextColorHex,
                borderColorHex = blueBorderColorHex,
                gradientLeftColorHex = blueGradientLeftColorHex,
                gradientRightColorHex = blueGradientRightColorHex,
                logoFile = blueTeamLogoFile,
                logoWidth = blueLogoWidth,
                logoHeight = blueLogoHeight,
                logoOffsetX = blueLogoOffsetX,
                logoOffsetY = blueLogoOffsetY,
                minimapPlayerColorHex = blueMinimapPlayerColorHex,
                minimapNumberColorHex = blueMinimapNumberColorHex
            };
        }
        
        public TeamPreset CreateRedPresetFromCurrent(string presetName)
        {
            return new TeamPreset
            {
                presetName = presetName,
                teamName = redTeamName,
                teamColorHex = redTeamColorHex,
                teamTextColorHex = redTeamTextColorHex,
                borderColorHex = redBorderColorHex,
                gradientLeftColorHex = redGradientLeftColorHex,
                gradientRightColorHex = redGradientRightColorHex,
                logoFile = redTeamLogoFile,
                logoWidth = redLogoWidth,
                logoHeight = redLogoHeight,
                logoOffsetX = redLogoOffsetX,
                logoOffsetY = redLogoOffsetY,
                minimapPlayerColorHex = redMinimapPlayerColorHex,
                minimapNumberColorHex = redMinimapNumberColorHex
            };
        }
        
        public SizePreset CreateSizePresetFromCurrent(string presetName)
        {
            return new SizePreset
            {
                presetName = presetName,
                scoreboardX = scoreboardX,
                scoreboardY = scoreboardY,
                scoreboardScale = scoreboardScale,
                leagueLogoFile = leagueLogoFile,
                leagueLogoOffsetX = leagueLogoOffsetX,
                leagueLogoOffsetY = leagueLogoOffsetY,
                leagueLogoWidth = leagueLogoWidth,
                leagueLogoHeight = leagueLogoHeight,
                lineupPopupOffsetY = lineupPopupOffsetY,
                lineupPopupOffsetX = lineupPopupOffsetX,
                blueStatPopupOffsetX = blueStatPopupOffsetX,
                redStatPopupOffsetX = redStatPopupOffsetX,
                statPopupOffsetY = statPopupOffsetY,
                scoringSummaryX = scoringSummaryX,
                scoringSummaryY = scoringSummaryY,
                scoringSummaryScale = scoringSummaryScale,
                periodSummaryX = periodSummaryX,
                periodSummaryY = periodSummaryY,
                periodSummaryScale = periodSummaryScale,
                goalOverlayOffsetX = goalOverlayOffsetX,
                goalOverlayWidth = goalOverlayWidth,
                goalOverlayHeight = goalOverlayHeight,
                animationLogoWidth = animationLogoWidth,
                animationLogoHeight = animationLogoHeight,
                leagueLogoSectionWidth = leagueLogoSectionWidth,
                leagueLogoSectionColorHex = leagueLogoSectionColorHex,
                leagueLogoSectionBorderColorHex = leagueLogoSectionBorderColorHex,
                periodBoxWidth = periodBoxWidth,
                periodBoxColorHex = periodBoxColorHex,
                periodBoxBorderColorHex = periodBoxBorderColorHex,
                periodTextColorHex = periodTextColorHex,
                timeBoxWidth = timeBoxWidth,
                timeBoxColorHex = timeBoxColorHex,
                timeBoxBorderColorHex = timeBoxBorderColorHex,
                timeTextColorHex = timeTextColorHex,
                statPopupSlideDistance = statPopupSlideDistance,
                lineupPopupSlideDistance = lineupPopupSlideDistance,
                scoringSummarySlideDistance = scoringSummarySlideDistance,
                periodSummarySlideDistance = periodSummarySlideDistance,
                statPopupWidth = statPopupWidth,
                statPopupHeight = statPopupHeight,
                lineupPopupWidth = lineupPopupWidth,
                lineupPopupHeight = lineupPopupHeight,
                scoringSummaryWidth = scoringSummaryWidth,
                scoringSummaryHeight = scoringSummaryHeight,
                periodSummaryWidth = periodSummaryWidth,
                periodSummaryHeight = periodSummaryHeight,
                gameSummaryOffsetX = gameSummaryOffsetX,
                gameSummaryOffsetY = gameSummaryOffsetY,
                gameSummarySlideDistance = gameSummarySlideDistance,
                gameSummaryWidth = gameSummaryWidth,
                gameSummaryHeight = gameSummaryHeight
            };
        }
        
        public void ApplySizePreset(SizePreset preset)
        {
            scoreboardX = preset.scoreboardX;
            scoreboardY = preset.scoreboardY;
            scoreboardScale = preset.scoreboardScale;
            leagueLogoFile = preset.leagueLogoFile;
            leagueLogoOffsetX = preset.leagueLogoOffsetX;
            leagueLogoOffsetY = preset.leagueLogoOffsetY;
            leagueLogoWidth = preset.leagueLogoWidth;
            leagueLogoHeight = preset.leagueLogoHeight;
            lineupPopupOffsetY = preset.lineupPopupOffsetY;
            lineupPopupOffsetX = preset.lineupPopupOffsetX;
            blueStatPopupOffsetX = preset.blueStatPopupOffsetX;
            redStatPopupOffsetX = preset.redStatPopupOffsetX;
            statPopupOffsetY = preset.statPopupOffsetY;
            scoringSummaryX = preset.scoringSummaryX;
            scoringSummaryY = preset.scoringSummaryY;
            scoringSummaryScale = preset.scoringSummaryScale;
            periodSummaryX = preset.periodSummaryX;
            periodSummaryY = preset.periodSummaryY;
            periodSummaryScale = preset.periodSummaryScale;
            goalOverlayOffsetX = preset.goalOverlayOffsetX;
            goalOverlayWidth = preset.goalOverlayWidth;
            goalOverlayHeight = preset.goalOverlayHeight;
            animationLogoWidth = preset.animationLogoWidth;
            animationLogoHeight = preset.animationLogoHeight;
            leagueLogoSectionWidth = preset.leagueLogoSectionWidth;
            leagueLogoSectionColorHex = preset.leagueLogoSectionColorHex;
            leagueLogoSectionBorderColorHex = preset.leagueLogoSectionBorderColorHex;
            periodBoxWidth = preset.periodBoxWidth;
            periodBoxColorHex = preset.periodBoxColorHex;
            periodBoxBorderColorHex = preset.periodBoxBorderColorHex;
            periodTextColorHex = preset.periodTextColorHex;
            timeBoxWidth = preset.timeBoxWidth;
            timeBoxColorHex = preset.timeBoxColorHex;
            timeBoxBorderColorHex = preset.timeBoxBorderColorHex;
            timeTextColorHex = preset.timeTextColorHex;
            statPopupSlideDistance = preset.statPopupSlideDistance;
            lineupPopupSlideDistance = preset.lineupPopupSlideDistance;
            scoringSummarySlideDistance = preset.scoringSummarySlideDistance;
            periodSummarySlideDistance = preset.periodSummarySlideDistance;
            statPopupWidth = preset.statPopupWidth;
            statPopupHeight = preset.statPopupHeight;
            lineupPopupWidth = preset.lineupPopupWidth;
            lineupPopupHeight = preset.lineupPopupHeight;
            scoringSummaryWidth = preset.scoringSummaryWidth;
            scoringSummaryHeight = preset.scoringSummaryHeight;
            periodSummaryWidth = preset.periodSummaryWidth;
            periodSummaryHeight = preset.periodSummaryHeight;
            gameSummaryOffsetX = preset.gameSummaryOffsetX;
            gameSummaryOffsetY = preset.gameSummaryOffsetY;
            gameSummarySlideDistance = preset.gameSummarySlideDistance;
            gameSummaryWidth = preset.gameSummaryWidth;
            gameSummaryHeight = preset.gameSummaryHeight;
            selectedSizePreset = preset.presetName;
        }
        
        public SizePreset ToSizePreset()
        {
            return new SizePreset
            {
                presetName = "Current Settings",
                scoreboardX = scoreboardX,
                scoreboardY = scoreboardY,
                scoreboardScale = scoreboardScale,
                leagueLogoFile = leagueLogoFile,
                leagueLogoOffsetX = leagueLogoOffsetX,
                leagueLogoOffsetY = leagueLogoOffsetY,
                leagueLogoWidth = leagueLogoWidth,
                leagueLogoHeight = leagueLogoHeight,
                lineupPopupOffsetY = lineupPopupOffsetY,
                lineupPopupOffsetX = lineupPopupOffsetX,
                blueStatPopupOffsetX = blueStatPopupOffsetX,
                redStatPopupOffsetX = redStatPopupOffsetX,
                statPopupOffsetY = statPopupOffsetY,
                scoringSummaryX = scoringSummaryX,
                scoringSummaryY = scoringSummaryY,
                scoringSummaryScale = scoringSummaryScale,
                periodSummaryX = periodSummaryX,
                periodSummaryY = periodSummaryY,
                periodSummaryScale = periodSummaryScale,
                goalOverlayOffsetX = goalOverlayOffsetX,
                goalOverlayWidth = goalOverlayWidth,
                goalOverlayHeight = goalOverlayHeight,
                animationLogoWidth = animationLogoWidth,
                animationLogoHeight = animationLogoHeight,
                leagueLogoSectionWidth = leagueLogoSectionWidth,
                leagueLogoSectionColorHex = leagueLogoSectionColorHex,
                leagueLogoSectionBorderColorHex = leagueLogoSectionBorderColorHex,
                periodBoxWidth = periodBoxWidth,
                periodBoxColorHex = periodBoxColorHex,
                periodBoxBorderColorHex = periodBoxBorderColorHex,
                periodTextColorHex = periodTextColorHex,
                timeBoxWidth = timeBoxWidth,
                timeBoxColorHex = timeBoxColorHex,
                timeBoxBorderColorHex = timeBoxBorderColorHex,
                timeTextColorHex = timeTextColorHex,
                statPopupSlideDistance = statPopupSlideDistance,
                lineupPopupSlideDistance = lineupPopupSlideDistance,
                scoringSummarySlideDistance = scoringSummarySlideDistance,
                periodSummarySlideDistance = periodSummarySlideDistance,
                statPopupWidth = statPopupWidth,
                statPopupHeight = statPopupHeight,
                lineupPopupWidth = lineupPopupWidth,
                lineupPopupHeight = lineupPopupHeight,
                scoringSummaryWidth = scoringSummaryWidth,
                scoringSummaryHeight = scoringSummaryHeight,
                periodSummaryWidth = periodSummaryWidth,
                periodSummaryHeight = periodSummaryHeight,
                gameSummaryOffsetX = gameSummaryOffsetX,
                gameSummaryOffsetY = gameSummaryOffsetY,
                gameSummarySlideDistance = gameSummarySlideDistance,
                gameSummaryWidth = gameSummaryWidth,
                gameSummaryHeight = gameSummaryHeight
            };
        }
    }
}