using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UITK = UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        // Professional row maker to match PoncePlayerInput format
        private UITK.VisualElement MakeSliderRow(string label, float start, float min, float max, out UITK.Slider slider, out UITK.TextField valueField, System.Action<float> onChange)
        {
            // CompAdjust slider row layout: fixed-width label column, then the
            // editable value field, then the slider filling the rest of the row.
            var row = new UITK.VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 50,
                    marginBottom = 8,
                    backgroundColor = new StyleColor(RowBg),
                    paddingLeft = 12,
                    paddingRight = 12
                }
            };
            MarkSearchable(row, label);

            var textContainer = new UITK.VisualElement();
            textContainer.style.minWidth = 300;
            textContainer.style.maxWidth = 300;
            textContainer.style.flexDirection = FlexDirection.Column;
            textContainer.style.justifyContent = Justify.Center;

            var lab = new UITK.Label(label);
            MakeReadable(lab);
            lab.style.fontSize = 24;
            // Keep long labels on one line; truncate rather than wrap into the
            // value field (CompAdjust labels are short enough to never hit this).
            lab.style.whiteSpace = WhiteSpace.NoWrap;
            lab.style.overflow = Overflow.Hidden;
            lab.style.textOverflow = TextOverflow.Ellipsis;
            textContainer.Add(lab);
            row.Add(textContainer);

            // Editable value field, left of the slider.
            var localValueField = new UITK.TextField();
            localValueField.value = start.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            MakeReadable(localValueField);
            localValueField.style.minWidth = 65;
            localValueField.style.maxWidth = 65;
            localValueField.style.maxHeight = 30;
            localValueField.style.unityTextAlign = TextAnchor.MiddleRight;
            localValueField.style.marginLeft = 8;
            localValueField.style.marginRight = 8;
            row.Add(localValueField);
            valueField = localValueField;

            // Slider fills the remaining width to the right edge.
            var localSlider = new UITK.Slider(min, max) { value = Mathf.Clamp(start, min, max) };
            localSlider.style.flexGrow = 1;
            localSlider.style.flexBasis = 0;
            localSlider.style.marginLeft = 6;
            localSlider.style.marginRight = 6;
            StyleSliderLikeBase(localSlider);
            row.Add(localSlider);
            slider = localSlider;

            // Two-way binding between slider and text field
            localSlider.RegisterValueChangedCallback(ev => {
                float clampedValue = Mathf.Clamp(ev.newValue, min, max);
                localValueField.SetValueWithoutNotify(clampedValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                onChange?.Invoke(clampedValue);
            });

            localValueField.RegisterValueChangedCallback(ev => {
                if (float.TryParse(ev.newValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float newValue))
                {
                    float clampedValue = Mathf.Clamp(newValue, min, max);
                    localSlider.SetValueWithoutNotify(clampedValue);
                    onChange?.Invoke(clampedValue);
                }
            });

            return row;
        }

        private UITK.VisualElement MakeTextFieldRow(string label, string startValue, System.Action<string> onChange)
        {
            var row = new UITK.VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 50,
                    marginBottom = 8,
                    backgroundColor = new StyleColor(RowBg),
                    paddingLeft = 12,
                    paddingRight = 12
                }
            };
            MarkSearchable(row, label);

            var lab = new UITK.Label(label);
            MakeReadable(lab);
            lab.style.whiteSpace = WhiteSpace.NoWrap;
            lab.style.fontSize = 24;
            lab.style.minWidth = 220;
            lab.style.maxWidth = 220;
            row.Add(lab);

            // Spacer to push text field to the right
            var spacer = new UITK.VisualElement();
            spacer.style.flexGrow = 1;
            row.Add(spacer);

            var textField = new UITK.TextField();
            textField.value = startValue;
            MakeReadable(textField);
            textField.style.width = 200;
            textField.style.height = 34;
            row.Add(textField);

            textField.RegisterValueChangedCallback(ev => {
                onChange?.Invoke(ev.newValue);
            });

            return row;
        }

        private UITK.VisualElement MakeToggleRow(string label, bool start, System.Action<bool> onChange)
        {
            var row = new UITK.VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 50,
                    marginBottom = 8,
                    backgroundColor = new StyleColor(RowBg),
                    paddingLeft = 12,
                    paddingRight = 12
                }
            };
            MarkSearchable(row, label);

            var lab = new UITK.Label(label);
            MakeReadable(lab);
            lab.style.whiteSpace = WhiteSpace.NoWrap;
            lab.style.fontSize = 24;
            lab.style.minWidth = 220;
            lab.style.maxWidth = 220;
            row.Add(lab);

            // Spacer to push toggle to the right
            var spacer = new UITK.VisualElement();
            spacer.style.flexGrow = 1;
            row.Add(spacer);

            var t = new UITK.Toggle { value = start };
            StyleConfigCheckbox(t);
            row.Add(t);
            t.RegisterValueChangedCallback(ev => onChange?.Invoke(ev.newValue));
            return row;
        }

        private UITK.VisualElement MakeDropdownRow(string label, string currentValue, List<string> options, System.Action<string> onChange)
        {
            var row = new UITK.VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 50,
                    marginBottom = 8,
                    backgroundColor = new StyleColor(RowBg),
                    paddingLeft = 12,
                    paddingRight = 12
                }
            };
            MarkSearchable(row, label);

            var lab = new UITK.Label(label);
            MakeReadable(lab);
            lab.style.whiteSpace = WhiteSpace.NoWrap;
            lab.style.fontSize = 24;
            lab.style.minWidth = 220;
            lab.style.maxWidth = 220;
            row.Add(lab);

            // Spacer to push dropdown to the right
            var spacer = new UITK.VisualElement();
            spacer.style.flexGrow = 1;
            row.Add(spacer);

            // Proper DropdownField with scrollable options
            var dropdown = new UITK.DropdownField();
            dropdown.choices = options;
            dropdown.value = options.Contains(currentValue) ? currentValue : (options.Count > 0 ? options[0] : "");
            dropdown.style.width = 300;
            dropdown.style.height = 34;
            StyleDropdown(dropdown);
            
            dropdown.RegisterValueChangedCallback(ev => onChange?.Invoke(ev.newValue));
            row.Add(dropdown);

            return row;
        }

        private void StyleDropdown(UITK.DropdownField dropdown)
        {
            dropdown.style.backgroundColor = new StyleColor(TextFieldBg);
            dropdown.style.color = Color.white;
            ForceUIFont(dropdown);
            
            // Prevent text wrapping and truncate with ellipsis
            var textElement = dropdown.Q<UITK.TextElement>();
            if (textElement != null)
            {
                textElement.style.overflow = Overflow.Hidden;
                textElement.style.textOverflow = TextOverflow.Ellipsis;
                textElement.style.whiteSpace = WhiteSpace.NoWrap;
                textElement.style.color = Color.white;
                ForceUIFont(textElement);
            }
            
            // Style the label inside the dropdown (CompAdjust keeps the built-in
            // DropdownField arrow rather than overlaying a custom one).
            var label = dropdown.Q<UITK.Label>();
            if (label != null)
            {
                label.style.color = Color.white;
                ForceUIFont(label);
            }
        }

        private List<string> GetLogoFiles()
        {
            var logoFiles = new List<string>();
            try
            {
                string logoDir = ScoreboardPaths.LogosDir;
                if (Directory.Exists(logoDir))
                {
                    var pngFiles = Directory.GetFiles(logoDir, "*.png");
                    foreach (var file in pngFiles)
                    {
                        logoFiles.Add(Path.GetFileName(file));
                    }
                }

                if (logoFiles.Count == 0)
                {
                    logoFiles.Add("leaguelogo.png");
                    logoFiles.Add("blueteamlogo.png");
                    logoFiles.Add("redteamlogo.png");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Scoreboard] Failed to scan logo directory: " + e);
                logoFiles.Add("leaguelogo.png");
                logoFiles.Add("blueteamlogo.png");
                logoFiles.Add("redteamlogo.png");
            }
            return logoFiles;
        }
    }
}
