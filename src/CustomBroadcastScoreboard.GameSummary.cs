using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

namespace CustomScoreboard.UI
{
    public partial class CustomBroadcastScoreboard
    {
        private static readonly Color SummaryButtonBg = new Color(57f / 255f, 57f / 255f, 57f / 255f, 1f);
        private static readonly Color SummaryPanelBg = new Color(0.08f, 0.08f, 0.08f, 0.97f);
        private static readonly Color SummaryDivider = new Color(0.25f, 0.25f, 0.25f);
        private static readonly Color StarGold = new Color(1f, 0.82f, 0.10f);

        private void ShowEndOfGameSummary()
        {
            DebugLog("[CustomScoreboard] ===== ShowEndOfGameSummary START =====");

            if (config == null || !config.enablePopups)
            {
                DebugWarning($"[CustomScoreboard] Cannot show summary - config={(config == null ? "NULL" : "exists")}, enablePopups={(config == null ? "N/A" : config.enablePopups.ToString())}");
                return;
            }

            try
            {
                if (scoreboardContainer == null)
                {
                    DebugWarning("[CustomScoreboard] scoreboardContainer is NULL - cannot display summary");
                    return;
                }

                VisualElement gameSummary = new VisualElement();
                gameSummary.name = "gameSummaryRoot";
                gameSummary.style.position = Position.Absolute;
                gameSummary.style.width = ScorebugAnchor.CenteredPopupWidth;
                gameSummary.style.backgroundColor = SummaryPanelBg;
                gameSummary.style.borderTopWidth = 2;
                gameSummary.style.borderBottomWidth = 2;
                gameSummary.style.borderLeftWidth = 2;
                gameSummary.style.borderRightWidth = 2;
                gameSummary.style.borderTopColor = SummaryDivider;
                gameSummary.style.borderBottomColor = SummaryDivider;
                gameSummary.style.borderLeftColor = SummaryDivider;
                gameSummary.style.borderRightColor = SummaryDivider;
                gameSummary.style.paddingTop = 14;
                gameSummary.style.paddingBottom = 18;
                gameSummary.style.paddingLeft = 24;
                gameSummary.style.paddingRight = 24;
                gameSummary.style.flexDirection = FlexDirection.Column;

                // Anchored to scoreboardContainer; coordinates are scoreboard-local.
                float startY = 0f;
                float finalY = ScorebugAnchor.GameSummarySlideTo;

                gameSummary.style.top = startY;
                gameSummary.style.left = ScorebugAnchor.CenteredPopupLeft;

                UnityEngine.Font uiFont = GetUIFont();

                gameSummary.Add(BuildHeaderRow(gameSummary, uiFont));
                gameSummary.Add(BuildFinalScoreRow(uiFont));
                gameSummary.Add(BuildDivider(8, 14));

                bool hasStars = !string.IsNullOrEmpty(firstStar) || !string.IsNullOrEmpty(secondStar) || !string.IsNullOrEmpty(thirdStar);
                if (hasStars)
                {
                    gameSummary.Add(BuildThreeStarsSection(uiFont));
                    gameSummary.Add(BuildDivider(14, 14));
                }

                gameSummary.Add(BuildTeamStatsRow(uiFont));

                // Insert behind scorebug flex children for the slide-from-behind look.
                scoreboardContainer.Insert(0, gameSummary);

                DOTween.Sequence().SetTarget(gameSummary)
                    .Append(DOTween.To(() => gameSummary.style.top.value.value,
                        y => gameSummary.style.top = y, finalY, 0.8f).SetEase(Ease.OutCubic))
                    .AppendInterval(10f)
                    .Append(DOTween.To(() => gameSummary.style.top.value.value,
                        y => gameSummary.style.top = y, startY, 0.6f).SetEase(Ease.InCubic))
                    .OnComplete(() => { if (gameSummary.parent != null) gameSummary.RemoveFromHierarchy(); })
                    .OnKill(() => { if (gameSummary.parent != null) gameSummary.RemoveFromHierarchy(); });

                DebugLog("[CustomScoreboard] End-of-game summary displayed");
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error creating end-of-game summary: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private VisualElement BuildHeaderRow(VisualElement gameSummary, UnityEngine.Font uiFont)
        {
            VisualElement headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 6;

            Label title = new Label("FINAL");
            title.style.fontSize = 22;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.letterSpacing = 4;
            if (uiFont != null) title.style.unityFont = uiFont;
            headerRow.Add(title);

            Button closeBtn = new Button(() =>
            {
                DOTween.Kill(gameSummary);
                if (gameSummary.parent != null) gameSummary.RemoveFromHierarchy();
            });
            closeBtn.text = "CLOSE";
            closeBtn.style.width = 70;
            closeBtn.style.height = 28;
            closeBtn.style.fontSize = 11;
            closeBtn.style.color = Color.white;
            closeBtn.style.backgroundColor = SummaryButtonBg;
            closeBtn.style.paddingTop = 0;
            closeBtn.style.paddingBottom = 0;
            closeBtn.style.paddingLeft = 0;
            closeBtn.style.paddingRight = 0;
            closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            closeBtn.style.whiteSpace = WhiteSpace.NoWrap;
            closeBtn.style.borderTopWidth = 0;
            closeBtn.style.borderBottomWidth = 0;
            closeBtn.style.borderLeftWidth = 0;
            closeBtn.style.borderRightWidth = 0;
            closeBtn.pickingMode = PickingMode.Position;
            if (uiFont != null) closeBtn.style.unityFont = uiFont;
            closeBtn.RegisterCallback<PointerEnterEvent>(_ =>
            {
                closeBtn.style.backgroundColor = Color.white;
                closeBtn.style.color = Color.black;
            });
            closeBtn.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                closeBtn.style.backgroundColor = SummaryButtonBg;
                closeBtn.style.color = Color.white;
            });
            headerRow.Add(closeBtn);

            return headerRow;
        }

