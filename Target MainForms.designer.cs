namespace BeatTheMarketApp
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabControlApps = new TabControl();
            tabTechnicalAnalysis = new TabPage();
            groupBoxTickerSettingsTA = new GroupBox();
            comboBoxClosePriceColumnTA = new ComboBox();
            labelClosePriceColumnTA = new Label();
            labelTickerListToRunTA = new Label();
            comboBoxBenchmarkAssetTA = new ComboBox();
            comboBoxTickerListToRunTA = new ComboBox();
            labelBenchmarkAssetTA = new Label();
            tabInvestmentAnalysis = new TabPage();
            tabBacktesting = new TabPage();
            tabControlBacktesting = new TabControl();
            tabMain = new TabPage();
            tabOther = new TabPage();
            labelBeatTheMarketAppHeader = new Label();
            buttonRun = new Button();
            groupBoxStatus = new GroupBox();
            textBoxStatus = new TextBox();
            comboBoxClosePriceColumnBT = new ComboBox();
            labelClosePriceColumnBT = new Label();
            labelTickerListToRunBT = new Label();
            comboBoxComplemetaryAssetBT = new ComboBox();
            comboBoxTickerListToRunBT = new ComboBox();
            labelComplemetaryAssetBT = new Label();
            groupBoxTickerSettingBT = new GroupBox();
            tabControlApps.SuspendLayout();
            tabTechnicalAnalysis.SuspendLayout();
            groupBoxTickerSettingsTA.SuspendLayout();
            tabBacktesting.SuspendLayout();
            tabControlBacktesting.SuspendLayout();
            tabMain.SuspendLayout();
            groupBoxStatus.SuspendLayout();
            groupBoxTickerSettingBT.SuspendLayout();
            SuspendLayout();
            // 
            // tabControlApps
            // 
            tabControlApps.Controls.Add(tabTechnicalAnalysis);
            tabControlApps.Controls.Add(tabInvestmentAnalysis);
            tabControlApps.Controls.Add(tabBacktesting);
            tabControlApps.Location = new Point(12, 37);
            tabControlApps.Name = "tabControlApps";
            tabControlApps.SelectedIndex = 0;
            tabControlApps.Size = new Size(932, 234);
            tabControlApps.TabIndex = 0;
            // 
            // tabTechnicalAnalysis
            // 
            tabTechnicalAnalysis.Controls.Add(groupBoxTickerSettingsTA);
            tabTechnicalAnalysis.Location = new Point(4, 24);
            tabTechnicalAnalysis.Name = "tabTechnicalAnalysis";
            tabTechnicalAnalysis.Padding = new Padding(3);
            tabTechnicalAnalysis.Size = new Size(924, 206);
            tabTechnicalAnalysis.TabIndex = 0;
            tabTechnicalAnalysis.Text = "Technical Analysis";
            tabTechnicalAnalysis.UseVisualStyleBackColor = true;
            // 
            // groupBoxTickerSettingsTA
            // 
            groupBoxTickerSettingsTA.Controls.Add(comboBoxClosePriceColumnTA);
            groupBoxTickerSettingsTA.Controls.Add(labelClosePriceColumnTA);
            groupBoxTickerSettingsTA.Controls.Add(labelTickerListToRunTA);
            groupBoxTickerSettingsTA.Controls.Add(comboBoxBenchmarkAssetTA);
            groupBoxTickerSettingsTA.Controls.Add(comboBoxTickerListToRunTA);
            groupBoxTickerSettingsTA.Controls.Add(labelBenchmarkAssetTA);
            groupBoxTickerSettingsTA.Location = new Point(6, 6);
            groupBoxTickerSettingsTA.Name = "groupBoxTickerSettingsTA";
            groupBoxTickerSettingsTA.Size = new Size(276, 137);
            groupBoxTickerSettingsTA.TabIndex = 4;
            groupBoxTickerSettingsTA.TabStop = false;
            groupBoxTickerSettingsTA.Text = "Ticker Settings";
            // 
            // comboBoxClosePriceColumnTA
            // 
            comboBoxClosePriceColumnTA.FormattingEnabled = true;
            comboBoxClosePriceColumnTA.Items.AddRange(new object[] { "5", "6" });
            comboBoxClosePriceColumnTA.Location = new Point(133, 90);
            comboBoxClosePriceColumnTA.Name = "comboBoxClosePriceColumnTA";
            comboBoxClosePriceColumnTA.Size = new Size(111, 23);
            comboBoxClosePriceColumnTA.TabIndex = 5;
            // 
            // labelClosePriceColumnTA
            // 
            labelClosePriceColumnTA.AutoSize = true;
            labelClosePriceColumnTA.Location = new Point(6, 93);
            labelClosePriceColumnTA.Name = "labelClosePriceColumnTA";
            labelClosePriceColumnTA.Size = new Size(111, 15);
            labelClosePriceColumnTA.TabIndex = 4;
            labelClosePriceColumnTA.Text = "Close Price Column";
            // 
            // labelTickerListToRunTA
            // 
            labelTickerListToRunTA.AutoSize = true;
            labelTickerListToRunTA.Location = new Point(6, 29);
            labelTickerListToRunTA.Name = "labelTickerListToRunTA";
            labelTickerListToRunTA.Size = new Size(101, 15);
            labelTickerListToRunTA.TabIndex = 0;
            labelTickerListToRunTA.Text = "Ticker List to Run:";
            // 
            // comboBoxBenchmarkAssetTA
            // 
            comboBoxBenchmarkAssetTA.FormattingEnabled = true;
            comboBoxBenchmarkAssetTA.Location = new Point(135, 56);
            comboBoxBenchmarkAssetTA.Name = "comboBoxBenchmarkAssetTA";
            comboBoxBenchmarkAssetTA.Size = new Size(121, 23);
            comboBoxBenchmarkAssetTA.TabIndex = 3;
            // 
            // comboBoxTickerListToRunTA
            // 
            comboBoxTickerListToRunTA.FormattingEnabled = true;
            comboBoxTickerListToRunTA.Location = new Point(133, 26);
            comboBoxTickerListToRunTA.Name = "comboBoxTickerListToRunTA";
            comboBoxTickerListToRunTA.Size = new Size(121, 23);
            comboBoxTickerListToRunTA.TabIndex = 1;
            // 
            // labelBenchmarkAssetTA
            // 
            labelBenchmarkAssetTA.AutoSize = true;
            labelBenchmarkAssetTA.Location = new Point(6, 59);
            labelBenchmarkAssetTA.Name = "labelBenchmarkAssetTA";
            labelBenchmarkAssetTA.Size = new Size(101, 15);
            labelBenchmarkAssetTA.TabIndex = 2;
            labelBenchmarkAssetTA.Text = "Benchmark Asset:";
            // 
            // tabInvestmentAnalysis
            // 
            tabInvestmentAnalysis.Location = new Point(4, 24);
            tabInvestmentAnalysis.Name = "tabInvestmentAnalysis";
            tabInvestmentAnalysis.Padding = new Padding(3);
            tabInvestmentAnalysis.Size = new Size(924, 206);
            tabInvestmentAnalysis.TabIndex = 1;
            tabInvestmentAnalysis.Text = "Investment Analysis";
            tabInvestmentAnalysis.UseVisualStyleBackColor = true;
            // 
            // tabBacktesting
            // 
            tabBacktesting.Controls.Add(tabControlBacktesting);
            tabBacktesting.Location = new Point(4, 24);
            tabBacktesting.Name = "tabBacktesting";
            tabBacktesting.Size = new Size(924, 206);
            tabBacktesting.TabIndex = 2;
            tabBacktesting.Text = "Backtesting";
            tabBacktesting.UseVisualStyleBackColor = true;
            // 
            // tabControlBacktesting
            // 
            tabControlBacktesting.Controls.Add(tabMain);
            tabControlBacktesting.Controls.Add(tabOther);
            tabControlBacktesting.Location = new Point(2, 2);
            tabControlBacktesting.Name = "tabControlBacktesting";
            tabControlBacktesting.SelectedIndex = 0;
            tabControlBacktesting.Size = new Size(919, 201);
            tabControlBacktesting.TabIndex = 0;
            // 
            // tabMain
            // 
            tabMain.Controls.Add(groupBoxTickerSettingBT);
            tabMain.Location = new Point(4, 24);
            tabMain.Name = "tabMain";
            tabMain.Padding = new Padding(3);
            tabMain.Size = new Size(911, 173);
            tabMain.TabIndex = 0;
            tabMain.Text = "Main";
            tabMain.UseVisualStyleBackColor = true;
            // 
            // tabOther
            // 
            tabOther.Location = new Point(4, 24);
            tabOther.Name = "tabOther";
            tabOther.Padding = new Padding(3);
            tabOther.Size = new Size(911, 173);
            tabOther.TabIndex = 1;
            tabOther.Text = "Other";
            tabOther.UseVisualStyleBackColor = true;
            // 
            // labelBeatTheMarketAppHeader
            // 
            labelBeatTheMarketAppHeader.AutoSize = true;
            labelBeatTheMarketAppHeader.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            labelBeatTheMarketAppHeader.Location = new Point(20, 12);
            labelBeatTheMarketAppHeader.Name = "labelBeatTheMarketAppHeader";
            labelBeatTheMarketAppHeader.Size = new Size(148, 19);
            labelBeatTheMarketAppHeader.TabIndex = 1;
            labelBeatTheMarketAppHeader.Text = "Beat the Market App";
            // 
            // buttonRun
            // 
            buttonRun.Location = new Point(188, 12);
            buttonRun.Name = "buttonRun";
            buttonRun.Size = new Size(75, 23);
            buttonRun.TabIndex = 2;
            buttonRun.Text = "Run";
            buttonRun.UseVisualStyleBackColor = true;
            buttonRun.Click += buttonRun_Click;
            // 
            // groupBoxStatus
            // 
            groupBoxStatus.Controls.Add(textBoxStatus);
            groupBoxStatus.Location = new Point(15, 274);
            groupBoxStatus.Name = "groupBoxStatus";
            groupBoxStatus.Size = new Size(925, 261);
            groupBoxStatus.TabIndex = 3;
            groupBoxStatus.TabStop = false;
            groupBoxStatus.Text = "Status";
            // 
            // textBoxStatus
            // 
            textBoxStatus.Location = new Point(4, 19);
            textBoxStatus.Multiline = true;
            textBoxStatus.Name = "textBoxStatus";
            textBoxStatus.ScrollBars = ScrollBars.Vertical;
            textBoxStatus.Size = new Size(918, 235);
            textBoxStatus.TabIndex = 0;
            // 
            // comboBoxClosePriceColumnBT
            // 
            comboBoxClosePriceColumnBT.AutoCompleteCustomSource.AddRange(new string[] { "5", "6" });
            comboBoxClosePriceColumnBT.FormattingEnabled = true;
            comboBoxClosePriceColumnBT.Items.AddRange(new object[] { "5", "6" });
            comboBoxClosePriceColumnBT.Location = new Point(133, 90);
            comboBoxClosePriceColumnBT.Name = "comboBoxClosePriceColumnBT";
            comboBoxClosePriceColumnBT.Size = new Size(111, 23);
            comboBoxClosePriceColumnBT.TabIndex = 5;
            // 
            // labelClosePriceColumnBT
            // 
            labelClosePriceColumnBT.AutoSize = true;
            labelClosePriceColumnBT.Location = new Point(6, 93);
            labelClosePriceColumnBT.Name = "labelClosePriceColumnBT";
            labelClosePriceColumnBT.Size = new Size(111, 15);
            labelClosePriceColumnBT.TabIndex = 4;
            labelClosePriceColumnBT.Text = "Close Price Column";
            // 
            // labelTickerListToRunBT
            // 
            labelTickerListToRunBT.AutoSize = true;
            labelTickerListToRunBT.Location = new Point(6, 29);
            labelTickerListToRunBT.Name = "labelTickerListToRunBT";
            labelTickerListToRunBT.Size = new Size(101, 15);
            labelTickerListToRunBT.TabIndex = 0;
            labelTickerListToRunBT.Text = "Ticker List to Run:";
            // 
            // comboBoxComplemetaryAssetBT
            // 
            comboBoxComplemetaryAssetBT.FormattingEnabled = true;
            comboBoxComplemetaryAssetBT.Location = new Point(135, 56);
            comboBoxComplemetaryAssetBT.Name = "comboBoxComplemetaryAssetBT";
            comboBoxComplemetaryAssetBT.Size = new Size(121, 23);
            comboBoxComplemetaryAssetBT.TabIndex = 3;
            // 
            // comboBoxTickerListToRunBT
            // 
            comboBoxTickerListToRunBT.FormattingEnabled = true;
            comboBoxTickerListToRunBT.Location = new Point(133, 26);
            comboBoxTickerListToRunBT.Name = "comboBoxTickerListToRunBT";
            comboBoxTickerListToRunBT.Size = new Size(121, 23);
            comboBoxTickerListToRunBT.TabIndex = 1;
            // 
            // labelComplemetaryAssetBT
            // 
            labelComplemetaryAssetBT.AutoSize = true;
            labelComplemetaryAssetBT.Location = new Point(6, 59);
            labelComplemetaryAssetBT.Name = "labelComplemetaryAssetBT";
            labelComplemetaryAssetBT.Size = new Size(120, 15);
            labelComplemetaryAssetBT.TabIndex = 2;
            labelComplemetaryAssetBT.Text = "Complemetary Asset:";
            // 
            // groupBoxTickerSettingBT
            // 
            groupBoxTickerSettingBT.Controls.Add(comboBoxClosePriceColumnBT);
            groupBoxTickerSettingBT.Controls.Add(labelClosePriceColumnBT);
            groupBoxTickerSettingBT.Controls.Add(labelTickerListToRunBT);
            groupBoxTickerSettingBT.Controls.Add(comboBoxComplemetaryAssetBT);
            groupBoxTickerSettingBT.Controls.Add(comboBoxTickerListToRunBT);
            groupBoxTickerSettingBT.Controls.Add(labelComplemetaryAssetBT);
            groupBoxTickerSettingBT.Location = new Point(6, 6);
            groupBoxTickerSettingBT.Name = "groupBoxTickerSettingBT";
            groupBoxTickerSettingBT.Size = new Size(276, 137);
            groupBoxTickerSettingBT.TabIndex = 5;
            groupBoxTickerSettingBT.TabStop = false;
            groupBoxTickerSettingBT.Text = "Ticker Settings";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(951, 547);
            Controls.Add(groupBoxStatus);
            Controls.Add(buttonRun);
            Controls.Add(labelBeatTheMarketAppHeader);
            Controls.Add(tabControlApps);
            Name = "MainForm";
            tabControlApps.ResumeLayout(false);
            tabTechnicalAnalysis.ResumeLayout(false);
            groupBoxTickerSettingsTA.ResumeLayout(false);
            groupBoxTickerSettingsTA.PerformLayout();
            tabBacktesting.ResumeLayout(false);
            tabControlBacktesting.ResumeLayout(false);
            tabMain.ResumeLayout(false);
            groupBoxStatus.ResumeLayout(false);
            groupBoxStatus.PerformLayout();
            groupBoxTickerSettingBT.ResumeLayout(false);
            groupBoxTickerSettingBT.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TabControl tabControlApps;
        private TabPage tabTechnicalAnalysis;
        private TabPage tabInvestmentAnalysis;
        private TabPage tabBacktesting;
        private TabControl tabControlBacktesting;
        private TabPage tabMain;
        private TabPage tabOther;
        private ComboBox comboBoxBenchmarkAssetTA;
        private Label labelBenchmarkAssetTA;
        private ComboBox comboBoxTickerListToRunTA;
        private Label labelTickerListToRunTA;
        private GroupBox groupBoxTickerSettingsTA;
        private Label labelBeatTheMarketAppHeader;
        private Button buttonRun;
        private ComboBox comboBoxClosePriceColumnTA;
        private Label labelClosePriceColumnTA;
        private GroupBox groupBoxStatus;
        private TextBox textBoxStatus;
        private GroupBox groupBoxTickerSettingBT;
        private ComboBox comboBoxClosePriceColumnBT;
        private Label labelClosePriceColumnBT;
        private Label labelTickerListToRunBT;
        private ComboBox comboBoxComplemetaryAssetBT;
        private ComboBox comboBoxTickerListToRunBT;
        private Label labelComplemetaryAssetBT;
    }
}