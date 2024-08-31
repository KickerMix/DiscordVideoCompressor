using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private string inputFile;
        private string createdFile;
        private CancellationTokenSource cancellationTokenSource;
        private Process ffmpegProcess;


        public Form1()
        {
            InitializeComponent();

            // Загрузка изображения в PictureBox
            pictureBox1.Image = Properties.Resources.logo; // logo init
        }

        private void button1_Click(object sender, EventArgs e) // Выбрать медиа файл
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Video Files (*.mp4;*.avi;*.mkv)|*.mp4;*.avi;*.mkv";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    inputFile = openFileDialog.FileName;
                    string fileName = Path.GetFileName(inputFile);
                    label1.Text = "Выбранный файл: " + fileName;
                }
            }
        }

        private async void button2_Click(object sender, EventArgs e) // Конвертировать
        {
            // Скрываем кнопку "Конвертировать"
            button2.Visible = false;
            comboBoxFormat.Visible = false;
            progressBar1.Visible = true;

            if (string.IsNullOrEmpty(inputFile))
            {
                MessageBox.Show("Выберите медиа файл.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                button2.Visible = true; // Показываем кнопку снова, если была ошибка
                comboBoxFormat.Visible = true;
                progressBar1.Visible = false;

                return;
            }

            long targetSizeBytes = 0;
            long presetSizeMB = 0;

            if (radioButton1.Checked) // Discord 24MB
            {
                presetSizeMB = 24;
                targetSizeBytes = presetSizeMB * 1024 * 1024;
            }
            else if (radioButton3.Checked) // Custom Size
            {
                if (long.TryParse(textBox1.Text, out long customSizeMB) && customSizeMB > 0)
                {
                    presetSizeMB = customSizeMB;
                    targetSizeBytes = presetSizeMB * 1024 * 1024;
                }
                else
                {
                    MessageBox.Show("Введите корректный размер в MB (положительное число).", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                MessageBox.Show("Выберите опцию размера.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            createdFile = null; // Сброс пути к созданному файлу

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            try
            {
                progressBar1.Value = 0;
                await Task.Run(() => ConvertFile(inputFile, targetSizeBytes, token), token);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Конвертация была отменена.", "Сообщение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                progressBar1.Value = 0; // Сброс прогресса после завершения
                button2.Visible = true; // Показываем кнопку "Конвертировать" снова
                comboBoxFormat.Visible = true;
                progressBar1.Visible = false;
            }
        }



        private void button3_Click(object sender, EventArgs e) // Принудительно завершить
        {
            cancellationTokenSource?.Cancel();

            // Завершение процесса ffmpeg, если он запущен
            if (ffmpegProcess != null && !ffmpegProcess.HasExited)
            {
                ffmpegProcess.Kill();
            }
        }

        private bool IsFfmpegAvailableInPath()
        {
            try
            {
                // Запускаем команду ffmpeg -version для проверки доступности
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
            if (IsFfmpegAvailableInPath())
            {
                return "ffmpeg";
            }
            else if (IsFfmpegAvailableInAppDirectory())
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            }
            else
            {
                // Показать сообщение об ошибке и выйти
                MessageBox.Show("Не найден ffmpeg. Пожалуйста, убедитесь, что ffmpeg установлен и доступен в PATH или находится в той же директории, что и ваше приложение.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1); // Завершаем приложение с ошибкой
                return null;
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
                        MessageBox.Show("Файл не существует.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }

                // Используем Invoke, чтобы получить значение в UI-потоке
                string outputFormat = "";
                long presetSizeMB = 0;
                string audioSampleRate = "";
                Invoke((Action)(() =>
                {
                    outputFormat = comboBoxFormat.SelectedItem.ToString().ToLower();
                    // Получаем размер для записи в переменную presetSizeMB
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

                Invoke((Action)(() =>
                {
                    // Получаем значение из comboBoxSampleRate
                    if (comboBoxSampleRate.SelectedItem != null)
                    {
                        audioSampleRate = comboBoxSampleRate.SelectedItem.ToString();
                    }
                }));

                // Изменяем имя выходного файла, добавляя _cnvrtd, если формат MP4
                string outputFile = Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile) +
                    (outputFormat == "mp4" ? "_cnvrtd" : "") + $".{outputFormat}");

                bool conversionSuccess = false;
                int pass = 0;

                string ffmpegPath = GetFfmpegPath();
                if (ffmpegPath == null)
                {
                    // ffmpeg не найден, не продолжаем конвертацию
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
                            MessageBox.Show("Не удалось добиться желаемого размера файла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    // Запуск процесса ffmpeg
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
                            MessageBox.Show("Не удалось определить продолжительность видео. Вывод ffmpeg:\n" + output, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    int hours = int.Parse(match.Groups[1].Value);
                    int minutes = int.Parse(match.Groups[2].Value);
                    if (!double.TryParse(match.Groups[3].Value.Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double seconds))
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show("Ошибка при преобразовании времени. Проверьте формат строки времени.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    double duration = hours * 3600 + minutes * 60 + seconds;

                    if (duration <= 0)
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show("Продолжительность видео некорректна.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }

                    long targetBitrate = (long)((targetSizeBytes * 8) / duration);
                    int audioBitrate = 128 * 1024; // 128 kbps для аудио
                    long videoBitrate = targetBitrate - audioBitrate;

                    if (videoBitrate <= 0)
                    {
                        Invoke((Action)(() =>
                        {
                            MessageBox.Show("Целевой размер слишком мал для данного видео.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                            Arguments = $"-i \"{inputFile}\" -b:v {videoBitrate} -b:a {audioBitrate} -ar {audioSampleRate} -c:v {codecVideo} -c:a {codecAudio} -preset medium -threads 6 -y -fs {presetSizeMB}M \"{outputFile}\"",
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    Debug.WriteLine($"Запуск команды: {ffmpegPath} -i \"{inputFile}\" -b:v {videoBitrate} -b:a {audioBitrate} -ar {audioSampleRate} -c:v {codecVideo} -c:a {codecAudio} -preset medium -threads 6 -y -fs {presetSizeMB}M \"{outputFile}\"");

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
                            MessageBox.Show($"Ошибка при конвертации: {ffmpegProcess.StandardError.ReadToEnd()}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        targetBitrate = (long)(targetBitrate / 1.1); // Уменьшаем битрейт для следующей попытки
                    }

                } while (!conversionSuccess);

                if (conversionSuccess)
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show($"Конвертация завершена: {createdFile}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));
                }
            }
            catch (OperationCanceledException)
            {
                Invoke((Action)(() =>
                {
                    MessageBox.Show("Конвертация была отменена.", "Сообщение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));

                if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                {
                    try
                    {
                        ffmpegProcess.Kill();
                    }
                    catch (Exception killEx)
                    {
                        Console.WriteLine($"Ошибка при завершении процесса: {killEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:");
                Console.WriteLine(ex.ToString());

                Invoke((Action)(() =>
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));

                if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                {
                    try
                    {
                        ffmpegProcess.Kill();
                    }
                    catch (Exception killEx)
                    {
                        Console.WriteLine($"Ошибка при завершении процесса: {killEx.Message}");
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

        private void Form1_Load(object sender, EventArgs e)
        {
            // Любая инициализация может быть выполнена здесь
        }

        private void comboBoxSampleRate_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