        private VisualElement BuildFinalScoreRow(UnityEngine.Font uiFont)
        {
            VisualElement scoreRow = new VisualElement();
            scoreRow.style.flexDirection = FlexDirection.Row;
            scoreRow.style.justifyContent = Justify.Center;
            scoreRow.style.alignItems = Align.Center;
            scoreRow.style.marginTop = 4;
            scoreRow.style.marginBottom = 4;

            Color blueColor = GetBlueTeamColor();
            Color redColor = GetRedTeamColor();
            int blueScore = lastBlueScore;
            int redScore = lastRedScore;
            bool blueWon = blueScore > redScore;
            bool redWon = redScore > blueScore;

            Label blueName = new Label(config.blueTeamName.ToUpperInvariant());
            blueName.style.fontSize = 22;
            blueName.style.unityFontStyleAndWeight = FontStyle.Bold;
            blueName.style.color = blueColor;
            blueName.style.unityTextAlign = TextAnchor.MiddleRight;
            blueName.style.flexGrow = 1;
            blueName.style.opacity = redWon ? 0.55f : 1f;
            if (uiFont != null) blueName.style.unityFont = uiFont;
            scoreRow.Add(blueName);

            Label blueScoreLabel = new Label(blueScore.ToString());
            blueScoreLabel.style.fontSize = 38;
            blueScoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            blueScoreLabel.style.color = Color.white;
            blueScoreLabel.style.marginLeft = 18;
            blueScoreLabel.style.marginRight = 12;
            blueScoreLabel.style.minWidth = 50;
            blueScoreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (uiFont != null) blueScoreLabel.style.unityFont = uiFont;
            scoreRow.Add(blueScoreLabel);

            Label dash = new Label("–");
            dash.style.fontSize = 32;
            dash.style.color = new Color(0.6f, 0.6f, 0.6f);
            dash.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (uiFont != null) dash.style.unityFont = uiFont;
            scoreRow.Add(dash);

            Label redScoreLabel = new Label(redScore.ToString());
            redScoreLabel.style.fontSize = 38;
            redScoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            redScoreLabel.style.color = Color.white;
            redScoreLabel.style.marginLeft = 12;
            redScoreLabel.style.marginRight = 18;
            redScoreLabel.style.minWidth = 50;
            redScoreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (uiFont != null) redScoreLabel.style.unityFont = uiFont;
            scoreRow.Add(redScoreLabel);

            Label redName = new Label(config.redTeamName.ToUpperInvariant());
            redName.style.fontSize = 22;
            redName.style.unityFontStyleAndWeight = FontStyle.Bold;
            redName.style.color = redColor;
            redName.style.unityTextAlign = TextAnchor.MiddleLeft;
            redName.style.flexGrow = 1;
            redName.style.opacity = blueWon ? 0.55f : 1f;
            if (uiFont != null) redName.style.unityFont = uiFont;
            scoreRow.Add(redName);

            return scoreRow;
        }

