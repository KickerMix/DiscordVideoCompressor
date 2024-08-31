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

        private string ExtractFfmpeg()
        {
            string ffmpegPath = Path.Combine(Path.GetTempPath(), "ffmpeg.exe");

            if (!File.Exists(ffmpegPath))
            {
                try
                {
                    File.WriteAllBytes(ffmpegPath, Properties.Resources.ffmpeg);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Resources.Strings.FfmpegExtractionErrorMessage + "\n" + ex.Message, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1); // Exit with error
                }
            }

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
            comboBoxLanguage.SelectedIndex = 1;

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
                    string fileName = Path.GetFileName(inputFile);
                    label1.Text = Resources.Strings.SelectedFileLabelText + ": " + fileName;
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
            Debug.WriteLine("EN");
            switch (selectedLanguage)
            {
                case "EN":
                    SetLanguage("en");
                    Debug.WriteLine("EN");
                    break;
                case "RU":
                    SetLanguage("ru");
                    Debug.WriteLine("RU");
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
            label1.Text = Resources.Strings.SelectedFileLabelText;

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
                openFileDialog.Filter = "Video Files (*.mp4;*.avi;*.mkv;*.webm)|*.mp4;*.avi;*.mkv;*.webm)";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    inputFile = openFileDialog.FileName;
                    string fileName = Path.GetFileName(inputFile);
                    label1.Text = Resources.Strings.SelectedFileLabelText + ": " + fileName;
                }
            }
        }

        private async void button2_Click(object sender, EventArgs e) // Convertation
        {
            // Hide button "Convertation"
            button2.Visible = false;
            comboBoxFormat.Visible = false;
            progressBar1.Visible = true;

            if (string.IsNullOrEmpty(inputFile))
            {
                MessageBox.Show(Resources.Strings.SelectMediaFileMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                button2.Visible = true; // Show button if error
                comboBoxFormat.Visible = true;
                progressBar1.Visible = false;
                return;
            }

            long targetSizeBytes = 0;
            long presetSizeMB = 0;

            if (radioButton1.Checked) // Discord 24MB preset
            {
                presetSizeMB = 24;
                targetSizeBytes = presetSizeMB * 1024 * 1024;
            }
            else if (radioButton3.Checked) // Custom size preset
            {
                if (long.TryParse(textBox1.Text, out long customSizeMB) && customSizeMB > 0)
                {
                    presetSizeMB = customSizeMB;
                    targetSizeBytes = presetSizeMB * 1024 * 1024;
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

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            try
            {
                progressBar1.Value = 0;
                await Task.Run(() => ConvertFile(inputFile, targetSizeBytes, token), token);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(Resources.Strings.ConversionCanceledMessage, Resources.Strings.MessageTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                progressBar1.Value = 0; // Reset progression after finish
                button2.Visible = true; // Show button "Convertation" again
                comboBoxFormat.Visible = true;
                progressBar1.Visible = false;
            }
        }

        private void button3_Click(object sender, EventArgs e) // Forse stop
        {
            cancellationTokenSource?.Cancel();

            // Force stop ffmpeg, if it's running
            if (ffmpegProcess != null && !ffmpegProcess.HasExited)
            {
                ffmpegProcess.Kill();
            }
        }

        private bool IsFfmpegAvailableInPath()
        {
            try
            {
                // Run ffmpeg -version for exist
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private bool IsFfmpegAvailableInAppDirectory()
        {
            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            return File.Exists(ffmpegPath);
        }

        private string GetFfmpegPath()
        {
            return ExtractFfmpeg();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                string ffmpegPath = ExtractFfmpeg();
                if (File.Exists(ffmpegPath))
                {
                    File.Delete(ffmpegPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error deleting ffmpeg: " + ex.Message);
            }
        }

        private void ConvertFile(string inputFile, long targetSizeBytes, CancellationToken token)
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

                // Use Invoke, to get UI
                string outputFormat = "";
                long presetSizeMB = 0;
                Invoke((Action)(() =>
                {
                    outputFormat = comboBoxFormat.SelectedItem.ToString().ToLower();
                    // Get size to set presetSizeMB
                    if (radioButton1.Checked)
                    {
                        presetSizeMB = 24;
                    }
                    else if (radioButton3.Checked)
                    {
                        if (long.TryParse(textBox1.Text, out long customSizeMB) && customSizeMB > 0)
                        {
                            presetSizeMB = customSizeMB;
                        }
                    }
                }));

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

                do
                {
                    token.ThrowIfCancellationRequested();

                    pass++;
                    if (pass > 5)
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show(Resources.Strings.SelectSizeOptionMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }));
                        return;
                    }

                    // Run ffmpeg
                    ffmpegProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = ffmpegPath,
                            Arguments = $"-i \"{inputFile}\" -hide_banner",
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    ffmpegProcess.Start();
                    string output = ffmpegProcess.StandardError.ReadToEnd();
                    ffmpegProcess.WaitForExit();

                    Regex regex = new Regex(@"Duration:\s(\d+):(\d+):(\d+\.?\d*)");
                    Match match = regex.Match(output);

                    if (!match.Success)
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show(Resources.Strings.FileNotExistError, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    int hours = int.Parse(match.Groups[1].Value);
                    int minutes = int.Parse(match.Groups[2].Value);
                    if (!double.TryParse(match.Groups[3].Value.Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double seconds))
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show(Resources.Strings.VideoDurationErrorMessage + "\n" + output, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    double duration = hours * 3600 + minutes * 60 + seconds;

                    if (duration <= 0)
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show(Resources.Strings.TimeConversionErrorMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    long targetBitrate = (long)((targetSizeBytes * 8) / duration);
                    int audioBitrate = 128 * 1024; // 128 kbps for audio
                    long videoBitrate = targetBitrate - audioBitrate;

                    if (videoBitrate <= 0)
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show(Resources.Strings.VideoDurationErrorMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    string codecVideo = "";
                    string codecAudio = "";
                    string fileExtension = "";

                    if (outputFormat == "webm")
                    {
                        codecVideo = "libvpx-vp9";
                        codecAudio = "libopus";
                        fileExtension = "webm";
                    }
                    else if (outputFormat == "mp4")
                    {
                        codecVideo = "libx264";
                        codecAudio = "aac";
                        fileExtension = "mp4";
                    }

                    ffmpegProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = ffmpegPath,
                            Arguments = $"-i \"{inputFile}\" -b:v {videoBitrate} -b:a {audioBitrate} -c:v {codecVideo} -c:a {codecAudio} -preset veryfast -threads 6 -y -fs {presetSizeMB}M \"{outputFile}\"",
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    Debug.WriteLine($"Execution ffmpeg: {ffmpegPath} -i \"{inputFile}\" -b:v {videoBitrate} -b:a {audioBitrate} -c:v {codecVideo} -c:a {codecAudio} -preset veryfast -threads 6 -y -fs {presetSizeMB}M \"{outputFile}\"");

                    ffmpegProcess.Start();

                    string stdErrLine;
                    Regex timeRegex = new Regex(@"time=(\d+):(\d+):(\d+\.?\d*)");

                    while ((stdErrLine = ffmpegProcess.StandardError.ReadLine()) != null)
                    {
                        token.ThrowIfCancellationRequested();

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

                    if (ffmpegProcess.ExitCode != 0)
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show($"{Resources.Strings.ConversionErrorMessage}: {ffmpegProcess.StandardError.ReadToEnd()}", Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        targetBitrate = (long)(targetBitrate / 1.1); // lowering bitrate for next try
                    }

                } while (!conversionSuccess);

                if (conversionSuccess)
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show($"{Resources.Strings.ConversionSuccessMessage}: {createdFile}", Resources.Strings.SuccessTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));
                }
            }
            catch (OperationCanceledException)
            {
                Invoke((Action)(() =>
                {
                    MessageBox.Show(Resources.Strings.ConversionCanceledMessage, Resources.Strings.MessageTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            catch (Exception ex)
            {
                Console.WriteLine("Exception:");
                Console.WriteLine(ex.ToString());

                Invoke((Action)(() =>
                {
                    MessageBox.Show(Resources.Strings.BitrateErrorMessage, Resources.Strings.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Invoke((Action)(() =>
                {
                    progressBar1.Value = 0;
                }));
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            comboBoxLanguage.SelectedIndex = 0;
        }
    }
}
