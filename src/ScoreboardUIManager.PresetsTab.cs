using System;
using UnityEngine;
using UnityEngine.UIElements;
using UITK = UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        private void BuildPresetsTab(VisualElement container)
        {
            // ======================
            // TEAM PRESETS
            // ======================
            AddSection(container, "TEAM PRESETS");
            
            _teamPresetsContainer = new VisualElement();
            container.Add(_teamPresetsContainer);
            
            _presetRowsRoot = new VisualElement();
            _teamPresetsContainer.Add(_presetRowsRoot);
            
            RefreshPresetRows();
            
            var addButtonsRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 50,
                    marginBottom = 22,
                    marginTop = 10
                }
            };
            
            var addBlueButton = new UITK.Button(() => AddPresetRow(true, "New Team Preset"));
            addBlueButton.text = "CREATE PRESET FROM BLUE SIDE";
            addBlueButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            addBlueButton.style.height = 50;
            addBlueButton.style.flexGrow = 1;
            addBlueButton.style.marginRight = 6;
            styleButton(addBlueButton);
            addButtonsRow.Add(addBlueButton);
            
            var addRedButton = new UITK.Button(() => AddPresetRow(false, "New Team Preset"));
            addRedButton.text = "CREATE PRESET FROM RED SIDE";
            addRedButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            addRedButton.style.height = 50;
            addRedButton.style.flexGrow = 1;
            addRedButton.style.marginLeft = 6;
            styleButton(addRedButton);
            addButtonsRow.Add(addRedButton);
            
            _teamPresetsContainer.Add(addButtonsRow);

            // ======================
            // SIZE PRESETS
            // ======================
            AddSection(container, "SIZE PRESETS");
            
            _sizePresetsContainer = new VisualElement();
            container.Add(_sizePresetsContainer);
            
            _sizePresetRowsRoot = new VisualElement();
            _sizePresetsContainer.Add(_sizePresetRowsRoot);
            
            RefreshSizePresetRows();
            
            var addSizePresetRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 50,
                    marginBottom = 22,
                    marginTop = 10
                }
            };
            
            var addSizePresetButton = new UITK.Button(() => AddSizePresetRow("New Size Preset"));
            addSizePresetButton.text = "CREATE SIZE PRESET FROM CURRENT SETTINGS";
            addSizePresetButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            addSizePresetButton.style.height = 50;
            addSizePresetButton.style.flexGrow = 1;
            styleButton(addSizePresetButton);
            addSizePresetRow.Add(addSizePresetButton);
            
            _sizePresetsContainer.Add(addSizePresetRow);
        }
    }
}
