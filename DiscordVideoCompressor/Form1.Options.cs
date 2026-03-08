using System;
using System.Windows.Forms;

namespace DiscordVideoCompressor
{
    public partial class Form1
    {
        private bool TryBuildConversionOptions(out ConversionOptions options, out string validationMessage)
        {
            options = null;
            validationMessage = null;

            if (!TryGetOutputFormat(out string outputFormat))
            {
                validationMessage = Resources.Strings.SelectOutputFormatMessage;
                return false;
            }

            long targetSizeBytes;
            if (radioButton1.Checked)
            {
                targetSizeBytes = 9L * 1024 * 1024;
            }
            else if (radioButton3.Checked)
            {
                if (!long.TryParse(customSizeTextBox.Text, out long customSizeMb) || customSizeMb <= 0)
                {
                    validationMessage = Resources.Strings.EnterValidSizeMessage;
                    return false;
                }

                targetSizeBytes = customSizeMb * 1024 * 1024;
            }
            else
            {
                validationMessage = Resources.Strings.SelectSizeOptionMessage;
                return false;
            }

            options = new ConversionOptions
            {
                TargetSizeBytes = targetSizeBytes,
                OutputFormat = outputFormat,
                AudioSampleRate = GetSampleRate(),
                AudioBitDepth = GetAudioBitDepth(),
                VideoResolution = GetVideoResolution(),
                VideoFps = GetVideoFps(),
                EnableSpeedEffect = checkBoxSpeedEffect.Checked,
                EnableDatamosh = checkBoxDatamosh.Checked,
                EnableGlitchEffect = checkBoxGlitchEffect.Checked,
                GlitchJumpSeconds = GetGlitchJumpLengthSeconds(),
                GlitchChance = GetGlitchChance()
            };

            return true;
        }

        private bool TryGetOutputFormat(out string outputFormat)
        {
            outputFormat = comboBoxFormat.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(outputFormat))
            {
                outputFormat = comboBoxFormat.Text;
            }

            if (string.IsNullOrWhiteSpace(outputFormat))
            {
                return false;
            }

            outputFormat = outputFormat.Trim().ToUpperInvariant();
            return outputFormat == "MP4" || outputFormat == "WEBM";
        }

        private int GetSampleRate()
        {
            string sampleRateValue = comboBoxSampleRate.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(sampleRateValue))
            {
                sampleRateValue = comboBoxSampleRate.Text;
            }

            if (int.TryParse(sampleRateValue, out int parsedRate) && parsedRate > 0)
            {
                return parsedRate;
            }

            return 44100;
        }

        private string GetVideoResolution()
        {
            string resolutionValue = comboBoxResolution.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(resolutionValue))
            {
                resolutionValue = comboBoxResolution.Text;
            }

            return string.IsNullOrWhiteSpace(resolutionValue) ? "1920x1080" : resolutionValue.Trim();
        }

        private string GetAudioBitDepth()
        {
            string bitDepthValue = comboBoxBitDepth.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(bitDepthValue))
            {
                bitDepthValue = comboBoxBitDepth.Text;
            }

            return string.IsNullOrWhiteSpace(bitDepthValue) ? "s16" : bitDepthValue.Trim();
        }

        private int GetVideoFps()
        {
            string fpsValue = comboBoxVideoFps.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(fpsValue))
            {
                fpsValue = comboBoxVideoFps.Text;
            }

            if (int.TryParse(fpsValue, out int parsedFps) && parsedFps > 0)
            {
                return parsedFps;
            }

            return 60;
        }

        private double GetGlitchJumpLengthSeconds()
        {
            if (numericGlitchLength == null)
            {
                return 1.2;
            }

            double value = (double)numericGlitchLength.Value;
            if (value <= 0)
            {
                return 1.2;
            }

            return value;
        }

        private double GetGlitchChance()
        {
            if (numericGlitchChance == null)
            {
                return 0.25;
            }

            double percent = (double)numericGlitchChance.Value;
            if (percent < 0)
            {
                percent = 0;
            }

            if (percent > 100)
            {
                percent = 100;
            }

            return percent / 100.0;
        }

        internal void ApplyDiscordDefaultPreset()
        {
            radioButton1.Checked = true;
            radioButton3.Checked = false;
            customSizeTextBox.Text = string.Empty;

            SetComboBoxValue(comboBoxFormat, "MP4");
            SetComboBoxValue(comboBoxResolution, "1920x1080");
            SetComboBoxValue(comboBoxSampleRate, "44100");
            SetComboBoxValue(comboBoxBitDepth, "s16");
            SetComboBoxValue(comboBoxVideoFps, "60");

            checkBoxSpeedEffect.Checked = false;
            checkBoxDatamosh.Checked = false;
            checkBoxGlitchEffect.Checked = false;

            if (numericGlitchLength != null)
            {
                numericGlitchLength.Value = 1.2m;
            }

            if (numericGlitchChance != null)
            {
                numericGlitchChance.Value = 25m;
            }

            UpdateGlitchControlsVisibility();
        }

        private void SetDefaultSelections()
        {
            if (comboBoxResolution.SelectedIndex < 0)
            {
                SetComboBoxValue(comboBoxResolution, "1920x1080");
            }

            if (comboBoxSampleRate.SelectedIndex < 0)
            {
                SetComboBoxValue(comboBoxSampleRate, "44100");
            }

            if (comboBoxBitDepth.SelectedIndex < 0)
            {
                SetComboBoxValue(comboBoxBitDepth, "s16");
            }

            if (comboBoxVideoFps.SelectedIndex < 0)
            {
                SetComboBoxValue(comboBoxVideoFps, "60");
            }
        }

        private void UpdateGlitchControlsVisibility()
        {
            bool show = checkBoxGlitchEffect.Checked;
            labelGlitchLength.Visible = show;
            numericGlitchLength.Visible = show;
            labelGlitchChance.Visible = show;
            numericGlitchChance.Visible = show;
        }

        private void SetComboBoxValue(ComboBox comboBox, string value)
        {
            int index = comboBox.FindStringExact(value);
            if (index >= 0)
            {
                comboBox.SelectedIndex = index;
                return;
            }

            comboBox.Text = value;
        }
    }
}
