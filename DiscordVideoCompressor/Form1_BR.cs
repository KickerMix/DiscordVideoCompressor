using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DiscordVideoCompressor
{
    public partial class Form1 : Form
    {
        private string inputFile;
        private string createdFile;
        private CancellationTokenSource cancellationTokenSource;
        private readonly FfmpegConversionService conversionService;
        private readonly string startupInputFile;
        private readonly bool copyOutputToClipboardOnSuccess;
        private readonly ToolStripStatusLabel statusLabelLicenses;
        private bool startupConversionStarted;
        private double progressStageStart;
        private double progressStageSpan = 1.0;
        private double progressStageDuration;
        private bool progressStageHasProgress;
        private double progressLastPercent;
        private double progressLastSpeed;
        private double progressCurrentSeconds;
        private double progressTotalSeconds;
        private double? progressEtaSeconds;
        private string progressStageName;

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public Form1(string startupInputFile = null, bool copyOutputToClipboardOnSuccess = false)
        {
            InitializeComponent();
            statusLabelLicenses = new ToolStripStatusLabel
            {
                IsLink = true,
                LinkColor = System.Drawing.Color.LightSkyBlue
            };
            statusLabelLicenses.Click += statusLabelLicenses_Click;
            statusStrip1.Items.Add(statusLabelLicenses);
            this.startupInputFile = startupInputFile;
            this.copyOutputToClipboardOnSuccess = copyOutputToClipboardOnSuccess;
            conversionService = new FfmpegConversionService(
                SetProgressStage,
                UpdateProgress,
                BuildVideoFilter,
                BuildAudioFilter,
                BuildSpeedFilterComplex,
                BuildDatamoshFilterComplex,
                BuildDatamoshSegments,
                SelectAudioBitrate,
                CreateConversionText());
            FormClosing += Form1_FormClosing;
            Shown += Form1_Shown;

            EnableDarkMode(Handle);

            logoPictureBox.Image = Properties.Resources.logo;

            comboBoxLanguage.Items.AddRange(new string[] { "EN", "RU" });
            comboBoxLanguage.SelectedIndex = 0;
            SetLanguage("en");

            comboBoxFormat.SelectedIndex = 0;
            SetDefaultSelections();

            AllowDrop = true;
            DragEnter += Form1_DragEnter;
            DragDrop += Form1_DragDrop;

            UpdateSelectedFileLabel();
            UpdateGlitchControlsVisibility();
        }

        private void EnableDarkMode(IntPtr handle)
        {
            if (IsDarkModeEnabled())
            {
                int isDarkMode = 1;
                DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref isDarkMode, sizeof(int));
            }
        }

        private bool IsDarkModeEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))
            {
                object value = key?.GetValue("AppsUseLightTheme");
                return value != null && (int)value == 0;
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 0)
            {
                string file = files[0];

                if (IsSupportedVideoFile(file))
                {
                    inputFile = file;
                    UpdateSelectedFileLabel();
                }
                else
                {
                    MessageBox.Show(Resources.Strings.InvalidFileFormatMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private static bool IsSupportedVideoFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".mp4" || extension == ".avi" || extension == ".mkv" || extension == ".webm";
        }

        private void comboBoxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedLanguage = comboBoxLanguage.SelectedItem?.ToString();
            switch (selectedLanguage)
            {
                case "EN":
                    SetLanguage("en");
                    break;
                case "RU":
                    SetLanguage("ru");
                    break;
            }
        }

        private void SetLanguage(string cultureName)
        {
            CultureInfo culture = new CultureInfo(cultureName);
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Application.CurrentCulture = culture;
            UpdateUI();
        }

        private void UpdateUI()
        {
            selectFileButton.Text = Resources.Strings.ChooseMediaFileButtonText;
            convertButton.Text = Resources.Strings.ConvertationButtonText;
            cancelButton.Text = Resources.Strings.ForceStopButtonText;
            radioButton1.Text = Resources.Strings.DiscordPresetText;
            radioButton3.Text = Resources.Strings.CustomMaxSizeText;
            brainrotLabel.Text = Resources.Strings.BrainrotEditionText;
            label4.Text = Resources.Strings.ResolutionLabelText;
            label5.Text = Resources.Strings.SampleRateLabelText;
            label6.Text = Resources.Strings.BitDepthLabelText;
            label7.Text = Resources.Strings.FpsLabelText;
            checkBoxSpeedEffect.Text = Resources.Strings.BrainrotSpeedEffectText;
            checkBoxDatamosh.Text = Resources.Strings.BrainrotDatamoshEffectText;
            checkBoxGlitchEffect.Text = Resources.Strings.BrainrotGlitchEffectText;
            statusLabelLicenses.Text = Resources.Strings.LicensesLabelText;
            labelGlitchLength.Text = Resources.Strings.GlitchJumpLengthLabelText;
            labelGlitchChance.Text = Resources.Strings.GlitchChanceLabelText;
            UpdateSelectedFileLabel();
            ApplyProgressText();

            foreach (Control control in Controls)
            {
                control.Refresh();
            }
        }

        private void selectFileButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Video Files (*.mp4;*.avi;*.mkv;*.webm)|*.mp4;*.avi;*.mkv;*.webm";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    inputFile = openFileDialog.FileName;
                    UpdateSelectedFileLabel();
                }
            }
        }

        private async void convertButton_Click(object sender, EventArgs e)
        {
            await StartConversionAsync(false);
        }

        private async Task StartConversionAsync(bool copyOutputToClipboardAfterSuccess)
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                MessageBox.Show(Resources.Strings.SelectMediaFileMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!TryBuildConversionOptions(out ConversionOptions options, out string validationMessage))
            {
                MessageBox.Show(validationMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            createdFile = null;
            SetConversionUiState(true);

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            try
            {
                progressBar1.Value = 0;
                string warningMessage = null;
                string outputPath = await Task.Run(() => conversionService.ConvertFile(inputFile, options, token, out warningMessage), token);
                createdFile = outputPath;

                if (!string.IsNullOrWhiteSpace(warningMessage))
                {
                    MessageBox.Show(warningMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                if (!string.IsNullOrWhiteSpace(createdFile))
                {
                    if (copyOutputToClipboardAfterSuccess)
                    {
                        await TryCopyOutputFileToClipboardAsync(createdFile);
                    }

                    MessageBox.Show(Resources.Strings.ConversionSuccessMessage + createdFile, Resources.Strings.SuccessTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(Resources.Strings.ConversionCanceledMessage, Resources.Strings.MessageTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetConversionUiState(false);
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            if (startupConversionStarted || string.IsNullOrWhiteSpace(startupInputFile))
            {
                return;
            }

            startupConversionStarted = true;
            if (!File.Exists(startupInputFile))
            {
                MessageBox.Show(Resources.Strings.FileNotExistError, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsSupportedVideoFile(startupInputFile))
            {
                MessageBox.Show(Resources.Strings.InvalidFileFormatMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ApplyDiscordDefaultPreset();
            inputFile = startupInputFile;
            UpdateSelectedFileLabel();
            await StartConversionAsync(copyOutputToClipboardOnSuccess);
        }

        private async Task TryCopyOutputFileToClipboardAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            var fileDropList = new System.Collections.Specialized.StringCollection();
            fileDropList.Add(filePath);

            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    Clipboard.SetFileDropList(fileDropList);
                    return;
                }
                catch (ExternalException)
                {
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error copying converted file to clipboard: " + ex.Message);
                    return;
                }
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
            conversionService.Cancel();
        }

        private void brainrotLabel_Click(object sender, EventArgs e)
        {
            SetComboBoxValue(comboBoxResolution, "224x144");
            SetComboBoxValue(comboBoxBitDepth, "u8");
            SetComboBoxValue(comboBoxSampleRate, "8000");
            SetComboBoxValue(comboBoxVideoFps, "10");
        }

        private void logoPictureBox_Click(object sender, EventArgs e)
        {
            SetComboBoxValue(comboBoxResolution, "1920x1080");
            SetComboBoxValue(comboBoxBitDepth, "s16");
            SetComboBoxValue(comboBoxSampleRate, "44100");
            SetComboBoxValue(comboBoxVideoFps, "60");
        }

        private void statusLabelLicenses_Click(object sender, EventArgs e)
        {
            string version = Application.ProductVersion;
            MessageBox.Show(
                $"Discord Video Compressor {version}\n\n{Resources.Strings.LicensesMessageText}",
                Resources.Strings.LicensesLabelText,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetDefaultSelections();
            UpdateSelectedFileLabel();
            UpdateGlitchControlsVisibility();
            ResetProgressStatus();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                conversionService.Cancel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error stopping ffmpeg: " + ex.Message);
            }
        }

        private void comboBoxSampleRate_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void checkBoxDatamosh_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void checkBoxGlitchEffect_CheckedChanged(object sender, EventArgs e)
        {
            UpdateGlitchControlsVisibility();
        }

        private void SetConversionUiState(bool isRunning)
        {
            convertButton.Visible = !isRunning;
            comboBoxFormat.Visible = !isRunning;
            progressBar1.Visible = isRunning;

            selectFileButton.Enabled = !isRunning;
            comboBoxFormat.Enabled = !isRunning;
            radioButton1.Enabled = !isRunning;
            radioButton3.Enabled = !isRunning;
            customSizeTextBox.Enabled = !isRunning;
            comboBoxLanguage.Enabled = !isRunning;
            comboBoxResolution.Enabled = !isRunning;
            comboBoxSampleRate.Enabled = !isRunning;
            comboBoxBitDepth.Enabled = !isRunning;
            comboBoxVideoFps.Enabled = !isRunning;
            brainrotLabel.Enabled = !isRunning;
            logoPictureBox.Enabled = !isRunning;
            checkBoxSpeedEffect.Enabled = !isRunning;
            checkBoxDatamosh.Enabled = !isRunning;
            checkBoxGlitchEffect.Enabled = !isRunning;
            numericGlitchLength.Enabled = !isRunning;
            numericGlitchChance.Enabled = !isRunning;

            if (!isRunning)
            {
                progressBar1.Value = 0;
                progressBar1.Style = ProgressBarStyle.Continuous;
                ResetProgressStatus();
            }
        }

        private void UpdateSelectedFileLabel()
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                selectedFileLabel.Text = Resources.Strings.SelectedFileLabelText + Resources.Strings.NoFileSelectedText;
                return;
            }

            selectedFileLabel.Text = Resources.Strings.SelectedFileLabelText + Path.GetFileName(inputFile);
        }

        private void comboBoxVideoFps_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void comboBoxResolution_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private ConversionText CreateConversionText()
        {
            return new ConversionText
            {
                FileNotFound = Resources.Strings.FileNotExistError,
                DurationError = Resources.Strings.DurationErrorMessage,
                TimeConversionError = Resources.Strings.TimeConversionErrorMessage,
                VideoDurationError = Resources.Strings.VideoDurationErrorMessage,
                DatamoshSpeedConflict = Resources.Strings.DatamoshSpeedConflictMessage,
                DatamoshMp4Only = Resources.Strings.DatamoshMp4OnlyMessage,
                BitrateError = Resources.Strings.BitrateErrorMessage,
                InvalidFileFormat = Resources.Strings.InvalidFileFormatMessage,
                ConversionErrorPrefix = Resources.Strings.ConversionErrorMessage,
                FileConversionError = Resources.Strings.FileConversionErrorMessage,
                DatamoshAudioMissing = Resources.Strings.DatamoshAudioMissingMessage,
                ProgressStagePrepare = Resources.Strings.ProgressStagePrepare,
                ProgressStageGlitch = Resources.Strings.ProgressStageGlitch,
                ProgressStageSpeed = Resources.Strings.ProgressStageSpeed,
                ProgressStagePass1 = Resources.Strings.ProgressStagePass1,
                ProgressStagePass2 = Resources.Strings.ProgressStagePass2,
                ProgressStageDatamoshEncode = Resources.Strings.ProgressStageDatamoshEncode,
                ProgressStageDatamoshTransform = Resources.Strings.ProgressStageDatamoshTransform,
                ProgressStageDatamoshRemux = Resources.Strings.ProgressStageDatamoshRemux
            };
        }
    }
}
