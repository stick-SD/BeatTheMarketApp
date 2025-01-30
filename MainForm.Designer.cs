namespace BeatTheMarketApp
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControlApps = new System.Windows.Forms.TabControl();
            this.tabTechnicalAnalysis = new System.Windows.Forms.TabPage();
            this.tabInvestmentAnalysis = new System.Windows.Forms.TabPage();
            this.tabBacktesting = new System.Windows.Forms.TabPage();
            this.tabControlBacktesting = new System.Windows.Forms.TabControl();
            this.tabMain = new System.Windows.Forms.TabPage();
            this.tabOther = new System.Windows.Forms.TabPage();
            this.groupBoxTickerSettingsTA = new System.Windows.Forms.GroupBox();
            this.comboBoxClosePriceColumnTA = new System.Windows.Forms.ComboBox();
            this.labelClosePriceColumnTA = new System.Windows.Forms.Label();
            this.labelTickerListToRunTA = new System.Windows.Forms.Label();
            this.comboBoxBenchmarkAssetTA = new System.Windows.Forms.ComboBox();
            this.comboBoxTickerListToRunTA = new System.Windows.Forms.ComboBox();
            this.labelBenchmarkAssetTA = new System.Windows.Forms.Label();
            this.labelBeatTheMarketAppHeader = new System.Windows.Forms.Label();
            this.buttonRun = new System.Windows.Forms.Button();
            this.groupBoxStatus = new System.Windows.Forms.GroupBox();
            this.textBoxStatus = new System.Windows.Forms.TextBox();
            this.groupBoxTickerSettingBT = new System.Windows.Forms.GroupBox();
            this.comboBoxClosePriceColumnBT = new System.Windows.Forms.ComboBox();
            this.labelClosePriceColumnBT = new System.Windows.Forms.Label();
            this.labelTickerListToRunBT = new System.Windows.Forms.Label();
            this.comboBoxComplemetaryAssetBT = new System.Windows.Forms.ComboBox();
            this.comboBoxTickerListToRunBT = new System.Windows.Forms.ComboBox();
            this.labelComplemetaryAssetBT = new System.Windows.Forms.Label();
            // 
            // tabControlApps
            this.tabControlApps.Location = new System.Drawing.Point(12, 37);
            this.tabControlApps.Name = "tabControlApps";
            this.tabControlApps.Size = new System.Drawing.Size(932, 234);
            this.tabControlApps.TabIndex = 0;
            this.tabControlApps.TabStop = true;
            // 
            // tabTechnicalAnalysis
            this.tabTechnicalAnalysis.Text = "Technical Analysis";
            this.tabTechnicalAnalysis.Location = new System.Drawing.Point(4, 24);
            this.tabTechnicalAnalysis.Name = "tabTechnicalAnalysis";
            this.tabTechnicalAnalysis.Size = new System.Drawing.Size(924, 206);
            this.tabTechnicalAnalysis.TabIndex = 0;
            this.tabTechnicalAnalysis.TabStop = true;
            // 
            // tabInvestmentAnalysis
            this.tabInvestmentAnalysis.Text = "Investment Analysis";
            this.tabInvestmentAnalysis.Location = new System.Drawing.Point(4, 24);
            this.tabInvestmentAnalysis.Name = "tabInvestmentAnalysis";
            this.tabInvestmentAnalysis.Size = new System.Drawing.Size(924, 206);
            this.tabInvestmentAnalysis.TabIndex = 0;
            this.tabInvestmentAnalysis.TabStop = true;
            // 
            // tabBacktesting
            this.tabBacktesting.Text = "Backtesting";
            this.tabBacktesting.Location = new System.Drawing.Point(4, 24);
            this.tabBacktesting.Name = "tabBacktesting";
            this.tabBacktesting.Size = new System.Drawing.Size(924, 206);
            this.tabBacktesting.TabIndex = 0;
            this.tabBacktesting.TabStop = true;
            // 
            // tabControlBacktesting
            this.tabControlBacktesting.Location = new System.Drawing.Point(2, 2);
            this.tabControlBacktesting.Name = "tabControlBacktesting";
            this.tabControlBacktesting.Size = new System.Drawing.Size(919, 201);
            this.tabControlBacktesting.TabIndex = 0;
            this.tabControlBacktesting.TabStop = true;
            // 
            // tabMain
            this.tabMain.Text = "Main";
            this.tabMain.Location = new System.Drawing.Point(4, 24);
            this.tabMain.Name = "tabMain";
            this.tabMain.Size = new System.Drawing.Size(911, 173);
            this.tabMain.TabIndex = 0;
            this.tabMain.TabStop = true;
            // 
            // tabOther
            this.tabOther.Text = "Other";
            this.tabOther.Location = new System.Drawing.Point(4, 24);
            this.tabOther.Name = "tabOther";
            this.tabOther.Size = new System.Drawing.Size(911, 173);
            this.tabOther.TabIndex = 0;
            this.tabOther.TabStop = true;
            // 
            // groupBoxTickerSettingsTA
            this.groupBoxTickerSettingsTA.Text = "Ticker Settings";
            this.groupBoxTickerSettingsTA.Location = new System.Drawing.Point(6, 6);
            this.groupBoxTickerSettingsTA.Name = "groupBoxTickerSettingsTA";
            this.groupBoxTickerSettingsTA.Size = new System.Drawing.Size(276, 137);
            this.groupBoxTickerSettingsTA.TabIndex = 0;
            this.groupBoxTickerSettingsTA.TabStop = true;
            // 
            // comboBoxClosePriceColumnTA
            this.comboBoxClosePriceColumnTA.Location = new System.Drawing.Point(133, 90);
            this.comboBoxClosePriceColumnTA.Name = "comboBoxClosePriceColumnTA";
            this.comboBoxClosePriceColumnTA.Size = new System.Drawing.Size(111, 23);
            this.comboBoxClosePriceColumnTA.TabIndex = 0;
            this.comboBoxClosePriceColumnTA.TabStop = true;
            // 
            // labelClosePriceColumnTA
            this.labelClosePriceColumnTA.Text = "Close Price Column";
            this.labelClosePriceColumnTA.Location = new System.Drawing.Point(6, 93);
            this.labelClosePriceColumnTA.Name = "labelClosePriceColumnTA";
            this.labelClosePriceColumnTA.Size = new System.Drawing.Size(111, 15);
            this.labelClosePriceColumnTA.TabIndex = 0;
            this.labelClosePriceColumnTA.TabStop = true;
            // 
            // labelTickerListToRunTA
            this.labelTickerListToRunTA.Text = "Ticker List to Run:";
            this.labelTickerListToRunTA.Location = new System.Drawing.Point(6, 29);
            this.labelTickerListToRunTA.Name = "labelTickerListToRunTA";
            this.labelTickerListToRunTA.Size = new System.Drawing.Size(101, 15);
            this.labelTickerListToRunTA.TabIndex = 0;
            this.labelTickerListToRunTA.TabStop = true;
            // 
            // comboBoxBenchmarkAssetTA
            this.comboBoxBenchmarkAssetTA.Location = new System.Drawing.Point(135, 56);
            this.comboBoxBenchmarkAssetTA.Name = "comboBoxBenchmarkAssetTA";
            this.comboBoxBenchmarkAssetTA.Size = new System.Drawing.Size(121, 23);
            this.comboBoxBenchmarkAssetTA.TabIndex = 0;
            this.comboBoxBenchmarkAssetTA.TabStop = true;
            // 
            // comboBoxTickerListToRunTA
            this.comboBoxTickerListToRunTA.Location = new System.Drawing.Point(133, 26);
            this.comboBoxTickerListToRunTA.Name = "comboBoxTickerListToRunTA";
            this.comboBoxTickerListToRunTA.Size = new System.Drawing.Size(121, 23);
            this.comboBoxTickerListToRunTA.TabIndex = 0;
            this.comboBoxTickerListToRunTA.TabStop = true;
            // 
            // labelBenchmarkAssetTA
            this.labelBenchmarkAssetTA.Text = "Benchmark Asset:";
            this.labelBenchmarkAssetTA.Location = new System.Drawing.Point(6, 59);
            this.labelBenchmarkAssetTA.Name = "labelBenchmarkAssetTA";
            this.labelBenchmarkAssetTA.Size = new System.Drawing.Size(101, 15);
            this.labelBenchmarkAssetTA.TabIndex = 0;
            this.labelBenchmarkAssetTA.TabStop = true;
            // 
            // labelBeatTheMarketAppHeader
            this.labelBeatTheMarketAppHeader.Text = "Beat the Market App";
            this.labelBeatTheMarketAppHeader.Location = new System.Drawing.Point(20, 12);
            this.labelBeatTheMarketAppHeader.Name = "labelBeatTheMarketAppHeader";
            this.labelBeatTheMarketAppHeader.Size = new System.Drawing.Size(148, 19);
            this.labelBeatTheMarketAppHeader.TabIndex = 0;
            this.labelBeatTheMarketAppHeader.TabStop = true;
            // 
            // buttonRun
            this.buttonRun.Text = "Run";
            this.buttonRun.Location = new System.Drawing.Point(188, 12);
            this.buttonRun.Name = "buttonRun";
            this.buttonRun.Size = new System.Drawing.Size(75, 23);
            this.buttonRun.TabIndex = 0;
            this.buttonRun.TabStop = true;
            // 
            // groupBoxStatus
            this.groupBoxStatus.Text = "Status";
            this.groupBoxStatus.Location = new System.Drawing.Point(15, 274);
            this.groupBoxStatus.Name = "groupBoxStatus";
            this.groupBoxStatus.Size = new System.Drawing.Size(925, 261);
            this.groupBoxStatus.TabIndex = 0;
            this.groupBoxStatus.TabStop = true;
            // 
            // textBoxStatus
            this.textBoxStatus.Location = new System.Drawing.Point(4, 19);
            this.textBoxStatus.Name = "textBoxStatus";
            this.textBoxStatus.Size = new System.Drawing.Size(918, 235);
            this.textBoxStatus.TabIndex = 0;
            this.textBoxStatus.TabStop = true;
            // 
            // groupBoxTickerSettingBT
            this.groupBoxTickerSettingBT.Text = "Ticker Settings";
            this.groupBoxTickerSettingBT.Location = new System.Drawing.Point(6, 6);
            this.groupBoxTickerSettingBT.Name = "groupBoxTickerSettingBT";
            this.groupBoxTickerSettingBT.Size = new System.Drawing.Size(276, 137);
            this.groupBoxTickerSettingBT.TabIndex = 0;
            this.groupBoxTickerSettingBT.TabStop = true;
            // 
            // comboBoxClosePriceColumnBT
            this.comboBoxClosePriceColumnBT.Location = new System.Drawing.Point(133, 90);
            this.comboBoxClosePriceColumnBT.Name = "comboBoxClosePriceColumnBT";
            this.comboBoxClosePriceColumnBT.Size = new System.Drawing.Size(111, 23);
            this.comboBoxClosePriceColumnBT.TabIndex = 0;
            this.comboBoxClosePriceColumnBT.TabStop = true;
            // 
            // labelClosePriceColumnBT
            this.labelClosePriceColumnBT.Text = "Close Price Column";
            this.labelClosePriceColumnBT.Location = new System.Drawing.Point(6, 93);
            this.labelClosePriceColumnBT.Name = "labelClosePriceColumnBT";
            this.labelClosePriceColumnBT.Size = new System.Drawing.Size(111, 15);
            this.labelClosePriceColumnBT.TabIndex = 0;
            this.labelClosePriceColumnBT.TabStop = true;
            // 
            // labelTickerListToRunBT
            this.labelTickerListToRunBT.Text = "Ticker List to Run:";
            this.labelTickerListToRunBT.Location = new System.Drawing.Point(6, 29);
            this.labelTickerListToRunBT.Name = "labelTickerListToRunBT";
            this.labelTickerListToRunBT.Size = new System.Drawing.Size(101, 15);
            this.labelTickerListToRunBT.TabIndex = 0;
            this.labelTickerListToRunBT.TabStop = true;
            // 
            // comboBoxComplemetaryAssetBT
            this.comboBoxComplemetaryAssetBT.Location = new System.Drawing.Point(135, 56);
            this.comboBoxComplemetaryAssetBT.Name = "comboBoxComplemetaryAssetBT";
            this.comboBoxComplemetaryAssetBT.Size = new System.Drawing.Size(121, 23);
            this.comboBoxComplemetaryAssetBT.TabIndex = 0;
            this.comboBoxComplemetaryAssetBT.TabStop = true;
            // 
            // comboBoxTickerListToRunBT
            this.comboBoxTickerListToRunBT.Location = new System.Drawing.Point(133, 26);
            this.comboBoxTickerListToRunBT.Name = "comboBoxTickerListToRunBT";
            this.comboBoxTickerListToRunBT.Size = new System.Drawing.Size(121, 23);
            this.comboBoxTickerListToRunBT.TabIndex = 0;
            this.comboBoxTickerListToRunBT.TabStop = true;
            // 
            // labelComplemetaryAssetBT
            this.labelComplemetaryAssetBT.Text = "Complemetary Asset:";
            this.labelComplemetaryAssetBT.Location = new System.Drawing.Point(6, 59);
            this.labelComplemetaryAssetBT.Name = "labelComplemetaryAssetBT";
            this.labelComplemetaryAssetBT.Size = new System.Drawing.Size(120, 15);
            this.labelComplemetaryAssetBT.TabIndex = 0;
            this.labelComplemetaryAssetBT.TabStop = true;
            // 
            this.Controls.Add(this.tabControlApps);
            this.Controls.Add(this.tabTechnicalAnalysis);
            this.Controls.Add(this.tabInvestmentAnalysis);
            this.Controls.Add(this.tabBacktesting);
            this.Controls.Add(this.tabControlBacktesting);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.tabOther);
            this.Controls.Add(this.groupBoxTickerSettingsTA);
            this.Controls.Add(this.comboBoxClosePriceColumnTA);
            this.Controls.Add(this.labelClosePriceColumnTA);
            this.Controls.Add(this.labelTickerListToRunTA);
            this.Controls.Add(this.comboBoxBenchmarkAssetTA);
            this.Controls.Add(this.comboBoxTickerListToRunTA);
            this.Controls.Add(this.labelBenchmarkAssetTA);
            this.Controls.Add(this.labelBeatTheMarketAppHeader);
            this.Controls.Add(this.buttonRun);
            this.Controls.Add(this.groupBoxStatus);
            this.Controls.Add(this.textBoxStatus);
            this.Controls.Add(this.groupBoxTickerSettingBT);
            this.Controls.Add(this.comboBoxClosePriceColumnBT);
            this.Controls.Add(this.labelClosePriceColumnBT);
            this.Controls.Add(this.labelTickerListToRunBT);
            this.Controls.Add(this.comboBoxComplemetaryAssetBT);
            this.Controls.Add(this.comboBoxTickerListToRunBT);
            this.Controls.Add(this.labelComplemetaryAssetBT);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlApps;
        private System.Windows.Forms.TabPage tabTechnicalAnalysis;
        private System.Windows.Forms.TabPage tabInvestmentAnalysis;
        private System.Windows.Forms.TabPage tabBacktesting;
        private System.Windows.Forms.TabControl tabControlBacktesting;
        private System.Windows.Forms.TabPage tabMain;
        private System.Windows.Forms.TabPage tabOther;
        private System.Windows.Forms.GroupBox groupBoxTickerSettingsTA;
        private System.Windows.Forms.ComboBox comboBoxClosePriceColumnTA;
        private System.Windows.Forms.Label labelClosePriceColumnTA;
        private System.Windows.Forms.Label labelTickerListToRunTA;
        private System.Windows.Forms.ComboBox comboBoxBenchmarkAssetTA;
        private System.Windows.Forms.ComboBox comboBoxTickerListToRunTA;
        private System.Windows.Forms.Label labelBenchmarkAssetTA;
        private System.Windows.Forms.Label labelBeatTheMarketAppHeader;
        private System.Windows.Forms.Button buttonRun;
        private System.Windows.Forms.GroupBox groupBoxStatus;
        private System.Windows.Forms.TextBox textBoxStatus;
        private System.Windows.Forms.GroupBox groupBoxTickerSettingBT;
        private System.Windows.Forms.ComboBox comboBoxClosePriceColumnBT;
        private System.Windows.Forms.Label labelClosePriceColumnBT;
        private System.Windows.Forms.Label labelTickerListToRunBT;
        private System.Windows.Forms.ComboBox comboBoxComplemetaryAssetBT;
        private System.Windows.Forms.ComboBox comboBoxTickerListToRunBT;
        private System.Windows.Forms.Label labelComplemetaryAssetBT;
    }
}
