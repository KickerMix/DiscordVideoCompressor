namespace DiscordVideoCompressor
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора - не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.selectFileButton = new System.Windows.Forms.Button();
            this.convertButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.selectedFileLabel = new System.Windows.Forms.Label();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.customSizeTextBox = new System.Windows.Forms.TextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.comboBoxFormat = new System.Windows.Forms.ComboBox();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            this.comboBoxSampleRate = new System.Windows.Forms.ComboBox();
            this.comboBoxLanguage = new System.Windows.Forms.ComboBox();
            this.brainrotLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBoxResolution = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxBitDepth = new System.Windows.Forms.ComboBox();
            this.comboBoxVideoFps = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.checkBoxSpeedEffect = new System.Windows.Forms.CheckBox();
            this.checkBoxDatamosh = new System.Windows.Forms.CheckBox();
            this.checkBoxGlitchEffect = new System.Windows.Forms.CheckBox();
            this.labelGlitchLength = new System.Windows.Forms.Label();
            this.numericGlitchLength = new System.Windows.Forms.NumericUpDown();
            this.labelGlitchChance = new System.Windows.Forms.Label();
            this.numericGlitchChance = new System.Windows.Forms.NumericUpDown();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusLabelStage = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusLabelTime = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusLabelSpeed = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericGlitchLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericGlitchChance)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // selectFileButton
            // 
            this.selectFileButton.BackColor = System.Drawing.Color.Black;
            this.selectFileButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.selectFileButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.selectFileButton.ForeColor = System.Drawing.Color.LightGreen;
            this.selectFileButton.Location = new System.Drawing.Point(10, 10);
            this.selectFileButton.Name = "selectFileButton";
            this.selectFileButton.Size = new System.Drawing.Size(200, 23);
            this.selectFileButton.TabIndex = 0;
            this.selectFileButton.Tag = "selectFileButton";
            this.selectFileButton.Text = "Выбрать медиа файл";
            this.selectFileButton.UseVisualStyleBackColor = false;
            this.selectFileButton.Click += new System.EventHandler(this.selectFileButton_Click);
            // 
            // convertButton
            // 
            this.convertButton.BackColor = System.Drawing.Color.Black;
            this.convertButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.convertButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.convertButton.ForeColor = System.Drawing.Color.Coral;
            this.convertButton.Location = new System.Drawing.Point(220, 56);
            this.convertButton.Name = "convertButton";
            this.convertButton.Size = new System.Drawing.Size(100, 30);
            this.convertButton.TabIndex = 1;
            this.convertButton.Tag = "convertButton";
            this.convertButton.Text = "Конвертировать";
            this.convertButton.UseVisualStyleBackColor = false;
            this.convertButton.Click += new System.EventHandler(this.convertButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.BackColor = System.Drawing.Color.Black;
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancelButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cancelButton.ForeColor = System.Drawing.Color.IndianRed;
            this.cancelButton.Location = new System.Drawing.Point(363, 10);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(206, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Tag = "forceStopButton";
            this.cancelButton.Text = "Принудительно завершить";
            this.cancelButton.UseVisualStyleBackColor = false;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // selectedFileLabel
            // 
            this.selectedFileLabel.AutoSize = true;
            this.selectedFileLabel.Location = new System.Drawing.Point(10, 39);
            this.selectedFileLabel.Name = "selectedFileLabel";
            this.selectedFileLabel.Size = new System.Drawing.Size(155, 13);
            this.selectedFileLabel.TabIndex = 3;
            this.selectedFileLabel.Tag = "fileLabel";
            this.selectedFileLabel.Text = "Выбранный файл: Нет файла";
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.radioButton1.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.radioButton1.Location = new System.Drawing.Point(10, 104);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(101, 17);
            this.radioButton1.TabIndex = 4;
            this.radioButton1.TabStop = true;
            this.radioButton1.Tag = "discordRadioButton";
            this.radioButton1.Text = "Discord 9 MB";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.radioButton3.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.radioButton3.Location = new System.Drawing.Point(10, 138);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(151, 17);
            this.radioButton3.TabIndex = 6;
            this.radioButton3.Tag = "customRadioButton";
            this.radioButton3.Text = "Custom Max Size (MB)";
            this.radioButton3.UseVisualStyleBackColor = true;
            // 
            // customSizeTextBox
            // 
            this.customSizeTextBox.BackColor = System.Drawing.Color.Black;
            this.customSizeTextBox.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.customSizeTextBox.Location = new System.Drawing.Point(163, 135);
            this.customSizeTextBox.Name = "customSizeTextBox";
            this.customSizeTextBox.Size = new System.Drawing.Size(100, 20);
            this.customSizeTextBox.TabIndex = 7;
            this.customSizeTextBox.Tag = "customSizeTextBox";
            // 
            // progressBar1
            // 
            this.progressBar1.Cursor = System.Windows.Forms.Cursors.No;
            this.progressBar1.ForeColor = System.Drawing.Color.PaleGreen;
            this.progressBar1.Location = new System.Drawing.Point(10, 66);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(562, 20);
            this.progressBar1.TabIndex = 8;
            this.progressBar1.Visible = false;
            // 
            // comboBoxFormat
            // 
            this.comboBoxFormat.BackColor = System.Drawing.Color.Black;
            this.comboBoxFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFormat.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.comboBoxFormat.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.comboBoxFormat.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.comboBoxFormat.FormattingEnabled = true;
            this.comboBoxFormat.Items.AddRange(new object[] {
            "MP4",
            "WEBM"});
            this.comboBoxFormat.Location = new System.Drawing.Point(326, 61);
            this.comboBoxFormat.Name = "comboBoxFormat";
            this.comboBoxFormat.Size = new System.Drawing.Size(50, 21);
            this.comboBoxFormat.TabIndex = 11;
            this.comboBoxFormat.Tag = "comboBoxFormat";
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.ImageLocation = "";
            this.logoPictureBox.Location = new System.Drawing.Point(516, 178);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new System.Drawing.Size(60, 60);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.logoPictureBox.TabIndex = 12;
            this.logoPictureBox.TabStop = false;
            this.logoPictureBox.Click += new System.EventHandler(this.logoPictureBox_Click);
            // 
            // comboBoxSampleRate
            // 
            this.comboBoxSampleRate.BackColor = System.Drawing.Color.Black;
            this.comboBoxSampleRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSampleRate.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.comboBoxSampleRate.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.comboBoxSampleRate.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.comboBoxSampleRate.FormattingEnabled = true;
            this.comboBoxSampleRate.Items.AddRange(new object[] {
            "8000",
            "11025",
            "16000",
            "22050",
            "44100",
            "48000"});
            this.comboBoxSampleRate.Location = new System.Drawing.Point(121, 205);
            this.comboBoxSampleRate.Name = "comboBoxSampleRate";
            this.comboBoxSampleRate.Size = new System.Drawing.Size(100, 21);
            this.comboBoxSampleRate.TabIndex = 17;
            this.comboBoxSampleRate.Tag = "comboBoxSampleRate";
            this.comboBoxSampleRate.SelectedIndexChanged += new System.EventHandler(this.comboBoxSampleRate_SelectedIndexChanged);
            // 
            // comboBoxLanguage
            // 
            this.comboBoxLanguage.BackColor = System.Drawing.Color.Black;
            this.comboBoxLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLanguage.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.comboBoxLanguage.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.comboBoxLanguage.FormattingEnabled = true;
            this.comboBoxLanguage.Location = new System.Drawing.Point(270, 10);
            this.comboBoxLanguage.Name = "comboBoxLanguage";
            this.comboBoxLanguage.Size = new System.Drawing.Size(50, 21);
            this.comboBoxLanguage.TabIndex = 13;
            this.comboBoxLanguage.Tag = "comboBoxLanguage";
            this.comboBoxLanguage.SelectedIndexChanged += new System.EventHandler(this.comboBoxLanguage_SelectedIndexChanged);
            // 
            // brainrotLabel
            // 
            this.brainrotLabel.AutoSize = true;
            this.brainrotLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.brainrotLabel.ForeColor = System.Drawing.Color.Crimson;
            this.brainrotLabel.Location = new System.Drawing.Point(228, 171);
            this.brainrotLabel.Name = "brainrotLabel";
            this.brainrotLabel.Size = new System.Drawing.Size(121, 17);
            this.brainrotLabel.TabIndex = 14;
            this.brainrotLabel.Text = "Brainrot Edition";
            this.brainrotLabel.Click += new System.EventHandler(this.brainrotLabel_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label3.ForeColor = System.Drawing.Color.Black;
            this.label3.Location = new System.Drawing.Point(7, 158);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(572, 17);
            this.label3.TabIndex = 15;
            this.label3.Text = "---------------------------------------------------------------------------------" +
    "-------------";
            // 
            // comboBoxResolution
            // 
            this.comboBoxResolution.BackColor = System.Drawing.Color.Black;
            this.comboBoxResolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxResolution.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.comboBoxResolution.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.comboBoxResolution.FormattingEnabled = true;
            this.comboBoxResolution.Items.AddRange(new object[] {
            "224x144",
            "320x240",
            "640x360",
            "854x480",
            "1280x720",
            "1920x1080"});
            this.comboBoxResolution.Location = new System.Drawing.Point(10, 205);
            this.comboBoxResolution.Name = "comboBoxResolution";
            this.comboBoxResolution.Size = new System.Drawing.Size(105, 21);
            this.comboBoxResolution.TabIndex = 16;
            this.comboBoxResolution.Tag = "comboBoxResolution";
            this.comboBoxResolution.SelectedIndexChanged += new System.EventHandler(this.comboBoxResolution_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label4.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.label4.Location = new System.Drawing.Point(26, 190);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(67, 13);
            this.label4.TabIndex = 18;
            this.label4.Text = "Resolution";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label5.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.label5.Location = new System.Drawing.Point(131, 190);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(79, 13);
            this.label5.TabIndex = 19;
            this.label5.Text = "Sample Rate";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label6.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.label6.Location = new System.Drawing.Point(137, 225);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(60, 13);
            this.label6.TabIndex = 21;
            this.label6.Text = "Bit Depth";
            // 
            // comboBoxBitDepth
            // 
            this.comboBoxBitDepth.BackColor = System.Drawing.Color.Black;
            this.comboBoxBitDepth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBitDepth.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.comboBoxBitDepth.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.comboBoxBitDepth.FormattingEnabled = true;
            this.comboBoxBitDepth.Items.AddRange(new object[] {
            "u8",
            "s16"});
            this.comboBoxBitDepth.Location = new System.Drawing.Point(140, 241);
            this.comboBoxBitDepth.Name = "comboBoxBitDepth";
            this.comboBoxBitDepth.Size = new System.Drawing.Size(55, 21);
            this.comboBoxBitDepth.TabIndex = 20;
            this.comboBoxBitDepth.Tag = "comboBoxBitDepth";
            // 
            // comboBoxVideoFps
            // 
            this.comboBoxVideoFps.BackColor = System.Drawing.Color.Black;
            this.comboBoxVideoFps.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxVideoFps.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.comboBoxVideoFps.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.comboBoxVideoFps.FormattingEnabled = true;
            this.comboBoxVideoFps.Items.AddRange(new object[] {
            "5",
            "10",
            "24",
            "30",
            "60"});
            this.comboBoxVideoFps.Location = new System.Drawing.Point(32, 237);
            this.comboBoxVideoFps.Name = "comboBoxVideoFps";
            this.comboBoxVideoFps.Size = new System.Drawing.Size(47, 21);
            this.comboBoxVideoFps.TabIndex = 22;
            this.comboBoxVideoFps.Tag = "comboBoxvideoFPS";
            this.comboBoxVideoFps.SelectedIndexChanged += new System.EventHandler(this.comboBoxVideoFps_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label7.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.label7.Location = new System.Drawing.Point(82, 240);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(30, 13);
            this.label7.TabIndex = 23;
            this.label7.Text = "FPS";
            // 
            // checkBoxSpeedEffect
            // 
            this.checkBoxSpeedEffect.AutoSize = true;
            this.checkBoxSpeedEffect.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.checkBoxSpeedEffect.Location = new System.Drawing.Point(441, 105);
            this.checkBoxSpeedEffect.Name = "checkBoxSpeedEffect";
            this.checkBoxSpeedEffect.Size = new System.Drawing.Size(87, 17);
            this.checkBoxSpeedEffect.TabIndex = 24;
            this.checkBoxSpeedEffect.Text = "Speed effect";
            this.checkBoxSpeedEffect.UseVisualStyleBackColor = true;
            // 
            // checkBoxDatamosh
            // 
            this.checkBoxDatamosh.AutoSize = true;
            this.checkBoxDatamosh.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.checkBoxDatamosh.Location = new System.Drawing.Point(441, 128);
            this.checkBoxDatamosh.Name = "checkBoxDatamosh";
            this.checkBoxDatamosh.Size = new System.Drawing.Size(74, 17);
            this.checkBoxDatamosh.TabIndex = 25;
            this.checkBoxDatamosh.Text = "Datamosh";
            this.checkBoxDatamosh.UseVisualStyleBackColor = true;
            this.checkBoxDatamosh.CheckedChanged += new System.EventHandler(this.checkBoxDatamosh_CheckedChanged);
            // 
            // checkBoxGlitchEffect
            // 
            this.checkBoxGlitchEffect.AutoSize = true;
            this.checkBoxGlitchEffect.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.checkBoxGlitchEffect.Location = new System.Drawing.Point(516, 247);
            this.checkBoxGlitchEffect.Name = "checkBoxGlitchEffect";
            this.checkBoxGlitchEffect.Size = new System.Drawing.Size(53, 17);
            this.checkBoxGlitchEffect.TabIndex = 26;
            this.checkBoxGlitchEffect.Text = "Glitch";
            this.checkBoxGlitchEffect.UseVisualStyleBackColor = true;
            this.checkBoxGlitchEffect.CheckedChanged += new System.EventHandler(this.checkBoxGlitchEffect_CheckedChanged);
            // 
            // labelGlitchLength
            // 
            this.labelGlitchLength.AutoSize = true;
            this.labelGlitchLength.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.labelGlitchLength.Location = new System.Drawing.Point(392, 229);
            this.labelGlitchLength.Name = "labelGlitchLength";
            this.labelGlitchLength.Size = new System.Drawing.Size(78, 13);
            this.labelGlitchLength.TabIndex = 27;
            this.labelGlitchLength.Text = "Jump length (s)";
            this.labelGlitchLength.Visible = false;
            // 
            // numericGlitchLength
            // 
            this.numericGlitchLength.BackColor = System.Drawing.Color.Black;
            this.numericGlitchLength.DecimalPlaces = 1;
            this.numericGlitchLength.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.numericGlitchLength.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericGlitchLength.Location = new System.Drawing.Point(326, 226);
            this.numericGlitchLength.Maximum = new decimal(new int[] {
            6,
            0,
            0,
            0});
            this.numericGlitchLength.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            65536});
            this.numericGlitchLength.Name = "numericGlitchLength";
            this.numericGlitchLength.Size = new System.Drawing.Size(60, 20);
            this.numericGlitchLength.TabIndex = 28;
            this.numericGlitchLength.Value = new decimal(new int[] {
            12,
            0,
            0,
            65536});
            this.numericGlitchLength.Visible = false;
            // 
            // labelGlitchChance
            // 
            this.labelGlitchChance.AutoSize = true;
            this.labelGlitchChance.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.labelGlitchChance.Location = new System.Drawing.Point(392, 251);
            this.labelGlitchChance.Name = "labelGlitchChance";
            this.labelGlitchChance.Size = new System.Drawing.Size(61, 13);
            this.labelGlitchChance.TabIndex = 29;
            this.labelGlitchChance.Text = "Chance (%)";
            this.labelGlitchChance.Visible = false;
            // 
            // numericGlitchChance
            // 
            this.numericGlitchChance.BackColor = System.Drawing.Color.Black;
            this.numericGlitchChance.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.numericGlitchChance.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericGlitchChance.Location = new System.Drawing.Point(326, 248);
            this.numericGlitchChance.Name = "numericGlitchChance";
            this.numericGlitchChance.Size = new System.Drawing.Size(60, 20);
            this.numericGlitchChance.TabIndex = 30;
            this.numericGlitchChance.Value = new decimal(new int[] {
            25,
            0,
            0,
            0});
            this.numericGlitchChance.Visible = false;
            // 
            // statusStrip1
            // 
            this.statusStrip1.BackColor = System.Drawing.Color.Black;
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabelStage,
            this.statusLabelTime,
            this.statusLabelSpeed});
            this.statusStrip1.Location = new System.Drawing.Point(0, 288);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(584, 22);
            this.statusStrip1.SizingGrip = false;
            this.statusStrip1.TabIndex = 31;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // statusLabelStage
            // 
            this.statusLabelStage.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.statusLabelStage.Name = "statusLabelStage";
            this.statusLabelStage.Size = new System.Drawing.Size(383, 17);
            this.statusLabelStage.Spring = true;
            this.statusLabelStage.Text = "Stage: Idle";
            this.statusLabelStage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // statusLabelTime
            // 
            this.statusLabelTime.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.statusLabelTime.Name = "statusLabelTime";
            this.statusLabelTime.Size = new System.Drawing.Size(98, 17);
            this.statusLabelTime.Text = "--:-- / --:--";
            // 
            // statusLabelSpeed
            // 
            this.statusLabelSpeed.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.statusLabelSpeed.Name = "statusLabelSpeed";
            this.statusLabelSpeed.Size = new System.Drawing.Size(88, 17);
            this.statusLabelSpeed.Text = "Speed: --";
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(42)))), ((int)(((byte)(42)))));
            this.ClientSize = new System.Drawing.Size(584, 310);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.comboBoxVideoFps);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.comboBoxBitDepth);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.comboBoxSampleRate);
            this.Controls.Add(this.comboBoxResolution);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.numericGlitchChance);
            this.Controls.Add(this.labelGlitchChance);
            this.Controls.Add(this.numericGlitchLength);
            this.Controls.Add(this.labelGlitchLength);
            this.Controls.Add(this.checkBoxGlitchEffect);
            this.Controls.Add(this.checkBoxDatamosh);
            this.Controls.Add(this.checkBoxSpeedEffect);
            this.Controls.Add(this.brainrotLabel);
            this.Controls.Add(this.comboBoxLanguage);
            this.Controls.Add(this.logoPictureBox);
            this.Controls.Add(this.comboBoxFormat);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.customSizeTextBox);
            this.Controls.Add(this.radioButton3);
            this.Controls.Add(this.radioButton1);
            this.Controls.Add(this.selectedFileLabel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.convertButton);
            this.Controls.Add(this.selectFileButton);
            this.Controls.Add(this.statusStrip1);
            this.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Form1";
            this.Text = "Video Converter";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericGlitchLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericGlitchChance)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button selectFileButton;
        private System.Windows.Forms.Button convertButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label selectedFileLabel;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.TextBox customSizeTextBox;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.ComboBox comboBoxFormat;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.ComboBox comboBoxSampleRate;
        private System.Windows.Forms.ComboBox comboBoxLanguage;
        private System.Windows.Forms.Label brainrotLabel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBoxResolution;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBoxBitDepth;
        private System.Windows.Forms.ComboBox comboBoxVideoFps;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox checkBoxSpeedEffect;
        private System.Windows.Forms.CheckBox checkBoxDatamosh;
        private System.Windows.Forms.CheckBox checkBoxGlitchEffect;
        private System.Windows.Forms.Label labelGlitchLength;
        private System.Windows.Forms.NumericUpDown numericGlitchLength;
        private System.Windows.Forms.Label labelGlitchChance;
        private System.Windows.Forms.NumericUpDown numericGlitchChance;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusLabelStage;
        private System.Windows.Forms.ToolStripStatusLabel statusLabelTime;
        private System.Windows.Forms.ToolStripStatusLabel statusLabelSpeed;
    }
}
