using System;
using UnityEngine;
using System.IO;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        // Workshop Preset Scanning
        private void ScanWorkshopForPresets()
        {
            try
            {
                // Get workshop content folder - use the mod's assembly location
                string modDllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string thisModFolder = Path.GetDirectoryName(modDllPath);
                string workshopContentFolder = Path.GetDirectoryName(thisModFolder); // Parent of our mod folder
                
                Debug.Log($"[Scoreboard] Mod DLL at: {modDllPath}");
                Debug.Log($"[Scoreboard] Looking for workshop mods at: {workshopContentFolder}");
                
                if (!Directory.Exists(workshopContentFolder))
                {
                    Debug.Log("[Scoreboard] Workshop content folder not found, skipping preset scan");
                    return;
                }
                
                int presetsFound = 0;
                int logosFound = 0;
                
                // Scan all workshop mod folders
                var workshopModFolders = Directory.GetDirectories(workshopContentFolder);
                Debug.Log($"[Scoreboard] Found {workshopModFolders.Length} workshop folders");
                
                foreach (var modFolder in workshopModFolders)
                {
                    // Skip our own mod folder
                    string modFolderName = Path.GetFileName(modFolder);
                    if (modFolderName == "3608332960")
                    {
                        Debug.Log($"[Scoreboard] Skipping own mod folder: {modFolder}");
                        continue;
                    }
                    
                    Debug.Log($"[Scoreboard] Checking mod folder: {modFolder}");
                    
                    // Look for preset pack folders inside each mod folder
                    if (Directory.Exists(modFolder))
                    {
                        var packFolders = Directory.GetDirectories(modFolder);
                        foreach (var packFolder in packFolders)
                        {
                            string packName = Path.GetFileName(packFolder);
                            string presetFile = Path.Combine(packFolder, "ScoreboardPresets.json");
                            
                            Debug.Log($"[Scoreboard] Checking pack: {packName} at {presetFile}");
                            
                            if (File.Exists(presetFile))
                            {
                                try
                                {
                                    string json = File.ReadAllText(presetFile);
                                    var workshopPresets = new PresetsConfig();
                                    
                                    // Parse team presets
                                    int presetsStart = json.IndexOf("\"teamPresets\": [");
                                    if (presetsStart >= 0)
                                    {
                                        presetsStart = json.IndexOf('[', presetsStart);
                                        int presetsEnd = FindMatchingBracket(json, presetsStart);
                                        if (presetsEnd > presetsStart)
                                        {
                                            ParsePresets(json.Substring(presetsStart, presetsEnd - presetsStart + 1), workshopPresets.teamPresets);
                                        }
                                    }
                                    
                                    // Parse size presets
                                    int sizePresetsStart = json.IndexOf("\"sizePresets\": [");
                                    if (sizePresetsStart >= 0)
                                    {
                                        sizePresetsStart = json.IndexOf('[', sizePresetsStart);
                                        int sizePresetsEnd = FindMatchingBracket(json, sizePresetsStart);
                                        if (sizePresetsEnd > sizePresetsStart)
                                        {
                                            ParseSizePresets(json.Substring(sizePresetsStart, sizePresetsEnd - sizePresetsStart + 1), workshopPresets.sizePresets);
                                        }
                                    }
                                    
                                    // Add workshop presets to our config with pack name
                                    foreach (var preset in workshopPresets.teamPresets)
                                    {
                                        preset.packName = packName;
                                        _presetsConfig.teamPresets.Add(preset);
                                        presetsFound++;
                                    }
                                    
                                    foreach (var preset in workshopPresets.sizePresets)
                                    {
                                        preset.packName = packName;
                                        _presetsConfig.sizePresets.Add(preset);
                                        presetsFound++;
                                    }
                                    
                                    Debug.Log($"[Scoreboard] Loaded presets from workshop pack: {packName}");
                                }
                                catch (Exception e)
                                {
                                    Debug.LogWarning($"[Scoreboard] Failed to load workshop preset from {packFolder}: {e.Message}");
                                }
                            }
                            
                            // Copy logo files from workshop pack to our scorebuglogos folder
                            string packLogosFolder = Path.Combine(packFolder, "scorebuglogos");
                            if (Directory.Exists(packLogosFolder))
                            {
                                try
                                {
                                    string targetLogosFolder = ScoreboardPaths.LogosDir;
                                    Directory.CreateDirectory(targetLogosFolder);
                                    
                                    var logoFiles = Directory.GetFiles(packLogosFolder, "*.png");
                                    foreach (var logoFile in logoFiles)
                                    {
                                        string fileName = Path.GetFileName(logoFile);
                                        string targetPath = Path.Combine(targetLogosFolder, fileName);
                                        
                                        // Only copy if doesn't exist or is newer
                                        if (!File.Exists(targetPath) || File.GetLastWriteTime(logoFile) > File.GetLastWriteTime(targetPath))
                                        {
                                            File.Copy(logoFile, targetPath, true);
                                            logosFound++;
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Debug.LogWarning($"[Scoreboard] Failed to copy logos from {packFolder}: {e.Message}");
                                }
                            }
                        }
                    }
                }
                
                if (presetsFound > 0 || logosFound > 0)
                {
                    Debug.Log($"[Scoreboard] Workshop scan complete: {presetsFound} presets, {logosFound} logos");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Scoreboard] Workshop scanning failed: {e}");
            }
        }
    }
}