        private VisualElement BuildDivider(float marginTop, float marginBottom)
        {
            VisualElement div = new VisualElement();
            div.style.height = 1;
            div.style.backgroundColor = SummaryDivider;
            div.style.marginTop = marginTop;
            div.style.marginBottom = marginBottom;
            return div;
        }

        private VisualElement BuildThreeStarsSection(UnityEngine.Font uiFont)
        {
            VisualElement starsBox = new VisualElement();
            starsBox.style.flexDirection = FlexDirection.Column;
            starsBox.style.alignItems = Align.Center;

            Label starsTitle = new Label("★ THREE STARS ★");
            starsTitle.style.fontSize = 13;
            starsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            starsTitle.style.color = StarGold;
            starsTitle.style.letterSpacing = 3;
            starsTitle.style.marginBottom = 8;
            if (uiFont != null) starsTitle.style.unityFont = uiFont;
            starsBox.Add(starsTitle);

            string[] labels = { "1ST", "2ND", "3RD" };
            string[] names = { firstStar, secondStar, thirdStar };
            for (int i = 0; i < 3; i++)
            {
                if (string.IsNullOrEmpty(names[i])) continue;

                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.Center;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 3;

                Label pos = new Label(labels[i]);
                pos.style.fontSize = 12;
                pos.style.unityFontStyleAndWeight = FontStyle.Bold;
                pos.style.color = StarGold;
                pos.style.minWidth = 38;
                pos.style.unityTextAlign = TextAnchor.MiddleRight;
                pos.style.marginRight = 10;
                if (uiFont != null) pos.style.unityFont = uiFont;
                row.Add(pos);

                Label name = new Label(names[i]);
                name.style.fontSize = 14;
                name.style.color = Color.white;
                if (uiFont != null) name.style.unityFont = uiFont;
                row.Add(name);

                starsBox.Add(row);
            }

            return starsBox;
        }

        private VisualElement BuildTeamStatsRow(UnityEngine.Font uiFont)
        {
            VisualElement statsRow = new VisualElement();
            statsRow.style.flexDirection = FlexDirection.Row;
            statsRow.style.justifyContent = Justify.SpaceAround;

            statsRow.Add(CreateTeamStatsColumn(PlayerTeam.Blue, uiFont));

            VisualElement divider = new VisualElement();
            divider.style.width = 1;
            divider.style.backgroundColor = SummaryDivider;
            divider.style.marginLeft = 10;
            divider.style.marginRight = 10;
            statsRow.Add(divider);

            statsRow.Add(CreateTeamStatsColumn(PlayerTeam.Red, uiFont));

            return statsRow;
        }

