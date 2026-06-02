using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        private void ParsePresets(string arrayJson, List<TeamPreset> presetList)
        {
            // Find all preset objects in the array
            int searchStart = 1; // Skip opening bracket
            while (searchStart < arrayJson.Length)
            {
                int objStart = arrayJson.IndexOf('{', searchStart);
                if (objStart < 0) break;
                
                int objEnd = FindMatchingBrace(arrayJson, objStart);
                if (objEnd < 0) break;
                
                string objJson = arrayJson.Substring(objStart, objEnd - objStart + 1);
                TeamPreset preset = new TeamPreset();
                
                preset.presetName = ParseStringValue(objJson, "presetName");
                if (string.IsNullOrEmpty(preset.presetName)) preset.presetName = "Preset";
                
                preset.teamName = ParseStringValue(objJson, "teamName");
                if (string.IsNullOrEmpty(preset.teamName)) preset.teamName = "TEAM";
                
                preset.teamColorHex = ParseStringValue(objJson, "teamColorHex");
                if (string.IsNullOrEmpty(preset.teamColorHex)) preset.teamColorHex = "#2E59C1";
                
                preset.teamTextColorHex = ParseStringValue(objJson, "teamTextColorHex");
                if (string.IsNullOrEmpty(preset.teamTextColorHex)) preset.teamTextColorHex = "#FFFFFF";
                
                preset.borderColorHex = ParseStringValue(objJson, "borderColorHex");
                if (string.IsNullOrEmpty(preset.borderColorHex)) preset.borderColorHex = "#1E3A8A";
                
                preset.gradientLeftColorHex = ParseStringValue(objJson, "gradientLeftColorHex");
                if (string.IsNullOrEmpty(preset.gradientLeftColorHex)) preset.gradientLeftColorHex = "#FFFFFF";
                
                preset.gradientRightColorHex = ParseStringValue(objJson, "gradientRightColorHex");
                if (string.IsNullOrEmpty(preset.gradientRightColorHex)) preset.gradientRightColorHex = "#000000";
                
                preset.logoFile = ParseStringValue(objJson, "logoFile");
                if (string.IsNullOrEmpty(preset.logoFile)) preset.logoFile = "blueteamlogo.png";
                
                preset.logoWidth = ParseFloatValue(objJson, "logoWidth");
                preset.logoHeight = ParseFloatValue(objJson, "logoHeight");
                preset.logoOffsetX = ParseFloatValue(objJson, "logoOffsetX");
                preset.logoOffsetY = ParseFloatValue(objJson, "logoOffsetY");
                
                preset.minimapPlayerColorHex = ParseStringValue(objJson, "minimapPlayerColorHex");
                preset.minimapNumberColorHex = ParseStringValue(objJson, "minimapNumberColorHex");
                // Empty strings are valid - means use team color/text color

                preset.trlPresetName = ParseStringValue(objJson, "trlPresetName");
                // Empty string is valid - means no linked ToasterReskinLoader preset

                presetList.Add(preset);
                
                searchStart = objEnd + 1;
            }
        }
        
        private void ParseSizePresets(string arrayJson, List<SizePreset> presetList)
        {
            // Find all preset objects in the array
            int searchStart = 1; // Skip opening bracket
            while (searchStart < arrayJson.Length)
            {
                int objStart = arrayJson.IndexOf('{', searchStart);
                if (objStart < 0) break;
                
                int objEnd = FindMatchingBrace(arrayJson, objStart);
                if (objEnd < 0) break;
                
                string objJson = arrayJson.Substring(objStart, objEnd - objStart + 1);
                SizePreset preset = new SizePreset();
                
                preset.presetName = ParseStringValue(objJson, "presetName");
                if (string.IsNullOrEmpty(preset.presetName)) preset.presetName = "Size Preset";
                
                preset.scoreboardX = ParseFloatValue(objJson, "scoreboardX");
                preset.scoreboardY = ParseFloatValue(objJson, "scoreboardY");
                preset.scoreboardScale = ParseFloatValue(objJson, "scoreboardScale");
                preset.leagueLogoFile = ParseStringValue(objJson, "leagueLogoFile");
                if (string.IsNullOrEmpty(preset.leagueLogoFile)) preset.leagueLogoFile = "nhl.png";
                preset.leagueLogoOffsetX = ParseFloatValue(objJson, "leagueLogoOffsetX");
                preset.leagueLogoOffsetY = ParseFloatValue(objJson, "leagueLogoOffsetY");
                preset.leagueLogoWidth = ParseFloatValue(objJson, "leagueLogoWidth");
                preset.leagueLogoHeight = ParseFloatValue(objJson, "leagueLogoHeight");
                preset.lineupPopupOffsetY = ParseFloatValue(objJson, "lineupPopupOffsetY");
                preset.lineupPopupOffsetX = ParseFloatValue(objJson, "lineupPopupOffsetX");
                preset.blueStatPopupOffsetX = ParseFloatValue(objJson, "blueStatPopupOffsetX");
                preset.redStatPopupOffsetX = ParseFloatValue(objJson, "redStatPopupOffsetX");
                preset.statPopupOffsetY = ParseFloatValue(objJson, "statPopupOffsetY");
                preset.scoringSummaryX = ParseFloatValue(objJson, "scoringSummaryX");
                preset.scoringSummaryY = ParseFloatValue(objJson, "scoringSummaryY");
                preset.scoringSummaryScale = ParseFloatValue(objJson, "scoringSummaryScale");
                preset.periodSummaryX = ParseFloatValue(objJson, "periodSummaryX");
                preset.periodSummaryY = ParseFloatValue(objJson, "periodSummaryY");
                preset.periodSummaryScale = ParseFloatValue(objJson, "periodSummaryScale");
                preset.goalOverlayOffsetX = ParseFloatValue(objJson, "goalOverlayOffsetX");
                preset.goalOverlayWidth = ParseFloatValue(objJson, "goalOverlayWidth");
                preset.goalOverlayHeight = ParseFloatValue(objJson, "goalOverlayHeight");
                preset.animationLogoWidth = ParseFloatValue(objJson, "animationLogoWidth");
                preset.animationLogoHeight = ParseFloatValue(objJson, "animationLogoHeight");
                // Section customization fields (with defaults if not present)
                preset.leagueLogoSectionWidth = ParseFloatValue(objJson, "leagueLogoSectionWidth");
                if (preset.leagueLogoSectionWidth == 0f) preset.leagueLogoSectionWidth = 20f;
                preset.leagueLogoSectionColorHex = ParseStringValue(objJson, "leagueLogoSectionColorHex");
                if (string.IsNullOrEmpty(preset.leagueLogoSectionColorHex)) preset.leagueLogoSectionColorHex = "#262626";
                preset.leagueLogoSectionBorderColorHex = ParseStringValue(objJson, "leagueLogoSectionBorderColorHex");
                if (string.IsNullOrEmpty(preset.leagueLogoSectionBorderColorHex)) preset.leagueLogoSectionBorderColorHex = "#1A1A1A";
                
                preset.periodBoxWidth = ParseFloatValue(objJson, "periodBoxWidth");
                if (preset.periodBoxWidth == 0f) preset.periodBoxWidth = 80f;
                preset.periodBoxColorHex = ParseStringValue(objJson, "periodBoxColorHex");
                if (string.IsNullOrEmpty(preset.periodBoxColorHex)) preset.periodBoxColorHex = "#1A1A1A";
                preset.periodBoxBorderColorHex = ParseStringValue(objJson, "periodBoxBorderColorHex");
                if (string.IsNullOrEmpty(preset.periodBoxBorderColorHex)) preset.periodBoxBorderColorHex = "#0D0D0D";
                preset.periodTextColorHex = ParseStringValue(objJson, "periodTextColorHex");
                if (string.IsNullOrEmpty(preset.periodTextColorHex)) preset.periodTextColorHex = "#FFFFFF";
                
                preset.timeBoxWidth = ParseFloatValue(objJson, "timeBoxWidth");
                if (preset.timeBoxWidth == 0f) preset.timeBoxWidth = 120f;
                preset.timeBoxColorHex = ParseStringValue(objJson, "timeBoxColorHex");
                if (string.IsNullOrEmpty(preset.timeBoxColorHex)) preset.timeBoxColorHex = "#4D4D4D";
                preset.timeBoxBorderColorHex = ParseStringValue(objJson, "timeBoxBorderColorHex");
                if (string.IsNullOrEmpty(preset.timeBoxBorderColorHex)) preset.timeBoxBorderColorHex = "#333333";
                preset.timeTextColorHex = ParseStringValue(objJson, "timeTextColorHex");
                if (string.IsNullOrEmpty(preset.timeTextColorHex)) preset.timeTextColorHex = "#FFFFFF";
                
                preset.statPopupSlideDistance = ParseFloatValue(objJson, "statPopupSlideDistance");
                preset.lineupPopupSlideDistance = ParseFloatValue(objJson, "lineupPopupSlideDistance");
                preset.scoringSummarySlideDistance = ParseFloatValue(objJson, "scoringSummarySlideDistance");
                preset.periodSummarySlideDistance = ParseFloatValue(objJson, "periodSummarySlideDistance");
                preset.statPopupWidth = ParseFloatValue(objJson, "statPopupWidth");
                preset.statPopupHeight = ParseFloatValue(objJson, "statPopupHeight");
                preset.lineupPopupWidth = ParseFloatValue(objJson, "lineupPopupWidth");
                preset.lineupPopupHeight = ParseFloatValue(objJson, "lineupPopupHeight");
                preset.scoringSummaryWidth = ParseFloatValue(objJson, "scoringSummaryWidth");
                preset.scoringSummaryHeight = ParseFloatValue(objJson, "scoringSummaryHeight");
                preset.periodSummaryWidth = ParseFloatValue(objJson, "periodSummaryWidth");
                preset.periodSummaryHeight = ParseFloatValue(objJson, "periodSummaryHeight");
                preset.gameSummaryOffsetX = ParseFloatValue(objJson, "gameSummaryOffsetX");
                preset.gameSummaryOffsetY = ParseFloatValue(objJson, "gameSummaryOffsetY");
                preset.gameSummarySlideDistance = ParseFloatValue(objJson, "gameSummarySlideDistance");
                preset.gameSummaryWidth = ParseFloatValue(objJson, "gameSummaryWidth");
                preset.gameSummaryHeight = ParseFloatValue(objJson, "gameSummaryHeight");
                
                presetList.Add(preset);
                
                searchStart = objEnd + 1;
            }
        }
        
        private int FindMatchingBrace(string json, int startPos)
        {
            int depth = 0;
            for (int i = startPos; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }
        
        private string ParseStringValue(string json, string key)
        {
            string searchKey = $"\"{key}\": \"";
            int start = json.IndexOf(searchKey);
            if (start < 0) return "";
            start += searchKey.Length;
            int end = json.IndexOf('"', start);
            if (end < 0) return "";
            return json.Substring(start, end - start);
        }
        
        private float ParseFloatValue(string json, string key)
        {
            string searchKey = $"\"{key}\": ";
            int start = json.IndexOf(searchKey);
            if (start < 0) return 0f;
            start += searchKey.Length;
            int end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.' || json[end] == '-'))
                end++;
            if (end <= start) return 0f;
            string valueStr = json.Substring(start, end - start);
            float result;
            if (float.TryParse(valueStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result))
                return result;
            return 0f;
        }

        private void SavePresetsConfig(PresetsConfig presets, string customPath = null)
        {
            try
            {
                string configPath;
                if (customPath != null)
                {
                    configPath = customPath;
                }
                else
                {
                    Directory.CreateDirectory(ScoreboardPaths.ConfigDir);
                    configPath = ScoreboardPaths.PresetsPath;
                }
                
                // Filter out workshop presets - only save Built-in presets unless this is a custom export path
                var teamPresetsToSave = presets.teamPresets;
                var sizePresetsToSave = presets.sizePresets;
                
                if (customPath == null)
                {
                    // When saving to main config, exclude workshop presets
                    teamPresetsToSave = presets.teamPresets.Where(p => string.IsNullOrEmpty(p.packName) || p.packName == "Built-in").ToList();
                    sizePresetsToSave = presets.sizePresets.Where(p => string.IsNullOrEmpty(p.packName) || p.packName == "Built-in").ToList();
                    Debug.Log($"[Scoreboard] Saving presets (filtered): {teamPresetsToSave.Count} team presets, {sizePresetsToSave.Count} size presets");
                }
                
                // Manually build JSON since Unity's JsonUtility doesn't handle nested arrays well
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                
                // Team presets (single list for all teams)
                sb.AppendLine("  \"teamPresets\": [");
                for (int i = 0; i < teamPresetsToSave.Count; i++)
                {
                    var preset = teamPresetsToSave[i];
                    sb.AppendLine("    {");
                    sb.AppendLine($"      \"presetName\": \"{EscapeJson(preset.presetName ?? "Preset")}\",");
                    sb.AppendLine($"      \"teamName\": \"{EscapeJson(preset.teamName ?? "TEAM")}\",");
                    sb.AppendLine($"      \"teamColorHex\": \"{preset.teamColorHex ?? "#2E59C1"}\",");
                    sb.AppendLine($"      \"teamTextColorHex\": \"{preset.teamTextColorHex ?? "#FFFFFF"}\",");
                    sb.AppendLine($"      \"borderColorHex\": \"{preset.borderColorHex ?? "#1E3A8A"}\",");
                    sb.AppendLine($"      \"gradientLeftColorHex\": \"{preset.gradientLeftColorHex ?? "#FFFFFF"}\",");
                    sb.AppendLine($"      \"gradientRightColorHex\": \"{preset.gradientRightColorHex ?? "#000000"}\",");
                    sb.AppendLine($"      \"logoFile\": \"{EscapeJson(preset.logoFile ?? "blueteamlogo.png")}\",");
                    sb.AppendLine($"      \"logoWidth\": {preset.logoWidth},");
                    sb.AppendLine($"      \"logoHeight\": {preset.logoHeight},");
                    sb.AppendLine($"      \"logoOffsetX\": {preset.logoOffsetX},");
                    sb.AppendLine($"      \"logoOffsetY\": {preset.logoOffsetY},");
                    sb.AppendLine($"      \"minimapPlayerColorHex\": \"{preset.minimapPlayerColorHex ?? ""}\",");
                    sb.AppendLine($"      \"minimapNumberColorHex\": \"{preset.minimapNumberColorHex ?? ""}\",");
                    sb.AppendLine($"      \"trlPresetName\": \"{EscapeJson(preset.trlPresetName ?? "")}\"");
                    sb.Append("    }");
                    if (i < teamPresetsToSave.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("  ]");
                
                // Size presets
                sb.AppendLine("  ,");
                sb.AppendLine("  \"sizePresets\": [");
                for (int i = 0; i < sizePresetsToSave.Count; i++)
                {
                    var preset = sizePresetsToSave[i];
                    sb.AppendLine("    {");
                    sb.AppendLine($"      \"presetName\": \"{EscapeJson(preset.presetName ?? "Size Preset")}\",");
                    sb.AppendLine($"      \"scoreboardX\": {preset.scoreboardX},");
                    sb.AppendLine($"      \"scoreboardY\": {preset.scoreboardY},");
                    sb.AppendLine($"      \"scoreboardScale\": {preset.scoreboardScale},");
                    sb.AppendLine($"      \"leagueLogoFile\": \"{EscapeJson(preset.leagueLogoFile ?? "nhl.png")}\",");
                    sb.AppendLine($"      \"leagueLogoOffsetX\": {preset.leagueLogoOffsetX},");
                    sb.AppendLine($"      \"leagueLogoOffsetY\": {preset.leagueLogoOffsetY},");
                    sb.AppendLine($"      \"leagueLogoWidth\": {preset.leagueLogoWidth},");
                    sb.AppendLine($"      \"leagueLogoHeight\": {preset.leagueLogoHeight},");
                    sb.AppendLine($"      \"lineupPopupOffsetY\": {preset.lineupPopupOffsetY},");
                    sb.AppendLine($"      \"lineupPopupOffsetX\": {preset.lineupPopupOffsetX},");
                    sb.AppendLine($"      \"blueStatPopupOffsetX\": {preset.blueStatPopupOffsetX},");
                    sb.AppendLine($"      \"redStatPopupOffsetX\": {preset.redStatPopupOffsetX},");
                    sb.AppendLine($"      \"statPopupOffsetY\": {preset.statPopupOffsetY},");
                    sb.AppendLine($"      \"scoringSummaryX\": {preset.scoringSummaryX},");
                    sb.AppendLine($"      \"scoringSummaryY\": {preset.scoringSummaryY},");
                    sb.AppendLine($"      \"scoringSummaryScale\": {preset.scoringSummaryScale},");
                    sb.AppendLine($"      \"periodSummaryX\": {preset.periodSummaryX},");
                    sb.AppendLine($"      \"periodSummaryY\": {preset.periodSummaryY},");
                    sb.AppendLine($"      \"periodSummaryScale\": {preset.periodSummaryScale},");
                    sb.AppendLine($"      \"goalOverlayOffsetX\": {preset.goalOverlayOffsetX},");
                    sb.AppendLine($"      \"goalOverlayWidth\": {preset.goalOverlayWidth},");
                    sb.AppendLine($"      \"goalOverlayHeight\": {preset.goalOverlayHeight},");
                    sb.AppendLine($"      \"animationLogoWidth\": {preset.animationLogoWidth},");
                    sb.AppendLine($"      \"animationLogoHeight\": {preset.animationLogoHeight},");
                    sb.AppendLine($"      \"statPopupSlideDistance\": {preset.statPopupSlideDistance},");
                    sb.AppendLine($"      \"lineupPopupSlideDistance\": {preset.lineupPopupSlideDistance},");
                    sb.AppendLine($"      \"scoringSummarySlideDistance\": {preset.scoringSummarySlideDistance},");
                    sb.AppendLine($"      \"periodSummarySlideDistance\": {preset.periodSummarySlideDistance},");
                    sb.AppendLine($"      \"statPopupWidth\": {preset.statPopupWidth},");
                    sb.AppendLine($"      \"statPopupHeight\": {preset.statPopupHeight},");
                    sb.AppendLine($"      \"lineupPopupWidth\": {preset.lineupPopupWidth},");
                    sb.AppendLine($"      \"lineupPopupHeight\": {preset.lineupPopupHeight},");
                    sb.AppendLine($"      \"scoringSummaryWidth\": {preset.scoringSummaryWidth},");
                    sb.AppendLine($"      \"scoringSummaryHeight\": {preset.scoringSummaryHeight},");
                    sb.AppendLine($"      \"periodSummaryWidth\": {preset.periodSummaryWidth},");
                    sb.AppendLine($"      \"periodSummaryHeight\": {preset.periodSummaryHeight},");
                    sb.AppendLine($"      \"gameSummaryOffsetX\": {preset.gameSummaryOffsetX},");
                    sb.AppendLine($"      \"gameSummaryOffsetY\": {preset.gameSummaryOffsetY},");
                    sb.AppendLine($"      \"gameSummarySlideDistance\": {preset.gameSummarySlideDistance},");
                    sb.AppendLine($"      \"gameSummaryWidth\": {preset.gameSummaryWidth},");
                    sb.AppendLine($"      \"gameSummaryHeight\": {preset.gameSummaryHeight}");
                    sb.Append("    }");
                    if (i < sizePresetsToSave.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("  ]");
                sb.AppendLine("}");
                
                string json = sb.ToString();
                File.WriteAllText(configPath, json);
                Debug.Log("[Scoreboard] Presets saved");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Scoreboard] Failed to save presets: " + e);
            }
        }
        
        private string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private void InitializeDefaultPresets(PresetsConfig presets)
        {
            // Only initialize if presets are empty
            if (presets.teamPresets.Count == 0)
            {
                presets.teamPresets.Add(new TeamPreset
                {
                    presetName = "PPHL Away",
                    teamName = " Away",
                    teamColorHex = "#FFFFFF",
                    teamTextColorHex = "#00205b",
                    borderColorHex = "#00205b",
                    gradientLeftColorHex = "#FFFFFF",
                    gradientRightColorHex = "#000000",
                    logoFile = "leaguelogo.png",
                    logoWidth = 100f,
                    logoHeight = 100f,
                    logoOffsetX = 93f,
                    logoOffsetY = -37f
                });
                
                presets.teamPresets.Add(new TeamPreset
                {
                    presetName = "PPHL Home",
                    teamName = " Home",
                    teamColorHex = "#00205b",
                    teamTextColorHex = "#FFFFFF",
                    borderColorHex = "#FFFFFF",
                    gradientLeftColorHex = "#00205b",
                    gradientRightColorHex = "#000000",
                    logoFile = "leaguelogo.png",
                    logoWidth = 100f,
                    logoHeight = 100f,
                    logoOffsetX = 93f,
                    logoOffsetY = -37f
                });
            }
            
            // Initialize default size presets if empty
            if (presets.sizePresets.Count == 0)
            {
                presets.sizePresets.Add(new SizePreset
                {
                    presetName = "Normal PPHL",
                    scoreboardX = -350,
                    scoreboardY = 40,
                    scoreboardScale = 1.68f,
                    leagueLogoFile = "PPHL.png",
                    leagueLogoOffsetX = -101,
                    leagueLogoOffsetY = -4,
                    leagueLogoWidth = 50,
                    leagueLogoHeight = 55,
                    lineupPopupOffsetY = 80,
                    lineupPopupOffsetX = 276,
                    blueStatPopupOffsetX = 97,
                    redStatPopupOffsetX = 597,
                    statPopupOffsetY = 11,
                    scoringSummaryX = 276,
                    scoringSummaryY = 70,
                    scoringSummaryScale = 1,
                    periodSummaryX = 276,
                    periodSummaryY = 90,
                    periodSummaryScale = 1,
                    goalOverlayOffsetX = 355,
                    goalOverlayWidth = 741,
                    goalOverlayHeight = 42,
                    animationLogoWidth = 500,
                    animationLogoHeight = 500,
                    statPopupSlideDistance = 18,
                    lineupPopupSlideDistance = 66,
                    scoringSummarySlideDistance = 60,
                    periodSummarySlideDistance = 66,
                    statPopupWidth = 80,
                    statPopupHeight = 20,
                    lineupPopupWidth = 735,
                    lineupPopupHeight = 40,
                    scoringSummaryWidth = 740,
                    scoringSummaryHeight = 20,
                    periodSummaryWidth = 740,
                    periodSummaryHeight = 40,
                    gameSummaryOffsetX = 940,
                    gameSummaryOffsetY = 100,
                    gameSummarySlideDistance = 160,
                    gameSummaryWidth = 800,
                    gameSummaryHeight = 300
                });
            }
        }
    }
}
