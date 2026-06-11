using System;
using System.Globalization;
using System.Windows.Forms;

namespace DiscordVideoCompressor
{
    public partial class Form1
    {
        private void ResetProgressStatus()
        {
            progressStageName = Resources.Strings.ProgressStageIdleText;
            progressStageStart = 0;
            progressStageSpan = 1;
            progressStageDuration = 0;
            progressStageHasProgress = false;
            progressLastPercent = 0;
            progressLastSpeed = 0;
            progressCurrentSeconds = 0;
            progressTotalSeconds = 0;
            progressEtaSeconds = null;

            ApplyProgressText();
        }

        private void SetProgressStage(string stageName, double stageStart, double stageSpan, bool hasProgress, double stageDuration)
        {
            PostToUi(() =>
            {
                progressStageName = stageName;
                progressStageStart = Math.Max(0.0, Math.Min(1.0, stageStart));
                progressStageSpan = Math.Max(0.0, Math.Min(1.0, stageSpan));
                progressStageHasProgress = hasProgress;
                progressStageDuration = stageDuration;
                progressCurrentSeconds = 0;
                progressTotalSeconds = hasProgress ? stageDuration : 0;
                progressEtaSeconds = null;
                progressLastSpeed = 0;
                progressLastPercent = progressStageStart * 100.0;
                progressBar1.Style = hasProgress ? ProgressBarStyle.Continuous : ProgressBarStyle.Marquee;
                if (hasProgress)
                {
                    progressBar1.Value = (int)Math.Max(0, Math.Min(100, progressLastPercent));
                }

                ApplyProgressText();
            });
        }

        private void UpdateProgress(double currentSeconds, double? speedValue)
        {
            PostToUi(() =>
            {
                if (!progressStageHasProgress || progressStageDuration <= 0)
                {
                    return;
                }

                double stageProgress = Math.Max(0.0, Math.Min(1.0, currentSeconds / progressStageDuration));
                double overallProgress = progressStageStart + stageProgress * progressStageSpan;
                double percent = Math.Max(progressLastPercent, overallProgress * 100.0);
                progressLastPercent = percent;
                progressCurrentSeconds = Math.Min(currentSeconds, progressStageDuration);
                progressTotalSeconds = progressStageDuration;

                if (speedValue.HasValue && speedValue.Value > 0)
                {
                    progressLastSpeed = speedValue.Value;
                }

                if (progressLastSpeed > 0)
                {
                    double remaining = Math.Max(0.0, progressStageDuration - currentSeconds);
                    progressEtaSeconds = remaining / progressLastSpeed;
                }
                else
                {
                    progressEtaSeconds = null;
                }

                progressBar1.Value = (int)Math.Max(0, Math.Min(100, percent));
                ApplyProgressText();
            });
        }

        private void PostToUi(Action action)
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
            {
                return;
            }

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (InvalidOperationException)
            {
                // The window was closed between the lifecycle check and BeginInvoke.
            }
        }

        private void ApplyProgressText()
        {
            if (statusLabelStage == null || statusLabelTime == null || statusLabelSpeed == null)
            {
                return;
            }

            string stageText = string.IsNullOrWhiteSpace(progressStageName)
                ? Resources.Strings.ProgressStageIdleText
                : progressStageName;
            statusLabelStage.Text = $"{Resources.Strings.ProgressStageLabelText} {stageText}";
            statusLabelTime.Text = FormatProgressTime(progressCurrentSeconds, progressTotalSeconds, progressEtaSeconds);

            if (progressLastSpeed > 0)
            {
                statusLabelSpeed.Text = $"{Resources.Strings.ProgressSpeedLabelText}: {progressLastSpeed.ToString("0.0", CultureInfo.InvariantCulture)}x";
            }
            else
            {
                statusLabelSpeed.Text = $"{Resources.Strings.ProgressSpeedLabelText}: --";
            }
        }

        private string FormatProgressTime(double currentSeconds, double totalSeconds, double? etaSeconds)
        {
            if (totalSeconds <= 0)
            {
                return Resources.Strings.ProgressTimeIdleText;
            }

            TimeSpan current = TimeSpan.FromSeconds(Math.Max(0.0, currentSeconds));
            TimeSpan total = TimeSpan.FromSeconds(Math.Max(0.0, totalSeconds));
            string text = $"{FormatTimeSpan(current)} / {FormatTimeSpan(total)}";

            if (etaSeconds.HasValue)
            {
                TimeSpan eta = TimeSpan.FromSeconds(Math.Max(0.0, etaSeconds.Value));
                text += $" • {Resources.Strings.ProgressEtaLabelText} {FormatTimeSpan(eta)}";
            }

            return text;
        }

        private string FormatTimeSpan(TimeSpan span)
        {
            if (span.TotalHours >= 1)
            {
                return span.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture);
            }

            return span.ToString("mm\\:ss", CultureInfo.InvariantCulture);
        }
    }
}
