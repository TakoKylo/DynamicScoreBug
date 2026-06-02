using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UITK = UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        // Display string for "no linked TRL preset" in the per-row selector.
        private const string NoTrlPresetLabel = "(none)";

        // Names of TRL presets, refreshed each time the preset rows are (re)built.
        private List<string> _trlPresetNames = new List<string>();

        private void RefreshPresetRows()
        {
            _presetRows.Clear();
            _presetRowsRoot?.Clear();

            // Ensure presets are loaded
            if (_presetsConfig == null)
            {
                _presetsConfig = LoadPresetsConfig();
            }

            // Cache the list of TRL presets once per rebuild (each call reads from disk).
            _trlPresetNames = Integration.ToasterReskinLoaderPresetSync.GetPresetNames();
            
            // Group presets by pack name
            var groupedPresets = new Dictionary<string, List<(TeamPreset preset, int index)>>();
            
            for (int i = 0; i < _presetsConfig.teamPresets.Count; i++)
            {
                var preset = _presetsConfig.teamPresets[i];
                string packName = string.IsNullOrEmpty(preset.packName) ? "Built-in" : preset.packName;
                
                if (!groupedPresets.ContainsKey(packName))
                {
                    groupedPresets[packName] = new List<(TeamPreset, int)>();
                }
                groupedPresets[packName].Add((preset, i));
            }
            
            // Create sections for each pack
            foreach (var pack in groupedPresets.OrderBy(kvp => kvp.Key == "Built-in" ? 0 : 1).ThenBy(kvp => kvp.Key))
            {
                string packName = pack.Key;
                var presets = pack.Value;
                
                // Built-in presets are shown directly without a collapsible section
                if (packName == "Built-in")
                {
                    foreach (var (preset, index) in presets)
                    {
                        CreatePresetRowInContainer(preset, index, _presetRowsRoot);
                    }
                }
                else
                {
                    // Workshop packs get a collapsible section
                    var packToggleButton = new UITK.Button();
                    packToggleButton.text = $"▶ {packName}";
                    packToggleButton.style.height = 40;
                    packToggleButton.style.marginTop = 5;
                    packToggleButton.style.marginBottom = 5;
                    packToggleButton.style.unityTextAlign = TextAnchor.MiddleLeft;
                    packToggleButton.style.paddingLeft = 15;
                    styleButton(packToggleButton);
                    
                    var packContainer = new VisualElement();
                    packContainer.style.display = DisplayStyle.None;
                    packContainer.style.paddingLeft = 10;
                    
                    packToggleButton.clicked += () =>
                    {
                        bool isExpanded = packContainer.style.display == DisplayStyle.Flex;
                        packContainer.style.display = isExpanded ? DisplayStyle.None : DisplayStyle.Flex;
                        packToggleButton.text = isExpanded ? $"▶ {packName}" : $"▼ {packName}";
                    };
                    
                    _presetRowsRoot.Add(packToggleButton);
                    _presetRowsRoot.Add(packContainer);
                    
                    // Add presets for this pack
                    foreach (var (preset, index) in presets)
                    {
                        CreatePresetRowInContainer(preset, index, packContainer);
                    }
                }
            }
        }
        
        private void AddPresetRow(bool isBlue, string defaultName)
        {
            // Ensure presets are loaded
            if (_presetsConfig == null)
            {
                _presetsConfig = LoadPresetsConfig();
            }
            
            // Create preset from current blue or red side settings
            TeamPreset newPreset;
            if (isBlue)
            {
                newPreset = _config.CreateBluePresetFromCurrent(defaultName);
            }
            else
            {
                newPreset = _config.CreateRedPresetFromCurrent(defaultName);
            }
            
            _presetsConfig.teamPresets.Add(newPreset);
            SavePresetsConfig(_presetsConfig);
            RefreshPresetRows();
        }
        
        private void CreatePresetRowInContainer(TeamPreset preset, int index, VisualElement container)
        {
            var model = new PresetRow { presetIndex = index, isBluePreset = false }; // isBluePreset no longer needed
            
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = 50;
            row.style.marginBottom = 10;
            row.style.backgroundColor = new StyleColor(RowBg);
            row.style.paddingLeft = 12;
            row.style.paddingRight = 10;
            row.style.paddingTop = 8;
            row.style.paddingBottom = 8;
            
            // Name text field
            var nameField = new UITK.TextField { value = preset.presetName };
            MakeReadable(nameField);
            nameField.style.flexGrow = 1;
            nameField.style.marginRight = 8;
            nameField.RegisterValueChangedCallback(evt =>
            {
                // Update preset name
                if (index < _presetsConfig.teamPresets.Count)
                {
                    _presetsConfig.teamPresets[index].presetName = evt.newValue;
                    SavePresetsConfig(_presetsConfig);
                }
            });
            row.Add(nameField);
            
            // Up arrow button (only show if not the first preset)
            if (index > 0)
            {
                var upBtn = new UITK.Button(() =>
                {
                    if (index > 0 && index < _presetsConfig.teamPresets.Count)
                    {
                        // Swap with previous preset
                        var temp = _presetsConfig.teamPresets[index];
                        _presetsConfig.teamPresets[index] = _presetsConfig.teamPresets[index - 1];
                        _presetsConfig.teamPresets[index - 1] = temp;
                        SavePresetsConfig(_presetsConfig);
                        RefreshPresetRows();
                    }
                });
                upBtn.text = "▲";
                styleButton(upBtn);
                // Override styleButton defaults AFTER calling it
                upBtn.style.width = 28;
                upBtn.style.minWidth = 28;
                upBtn.style.maxWidth = 28;
                upBtn.style.height = 30;
                upBtn.style.marginRight = 4;
                upBtn.style.marginLeft = 0;
                upBtn.style.fontSize = 12;
                upBtn.style.paddingLeft = 0;
                upBtn.style.paddingRight = 0;
                upBtn.style.paddingTop = 0;
                upBtn.style.paddingBottom = 0;
                upBtn.style.flexGrow = 0;
                upBtn.style.flexShrink = 0;
                row.Add(upBtn);
            }
            
            // Down arrow button (only show if not the last preset)
            if (index < _presetsConfig.teamPresets.Count - 1)
            {
                var downBtn = new UITK.Button(() =>
                {
                    if (index >= 0 && index < _presetsConfig.teamPresets.Count - 1)
                    {
                        // Swap with next preset
                        var temp = _presetsConfig.teamPresets[index];
                        _presetsConfig.teamPresets[index] = _presetsConfig.teamPresets[index + 1];
                        _presetsConfig.teamPresets[index + 1] = temp;
                        SavePresetsConfig(_presetsConfig);
                        RefreshPresetRows();
                    }
                });
                downBtn.text = "▼";
                styleButton(downBtn);
                // Override styleButton defaults AFTER calling it
                downBtn.style.width = 28;
                downBtn.style.minWidth = 28;
                downBtn.style.maxWidth = 28;
                downBtn.style.height = 30;
                downBtn.style.marginRight = 8;
                downBtn.style.marginLeft = 0;
                downBtn.style.fontSize = 12;
                downBtn.style.paddingLeft = 0;
                downBtn.style.paddingRight = 0;
                downBtn.style.paddingTop = 0;
                downBtn.style.paddingBottom = 0;
                downBtn.style.flexGrow = 0;
                downBtn.style.flexShrink = 0;
                row.Add(downBtn);
            }
            
            // Remove button
            var removeBtn = new UITK.Button(() =>
            {
                if (index < _presetsConfig.teamPresets.Count)
                {
                    _presetsConfig.teamPresets.RemoveAt(index);
                    SavePresetsConfig(_presetsConfig);
                    RefreshPresetRows();
                }
            });
            removeBtn.text = "REMOVE";
            removeBtn.style.width = 80;
            removeBtn.style.height = 30;
            removeBtn.style.marginRight = 8;
            styleButton(removeBtn);
            row.Add(removeBtn);

            // Optional link to a ToasterReskinLoader preset. The selector only appears when TRL's
            // preset system is installed (or this preset already has a saved link, so it isn't lost).
            // When set, applying this team preset to a side also applies the chosen TRL preset to it.
            string storedTrl = preset.trlPresetName ?? "";
            if (Integration.ToasterReskinLoaderPresetSync.IsPresetSystemAvailable || !string.IsNullOrEmpty(storedTrl))
            {
                var trlLabel = new UITK.Label("TRL");
                MakeReadable(trlLabel);
                trlLabel.style.fontSize = 16;
                trlLabel.style.marginRight = 4;
                row.Add(trlLabel);

                var trlChoices = new List<string> { NoTrlPresetLabel };
                trlChoices.AddRange(_trlPresetNames);
                // Keep an orphaned saved value selectable so it isn't silently dropped (e.g. TRL not loaded yet).
                if (!string.IsNullOrEmpty(storedTrl) && !trlChoices.Contains(storedTrl))
                    trlChoices.Add(storedTrl);

                var trlDropdown = new UITK.DropdownField { choices = trlChoices };
                trlDropdown.value = string.IsNullOrEmpty(storedTrl) ? NoTrlPresetLabel : storedTrl;
                trlDropdown.style.width = 150;
                trlDropdown.style.height = 30;
                trlDropdown.style.marginRight = 8;
                StyleDropdown(trlDropdown);
                trlDropdown.RegisterValueChangedCallback(evt =>
                {
                    if (index < _presetsConfig.teamPresets.Count)
                    {
                        _presetsConfig.teamPresets[index].trlPresetName =
                            evt.newValue == NoTrlPresetLabel ? "" : evt.newValue;
                        SavePresetsConfig(_presetsConfig);
                    }
                });
                row.Add(trlDropdown);
            }

            // Add BOTH apply buttons for all presets (can apply any preset to either side)
            // Apply to Blue button
            var applyBlueBtn = new UITK.Button(() =>
            {
                _config.blueTeamName = preset.teamName;
                _config.blueTeamColorHex = preset.teamColorHex;
                _config.blueTeamTextColorHex = preset.teamTextColorHex;
                _config.blueBorderColorHex = preset.borderColorHex;
                _config.blueGradientLeftColorHex = preset.gradientLeftColorHex;
                _config.blueGradientRightColorHex = preset.gradientRightColorHex;
                _config.blueTeamLogoFile = preset.logoFile;
                _config.blueLogoWidth = preset.logoWidth;
                _config.blueLogoHeight = preset.logoHeight;
                _config.blueLogoOffsetX = preset.logoOffsetX;
                _config.blueLogoOffsetY = preset.logoOffsetY;
                _config.blueMinimapPlayerColorHex = preset.minimapPlayerColorHex;
                _config.blueMinimapNumberColorHex = preset.minimapNumberColorHex;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();

                // Apply the linked ToasterReskinLoader preset to the same (blue/left) side, if any.
                Integration.ToasterReskinLoaderPresetSync.ApplyPresetByName(preset.trlPresetName, true);

                // Refresh the UI panel to show new config values
                RefreshUI();
            });
            applyBlueBtn.text = "APPLY LEFT";
            applyBlueBtn.style.width = 100;
            applyBlueBtn.style.height = 30;
            applyBlueBtn.style.marginRight = 8;
            styleButton(applyBlueBtn);
            row.Add(applyBlueBtn);
            
            // Apply to Red button
            var applyRedBtn = new UITK.Button(() =>
            {
                _config.redTeamName = preset.teamName;
                _config.redTeamColorHex = preset.teamColorHex;
                _config.redTeamTextColorHex = preset.teamTextColorHex;
                _config.redBorderColorHex = preset.borderColorHex;
                _config.redGradientLeftColorHex = preset.gradientLeftColorHex;
                _config.redGradientRightColorHex = preset.gradientRightColorHex;
                _config.redTeamLogoFile = preset.logoFile;
                _config.redLogoWidth = preset.logoWidth;
                _config.redLogoHeight = preset.logoHeight;
                _config.redLogoOffsetX = preset.logoOffsetX;
                _config.redLogoOffsetY = preset.logoOffsetY;
                _config.redMinimapPlayerColorHex = preset.minimapPlayerColorHex;
                _config.redMinimapNumberColorHex = preset.minimapNumberColorHex;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();

                // Apply the linked ToasterReskinLoader preset to the same (red/right) side, if any.
                Integration.ToasterReskinLoaderPresetSync.ApplyPresetByName(preset.trlPresetName, false);

                // Refresh the UI panel to show new config values
                RefreshUI();
            });
            applyRedBtn.text = "APPLY RIGHT";
            applyRedBtn.style.width = 110;
            applyRedBtn.style.height = 30;
            styleButton(applyRedBtn);
            row.Add(applyRedBtn);
            
            model.nameField = nameField;
            model.row = row;
            _presetRows.Add(model);
            container.Add(row);
        }
        
        private void RefreshSizePresetRows()
        {
            _sizePresetRows.Clear();
            _sizePresetRowsRoot?.Clear();
            
            // Ensure presets are loaded
            if (_presetsConfig == null)
            {
                _presetsConfig = LoadPresetsConfig();
            }
            
            // Group presets by pack name
            var groupedPresets = new Dictionary<string, List<(SizePreset preset, int index)>>();
            
            for (int i = 0; i < _presetsConfig.sizePresets.Count; i++)
            {
                var preset = _presetsConfig.sizePresets[i];
                string packName = string.IsNullOrEmpty(preset.packName) ? "Built-in" : preset.packName;
                
                if (!groupedPresets.ContainsKey(packName))
                {
                    groupedPresets[packName] = new List<(SizePreset, int)>();
                }
                groupedPresets[packName].Add((preset, i));
            }
            
            // Create sections for each pack
            foreach (var pack in groupedPresets.OrderBy(kvp => kvp.Key == "Built-in" ? 0 : 1).ThenBy(kvp => kvp.Key))
            {
                string packName = pack.Key;
                var presets = pack.Value;
                
                // Built-in presets are shown directly without a collapsible section
                if (packName == "Built-in")
                {
                    foreach (var (preset, index) in presets)
                    {
                        CreateSizePresetRowInContainer(preset, index, _sizePresetRowsRoot);
                    }
                }
                else
                {
                    // Workshop packs get a collapsible section
                    var packToggleButton = new UITK.Button();
                    packToggleButton.text = $"▶ {packName}";
                    packToggleButton.style.height = 40;
                    packToggleButton.style.marginTop = 5;
                    packToggleButton.style.marginBottom = 5;
                    packToggleButton.style.unityTextAlign = TextAnchor.MiddleLeft;
                    packToggleButton.style.paddingLeft = 15;
                    styleButton(packToggleButton);
                    
                    var packContainer = new VisualElement();
                    packContainer.style.display = DisplayStyle.None;
                    packContainer.style.paddingLeft = 10;
                    
                    packToggleButton.clicked += () =>
                    {
                        bool isExpanded = packContainer.style.display == DisplayStyle.Flex;
                        packContainer.style.display = isExpanded ? DisplayStyle.None : DisplayStyle.Flex;
                        packToggleButton.text = isExpanded ? $"▶ {packName}" : $"▼ {packName}";
                    };
                    
                    _sizePresetRowsRoot.Add(packToggleButton);
                    _sizePresetRowsRoot.Add(packContainer);
                    
                    // Add presets for this pack
                    foreach (var (preset, index) in presets)
                    {
                        CreateSizePresetRowInContainer(preset, index, packContainer);
                    }
                }
            }
        }
        
        private void AddSizePresetRow(string defaultName)
        {
            // Ensure presets are loaded
            if (_presetsConfig == null)
            {
                _presetsConfig = LoadPresetsConfig();
            }
            
            // Create preset from current size settings
            SizePreset newPreset = _config.CreateSizePresetFromCurrent(defaultName);
            
            _presetsConfig.sizePresets.Add(newPreset);
            SavePresetsConfig(_presetsConfig);
            RefreshSizePresetRows();
        }
        
        private void CreateSizePresetRowInContainer(SizePreset preset, int index, VisualElement container)
        {
            var model = new SizePresetRow { presetIndex = index };
            
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = 50;
            row.style.marginBottom = 10;
            row.style.backgroundColor = new StyleColor(RowBg);
            row.style.paddingLeft = 12;
            row.style.paddingRight = 10;
            row.style.paddingTop = 8;
            row.style.paddingBottom = 8;
            
            // Name text field
            var nameField = new UITK.TextField { value = preset.presetName };
            MakeReadable(nameField);
            nameField.style.flexGrow = 1;
            nameField.style.marginRight = 8;
            nameField.RegisterValueChangedCallback(evt =>
            {
                // Update preset name
                if (index < _presetsConfig.sizePresets.Count)
                {
                    _presetsConfig.sizePresets[index].presetName = evt.newValue;
                    SavePresetsConfig(_presetsConfig);
                }
            });
            row.Add(nameField);
            
            // Up arrow button (only show if not the first preset)
            if (index > 0)
            {
                var upBtn = new UITK.Button(() =>
                {
                    if (index > 0 && index < _presetsConfig.sizePresets.Count)
                    {
                        // Swap with previous preset
                        var temp = _presetsConfig.sizePresets[index];
                        _presetsConfig.sizePresets[index] = _presetsConfig.sizePresets[index - 1];
                        _presetsConfig.sizePresets[index - 1] = temp;
                        SavePresetsConfig(_presetsConfig);
                        RefreshSizePresetRows();
                    }
                });
                upBtn.text = "▲";
                styleButton(upBtn);
                upBtn.style.width = 28;
                upBtn.style.minWidth = 28;
                upBtn.style.maxWidth = 28;
                upBtn.style.height = 30;
                upBtn.style.marginRight = 4;
                upBtn.style.marginLeft = 0;
                upBtn.style.fontSize = 12;
                upBtn.style.paddingLeft = 0;
                upBtn.style.paddingRight = 0;
                upBtn.style.paddingTop = 0;
                upBtn.style.paddingBottom = 0;
                upBtn.style.flexGrow = 0;
                upBtn.style.flexShrink = 0;
                row.Add(upBtn);
            }
            
            // Down arrow button (only show if not the last preset)
            if (index < _presetsConfig.sizePresets.Count - 1)
            {
                var downBtn = new UITK.Button(() =>
                {
                    if (index >= 0 && index < _presetsConfig.sizePresets.Count - 1)
                    {
                        // Swap with next preset
                        var temp = _presetsConfig.sizePresets[index];
                        _presetsConfig.sizePresets[index] = _presetsConfig.sizePresets[index + 1];
                        _presetsConfig.sizePresets[index + 1] = temp;
                        SavePresetsConfig(_presetsConfig);
                        RefreshSizePresetRows();
                    }
                });
                downBtn.text = "▼";
                styleButton(downBtn);
                downBtn.style.width = 28;
                downBtn.style.minWidth = 28;
                downBtn.style.maxWidth = 28;
                downBtn.style.height = 30;
                downBtn.style.marginRight = 8;
                downBtn.style.marginLeft = 0;
                downBtn.style.fontSize = 12;
                downBtn.style.paddingLeft = 0;
                downBtn.style.paddingRight = 0;
                downBtn.style.paddingTop = 0;
                downBtn.style.paddingBottom = 0;
                downBtn.style.flexGrow = 0;
                downBtn.style.flexShrink = 0;
                row.Add(downBtn);
            }
            
            // Remove button
            var removeBtn = new UITK.Button(() =>
            {
                if (index < _presetsConfig.sizePresets.Count)
                {
                    _presetsConfig.sizePresets.RemoveAt(index);
                    SavePresetsConfig(_presetsConfig);
                    RefreshSizePresetRows();
                }
            });
            removeBtn.text = "REMOVE";
            removeBtn.style.width = 80;
            removeBtn.style.height = 30;
            removeBtn.style.marginRight = 8;
            styleButton(removeBtn);
            row.Add(removeBtn);
            
            // Apply button
            var applyBtn = new UITK.Button(() =>
            {
                if (index < _presetsConfig.sizePresets.Count)
                {
                    _config.ApplySizePreset(_presetsConfig.sizePresets[index]);
                    SaveScoreboardConfig(_config);
                    
                    // Refresh scoreboard with new size settings
                    if (_scoreboardReference != null)
                    {
                        var customScoreboard = _scoreboardReference as CustomBroadcastScoreboard;
                        if (customScoreboard != null && _config.enableCustomScoreboard)
                        {
                            customScoreboard.RefreshScoreboardUI();
                        }
                    }
                    
                    // Refresh the UI panel to show new config values
                    RefreshUI();
                    
                    Debug.Log($"[Scoreboard] Applied size preset: {_presetsConfig.sizePresets[index].presetName}");
                }
            });
            applyBtn.text = "APPLY";
            applyBtn.style.width = 80;
            applyBtn.style.height = 30;
            styleButton(applyBtn);
            row.Add(applyBtn);
            
            model.nameField = nameField;
            model.row = row;
            _sizePresetRows.Add(model);
            container.Add(row);
        }
    }
}
