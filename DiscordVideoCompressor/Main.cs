using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DiscordVideoCompressor
{
    public partial class Main : Form
    {
        private string inputFile;
        private string createdFile;
        private CancellationTokenSource cancellationTokenSource;
        private Process ffmpegProcess;
        private string extractedFfmpegPath;

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

        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public Main()
        {
            InitializeComponent();
            this.FormClosing += Main_FormClosing;

            // Enable dark mode if system theme is dark
            EnableDarkMode(this.Handle);

            // Loading logo in PictureBox
            pictureBox1.Image = Properties.Resources.logo; // Logo init

            comboBoxLanguage.Items.AddRange(new string[] { "EN", "RU" });
            comboBoxLanguage.SelectedIndex = 0;
            comboBoxFormat.SelectedIndex = 0;

            SetLanguage("en");

            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;
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
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value != null && (int)value == 0;
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
                string extension = Path.GetExtension(file).ToLower();

                // Проверка допустимых форматов файлов
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
            string selectedLanguage = comboBoxLanguage.SelectedItem.ToString();
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
            // Updating UI Text
            button1.Text = Resources.Strings.ChooseMediaFileButtonText;
            button2.Text = Resources.Strings.ConvertationButtonText;
            button3.Text = Resources.Strings.ForceStopButtonText;
            UpdateSelectedFileLabel();

            // Force Update
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

        private async void button2_Click(object sender, EventArgs e) // Convertation
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

            createdFile = null; // Reset path to created file

            SetConversionUiState(true);

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            try
            {
                progressBar1.Value = 0;
                await Task.Run(() => ConvertFile(inputFile, targetSizeBytes, outputFormat, token), token);
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

        private void button3_Click(object sender, EventArgs e) // Forse stop
        {
            cancellationTokenSource?.Cancel();

            // Force stop ffmpeg, if it's running
            TryKillFfmpeg();
        }

        private string GetFfmpegPath()
        {
            return ExtractFfmpeg();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
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

        private void ConvertFile(string inputFile, long targetSizeBytes, string outputFormat, CancellationToken token)
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
                    // no ffmpeg found, stop convertation
                    return;
                }

                int maxPasses = 5;
                long targetBitrate = 0;
                int audioBitrate = 128 * 1024; // 128 kbps for audio
                long videoBitrate = 0;

                double duration = 0;
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
                    if (!double.TryParse(match.Groups[3].Value.Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double seconds))
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show(Resources.Strings.TimeConversionErrorMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    duration = hours * 3600 + minutes * 60 + seconds;
                }

                if (duration <= 0)
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show(Resources.Strings.VideoDurationErrorMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }

                targetBitrate = (long)((targetSizeBytes * 8) / duration);
                videoBitrate = targetBitrate - audioBitrate;

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

                    ffmpegProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = ffmpegPath,
                            Arguments = $"-i \"{inputFile}\" -b:v {currentVideoBitrate} -b:a {audioBitrate} -c:v {codecVideo} -c:a {codecAudio} -preset veryfast -threads 6 -y \"{outputFile}\"",
                            RedirectStandardError = true,
                            RedirectStandardOutput = false,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    Debug.WriteLine($"Execution ffmpeg: {ffmpegPath} -i \"{inputFile}\" -b:v {currentVideoBitrate} -b:a {audioBitrate} -c:v {codecVideo} -c:a {codecAudio} -preset veryfast -threads 6 -y \"{outputFile}\"");

                    ffmpegProcess.Start();

                    string stdErrLine;
                    Regex timeRegex = new Regex(@"time=(\d+):(\d+):(\d+\.?\d*)");
                    var stdErrOutput = new System.Text.StringBuilder();

                    while ((stdErrLine = ffmpegProcess.StandardError.ReadLine()) != null)
                    {
                        token.ThrowIfCancellationRequested();
                        stdErrOutput.AppendLine(stdErrLine);

                        Match timeMatch = timeRegex.Match(stdErrLine);
                        if (timeMatch.Success)
                        {
                            int hoursCurrent = int.Parse(timeMatch.Groups[1].Value);
                            int minutesCurrent = int.Parse(timeMatch.Groups[2].Value);
                            if (!double.TryParse(timeMatch.Groups[3].Value.Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double secondsCurrent))
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

                    ffmpegProcess.WaitForExit();
                    int exitCode = ffmpegProcess.ExitCode;
                    ffmpegProcess.Dispose();
                    ffmpegProcess = null;

                    if (exitCode != 0)
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show(Resources.Strings.ConversionErrorMessage + stdErrOutput, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    FileInfo fileInfo = new FileInfo(outputFile);
                    if (fileInfo.Length <= targetSizeBytes)
                    {
                        conversionSuccess = true;
                        createdFile = outputFile;
                    }
                    else
                    {
                        double sizeRatio = (double)targetSizeBytes / Math.Max(1L, fileInfo.Length);
                        currentVideoBitrate = (long)(currentVideoBitrate * sizeRatio * 0.95);
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

                if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                {
                    try
                    {
                        ffmpegProcess.Kill();
                    }
                    catch (Exception killEx)
                    {
                        Console.WriteLine($"Error kill process: {killEx.Message}");
                    }
                }
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

        private void Main_Load(object sender, EventArgs e)
        {
            if (comboBoxLanguage.SelectedIndex < 0)
            {
                comboBoxLanguage.SelectedIndex = 0;
            }

            if (comboBoxFormat.SelectedIndex < 0)
            {
                comboBoxFormat.SelectedIndex = 0;
            }

            UpdateSelectedFileLabel();
        }

        private void SetConversionUiState(bool isRunning)
        {
            button2.Visible = !isRunning;
            comboBoxFormat.Visible = !isRunning;
            progressBar1.Visible = isRunning;

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
    }
}
