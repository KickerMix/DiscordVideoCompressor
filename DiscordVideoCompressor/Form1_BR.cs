using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
        private Process ffmpegProcess;
        private string extractedFfmpegPath;

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private sealed class SpeedSegment
        {
            public SpeedSegment(double start, double end, double speed)
            {
                Start = start;
                End = end;
                Speed = speed;
            }

            public double Start { get; }
            public double End { get; }
            public double Speed { get; }
        }

        private sealed class DatamoshSegment
        {
            public DatamoshSegment(double start, double end, double sourceStart, bool applyEffect, int seed)
            {
                Start = start;
                End = end;
                SourceStart = sourceStart;
                ApplyEffect = applyEffect;
                Seed = seed;
            }

            public double Start { get; }
            public double End { get; }
            public double SourceStart { get; }
            public bool ApplyEffect { get; }
            public int Seed { get; }
        }

        private sealed class DatamoshWindow
        {
            public DatamoshWindow(double start, double end)
            {
                Start = start;
                End = end;
            }

            public double Start { get; }
            public double End { get; }
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;

            EnableDarkMode(this.Handle);

            pictureBox1.Image = Properties.Resources.logo;

            comboBoxLanguage.Items.AddRange(new string[] { "EN", "RU" });
            comboBoxLanguage.SelectedIndex = 0;
            SetLanguage("en");

            comboBoxFormat.SelectedIndex = 0;
            SetDefaultSelections();

            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;

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
                string extension = Path.GetExtension(file).ToLowerInvariant();

                if (extension == ".mp4" || extension == ".avi" || extension == ".mkv" || extension == ".webm")
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
            Application.DoEvents();
            UpdateUI();
        }

        private void UpdateUI()
        {
            button1.Text = Resources.Strings.ChooseMediaFileButtonText;
            button2.Text = Resources.Strings.ConvertationButtonText;
            button3.Text = Resources.Strings.ForceStopButtonText;
            radioButton1.Text = Resources.Strings.DiscordPresetText;
            radioButton3.Text = Resources.Strings.CustomMaxSizeText;
            label2.Text = Resources.Strings.BrainrotEditionText;
            label4.Text = Resources.Strings.ResolutionLabelText;
            label5.Text = Resources.Strings.SampleRateLabelText;
            label6.Text = Resources.Strings.BitDepthLabelText;
            label7.Text = Resources.Strings.FpsLabelText;
            checkBoxSpeedEffect.Text = Resources.Strings.BrainrotSpeedEffectText;
            checkBoxDatamosh.Text = Resources.Strings.BrainrotDatamoshEffectText;
            checkBoxGlitchEffect.Text = Resources.Strings.BrainrotGlitchEffectText;
            labelGlitchLength.Text = Resources.Strings.GlitchJumpLengthLabelText;
            labelGlitchChance.Text = Resources.Strings.GlitchChanceLabelText;
            UpdateSelectedFileLabel();

            foreach (Control control in this.Controls)
            {
                control.Refresh();
            }
        }

        private void button1_Click(object sender, EventArgs e) // Choose media file
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

        private async void button2_Click(object sender, EventArgs e) // Convert
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                MessageBox.Show(Resources.Strings.SelectMediaFileMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!TryGetOutputFormat(out string outputFormat))
            {
                MessageBox.Show(Resources.Strings.SelectOutputFormatMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            long targetSizeBytes = 0;

            if (radioButton1.Checked) // Discord 9MB preset
            {
                targetSizeBytes = 9L * 1024 * 1024;
            }
            else if (radioButton3.Checked) // Custom size preset
            {
                if (long.TryParse(textBox1.Text, out long customSizeMB) && customSizeMB > 0)
                {
                    targetSizeBytes = customSizeMB * 1024 * 1024;
                }
                else
                {
                    MessageBox.Show(Resources.Strings.EnterValidSizeMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                MessageBox.Show(Resources.Strings.SelectSizeOptionMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int audioSampleRate = GetSampleRate();
            string audioBitDepth = GetAudioBitDepth();
            string videoResolution = GetVideoResolution();
            int videoFps = GetVideoFps();
            bool enableSpeedEffect = checkBoxSpeedEffect.Checked;
            bool enableDatamosh = checkBoxDatamosh.Checked;
            bool enableGlitchEffect = checkBoxGlitchEffect.Checked;
            double glitchJumpSeconds = GetGlitchJumpLengthSeconds();
            double glitchChance = GetGlitchChance();

            createdFile = null;
            SetConversionUiState(true);

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            try
            {
                progressBar1.Value = 0;
                await Task.Run(() => ConvertFile(inputFile, targetSizeBytes, outputFormat, audioSampleRate, audioBitDepth, videoResolution, videoFps, enableSpeedEffect, enableDatamosh, enableGlitchEffect, glitchJumpSeconds, glitchChance, token), token);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(Resources.Strings.ConversionCanceledMessage, Resources.Strings.MessageTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                SetConversionUiState(false);
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }

        private void button3_Click(object sender, EventArgs e) // Force stop
        {
            cancellationTokenSource?.Cancel();
            TryKillFfmpeg();
        }

        private void label2_Click(object sender, EventArgs e)
        {
            SetComboBoxValue(comboBoxResolution, "224x144");
            SetComboBoxValue(comboBoxBitDepth, "u8");
            SetComboBoxValue(comboBoxSampleRate, "8000");
            SetComboBoxValue(comboBoxvideoFPS, "10");
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            SetComboBoxValue(comboBoxResolution, "1920x1080");
            SetComboBoxValue(comboBoxBitDepth, "s16");
            SetComboBoxValue(comboBoxSampleRate, "44100");
            SetComboBoxValue(comboBoxvideoFPS, "60");
        }

        private string ExtractFfmpeg()
        {
            if (!string.IsNullOrEmpty(extractedFfmpegPath) && File.Exists(extractedFfmpegPath))
            {
                return extractedFfmpegPath;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
            string ffmpegPath = Path.Combine(tempDir, $"ffmpeg_{Guid.NewGuid():N}.exe");

            try
            {
                Directory.CreateDirectory(tempDir);
                File.WriteAllBytes(ffmpegPath, Properties.Resources.ffmpeg);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Resources.Strings.FfmpegExtractionErrorMessage + "\n" + ex.Message, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            extractedFfmpegPath = ffmpegPath;
            return ffmpegPath;
        }

        private string GetFfmpegPath()
        {
            return ExtractFfmpeg();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                TryKillFfmpeg();
                CleanupFfmpeg();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error deleting ffmpeg: " + ex.Message);
            }
        }

        private void ConvertFile(string inputFile, long targetSizeBytes, string outputFormat, int audioSampleRate, string audioBitDepth, string videoResolution, int videoFps, bool enableSpeedEffect, bool enableDatamosh, bool enableGlitchEffect, double glitchJumpSeconds, double glitchChance, CancellationToken token)
        {
            try
            {
                if (!File.Exists(inputFile))
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show(Resources.Strings.FileNotExistError, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }

                outputFormat = outputFormat.Trim().ToLowerInvariant();

                string outputFile = Path.Combine(
                    Path.GetDirectoryName(inputFile),
                    Path.GetFileNameWithoutExtension(inputFile) + "_cnvrtd" + $".{outputFormat}"
                );

                bool conversionSuccess = false;
                int pass = 0;

                string ffmpegPath = GetFfmpegPath();
                if (ffmpegPath == null)
                {
                    return;
                }

                int maxPasses = 3;

                double duration = 0;
                int inputAudioSampleRate = audioSampleRate;
                using (var probeProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = $"-i \"{inputFile}\" -hide_banner",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                })
                {
                    probeProcess.Start();
                    string output = probeProcess.StandardError.ReadToEnd();
                    probeProcess.WaitForExit();

                    Regex regex = new Regex(@"Duration:\s(\d+):(\d+):(\d+\.?\d*)");
                    Match match = regex.Match(output);

                    if (!match.Success)
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show(Resources.Strings.DurationErrorMessage + "\n" + output, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    int hours = int.Parse(match.Groups[1].Value);
                    int minutes = int.Parse(match.Groups[2].Value);
                    if (!double.TryParse(match.Groups[3].Value.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show(Resources.Strings.TimeConversionErrorMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    duration = hours * 3600 + minutes * 60 + seconds;

                    Match audioMatch = Regex.Match(output, @"Audio:\s*([^,\s]+).*?(\d+)\s*Hz");
                    if (audioMatch.Success)
                    {
                        if (int.TryParse(audioMatch.Groups[2].Value, out int parsedInputSampleRate) && parsedInputSampleRate > 0)
                        {
                            inputAudioSampleRate = parsedInputSampleRate;
                        }
                    }
                }

                if (duration <= 0)
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show(Resources.Strings.VideoDurationErrorMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }

                if (enableDatamosh && enableSpeedEffect)
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show(Resources.Strings.DatamoshSpeedConflictMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }));
                    return;
                }

                if (enableDatamosh && outputFormat != "mp4")
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show(Resources.Strings.DatamoshMp4OnlyMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }));
                    return;
                }

                int datamoshSeed = Environment.TickCount;
                var glitchSegments = enableGlitchEffect
                    ? BuildDatamoshSegments(duration, datamoshSeed, glitchJumpSeconds, glitchChance)
                    : null;
                int datamoshPostSeed = datamoshSeed ^ 0x4f1bbc;

                long targetBitrate = (long)((targetSizeBytes * 8) / duration);
                int audioBitrate = SelectAudioBitrate(targetBitrate);
                long videoBitrate = targetBitrate - audioBitrate;

                if (videoBitrate <= 0)
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show(Resources.Strings.BitrateErrorMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }

                string codecVideo = "";
                string codecAudio = "";

                if (outputFormat == "webm")
                {
                    codecVideo = "libvpx-vp9";
                    codecAudio = "libopus";
                }
                else if (outputFormat == "mp4")
                {
                    codecVideo = "libx264";
                    codecAudio = "aac";
                }
                else
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show(Resources.Strings.InvalidFileFormatMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }

                string videoFilter = BuildVideoFilter(videoResolution);
                string videoFilterArg = string.IsNullOrWhiteSpace(videoFilter) ? "" : $" -vf \"{videoFilter}\"";
                string audioFilter = BuildAudioFilter(audioBitDepth);
                string audioFilterArg = string.IsNullOrWhiteSpace(audioFilter) ? "" : $" -af \"{audioFilter}\"";
                string fpsArg = videoFps > 0 ? $" -r {videoFps}" : "";
                string pixelFormatArg = outputFormat == "mp4" ? " -pix_fmt yuv420p" : "";

                long currentVideoBitrate = videoBitrate;

                do
                {
                    token.ThrowIfCancellationRequested();

                    pass++;
                    if (pass > maxPasses)
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show(Resources.Strings.FileConversionErrorMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }));
                        return;
                    }

                    if (currentVideoBitrate <= 0)
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show(Resources.Strings.BitrateErrorMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    string passLogFileBase = null;
                    string outputFileTemp = Path.Combine(
                        Path.GetDirectoryName(outputFile),
                        Path.GetFileNameWithoutExtension(outputFile) + ".tmp." + outputFormat
                    );
                    string datamoshIntermediate = null;
                    string glitchIntermediate = null;

                    try
                    {
                        if (File.Exists(outputFileTemp))
                        {
                            File.Delete(outputFileTemp);
                        }

                        string pass1Args;
                        string pass2Args;

                        if (enableSpeedEffect && enableGlitchEffect)
                        {
                            string tempDir = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
                            Directory.CreateDirectory(tempDir);
                            glitchIntermediate = Path.Combine(tempDir, $"glitch_speed_{Guid.NewGuid():N}.{outputFormat}");

                            var segments = glitchSegments ?? BuildDatamoshSegments(duration, datamoshSeed, glitchJumpSeconds, glitchChance);
                            string glitchFilter = BuildDatamoshFilterComplex(videoResolution, audioBitDepth, audioSampleRate, videoFps, segments, out string glitchVLabel, out string glitchALabel);
                            string glitchRateArgs = outputFormat == "mp4"
                                ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}"
                                : "";

                            string glitchArgs = $"-i \"{inputFile}\" -filter_complex \"{glitchFilter}\" -map \"{glitchVLabel}\" -map \"{glitchALabel}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{glitchRateArgs} -b:a {audioBitrate} -c:a {codecAudio} -ar {audioSampleRate} -preset veryfast{pixelFormatArg} -y \"{glitchIntermediate}\"";
                            if (!RunFfmpegProcess(ffmpegPath, glitchArgs, token, true, duration, out StringBuilder glitchOutput))
                            {
                                Invoke((Action)(() =>
                                {
                                    MessageBox.Show(Resources.Strings.ConversionErrorMessage + glitchOutput, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }));
                                return;
                            }

                            string speedFilter = BuildSpeedFilterComplex(duration, videoResolution, audioBitDepth, audioSampleRate, audioSampleRate, videoFps, null, true, out string vOutLabel, out string aOutLabel);
                            string speedRateArgs = outputFormat == "mp4"
                                ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}"
                                : "";

                            pass1Args = $"-i \"{glitchIntermediate}\" -filter_complex \"{speedFilter}\" -map \"{vOutLabel}\" -map \"{aOutLabel}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{speedRateArgs} -b:a {audioBitrate} -c:a {codecAudio} -ar {audioSampleRate} -preset veryfast{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass2Args = null;
                        }
                        else if (enableSpeedEffect)
                        {
                            string filterComplex = BuildSpeedFilterComplex(duration, videoResolution, audioBitDepth, audioSampleRate, inputAudioSampleRate, videoFps, null, true, out string vOutLabel, out string aOutLabel);
                            string rateControlArgs = outputFormat == "mp4"
                                ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}"
                                : "";

                            pass1Args = $"-i \"{inputFile}\" -filter_complex \"{filterComplex}\" -map \"{vOutLabel}\" -map \"{aOutLabel}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{rateControlArgs} -b:a {audioBitrate} -c:a {codecAudio} -ar {audioSampleRate} -preset veryfast{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass2Args = null;
                        }
                        else if (enableDatamosh && enableGlitchEffect)
                        {
                            string tempDir = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
                            Directory.CreateDirectory(tempDir);
                            datamoshIntermediate = Path.Combine(tempDir, $"datamosh_glitch_{Guid.NewGuid():N}.mp4");

                            if (!RunTrueDatamoshPipeline(ffmpegPath, inputFile, datamoshIntermediate, currentVideoBitrate, audioBitrate, audioSampleRate, audioBitDepth, videoResolution, videoFps, duration, datamoshSeed, token, out StringBuilder datamoshOutput, out bool datamoshHasAudio))
                            {
                                Invoke((Action)(() =>
                                {
                                    MessageBox.Show(Resources.Strings.ConversionErrorMessage + datamoshOutput, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }));
                                return;
                            }

                            if (!datamoshHasAudio)
                            {
                                Invoke((Action)(() =>
                                {
                                    MessageBox.Show(Resources.Strings.DatamoshAudioMissingMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }));
                            }

                            var segments = glitchSegments ?? BuildDatamoshSegments(duration, datamoshPostSeed, glitchJumpSeconds, glitchChance);
                            string filterComplex = BuildDatamoshFilterComplex(videoResolution, audioBitDepth, audioSampleRate, videoFps, segments, out string vOutLabel, out string aOutLabel);
                            string rateControlArgs = outputFormat == "mp4"
                                ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}"
                                : "";

                            pass1Args = $"-i \"{datamoshIntermediate}\" -filter_complex \"{filterComplex}\" -map \"{vOutLabel}\" -map \"{aOutLabel}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{rateControlArgs} -b:a {audioBitrate} -c:a {codecAudio} -ar {audioSampleRate} -preset veryfast{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass2Args = null;
                        }
                        else if (enableDatamosh)
                        {
                            if (!RunTrueDatamoshPipeline(ffmpegPath, inputFile, outputFileTemp, currentVideoBitrate, audioBitrate, audioSampleRate, audioBitDepth, videoResolution, videoFps, duration, datamoshSeed, token, out StringBuilder datamoshOutput, out bool datamoshHasAudio))
                            {
                                Invoke((Action)(() =>
                                {
                                    MessageBox.Show(Resources.Strings.ConversionErrorMessage + datamoshOutput, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }));
                                return;
                            }

                            if (!datamoshHasAudio)
                            {
                                Invoke((Action)(() =>
                                {
                                    MessageBox.Show(Resources.Strings.DatamoshAudioMissingMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }));
                            }

                            pass1Args = null;
                            pass2Args = null;
                        }
                        else if (enableGlitchEffect)
                        {
                            var segments = glitchSegments ?? BuildDatamoshSegments(duration, datamoshPostSeed, glitchJumpSeconds, glitchChance);
                            string filterComplex = BuildDatamoshFilterComplex(videoResolution, audioBitDepth, audioSampleRate, videoFps, segments, out string vOutLabel, out string aOutLabel);
                            string rateControlArgs = outputFormat == "mp4"
                                ? $" -maxrate {currentVideoBitrate} -bufsize {currentVideoBitrate * 2}"
                                : "";

                            pass1Args = $"-i \"{inputFile}\" -filter_complex \"{filterComplex}\" -map \"{vOutLabel}\" -map \"{aOutLabel}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{rateControlArgs} -b:a {audioBitrate} -c:a {codecAudio} -ar {audioSampleRate} -preset veryfast{pixelFormatArg} -y \"{outputFileTemp}\"";
                            pass2Args = null;
                        }
                        else
                        {
                            passLogFileBase = CreatePassLogFileBase();
                            pass1Args = $"-i \"{inputFile}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{fpsArg}{videoFilterArg} -preset veryfast{pixelFormatArg} -pass 1 -passlogfile \"{passLogFileBase}\" -an -f null NUL";
                            pass2Args = $"-i \"{inputFile}\" -c:v {codecVideo} -b:v {currentVideoBitrate}{fpsArg}{videoFilterArg} -pass 2 -passlogfile \"{passLogFileBase}\" -b:a {audioBitrate} -c:a {codecAudio}{audioFilterArg} -ar {audioSampleRate} -preset veryfast{pixelFormatArg} -y \"{outputFileTemp}\"";
                        }
                        if (!string.IsNullOrWhiteSpace(pass1Args))
                        {
                            if (!RunFfmpegProcess(ffmpegPath, pass1Args, token, true, duration, out StringBuilder pass1Output))
                            {
                                Invoke((Action)(() =>
                                {
                                    MessageBox.Show(Resources.Strings.ConversionErrorMessage + pass1Output, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }));
                                return;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(pass2Args))
                        {
                            if (!RunFfmpegProcess(ffmpegPath, pass2Args, token, true, duration, out StringBuilder pass2Output))
                            {
                                Invoke((Action)(() =>
                                {
                                    MessageBox.Show(Resources.Strings.ConversionErrorMessage + pass2Output, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }));
                                return;
                            }
                        }

                        FileInfo fileInfo = new FileInfo(outputFileTemp);
                        if (fileInfo.Length <= targetSizeBytes)
                        {
                            string finalOutput = FinalizeOutputFile(outputFileTemp, outputFile);
                            conversionSuccess = true;
                            createdFile = finalOutput;
                        }
                        else
                        {
                            double sizeRatio = (double)targetSizeBytes / Math.Max(1L, fileInfo.Length);
                            currentVideoBitrate = (long)(currentVideoBitrate * sizeRatio * 0.95);
                        }
                    }
                    finally
                    {
                        CleanupPassLogFiles(passLogFileBase);
                        if (!conversionSuccess && File.Exists(outputFileTemp))
                        {
                            File.Delete(outputFileTemp);
                        }
                        TryDeleteFile(datamoshIntermediate);
                        TryDeleteFile(glitchIntermediate);
                    }

                } while (!conversionSuccess);

                if (conversionSuccess)
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show(Resources.Strings.ConversionSuccessMessage + createdFile, Resources.Strings.SuccessTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));
                }
            }
            catch (OperationCanceledException)
            {
                TryKillFfmpeg();
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:");
                Console.WriteLine(ex.ToString());

                Invoke((Action)(() =>
                {
                    MessageBox.Show(Resources.Strings.ConversionErrorMessage + ex.Message, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));

                TryKillFfmpeg();
            }
            finally
            {
                if (ffmpegProcess != null)
                {
                    ffmpegProcess.Dispose();
                    ffmpegProcess = null;
                }

                Invoke((Action)(() =>
                {
                    progressBar1.Value = 0;
                }));
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetDefaultSelections();
            UpdateSelectedFileLabel();
            UpdateGlitchControlsVisibility();
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
            button2.Visible = !isRunning;
            comboBoxFormat.Visible = !isRunning;
            progressBar1.Visible = isRunning;

            button1.Enabled = !isRunning;
            comboBoxFormat.Enabled = !isRunning;
            radioButton1.Enabled = !isRunning;
            radioButton3.Enabled = !isRunning;
            textBox1.Enabled = !isRunning;
            comboBoxLanguage.Enabled = !isRunning;
            comboBoxResolution.Enabled = !isRunning;
            comboBoxSampleRate.Enabled = !isRunning;
            comboBoxBitDepth.Enabled = !isRunning;
            comboBoxvideoFPS.Enabled = !isRunning;
            label2.Enabled = !isRunning;
            pictureBox1.Enabled = !isRunning;
            checkBoxSpeedEffect.Enabled = !isRunning;
            checkBoxDatamosh.Enabled = !isRunning;
            checkBoxGlitchEffect.Enabled = !isRunning;
            numericGlitchLength.Enabled = !isRunning;
            numericGlitchChance.Enabled = !isRunning;

            if (!isRunning)
            {
                progressBar1.Value = 0;
            }
        }

        private void UpdateSelectedFileLabel()
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                label1.Text = Resources.Strings.SelectedFileLabelText + Resources.Strings.NoFileSelectedText;
                return;
            }

            label1.Text = Resources.Strings.SelectedFileLabelText + Path.GetFileName(inputFile);
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
            string fpsValue = comboBoxvideoFPS.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(fpsValue))
            {
                fpsValue = comboBoxvideoFPS.Text;
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

            if (comboBoxvideoFPS.SelectedIndex < 0)
            {
                SetComboBoxValue(comboBoxvideoFPS, "60");
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

        private int SelectAudioBitrate(long targetBitrate)
        {
            int[] options = { 32000, 48000, 64000, 96000, 128000 };
            long desired = Math.Max(32000, Math.Min(128000, targetBitrate / 6));
            int closest = options[0];

            foreach (int option in options)
            {
                if (Math.Abs(option - desired) < Math.Abs(closest - desired))
                {
                    closest = option;
                }
            }

            return closest;
        }

        private string BuildAudioFilter(string audioBitDepth)
        {
            var filters = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(audioBitDepth))
            {
                filters.Add($"aformat=sample_fmts={audioBitDepth}");
            }

            filters.Add("anlmdn");
            return string.Join(",", filters);
        }

        private string BuildVideoFilter(string resolution)
        {
            if (!TryParseResolution(resolution, out int width, out int height))
            {
                return string.Empty;
            }

            width = (width / 2) * 2;
            height = (height / 2) * 2;

            if (width < 2 || height < 2)
            {
                return string.Empty;
            }

            return $"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2";
        }

        private string BuildSpeedFilterComplex(double duration, string resolution, string audioBitDepth, int outputAudioSampleRate, int inputAudioSampleRate, int videoFps, string extraVideoFilter, bool includeAudio, out string videoOutLabel, out string audioOutLabel)
        {
            var segments = BuildSpeedSegments(duration);
            var builder = new StringBuilder();

            string scaleFilter = BuildVideoFilter(resolution);

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                int adjustedRate = (int)Math.Round(inputAudioSampleRate * segment.Speed);
                if (adjustedRate < 1000)
                {
                    adjustedRate = 1000;
                }
                if (adjustedRate > 192000)
                {
                    adjustedRate = 192000;
                }

                double effectiveSpeed = inputAudioSampleRate > 0 ? adjustedRate / (double)inputAudioSampleRate : segment.Speed;

                builder.AppendFormat(CultureInfo.InvariantCulture, "[0:v]trim=start={0}:end={1},setpts=PTS-STARTPTS,setpts=PTS/{2}[v{3}];", segment.Start, segment.End, effectiveSpeed, i);

                if (includeAudio)
                {
                    builder.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "[0:a]atrim=start={0}:end={1},asetpts=PTS-STARTPTS,asetrate={2},aresample={3}[a{4}];",
                        segment.Start,
                        segment.End,
                        adjustedRate,
                        outputAudioSampleRate,
                        i);
                }
            }

            for (int i = 0; i < segments.Count; i++)
            {
                builder.AppendFormat("[v{0}]", i);
                if (includeAudio)
                {
                    builder.AppendFormat("[a{0}]", i);
                }
            }

            if (includeAudio)
            {
                builder.AppendFormat("concat=n={0}:v=1:a=1[vtmp][atmp];", segments.Count);
            }
            else
            {
                builder.AppendFormat("concat=n={0}:v=1:a=0[vtmp];", segments.Count);
            }

            var videoPostFilters = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(scaleFilter))
            {
                videoPostFilters.Add(scaleFilter);
            }
            if (videoFps > 0)
            {
                videoPostFilters.Add($"fps=fps={videoFps}");
            }
            if (!string.IsNullOrWhiteSpace(extraVideoFilter))
            {
                videoPostFilters.Add(extraVideoFilter);
            }

            if (videoPostFilters.Count > 0)
            {
                builder.AppendFormat("[vtmp]{0}[vout];", string.Join(",", videoPostFilters));
            }
            else
            {
                builder.Append("[vtmp]null[vout];");
            }

            if (includeAudio)
            {
                string audioPostFilter = BuildAudioPostFilter(audioBitDepth);
                builder.AppendFormat("[atmp]{0}[aout]", audioPostFilter);
                audioOutLabel = "[aout]";
            }
            else
            {
                audioOutLabel = null;
            }

            videoOutLabel = "[vout]";
            return builder.ToString().TrimEnd(';');
        }

        private string BuildAudioPostFilter(string audioBitDepth)
        {
            var filters = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(audioBitDepth))
            {
                filters.Add($"aformat=sample_fmts={audioBitDepth}");
            }

            filters.Add("anlmdn");
            return string.Join(",", filters);
        }

        private System.Collections.Generic.List<SpeedSegment> BuildSpeedSegments(double duration)
        {
            var segments = new System.Collections.Generic.List<SpeedSegment>();
            if (duration <= 0)
            {
                return segments;
            }

            const double minSpeed = 0.6;
            const double maxSpeed = 1.4;
            const double cycles = 2.0;

            double period = duration / cycles;
            if (period <= 0.0)
            {
                return segments;
            }

            int segmentCount = (int)Math.Round(duration * 2);
            if (segmentCount < 16)
            {
                segmentCount = 16;
            }
            if (segmentCount > 80)
            {
                segmentCount = 80;
            }

            double step = duration / segmentCount;
            double center = (minSpeed + maxSpeed) / 2.0;
            double amplitude = (maxSpeed - minSpeed) / 2.0;

            for (int i = 0; i < segmentCount; i++)
            {
                double start = i * step;
                double end = Math.Min(duration, (i + 1) * step);
                double mid = (start + end) / 2.0;
                double speed = center + amplitude * Math.Sin(2.0 * Math.PI * mid / period);

                if (speed < 0.1)
                {
                    speed = 0.1;
                }

                segments.Add(new SpeedSegment(start, end, speed));
            }

            return segments;
        }

        private bool RunTrueDatamoshPipeline(string ffmpegPath, string inputFile, string outputFileTemp, long videoBitrate, int audioBitrate, int audioSampleRate, string audioBitDepth, string videoResolution, int videoFps, double duration, int seed, CancellationToken token, out StringBuilder outputLog, out bool outputHasAudio)
        {
            outputLog = new StringBuilder();
            outputHasAudio = true;
            string tempDir = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
            Directory.CreateDirectory(tempDir);

            string rawVideoPath = Path.Combine(tempDir, $"datamosh_{Guid.NewGuid():N}.h264");
            string moshVideoPath = Path.Combine(tempDir, $"datamosh_{Guid.NewGuid():N}_mosh.h264");

            try
            {
                token.ThrowIfCancellationRequested();

                int gop = GetDatamoshGop(videoFps);
                string videoFilter = BuildVideoFilter(videoResolution);
                string videoFilterArg = string.IsNullOrWhiteSpace(videoFilter) ? "" : $" -vf \"{videoFilter}\"";
                string fpsArg = videoFps > 0 ? $" -r {videoFps}" : "";
                string x264Params = $"keyint={gop}:min-keyint=1:scenecut=40:open-gop=0";

                string videoArgs = $"-i \"{inputFile}\" -c:v libx264 -b:v {videoBitrate}{videoFilterArg}{fpsArg} -preset veryfast -pix_fmt yuv420p -bf 0 -x264-params \"{x264Params}\" -an -f h264 -y \"{rawVideoPath}\"";
                if (!RunFfmpegProcess(ffmpegPath, videoArgs, token, true, duration, out StringBuilder videoOutput))
                {
                    outputLog.Append(videoOutput);
                    return false;
                }

                token.ThrowIfCancellationRequested();

                var windows = BuildDatamoshWindows(duration, seed);
                if (!ApplyTrueDatamosh(rawVideoPath, moshVideoPath, windows, videoFps, seed, true, token, out string moshError))
                {
                    outputLog.Append(moshError);
                    return false;
                }

                token.ThrowIfCancellationRequested();

                string inputFpsArg = videoFps > 0 ? $" -r {videoFps}" : "";
                string audioFilter = BuildAudioFilter(audioBitDepth);
                string audioFilterArg = string.IsNullOrWhiteSpace(audioFilter) ? "" : $" -af \"{audioFilter}\"";
                string rateControlArgs = $" -maxrate {videoBitrate} -bufsize {videoBitrate * 2}";
                string remuxArgs = $"{inputFpsArg} -fflags +genpts -i \"{moshVideoPath}\" -i \"{inputFile}\" -map 0:v:0 -map 1:a:0? -c:v libx264 -b:v {videoBitrate}{rateControlArgs} -preset veryfast -pix_fmt yuv420p -bf 0 -c:a aac -b:a {audioBitrate}{audioFilterArg} -ar {audioSampleRate} -shortest -movflags +faststart -y \"{outputFileTemp}\"";
                if (!RunFfmpegProcess(ffmpegPath, remuxArgs, token, false, duration, out StringBuilder remuxOutput))
                {
                    outputLog.Append(remuxOutput);
                    return false;
                }

                outputHasAudio = ProbeHasAudio(ffmpegPath, outputFileTemp, out _);

                return true;
            }
            finally
            {
                TryDeleteFile(rawVideoPath);
                TryDeleteFile(moshVideoPath);
            }
        }

        private bool ProbeHasAudio(string ffmpegPath, string filePath, out string probeLog)
        {
            probeLog = string.Empty;
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = $"-i \"{filePath}\" -hide_banner",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();
                probeLog = output;
                return Regex.IsMatch(output, @"Audio:\s", RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                probeLog = "Probe failed: " + ex.Message;
                return false;
            }
        }

        private int GetDatamoshGop(int videoFps)
        {
            int fps = videoFps > 0 ? videoFps : 60;
            int gop = fps;
            if (gop < 15)
            {
                gop = 15;
            }
            if (gop > 120)
            {
                gop = 120;
            }
            return gop;
        }

        private bool ApplyTrueDatamosh(string inputPath, string outputPath, System.Collections.Generic.List<DatamoshWindow> windows, int videoFps, int seed, bool maxAggressive, CancellationToken token, out string error)
        {
            error = string.Empty;
            byte[] data;

            try
            {
                data = File.ReadAllBytes(inputPath);
            }
            catch (Exception ex)
            {
                error = "Failed to read datamosh source: " + ex.Message;
                return false;
            }

            var nalStarts = FindNalStartCodes(data);
            if (nalStarts.Count == 0)
            {
                error = "Datamosh failed: no NAL units found.";
                return false;
            }

            int fps = videoFps > 0 ? videoFps : 60;
            var rng = new System.Random(seed);
            int frameIndex = 0;
            int idrIndex = 0;
            int windowIndex = 0;
            int convertedIdr = 0;
            var idrHeaders = new System.Collections.Generic.List<int>();

            for (int i = 0; i < nalStarts.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                var current = nalStarts[i];
                int nextStart = i + 1 < nalStarts.Count ? nalStarts[i + 1].Index : data.Length;
                int headerIndex = current.Index + current.Length;
                if (headerIndex >= nextStart)
                {
                    continue;
                }

                byte nalHeader = data[headerIndex];
                int nalType = nalHeader & 0x1F;

                if (nalType == 1 || nalType == 5)
                {
                    double timeSeconds = frameIndex / (double)fps;
                    while (windowIndex < windows.Count && timeSeconds > windows[windowIndex].End)
                    {
                        windowIndex++;
                    }

                    bool inWindow = windowIndex < windows.Count && timeSeconds >= windows[windowIndex].Start && timeSeconds <= windows[windowIndex].End;
                    if (maxAggressive)
                    {
                        inWindow = true;
                    }

                    if (nalType == 5)
                    {
                        if (idrIndex > 0)
                        {
                            idrHeaders.Add(headerIndex);
                        }

                        bool shouldMosh = idrIndex > 0 && inWindow;
                        if (shouldMosh)
                        {
                            data[headerIndex] = (byte)((nalHeader & 0xE0) | 0x01);
                            convertedIdr++;
                        }

                        idrIndex++;
                    }

                    frameIndex++;
                }
            }

            if (convertedIdr == 0 && idrHeaders.Count > 0)
            {
                int pick = idrHeaders[rng.Next(idrHeaders.Count)];
                data[pick] = (byte)((data[pick] & 0xE0) | 0x01);
            }

            try
            {
                File.WriteAllBytes(outputPath, data);
                return true;
            }
            catch (Exception ex)
            {
                error = "Failed to write datamosh output: " + ex.Message;
                return false;
            }
        }

        private System.Collections.Generic.List<DatamoshWindow> BuildDatamoshWindows(double duration, int seed)
        {
            var windows = new System.Collections.Generic.List<DatamoshWindow>();
            if (duration <= 0)
            {
                return windows;
            }

            var rng = new System.Random(seed);
            double current = 0;

            while (current < duration)
            {
                double gap = 0.1 + rng.NextDouble() * 0.4;
                current += gap;
                if (current >= duration)
                {
                    break;
                }

                double length = 0.9 + rng.NextDouble() * 1.3;
                double end = Math.Min(duration, current + length);
                windows.Add(new DatamoshWindow(current, end));
                current = end;
            }

            if (windows.Count == 0 && duration > 0.2)
            {
                windows.Add(new DatamoshWindow(0.1, Math.Min(duration, 0.7)));
            }

            return windows;
        }

        private struct NalStart
        {
            public NalStart(int index, int length)
            {
                Index = index;
                Length = length;
            }

            public int Index { get; }
            public int Length { get; }
        }

        private System.Collections.Generic.List<NalStart> FindNalStartCodes(byte[] data)
        {
            var starts = new System.Collections.Generic.List<NalStart>();
            for (int i = 0; i < data.Length - 3; i++)
            {
                if (data[i] == 0x00 && data[i + 1] == 0x00 && data[i + 2] == 0x01)
                {
                    starts.Add(new NalStart(i, 3));
                    i += 2;
                    continue;
                }

                if (i < data.Length - 4 && data[i] == 0x00 && data[i + 1] == 0x00 && data[i + 2] == 0x00 && data[i + 3] == 0x01)
                {
                    starts.Add(new NalStart(i, 4));
                    i += 3;
                }
            }

            return starts;
        }

        private void TryDeleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error deleting temp file: " + ex.Message);
            }
        }

        private string BuildDatamoshVideoFilter(int seed)
        {
            return $"random=frames=30:seed={seed},tmix=frames=5:weights='1 1 1 1 1',tblend=all_mode=average";
        }

        private string BuildGlitchAudioFilter(int seed, int outputAudioSampleRate)
        {
            var rng = new System.Random(seed);
            int baseRate = outputAudioSampleRate > 0 ? outputAudioSampleRate : 44100;
            int glitchRate = Math.Max(4000, Math.Min(8000, baseRate / 2));
            if (glitchRate >= baseRate)
            {
                glitchRate = Math.Max(4000, baseRate / 2);
            }

            int highpass = 200 + rng.Next(0, 400);
            int lowpass = 3000 + rng.Next(0, 2500);
            double volume = 1.2 + rng.NextDouble() * 0.5;

            return $"aresample={glitchRate},aresample={baseRate},highpass=f={highpass},lowpass=f={lowpass},volume={volume.ToString(CultureInfo.InvariantCulture)}";
        }

        private string BuildDatamoshFilterComplex(string resolution, string audioBitDepth, int outputAudioSampleRate, int videoFps, System.Collections.Generic.List<DatamoshSegment> segments, out string videoOutLabel, out string audioOutLabel)
        {
            if (segments == null || segments.Count == 0)
            {
                videoOutLabel = "[vout]";
                audioOutLabel = "[aout]";
                return "[0:v]null[vout];[0:a]anull[aout]";
            }

            var builder = new StringBuilder();
            string scaleFilter = BuildVideoFilter(resolution);

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                string videoFilters = $"trim=start={segment.SourceStart.ToString(CultureInfo.InvariantCulture)}:end={(segment.SourceStart + (segment.End - segment.Start)).ToString(CultureInfo.InvariantCulture)},setpts=PTS-STARTPTS";
                if (segment.ApplyEffect)
                {
                    videoFilters += "," + BuildDatamoshVideoFilter(segment.Seed);
                }

                builder.AppendFormat(CultureInfo.InvariantCulture, "[0:v]{0}[v{1}];", videoFilters, i);

                string audioFilters = $"atrim=start={segment.SourceStart.ToString(CultureInfo.InvariantCulture)}:end={(segment.SourceStart + (segment.End - segment.Start)).ToString(CultureInfo.InvariantCulture)},asetpts=PTS-STARTPTS";
                builder.AppendFormat(CultureInfo.InvariantCulture, "[0:a]{0}[a{1}];", audioFilters, i);
            }

            for (int i = 0; i < segments.Count; i++)
            {
                builder.AppendFormat("[v{0}][a{0}]", i);
            }

            builder.AppendFormat("concat=n={0}:v=1:a=1[vtmp][atmp];", segments.Count);

            var videoPostFilters = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(scaleFilter))
            {
                videoPostFilters.Add(scaleFilter);
            }
            if (videoFps > 0)
            {
                videoPostFilters.Add($"fps=fps={videoFps}");
            }

            if (videoPostFilters.Count > 0)
            {
                builder.AppendFormat("[vtmp]{0}[vout];", string.Join(",", videoPostFilters));
            }
            else
            {
                builder.Append("[vtmp]null[vout];");
            }

            string audioPostFilter = BuildAudioPostFilter(audioBitDepth);
            builder.AppendFormat("[atmp]{0}[aout]", audioPostFilter);

            videoOutLabel = "[vout]";
            audioOutLabel = "[aout]";
            return builder.ToString().TrimEnd(';');
        }

        private System.Collections.Generic.List<DatamoshSegment> BuildDatamoshSegments(double duration, int seed, double jumpLengthSeconds, double glitchChance)
        {
            var segments = new System.Collections.Generic.List<DatamoshSegment>();
            if (duration <= 0)
            {
                return segments;
            }

            var rng = new System.Random(seed);
            double baseLength = jumpLengthSeconds;
            if (baseLength <= 0)
            {
                baseLength = duration / 25.0;
            }
            if (baseLength < 0.2)
            {
                baseLength = 0.2;
            }
            if (baseLength > duration)
            {
                baseLength = duration;
            }

            double chance = glitchChance;
            if (chance < 0.0)
            {
                chance = 0.0;
            }
            if (chance > 1.0)
            {
                chance = 1.0;
            }

            double current = 0;
            while (current < duration)
            {
                double jitter = 0.8 + rng.NextDouble() * 0.9;
                double length = Math.Min(duration - current, baseLength * jitter);
                if (length < 0.1)
                {
                    length = Math.Min(duration - current, 0.1);
                }

                bool applyEffect = rng.NextDouble() < chance;
                int segmentSeed = rng.Next(1, int.MaxValue);
                double sourceStart = current;
                if (applyEffect)
                {
                    double maxStart = Math.Max(0.0, duration - length);
                    sourceStart = rng.NextDouble() * maxStart;
                    if (Math.Abs(sourceStart - current) < Math.Min(0.2, length * 0.2))
                    {
                        double offset = Math.Min(length, maxStart);
                        sourceStart = Math.Min(maxStart, sourceStart + offset);
                    }
                }

                segments.Add(new DatamoshSegment(current, current + length, sourceStart, applyEffect, segmentSeed));
                current += length;
            }

            bool hasEffect = false;
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].ApplyEffect)
                {
                    hasEffect = true;
                    break;
                }
            }

            if (!hasEffect && segments.Count > 0 && chance > 0.0)
            {
                int index = rng.Next(segments.Count);
                DatamoshSegment segment = segments[index];
                double length = segment.End - segment.Start;
                double maxStart = Math.Max(0.0, duration - length);
                double sourceStart = rng.NextDouble() * maxStart;
                if (Math.Abs(sourceStart - segment.Start) < Math.Min(0.2, length * 0.2))
                {
                    double offset = Math.Min(length, maxStart);
                    sourceStart = Math.Min(maxStart, sourceStart + offset);
                }
                segments[index] = new DatamoshSegment(segment.Start, segment.End, sourceStart, true, rng.Next(1, int.MaxValue));
            }

            return segments;
        }

        private bool TryParseResolution(string resolution, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (string.IsNullOrWhiteSpace(resolution))
            {
                return false;
            }

            string[] parts = resolution.ToLowerInvariant().Split('x');
            if (parts.Length != 2)
            {
                return false;
            }

            return int.TryParse(parts[0], out width) && int.TryParse(parts[1], out height);
        }

        private string CreatePassLogFileBase()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "DiscordVideoCompressor");
            Directory.CreateDirectory(tempDir);
            return Path.Combine(tempDir, $"ffmpeg2pass_{Guid.NewGuid():N}");
        }

        private void CleanupPassLogFiles(string passLogFileBase)
        {
            if (string.IsNullOrWhiteSpace(passLogFileBase))
            {
                return;
            }

            string directory = Path.GetDirectoryName(passLogFileBase);
            string baseName = Path.GetFileName(passLogFileBase);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseName))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(directory, baseName + "*"))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error deleting pass log file: " + ex.Message);
                }
            }
        }

        private string FinalizeOutputFile(string tempFile, string outputFile)
        {
            try
            {
                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }

                File.Move(tempFile, outputFile);
                return outputFile;
            }
            catch (IOException ex)
            {
                Debug.WriteLine("Output file in use, creating alternate name: " + ex.Message);
                string directory = Path.GetDirectoryName(outputFile);
                string baseName = Path.GetFileNameWithoutExtension(outputFile);
                string extension = Path.GetExtension(outputFile);
                string suffix = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                string alternate = Path.Combine(directory, baseName + "_" + suffix + extension);

                File.Move(tempFile, alternate);
                return alternate;
            }
        }

        private bool RunFfmpegProcess(string ffmpegPath, string arguments, CancellationToken token, bool captureProgress, double duration, out StringBuilder stdErrOutput)
        {
            stdErrOutput = new StringBuilder();

            ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardError = true,
                    RedirectStandardOutput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                ffmpegProcess.Start();

                Regex timeRegex = captureProgress ? new Regex(@"time=(\d+):(\d+):(\d+\.?\d*)") : null;
                string stdErrLine;

                while ((stdErrLine = ffmpegProcess.StandardError.ReadLine()) != null)
                {
                    token.ThrowIfCancellationRequested();
                    stdErrOutput.AppendLine(stdErrLine);

                    if (captureProgress)
                    {
                        Match timeMatch = timeRegex.Match(stdErrLine);
                        if (timeMatch.Success)
                        {
                            int hoursCurrent = int.Parse(timeMatch.Groups[1].Value);
                            int minutesCurrent = int.Parse(timeMatch.Groups[2].Value);
                            if (!double.TryParse(timeMatch.Groups[3].Value.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out double secondsCurrent))
                            {
                                secondsCurrent = 0;
                            }

                            double currentDuration = hoursCurrent * 3600 + minutesCurrent * 60 + secondsCurrent;
                            double progress = Math.Min(100, currentDuration / duration * 100);

                            Invoke((Action)(() =>
                            {
                                progressBar1.Value = (int)progress;
                            }));
                        }
                    }
                }

                ffmpegProcess.WaitForExit();
                return ffmpegProcess.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                TryKillFfmpeg();
                throw;
            }
            finally
            {
                if (ffmpegProcess != null)
                {
                    ffmpegProcess.Dispose();
                    ffmpegProcess = null;
                }
            }
        }

        private void CleanupFfmpeg()
        {
            if (!string.IsNullOrEmpty(extractedFfmpegPath) && File.Exists(extractedFfmpegPath))
            {
                File.Delete(extractedFfmpegPath);
                extractedFfmpegPath = null;
            }
        }

        private void TryKillFfmpeg()
        {
            if (ffmpegProcess == null)
            {
                return;
            }

            try
            {
                if (!ffmpegProcess.HasExited)
                {
                    ffmpegProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error killing ffmpeg: " + ex.Message);
            }
        }

        private void comboBoxvideoFPS_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBoxResolution_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
