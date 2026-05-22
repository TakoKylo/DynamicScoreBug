using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

namespace CustomScoreboard.UI
{
    /// <summary>
    /// Partial class containing UI creation methods.
    /// Main scoreboard UI construction and configuration loading/saving.
    /// </summary>
    public partial class CustomBroadcastScoreboard
    {
        private void CreateCustomScoreboard()
        {
            try
            {
                VisualElement root = GetRootVisualElement();
                if (root == null) return;

                // Get config - create default if needed
                config = LoadScoreboardConfig();
            
                // Sync colors to ToasterReskinLoader
                CustomScoreboard.Integration.ToasterReskinLoaderColorSync.SyncTeamColors(config.blueTeamColorHex, config.redTeamColorHex);
                
                // Sync minimap colors to ToasterReskinLoader (falls back to team colors if empty)
                CustomScoreboard.Integration.ToasterReskinLoaderColorSync.SyncMinimapColors(
                    config.blueMinimapPlayerColorHex, 
                    config.redMinimapPlayerColorHex,
                    config.blueMinimapNumberColorHex,
                    config.redMinimapNumberColorHex,
                    config.blueTeamColorHex,
                    config.redTeamColorHex,
                    config.blueTeamTextColorHex,
                    config.redTeamTextColorHex
                );
                
                // Sync team names to ToasterReskinLoader
                CustomScoreboard.Integration.ToasterReskinLoaderColorSync.SyncTeamNames(config.blueTeamName, config.redTeamName);
            
                DebugLog($"[CustomScoreboard] Config loaded: enableCustomScoreboard = {config.enableCustomScoreboard}");
            
                if (!config.enableCustomScoreboard) 
                {
                    DebugLog("[CustomScoreboard] Scoreboard disabled in config, not creating UI");
                    return;
                }

                // Parse team colors from config
                Color blueTeamColor = GetBlueTeamColor();
                Color redTeamColor = GetRedTeamColor();

            // Get the UI font from UIManager
            UnityEngine.Font uiFont = null;
            try
            {
                var uiManager = GetUIManager();
                var panelSettings = uiManager?.PanelSettings;
                if (panelSettings != null && panelSettings.textSettings != null)
                {
                    var fontAsset = panelSettings.textSettings.defaultFontAsset;
                    if (fontAsset != null)
                    {
                        uiFont = fontAsset.sourceFontFile;
                    }
                }
            }
            catch { }

            if (uiFont == null)
            {
                // Fallback to Arial if we can't get the UI font
                uiFont = UnityEngine.Font.CreateDynamicFontFromOSFont("Arial", 14);
            }

            // Load logo images from config filenames
            Texture2D leagueLogoTexture = LoadLogoImage(config.leagueLogoFile);
            Texture2D blueLogoTexture = LoadLogoImage(config.blueTeamLogoFile);
            Texture2D redLogoTexture = LoadLogoImage(config.redTeamLogoFile);
            
            // Store team logos for animation use
            blueTeamLogoTexture = blueLogoTexture;
            redTeamLogoTexture = redLogoTexture;

            DebugLog("[CustomScoreboard] Creating scoreboard container...");
            scoreboardContainer = new VisualElement();
            scoreboardContainer.name = "CustomBroadcastScoreboard";
            scoreboardContainer.style.position = Position.Absolute;
            scoreboardContainer.style.top = config.scoreboardY;
            scoreboardContainer.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            scoreboardContainer.style.translate = new StyleTranslate(new Translate(new Length(config.scoreboardX, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel)));
            scoreboardContainer.style.width = 780; // 280 blue + 20 center + 280 red + 80 period + 120 time
            scoreboardContainer.style.height = 41; // 40px + 1px top border
            scoreboardContainer.style.scale = new StyleScale(new Scale(new Vector2(config.scoreboardScale, config.scoreboardScale)));
            scoreboardContainer.style.flexDirection = FlexDirection.Row;
            scoreboardContainer.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f); // Transparent background
            scoreboardContainer.style.alignItems = Align.Center;
            scoreboardContainer.style.display = DisplayStyle.None; // Start hidden until game starts
            scoreboardContainer.pickingMode = PickingMode.Ignore;

            // League logo is created later by AddLeagueLogo() (anchored to scoreboardContainer).

            // Create blue section
            CreateBlueSection(uiFont, blueTeamColor, blueLogoTexture);

            // League logo section - sits between blue and red (small container)
            CreateLeagueLogoSection();

            // Create red section
            CreateRedSection(uiFont, redTeamColor, redLogoTexture);

            // Time section - split into two sections: black for period, grey for time
            CreateTimeSection(uiFont);
            
            // Create stat popups and attach them as children of scoreboardContainer
            // (so they follow the scorebug's position/scale automatically).
            CreateStatPopups(uiFont);

            // Insert stat popups BEFORE the flex children so they render behind the scorebug
            // (slide-from-behind look). Index 0 = first child = rendered first = back layer.
            scoreboardContainer.Insert(0, blueStatPopup);
            scoreboardContainer.Insert(0, redStatPopup);

            // Create goal and win overlays. They're children of scoreboardContainer and
            // added at the END so they render ON TOP of the scorebug's flex children
            // (covering the scorebug during the animation).
            CreateGoalOverlay(uiFont);
            CreateWinOverlay(uiFont);

            // League logo anchored to the scorebug's league-logo section. Added BEFORE the
            // goal/win overlays so animations cover it.
            AddLeagueLogo(root, leagueLogoTexture);
            scoreboardContainer.Add(leagueLogo);

            scoreboardContainer.Add(goalOverlay);
            scoreboardContainer.Add(winOverlay);

            root.Add(scoreboardContainer);
            DebugLog("[CustomScoreboard] Added scoreboardContainer to root, root children=" + root.childCount);
            
            DebugLog("[CustomScoreboard] UI created");
            
            // Test: Set initial values to verify UI is working
            DebugLog("[CustomScoreboard] Setting test shot values: Blue=0, Red=0");
            SetBlueShots(0);
            SetRedShots(0);
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in CreateCustomScoreboard: {ex.Message}");
            }
        }

