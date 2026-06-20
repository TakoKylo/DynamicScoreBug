using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CustomScoreboard.UI
{
    /// <summary>
    /// Partial class containing Stats mod integration methods.
    /// Primary method: Direct reflection into Stats mod memory (UpdateStatsFromStatsMod)
    /// Fallback method: Log file parsing (only if reflection fails, runs every 500ms)
    /// </summary>
    public partial class CustomBroadcastScoreboard
    {
        private void StartLogMonitoring()
        {
            // Only start log monitoring as a fallback if reflection approach doesn't work
            try
            {
                // First, try the reflection approach to see if a supported Stats mod is available
                var statsType = ResolveStatsModType();
                if (statsType != null)
                {
                    DebugLog("Stats mod detected - using direct memory access (no log monitoring needed)");
                    return;
                }
                
                // Stats mod not found via reflection, fall back to log monitoring
                string logPath = Path.Combine(Application.dataPath, "..", "Logs", "Puck.log");
                if (File.Exists(logPath))
                {
                    lastLogPosition = new FileInfo(logPath).Length;
                    isMonitoringLog = true;
                    logMonitorThread = new System.Threading.Thread(() => MonitorLogFile(logPath));
                    logMonitorThread.IsBackground = true;
                    logMonitorThread.Start();
                    DebugLog("Started log file monitoring (fallback mode - Stats mod not found via reflection)");
                }
            }
            catch (System.Exception ex)
            {
                DebugLog($"Error in StartLogMonitoring: {ex.Message}");
            }
        }
        
        private void MonitorLogFile(string logPath)
        {
            int linesRead = 0;
            string pendingStatsLine = null;
            
            while (isMonitoringLog)
            {
                try
                {
                    using (FileStream fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fs.Seek(lastLogPosition, SeekOrigin.Begin);
                        using (StreamReader reader = new StreamReader(fs))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                linesRead++;
                                
                                // Check if we have a pending stats line that needs continuation
                                if (pendingStatsLine != null)
                                {
                                    // If this line starts with a timestamp (new log entry), process the pending line as-is
                                    if (Regex.IsMatch(line, @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}"))
                                    {
                                        // Process the pending line
                                        ParseStatsModLogLine(pendingStatsLine);
                                        pendingStatsLine = null;
                                        
                                        // Check if this new line is also a stats line
                                        if (line.Contains("[oomtm450_stats] Received data") && line.Contains("Content :"))
                                        {
                                            // Check if content might continue (ends with semicolon or partial data)
                                            if (line.EndsWith(";") || Regex.IsMatch(line, @";\d+$") == false && line.Contains("BATCH"))
                                            {
                                                pendingStatsLine = line;
                                            }
                                            else
                                            {
                                                ParseStatsModLogLine(line);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // This line is a continuation of the previous stats line
                                        pendingStatsLine += line.Trim();
                                    }
                                }
                                else
                                {
                                    // Check if current line is a stats line
                                    if (line.Contains("[oomtm450_stats] Received data") && line.Contains("Content :"))
                                    {
                                        // BATCH messages often wrap, so save and wait for potential continuation
                                        if (line.Contains("BATCH"))
                                        {
                                            pendingStatsLine = line;
                                        }
                                        else
                                        {
                                            ParseStatsModLogLine(line);
                                        }
                                    }
                                }
                            }
                            
                            // Process any remaining pending line at end of read
                            if (pendingStatsLine != null)
                            {
                                // Keep it pending for next iteration in case more data comes
                            }
                            
                            lastLogPosition = fs.Position;
                        }
                    }
                }
                catch (Exception)
                {
                    // Silently ignore log reading errors
                }
                System.Threading.Thread.Sleep(500); // Check every 500ms for faster updates
            }
        }
        
        private void ParseStatsModLogLine(string logLine)
        {
            // Parse Stats mod log messages - supports both individual and batch formats
            // This is a fallback method - prefer UpdateStatsFromStatsMod() reflection approach
            // Individual: "oomtm450_statsTAKEAWAY76561197996108135 ... Content : 12"
            // Batch SOG: "oomtm450_statsBATCHSOG ... Content : 76561199081913522;1;76561198313645036;0;..."
            // Batch SavePerc: "oomtm450_statsBATCHSAVEPERC ... Content : 76561199081913522;(0, 0);76561198415010007;(2, 3);..."
            if (logLine.Contains("[oomtm450_stats] Received data"))
            {
                try
                {
                    // Try parsing BATCH SOG messages first (most common format in newer Stats mod)
                    // Format: oomtm450_statsBATCHSOG ... Content : steamid;shots;steamid;shots;...
                    if (logLine.Contains("oomtm450_statsBATCHSOG"))
                    {
                        var contentMatch = Regex.Match(logLine, @"Content\s*:\s*(.+)$");
                        if (contentMatch.Success)
                        {
                            string content = contentMatch.Groups[1].Value.Trim();
                            var parts = content.Split(';');
                            
                            // Parse pairs of steamId;shots
                            lock (playerHits)
                            {
                                for (int i = 0; i < parts.Length - 1; i += 2)
                                {
                                    string steamId = parts[i].Trim();
                                    if (int.TryParse(parts[i + 1].Trim(), out int shots))
                                    {
                                        // Store SOG per player by Steam ID
                                        playerSOG[steamId] = shots;
                                    }
                                }
                            }
                        }
                        return;
                    }
                    
                    // Try parsing BATCH SAVEPERC messages
                    // Format: oomtm450_statsBATCHSAVEPERC ... Content : steamid;(saves, shotsAgainst);steamid;(saves, shotsAgainst);...
                    if (logLine.Contains("oomtm450_statsBATCHSAVEPERC"))
                    {
                        var contentMatch = Regex.Match(logLine, @"Content\s*:\s*(.+)$");
                        if (contentMatch.Success)
                        {
                            string content = contentMatch.Groups[1].Value.Trim();
                            // Split on semicolons but preserve the (x, y) tuples
                            var savePercMatches = Regex.Matches(content, @"(\d{17});?\s*\((\d+),\s*(\d+)\)");
                            
                            lock (playerHits)
                            {
                                foreach (Match m in savePercMatches)
                                {
                                    string steamId = m.Groups[1].Value;
                                    if (int.TryParse(m.Groups[2].Value, out int saves) && 
                                        int.TryParse(m.Groups[3].Value, out int shotsAgainst))
                                    {
                                        goalieSaveStats[steamId] = (saves, shotsAgainst);
                                    }
                                }
                            }
                        }
                        return;
                    }
                    
                    // Try parsing BATCH stats for HIT, PASS, TAKEAWAY, TURNOVER, BLOCK
                    // Format: oomtm450_statsBATCH{STAT} ... Content : steamid;value;steamid;value;...
                    var batchMatch = Regex.Match(logLine, @"oomtm450_statsBATCH(HIT|PASS|TAKEAWAY|TURNOVER|BLOCK)");
                    if (batchMatch.Success)
                    {
                        string statType = batchMatch.Groups[1].Value;
                        var contentMatch = Regex.Match(logLine, @"Content\s*:\s*(.+)$");
                        if (contentMatch.Success)
                        {
                            string content = contentMatch.Groups[1].Value.Trim();
                            var parts = content.Split(';');
                            
                            lock (playerHits)
                            {
                                for (int i = 0; i < parts.Length - 1; i += 2)
                                {
                                    string steamId = parts[i].Trim();
                                    if (int.TryParse(parts[i + 1].Trim(), out int value))
                                    {
                                        switch (statType)
                                        {
                                            case "HIT":
                                                playerHits[steamId] = value;
                                                break;
                                            case "PASS":
                                                playerPasses[steamId] = value;
                                                break;
                                            case "TAKEAWAY":
                                                playerTakeaways[steamId] = value;
                                                break;
                                            case "TURNOVER":
                                                playerTurnovers[steamId] = value;
                                                break;
                                            case "BLOCK":
                                                playerBlocks[steamId] = value;
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                        return;
                    }
                    
                    // Try parsing individual stats (HIT, PASS, TAKEAWAY, TURNOVER, BLOCK) - legacy format
                    var match = Regex.Match(logLine, @"oomtm450_stats(HIT|PASS|TAKEAWAY|TURNOVER|BLOCK)(\d+).*Content\s*:\s*(\d+)");
                    if (match.Success)
                    {
                        string statType = match.Groups[1].Value;
                        string steamId = match.Groups[2].Value;
                        string valueStr = match.Groups[3].Value;
                        
                        if (int.TryParse(valueStr, out int value))
                        {
                            // Update on main thread-safe dictionary
                            lock (playerHits)
                            {
                                switch (statType)
                                {
                                    case "HIT":
                                        playerHits[steamId] = value;
                                        break;
                                    case "PASS":
                                        playerPasses[steamId] = value;
                                        break;
                                    case "TAKEAWAY":
                                        playerTakeaways[steamId] = value;
                                        break;
                                    case "TURNOVER":
                                        playerTurnovers[steamId] = value;
                                        break;
                                    case "BLOCK":
                                        playerBlocks[steamId] = value;
                                        break;
                                }
                            }
                        }
                        return;
                    }
                    
                    // Try parsing individual SOG messages - legacy format
                    var sogMatch = Regex.Match(logLine, @"oomtm450_statsSOG(\d+).*Content\s*:\s*(\d+)");
                    if (sogMatch.Success)
                    {
                        string steamId = sogMatch.Groups[1].Value;
                        if (int.TryParse(sogMatch.Groups[2].Value, out int shots))
                        {
                            lock (playerHits)
                            {
                                playerSOG[steamId] = shots;
                            }
                        }
                        return;
                    }

                    // Try parsing individual SAVEPERC messages - legacy format
                    // Format: oomtm450_statsSAVEPERC76561198206675268 ... Content : (0, 1)
                    var savePercMatch = Regex.Match(logLine, @"oomtm450_statsSAVEPERC(\d+).*Content\s*:\s*\((\d+),\s*(\d+)\)");
                    if (savePercMatch.Success)
                    {
                        string steamId = savePercMatch.Groups[1].Value;
                        string savesStr = savePercMatch.Groups[2].Value;
                        string shotsStr = savePercMatch.Groups[3].Value;
                        
                        if (int.TryParse(savesStr, out int saves) && int.TryParse(shotsStr, out int shots))
                        {
                            lock (playerHits)
                            {
                                goalieSaveStats[steamId] = (saves, shots);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Silently ignore parse errors
                }
            }
        }
        
        // Resolves the active Stats mod's main type across the known forks. They all ship the
        // same wire format and a private static `_sog` LockDictionary, but under different
        // assembly/namespace names: oomtm450's "oomtm450PuckMod_Stats" and Dalfan's "StatsTooltip".
        // Result is cached; a one-time assembly scan covers any future rename.
        private static System.Type _resolvedStatsType;
        private static bool _scannedAssembliesForStats;

        private System.Type ResolveStatsModType()
        {
            if (_resolvedStatsType != null) return _resolvedStatsType;

            // Cheap path: known assembly-qualified names.
            _resolvedStatsType = System.Type.GetType("oomtm450PuckMod_Stats.Stats, oomtm450PuckMod_Stats")
                              ?? System.Type.GetType("StatsTooltip.Stats, StatsTooltip");
            if (_resolvedStatsType != null) return _resolvedStatsType;

            // Expensive fallback, done at most once: any "Stats" type exposing a static _sog field.
            if (_scannedAssembliesForStats) return null;
            _scannedAssembliesForStats = true;
            try
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    System.Type[] types;
                    try { types = asm.GetTypes(); }
                    catch { continue; }
                    foreach (var t in types)
                    {
                        if (t.Name == "Stats" &&
                            t.GetField("_sog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) != null)
                        {
                            _resolvedStatsType = t;
                            return t;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        // Reads per-player shots from ToastersRinkCompanion, which keeps its OWN stats store
        // (independent of the oomtm/Dalfan Stats mod) and is the only SOG source on Toasters Rink
        // servers. Feeds the same playerSOG dictionary so the team-total summation is source-agnostic.
        private static System.Reflection.MethodInfo _trcGetStats;
        private static System.Reflection.FieldInfo _trcShotsField;

        private void UpdateStatsFromToastersRink()
        {
            try
            {
                // Resolve once and cache. Only latch on success so a late-loading TRC assembly
                // (mod load-order) is still picked up on a later tick instead of being locked out.
                if (_trcGetStats == null)
                {
                    var storeType = System.Type.GetType("ToastersRinkCompanion.modifiers.PlayerStatsStore, ToastersRinkCompanion");
                    if (storeType != null)
                        _trcGetStats = storeType.GetMethod("GetStats", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (_trcGetStats == null) return; // TRC not loaded (yet) — retry on a later tick
                }

                var playerManager = GetPlayerManager();
                if (playerManager == null) return;

                foreach (var player in playerManager.GetPlayers(false))
                {
                    string steamId = player.SteamId.Value.ToString();
                    var entry = _trcGetStats.Invoke(null, new object[] { steamId });
                    if (entry == null) continue; // no stats for this player yet

                    if (_trcShotsField == null)
                        _trcShotsField = entry.GetType().GetField("shots");
                    if (_trcShotsField == null) return; // shape changed — give up quietly

                    int shots = Convert.ToInt32(_trcShotsField.GetValue(entry));
                    // Keep the higher value, matching ReadStatsField. Locked on playerHits — the
                    // log-monitor thread can write playerSOG concurrently on the fallback path.
                    lock (playerHits)
                    {
                        if (!playerSOG.ContainsKey(steamId) || playerSOG[steamId] < shots)
                            playerSOG[steamId] = shots;
                    }
                }
            }
            catch (System.Exception ex)
            {
                DebugLog($"Error reading ToastersRinkCompanion stats: {ex.Message}");
            }
        }

        // Use reflection to access Stats mod's private LockDictionary fields
        // Stats mod v0.7.1+ fields: _sog, _shotAttempts, _savePerc, _stickSaves, _bodySaves, _blocks, _hits, 
        // _takeaways, _turnovers, _passes, _puckTouches, _exits, _entries, _possessionTimeSeconds, 
        // _timeOnIceSeconds, _plusMinus, _puckBattleWins, _puckBattleLosses
        private void UpdateStatsFromStatsMod()
        {
            try
            {
                // Find the Stats class from a supported stats mod (oomtm450 Stats or the
                // "StatsTooltip" fork — both expose the same private static _sog dictionary).
                var statsType = ResolveStatsModType();
                if (statsType == null)
                {
                    DebugLog("Stats mod type not found via reflection");
                    return;
                }
                
                // Field names match the Stats mod source exactly. Only fields that we actually
                // surface in the UI are read here — the others were dropped along with their dictionaries.
                ReadStatsField(GetPrivateStaticField(statsType, "_sog"), playerSOG, "SOG");
                ReadStatsField(GetPrivateStaticField(statsType, "_stickSaves"), playerStickSaves, "StickSaves");
                ReadStatsField(GetPrivateStaticField(statsType, "_bodySaves"), playerBodySaves, "BodySaves");
                ReadStatsField(GetPrivateStaticField(statsType, "_blocks"), playerBlocks, "Blocks");
                ReadStatsField(GetPrivateStaticField(statsType, "_hits"), playerHits, "Hits");
                ReadStatsField(GetPrivateStaticField(statsType, "_takeaways"), playerTakeaways, "Takeaways");
                ReadStatsField(GetPrivateStaticField(statsType, "_turnovers"), playerTurnovers, "Turnovers");
                ReadStatsField(GetPrivateStaticField(statsType, "_passes"), playerPasses, "Passes");
                ReadStatsField(GetPrivateStaticField(statsType, "_plusMinus"), playerPlusMinus, "PlusMinus");
                ReadStatsField(GetPrivateStaticField(statsType, "_puckBattleWins"), playerPuckBattleWins, "PuckBattleWins");
                ReadStatsField(GetPrivateStaticField(statsType, "_puckBattleLosses"), playerPuckBattleLosses, "PuckBattleLosses");
                ReadDoubleStatsField(GetPrivateStaticField(statsType, "_timeOnIceSeconds"), playerTimeOnIce, "TimeOnIce");
                ReadSavePercField(GetPrivateStaticField(statsType, "_savePerc"));

                DebugLog($"Updated stats from Stats mod - SOG: {playerSOG.Count}, Saves: {goalieSaveStats.Count}, Hits: {playerHits.Count}, Passes: {playerPasses.Count}, PlusMinus: {playerPlusMinus.Count}, TOI: {playerTimeOnIce.Count}");
            }
            catch (System.Exception ex)
            {
                DebugLog($"Error accessing Stats mod data via reflection: {ex.Message}");
            }
        }
        
        private System.Collections.IDictionary UnwrapLockDictionary(System.Reflection.FieldInfo field)
        {
            if (field == null) return null;
            var obj = field.GetValue(null);
            if (obj == null) return null;
            var internalDictField = GetPrivateInstanceField(obj.GetType(), "_dictionary");
            return (internalDictField?.GetValue(obj) as System.Collections.IDictionary)
                ?? (obj as System.Collections.IDictionary);
        }

        private void ReadStatsField(System.Reflection.FieldInfo field, Dictionary<string, int> targetDict, string fieldName)
        {
            try
            {
                var dict = UnwrapLockDictionary(field);
                if (dict == null) return;
                // playerHits is the shared monitor for all stat dictionaries (the log-monitor thread
                // writes them under the same lock); guard the read-modify-write against that thread.
                lock (playerHits)
                {
                    foreach (System.Collections.DictionaryEntry entry in dict)
                    {
                        string key = entry.Key.ToString();
                        int value = Convert.ToInt32(entry.Value);
                        // Keep the higher value (Stats mod has authoritative data)
                        if (!targetDict.ContainsKey(key) || targetDict[key] < value)
                            targetDict[key] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error reading {fieldName} field: {ex.Message}");
            }
        }

        private void ReadDoubleStatsField(System.Reflection.FieldInfo field, Dictionary<string, double> targetDict, string fieldName)
        {
            try
            {
                var dict = UnwrapLockDictionary(field);
                if (dict == null) return;
                foreach (System.Collections.DictionaryEntry entry in dict)
                {
                    string key = entry.Key.ToString();
                    double value = Convert.ToDouble(entry.Value);
                    if (!targetDict.ContainsKey(key) || targetDict[key] < value)
                        targetDict[key] = value;
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error reading {fieldName} field: {ex.Message}");
            }
        }

        private void ReadSavePercField(System.Reflection.FieldInfo field)
        {
            try
            {
                var dict = UnwrapLockDictionary(field);
                if (dict == null) return;
                // goalieSaveStats is also written by the log-monitor thread under lock(playerHits).
                lock (playerHits)
                {
                    foreach (System.Collections.DictionaryEntry entry in dict)
                    {
                        // Value is ValueTuple<int, int> with named fields (Saves, Shots) — try named, fall back to Item1/Item2.
                        var tupleType = entry.Value.GetType();
                        var savesField = tupleType.GetField("Saves") ?? tupleType.GetField("Item1");
                        var shotsField = tupleType.GetField("Shots") ?? tupleType.GetField("Item2");
                        if (savesField == null || shotsField == null) continue;
                        int saves = Convert.ToInt32(savesField.GetValue(entry.Value));
                        int shots = Convert.ToInt32(shotsField.GetValue(entry.Value));
                        goalieSaveStats[entry.Key.ToString()] = (saves, shots);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error reading SavePerc field: {ex.Message}");
            }
        }
    }
}