        private VisualElement CreateTeamStatsColumn(PlayerTeam team, UnityEngine.Font uiFont)
        {
            VisualElement column = new VisualElement();
            column.style.flexDirection = FlexDirection.Column;
            column.style.alignItems = Align.Stretch;
            column.style.flexGrow = 1;
            column.style.flexBasis = 0;
            column.style.paddingLeft = 6;
            column.style.paddingRight = 6;

            string teamName = team == PlayerTeam.Blue ? config.blueTeamName : config.redTeamName;
            Color teamColor = team == PlayerTeam.Blue ? GetBlueTeamColor() : GetRedTeamColor();

            Label nameLabel = new Label(teamName.ToUpperInvariant());
            nameLabel.style.fontSize = 15;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.color = teamColor;
            nameLabel.style.letterSpacing = 2;
            nameLabel.style.marginBottom = 8;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (uiFont != null) nameLabel.style.unityFont = uiFont;
            column.Add(nameLabel);

            int teamShots = team == PlayerTeam.Blue ? blueTeamShots : redTeamShots;
            int teamGoals = team == PlayerTeam.Blue ? lastBlueScore : lastRedScore;
            float shotPercent = teamShots > 0 ? (float)teamGoals / teamShots * 100f : 0f;

            column.Add(BuildStatRow("SHOTS", teamShots.ToString(), uiFont));
            column.Add(BuildStatRow("SHOT %", $"{shotPercent:0.0}%", uiFont));

            // Player rosters
            List<(string name, int number, int goals, int assists, int shots, ulong clientId, PlayerRole role, string steamId)> roster = CollectTeamRoster(team);

            if (roster.Count == 0)
            {
                return column;
            }

            var skaters = roster.Where(p => p.role == PlayerRole.Attacker).OrderByDescending(p => p.goals + p.assists).ThenByDescending(p => p.goals).ToList();
            var goalies = roster.Where(p => p.role == PlayerRole.Goalie).ToList();

            if (skaters.Count > 0)
            {
                column.Add(BuildSectionHeader("SKATERS", uiFont));
                foreach (var p in skaters)
                {
                    int points = p.goals + p.assists;
                    string stats = $"{p.goals}G {p.assists}A ({points}P) · {p.shots}S";
                    column.Add(BuildPlayerRow($"#{p.number}", p.name, stats, uiFont));
                }
            }

            if (goalies.Count > 0)
            {
                column.Add(BuildSectionHeader("GOALIES", uiFont));
                int opponentShots = team == PlayerTeam.Blue ? redTeamShots : blueTeamShots;
                int opponentGoals = team == PlayerTeam.Blue ? lastRedScore : lastBlueScore;
                foreach (var g in goalies)
                {
                    string statText = FormatGoalieLine(g.steamId, opponentShots, opponentGoals);
                    column.Add(BuildPlayerRow($"#{g.number}", g.name, statText, uiFont));
                }
            }

            return column;
        }

        private List<(string name, int number, int goals, int assists, int shots, ulong clientId, PlayerRole role, string steamId)> CollectTeamRoster(PlayerTeam team)
        {
            var roster = new List<(string, int, int, int, int, ulong, PlayerRole, string)>();

            if (MonoBehaviourSingleton<PlayerManager>.Instance != null)
            {
                var players = MonoBehaviourSingleton<PlayerManager>.Instance.GetPlayers(false);
                foreach (var player in players)
                {
                    if (player.Team != team) continue;

                    ulong clientId = player.OwnerClientId;
                    string steamId = player.SteamId.Value.ToString();
                    int shots = 0;
                    if (playerSOG.ContainsKey(steamId)) shots = playerSOG[steamId];
                    else if (playerShots.ContainsKey(clientId)) shots = playerShots[clientId];

                    string positionName = GetPlayerPositionName(player);
                    cachedPlayerStats[clientId] = (
                        player.Username.Value.ToString(),
                        player.Number.Value,
                        player.Team,
                        player.Role,
                        player.Goals.Value,
                        player.Assists.Value,
                        shots,
                        steamId,
                        positionName
                    );

                    roster.Add((
                        player.Username.Value.ToString(),
                        player.Number.Value,
                        player.Goals.Value,
                        player.Assists.Value,
                        shots,
                        clientId,
                        player.Role,
                        steamId
                    ));
                }
            }

            // Disconnected players from cache
            foreach (var kvp in cachedPlayerStats)
            {
                if (kvp.Value.team != team) continue;
                if (roster.Any(p => p.Item6 == kvp.Key)) continue;

                int cachedShots = kvp.Value.shots;
                if (!string.IsNullOrEmpty(kvp.Value.steamId) && playerSOG.ContainsKey(kvp.Value.steamId))
                    cachedShots = playerSOG[kvp.Value.steamId];

                roster.Add((
                    kvp.Value.name,
                    kvp.Value.number,
                    kvp.Value.goals,
                    kvp.Value.assists,
                    cachedShots,
                    kvp.Key,
                    kvp.Value.role,
                    kvp.Value.steamId
                ));
            }

            return roster;
        }