        private void CreateBlueSection(UnityEngine.Font uiFont, Color blueTeamColor, Texture2D blueLogoTexture)
        {
            Color blueTextColor = ParseHexColor(config.blueTeamTextColorHex, Color.white);
            
            blueSection = new VisualElement();
            blueSection.style.width = 280;
            blueSection.style.height = 40;
            // Apply global opacity to background color
            Color blueColorWithOpacity = blueTeamColor;
            blueColorWithOpacity.a = config.scoreboardOpacity;
            blueSection.style.backgroundColor = blueColorWithOpacity;
            blueSection.style.flexDirection = FlexDirection.Row;
            blueSection.style.alignItems = Align.Center;
            blueSection.style.position = Position.Relative; // Allow absolute positioned children
            blueSection.style.overflow = Overflow.Hidden; // Clip logos at edges
            blueSection.style.justifyContent = Justify.SpaceBetween;
            blueSection.style.paddingLeft = 10;
            blueSection.style.paddingRight = 10;
            blueSection.pickingMode = PickingMode.Ignore;
            // Add border with opacity
            Color blueBorderColor = ParseHexColor(config.blueBorderColorHex, new Color(0.12f, 0.23f, 0.54f, 1f));
            blueBorderColor.a = config.scoreboardOpacity;
            blueSection.style.borderTopWidth = config.borderWidth;
            blueSection.style.borderBottomWidth = config.borderWidth;
            blueSection.style.borderLeftWidth = config.borderWidth;
            blueSection.style.borderTopColor = blueBorderColor;
            blueSection.style.borderBottomColor = blueBorderColor;
            blueSection.style.borderLeftColor = blueBorderColor;

            // Add gradient overlay for blue section (if enabled)
            if (config.enableGradients)
            {
                AddGradientOverlay(blueSection, config.blueGradientLeftColorHex, config.blueGradientRightColorHex, 
                    new Color(0.15f, 0.35f, 0.75f), new Color(0.12f, 0.23f, 0.54f));
            }

            // Blue team logo
            blueTeamLogo = new VisualElement();
            blueTeamLogo.style.position = Position.Absolute;
            blueTeamLogo.style.width = config.blueLogoWidth;
            blueTeamLogo.style.height = config.blueLogoHeight;
            blueTeamLogo.style.left = config.blueLogoOffsetX;
            blueTeamLogo.style.top = config.blueLogoOffsetY;
            if (blueLogoTexture != null)
            {
                blueTeamLogo.style.backgroundImage = new StyleBackground(blueLogoTexture);
                #pragma warning disable CS0618
                blueTeamLogo.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                #pragma warning restore CS0618
                blueTeamLogo.style.backgroundColor = new Color(0, 0, 0, 0); // Transparent
            }
            else
            {
                blueTeamLogo.style.backgroundColor = new Color(1f, 1f, 1f, 0.2f); // Semi-transparent fallback
            }
            blueSection.Add(blueTeamLogo);

            // Team name container
            VisualElement blueNameContainer = new VisualElement();
            blueNameContainer.style.flexDirection = FlexDirection.Column;
            blueNameContainer.style.position = Position.Relative;
            blueNameContainer.style.justifyContent = Justify.Center;
            blueNameContainer.style.flexGrow = 1f;

            blueNameLabel = new Label(config.blueTeamName);
            blueNameLabel.style.fontSize = 20;
            blueNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            blueNameLabel.style.color = blueTextColor;
            blueNameLabel.style.unityFont = uiFont;
            blueNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            blueNameLabel.style.position = Position.Relative;
            blueNameLabel.style.paddingLeft = 4;
            blueNameContainer.Add(blueNameLabel);
            blueSection.Add(blueNameContainer);

            // Score container
            VisualElement blueScoreContainer = new VisualElement();
            blueScoreContainer.style.justifyContent = Justify.Center;
            blueScoreContainer.style.alignItems = Align.Center;
            blueScoreContainer.style.flexDirection = FlexDirection.Row;
            blueScoreContainer.style.position = Position.Relative;

            blueScoreLabel = new Label("0");
            blueScoreLabel.style.fontSize = 32;
            blueScoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            blueScoreLabel.style.color = blueTextColor;
            blueScoreLabel.style.unityFont = uiFont;
            blueScoreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            blueScoreLabel.style.paddingLeft = 10;
            blueScoreLabel.style.paddingRight = 10;
            blueScoreContainer.Add(blueScoreLabel);
            blueSection.Add(blueScoreContainer);

            // Shot container
            VisualElement blueShotContainer = new VisualElement();
            blueShotContainer.style.justifyContent = Justify.Center;
            blueShotContainer.style.alignItems = Align.Center;
            blueShotContainer.style.flexDirection = FlexDirection.Column;
            blueShotContainer.style.paddingRight = 4;

            blueShotLabel = new Label("0");
            blueShotLabel.style.fontSize = 18;
            blueShotLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            blueShotLabel.style.color = blueTextColor;
            blueShotLabel.style.unityFont = uiFont;
            blueShotLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            blueShotContainer.Add(blueShotLabel);

            Label blueShotsTextLabel = new Label("SHOTS");
            blueShotsTextLabel.style.fontSize = 10;
            blueShotsTextLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            blueShotsTextLabel.style.color = new Color(blueTextColor.r, blueTextColor.g, blueTextColor.b, 0.6f);
            blueShotsTextLabel.style.unityFont = uiFont;
            blueShotsTextLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            blueShotContainer.Add(blueShotsTextLabel);

            blueSection.Add(blueShotContainer);
            scoreboardContainer.Add(blueSection);
        }

