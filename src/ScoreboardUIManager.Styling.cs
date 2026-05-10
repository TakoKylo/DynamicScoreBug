using System;
using UnityEngine;
using UnityEngine.UIElements;
using UITK = UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        // Note: previously took a `color` arg, but AddButtonFlash unconditionally re-sets the
        // background to BtnBrightGray on hover/click/geometry — so any caller-supplied color
        // was overwritten on the very next layout pass. Param dropped to stop misleading callers.
        private void styleButton(Button button)
        {
            ForceUIFont(button);
            button.style.backgroundColor = new StyleColor(BtnBrightGray);
            button.style.color = Color.white;
            button.style.fontSize = 14;
            button.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Normal;
            button.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            button.style.paddingTop = 8;
            button.style.paddingBottom = 8;
            button.style.paddingLeft = 18;
            button.style.paddingRight = 18;
            button.style.height = 50;
            button.style.minWidth = 120;
            button.style.marginLeft = 4;
            button.style.marginRight = 4;
            button.style.whiteSpace = WhiteSpace.NoWrap;
            AddButtonFlash(button);
        }

        private void styleBottomButton(Button button)
        {
            ForceUIFont(button);
            button.style.backgroundColor = new StyleColor(BtnBrightGray);
            button.style.color = Color.white;
            button.style.fontSize = 24;
            button.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Normal;
            button.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            button.style.paddingTop = 8;
            button.style.paddingBottom = 8;
            button.style.paddingLeft = 18;
            button.style.paddingRight = 18;
            button.style.height = 50;
            AddButtonFlash(button);
        }

        private static void AddButtonFlash(Button b, int flashMs = 140)
        {
            var baseBg = BtnBrightGray;
            b.focusable = true;

            void SetBase()
            {
                b.style.backgroundColor = new StyleColor(baseBg);
                b.style.color = Color.white;
            }

            SetBase();
            bool hover = false, flashing = false;

            b.RegisterCallback<PointerEnterEvent>(_ =>
            {
                hover = true;
                b.style.backgroundColor = Color.white;
                b.style.color = Color.black;
            });

            b.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                hover = false;
                if (!flashing) SetBase();
            });

            b.RegisterCallback<GeometryChangedEvent>(_ => SetBase());

            b.RegisterCallback<PointerUpEvent>(_ =>
            {
                flashing = true;
                b.style.backgroundColor = Color.white;
                b.style.color = Color.black;
                b.schedule.Execute(() =>
                {
                    flashing = false;
                    if (!hover) SetBase();
                }).StartingIn(flashMs);
            });
        }

        private void AddSection(VisualElement container, string title, int fontSize = 24)
        {
            var section = new Label(title);
            MakeReadable(section);
            section.style.fontSize = fontSize;
            section.style.marginTop = 15;
            section.style.marginBottom = 10;
            section.style.paddingLeft = 5;
            section.style.paddingBottom = 5;
            container.Add(section);
        }

        private void AddSliderField(VisualElement container, string label, float value, float min, float max, System.Action<float> onChange)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 8;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.backgroundColor = new Color(RowBg.r / 255f, RowBg.g / 255f, RowBg.b / 255f, RowBg.a / 255f);

            var labelEl = new Label(label);
            MakeReadable(labelEl);
            labelEl.style.width = 150;
            labelEl.style.fontSize = 13;
            row.Add(labelEl);

            var slider = new Slider(min, max);
            slider.value = value;
            slider.style.flexGrow = 1;
            slider.style.marginLeft = 10;
            slider.style.marginRight = 10;
            row.Add(slider);

            var valueLabel = new Label(value.ToString("F1"));
            MakeReadable(valueLabel);
            valueLabel.style.width = 40;
            valueLabel.style.fontSize = 12;
            valueLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(valueLabel);

            slider.RegisterValueChangedCallback(evt => 
            {
                onChange(evt.newValue);
                valueLabel.text = evt.newValue.ToString("F1");
            });

            container.Add(row);
        }

        private void AddTextFieldRow(VisualElement container, string label, string value, System.Action<string> onChange)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 8;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.backgroundColor = new Color(RowBg.r / 255f, RowBg.g / 255f, RowBg.b / 255f, RowBg.a / 255f);

            var labelEl = new Label(label);
            MakeReadable(labelEl);
            labelEl.style.width = 150;
            labelEl.style.fontSize = 13;
            row.Add(labelEl);

            var textField = new TextField();
            textField.value = value;
            MakeReadable(textField);
            textField.style.flexGrow = 1;
            textField.style.marginLeft = 10;
            textField.RegisterValueChangedCallback(evt => onChange(evt.newValue));
            row.Add(textField);

            container.Add(row);
        }

        private void AddToggleField(VisualElement container, string label, bool value, System.Action<bool> onChange)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 8;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.backgroundColor = new Color(RowBg.r / 255f, RowBg.g / 255f, RowBg.b / 255f, RowBg.a / 255f);

            var labelEl = new Label(label);
            MakeReadable(labelEl);
            labelEl.style.width = 150;
            labelEl.style.fontSize = 13;
            row.Add(labelEl);

            var toggle = new Toggle();
            toggle.value = value;
            toggle.style.marginLeft = 10;
            toggle.RegisterValueChangedCallback(evt => onChange(evt.newValue));
            row.Add(toggle);

            container.Add(row);
        }
    }
}
