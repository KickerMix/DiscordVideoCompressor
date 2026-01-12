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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.comboBoxFormat = new System.Windows.Forms.ComboBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.comboBoxSampleRate = new System.Windows.Forms.ComboBox();
            this.comboBoxLanguage = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBoxResolution = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxBitDepth = new System.Windows.Forms.ComboBox();
            this.comboBoxvideoFPS = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.checkBoxSpeedEffect = new System.Windows.Forms.CheckBox();
            this.checkBoxDatamosh = new System.Windows.Forms.CheckBox();
            this.checkBoxGlitchEffect = new System.Windows.Forms.CheckBox();
            this.labelGlitchLength = new System.Windows.Forms.Label();
            this.numericGlitchLength = new System.Windows.Forms.NumericUpDown();
            this.labelGlitchChance = new System.Windows.Forms.Label();
            this.numericGlitchChance = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericGlitchLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericGlitchChance)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.Black;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button1.ForeColor = System.Drawing.Color.LightGreen;
            this.button1.Location = new System.Drawing.Point(10, 10);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(200, 23);
            this.button1.TabIndex = 0;
            this.button1.Tag = "selectFileButton";
            this.button1.Text = "Выбрать медиа файл";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.Black;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button2.ForeColor = System.Drawing.Color.Coral;
            this.button2.Location = new System.Drawing.Point(220, 56);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(100, 30);
            this.button2.TabIndex = 1;
            this.button2.Tag = "convertButton";
            this.button2.Text = "Конвертировать";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.Color.Black;
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button3.ForeColor = System.Drawing.Color.IndianRed;
            this.button3.Location = new System.Drawing.Point(363, 10);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(206, 23);
            this.button3.TabIndex = 2;
            this.button3.Tag = "forceStopButton";
            this.button3.Text = "Принудительно завершить";
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(155, 13);
            this.label1.TabIndex = 3;
            this.label1.Tag = "fileLabel";
            this.label1.Text = "Выбранный файл: Нет файла";
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
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.Black;
            this.textBox1.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.textBox1.Location = new System.Drawing.Point(163, 135);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 20);
            this.textBox1.TabIndex = 7;
            this.textBox1.Tag = "customSizeTextBox";
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
            // pictureBox1
            // 
            this.pictureBox1.ImageLocation = "";
            this.pictureBox1.Location = new System.Drawing.Point(516, 178);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(60, 60);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 12;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
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
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.ForeColor = System.Drawing.Color.Crimson;
            this.label2.Location = new System.Drawing.Point(228, 171);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(121, 17);
            this.label2.TabIndex = 14;
            this.label2.Text = "Brainrot Edition";
            this.label2.Click += new System.EventHandler(this.label2_Click);
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
            // comboBoxvideoFPS
            // 
            this.comboBoxvideoFPS.BackColor = System.Drawing.Color.Black;
            this.comboBoxvideoFPS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxvideoFPS.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.comboBoxvideoFPS.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.comboBoxvideoFPS.FormattingEnabled = true;
            this.comboBoxvideoFPS.Items.AddRange(new object[] {
            "5",
            "10",
            "24",
            "30",
            "60"});
            this.comboBoxvideoFPS.Location = new System.Drawing.Point(32, 237);
            this.comboBoxvideoFPS.Name = "comboBoxvideoFPS";
            this.comboBoxvideoFPS.Size = new System.Drawing.Size(47, 21);
            this.comboBoxvideoFPS.TabIndex = 22;
            this.comboBoxvideoFPS.Tag = "comboBoxvideoFPS";
            this.comboBoxvideoFPS.SelectedIndexChanged += new System.EventHandler(this.comboBoxvideoFPS_SelectedIndexChanged);
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
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(42)))), ((int)(((byte)(42)))));
            this.ClientSize = new System.Drawing.Size(584, 280);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.comboBoxvideoFPS);
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
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBoxLanguage);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.comboBoxFormat);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.radioButton3);
            this.Controls.Add(this.radioButton1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Form1";
            this.Text = "Video Converter";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericGlitchLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericGlitchChance)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.ComboBox comboBoxFormat;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ComboBox comboBoxSampleRate;
        private System.Windows.Forms.ComboBox comboBoxLanguage;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBoxResolution;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBoxBitDepth;
        private System.Windows.Forms.ComboBox comboBoxvideoFPS;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox checkBoxSpeedEffect;
        private System.Windows.Forms.CheckBox checkBoxDatamosh;
        private System.Windows.Forms.CheckBox checkBoxGlitchEffect;
        private System.Windows.Forms.Label labelGlitchLength;
        private System.Windows.Forms.NumericUpDown numericGlitchLength;
        private System.Windows.Forms.Label labelGlitchChance;
        private System.Windows.Forms.NumericUpDown numericGlitchChance;
    }
}