        private void CreateRedSection(UnityEngine.Font uiFont, Color redTeamColor, Texture2D redLogoTexture)
        {
            Color redTextColor = ParseHexColor(config.redTeamTextColorHex, Color.white);
            
            redSection = new VisualElement();
            redSection.style.width = 280;
            redSection.style.height = 40;
            // Apply global opacity to background color
            Color redColorWithOpacity = redTeamColor;
            redColorWithOpacity.a = config.scoreboardOpacity;
            redSection.style.backgroundColor = redColorWithOpacity;
            redSection.style.flexDirection = FlexDirection.Row;
            redSection.style.alignItems = Align.Center;
            redSection.style.position = Position.Relative;
            redSection.style.overflow = Overflow.Hidden;
            redSection.style.justifyContent = Justify.SpaceBetween;
            redSection.style.paddingLeft = 10;
            redSection.style.paddingRight = 10;
            redSection.pickingMode = PickingMode.Ignore;
            // Add border with opacity
            Color redBorderColor = ParseHexColor(config.redBorderColorHex, new Color(0.60f, 0.11f, 0.11f, 1f));
            redBorderColor.a = config.scoreboardOpacity;
            redSection.style.borderTopWidth = config.borderWidth;
            redSection.style.borderBottomWidth = config.borderWidth;
            redSection.style.borderRightWidth = config.borderWidth;
            redSection.style.borderTopColor = redBorderColor;
            redSection.style.borderBottomColor = redBorderColor;
            redSection.style.borderRightColor = redBorderColor;

            // Add gradient overlay for red section (if enabled)
            if (config.enableGradients)
            {
                AddGradientOverlay(redSection, config.redGradientLeftColorHex, config.redGradientRightColorHex,
                    new Color(0.85f, 0.15f, 0.15f), new Color(0.6f, 0.11f, 0.11f));
            }

            // Red team logo
            redTeamLogo = new VisualElement();
            redTeamLogo.style.position = Position.Absolute;
            redTeamLogo.style.width = config.redLogoWidth;
            redTeamLogo.style.height = config.redLogoHeight;
            redTeamLogo.style.left = config.redLogoOffsetX;
            redTeamLogo.style.top = config.redLogoOffsetY;
            if (redLogoTexture != null)
            {
                redTeamLogo.style.backgroundImage = new StyleBackground(redLogoTexture);
                #pragma warning disable CS0618
                redTeamLogo.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                #pragma warning restore CS0618
                redTeamLogo.style.backgroundColor = new Color(0, 0, 0, 0);
            }
            else
            {
                redTeamLogo.style.backgroundColor = new Color(1f, 1f, 1f, 0.2f);
            }
            redSection.Add(redTeamLogo);

            // Team name container
            VisualElement redNameContainer = new VisualElement();
            redNameContainer.style.flexDirection = FlexDirection.Column;
            redNameContainer.style.position = Position.Relative;
            redNameContainer.style.justifyContent = Justify.Center;
            redNameContainer.style.flexGrow = 1f;

            redNameLabel = new Label(config.redTeamName);
            redNameLabel.style.fontSize = 20;
            redNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            redNameLabel.style.color = redTextColor;
            redNameLabel.style.unityFont = uiFont;
            redNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            redNameLabel.style.position = Position.Relative;
            redNameLabel.style.paddingLeft = 4;
            redNameContainer.Add(redNameLabel);
            redSection.Add(redNameContainer);

            // Score container
            VisualElement redScoreContainer = new VisualElement();
            redScoreContainer.style.justifyContent = Justify.Center;
            redScoreContainer.style.alignItems = Align.Center;
            redScoreContainer.style.flexDirection = FlexDirection.Row;
            redScoreContainer.style.position = Position.Relative;

            redScoreLabel = new Label("0");
            redScoreLabel.style.fontSize = 32;
            redScoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            redScoreLabel.style.color = redTextColor;
            redScoreLabel.style.unityFont = uiFont;
            redScoreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            redScoreLabel.style.paddingLeft = 10;
            redScoreLabel.style.paddingRight = 10;
            redScoreContainer.Add(redScoreLabel);
            redSection.Add(redScoreContainer);

            // Shot container
            VisualElement redShotContainer = new VisualElement();
            redShotContainer.style.justifyContent = Justify.Center;
            redShotContainer.style.alignItems = Align.Center;
            redShotContainer.style.flexDirection = FlexDirection.Column;
            redShotContainer.style.paddingRight = 4;

            redShotLabel = new Label("0");
            redShotLabel.style.fontSize = 18;
            redShotLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            redShotLabel.style.color = redTextColor;
            redShotLabel.style.unityFont = uiFont;
            redShotLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            redShotContainer.Add(redShotLabel);

            Label redShotsTextLabel = new Label("SHOTS");
            redShotsTextLabel.style.fontSize = 10;
            redShotsTextLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            redShotsTextLabel.style.color = new Color(redTextColor.r, redTextColor.g, redTextColor.b, 0.6f);
            redShotsTextLabel.style.unityFont = uiFont;
            redShotsTextLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            redShotContainer.Add(redShotsTextLabel);

            redSection.Add(redShotContainer);
            scoreboardContainer.Add(redSection);
        }

