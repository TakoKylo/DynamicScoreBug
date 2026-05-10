using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using UITK = UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        // Export selected presets as workshop preset pack
        public void ExportPresetPack(List<int> selectedTeamPresets, List<int> selectedSizePresets, string packName)
        {
            try
            {
                // Create PresetPacks folder and ensure README.txt exists
                string presetPacksFolder = ScoreboardPaths.PresetPacksDir;
                Directory.CreateDirectory(presetPacksFolder);
                EnsureReadmeExists(presetPacksFolder);
                
                // Create pack folder
                string packFolderName = packName.Replace(" ", "_");
                string packFolder = Path.Combine(presetPacksFolder, packFolderName);
                Directory.CreateDirectory(packFolder);
                
                // Create scorebuglogos subfolder for images
                string packLogosFolder = Path.Combine(packFolder, "scorebuglogos");
                Directory.CreateDirectory(packLogosFolder);
                
                var exportPresets = new PresetsConfig();
                var logoFiles = new HashSet<string>();
                
                // Add selected size presets
                foreach (int index in selectedSizePresets)
                {
                    if (index >= 0 && index < _presetsConfig.sizePresets.Count)
                    {
                        var sizePreset = _presetsConfig.sizePresets[index];
                        exportPresets.sizePresets.Add(new SizePreset
                        {
                            presetName = sizePreset.presetName,
                            scoreboardX = sizePreset.scoreboardX,
                            scoreboardY = sizePreset.scoreboardY,
                            scoreboardScale = sizePreset.scoreboardScale,
                            leagueLogoFile = sizePreset.leagueLogoFile,
                            leagueLogoOffsetX = sizePreset.leagueLogoOffsetX,
                            leagueLogoOffsetY = sizePreset.leagueLogoOffsetY,
                            leagueLogoWidth = sizePreset.leagueLogoWidth,
                            leagueLogoHeight = sizePreset.leagueLogoHeight,
                            lineupPopupOffsetY = sizePreset.lineupPopupOffsetY,
                            lineupPopupOffsetX = sizePreset.lineupPopupOffsetX,
                            blueStatPopupOffsetX = sizePreset.blueStatPopupOffsetX,
                            redStatPopupOffsetX = sizePreset.redStatPopupOffsetX,
                            statPopupOffsetY = sizePreset.statPopupOffsetY,
                            scoringSummaryX = sizePreset.scoringSummaryX,
                            scoringSummaryY = sizePreset.scoringSummaryY,
                            scoringSummaryScale = sizePreset.scoringSummaryScale,
                            periodSummaryX = sizePreset.periodSummaryX,
                            periodSummaryY = sizePreset.periodSummaryY,
                            periodSummaryScale = sizePreset.periodSummaryScale,
                            goalOverlayOffsetX = sizePreset.goalOverlayOffsetX,
                            goalOverlayWidth = sizePreset.goalOverlayWidth,
                            goalOverlayHeight = sizePreset.goalOverlayHeight,
                            animationLogoWidth = sizePreset.animationLogoWidth,
                            animationLogoHeight = sizePreset.animationLogoHeight,
                            statPopupSlideDistance = sizePreset.statPopupSlideDistance,
                            lineupPopupSlideDistance = sizePreset.lineupPopupSlideDistance,
                            scoringSummarySlideDistance = sizePreset.scoringSummarySlideDistance,
                            periodSummarySlideDistance = sizePreset.periodSummarySlideDistance,
                            statPopupWidth = sizePreset.statPopupWidth,
                            statPopupHeight = sizePreset.statPopupHeight,
                            lineupPopupWidth = sizePreset.lineupPopupWidth,
                            lineupPopupHeight = sizePreset.lineupPopupHeight,
                            scoringSummaryWidth = sizePreset.scoringSummaryWidth,
                            scoringSummaryHeight = sizePreset.scoringSummaryHeight,
                            periodSummaryWidth = sizePreset.periodSummaryWidth,
                            periodSummaryHeight = sizePreset.periodSummaryHeight,
                            gameSummaryOffsetX = sizePreset.gameSummaryOffsetX,
                            gameSummaryOffsetY = sizePreset.gameSummaryOffsetY,
                            gameSummarySlideDistance = sizePreset.gameSummarySlideDistance,
                            gameSummaryWidth = sizePreset.gameSummaryWidth,
                            gameSummaryHeight = sizePreset.gameSummaryHeight
                        });
                        
                        // Track league logo file for copying
                        if (!string.IsNullOrEmpty(sizePreset.leagueLogoFile))
                        {
                            logoFiles.Add(sizePreset.leagueLogoFile);
                        }
                    }
                }
                
                // Add selected team presets
                foreach (int index in selectedTeamPresets)
                {
                    if (index >= 0 && index < _presetsConfig.teamPresets.Count)
                    {
                        var preset = _presetsConfig.teamPresets[index];
                        exportPresets.teamPresets.Add(new TeamPreset
                        {
                            presetName = preset.presetName,
                            teamName = preset.teamName,
                            teamColorHex = preset.teamColorHex,
                            teamTextColorHex = preset.teamTextColorHex,
                            borderColorHex = preset.borderColorHex,
                            gradientLeftColorHex = preset.gradientLeftColorHex,
                            gradientRightColorHex = preset.gradientRightColorHex,
                            logoFile = preset.logoFile,
                            logoWidth = preset.logoWidth,
                            logoHeight = preset.logoHeight,
                            logoOffsetX = preset.logoOffsetX,
                            logoOffsetY = preset.logoOffsetY
                        });
                        
                        // Track logo file for copying
                        if (!string.IsNullOrEmpty(preset.logoFile))
                        {
                            logoFiles.Add(preset.logoFile);
                        }
                    }
                }
                
                // Copy all logo files to pack folder
                string sourceLogosFolder = ScoreboardPaths.LogosDir;
                int copiedLogos = 0;
                foreach (string logoFile in logoFiles)
                {
                    string sourcePath = Path.Combine(sourceLogosFolder, logoFile);
                    string targetPath = Path.Combine(packLogosFolder, logoFile);
                    
                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, targetPath, true);
                        copiedLogos++;
                    }
                    else
                    {
                        Debug.LogWarning($"[Scoreboard] Logo file not found: {logoFile}");
                    }
                }
                
                // Save presets to pack folder
                string exportPath = Path.Combine(packFolder, "ScoreboardPresets.json");
                SavePresetsConfig(exportPresets, exportPath);
                
                Debug.Log($"[Scoreboard] Exported preset pack '{packName}' with {exportPresets.teamPresets.Count} team presets, {exportPresets.sizePresets.Count} size presets, and {copiedLogos} logos to: {packFolder}");
                
                // Open the PresetPacks parent folder in Windows Explorer
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", presetPacksFolder);
                    Debug.Log($"[Scoreboard] Opened PresetPacks folder in Explorer: {presetPacksFolder}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Scoreboard] Could not open folder in Explorer: {ex.Message}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Scoreboard] Failed to export preset pack: {e}");
            }
        }
        
        private void EnsureReadmeExists(string presetPacksFolder)
        {
            string readmePath = Path.Combine(presetPacksFolder, "README.txt");
            if (!File.Exists(readmePath))
            {
                try
                {
                    string readmeContent = @"=============================================================================
CUSTOM BROADCAST SCOREBOARD - WORKSHOP PRESET PACKS
=============================================================================

This folder contains preset packs that you've exported from the scoreboard.
Each subfolder is a separate preset pack that can be uploaded to Steam Workshop.

HOW TO UPLOAD TO WORKSHOP:
=============================================================================

1. PREPARE YOUR PACK
   - Each pack folder must contain:
     * ScoreboardPresets.json (automatically created)
     * scorebuglogos/ folder with all logo images (PNG format)
   
2. FOLDER STRUCTURE (IMPORTANT!)
   - Your pack folder should look like this:
     [PackName]/
       |- ScoreboardPresets.json
       |- scorebuglogos/
          |- logo1.png
          |- logo2.png
          |- etc...

3. UPLOAD TO STEAM WORKSHOP
   - Go to: https://github.com/nihilocrat/SteamWorkshopUploader
   - Download the Steam Workshop Uploader - Upload the ENTIRE PACK FOLDER (not just the JSON file!)
   - Add a clear title, description, and a png image for the workshop mod
   - Set visibility and publish

4. IMPORTANT NOTES
   - Logo files must be PNG format
   - Pack names should use underscores (e.g., ""NHL_Teams"" not ""NHL Teams"")
   - Only upload content you have rights to use
   - Be cautious with official team logos - check with the image owners
   - Subscribers will automatically get updates when they subscribe

5. TESTING YOUR PACK LOCALLY
   - Before uploading, you can test by copying your pack to:
     steamapps/workshop/content/2994020/[any_mod]/[YourPackName]/
   - Launch the game and check if presets appear in the dropdowns
   - They should be grouped under your pack name

For more details look up how to upload mods to the steam workshop!

=============================================================================
";
                    File.WriteAllText(readmePath, readmeContent);
                    Debug.Log($"[Scoreboard] Created README.txt in PresetPacks folder");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Scoreboard] Could not create README.txt: {ex.Message}");
                }
            }
        }
        
        private void ShowExportPackDialog()
        {
            try
            {
                var root = GetUIRoot();
                if (root == null) return;
                
                // Create dialog overlay
                var dialogOverlay = new VisualElement();
                dialogOverlay.name = "ExportPackDialogOverlay";
                dialogOverlay.style.position = Position.Absolute;
                dialogOverlay.style.left = 0;
                dialogOverlay.style.top = 0;
                dialogOverlay.style.right = 0;
                dialogOverlay.style.bottom = 0;
                dialogOverlay.style.backgroundColor = new Color(0, 0, 0, 0.8f);
                dialogOverlay.style.alignItems = Align.Center;
                dialogOverlay.style.justifyContent = Justify.Center;
                
                // Create dialog panel
                var dialog = new VisualElement();
                dialog.style.width = 600;
                dialog.style.maxHeight = 700;
                dialog.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
                dialog.style.borderTopLeftRadius = 8;
                dialog.style.borderTopRightRadius = 8;
                dialog.style.borderBottomLeftRadius = 8;
                dialog.style.borderBottomRightRadius = 8;
                dialog.style.paddingTop = 20;
                dialog.style.paddingBottom = 20;
                dialog.style.paddingLeft = 20;
                dialog.style.paddingRight = 20;
                
                // Title
                var title = new Label("Export Preset Pack for Workshop");
                title.style.fontSize = 18;
                title.style.color = P_White;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.marginBottom = 15;
                ForceUIFont(title);
                dialog.Add(title);
                
                // Pack name field
                var nameLabel = new Label("Pack Name:");
                nameLabel.style.color = P_White;
                nameLabel.style.marginBottom = 5;
                ForceUIFont(nameLabel);
                dialog.Add(nameLabel);
                
                var packNameField = new UITK.TextField { value = "My Preset Pack" };
                packNameField.style.backgroundColor = new StyleColor((Color)TextFieldBg);
                packNameField.style.color = P_White;
                packNameField.style.marginBottom = 15;
                ForceUIFont(packNameField);
                dialog.Add(packNameField);
                
                // Create containers for both pages
                var selectedTeamIndices = new List<int>();
                var selectedSizeIndices = new List<int>();
                
                // Page 1: Team Presets
                var teamPresetsPage = new VisualElement();
                teamPresetsPage.style.display = DisplayStyle.Flex;
                
                var teamInstructions = new Label("Step 1: Select team presets to include");
                teamInstructions.style.color = P_White;
                teamInstructions.style.marginBottom = 10;
                teamInstructions.style.fontSize = 14;
                teamInstructions.style.unityFontStyleAndWeight = FontStyle.Bold;
                ForceUIFont(teamInstructions);
                teamPresetsPage.Add(teamInstructions);
                
                var teamScrollView = new ScrollView(ScrollViewMode.Vertical);
                teamScrollView.style.maxHeight = 350;
                teamScrollView.style.marginBottom = 15;
                
                var teamCheckboxes = new List<Toggle>();
                
                for (int i = 0; i < _presetsConfig.teamPresets.Count; i++)
                {
                    var preset = _presetsConfig.teamPresets[i];
                    
                    // Skip workshop presets - only show Built-in presets for export
                    if (!string.IsNullOrEmpty(preset.packName) && preset.packName != "Built-in")
                    {
                        continue;
                    }
                    
                    var index = i;
                    
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 5;
                    row.style.paddingLeft = 10;
                    row.style.paddingRight = 10;
                    row.style.paddingTop = 5;
                    row.style.paddingBottom = 5;
                    row.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                    row.style.borderTopLeftRadius = 4;
                    row.style.borderTopRightRadius = 4;
                    row.style.borderBottomLeftRadius = 4;
                    row.style.borderBottomRightRadius = 4;
                    
                    var checkbox = new Toggle();
                    checkbox.value = false;
                    checkbox.style.marginRight = 10;
                    checkbox.RegisterValueChangedCallback(evt => {
                        if (evt.newValue && !selectedTeamIndices.Contains(index))
                        {
                            selectedTeamIndices.Add(index);
                        }
                        else if (!evt.newValue && selectedTeamIndices.Contains(index))
                        {
                            selectedTeamIndices.Remove(index);
                        }
                    });
                    teamCheckboxes.Add(checkbox);
                    row.Add(checkbox);
                    
                    var presetLabel = new Label($"{preset.presetName} ({preset.teamName})");
                    presetLabel.style.color = P_White;
                    presetLabel.style.flexGrow = 1;
                    ForceUIFont(presetLabel);
                    row.Add(presetLabel);
                    
                    teamScrollView.Add(row);
                }
                
                teamPresetsPage.Add(teamScrollView);
                
                var teamSelectButtonsRow = new VisualElement();
                teamSelectButtonsRow.style.flexDirection = FlexDirection.Row;
                teamSelectButtonsRow.style.marginBottom = 15;
                
                var teamSelectAllBtn = new Button(() => {
                    foreach (var cb in teamCheckboxes) cb.value = true;
                }) { text = "Select All" };
                styleButton(teamSelectAllBtn);
                teamSelectAllBtn.style.marginRight = 10;
                teamSelectButtonsRow.Add(teamSelectAllBtn);
                
                var teamDeselectAllBtn = new Button(() => {
                    foreach (var cb in teamCheckboxes) cb.value = false;
                }) { text = "Deselect All" };
                styleButton(teamDeselectAllBtn);
                teamSelectButtonsRow.Add(teamDeselectAllBtn);
                
                teamPresetsPage.Add(teamSelectButtonsRow);
                
                dialog.Add(teamPresetsPage);
                
                // Page 2: Size Presets
                var sizePresetsPage = new VisualElement();
                sizePresetsPage.style.display = DisplayStyle.None;
                
                var sizeInstructions = new Label("Step 2: Select size presets to include");
                sizeInstructions.style.color = P_White;
                sizeInstructions.style.marginBottom = 10;
                sizeInstructions.style.fontSize = 14;
                sizeInstructions.style.unityFontStyleAndWeight = FontStyle.Bold;
                ForceUIFont(sizeInstructions);
                sizePresetsPage.Add(sizeInstructions);
                
                var sizeScrollView = new ScrollView(ScrollViewMode.Vertical);
                sizeScrollView.style.maxHeight = 350;
                sizeScrollView.style.marginBottom = 15;
                
                var sizeCheckboxes = new List<Toggle>();
                
                for (int i = 0; i < _presetsConfig.sizePresets.Count; i++)
                {
                    var preset = _presetsConfig.sizePresets[i];
                    
                    // Skip workshop presets - only show Built-in presets for export
                    if (!string.IsNullOrEmpty(preset.packName) && preset.packName != "Built-in")
                    {
                        continue;
                    }
                    
                    var index = i;
                    
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 5;
                    row.style.paddingLeft = 10;
                    row.style.paddingRight = 10;
                    row.style.paddingTop = 5;
                    row.style.paddingBottom = 5;
                    row.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                    row.style.borderTopLeftRadius = 4;
                    row.style.borderTopRightRadius = 4;
                    row.style.borderBottomLeftRadius = 4;
                    row.style.borderBottomRightRadius = 4;
                    
                    var checkbox = new Toggle();
                    checkbox.value = false;
                    checkbox.style.marginRight = 10;
                    checkbox.RegisterValueChangedCallback(evt => {
                        if (evt.newValue && !selectedSizeIndices.Contains(index))
                        {
                            selectedSizeIndices.Add(index);
                        }
                        else if (!evt.newValue && selectedSizeIndices.Contains(index))
                        {
                            selectedSizeIndices.Remove(index);
                        }
                    });
                    sizeCheckboxes.Add(checkbox);
                    row.Add(checkbox);
                    
                    var presetLabel = new Label(preset.presetName);
                    presetLabel.style.color = P_White;
                    presetLabel.style.flexGrow = 1;
                    ForceUIFont(presetLabel);
                    row.Add(presetLabel);
                    
                    sizeScrollView.Add(row);
                }
                
                sizePresetsPage.Add(sizeScrollView);
                
                var sizeSelectButtonsRow = new VisualElement();
                sizeSelectButtonsRow.style.flexDirection = FlexDirection.Row;
                sizeSelectButtonsRow.style.marginBottom = 15;
                
                var sizeSelectAllBtn = new Button(() => {
                    foreach (var cb in sizeCheckboxes) cb.value = true;
                }) { text = "Select All" };
                styleButton(sizeSelectAllBtn);
                sizeSelectAllBtn.style.marginRight = 10;
                sizeSelectButtonsRow.Add(sizeSelectAllBtn);
                
                var sizeDeselectAllBtn = new Button(() => {
                    foreach (var cb in sizeCheckboxes) cb.value = false;
                }) { text = "Deselect All" };
                styleButton(sizeDeselectAllBtn);
                sizeSelectButtonsRow.Add(sizeDeselectAllBtn);
                
                sizePresetsPage.Add(sizeSelectButtonsRow);
                
                dialog.Add(sizePresetsPage);
                
                // Declare button rows first
                var page1ButtonsRow = new VisualElement();
                var page2ButtonsRow = new VisualElement();
                
                // Page 1 Buttons (Cancel and Next)
                page1ButtonsRow.style.flexDirection = FlexDirection.Row;
                page1ButtonsRow.style.justifyContent = Justify.FlexEnd;
                
                var cancelBtn = new Button(() => {
                    root.Remove(dialogOverlay);
                }) { text = "Cancel" };
                styleButton(cancelBtn);
                cancelBtn.style.marginRight = 10;
                page1ButtonsRow.Add(cancelBtn);
                
                var nextBtn = new Button(() => {
                    teamPresetsPage.style.display = DisplayStyle.None;
                    sizePresetsPage.style.display = DisplayStyle.Flex;
                    page1ButtonsRow.style.display = DisplayStyle.None;
                    page2ButtonsRow.style.display = DisplayStyle.Flex;
                }) { text = "Next" };
                styleButton(nextBtn);
                page1ButtonsRow.Add(nextBtn);
                
                dialog.Add(page1ButtonsRow);
                
                // Page 2 Buttons (Back and Export)
                page2ButtonsRow.style.flexDirection = FlexDirection.Row;
                page2ButtonsRow.style.justifyContent = Justify.FlexEnd;
                page2ButtonsRow.style.display = DisplayStyle.None;
                
                var backBtn = new Button(() => {
                    sizePresetsPage.style.display = DisplayStyle.None;
                    teamPresetsPage.style.display = DisplayStyle.Flex;
                    page2ButtonsRow.style.display = DisplayStyle.None;
                    page1ButtonsRow.style.display = DisplayStyle.Flex;
                }) { text = "Back" };
                styleButton(backBtn);
                backBtn.style.marginRight = 10;
                page2ButtonsRow.Add(backBtn);
                
                var exportBtn = new Button(() => {
                    if (selectedTeamIndices.Count == 0 && selectedSizeIndices.Count == 0)
                    {
                        Debug.LogWarning("[Scoreboard] No presets selected for export");
                        root.Remove(dialogOverlay);
                        return;
                    }
                    
                    string packName = packNameField.value;
                    if (string.IsNullOrWhiteSpace(packName))
                    {
                        packName = "My Preset Pack";
                    }
                    
                    ExportPresetPack(selectedTeamIndices, selectedSizeIndices, packName);
                    root.Remove(dialogOverlay);
                }) { text = "Export Pack" };
                styleButton(exportBtn);
                page2ButtonsRow.Add(exportBtn);
                
                dialog.Add(page2ButtonsRow);
                
                dialogOverlay.Add(dialog);
                root.Add(dialogOverlay);
                
                // Close on overlay click
                dialogOverlay.RegisterCallback<ClickEvent>(evt => {
                    if (evt.target == dialogOverlay)
                    {
                        root.Remove(dialogOverlay);
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[Scoreboard] Failed to show export dialog: {e}");
            }
        }
    }
}