        private string FormatGoalieLine(string steamId, int opponentShots, int opponentGoals)
        {
            if (!string.IsNullOrEmpty(steamId) && goalieSaveStats.ContainsKey(steamId))
            {
                var (saves, shots) = goalieSaveStats[steamId];
                float pct = shots > 0 ? (float)saves / shots : 0f;
                return $"{saves}/{shots} SV · {pct.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (opponentShots > 0)
            {
                int saves = Math.Max(0, opponentShots - opponentGoals);
                float pct = (float)saves / opponentShots;
                return $"{saves}/{opponentShots} SV · {pct.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)}";
            }

            return "0/0 SV · .000";
        }

        private VisualElement BuildStatRow(string label, string value, UnityEngine.Font uiFont)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 2;

            Label lbl = new Label(label);
            lbl.style.fontSize = 11;
            lbl.style.color = new Color(0.62f, 0.62f, 0.62f);
            lbl.style.letterSpacing = 1;
            if (uiFont != null) lbl.style.unityFont = uiFont;
            row.Add(lbl);

            Label val = new Label(value);
            val.style.fontSize = 12;
            val.style.unityFontStyleAndWeight = FontStyle.Bold;
            val.style.color = Color.white;
            if (uiFont != null) val.style.unityFont = uiFont;
            row.Add(val);

            return row;
        }

        private VisualElement BuildSectionHeader(string title, UnityEngine.Font uiFont)
        {
            Label header = new Label(title);
            header.style.fontSize = 11;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.75f, 0.75f, 0.75f);
            header.style.letterSpacing = 2;
            header.style.marginTop = 10;
            header.style.marginBottom = 4;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = SummaryDivider;
            header.style.paddingBottom = 2;
            if (uiFont != null) header.style.unityFont = uiFont;
            return header;
        }

        private VisualElement BuildPlayerRow(string number, string name, string stats, UnityEngine.Font uiFont)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 1;

            VisualElement left = new VisualElement();
            left.style.flexDirection = FlexDirection.Row;
            left.style.alignItems = Align.Center;
            left.style.flexShrink = 1;
            left.style.overflow = Overflow.Hidden;

            Label num = new Label(number);
            num.style.fontSize = 11;
            num.style.color = new Color(0.6f, 0.6f, 0.6f);
            num.style.minWidth = 28;
            if (uiFont != null) num.style.unityFont = uiFont;
            left.Add(num);

            Label nm = new Label(name);
            nm.style.fontSize = 12;
            nm.style.color = new Color(0.95f, 0.95f, 0.95f);
            nm.style.whiteSpace = WhiteSpace.NoWrap;
            nm.style.overflow = Overflow.Hidden;
            nm.style.textOverflow = TextOverflow.Ellipsis;
            if (uiFont != null) nm.style.unityFont = uiFont;
            left.Add(nm);

            row.Add(left);

            Label statsLbl = new Label(stats);
            statsLbl.style.fontSize = 11;
            statsLbl.style.color = new Color(0.85f, 0.85f, 0.85f);
            statsLbl.style.marginLeft = 8;
            statsLbl.style.whiteSpace = WhiteSpace.NoWrap;
            if (uiFont != null) statsLbl.style.unityFont = uiFont;
            row.Add(statsLbl);

            return row;
        }
    }
}