        private void AddGradientOverlay(VisualElement section, string leftColorHex, string rightColorHex, Color defaultLeft, Color defaultRight)
        {
            Color gradLeft = ParseHexColor(leftColorHex, defaultLeft);
            Color gradRight = ParseHexColor(rightColorHex, defaultRight);
            
            // Create 280 strips (1 per pixel for 280px width) for smoothest possible gradient
            int stripCount = 280;
            for (int i = 0; i < stripCount; i++)
            {
                VisualElement gradStrip = new VisualElement();
                gradStrip.style.position = Position.Absolute;
                gradStrip.style.left = i;
                gradStrip.style.top = 0;
                gradStrip.style.width = 2; // 2 pixels wide for overlap
                gradStrip.style.height = Length.Percent(100);
                
                float t = i / (float)(stripCount - 1);
                Color stripColor = Color.Lerp(gradLeft, gradRight, t);
                stripColor.a = 1f;
                gradStrip.style.backgroundColor = stripColor;
                section.Add(gradStrip);
            }
        }

        private void CreateLeagueLogoSection()
        {
            leagueLogoSection = new VisualElement();
            leagueLogoSection.style.width = config.leagueLogoSectionWidth;
            leagueLogoSection.style.height = 40;
            // Apply opacity to league logo section
            Color leagueLogoColor = ParseHexColor(config.leagueLogoSectionColorHex, new Color(0.15f, 0.15f, 0.15f, 1f));
            leagueLogoColor.a = config.scoreboardOpacity;
            leagueLogoSection.style.backgroundColor = leagueLogoColor;
            leagueLogoSection.style.borderTopWidth = config.borderWidth;
            leagueLogoSection.style.borderBottomWidth = config.borderWidth;
            Color leagueBorderColor = ParseHexColor(config.leagueLogoSectionBorderColorHex, new Color(0.1f, 0.1f, 0.1f, 1f));
            leagueBorderColor.a = config.scoreboardOpacity;
            leagueLogoSection.style.borderTopColor = leagueBorderColor;
            leagueLogoSection.style.borderBottomColor = leagueBorderColor;
            leagueLogoSection.style.overflow = Overflow.Visible;
            leagueLogoSection.style.position = Position.Relative;
            leagueLogoSection.style.unityOverflowClipBox = OverflowClipBox.ContentBox;
            leagueLogoSection.pickingMode = PickingMode.Ignore;
            
            scoreboardContainer.Add(leagueLogoSection);
        }

