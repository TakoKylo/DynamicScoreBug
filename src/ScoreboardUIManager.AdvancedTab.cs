using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UITK = UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        private void BuildAdvancedTab(VisualElement container)
        {
            // Team settings
            AddSection(container, "TEAM SETTINGS");
            container.Add(MakeTextFieldRow("BLUE TEAM NAME", _config.blueTeamName, (value) => {
                _config.blueTeamName = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
                Integration.ToasterReskinLoaderColorSync.SyncTeamNames(_config.blueTeamName, _config.redTeamName);
            }));
            container.Add(MakeTextFieldRow("RED TEAM NAME", _config.redTeamName, (value) => {
                _config.redTeamName = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
                Integration.ToasterReskinLoaderColorSync.SyncTeamNames(_config.blueTeamName, _config.redTeamName);
            }));
            container.Add(MakeTextFieldRow("BLUE TEAM COLOR", _config.blueTeamColorHex, (value) => {
                _config.blueTeamColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("RED TEAM COLOR", _config.redTeamColorHex, (value) => {
                _config.redTeamColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("BLUE TEXT COLOR", _config.blueTeamTextColorHex, (value) => {
                _config.blueTeamTextColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("RED TEXT COLOR", _config.redTeamTextColorHex, (value) => {
                _config.redTeamTextColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("BLUE MINIMAP COLOR <size=12>leave empty for team color</size>", _config.blueMinimapPlayerColorHex, (value) => {
                _config.blueMinimapPlayerColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.ApplyConfigChanges(_config);
            }));
            container.Add(MakeTextFieldRow("RED MINIMAP COLOR <size=12>leave empty for team color</size>", _config.redMinimapPlayerColorHex, (value) => {
                _config.redMinimapPlayerColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.ApplyConfigChanges(_config);
            }));
            container.Add(MakeTextFieldRow("BLUE MINIMAP NUMBER COLOR <size=12>leave empty for text color</size>", _config.blueMinimapNumberColorHex, (value) => {
                _config.blueMinimapNumberColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.ApplyConfigChanges(_config);
            }));
            container.Add(MakeTextFieldRow("RED MINIMAP NUMBER COLOR <size=12>leave empty for text color</size>", _config.redMinimapNumberColorHex, (value) => {
                _config.redMinimapNumberColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.ApplyConfigChanges(_config);
            }));

            // Border settings
            AddSection(container, "BORDERS & GRADIENTS");

            UITK.Slider borderSlider; UITK.TextField borderField;
            container.Add(MakeSliderRow("BORDER WIDTH", _config.borderWidth, 0f, 10f, out borderSlider, out borderField, (value) => {
                _config.borderWidth = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));

            container.Add(MakeTextFieldRow("BLUE BORDER COLOR", _config.blueBorderColorHex, (value) => {
                _config.blueBorderColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("RED BORDER COLOR", _config.redBorderColorHex, (value) => {
                _config.redBorderColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));

            container.Add(MakeToggleRow("ENABLE GRADIENTS", _config.enableGradients, (value) => {
                _config.enableGradients = value;
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));

            container.Add(MakeTextFieldRow("BLUE GRADIENT LEFT", _config.blueGradientLeftColorHex, (value) => {
                _config.blueGradientLeftColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("BLUE GRADIENT RIGHT", _config.blueGradientRightColorHex, (value) => {
                _config.blueGradientRightColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("RED GRADIENT LEFT", _config.redGradientLeftColorHex, (value) => {
                _config.redGradientLeftColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("RED GRADIENT RIGHT", _config.redGradientRightColorHex, (value) => {
                _config.redGradientRightColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));

            // Section styling
            AddSection(container, "SECTION STYLING");
            
            UITK.Slider leagueSecWidthSlider; UITK.TextField leagueSecWidthField;
            container.Add(MakeSliderRow("LEAGUE LOGO SECTION WIDTH", _config.leagueLogoSectionWidth, 10f, 100f, out leagueSecWidthSlider, out leagueSecWidthField, (value) => {
                _config.leagueLogoSectionWidth = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("LEAGUE SECTION COLOR", _config.leagueLogoSectionColorHex, (value) => {
                _config.leagueLogoSectionColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("LEAGUE SECTION BORDER", _config.leagueLogoSectionBorderColorHex, (value) => {
                _config.leagueLogoSectionBorderColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            
            UITK.Slider periodWidthSlider; UITK.TextField periodWidthField;
            container.Add(MakeSliderRow("PERIOD BOX WIDTH", _config.periodBoxWidth, 40f, 200f, out periodWidthSlider, out periodWidthField, (value) => {
                _config.periodBoxWidth = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("PERIOD BOX COLOR", _config.periodBoxColorHex, (value) => {
                _config.periodBoxColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("PERIOD BOX BORDER", _config.periodBoxBorderColorHex, (value) => {
                _config.periodBoxBorderColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("PERIOD TEXT COLOR", _config.periodTextColorHex, (value) => {
                _config.periodTextColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            
            UITK.Slider timeWidthSlider; UITK.TextField timeWidthField;
            container.Add(MakeSliderRow("TIME BOX WIDTH", _config.timeBoxWidth, 60f, 300f, out timeWidthSlider, out timeWidthField, (value) => {
                _config.timeBoxWidth = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("TIME BOX COLOR", _config.timeBoxColorHex, (value) => {
                _config.timeBoxColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("TIME BOX BORDER", _config.timeBoxBorderColorHex, (value) => {
                _config.timeBoxBorderColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));
            container.Add(MakeTextFieldRow("TIME TEXT COLOR", _config.timeTextColorHex, (value) => {
                _config.timeTextColorHex = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.RefreshScoreboardUI();
            }));

            // Logo file selection
            AddSection(container, "LOGO FILES");
            var logoFiles = GetLogoFiles();
            container.Add(MakeDropdownRow("LEAGUE LOGO FILE", _config.leagueLogoFile, logoFiles, (value) =>
            {
                _config.leagueLogoFile = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));
            container.Add(MakeDropdownRow("BLUE TEAM LOGO FILE", _config.blueTeamLogoFile, logoFiles, (value) =>
            {
                _config.blueTeamLogoFile = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));
            container.Add(MakeDropdownRow("RED TEAM LOGO FILE", _config.redTeamLogoFile, logoFiles, (value) =>
            {
                _config.redTeamLogoFile = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));

            // Logo positions (league logo is anchored to scorebug now; X/Y sliders removed.)
            AddSection(container, "LOGO POSITIONS");

            UITK.Slider blueXSlider; UITK.TextField blueXField;
            container.Add(MakeSliderRow("BLUE LOGO X", _config.blueLogoOffsetX, -1000f, 1000f, out blueXSlider, out blueXField, (value) =>
            {
                _config.blueLogoOffsetX = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));

            UITK.Slider blueYSlider; UITK.TextField blueYField;
            container.Add(MakeSliderRow("BLUE LOGO Y", _config.blueLogoOffsetY, -1000f, 1000f, out blueYSlider, out blueYField, (value) =>
            {
                _config.blueLogoOffsetY = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));

            UITK.Slider redXSlider; UITK.TextField redXField;
            container.Add(MakeSliderRow("RED LOGO X", _config.redLogoOffsetX, -1000f, 1000f, out redXSlider, out redXField, (value) =>
            {
                _config.redLogoOffsetX = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));

            UITK.Slider redYSlider; UITK.TextField redYField;
            container.Add(MakeSliderRow("RED LOGO Y", _config.redLogoOffsetY, -1000f, 1000f, out redYSlider, out redYField, (value) =>
            {
                _config.redLogoOffsetY = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));

            // Logo sizes
            AddSection(container, "LOGO SIZES");

            UITK.Slider leagueWidthSlider; UITK.TextField leagueWidthField;
            container.Add(MakeSliderRow("LEAGUE LOGO WIDTH", _config.leagueLogoWidth, 20f, 400f, out leagueWidthSlider, out leagueWidthField, (value) =>
            {
                _config.leagueLogoWidth = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));

            UITK.Slider leagueHeightSlider; UITK.TextField leagueHeightField;
            container.Add(MakeSliderRow("LEAGUE LOGO HEIGHT", _config.leagueLogoHeight, 20f, 400f, out leagueHeightSlider, out leagueHeightField, (value) =>
            {
                _config.leagueLogoHeight = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));

            UITK.Slider blueWidthSlider; UITK.TextField blueWidthField;
            container.Add(MakeSliderRow("BLUE LOGO WIDTH", _config.blueLogoWidth, 20f, 300f, out blueWidthSlider, out blueWidthField, (value) =>
            {
                _config.blueLogoWidth = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));

            UITK.Slider blueHeightSlider; UITK.TextField blueHeightField;
            container.Add(MakeSliderRow("BLUE LOGO HEIGHT", _config.blueLogoHeight, 20f, 300f, out blueHeightSlider, out blueHeightField, (value) =>
            {
                _config.blueLogoHeight = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));

            UITK.Slider redWidthSlider; UITK.TextField redWidthField;
            container.Add(MakeSliderRow("RED LOGO WIDTH", _config.redLogoWidth, 20f, 300f, out redWidthSlider, out redWidthField, (value) =>
            {
                _config.redLogoWidth = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));

            UITK.Slider redHeightSlider; UITK.TextField redHeightField;
            container.Add(MakeSliderRow("RED LOGO HEIGHT", _config.redLogoHeight, 20f, 300f, out redHeightSlider, out redHeightField, (value) =>
            {
                _config.redLogoHeight = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.HotswapLogos(_config);
            }));

            // Position settings
            AddSection(container, "POSITION & SCALE");

            UITK.Slider xSlider; UITK.TextField xField;
            container.Add(MakeSliderRow("X POSITION", _config.scoreboardX, -2000f, 1000f, out xSlider, out xField, (value) =>
            {
                _config.scoreboardX = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.UpdatePositionAndScale(_config);
            }));

            UITK.Slider ySlider; UITK.TextField yField;
            container.Add(MakeSliderRow("Y POSITION", _config.scoreboardY, -1000f, 1000f, out ySlider, out yField, (value) =>
            {
                _config.scoreboardY = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.UpdatePositionAndScale(_config);
            }));

            UITK.Slider scaleSlider; UITK.TextField scaleField;
            container.Add(MakeSliderRow("SCALE", _config.scoreboardScale, 0.3f, 3.0f, out scaleSlider, out scaleField, (value) =>
            {
                _config.scoreboardScale = value;
                SaveScoreboardConfig(_config);
                if (_scoreboardReference != null) _scoreboardReference.UpdatePositionAndScale(_config);
            }));

            // Popup positioning is now automatic — popups are children of the scorebug
            // and follow its position/scale. Per-popup X/Y/scale offsets and slide-distance
            // sliders were removed. Width/height sliders remain in POPUP SIZES below.

            // Popup sizes and goal/win animation dimensions are hardcoded (anchored to
            // scorebug) — sliders for them were removed. Only the animation team-logo
            // settings remain since those affect visual style, not anchor math.
            AddSection(container, "GOAL/WIN ANIMATION LOGO");

            UITK.Slider animLogoWidthSlider; UITK.TextField animLogoWidthField;
            container.Add(MakeSliderRow("TEAM LOGO WIDTH", _config.animationLogoWidth, 50f, 500f, out animLogoWidthSlider, out animLogoWidthField, (value) => { _config.animationLogoWidth = value; SaveScoreboardConfig(_config); }));

            UITK.Slider animLogoHeightSlider; UITK.TextField animLogoHeightField;
            container.Add(MakeSliderRow("TEAM LOGO HEIGHT", _config.animationLogoHeight, 50f, 500f, out animLogoHeightSlider, out animLogoHeightField, (value) => { _config.animationLogoHeight = value; SaveScoreboardConfig(_config); }));

            UITK.Slider animLogoOpacitySlider; UITK.TextField animLogoOpacityField;
            container.Add(MakeSliderRow("TEAM LOGO OPACITY", _config.animationLogoOpacity, 0f, 1f, out animLogoOpacitySlider, out animLogoOpacityField, (value) => { _config.animationLogoOpacity = value; SaveScoreboardConfig(_config); }));

        }
    }
}