        private void CreateTimeSection(UnityEngine.Font uiFont)
        {
            VisualElement timeSection = new VisualElement();
            // Width matches the sum of children (periodBox + timeBox = 200 by default) so
            // the section doesn't shrink its kids and the scorebug's total visual width
            // matches scoreboardContainer.width (780). Otherwise popups end up overhanging.
            timeSection.style.width = config.periodBoxWidth + config.timeBoxWidth;
            timeSection.style.height = 40;
            timeSection.style.flexDirection = FlexDirection.Row;
            timeSection.style.flexGrow = 0f;
            timeSection.style.flexShrink = 0f;
            timeSection.pickingMode = PickingMode.Ignore;

            // Black box for period
            VisualElement periodBox = new VisualElement();
            periodBox.style.width = config.periodBoxWidth;
            periodBox.style.height = 40;
            Color periodBoxColor = ParseHexColor(config.periodBoxColorHex, new Color(0.1f, 0.1f, 0.1f, 1f));
            periodBoxColor.a = config.scoreboardOpacity;
            periodBox.style.backgroundColor = periodBoxColor;
            periodBox.style.alignItems = Align.Center;
            periodBox.style.justifyContent = Justify.Center;
            periodBox.style.borderTopWidth = config.borderWidth;
            periodBox.style.borderBottomWidth = config.borderWidth;
            Color periodBorderColor = ParseHexColor(config.periodBoxBorderColorHex, new Color(0.05f, 0.05f, 0.05f, 1f));
            periodBorderColor.a = config.scoreboardOpacity;
            periodBox.style.borderTopColor = periodBorderColor;
            periodBox.style.borderBottomColor = periodBorderColor;
            periodBox.pickingMode = PickingMode.Ignore;

            periodLabel = new Label("1ST");
            periodLabel.style.fontSize = 14;
            periodLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            periodLabel.style.color = ParseHexColor(config.periodTextColorHex, Color.white);
            periodLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            periodLabel.style.unityFont = uiFont;
            periodLabel.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            periodLabel.style.maxWidth = config.periodBoxWidth - 6;
            periodLabel.style.whiteSpace = WhiteSpace.Normal;
            periodLabel.style.flexShrink = 1;
            periodBox.Add(periodLabel);
            
            timeSection.Add(periodBox);

            // Grey box for time
            VisualElement timeBox = new VisualElement();
            timeBox.style.width = config.timeBoxWidth;
            timeBox.style.height = 40;
            Color timeBoxColor = ParseHexColor(config.timeBoxColorHex, new Color(0.3f, 0.3f, 0.3f, 1f));
            timeBoxColor.a = config.scoreboardOpacity;
            timeBox.style.backgroundColor = timeBoxColor;
            timeBox.style.alignItems = Align.Center;
            timeBox.style.justifyContent = Justify.Center;
            timeBox.style.borderTopWidth = config.borderWidth;
            timeBox.style.borderBottomWidth = config.borderWidth;
            timeBox.style.borderRightWidth = config.borderWidth;
            Color timeBorderColor = ParseHexColor(config.timeBoxBorderColorHex, new Color(0.2f, 0.2f, 0.2f, 1f));
            timeBorderColor.a = config.scoreboardOpacity;
            timeBox.style.borderTopColor = timeBorderColor;
            timeBox.style.borderBottomColor = timeBorderColor;
            timeBox.style.borderRightColor = timeBorderColor;
            timeBox.pickingMode = PickingMode.Ignore;

            timeLabel = new Label("5:00");
            timeLabel.style.fontSize = 24;
            timeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            timeLabel.style.color = ParseHexColor(config.timeTextColorHex, Color.white);
            timeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            timeLabel.style.unityFont = uiFont;
            timeBox.Add(timeLabel);
            
            timeSection.Add(timeBox);
            scoreboardContainer.Add(timeSection);
        }

        private void CreateStatPopups(UnityEngine.Font uiFont)
        {
            // Positioned in scoreboardContainer-local coordinates (anchored to scorebug).
            // Blue shots area lives at the right of blueSection (x ~= 220-280).
            // Red shots area lives at the right of redSection (x ~= 520-580).
            // Stat popups are 80x20 by default; we center them under their team's shots.
            blueStatPopup = new VisualElement();
            blueStatPopup.style.position = Position.Absolute;
            blueStatPopup.style.width = config.statPopupWidth;
            blueStatPopup.style.height = config.statPopupHeight;
            blueStatPopup.style.left = ScorebugAnchor.BlueStatPopupLeft;
            blueStatPopup.style.top = 0; // hidden behind scorebug top; ShowStatPopup slides it down
            Color blueStatPopupColor = GetBlueTeamColor();
            blueStatPopupColor.a = 0.95f;
            blueStatPopup.style.backgroundColor = blueStatPopupColor;
            blueStatPopup.style.borderBottomLeftRadius = 4;
            blueStatPopup.style.borderBottomRightRadius = 4;
            blueStatPopup.style.alignItems = Align.Center;
            blueStatPopup.style.justifyContent = Justify.Center;
            blueStatPopup.style.display = DisplayStyle.None;
            blueStatPopup.style.overflow = Overflow.Hidden;
            blueStatPopup.pickingMode = PickingMode.Ignore;

            blueStatLabel = new Label("100 MPH");
            blueStatLabel.style.fontSize = 10;
            blueStatLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            blueStatLabel.style.color = Color.white;
            blueStatLabel.style.unityFont = uiFont;
            blueStatLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            blueStatPopup.Add(blueStatLabel);

            redStatPopup = new VisualElement();
            redStatPopup.style.position = Position.Absolute;
            redStatPopup.style.width = config.statPopupWidth;
            redStatPopup.style.height = config.statPopupHeight;
            redStatPopup.style.left = ScorebugAnchor.RedStatPopupLeft;
            redStatPopup.style.top = 0;
            Color redStatPopupColor = GetRedTeamColor();
            redStatPopupColor.a = 0.95f;
            redStatPopup.style.backgroundColor = redStatPopupColor;
            redStatPopup.style.borderBottomLeftRadius = 4;
            redStatPopup.style.borderBottomRightRadius = 4;
            redStatPopup.style.alignItems = Align.Center;
            redStatPopup.style.justifyContent = Justify.Center;
            redStatPopup.style.display = DisplayStyle.None;
            redStatPopup.style.overflow = Overflow.Hidden;
            redStatPopup.pickingMode = PickingMode.Ignore;

            redStatLabel = new Label("100 MPH");
            redStatLabel.style.fontSize = 10;
            redStatLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            redStatLabel.style.color = Color.white;
            redStatLabel.style.unityFont = uiFont;
            redStatLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            redStatPopup.Add(redStatLabel);
        }

        private void CreateGoalOverlay(UnityEngine.Font uiFont)
        {
            // Goal overlay is a child of scoreboardContainer (added later, in CreateCustomScoreboard).
            // Covers the full scorebug; width animates 0 → ScorebugAnchor.Width during the wipe.
            goalOverlay = new VisualElement();
            goalOverlay.style.position = Position.Absolute;
            goalOverlay.style.width = 0; // animated by ShowGoalAnimation
            goalOverlay.style.height = ScorebugAnchor.GoalOverlayHeight;
            goalOverlay.style.alignItems = Align.Center;
            goalOverlay.style.justifyContent = Justify.Center;
            goalOverlay.style.display = DisplayStyle.None;
            goalOverlay.style.overflow = Overflow.Hidden;
            goalOverlay.style.left = ScorebugAnchor.GoalOverlayLeft;
            goalOverlay.style.top = ScorebugAnchor.GoalOverlayTop;
            goalOverlay.pickingMode = PickingMode.Ignore;
            
            goalOverlayLabel = new Label("GOAL!");
            goalOverlayLabel.style.fontSize = 48;
            goalOverlayLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            goalOverlayLabel.style.color = Color.white;
            goalOverlayLabel.style.unityFont = uiFont;
            goalOverlayLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            goalOverlayLabel.style.width = ScorebugAnchor.GoalOverlayWidth;
            goalOverlayLabel.style.height = ScorebugAnchor.GoalOverlayHeight;
            goalOverlayLabel.style.textShadow = new TextShadow { offset = new Vector2(2, 2), blurRadius = 4, color = new Color(0, 0, 0, 0.8f) };
            goalOverlay.Add(goalOverlayLabel);
        }

        // Returns the x position of the center of the league logo section, in scoreboardContainer-local
        // pixels. Computed from config so it stays stable across scale/translate changes — using the
        // dynamic layout (GeometryChangedEvent) misfired during transform changes and made the logo
        // jump positions. blueSection.width is hardcoded to 280 (see CreateBlueSection).
        private float LeagueLogoSectionCenterX =>
            280f + (config != null ? config.leagueLogoSectionWidth / 2f : 10f);

        private void AddLeagueLogo(VisualElement root, Texture2D leagueLogoTexture)
        {
            // League logo is anchored to scoreboardContainer (sibling of the team sections).
            // Position is purely a function of config (blueSection width + half of league logo
            // section width) so it doesn't depend on Unity's resolved-layout system.
            leagueLogo = new VisualElement();
            leagueLogo.style.position = Position.Absolute;
            leagueLogo.style.width = config.leagueLogoWidth;
            leagueLogo.style.height = config.leagueLogoHeight;
            leagueLogo.style.left = LeagueLogoSectionCenterX - (config.leagueLogoWidth / 2f);
            leagueLogo.style.top = (ScorebugAnchor.Height - config.leagueLogoHeight) / 2f;
            leagueLogo.pickingMode = PickingMode.Ignore;
            if (leagueLogoTexture != null)
            {
                leagueLogo.style.backgroundImage = new StyleBackground(leagueLogoTexture);
            }
            else
            {
                leagueLogo.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            }
        }

        private void CreateWinOverlay(UnityEngine.Font uiFont)
        {
            // Win overlay is a child of scoreboardContainer. Same anchoring as goal overlay.
            winOverlay = new VisualElement();
            winOverlay.style.position = Position.Absolute;
            winOverlay.style.width = 0; // animated by ShowWinAnimation
            winOverlay.style.height = ScorebugAnchor.GoalOverlayHeight;
            winOverlay.style.alignItems = Align.Center;
            winOverlay.style.justifyContent = Justify.Center;
            winOverlay.style.overflow = Overflow.Hidden;
            winOverlay.style.display = DisplayStyle.None;
            winOverlay.style.left = ScorebugAnchor.GoalOverlayLeft;
            winOverlay.style.top = ScorebugAnchor.GoalOverlayTop;
            winOverlay.pickingMode = PickingMode.Ignore;
            
            winOverlayLabel = new Label("");
            winOverlayLabel.style.fontSize = 48;
            winOverlayLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            winOverlayLabel.style.color = Color.white;
            winOverlayLabel.style.unityFont = uiFont;
            winOverlayLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            winOverlayLabel.style.width = ScorebugAnchor.GoalOverlayWidth;
            winOverlayLabel.style.height = ScorebugAnchor.GoalOverlayHeight;
            winOverlayLabel.style.textShadow = new TextShadow { offset = new Vector2(2, 2), blurRadius = 4, color = new Color(0, 0, 0, 0.8f) };
            winOverlay.Add(winOverlayLabel);
        }

        private CustomScoreboard.ScoreboardConfig LoadScoreboardConfig()
        {
            try
            {
                string configPath = CustomScoreboard.ScoreboardPaths.ConfigPath;
                
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    return JsonUtility.FromJson<CustomScoreboard.ScoreboardConfig>(json) ?? new CustomScoreboard.ScoreboardConfig();
                }
                else
                {
                    // Create default config
                    var defaultConfig = new CustomScoreboard.ScoreboardConfig();
                    SaveScoreboardConfig(defaultConfig);
                    return defaultConfig;
                }
            }
            catch (Exception e)
            {
                DebugWarning($"[CustomScoreboard] Failed to load config: {e}");
                return new CustomScoreboard.ScoreboardConfig();
            }
        }

        private void SaveScoreboardConfig(CustomScoreboard.ScoreboardConfig config)
        {
            try
            {
                Directory.CreateDirectory(CustomScoreboard.ScoreboardPaths.ConfigDir);
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(CustomScoreboard.ScoreboardPaths.ConfigPath, json);
                DebugLog("[CustomScoreboard] Config saved");
            }
            catch (Exception e)
            {
                DebugWarning($"[CustomScoreboard] Failed to save config: " + e);
            }
        }
    }
}
