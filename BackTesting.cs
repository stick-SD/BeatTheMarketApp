using BeatTheMarketApp.InvestmentLibrary;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BeatTheMarketApp.Common;

namespace BeatTheMarketApp.BackTesting
{
    public static class BackTestFunctions
    {
        public static void RunBackTestAnalysis(string complementaryInvestmentToRun, List<string> tickersToRunBT, BackTestSettings settings, FileControlSettings fileSettings, IStatusUpdater statusUpdater, PathDefinitions pathDefinitions, int closePriceColumn)
        {
            try
            {
                statusUpdater.UpdateStatus($"BackTest Analysis App  {DateTime.Now:MM-dd-yyyy HH:mm:ss}{Environment.NewLine}");

                // Create a Stopwatch instance to measure elapsed time
                Stopwatch stopwatch = Stopwatch.StartNew(); // Start stopwatch

                //=================================================================================================
                // Validate input flags. Ensure that flag settings will not cause conflicts
                //=================================================================================================
                BackTestUtilities.ValidateInputFlagSettings(settings);

                //=================================================================================================
                // Initialize variables
                //=================================================================================================
                int countRuns = 0;
                double sumDeltaAnnualReturn = 0.0;
                double maxDeltaReturn = -1000.0;
                double minDeltaReturn = 1000.0;

                //=================================================================================================
                // Check each Sell Profile for compatibility with reduceSellExecutionPctCrit2DuringSTCProcess and Eliminate non-conforming profiles.
                // To be conforming the value difference between adjacent sellProfileRelativeMrktLevel components must be equivalent.
                // Note:  This routine will crash if the spuriousSellFlag is set to 1.
                //=================================================================================================
                statusUpdater.UpdateStatus("Running: Sell Profile compatibility check ...");
                settings.SP = BackTestUtilities.SellProfileCompatibilityCheck(settings, fileSettings, statusUpdater);

                //=================================================================================================
                // Calculate the number of runs that will be executed.
                //=================================================================================================
                // Calculate / extract the number of runs
                if (settings.PostProcessFlag == 0 || settings.PostProcessFlag == 1)
                {
                    //fileSettings.RunCalculation = settings.NumberOfTimePeriodLoops * MI.count * SP.count * BP.count * STR.count;
                }
                else if (settings.PostProcessFlag == 2)
                {
                    //splitString = regexp(externalResultsFileBase, '_', 'split'); // Split date into day, month, year
                    //splitString2 = regexp(cell2mat(splitString(1, 3)), '-', 'split');
                    //fileSettings.RunCalculation = str2double(splitString2(1, 1)); // Extract the number of runs from end of string
                }
                else
                {
                    fileSettings.RunCalculation = 1;
                }

                statusUpdater.UpdateStatus($"Number of runs to be executed: {fileSettings.RunCalculation}");

                //=================================================================================================
                // File I/O Control
                //=================================================================================================
                statusUpdater.UpdateStatus($"Running File I/O Setup:");

                // Create results folder where all the results for this run will be placed
                string resultFilesFolder = Path.Combine(fileSettings.UserFilesBasePath, fileSettings.ResultFilesFolder);

                if (!Directory.Exists(resultFilesFolder))
                {
                    Directory.CreateDirectory(resultFilesFolder);
                }

                string templateFilePath = null;

                // Define Results.xlsx File
                string resultsXLSFile = Path.Combine(resultFilesFolder, fileSettings.ResultsXLSFile);
                //templateFilePath = Path.Combine(PathDefinitions.InputOutputFilesPath, @"Neil\Results_Neil-v158.xlsx");
                templateFilePath = Path.Combine(fileSettings.UserFilesBasePath, "Results-v158.xlsx");
                File.Copy(templateFilePath, resultsXLSFile, overwrite: true);

                // Define ResultsDetail.xlsx File
                string resultsDetailXLSFile = Path.Combine(resultFilesFolder, fileSettings.ResultsDetailXLSFile);
                //templateFilePath = Path.Combine(PathDefinitions.InputOutputFilesPath, @"Neil\ResultsDetail_Neil.xlsx");
                templateFilePath = Path.Combine(fileSettings.UserFilesBasePath, "ResultsDetail.xlsx");
                File.Copy(templateFilePath, resultsDetailXLSFile, overwrite: true);

                // Define DeferredTransactions.xlsx File
                string deferredTransactionsXLSFile = Path.Combine(resultFilesFolder, fileSettings.DeferredTransactionsXLSFile);
                //templateFilePath = Path.Combine(PathDefinitions.InputOutputFilesPath, @"Neil\ResultsDeferredTransactions_Neil.xlsx");
                templateFilePath = Path.Combine(fileSettings.UserFilesBasePath, "ResultsDeferredTransactions.xlsx");
                File.Copy(templateFilePath, deferredTransactionsXLSFile, overwrite: true);

                // Define Results.csv File
                string resultsCSVFile = Path.Combine(resultFilesFolder, fileSettings.ResultsCSVFile);
                HelperMethods.CreateFile(resultsCSVFile, statusUpdater);

                // Define Results.csv File
                string resultsSummaryCSVFile = Path.Combine(resultFilesFolder, fileSettings.ResultsSummaryCSVFile);
                HelperMethods.CreateFile(resultsSummaryCSVFile, statusUpdater);

                // Define RunSummary.xlsx File
                string runSummaryFile = Path.Combine(resultFilesFolder, fileSettings.RunSummaryFile);
                //templateFilePath = Path.Combine(PathDefinitions.InputOutputFilesPath, @"Neil\RunSummary_Neil.xlsx");
                templateFilePath = Path.Combine(fileSettings.UserFilesBasePath, "RunSummary.xlsx");
                File.Copy(templateFilePath, runSummaryFile, overwrite: true);

                // Define Settings.xlsx File
                string settingsFile = Path.Combine(resultFilesFolder, fileSettings.SettingsFile);
                //templateFilePath = Path.Combine(PathDefinitions.InputOutputFilesPath, @"Neil\SettingsV156.xlsx");
                templateFilePath = Path.Combine(PathDefinitions.InputOutputFilesPath, "SettingsV156.xlsx");
                File.Copy(templateFilePath, settingsFile, overwrite: true);

                //=================================================================================================
                // Read in inflation data
                //=================================================================================================
                //string inflationDataFile = fileSettings.DBFilesPath + "Inflation.xlsx";
                string inflationDataFile = Path.Combine(PathDefinitions.InputOutputFilesPath, "Inflation.xlsx");
                string sheetName = "1914To2092";
                string range = "A4:R181";
                statusUpdater.UpdateStatus("Reading Inflation data file ...");
                List<InflationData> inflationData = BackTestUtilities.ReadInflationData(inflationDataFile, sheetName, range, statusUpdater);

                //<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>
                //Starting BackTest Analysis
                //<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>
                //=================================================================================================
                // Main Loop 
                //=================================================================================================
                //int closePriceColumn = 5; // Column 5 is the close price; Column 6 is the adjusted close price. Select based on analysis objective

                // Read in complementary investment data
                InvestmentData complementaryInvestmentData = new InvestmentData();
                complementaryInvestmentData = InvestmentProcessor.GetInvestmentData(pathDefinitions, complementaryInvestmentToRun, closePriceColumn, false);

                // Remove duplicate tickers
                tickersToRunBT = tickersToRunBT.Distinct().ToList();

                // Remove unwanted tickers
                List<string> tickersToRemove = new List<string>
                {
                    "^W5000",
                    "FZOLX",
                    "GEV",
                    "IBIT",
                    "NUKZ",
                    "QQQI"
                }; // W5000 has mutiple issues, the others dont have enough data          
                tickersToRunBT = tickersToRunBT.Except(tickersToRemove).ToList();

                int tickerCount = tickersToRunBT.Count;

                InvestmentData investmentData = new InvestmentData();
                foreach (var ticker in tickersToRunBT)
                {
                    /// Read in the mainInvestment data
                    investmentData = InvestmentProcessor.GetInvestmentData(pathDefinitions, ticker, closePriceColumn, false);
                    statusUpdater.UpdateStatus($"Starting Main BackTest Loop for ticker: {investmentData.Ticker}");

                    //=================================================================================================
                    // Create a copy of the MI and CI data set so that it can be modified by subsequent routines while maintaining the original
                    //=================================================================================================
                    List<DateTime> mainInvestmentCloseDate = investmentData.DateLog;
                    List<double> mainInvestmentCloseDateNumber = investmentData.MainInvestmentCloseDateNumber;
                    List<double> mainInvestmentClosePrice = investmentData.MainInvestmentClosePrice;
                    List<int> mainInvestmentYearArray = investmentData.YearArray;
                    DateTime startDateAnalysis = investmentData.StartDateAnalysis;
                    DateTime endDateAnalysis = investmentData.EndDateAnalysis;

                    List<DateTime> complementaryinvestmentCloseDate = complementaryInvestmentData.DateLog;
                    List<double> complementaryInvestmentCloseDateNumber = complementaryInvestmentData.MainInvestmentCloseDateNumber;
                    List<double> complementaryInvestmentClosePrice = complementaryInvestmentData.MainInvestmentClosePrice;
                    List<int> complementaryInvestmentYearArray = complementaryInvestmentData.YearArray;
                    DateTime complementaryInvestmentStartDateAnalysis = complementaryInvestmentData.StartDateAnalysis;

                    //=================================================================================================
                    // Ensure compatibility between MI and CI data arrays
                    // This may modify the above arrays that are designated as ref in the method call
                    //=================================================================================================
                    // Modify the MI and CI data sets so that they have compatible dates/elements over their overlapping date ranges
                    //   -First modify CI data to ensure that there are no data points that CI contains that MI doesn't contain
                    //   -Second modify MI data to ensure that there are no data points that MI contains that CI doesn't contain
                    if (settings.ComplementaryInvestmentFlag == 1)
                    {
                        statusUpdater.UpdateStatus("Running: Ensure MI/CI Price Array Compatibility method ...");
                        BackTestUtilities.EnsureArrayCompatibility(ref mainInvestmentCloseDateNumber, ref complementaryInvestmentCloseDateNumber, ref mainInvestmentClosePrice, ref complementaryInvestmentClosePrice, ref mainInvestmentCloseDate, ref complementaryinvestmentCloseDate, ref mainInvestmentYearArray, ref complementaryInvestmentYearArray, statusUpdater);

                        // Adjust date range start date control parameters
                        if (complementaryInvestmentData.StartDateAnalysis < complementaryinvestmentCloseDate.First())
                        {
                            complementaryInvestmentStartDateAnalysis = complementaryinvestmentCloseDate[0];
                        }

                        if (investmentData.StartDateAnalysis < mainInvestmentCloseDate.First())
                        {
                            startDateAnalysis = mainInvestmentCloseDate[0];
                        }
                    }

                    //=================================================================================================
                    // This routine will:
                    // 1. Determine date numbers (startDateNumberAnalysis, endDateNumberAnalysis, startDateNumberRegression, and endDateNumberRegression) based on data input parameters and/or user specified dates
                    // 2. Adjust maininvestmentCloseDate, mainInvestmentCloseDateNumber, mainInvestmentClosePrice arrays based on calculated values for startDateNumberRegression and endDateNumberRegression
                    // 3. This get the data ready for the regressions analysis
                    // The "ref" arrays specified in the method call may be modified inside the routine
                    //=================================================================================================
                    statusUpdater.UpdateStatus("Running: AdjustStartAndEndDateCriteria method ...");

                    // Initialize the dateNumber variables.  The variables below will be calculated in the routine.
                    double startDateNumberAnalysis = 0.0;
                    double endDateNumberAnalysis = 0.0;
                    double startDateNumberRegression = 0.0;
                    double endDateNumberRegression = 0.0;

                    BackTestUtilities.AdjustStartAndEndDateCriteria(ref mainInvestmentCloseDate, ref mainInvestmentCloseDateNumber, ref mainInvestmentClosePrice, settings, startDateAnalysis, complementaryInvestmentStartDateAnalysis, out startDateNumberAnalysis, out endDateNumberAnalysis, out startDateNumberRegression, out endDateNumberRegression, statusUpdater);

                    //=================================================================================================
                    // Filter the MainInvestment data by clipping the closing price to reduce effects of anomalies, including stock market bubbles and excessive sell-offs
                    //=================================================================================================
                    //TODO: Figure out how this routine works.  It seems like it should use the outputs from the Regression routine below.
                    if (settings.MainInvestmentFilterFlag)
                    {
                        statusUpdater.UpdateStatus("Running: InvestmentDataFilteringAlgorithm method ...");
                        mainInvestmentClosePrice = BackTestUtilities.InvestmentDataFilteringAlgorithm(settings.MainInvestmentFilterFlag, settings.PlotFlagHistogram, settings.VerboseFlag, investmentData.MainInvestmentName, investmentData.MainInvestmentCloseDateNumber, investmentData.MainInvestmentClosePrice, settings.NumberOfFilteringIterations, settings.NumberOfFilteringStdDevs, statusUpdater);
                    }

                    //=================================================================================================
                    // Perform a linear regression on the Main Investment in Log10 space.
                    //=================================================================================================
                    statusUpdater.UpdateStatus("Running: Regression method ...");
                    // Declare the output variables before the method call
                    List<double> mainInvestmentValuationWRTZero;
                    List<double> mainInvestmentRegressionValue;
                    List<double> mainInvestmentClosePriceLog10;
                    List<double> fitCloseValueMinus60Pct;
                    List<double> fitCloseValueMinus50Pct;
                    List<double> fitCloseValueMinus40Pct;
                    List<double> fitCloseValueMinus30Pct;
                    List<double> fitCloseValueMinus20Pct;
                    List<double> fitCloseValueMinus10Pct;
                    List<double> fitCloseValue;
                    List<double> fitCloseValuePlus10Pct;
                    List<double> fitCloseValuePlus20Pct;
                    List<double> fitCloseValuePlus30Pct;
                    List<double> fitCloseValuePlus40Pct;
                    List<double> fitCloseValuePlus50Pct;
                    List<double> fitCloseValuePlus60Pct;
                    List<double> coeffsCloseValueLog10;
                    double stdDevFinal;
                    double meanValueFinal;
                    double minRangeFinal;
                    double maxRangeFinal;

                    // Call the Regression method
                    //BackTestUtilities.Regression(settings.VerboseFlag, investmentData.MainInvestmentName, investmentData.MainInvestmentCloseDateNumber, investmentData.MainInvestmentClosePrice, out mainInvestmentValuationWRTZero, out mainInvestmentRegressionValue, out mainInvestmentClosePriceLog10, out stdDevFinal, out meanValueFinal, out minRangeFinal, out maxRangeFinal, out fitCloseValue, out coeffsCloseValueLog10, statusUpdater);

                    BackTestUtilities.Regression(settings.VerboseFlag, investmentData.MainInvestmentName, investmentData.MainInvestmentCloseDateNumber, investmentData.MainInvestmentClosePrice, out mainInvestmentValuationWRTZero, out mainInvestmentRegressionValue, out mainInvestmentClosePriceLog10, out stdDevFinal, out meanValueFinal, out minRangeFinal, out maxRangeFinal, out fitCloseValueMinus60Pct, out fitCloseValueMinus50Pct, out fitCloseValueMinus40Pct, out fitCloseValueMinus30Pct, out fitCloseValueMinus20Pct, out fitCloseValueMinus10Pct, out fitCloseValue, out fitCloseValuePlus10Pct, out fitCloseValuePlus20Pct, out fitCloseValuePlus30Pct, out fitCloseValuePlus40Pct, out fitCloseValuePlus50Pct, out fitCloseValuePlus60Pct, out coeffsCloseValueLog10, statusUpdater);

                    // Write modified ticker history files to .csv file for debugging
                    //CsvWriter.WriteDebugDataToCsv(resultFilesFolder, maininvestmentCloseDate, mainInvestmentYearArray, mainInvestmentCloseDateNumber, mainInvestmentClosePrice, mainInvestmentValuationWRTZero, mainInvestmentRegressionValue, mainInvestmentClosePriceLog10, complementaryinvestmentCloseDate, complementaryInvestmentCloseDateNumber, complementaryInvestmentClosePrice, complementaryInvestmentYearArray, statusUpdater);
                    //CsvFileOpener.OpenCsvFileFromDebug("BackTestDebug.csv");

                    //=================================================================================================
                    //Set Predetermined Time Ranges
                    //=================================================================================================
                    int numberOfTimePeriodLoops = settings.NumberOfTimePeriodLoops;
                    if (settings.UsePredeterminedTimeRangesFlag && numberOfTimePeriodLoops > 1)
                    {
                        statusUpdater.UpdateStatus("Running: SetPredeterminedTimeRanges method ...");
                        BackTestUtilities.SetPredeterminedTimeRanges(ref numberOfTimePeriodLoops, fileSettings.PredeterminedTimeRangesFile, investmentData.DateLog, investmentData.MainInvestmentCloseDateNumber, startDateAnalysis, endDateAnalysis, fileSettings.UserName, statusUpdater);
                    }

                    //=================================================================================================
                    // Iterate over time ranges, sell profiles, buy profiles, and strategies
                    //=================================================================================================

                    //=================================================================================================
                    // Time period loop
                    //=================================================================================================
                    statusUpdater.UpdateStatus("Starting Time Period Loop");

                    // Initialize Variables
                    int day = -1;
                    int caseNo = 0;
                    int runDurationInMarketDays = 0;
                    DateTime startDateAnalysisThisRun;
                    DateTime endDateAnalysisThisRun;
                    double inflationRateAverageEntireTimePeriod;
                    int startingMarketDayThisRun;
                    int endingMarketDayThisRun;
                    double calculatedDurationInCalendarDaysForInflationCalc;
                    double calculatedDurationInCalendarDays;
                    int strategy20Flag = 0;
                    int marketHighDay = 0;
                    double marketHigh = 0;
                    double sharePriceMainInvestment = 0.0;
                    double marketCorrectionFromHigh = 0.0;
                    int marketLowDay = 0;
                    double marketCorrectionFromLow = 0.0;
                    //List<string> date = null;
                    List<DateTime> date = new List<DateTime>(); // Initialize as an empty list


                    for (int timePeriod = 1; timePeriod <= settings.NumberOfTimePeriodLoops; timePeriod++)
                    {
                        BackTestUtilities.RunTimePeriodAnalysis(timePeriod, settings, startDateNumberAnalysis, endDateNumberAnalysis, endDateNumberRegression, investmentData.MainInvestmentCloseDateNumber, investmentData.DateLog, inflationData, out runDurationInMarketDays, out startDateAnalysisThisRun, out endDateAnalysisThisRun, out inflationRateAverageEntireTimePeriod, out startingMarketDayThisRun, out endingMarketDayThisRun, out calculatedDurationInCalendarDaysForInflationCalc, out calculatedDurationInCalendarDays);

                        // Update Case Number
                        BackTestUtilities.UpdateCaseNumber(fileSettings, ref caseNo, statusUpdater);

                        // Initialize date Array
                        //date = Enumerable.Repeat(string.Empty, runDurationInMarketDays).ToList();

                        //string timePeriodMessage = ($"Running: Time Period: {timePeriod}, Case No: {caseNo}, Date range: {startDateAnalysisThisRun:MM-dd-yyyy} to {endDateAnalysisThisRun:MM-dd-yyyy}, Market Days: {runDurationInMarketDays}, Market Yrs: {runDurationInMarketDays / 253.0:F1}, Start Yr: {startDateAnalysisThisRun.Year}, Current Yr: {DateTime.Now.Year}, Avg Inflation Rate: {inflationRateAverageEntireTimePeriod:F2}");
                        string timePeriodMessage = ($"Running: Time Period: {timePeriod}, Case No: {caseNo}, Date range: {startDateAnalysisThisRun:MM-dd-yyyy} to {endDateAnalysisThisRun:MM-dd-yyyy}, Market Days: {runDurationInMarketDays}, Market Yrs: {runDurationInMarketDays / 253.0:F1}, Avg Inflation Rate: {inflationRateAverageEntireTimePeriod:F2}");
                        statusUpdater.UpdateStatus(timePeriodMessage);

                        using (var writer = new StreamWriter(resultsSummaryCSVFile, append: false))
                        {
                            writer.WriteLine(timePeriodMessage);
                        } // Note: File is automatically closed here when the using block ends

                        //=================================================================================================
                        // Sell Profile Loop
                        //================================================================================================
                        foreach (var sellProfile in settings.SP)
                        {
                            //Set-up Sell Profile
                            var sellProfileResult = BackTestUtilities.SellProfileGeneration(settings, sellProfile, statusUpdater);

                            //=================================================================================================
                            // Buy Profile Loop
                            //================================================================================================
                            foreach (var buyProfile in settings.BP)
                            {
                                //Set-up Buy Profile
                                if (buyProfile == 50 && sellProfile != 50) // If evaluating profile 50 then only want to run the buyProfile = 50 / sellProfile = 50 case
                                {
                                    break;
                                }

                                var buyProfileResult = BackTestUtilities.BuyProfileGeneration(settings, buyProfile, statusUpdater);

                                //=================================================================================================
                                // Sell Strategy Loop
                                //================================================================================================
                                foreach (var strategy in settings.STR)
                                {
                                    //Set-up Strategy
                                    if (buyProfile == 50 && sellProfile != 50) // If evaluating profile 50 then only want to run the buyProfile = 50 / sellProfile = 50 case
                                    {
                                        break;
                                    }

                                    var strategyResult = BackTestUtilities.StrategyGeneration(settings, strategy20Flag, strategy, sellProfileResult.SellCriteriaReset, buyProfileResult.BuyCriteriaReset, statusUpdater);

                                    double sellThreshold = strategyResult.SellThreshold;
                                    double buyThreshold = strategyResult.BuyThreshold;


                                    //=================================================================================================
                                    // Primary Back Test Loop
                                    //================================================================================================
                                    // This loop is necessary to eliminate the profile 50 runs when running for multiple strategies

                                    // Initialize variables
                                    //List<List<double>> aTransactions = null;
                                    double dateNumLast = 0.0;
                                    double startingCashPercentForThisRun = 0.0;
                                    double cashAvailableForInitialCIPurchasePercent = 0.0;
                                    List<double> cashAccountBalancesLast30Days = null;

                                    for (int bTest = 1; bTest <= 1; bTest++) // This loop eliminates profile 50 runs for multiple strategies
                                    {
                                        // Set-up Primary Back Test Loop
                                        if ((fileSettings.UserName == "Owner" || fileSettings.UserName == "stick") && buyProfile == 50 && sellProfile == 50 && strategy != settings.STR[0])
                                        {
                                            break; // If evaluating profile 50, only run the buyProfile=50/sellProfile=50 case, and only for one strategy
                                        }

                                        if ((fileSettings.UserName == "Owner" || fileSettings.UserName == "stick") && strategy == 20 && strategy20Flag == 1)
                                        {
                                            break; // If evaluating profile 20, only run the buyProfile=20/sellProfile=20 case, and only for one strategy
                                        }

                                        //=================================================================================================
                                        // Initialize Parameters for Secondary Back Test Loop
                                        //================================================================================================
                                        day = -1;
                                        int cg = -1; // capitalGainArray counter
                                        sharePriceMainInvestment = 0.0;
                                        double regressionPriceMainInvestment;
                                        double startingCash = 0.0;
                                        double sharesMainInvestment = 0.0;
                                        double sharesFullyInvestedMI = 0.0;
                                        double sharesComplementaryInvestment = 0.0;
                                        double startingSharesComplementaryInvestment = 0.0;
                                        double buyPctInitial = 0.0;
                                        double startingShares = 0.0;
                                        double marketValueMISharesMinCriteriaDollarAmt = settings.MarketValueMISharesMinCriteriaPctOfStartingPortfolioValue / 100 * settings.StartingPortfolioValue; //Min share market value to hold in account at all times in todays dollars
                                        double hypotheticalMainInvestmentPriceAtSellThreshold = 0.0;
                                        double hypotheticalMainInvestmentPriceAtBuyThreshold = 0.0;

                                        double marginAccountBalance = 0.0;
                                        List<double> cash = new List<double>();
                                        double cashBalanceMinCriteria = 0.0;
                                        double cashBalanceMinCriteriaDollarAmtAdjusted = 0.0;
                                        int cashInfusionFlag = 0;
                                        int cashWithdrawalFlag = 0;
                                        double lastCashInfusionDateNum = 0.0;
                                        double interestAmountOnCashAccount = 0.0;
                                        double lastInterestCalcDate = 0.0;

                                        double marketValueComplimentaryInvestment = 0.0;
                                        double marketValueMainInvestmentShares = 0.0;
                                        double marketValuePortfolio = 0.0;
                                        double cashAvailableForBuy = 0.0;
                                        double liquidity = 0.0;
                                        double fundsAvailableToBuyAtStartOfDay = 0.0;
                                        double shareBalanceMainInvestmentMinCriteria = 0.0;
                                        double ultimateCashBalanceMinCriteria = 0.0;
                                        double maxAllowedTransactionAmountCurrentYr = 0.0;
                                        double maxAllowedFundsForCIPurchaseCurrentYr = 0.0;
                                        double maxAllowedTransactionAmountCurrentYrSellOrder = 0.0;
                                        double marketValueMISharesMinCriteriaDollarAmtAdjusted = 0.0;
                                        double potentialBuyingPower;
                                        double maxAllowedTransactionAmountAdjusted = 0.0;
                                        double sharesMainInvestmentAvailableToSell;
                                        double sharesMainInvestmentAvailableToSellStartOfDay;
                                        double analysisStartDateNum = 0.0;
                                        double cashWithdrawalCurrentYr = 0.0;
                                        double cashAnnualWithdrawalAmountAdjusted = 0.0;
                                        double maxAllowedFundsForCIPurchaseAdjusted = 0.0;
                                        int sellFlag = 0;
                                        int buyFlag = 0;
                                        double confirmCIBuy = 0.0;
                                        int cntEnableMatchingBuy = 0;

                                        int cashLevelWatchFlag = 0;
                                        marginAccountBalance = 0.0; // Obsolete, but still used
                                        double cashAvailableForInitialCIPurchase;

                                        double movingAverage = 0.0;
                                        double BTC_STCMovingAvg = 0.0;
                                        double movingAverage5 = 0.0;
                                        double movingAverage20 = 0.0;
                                        double movingAverage50 = 0.0;
                                        double movingAverage100 = 0.0;
                                        double movingAverage200 = 0.0;

                                        double movingAverageWRTZero = 0.0;
                                        double BTC_STCMovingAvgWRTZero = 0.0;
                                        double movingAverage5WRTZero = 0.0;
                                        double movingAverage20WRTZero = 0.0;
                                        double movingAverage50WRTZero = 0.0;
                                        double movingAverage100WRTZero = 0.0;
                                        double movingAverage200WRTZero = 0.0;

                                        // Declare variables for each moving average and their rate of changes
                                        double movingAverageRateOfChange = 0.0;
                                        double movingAverageRateOfChangeWRTZero = 0.0;
                                        double movingAverage5RateOfChange = 0.0;
                                        double movingAverage5RateOfChangeWRTZero = 0.0;
                                        double movingAverage20RateOfChange = 0.0;
                                        double movingAverage20RateOfChangeWRTZero = 0.0;
                                        double movingAverage50RateOfChange = 0.0;
                                        double movingAverage50RateOfChangeWRTZero = 0.0;
                                        double movingAverage100RateOfChange = 0.0;
                                        double movingAverage100RateOfChangeWRTZero = 0.0;
                                        double movingAverage200RateOfChange = 0.0;
                                        double movingAverage200RateOfChangeWRTZero = 0.0;
                                        double BTC_STCmovingAverageRateOfChange = 0.0;
                                        double BTC_STCmovingAverageRateOfChangeWRTZero = 0.0;
                                        double averagePriceGain = 0.0;
                                        double averagePriceLoss = 0.0;

                                        double cashWithdrawalAmount = 0.0;
                                        double cashInfusionAmount = 0.0;

                                        // Sell parameters
                                        double lastMainInvestmentValuationWRTSellThreshold = 0.0;
                                        double deltaMainInvestmentWRTSellThreshold = 0.0;
                                        double lastTransactionSellExecutionPctCrit2 = 0.0;
                                        double actualTransactionSellLevelCrit1 = 0.0;
                                        double actualTransactionSellExecutionPctCrit2 = 0.0;
                                        double nextSellLevelCrit1 = 0.0;
                                        double actualMainInvestmentSharePriceAtLastSellTransaction = 0.0;
                                        double lastTransactionSellLevelCrit1 = 0.0;
                                        double currentTransactionSellLevelCrit1;
                                        double lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn = 0.0;
                                        double actualMainInvestmentValuationWRTSellThresholdAtLastTransaction = 0.0;
                                        double deltaSellCriteria = 0.0;
                                        int criteriaDaysSinceLastSellTransactionAtSameLevel = 0;
                                        var sellCriteriaOriginal = strategyResult.SellCriteria.Select(arr => arr.ToArray()).ToList();
                                        var sellCriteria = strategyResult.SellCriteria.Select(arr => arr.ToArray()).ToList();

                                        // Buy parameters
                                        double lastMainInvestmentValuationWRTBuyThreshold = 0.0;
                                        double deltaMainInvestmentWRTBuyThreshold = 0.0;
                                        double lastTransactionBuyExecutionPctCrit2 = 0.0; // Unimportant Variable
                                        double lastTransactionSellExecutionDeltaShares = 0.0;
                                        double actualTransactionBuyLevelCrit1 = 0.0;
                                        double actualTransactionBuyExecutionPctCrit2 = 0.0;
                                        double nextBuyLevelCrit1 = 0.0;
                                        double actualMainInvestmentSharePriceAtLastBuyTransaction = 0.0;
                                        double priceDecreaseFromLastSellOrder = 0.0;
                                        double lastTransactionBuyLevelCrit1 = 0.0;
                                        double currentTransactionBuyLevelCrit1;
                                        double lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution = 0.0;
                                        double actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction = 0.0;
                                        double deltaBuyCriteria = 0.0;
                                        int criteriaDaysSinceLastBuyTransactionAtSameLevel = 0;
                                        var buyCriteriaOriginal = strategyResult.BuyCriteria.Select(arr => arr.ToArray()).ToList();
                                        var buyCriteria = strategyResult.BuyCriteria.Select(arr => arr.ToArray()).ToList();

                                        // Sell Threshold Adjustment parameters
                                        int dayOfLastSell = 100000;
                                        double STCAdjustCriteria = 0.0;
                                        bool STCSellThresholdAdjustmentTracker = false;
                                        int STOrigCrossFlagSTC = 0;
                                        int BTOrigCrossFlagSTC = 0;
                                        int STCLocalBuyEnablementProcessTracker = 0;
                                        double movingAvgLowWRTZeroIntraSTCProcess = 0.0; // Use for cross from BELOW checks
                                        double movingAvgLowIntraSTCProcess = 0.0;
                                        double movingAvgHighWRTZeroIntraSTCProcess = 0.0; // Use for cross from ABOVE checks
                                        double movingAvgHighIntraSTCProcess = 0.0; // Use for intra STC process moving average downtrend checks
                                        double marketHighWRTZeroIntraSTCProcess = 0.0; // Use for intra STC process market downtrend checks
                                        double sellThresholdOriginal = 0.0;
                                        double marketLowIntraSTCProcess = 0.0;
                                        double marketLowWRTZeroIntraSTCProcess = 0.0;
                                        int marketLowDayIntraSTCProcess = 0;
                                        double marketHighIntraSTCProcess = 0.0;
                                        double BTCProcessMarketHigh = 0.0;
                                        double BTCProcessMovingAvgHigh = 0.0;
                                        int STCProcessSellThresholdAdjustmentMarker = 0;

                                        // Buy Threshold Adjustment parameters
                                        int dayOfLastBuy = 100000;
                                        int BTCBuyThresholdAdjustmentTracker = 0;
                                        int BTOrigCrossFlagBTC = 0;
                                        int STOrigCrossFlagBTC = 0;
                                        int STOrigCrossFlagBTCeq1 = 0;
                                        int BTOrigCrossFlagBTCeqM1 = 0;
                                        int BTOrigCrossFlagBTCeq1 = 0;
                                        int STOrigCrossFlagBTCeqM1 = 0;
                                        double buyThresholdOriginal = 0.0;
                                        double BTCProcessMarketHighWRTZero = 0.0;
                                        int BTCProcessMarketHighDay = 0;
                                        double BTCProcessMarketLow = 0.0;
                                        double BTCProcessMarketLowWRTZero = 0.0;
                                        double BTCProcessMovingAvgLow = 0.0;
                                        double BTCProcessMovingAvgHighWRTZero = 0.0;
                                        double BTCProcessMovingAvgLowWRTZero = 0.0;
                                        double BTCProcessMovingAvgIncreaseFromIntraBTCLow = 0.0;
                                        double BTCProcessMarketIncreaseWRTBTCPreemptiveTerminateLow = 0.0;
                                        double BTCProcessMarketIncreaseFromIntraBTCLow = 0.0;
                                        double BTCProcessMarketIncreaseFromIntraBTCLowALT = 0.0;
                                        double BTCProcessMovingAvgIncreaseFromIntraBTCLowALT = 0.0;
                                        int BTCProcessBuyThresholdAdjustmentMarker = 0;

                                        // BTC SellEnablement and PreemptSellEnablement
                                        int BTCLocalSellEnablementProcessTracker = 0;
                                        int BTCPreemptiveTerminateLocalSellEnablePrcsToggle = 0;
                                        double BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZero = 0.0;
                                        int BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZeroDay = 0;
                                        double BTCLocalSellEnablementProcessMarketHighWRTZero = 0.0;
                                        int BTCLocalSellEnablementProcessMarketHighWRTZeroDay = 0;
                                        double BTCAdjustCriteria = 0;

                                        // Sell/Buy Criteria Reset Flags
                                        int sellResetType1Flag;
                                        int sellResetType2Flag;
                                        int buyResetType1Flag;
                                        int buyResetType2Flag;

                                        int dayOfFailcashBalanceMinCriteria = 0;
                                        double lastMainInvestmentValuationWRTZero = 0.0;
                                        marketHighDay = 0;
                                        marketHigh = 0;

                                        double marketHighWRTZero = 0.0;
                                        double marketLow = 0.0;
                                        double marketLowWRTZero = 0.0;
                                        marketLowDay = 0;

                                        double marketTrend;
                                        marketCorrectionFromHigh = 0.0;
                                        marketCorrectionFromLow = 0.0;
                                        bool marketCorrectNegative;
                                        double marketDecentRate;
                                        double marketAccentRate;

                                        // Final calculations
                                        double startingFundsRatio;
                                        double startingMIMrktValueRatio;
                                        double currentMainInvestmentValuationWRTSellThreshold = 0.0;
                                        double currentMainInvestmentValuationWRTBuyThreshold = 0.0;

                                        double currentMainInvestmentValuationWRTZero = 0.0; // Percentage above regression line
                                        double currentMainInvestmentRegressionValue = 0.0; // Value of Regression line on the current day

                                        // Declare capitalGainArray
                                        List<double[]> capitalGainArray = new List<double[]>();

                                        // Declare capitalGainArrayCopy
                                        List<double[]> capitalGainArrayCopy = new List<double[]>();

                                        // Sell Order Results
                                        List<double[]> resultsSellOrder = new List<double[]>();

                                        // Buy Order Results
                                        List<double[]> resultsBuyOrder = new List<double[]>();

                                        // Main results array. Contains end-of-day results for all market days
                                        List<double[]> results = new List<double[]>();

                                        // General results array for portfolio-level data
                                        List<double[]> resultsGeneral = new List<double[]>();

                                        // Technical analysis results array
                                        List<double[]> resultsTechAnal = new List<double[]>();

                                        // Array for updates like interest, cash infusion, and withdrawals
                                        List<double[]> resultsUpdate = new List<double[]>();

                                        // STC (Sell Threshold Criteria) related data
                                        List<double[]> resultsSTC = new List<double[]>();

                                        // BTC (Buy Threshold Criteria) related data
                                        List<double[]> resultsBTC = new List<double[]>();

                                        // TODO: The next two lines are associated with the aTransactions routine that is not working.  Need to fix the routine and uncomment
                                        //List<double[]> aTransactions = new List<double[]>();
                                        //double totalDays;

                                        //=================================================================================================
                                        // Start Secondary Back Test Loop
                                        //================================================================================================
                                        statusUpdater.UpdateStatus($"Running: Secondary BackTest Loop with: Buy Profile = {buyProfile}, Sell Profile = {sellProfile}, Strategy = {strategy}");

                                        for (int i = startingMarketDayThisRun; i <= endingMarketDayThisRun; i++) // Example of loop range
                                        {
                                            day++; // The first day is day=0.
                                            double dateNum = mainInvestmentCloseDateNumber[i];
                                            //statusUpdater.UpdateStatus($"Day: {day}; DateNum: {dateNum}");

                                            date.Add(mainInvestmentCloseDate[i]);

                                            // Extract year from the maininvestmentCloseDate
                                            int year = mainInvestmentCloseDate[i].Year;

                                            // Determine savings amount and interest rate for the current year
                                            int yearIdx = inflationData.FindIndex(inflation => inflation.Year == year); // Find index of current year in inflation data
                                            double savingsCurrentYr = Math.Round(inflationData[yearIdx].AnnualSavingsAmount, 4);

                                            double interestRateCurrentYear;
                                            if (settings.InflationRateFlag == 0)
                                            {
                                                // Interest Rate for the current year is equal to the current inflation rate
                                                interestRateCurrentYear = inflationData[yearIdx].CPI;
                                            }
                                            else if (settings.InflationRateFlag == 1)
                                            {
                                                // Interest Rate for the current year is 2% less than the current inflation rate
                                                interestRateCurrentYear = Math.Max(0.1, inflationData[yearIdx].CPI - 2);
                                            }
                                            else if (settings.InflationRateFlag == 2)
                                            {
                                                // Interest Rate for the current year is 0
                                                interestRateCurrentYear = 0.0;
                                            }
                                            else
                                            {
                                                throw new ArgumentException("Invalid inflationRateFlag value.");
                                            }

                                            sharePriceMainInvestment = mainInvestmentClosePrice[i]; // $/share
                                            regressionPriceMainInvestment = Math.Pow(10, fitCloseValue[i]);

                                            double sharePriceComplementaryInvestment;
                                            if (settings.ComplementaryInvestmentFlag == 1)
                                            {
                                                // Find the share price of the complementary investment for the given date
                                                int idxComplimentaryInvestmentCloseDateNumber = complementaryInvestmentCloseDateNumber.FindIndex(x => x == dateNum);
                                                sharePriceComplementaryInvestment = complementaryInvestmentClosePrice[idxComplimentaryInvestmentCloseDateNumber];
                                            }
                                            else
                                            {
                                                sharePriceComplementaryInvestment = 0.0;
                                            }

                                            currentMainInvestmentValuationWRTZero = mainInvestmentValuationWRTZero[i]; // Percentage above regression line
                                            currentMainInvestmentRegressionValue = mainInvestmentRegressionValue[i]; // Value of Regression line on the current day

                                            //=================================================================================================
                                            // Identify the most recent high or low of consequence
                                            //=================================================================================================
                                            // Capture the market data over the range of startIndex to endIndex
                                            int dataMinimumInNumberOfMarketDays = 253;
                                            int startIndex = i - dataMinimumInNumberOfMarketDays;
                                            int endIndex = i;

                                            List<PriceHistoryData> priceDataList = new List<PriceHistoryData>();
                                            for (int m = startIndex; m < endIndex; m++)
                                            {
                                                priceDataList.Add(new PriceHistoryData { Date = mainInvestmentCloseDate[m], Close = mainInvestmentClosePrice[m] });
                                            }

                                            // Identify the most recent high or low of consequence
                                            // Define threshold criteria
                                            double lossThresholdCriteriaForMostRecentLowOfConsequence = -5;
                                            double gainThresholdCriteriaForMostRecentHighOfConsequence = 5;
                                            var mostRecentHighOrLowOfConsequence = HelperMethods.IdentifyMostRecentHighOrLowOfConsequence(priceDataList, lossThresholdCriteriaForMostRecentLowOfConsequence, gainThresholdCriteriaForMostRecentHighOfConsequence);
                                            //statusUpdater.UpdateStatus($"Most Recent {mostRecentHighOrLowOfConsequence.LevelType} of Consequence: Date = {mostRecentHighOrLowOfConsequence.LevelDate:MM-dd-yyyy}, Price = {mostRecentHighOrLowOfConsequence.LastHighOrLowOfConsequence:F1}, Gain/Loss Since: {mostRecentHighOrLowOfConsequence.GainOrLossSinceLastHighOrLowOfConsequence:F2}%");

                                            //====================================================================================================
                                            // Day 0: Calculate Portfolio Initial Investment/Cash Purchases and Set Initial Parameters (NOTE: day 0 is really day 1, becuase imbeciles who created C# use a start index of 0.  So entire world is turned upsidedown for them)
                                            //====================================================================================================
                                            if (day == 0)
                                            {
                                                analysisStartDateNum = mainInvestmentCloseDateNumber[i]; //Use to calculate calanderDays Elapsed 
                                                double inflationPeriod = calculatedDurationInCalendarDaysForInflationCalc / 365.0;

                                                // Starting Cash Auto Adjust Routine
                                                if (!settings.StartingCashAutoAdjustFlag)
                                                {
                                                    startingCashPercentForThisRun = settings.StartingCashPercent;
                                                }
                                                else
                                                {
                                                    double averageBuySellThresholdMarketDelta = currentMainInvestmentValuationWRTZero - (sellThreshold + buyThreshold) / 2;
                                                    double startingCashPercentBaselineTemp;

                                                    if (averageBuySellThresholdMarketDelta > settings.CurrentMIValuationWRTZeroHighPct)
                                                    {
                                                        startingCashPercentBaselineTemp = settings.StartingCashHighPct;
                                                    }
                                                    else if (averageBuySellThresholdMarketDelta < settings.CurrentMIValuationWRTZeroLowPct)
                                                    {
                                                        startingCashPercentBaselineTemp = settings.StartingCashLowPct;
                                                    }
                                                    else
                                                    {
                                                        startingCashPercentBaselineTemp = settings.StartingCashLowPct + ((averageBuySellThresholdMarketDelta - settings.CurrentMIValuationWRTZeroLowPct) / (settings.CurrentMIValuationWRTZeroHighPct - settings.CurrentMIValuationWRTZeroLowPct)) * (settings.StartingCashHighPct - settings.StartingCashLowPct);
                                                    }

                                                    startingCashPercentForThisRun = startingCashPercentBaselineTemp;
                                                }

                                                // Portfolio Starting Parameters
                                                double startingPortfolioValueInflationAdjusted = settings.StartingPortfolioValue / Math.Pow(1 + inflationRateAverageEntireTimePeriod / 100.0, inflationPeriod);
                                                cashBalanceMinCriteriaDollarAmtAdjusted = settings.CashBalanceMinCriteriaDollarAmt / Math.Pow(1 + inflationRateAverageEntireTimePeriod / 100.0, inflationPeriod);
                                                cashAnnualWithdrawalAmountAdjusted = settings.CashAnnualWithdrawalAmount / Math.Pow(1 + inflationRateAverageEntireTimePeriod / 100.0, inflationPeriod);
                                                //marketValueMISharesMinCriteriaDollarAmtAdjusted = settings.MarketValueMISharesMinCriteriaDollarAmt / Math.Pow(1 + inflationRateAverageEntireTimePeriod / 100.0, inflationPeriod);
                                                marketValueMISharesMinCriteriaDollarAmtAdjusted = marketValueMISharesMinCriteriaDollarAmt / Math.Pow(1 + inflationRateAverageEntireTimePeriod / 100.0, inflationPeriod);
                                                maxAllowedTransactionAmountAdjusted = settings.MaxAllowedTransactionAmount / Math.Pow(1 + inflationRateAverageEntireTimePeriod / 100.0, inflationPeriod);
                                                maxAllowedFundsForCIPurchaseAdjusted = settings.MaxAllowedFundsForCIPurchase / Math.Pow(1 + inflationRateAverageEntireTimePeriod / 100.0, inflationPeriod);

                                                if (!settings.CIInitialSetupFlag) // Original Method
                                                {
                                                    if (settings.ComplementaryInvestmentFlag == 0) // Divide funds between Cash and shares of Main Investment; sharesComplimentaryInvestment=0
                                                    {
                                                        startingShares = startingPortfolioValueInflationAdjusted * (1 - startingCashPercentForThisRun / 100.0) / sharePriceMainInvestment;
                                                        startingCash = startingPortfolioValueInflationAdjusted * startingCashPercentForThisRun / 100.0;
                                                        buyPctInitial = (1 - startingCashPercentForThisRun / 100.0) * 100.0;
                                                        cash.Add(startingCash);
                                                        sharesMainInvestment = startingShares;
                                                        cashAvailableForInitialCIPurchasePercent = 0.0;
                                                        sharesComplementaryInvestment = 0.0;
                                                        startingSharesComplementaryInvestment = 0.0;
                                                    }
                                                    else if (settings.ComplementaryInvestmentFlag == 1) // Divide funds between Cash, shares of CI, and  shares of Main Investment
                                                    {
                                                        startingShares = startingPortfolioValueInflationAdjusted * (1.0 - startingCashPercentForThisRun / 100.0) / sharePriceMainInvestment;
                                                        buyPctInitial = (1.0 - startingCashPercentForThisRun / 100.0) * 100.0;
                                                        double cashBalanceMinCriteriaDollarAmtAdjustedPct = cashBalanceMinCriteriaDollarAmtAdjusted / startingPortfolioValueInflationAdjusted * 100.0;

                                                        if (!settings.CashBalanceMinCriteriaFlag && startingCashPercentForThisRun > cashBalanceMinCriteriaDollarAmtAdjustedPct)
                                                        {
                                                            cashAvailableForInitialCIPurchasePercent = startingCashPercentForThisRun - cashBalanceMinCriteriaDollarAmtAdjustedPct;
                                                            startingCashPercentForThisRun = cashBalanceMinCriteriaDollarAmtAdjustedPct;
                                                        }
                                                        else if (settings.CashBalanceMinCriteriaFlag && startingCashPercentForThisRun > settings.CashBalanceMinCriteriaPct)
                                                        {
                                                            cashAvailableForInitialCIPurchasePercent = startingCashPercentForThisRun - settings.CashBalanceMinCriteriaPct;
                                                            startingCashPercentForThisRun = settings.CashBalanceMinCriteriaPct;
                                                        }
                                                        else
                                                        {
                                                            cashAvailableForInitialCIPurchasePercent = 0.0;
                                                        }

                                                        startingCash = startingPortfolioValueInflationAdjusted * startingCashPercentForThisRun / 100.0;
                                                        cash.Add(startingCash);
                                                        sharesMainInvestment = startingShares;
                                                        cashAvailableForInitialCIPurchase = startingPortfolioValueInflationAdjusted * cashAvailableForInitialCIPurchasePercent / 100.0;
                                                        startingSharesComplementaryInvestment = cashAvailableForInitialCIPurchase / sharePriceComplementaryInvestment;
                                                    }
                                                }
                                                else if (settings.CIInitialSetupFlag) // New Method
                                                {
                                                    if (settings.ComplementaryInvestmentFlag == 0 || (settings.ComplementaryInvestmentFlag == 1 && settings.EnableCIBuysBasedOnMarketLevel)) // Divide funds between Cash and shares of Main Investment; sharesComplimentaryInvestment=0
                                                    {
                                                        startingShares = startingPortfolioValueInflationAdjusted * (1.0 - startingCashPercentForThisRun / 100.0) / sharePriceMainInvestment;
                                                        startingCash = startingPortfolioValueInflationAdjusted * startingCashPercentForThisRun / 100.0;
                                                        buyPctInitial = (1.0 - startingCashPercentForThisRun / 100.0) * 100.0;
                                                        cash.Add(startingCash);
                                                        sharesMainInvestment = startingShares;
                                                        sharesComplementaryInvestment = 0.0;
                                                        startingSharesComplementaryInvestment = 0.0;
                                                    }
                                                    else if (settings.ComplementaryInvestmentFlag == 1 && !settings.EnableCIBuysBasedOnMarketLevel) // Divide funds between Cash, shares of CI, and shares of Main Investment;
                                                    {
                                                        startingShares = startingPortfolioValueInflationAdjusted * (1.0 - startingCashPercentForThisRun / 100.0) / sharePriceMainInvestment;
                                                        buyPctInitial = (1.0 - startingCashPercentForThisRun / 100.0) * 100.0;
                                                        double cashBalanceMinCriteriaDollarAmtAdjustedPct = cashBalanceMinCriteriaDollarAmtAdjusted / startingPortfolioValueInflationAdjusted * 100.0;

                                                        if (!settings.CashBalanceMinCriteriaFlag && startingCashPercentForThisRun > cashBalanceMinCriteriaDollarAmtAdjustedPct)
                                                        {
                                                            cashAvailableForInitialCIPurchasePercent = startingCashPercentForThisRun - cashBalanceMinCriteriaDollarAmtAdjustedPct;
                                                            startingCashPercentForThisRun = cashBalanceMinCriteriaDollarAmtAdjustedPct;
                                                        }
                                                        else if (settings.CashBalanceMinCriteriaFlag && startingCashPercentForThisRun > settings.CashBalanceMinCriteriaPct)
                                                        {
                                                            cashAvailableForInitialCIPurchasePercent = startingCashPercentForThisRun - settings.CashBalanceMinCriteriaPct;
                                                            startingCashPercentForThisRun = settings.CashBalanceMinCriteriaPct;
                                                        }
                                                        else
                                                        {
                                                            cashAvailableForInitialCIPurchasePercent = 0.0;
                                                        }

                                                        startingCash = startingPortfolioValueInflationAdjusted * startingCashPercentForThisRun / 100.0;
                                                        cash.Add(startingCash);
                                                        sharesMainInvestment = startingShares;
                                                        cashAvailableForInitialCIPurchase = startingPortfolioValueInflationAdjusted * cashAvailableForInitialCIPurchasePercent / 100.0;
                                                        startingSharesComplementaryInvestment = cashAvailableForInitialCIPurchase / sharePriceComplementaryInvestment;
                                                    }
                                                }

                                                sharesComplementaryInvestment = startingSharesComplementaryInvestment;
                                                sharesFullyInvestedMI = startingPortfolioValueInflationAdjusted / sharePriceMainInvestment;
                                                shareBalanceMainInvestmentMinCriteria = settings.ShareBalanceMainInvestmentMinCriteriaPct / 100.0 * sharesMainInvestment;

                                                if (!settings.BackTestApproachFlag)
                                                {
                                                    lastInterestCalcDate = dateNum;
                                                }
                                                else if (settings.BackTestApproachFlag)
                                                {
                                                    lastCashInfusionDateNum = dateNum;
                                                }

                                                // Market Parameters
                                                lastMainInvestmentValuationWRTZero = currentMainInvestmentValuationWRTZero;
                                                marketHigh = sharePriceMainInvestment;
                                                marketHighWRTZero = currentMainInvestmentValuationWRTZero;
                                                marketHighDay = day;
                                                marketLow = sharePriceMainInvestment;
                                                marketLowWRTZero = currentMainInvestmentValuationWRTZero;
                                                marketLowDay = day;

                                                // Sell parameters
                                                lastTransactionSellLevelCrit1 = strategyResult.SellCriteria.Min(x => x[0]) - 0.01;
                                                currentTransactionSellLevelCrit1 = strategyResult.SellCriteria.Min(x => x[0]);
                                                lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn = strategyResult.SellCriteria.Min(x => x[0]) - 0.01;
                                                actualMainInvestmentValuationWRTSellThresholdAtLastTransaction = lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn;
                                                deltaSellCriteria = strategyResult.SellCriteria[1][0] - strategyResult.SellCriteria[0][0];
                                                criteriaDaysSinceLastSellTransactionAtSameLevel = settings.CriteriaDaysSinceLastSellTransactionAtSameLevelDefault;

                                                // Buy parameters
                                                lastTransactionBuyLevelCrit1 = strategyResult.BuyCriteria.Max(x => x[0]) + 0.01;
                                                currentTransactionBuyLevelCrit1 = strategyResult.BuyCriteria.Max(x => x[0]);
                                                lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution = strategyResult.BuyCriteria.Max(x => x[0]) + 0.01;
                                                actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction = lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution;
                                                deltaBuyCriteria = strategyResult.BuyCriteria[1][0] - strategyResult.BuyCriteria[0][0];
                                                criteriaDaysSinceLastBuyTransactionAtSameLevel = settings.CriteriaDaysSinceLastBuyTransactionAtSameLevelDefault;

                                                // Sell Threshold Adjustment parameters
                                                sellThresholdOriginal = strategyResult.SellThreshold;
                                                marketLowIntraSTCProcess = sharePriceMainInvestment;
                                                marketLowWRTZeroIntraSTCProcess = currentMainInvestmentValuationWRTZero;
                                                marketLowDayIntraSTCProcess = day;
                                                marketHighIntraSTCProcess = sharePriceMainInvestment;
                                                marketHighWRTZeroIntraSTCProcess = currentMainInvestmentValuationWRTZero;
                                                BTCProcessMovingAvgHigh = sharePriceMainInvestment;

                                                // Set STCAdjustCriteria
                                                if (!settings.STCAdjustmentTypeCriteria)
                                                {
                                                    if (sellThresholdOriginal > settings.STCAdjustmentTriggerLevelCriteria)
                                                    {
                                                        STCAdjustCriteria = settings.STCAdjustmentTriggerLevelCriteria;
                                                    }
                                                    else
                                                    {
                                                        STCAdjustCriteria = sellThresholdOriginal;
                                                    }
                                                }
                                                else if (settings.STCAdjustmentTypeCriteria)
                                                {
                                                    STCAdjustCriteria = sellThresholdOriginal + settings.STCAdjustmentTriggerLevelCriteria;
                                                }

                                                // Buy Threshold Adjustment parameters
                                                buyThresholdOriginal = strategyResult.BuyThreshold;
                                                BTCProcessMarketHigh = sharePriceMainInvestment;
                                                BTCProcessMarketHighWRTZero = currentMainInvestmentValuationWRTZero;
                                                BTCProcessMarketHighDay = day;
                                                BTCProcessMarketLow = sharePriceMainInvestment;
                                                BTCProcessMarketLowWRTZero = currentMainInvestmentValuationWRTZero;
                                                BTCProcessMovingAvgLow = sharePriceMainInvestment;
                                                BTCProcessMovingAvgHighWRTZero = currentMainInvestmentValuationWRTZero;
                                                BTCProcessMovingAvgLowWRTZero = currentMainInvestmentValuationWRTZero;

                                                // BTC SellEnablement and PreemptSellEnablement
                                                BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZero = currentMainInvestmentValuationWRTZero;
                                                BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZeroDay = day;
                                                BTCLocalSellEnablementProcessMarketHighWRTZero = currentMainInvestmentValuationWRTZero;
                                                BTCLocalSellEnablementProcessMarketHighWRTZeroDay = day;

                                                // Set BTCAdjustCriteria
                                                if (!settings.BTCAdjustmentTypeCriteria) // Absolute adjustment
                                                {
                                                    if (buyThresholdOriginal < settings.BTCAdjustmentTriggerLevelCriteria)
                                                    {
                                                        BTCAdjustCriteria = settings.BTCAdjustmentTriggerLevelCriteria;
                                                    }
                                                    else
                                                    {
                                                        BTCAdjustCriteria = buyThresholdOriginal;
                                                    }
                                                }
                                                else if (settings.BTCAdjustmentTypeCriteria) // Relative adjustment
                                                {
                                                    BTCAdjustCriteria = buyThresholdOriginal + settings.BTCAdjustmentTriggerLevelCriteria;
                                                }

                                                // Final calculations
                                                if (fileSettings.RunCalculation <= settings.MaxRuns)
                                                {
                                                    marketValueComplimentaryInvestment = sharesComplementaryInvestment * sharePriceComplementaryInvestment;
                                                    marketValueMainInvestmentShares = sharesMainInvestment * sharePriceMainInvestment;
                                                    startingFundsRatio = (startingCash + marketValueComplimentaryInvestment) / startingPortfolioValueInflationAdjusted * 100.0;
                                                    startingMIMrktValueRatio = marketValueMainInvestmentShares / startingPortfolioValueInflationAdjusted * 100.0;
                                                    currentMainInvestmentValuationWRTSellThreshold = currentMainInvestmentValuationWRTZero - strategyResult.SellThreshold;
                                                    currentMainInvestmentValuationWRTBuyThreshold = currentMainInvestmentValuationWRTZero - strategyResult.BuyThreshold;
                                                }
                                            }
                                            else
                                            {
                                                cash.Add(Math.Round(cash[day - 1], 8)); // This sets the cash balance for the current day equal to the cash balance from the end of the previous day
                                            }

                                            //====================================================================================================
                                            // Day 1 to End: Update Market Parameters for market day 1 and on (NOTE: day 1 is really day 2)
                                            //====================================================================================================
                                            // Update portfolio market value components
                                            marketValueMainInvestmentShares = sharesMainInvestment * sharePriceMainInvestment;
                                            marketValueComplimentaryInvestment = sharesComplementaryInvestment * sharePriceComplementaryInvestment;
                                            marketValuePortfolio = cash[day] + marketValueMainInvestmentShares + marketValueComplimentaryInvestment - marginAccountBalance;

                                            double calendarDaysElapsed = dateNum - analysisStartDateNum;

                                            // Update minimum share balance criteria                                            
                                            if (!settings.ShareBalanceMainInvestmentMinCriteriaDynamicFlag)
                                            {
                                                if (!settings.ShareBalanceMainInvestmentMinCriteriaCalcMethodFlag)
                                                {
                                                    shareBalanceMainInvestmentMinCriteria = settings.ShareBalanceMainInvestmentMinCriteriaPct / 100.0 * marketValuePortfolio / sharePriceMainInvestment;
                                                }
                                                else if (settings.ShareBalanceMainInvestmentMinCriteriaCalcMethodFlag)
                                                {
                                                    shareBalanceMainInvestmentMinCriteria = settings.ShareBalanceMainInvestmentMinCriteriaPct / 100.0 * sharesMainInvestment;
                                                }
                                            }
                                            else if (settings.ShareBalanceMainInvestmentMinCriteriaDynamicFlag)
                                            {
                                                double shareBalanceMainInvestmentMinCriteriaPctDynamic = 90.0 + ((currentMainInvestmentValuationWRTZero - (-50.0)) / (50.0 - (-50.0))) * (settings.ShareBalanceMainInvestmentMinCriteriaPct - 90.0);

                                                if (!settings.ShareBalanceMainInvestmentMinCriteriaCalcMethodFlag)
                                                {
                                                    shareBalanceMainInvestmentMinCriteria = shareBalanceMainInvestmentMinCriteriaPctDynamic / 100.0 * marketValuePortfolio / sharePriceMainInvestment;
                                                }
                                                else if (settings.ShareBalanceMainInvestmentMinCriteriaCalcMethodFlag)
                                                {
                                                    shareBalanceMainInvestmentMinCriteria = shareBalanceMainInvestmentMinCriteriaPctDynamic / 100.0 * sharesMainInvestment;
                                                }
                                            }

                                            if (settings.UseUltimateShareBalanceMainInvestmentMinCriteriaFlag)
                                            {
                                                double ultimateShareBalanceMainInvestmentMinCriteria = (marketValueMISharesMinCriteriaDollarAmtAdjusted * Math.Pow(1 + inflationRateAverageEntireTimePeriod / 100.0, calendarDaysElapsed / 365.0)) / sharePriceMainInvestment;
                                                shareBalanceMainInvestmentMinCriteria = Math.Max(shareBalanceMainInvestmentMinCriteria, ultimateShareBalanceMainInvestmentMinCriteria);
                                            }

                                            // Update minimum cash balance criteria
                                            if (!settings.CashBalanceMinCriteriaFlag)
                                            {
                                                if (!settings.CashBalanceMinCriteriaDynamicFlag)
                                                {
                                                    cashBalanceMinCriteria = cashBalanceMinCriteriaDollarAmtAdjusted * Math.Pow(1 + inflationRateAverageEntireTimePeriod / 100.0, calendarDaysElapsed / 365.0);
                                                }
                                                else if (settings.CashBalanceMinCriteriaDynamicFlag)
                                                {
                                                    double cashBalanceMinCriteriaDollarAmtAdjustedMin = cashBalanceMinCriteriaDollarAmtAdjusted * Math.Pow(1.0 + inflationRateAverageEntireTimePeriod / 100.0, calendarDaysElapsed / 365.0);
                                                    double cashBalanceMinCriteriaDollarAmtAdjustedMax = cashBalanceMinCriteriaDollarAmtAdjustedMin * 10.0;

                                                    cashBalanceMinCriteria = cashBalanceMinCriteriaDollarAmtAdjustedMin + ((currentMainInvestmentValuationWRTZero - (-50.0)) / (50.0 - (-50.0))) * (cashBalanceMinCriteriaDollarAmtAdjustedMax - cashBalanceMinCriteriaDollarAmtAdjustedMin);
                                                }
                                            }
                                            else if (settings.CashBalanceMinCriteriaFlag)
                                            {
                                                if (!settings.CashBalanceMinCriteriaDynamicFlag)
                                                {
                                                    if (!settings.CashBalanceMinCriteriaCalcMethodFlag)
                                                    {
                                                        cashBalanceMinCriteria = settings.CashBalanceMinCriteriaPct / 100.0 * marketValuePortfolio;
                                                    }
                                                    else if (settings.CashBalanceMinCriteriaCalcMethodFlag)
                                                    {
                                                        cashBalanceMinCriteria = settings.CashBalanceMinCriteriaPct / 100.0 * cash[day];
                                                    }
                                                }
                                                else if (settings.CashBalanceMinCriteriaDynamicFlag)
                                                {
                                                    double cashBalanceMinCriteriaPctDynamic = settings.CashBalanceMinCriteriaPct + ((currentMainInvestmentValuationWRTZero - (-50.0)) / (50.0 - (-50.0))) * (90.0 - settings.CashBalanceMinCriteriaPct);

                                                    if (!settings.CashBalanceMinCriteriaCalcMethodFlag)
                                                    {
                                                        cashBalanceMinCriteria = cashBalanceMinCriteriaPctDynamic / 100.0 * marketValuePortfolio;
                                                    }
                                                    else if (settings.CashBalanceMinCriteriaCalcMethodFlag)
                                                    {
                                                        cashBalanceMinCriteria = cashBalanceMinCriteriaPctDynamic / 100.0 * cash[day];
                                                    }
                                                }
                                            }

                                            if (settings.UseUltimateCashBalanceMinCriteriaFlag)
                                            {
                                                ultimateCashBalanceMinCriteria = cashBalanceMinCriteriaDollarAmtAdjusted * Math.Pow(1.0 + inflationRateAverageEntireTimePeriod / 100.0, calendarDaysElapsed / 365.0);
                                                cashBalanceMinCriteria = Math.Max(cashBalanceMinCriteria, ultimateCashBalanceMinCriteria);
                                            }

                                            if (cashBalanceMinCriteria / cash[day] >= 0.90)
                                            {
                                                //statusUpdater.UpdateStatus($"WARNING: Cash level is very low. Day={day}, dateNum={dateNum}, cashBalanceMinCriteria={cashBalanceMinCriteria}, cash(day)={cash[day]}");
                                            }

                                            // Update buy/sell control parameters
                                            cashWithdrawalCurrentYr = cashAnnualWithdrawalAmountAdjusted * Math.Pow(1.0 + inflationRateAverageEntireTimePeriod / 100.0, calendarDaysElapsed / 365.0);
                                            maxAllowedTransactionAmountCurrentYr = maxAllowedTransactionAmountAdjusted * Math.Pow(1.0 + inflationRateAverageEntireTimePeriod / 100.0, calendarDaysElapsed / 365.0);
                                            maxAllowedTransactionAmountCurrentYrSellOrder = maxAllowedTransactionAmountCurrentYr * settings.SellOrderFactor;
                                            maxAllowedFundsForCIPurchaseCurrentYr = maxAllowedFundsForCIPurchaseAdjusted * Math.Pow(1.0 + inflationRateAverageEntireTimePeriod / 100.0, calendarDaysElapsed / 365.0);

                                            cashAvailableForBuy = Math.Round(cash[day] - cashBalanceMinCriteria, 8);
                                            potentialBuyingPower = Math.Round(cashAvailableForBuy + marketValueComplimentaryInvestment, 8);
                                            fundsAvailableToBuyAtStartOfDay = Math.Max(cashAvailableForBuy, potentialBuyingPower);
                                            liquidity = Math.Round(cash[day] + marketValueComplimentaryInvestment, 8);

                                            sharesMainInvestmentAvailableToSell = sharesMainInvestment - shareBalanceMainInvestmentMinCriteria;
                                            sharesMainInvestmentAvailableToSellStartOfDay = sharesMainInvestmentAvailableToSell;

                                            // Reset Sell/Buy Criteria Reset Flags for each loop
                                            sellResetType1Flag = 0;
                                            sellResetType2Flag = 0;
                                            buyResetType1Flag = 0;
                                            buyResetType2Flag = 0;

                                            STCProcessSellThresholdAdjustmentMarker = 0; // Reset to 0 for each loop
                                            BTCProcessBuyThresholdAdjustmentMarker = 0; // Reset to 0 for each loop

                                            if (sellFlag == 1)
                                            {
                                                criteriaDaysSinceLastBuyTransactionAtSameLevel = settings.CriteriaDaysSinceLastBuyTransactionAtSameLevelDefault; // Reset
                                            }

                                            if (buyFlag == 1)
                                            {
                                                criteriaDaysSinceLastSellTransactionAtSameLevel = settings.CriteriaDaysSinceLastSellTransactionAtSameLevelDefault; // Reset
                                            }

                                            // Check the status of cashAvailableForBuy and sharesMainInvestmentAvailableToSell
                                            if (cashAvailableForBuy < 0)
                                            {
                                                if (cashLevelWatchFlag == 0) // Write out message the cash balance is low. Set cashLevelWatchFlag
                                                {
                                                    if (settings.VerboseCashBalanceFlag)
                                                    {
                                                        statusUpdater.UpdateStatus($"              WARNING: Min cash criteria reached:  Day={day}, DateNum={dateNum}, Cash=${cash[day]:0.2f}, MinCashCriteria=${cashBalanceMinCriteria:0.2f}, SharesMI={sharesMainInvestment:0.1f}");
                                                    }

                                                    cashLevelWatchFlag = 1;
                                                    dayOfFailcashBalanceMinCriteria = day;
                                                }
                                            }
                                            else if (cashAvailableForBuy >= 0)
                                            {
                                                if (cashLevelWatchFlag == 1)
                                                {
                                                    if (settings.VerboseCashBalanceFlag)
                                                    {
                                                        statusUpdater.UpdateStatus($"              INFO: Day={day}, Delta Days={(day - dayOfFailcashBalanceMinCriteria)}, DateNum={dateNum}. Your cash balance of ${cash[day]:0.2f} has returned to a level above the required cashBalanceMinCriteria of ${cashBalanceMinCriteria:0.2f}");
                                                    }

                                                    cashLevelWatchFlag = 0;
                                                }
                                            }

                                            if (sharesMainInvestmentAvailableToSell < 0.0)
                                            {
                                                if (settings.VerboseSharesBalanceFlag)
                                                {
                                                    statusUpdater.UpdateStatus($"              WARNING: Day={day:0}. Min shares criteria reached: SharesMIAvailaToSell={sharesMainInvestmentAvailableToSell:0}, shareBalanceMainInvestmentMinCriteria={shareBalanceMainInvestmentMinCriteria:0.1f}, SharesMI={sharesMainInvestment:0.1f}, PriceMI={sharePriceMainInvestment:0.1f}, PortfolioValue={marketValuePortfolio:0.1f}");
                                                }
                                                // sharesMainInvestmentAvailableToSell = 0.0; // Uncomment if adjustment is required
                                            }

                                            // Calculate Market Trends
                                            // Update market high and low values
                                            if (sharePriceMainInvestment > marketHigh)
                                            {
                                                marketHigh = sharePriceMainInvestment;
                                                marketHighWRTZero = currentMainInvestmentValuationWRTZero;
                                                marketHighDay = day;
                                            }
                                            else if (sharePriceMainInvestment < marketLow)
                                            {
                                                marketLow = sharePriceMainInvestment;
                                                marketLowWRTZero = currentMainInvestmentValuationWRTZero;
                                                marketLowDay = day;
                                            }

                                            // Calculate market trend
                                            marketTrend = currentMainInvestmentValuationWRTZero - lastMainInvestmentValuationWRTZero;

                                            // Calculate corrections from market high and low
                                            marketCorrectionFromHigh = ((sharePriceMainInvestment - marketHigh) / marketHigh) * 100.0;
                                            marketCorrectionFromLow = ((sharePriceMainInvestment - marketLow) / marketLow) * 100.0;

                                            // Determine if market correction is negative
                                            marketCorrectNegative = marketCorrectionFromHigh < 0.0; // true if negative, false otherwise

                                            // Calculate market descent rate
                                            if (sharePriceMainInvestment < marketHigh)
                                            {
                                                marketDecentRate = 100.0 * ((sharePriceMainInvestment - marketHigh) / marketHigh) / (day - marketHighDay);
                                            }
                                            else
                                            {
                                                marketDecentRate = 0.0;
                                            }

                                            // Calculate market ascent rate
                                            if (sharePriceMainInvestment > marketLow)
                                            {
                                                marketAccentRate = 100.0 * ((sharePriceMainInvestment - marketLow) / marketLow) / (day - marketLowDay);
                                            }
                                            else
                                            {
                                                marketAccentRate = 0.0;
                                            }

                                            //======================================================================
                                            // Calculate Moving Averages
                                            // ================================================================
                                            double movingAverageWRTZeroLast = movingAverageWRTZero;
                                            // Default Moving Average (3-day)
                                            BackTestUtilities.MovingAverage(ref movingAverage, ref movingAverageWRTZero, out movingAverageRateOfChange, out movingAverageRateOfChangeWRTZero, settings.MovingAverageLookBackDaysInitial, i, day, mainInvestmentClosePrice, mainInvestmentValuationWRTZero);

                                            // Moving Average Buy/Sell (BS) Threshold Control
                                            BackTestUtilities.MovingAverage(ref BTC_STCMovingAvg, ref BTC_STCMovingAvgWRTZero, out BTC_STCmovingAverageRateOfChange, // Replace with appropriate variable if different
                                                out BTC_STCmovingAverageRateOfChangeWRTZero, // Replace with appropriate variable if different
                                                settings.MovingAverageBSThresholdControlLookBackDaysInitial, i, day, mainInvestmentClosePrice, mainInvestmentValuationWRTZero);

                                            // Moving Average 5-day (1 week)
                                            BackTestUtilities.MovingAverage(ref movingAverage5, ref movingAverage5WRTZero, out movingAverage5RateOfChange, // Replace with appropriate variable if different
                                                out movingAverage5RateOfChangeWRTZero, 5, i, day, mainInvestmentClosePrice, mainInvestmentValuationWRTZero);

                                            // Moving Average 20-day (4 week, 1 month)
                                            BackTestUtilities.MovingAverage(ref movingAverage20, ref movingAverage20WRTZero, out movingAverage20RateOfChange, // Replace with appropriate variable if different
                                                out movingAverage20RateOfChangeWRTZero, // Replace with appropriate variable if different
                                                20, i, day, mainInvestmentClosePrice, mainInvestmentValuationWRTZero);

                                            // Moving Average 50-day (10 week, 2.5 month)
                                            BackTestUtilities.MovingAverage(ref movingAverage50, ref movingAverage50WRTZero, out movingAverage50RateOfChange, // Replace with appropriate variable if different
                                                out movingAverage50RateOfChangeWRTZero, // Replace with appropriate variable if different
                                                50, i, day, mainInvestmentClosePrice, mainInvestmentValuationWRTZero);

                                            // Moving Average 100-day (20 week, 5 month)
                                            BackTestUtilities.MovingAverage(ref movingAverage100, ref movingAverage100WRTZero, out movingAverage100RateOfChange, // Replace with appropriate variable if different
                                                out movingAverage100RateOfChangeWRTZero, // Replace with appropriate variable if different
                                                100, i, day, mainInvestmentClosePrice, mainInvestmentValuationWRTZero);

                                            // Moving Average 200-day (40 week, 10 month)
                                            BackTestUtilities.MovingAverage(ref movingAverage200, ref movingAverage200WRTZero, out movingAverage200RateOfChange, // Replace with appropriate variable if different
                                                out movingAverage200RateOfChangeWRTZero, // Replace with appropriate variable if different
                                                200, i, day, mainInvestmentClosePrice, mainInvestmentValuationWRTZero);

                                            //======================================================================
                                            // Calculate BOLLINGER Bands (50, 2 sigma)
                                            //======================================================================
                                            double lowerBollingerBand, upperBollingerBand, standardDeviation, BBRatioStandardDeviations;

                                            BackTestUtilities.BollingerBands(movingAverage50, mainInvestmentClosePrice, i, 50, 2, out lowerBollingerBand, out upperBollingerBand, out standardDeviation, out BBRatioStandardDeviations, statusUpdater);

                                            //======================================================================
                                            //Calculate Relative Strength Index (RSI)
                                            //======================================================================
                                            double relativeStrengthIndex;
                                            int relativeStrengthIndexLookBackDays = 14;
                                            // Method call
                                            BackTestUtilities.RelativeStrengthIndex(ref averagePriceGain, ref averagePriceLoss, mainInvestmentClosePrice, i, day, relativeStrengthIndexLookBackDays, out relativeStrengthIndex);


                                            //======================================================================
                                            // Run Sell Threshold Control (STC) Algorithm; Sell Threshold Adjustment
                                            //======================================================================
                                            if (settings.SellThresholdControlFlag)
                                            {
                                                // Initial Settings
                                                // Update intra STC process market low (relative to current STC process)
                                                if (currentMainInvestmentValuationWRTZero < marketLowWRTZeroIntraSTCProcess)
                                                {
                                                    marketLowIntraSTCProcess = sharePriceMainInvestment;
                                                    marketLowWRTZeroIntraSTCProcess = currentMainInvestmentValuationWRTZero; // Criteria used for determining sellThreshold adjust
                                                    marketLowDayIntraSTCProcess = day;
                                                }

                                                // Adjust Sell Threshold (ST) to a lower level if criteria is met.
                                                // ========================================================================================================
                                                // When ST is adjusted (ST_Adjusted) to a lower value, the SellThresholdAdjust (STA) algorithm is enabled (STCSellThresholdAdjustmentTracker = 1),
                                                // and it stays that way until an adjust-to-ST_Orig event occurs at which time the STCSellThresholdAdjustmentTracker is set back to 0.
                                                // While the STCSellThresholdAdjustmentTracker = 1, sell orders are controlled by the value of ST_Adjusted.
                                                // The selling will continue up through and across the BuyThresholdOrig (BT_Orig) and ST_Orig lines and to higher market highs based on the sell profile.
                                                // Meanwhile, all buy orders are suspended unless the market crosses back over the BT_Orig plus the buffer or tolerance.
                                                // However, many different scenarios could develop that must be considered.
                                                // The STAdjusted will not reset back to STOrig unless:
                                                // [1]. The MI goes up through the BTOrig (BTOrigCrossFlagSTC = 1) and then comes back across the BTOrig at some later date.
                                                //      - In order to allow for localized downward fluctuations in the MI, the MIValueWRTZero must concurrently be less than the BTOrig by the
                                                //      the STCAdjustmentTypeCriteria. Note that a larger value for STCAdjustmentTypeCriteria
                                                //      will allow for larger downward fluctuations in the MI.
                                                if (marketLowDayIntraSTCProcess == day && marketLowWRTZeroIntraSTCProcess < STCAdjustCriteria)
                                                {
                                                    // Set sell threshold to the current market low plus the sellThresholdAdjustmentMarketLowOffset
                                                    if (sellThreshold <= marketLowWRTZeroIntraSTCProcess + settings.STCMrktLowOffsetCriteria)
                                                    {
                                                        // Keep the current sellThreshold level.
                                                    }
                                                    else
                                                    {
                                                        double sellThresholdProposed = marketLowWRTZeroIntraSTCProcess + settings.STCMrktLowOffsetCriteria;
                                                        if (sellThresholdProposed < sellThreshold - settings.STCUpdateOffsetCriteria)
                                                        {
                                                            // Decrease sellThreshold only if sellThresholdProposed is less than the current sellThreshold by STCUpdateOffsetCriteria.
                                                            sellThreshold = sellThresholdProposed;
                                                            STCSellThresholdAdjustmentTracker = true; // Tracks whether a sellThreshold adjustment has been made.
                                                            STCProcessSellThresholdAdjustmentMarker = 1; // This gets set =1 if the Sell Threshold gets adjusted during this loop. It will get reset back to zero at the start of the next day.

                                                            movingAvgLowWRTZeroIntraSTCProcess = BTC_STCMovingAvgWRTZero; // Use for cross from BELOW checks
                                                            movingAvgLowIntraSTCProcess = BTC_STCMovingAvg; // Use for

                                                            movingAvgHighWRTZeroIntraSTCProcess = BTC_STCMovingAvgWRTZero; // Use for cross from ABOVE checks
                                                            movingAvgHighIntraSTCProcess = BTC_STCMovingAvg; // Use for intra STC process moving average downtrend checks

                                                            marketHighWRTZeroIntraSTCProcess = currentMainInvestmentValuationWRTZero; // Use for intra STC process market downtrend checks
                                                            BTCProcessMarketHigh = sharePriceMainInvestment;

                                                            // This is to prevent a buy during an STC process if BTCBuyThresholdAdjustmentTracker is not enabled (i.e., BTCBuyThresholdAdjustmentTracker=0)
                                                            // Without this set, an unwanted buy can occur during an STC process when the time period under investigation starts-out at a lower-negative stock valuation, prompting an STCProcess-STAdjust
                                                            if (settings.STCResetLastMIValuationWRTBuyThresholdFlag && BTCBuyThresholdAdjustmentTracker == 0)
                                                            {
                                                                hypotheticalMainInvestmentPriceAtBuyThreshold = currentMainInvestmentRegressionValue * (1.0 + buyThreshold / 100.0);
                                                                if (!settings.UseMIPriceInsteadOfMIValuationForTransactionControl)
                                                                {
                                                                    lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution = currentMainInvestmentValuationWRTZero - buyThreshold; // Will be negative if market is down
                                                                }
                                                                else if (settings.UseMIPriceInsteadOfMIValuationForTransactionControl)
                                                                {
                                                                    lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution = (sharePriceMainInvestment - hypotheticalMainInvestmentPriceAtBuyThreshold) / hypotheticalMainInvestmentPriceAtBuyThreshold * 100; // Will be negative if market is down
                                                                }

                                                                actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction = lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution;
                                                            }

                                                            if (sellFlag == 1)
                                                            {
                                                                // Temporarily reset criteriaDaysSinceLastSellTransactionAtSameLevel to a lesser time period.
                                                                // This will be reset back to the default value upon the next buyFlag=1.
                                                                criteriaDaysSinceLastSellTransactionAtSameLevel = settings.CriteriaDaysSinceLastSellTransactionAtSameLevelDuringSTAdjust;
                                                            }
                                                        }
                                                    }
                                                }

                                                if (settings.STCReduceSellExecutionPctCrit2DuringSTCProcessFlag && STCSellThresholdAdjustmentTracker)
                                                {
                                                    // This is only enabled once a sell threshold adjustment has been made via the STC process.
                                                    // This routine adjusts column 2 of the sellCriteria (currentTransactionSellExecutionPctCrit2) by reducing the sell execution levels
                                                    // when the market is below the sellThresholdOriginal, while also ensuring that the sell exec levels are of the same magnitude as the sellCriteriaOriginal above the sellThresholdOriginal.
                                                    // When the STC process adjusts the sellThreshold back to the sellThresholdOriginal, the sellCriteria is returned back to the sellCriteriaOriginal.

                                                    int n = 0; // Counter
                                                    hypotheticalMainInvestmentPriceAtSellThreshold = currentMainInvestmentRegressionValue * (1.0 + sellThreshold / 100.0);
                                                    double currentMIValuationWRTHypotheticalMIPriceAtSellThreshold = (sharePriceMainInvestment - hypotheticalMainInvestmentPriceAtSellThreshold) / hypotheticalMainInvestmentPriceAtSellThreshold * 100.0;

                                                    for (int p = 0; p < sellCriteriaOriginal.Count; p++) // Loop through rows of sellCriteriaOriginal
                                                    {
                                                        if (!settings.UseMIPriceInsteadOfMIValuationForTransactionControl && (sellCriteriaOriginal[p][0] + sellThreshold < sellCriteriaOriginal[0][0] + sellThresholdOriginal))
                                                        {
                                                            if (sellCriteriaOriginal[p][0] < settings.STCReduceSellExecutionPctCrit2DuringSTCProcessNewPct)
                                                            {
                                                                sellCriteria[p][1] = sellCriteriaOriginal[p][0];
                                                            }
                                                            else
                                                            {
                                                                sellCriteria[p][1] = settings.STCReduceSellExecutionPctCrit2DuringSTCProcessNewPct;
                                                            }

                                                            n++;
                                                        }
                                                        else if (settings.UseMIPriceInsteadOfMIValuationForTransactionControl && ((sellCriteriaOriginal[p][0] + (currentMainInvestmentValuationWRTZero - currentMIValuationWRTHypotheticalMIPriceAtSellThreshold)) < sellCriteriaOriginal[0][0] + sellThresholdOriginal))
                                                        {
                                                            if (sellCriteriaOriginal[p][0] < settings.STCReduceSellExecutionPctCrit2DuringSTCProcessNewPct)
                                                            {
                                                                sellCriteria[p][1] = sellCriteriaOriginal[p][0];
                                                            }
                                                            else
                                                            {
                                                                sellCriteria[p][1] = settings.STCReduceSellExecutionPctCrit2DuringSTCProcessNewPct;
                                                            }

                                                            n++;
                                                        }
                                                        else
                                                        {
                                                            // Interpolation formula: y = y1 + ((x – x1) / (x2 – x1)) * (y2 – y1)
                                                            double x;
                                                            if (!settings.UseMIPriceInsteadOfMIValuationForTransactionControl)
                                                            {
                                                                x = sellCriteriaOriginal[p][0] + sellThreshold;
                                                            }
                                                            else
                                                            {
                                                                x = sellCriteriaOriginal[p][0] + (currentMainInvestmentValuationWRTZero - currentMIValuationWRTHypotheticalMIPriceAtSellThreshold);
                                                            }

                                                            double x1 = sellCriteriaOriginal[p - n][0] + sellThresholdOriginal;
                                                            double x2 = sellCriteriaOriginal[p - n + 1][0] + sellThresholdOriginal;
                                                            double y1 = sellCriteriaOriginal[p - n][1];
                                                            double y2 = sellCriteriaOriginal[p - n + 1][1];

                                                            sellCriteria[p][1] = y1 + ((x - x1) / (x2 - x1)) * (y2 - y1);
                                                        }
                                                    }
                                                }

                                                // Sell Threshold Control Process and Reset Loop
                                                if (STCSellThresholdAdjustmentTracker)
                                                {
                                                    // Update the Intra-STC market high for use in calculating the LOCAL DOWNTREND for the local Buy-Enable process
                                                    if (currentMainInvestmentValuationWRTZero > marketHighWRTZeroIntraSTCProcess)
                                                    {
                                                        marketHighWRTZeroIntraSTCProcess = currentMainInvestmentValuationWRTZero; // Use for cross from BELOW checks
                                                        marketHighIntraSTCProcess = sharePriceMainInvestment;
                                                        STCLocalBuyEnablementProcessTracker = 0; // Reset STCLocalBuyEnablementProcessTracker parameter
                                                    }

                                                    double marketDecreaseFromIntraSTCProcessHigh = currentMainInvestmentValuationWRTZero - marketHighWRTZeroIntraSTCProcess;
                                                    // marketDecreaseFromIntraSTCProcessHighALT = ((sharePriceMainInvestment - marketHighIntraSTCProcess) / marketHighIntraSTCProcess) * 100.0;

                                                    // Update the Intra-STC market moving average high
                                                    if (BTC_STCMovingAvgWRTZero > movingAvgHighWRTZeroIntraSTCProcess)
                                                    {
                                                        movingAvgHighWRTZeroIntraSTCProcess = BTC_STCMovingAvgWRTZero;
                                                        movingAvgHighIntraSTCProcess = BTC_STCMovingAvg;
                                                    }

                                                    double movingAvgDecreaseFromIntraSTCProcessHigh = BTC_STCMovingAvgWRTZero - movingAvgHighWRTZeroIntraSTCProcess; // This will be negative (downtrend)
                                                    // movingAvgDecreaseFromIntraSTCProcessHighALT = ((BTC_STCMovingAvg - movingAvgHighIntraSTCProcess) / movingAvgHighIntraSTCProcess) * 100; // This will be negative (downtrend)

                                                    // Original Buy and Sell Threshold Crossover Checks

                                                    // Check for cross buyThresholdOriginal from BELOW. Sets BTOrigCrossFlagBTC = 1 which prevents entering the Adjust-sellThreshold-to-original-level routine
                                                    if (movingAvgLowWRTZeroIntraSTCProcess < buyThresholdOriginal && buyThresholdOriginal <= BTC_STCMovingAvgWRTZero)
                                                    {
                                                        BTOrigCrossFlagSTC = 1;
                                                    }

                                                    // Check for cross sellThresholdOriginal from BELOW. Sets STOrigCrossFlagBTC = -1 which prevents entering the Adjust-sellThreshold-to-original-level routine
                                                    if (movingAvgLowWRTZeroIntraSTCProcess < sellThresholdOriginal && sellThresholdOriginal <= BTC_STCMovingAvgWRTZero)
                                                    {
                                                        STOrigCrossFlagSTC = -1; // This is considered the disabled condition
                                                    }

                                                    // Check for cross sellThresholdOriginal from ABOVE
                                                    // This occurs when the market passes the BTO and STO and comes back down from above.
                                                    // Sets STOrigCrossFlagBTC = 1 which allows entering the Adjust-sellThreshold-to-original-level
                                                    if (movingAvgHighWRTZeroIntraSTCProcess > sellThresholdOriginal && sellThresholdOriginal >= BTC_STCMovingAvgWRTZero)
                                                    {
                                                        STOrigCrossFlagSTC = 1; // This stays = 1 until it is changed
                                                    }

                                                    // Check for cross buyThresholdOriginal from ABOVE
                                                    // This occurs when the market crosses the BTO and STO from below and then comes back across from above.
                                                    // Sets BTOrigCrossFlagSTC = -1 which allows the STA to be shut down when a BTA is enabled
                                                    if (movingAvgHighWRTZeroIntraSTCProcess > buyThresholdOriginal && buyThresholdOriginal >= BTC_STCMovingAvgWRTZero && STOrigCrossFlagSTC == 1)
                                                    {
                                                        BTOrigCrossFlagSTC = -1; // This stays = -1 until it is changed
                                                    }

                                                    // Reset the sellThreshold to its Original level
                                                    // If the criteria below is met:
                                                    // - Reset the sellThreshold to its original level
                                                    // - Reset buy criteria
                                                    // - Reset local correction refinement flags
                                                    // - Reset STracker to OFF
                                                    // - Reset Threshold cross flags
                                                    bool RSTO1 = BTOrigCrossFlagSTC == 1;
                                                    bool RSTO2 = STOrigCrossFlagSTC != -1;
                                                    bool RSTO3 = STOrigCrossFlagSTC == 1;
                                                    bool RSTO4 = movingAvgDecreaseFromIntraSTCProcessHigh < settings.STCSellThresholdCrossBufferCriteria; // Downtrend must be more negative than criteria!
                                                    bool RSTO4a = movingAvgDecreaseFromIntraSTCProcessHigh < (settings.STCSellThresholdCrossBufferCriteria - (sellThresholdOriginal - buyThresholdOriginal)); // Downtrend must be more negative than criteria!
                                                    bool RSTO5 = day > dayOfLastSell; // Can't have a sell order on the same day that resets the STO

                                                    if (((RSTO1 && RSTO2) && RSTO4a) || (RSTO3 && RSTO4) && RSTO5)
                                                    {
                                                        sellThreshold = sellThresholdOriginal; // Reset
                                                        sellCriteria = sellCriteriaOriginal; // Reset

                                                        // Since the ST is being reset to STOrig, reset the Buy criteria, so that buy orders will not have knowledge of the past and can initiate from scratch. 
                                                        // Same effect as with sellOrderTriggerAdjustFlag == 1.
                                                        // FUNCTION CALL: Reset Buy Criteria T2 -When buy criteria is reset due to execution of a sell order or a sell threshold reset
                                                        BackTestUtilities.ResetBuyCriteria(ref buyCriteria, ref lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, criteriaDaysSinceLastBuyTransactionAtSameLevel, day, date, dateNum, 2, out lastTransactionBuyLevelCrit1, out actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction, out buyResetType1Flag, out buyResetType2Flag);

                                                        // FUNCTION CALL: Reset Sell Criteria T1
                                                        BackTestUtilities.ResetSellCriteria(ref sellCriteria, ref lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, criteriaDaysSinceLastSellTransactionAtSameLevel, day, date, dateNum, 1, out lastTransactionSellLevelCrit1, out actualMainInvestmentValuationWRTSellThresholdAtLastTransaction, out sellResetType1Flag, out sellResetType2Flag);

                                                        STCSellThresholdAdjustmentTracker = false; // Tracks whether a sellThreshold adjustment lower has been reset to STOrig.
                                                        marketLowIntraSTCProcess = sharePriceMainInvestment; // Reset to market high to a lower value so that the next BTA process can be triggered
                                                        marketLowWRTZeroIntraSTCProcess = currentMainInvestmentValuationWRTZero; // Reset to market low to a higher value so that the next STA process can be triggered
                                                        STOrigCrossFlagSTC = 0;
                                                        BTOrigCrossFlagSTC = 0;
                                                        STCLocalBuyEnablementProcessTracker = 0;
                                                    }

                                                    // Local Intra-STC Downtrend Buy Enable - Allow Buys during STA process prior to BTOrig cross
                                                    // This routine checks whether a local intra-STC market downtrend is in progress, and that the BTOrig line has not been crossed.
                                                    // This sets STCLocalBuyEnablementProcessTracker = 1. This flag will be set back to zero if the currentMainInvestmentValuationWRTZero > marketHighWRTZeroIntraSTCProcess.
                                                    bool RSTO7 = marketDecreaseFromIntraSTCProcessHigh <= settings.STCDecreaseFromIntraSTCProcessMarketHighCriteria;
                                                    bool RSTO8 = BTOrigCrossFlagSTC == 0; // MI did not cross the BTOrig line yet.
                                                    bool RSTO9 = STCLocalBuyEnablementProcessTracker != 1; // Local Downtrend not in process
                                                    bool RSTO10 = currentMainInvestmentValuationWRTZero > (sellThreshold + 5.0);
                                                    bool RSTO11 = movingAvgDecreaseFromIntraSTCProcessHigh <= (settings.MovingAverageKnockDownFactor * settings.STCDecreaseFromIntraSTCProcessMarketHighCriteria);

                                                    if (STCSellThresholdAdjustmentTracker && RSTO7 && RSTO8 && RSTO9 && RSTO10 && RSTO11)
                                                    {
                                                        STCLocalBuyEnablementProcessTracker = 1;

                                                        // Reset Buy Criteria T1
                                                        BackTestUtilities.ResetBuyCriteria(ref buyCriteria, ref lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, criteriaDaysSinceLastBuyTransactionAtSameLevel, day, date, dateNum, 1, out lastTransactionBuyLevelCrit1, out actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction, out buyResetType1Flag, out buyResetType2Flag);
                                                    }

                                                    // Reset the Buy criteria if, while STATracker is ON, cross STOrig & BTOrig from above
                                                    // FYI - this means that it already crossed BTOrig & STOrig from below.
                                                    if (STCSellThresholdAdjustmentTracker && BTOrigCrossFlagSTC == -1 && STOrigCrossFlagSTC == 1 && movingAvgDecreaseFromIntraSTCProcessHigh < settings.STCSellThresholdCrossBufferCriteria)
                                                    {
                                                        // FUNCTION CALL: Reset Buy Criteria T1
                                                        BackTestUtilities.ResetBuyCriteria(ref buyCriteria, ref lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, criteriaDaysSinceLastBuyTransactionAtSameLevel, day, date, dateNum, 1, out lastTransactionBuyLevelCrit1, out actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction, out buyResetType1Flag, out buyResetType2Flag);
                                                    }

                                                    // Ensure STCVarCount and STCVariablesArray are defined in the correct scope
                                                    int STCVarCount = 0; // Initialize row counter for the STC Variables array
                                                    object[,] STCVariablesArray = new object[1000, 42]; // Adjust size based on expected data

                                                    // STC Variables Array
                                                    if (settings.WriteToSTCVariablesXLSFileFlag)
                                                    {
                                                        STCVarCount++;

                                                        STCVariablesArray[STCVarCount, 1] = day;
                                                        STCVariablesArray[STCVarCount, 2] = date[day].ToString(); // Convert date to string
                                                        STCVariablesArray[STCVarCount, 3] = dateNum;
                                                        STCVariablesArray[STCVarCount, 4] = currentMainInvestmentValuationWRTZero;
                                                        STCVariablesArray[STCVarCount, 5] = marketLowWRTZeroIntraSTCProcess;
                                                        STCVariablesArray[STCVarCount, 6] = marketLowDayIntraSTCProcess;
                                                        STCVariablesArray[STCVarCount, 7] = marketHighWRTZeroIntraSTCProcess;
                                                        STCVariablesArray[STCVarCount, 8] = marketDecreaseFromIntraSTCProcessHigh;
                                                        STCVariablesArray[STCVarCount, 9] = BTC_STCMovingAvgWRTZero;
                                                        STCVariablesArray[STCVarCount, 10] = movingAvgLowWRTZeroIntraSTCProcess;
                                                        STCVariablesArray[STCVarCount, 11] = movingAvgHighWRTZeroIntraSTCProcess;
                                                        STCVariablesArray[STCVarCount, 12] = movingAvgDecreaseFromIntraSTCProcessHigh;
                                                        STCVariablesArray[STCVarCount, 13] = sellResetType1Flag;
                                                        STCVariablesArray[STCVarCount, 14] = sellResetType2Flag;
                                                        STCVariablesArray[STCVarCount, 15] = buyResetType1Flag;
                                                        STCVariablesArray[STCVarCount, 16] = buyResetType2Flag;
                                                        STCVariablesArray[STCVarCount, 17] = STCSellThresholdAdjustmentTracker;
                                                        STCVariablesArray[STCVarCount, 18] = sellThresholdOriginal;
                                                        STCVariablesArray[STCVarCount, 19] = sellThreshold;
                                                        STCVariablesArray[STCVarCount, 20] = buyThresholdOriginal;
                                                        STCVariablesArray[STCVarCount, 21] = buyThreshold;
                                                        STCVariablesArray[STCVarCount, 22] = BTOrigCrossFlagSTC;
                                                        STCVariablesArray[STCVarCount, 23] = STOrigCrossFlagSTC;
                                                        STCVariablesArray[STCVarCount, 24] = STCLocalBuyEnablementProcessTracker;
                                                        STCVariablesArray[STCVarCount, 25] = RSTO1.ToString(); // Convert boolean to string
                                                        STCVariablesArray[STCVarCount, 26] = RSTO2.ToString();
                                                        STCVariablesArray[STCVarCount, 27] = RSTO3.ToString();
                                                        STCVariablesArray[STCVarCount, 28] = RSTO4.ToString();
                                                        STCVariablesArray[STCVarCount, 29] = RSTO5.ToString();
                                                        STCVariablesArray[STCVarCount, 30] = RSTO7.ToString();
                                                        STCVariablesArray[STCVarCount, 31] = RSTO8.ToString();
                                                        STCVariablesArray[STCVarCount, 32] = RSTO9.ToString();
                                                        STCVariablesArray[STCVarCount, 33] = RSTO10.ToString();
                                                        STCVariablesArray[STCVarCount, 34] = RSTO11.ToString();
                                                        STCVariablesArray[STCVarCount, 35] = lastTransactionSellLevelCrit1;
                                                        STCVariablesArray[STCVarCount, 36] = lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn;
                                                        STCVariablesArray[STCVarCount, 37] = actualMainInvestmentValuationWRTSellThresholdAtLastTransaction;
                                                        STCVariablesArray[STCVarCount, 38] = marketLowIntraSTCProcess;
                                                        STCVariablesArray[STCVarCount, 39] = marketHighIntraSTCProcess;
                                                        STCVariablesArray[STCVarCount, 40] = movingAvgLowIntraSTCProcess;
                                                        STCVariablesArray[STCVarCount, 41] = movingAvgHighIntraSTCProcess;
                                                    }
                                                }
                                            }

                                            //======================================================================
                                            // Run Buy Threshold Control (BTC) Algorithm; Buy Threshold Adjustment
                                            //======================================================================
                                            if (settings.BuyThresholdControlFlag)
                                            {
                                                // Initial Settings
                                                // Update intra BTC process market high (relative to current BTC process).
                                                if (currentMainInvestmentValuationWRTZero > BTCProcessMarketHighWRTZero)
                                                {
                                                    BTCProcessMarketHighWRTZero = currentMainInvestmentValuationWRTZero; // Criteria used for enabling buyThreshold adjustment
                                                    BTCProcessMarketHigh = sharePriceMainInvestment;
                                                    BTCProcessMarketHighDay = day;
                                                }

                                                // Buy Threshold Adjustment
                                                // Adjust the Buy Threshold to a higher level if criteria are met.
                                                if (BTCProcessMarketHighDay == day && BTCProcessMarketHighWRTZero > BTCAdjustCriteria)
                                                {
                                                    // Set buy threshold to the current market high minus the BTCMrktHighOffsetCriteria.
                                                    if (buyThreshold >= BTCProcessMarketHighWRTZero - settings.BTCMrktHighOffsetCriteria)
                                                    {
                                                        // Keep the current buyThreshold level.
                                                    }
                                                    else
                                                    {
                                                        double buyThresholdProposed = BTCProcessMarketHighWRTZero - settings.BTCMrktHighOffsetCriteria;

                                                        if (buyThresholdProposed > buyThreshold + settings.BTCUpdateOffsetCriteria) // Increase buyThreshold only if buyThresholdProposed exceeds the current buyThreshold by BTCUpdateOffsetCriteria.
                                                        {
                                                            buyThreshold = buyThresholdProposed;
                                                            BTCBuyThresholdAdjustmentTracker = 1; // Tracks whether a buyThreshold adjustment has been made.
                                                            BTCProcessBuyThresholdAdjustmentMarker = 1; // This gets set =1 if the Buy Threshold gets adjusted during this loop. It will get reset back to zero at the start of the next day.

                                                            BTCProcessMovingAvgHighWRTZero = BTC_STCMovingAvgWRTZero; // Use for cross from ABOVE checks.
                                                            BTCProcessMovingAvgHigh = BTC_STCMovingAvg; // Use for intra BTC process.

                                                            BTCProcessMovingAvgLowWRTZero = BTC_STCMovingAvgWRTZero; // Use for cross from BELOW checks.
                                                            BTCProcessMovingAvgLow = BTC_STCMovingAvg; // Use for intra BTC process moving average uptrend checks.

                                                            BTCProcessMarketLowWRTZero = currentMainInvestmentValuationWRTZero; // Use for intra BTC process market downtrend checks.
                                                            BTCProcessMarketLow = sharePriceMainInvestment;

                                                            BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZero = BTCProcessMarketLowWRTZero;
                                                            BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZeroDay = day;

                                                            if (settings.BTCResetLastMIValuationWRTSellThresholdFlag && !STCSellThresholdAdjustmentTracker) // This is to prevent a sell during a BTC process.
                                                            {
                                                                hypotheticalMainInvestmentPriceAtSellThreshold = currentMainInvestmentRegressionValue * (1.0 + sellThreshold / 100.0);

                                                                if (!settings.UseMIPriceInsteadOfMIValuationForTransactionControl)
                                                                {
                                                                    lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn = currentMainInvestmentValuationWRTZero - sellThreshold;
                                                                }
                                                                else if (settings.UseMIPriceInsteadOfMIValuationForTransactionControl)
                                                                {
                                                                    lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn = (sharePriceMainInvestment - hypotheticalMainInvestmentPriceAtSellThreshold) / hypotheticalMainInvestmentPriceAtSellThreshold * 100.0;
                                                                }

                                                                actualMainInvestmentValuationWRTSellThresholdAtLastTransaction = lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn;
                                                            }

                                                            // Reset flags related to Buy/Sell Threshold cross events.
                                                            STOrigCrossFlagBTCeq1 = 0;
                                                            BTOrigCrossFlagBTCeqM1 = 0;
                                                            BTOrigCrossFlagBTCeq1 = 0;
                                                            STOrigCrossFlagBTCeqM1 = 0;

                                                            // Temporarily reset criteriaDaysSinceLastBuyTransactionAtSameLevel to lesser time period.
                                                            // This will be reset back to the default value upon the next sellFlag = 1.
                                                            if (buyFlag == 1)
                                                            {
                                                                criteriaDaysSinceLastBuyTransactionAtSameLevel = settings.CriteriaDaysSinceLastBuyTransactionAtSameLevelDuringBTAdjust;
                                                            }

                                                            if (settings.VerboseBTAFlag)
                                                            {
                                                                statusUpdater.UpdateStatus($"EVENT: BTAdjust + BTATrackON + Reset[MAHigh/Low, MrktLow] + RBC_T1: date={date[day]}, dateNum={dateNum}");
                                                                ;
                                                            }
                                                        }
                                                    }
                                                }

                                                if (BTCBuyThresholdAdjustmentTracker == 1)
                                                {
                                                    // Update the Intra-BTC market low for use in calculating the LOCAL UPTREND for the local Sell-Enable process
                                                    if (currentMainInvestmentValuationWRTZero < BTCProcessMarketLowWRTZero)
                                                    {
                                                        BTCProcessMarketLowWRTZero = currentMainInvestmentValuationWRTZero;
                                                        BTCProcessMarketLow = sharePriceMainInvestment;

                                                        BTCLocalSellEnablementProcessTracker = 0; // Reset BTCLocalSellEnablementProcessTracker parameter

                                                        BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZero = currentMainInvestmentValuationWRTZero;
                                                        BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZeroDay = day;

                                                        BTCLocalSellEnablementProcessMarketHighWRTZero = currentMainInvestmentValuationWRTZero;
                                                        BTCLocalSellEnablementProcessMarketHighWRTZeroDay = day;

                                                        BTCPreemptiveTerminateLocalSellEnablePrcsToggle = 0;
                                                    }

                                                    // BTC Preemptive Terminate LocalSellEnableProcess Algorithm
                                                    if (currentMainInvestmentValuationWRTZero < BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZero && (BTCPreemptiveTerminateLocalSellEnablePrcsToggle == 1 || BTCLocalSellEnablementProcessTracker == 1))
                                                    {
                                                        BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZero = currentMainInvestmentValuationWRTZero;
                                                        BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZeroDay = day;
                                                    }

                                                    if (currentMainInvestmentValuationWRTZero > BTCLocalSellEnablementProcessMarketHighWRTZero && (BTCPreemptiveTerminateLocalSellEnablePrcsToggle == 1 || BTCLocalSellEnablementProcessTracker == 1))
                                                    {
                                                        BTCLocalSellEnablementProcessMarketHighWRTZero = currentMainInvestmentValuationWRTZero;
                                                        BTCLocalSellEnablementProcessMarketHighWRTZeroDay = day;
                                                    }

                                                    if (BTCLocalSellEnablementProcessMarketHighWRTZero > BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZero && BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZeroDay >= BTCLocalSellEnablementProcessMarketHighWRTZeroDay && (BTCPreemptiveTerminateLocalSellEnablePrcsToggle == 1 || BTCLocalSellEnablementProcessTracker == 1))
                                                    {
                                                        BTCLocalSellEnablementProcessMarketHighWRTZero = currentMainInvestmentValuationWRTZero;
                                                        BTCLocalSellEnablementProcessMarketHighWRTZeroDay = day;
                                                    }

                                                    BTCProcessMarketIncreaseWRTBTCPreemptiveTerminateLow = BTCLocalSellEnablementProcessMarketHighWRTZero - BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZero;

                                                    // Terminate BTCLocalSellEnablementProcessTracker under specific conditions
                                                    if (settings.BTCPreemptiveTerminateLocalSellEnablePrcsFlag && BTCPreemptiveTerminateLocalSellEnablePrcsToggle == 0 && BTCLocalSellEnablementProcessTracker == 1 && BTCProcessMarketIncreaseWRTBTCPreemptiveTerminateLow >= settings.BTCPreemptiveTerminateLocalSellEnablePrcsLimitPct && BTCLocalSellEnablementProcessMarketHighWRTZero - currentMainInvestmentValuationWRTZero >= 5.0 && movingAverage5RateOfChangeWRTZero <= -0.05 && day - BTCLocalSellEnablementProcessMarketHighWRTZeroDay > 3.0)
                                                    {
                                                        BTCLocalSellEnablementProcessTracker = 0; // Reset BTCLocalSellEnablementProcessTracker parameter
                                                        BTCLocalSellEnablementProcessMarketHighWRTZero = currentMainInvestmentValuationWRTZero;
                                                        BTCLocalSellEnablementProcessMarketHighWRTZeroDay = day;
                                                        BTCPreemptiveTerminateLocalSellEnablePrcsToggle = 1;
                                                        BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZero = currentMainInvestmentValuationWRTZero; // Initialize Low valuation upon setting the Toggle to 1
                                                        BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZeroDay = day;

                                                        // FUNCTION CALL: Reset Buy Criteria T1
                                                        BackTestUtilities.ResetBuyCriteria(ref buyCriteria, ref lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, criteriaDaysSinceLastBuyTransactionAtSameLevel, day, date, dateNum, 1, out lastTransactionBuyLevelCrit1, out actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction, out buyResetType1Flag, out buyResetType2Flag);
                                                    }

                                                    // Original Buy and Sell Threshold Crossover Checks
                                                    BTCProcessMarketIncreaseFromIntraBTCLow = currentMainInvestmentValuationWRTZero - BTCProcessMarketLowWRTZero;
                                                    BTCProcessMarketIncreaseFromIntraBTCLowALT = ((sharePriceMainInvestment - BTCProcessMarketLow) / BTCProcessMarketLow) * 100.0;

                                                    // Update the Intra-BTC market moving average low
                                                    if (BTC_STCMovingAvgWRTZero < BTCProcessMovingAvgLowWRTZero)
                                                    {
                                                        BTCProcessMovingAvgLowWRTZero = BTC_STCMovingAvgWRTZero;
                                                        BTCProcessMovingAvgLow = BTC_STCMovingAvg;
                                                    }

                                                    BTCProcessMovingAvgIncreaseFromIntraBTCLow = BTC_STCMovingAvgWRTZero - BTCProcessMovingAvgLowWRTZero;
                                                    BTCProcessMovingAvgIncreaseFromIntraBTCLowALT = ((BTC_STCMovingAvg - BTCProcessMovingAvgLow) / BTCProcessMovingAvgLow) * 100.0;

                                                    // Check for cross sellThresholdOriginal from ABOVE
                                                    if (BTCProcessMovingAvgHighWRTZero > sellThresholdOriginal && sellThresholdOriginal >= BTC_STCMovingAvgWRTZero)
                                                    {
                                                        STOrigCrossFlagBTC = 1;
                                                        STOrigCrossFlagBTCeq1++;

                                                        if (settings.VerboseBTAFlag && STOrigCrossFlagBTCeq1 == 1)
                                                        {
                                                            statusUpdater.UpdateStatus($"EVENT: STOrigCrossFromABOVE; date={date[day]}, dateNum={dateNum}");
                                                        }
                                                    }

                                                    // Check for cross buyThresholdOriginal from ABOVE
                                                    if (BTCProcessMovingAvgHighWRTZero > buyThresholdOriginal && buyThresholdOriginal >= BTC_STCMovingAvgWRTZero)
                                                    {
                                                        BTOrigCrossFlagBTC = -1;
                                                        BTOrigCrossFlagBTCeqM1++;

                                                        if (settings.VerboseBTAFlag && BTOrigCrossFlagBTCeqM1 == 1)
                                                        {
                                                            statusUpdater.UpdateStatus("EVENT: BTOrigCrossFromABOVE; date={date[day]}, dateNum={dateNum}");
                                                        }
                                                    }

                                                    // Check for cross buyThresholdOriginal from BELOW
                                                    if (BTCProcessMovingAvgLowWRTZero < buyThresholdOriginal && buyThresholdOriginal <= BTC_STCMovingAvgWRTZero)
                                                    {
                                                        BTOrigCrossFlagBTC = 1;
                                                        BTOrigCrossFlagBTCeq1++;

                                                        if (settings.VerboseBTAFlag && BTOrigCrossFlagBTCeq1 == 1)
                                                        {
                                                            statusUpdater.UpdateStatus("EVENT: BTOrigCrossFromBELOW; date={date[day]}, dateNum={dateNum}");
                                                        }
                                                    }

                                                    // Check for cross sellThresholdOriginal from BELOW
                                                    if (BTCProcessMovingAvgLowWRTZero < sellThresholdOriginal && sellThresholdOriginal <= BTC_STCMovingAvgWRTZero && BTOrigCrossFlagBTC == 1)
                                                    {
                                                        STOrigCrossFlagBTC = -1;
                                                        STOrigCrossFlagBTCeqM1++;

                                                        if (settings.VerboseBTAFlag && STOrigCrossFlagBTCeqM1 == 1)
                                                        {
                                                            statusUpdater.UpdateStatus($"EVENT: STOrigCrossFromBELOW; date={date[day]}, dateNum={dateNum}");
                                                        }
                                                    }

                                                    // Reset Buy Threshold to Original Level
                                                    bool RBTO1 = STOrigCrossFlagBTC == 1; // Cross from above
                                                    bool RBTO2 = BTOrigCrossFlagBTC != -1; // Did not cross or crossed from below
                                                    bool RBTO3 = BTOrigCrossFlagBTC == 1; // Cross from below
                                                    bool RBTO4 = BTCProcessMovingAvgIncreaseFromIntraBTCLow > settings.BTCBuyThresholdCrossBufferCriteria;
                                                    bool RBTO4a = BTCProcessMovingAvgIncreaseFromIntraBTCLow > settings.BTCBuyThresholdCrossBufferCriteria + (sellThresholdOriginal - buyThresholdOriginal);
                                                    bool RBTO5 = day > dayOfLastBuy; // Cannot have a buy order on the same day that the BTO is reset

                                                    if (((RBTO1 && RBTO2 && RBTO4a) || (RBTO3 && RBTO4)) && RBTO5)
                                                    {
                                                        buyThreshold = buyThresholdOriginal;

                                                        // FUNCTION CALL: Reset Sell Criteria T2
                                                        BackTestUtilities.ResetSellCriteria(ref sellCriteria, ref lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, criteriaDaysSinceLastSellTransactionAtSameLevel, day, date, dateNum, 2, out lastTransactionSellLevelCrit1, out actualMainInvestmentValuationWRTSellThresholdAtLastTransaction, out sellResetType1Flag, out sellResetType2Flag);

                                                        // FUNCTION CALL: Reset Buy Criteria T1
                                                        BackTestUtilities.ResetBuyCriteria(ref buyCriteria, ref lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, criteriaDaysSinceLastBuyTransactionAtSameLevel, day, date, dateNum, 1, out lastTransactionBuyLevelCrit1, out actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction, out buyResetType1Flag, out buyResetType2Flag);

                                                        BTCBuyThresholdAdjustmentTracker = 0; // Reset tracker
                                                        BTCProcessMarketHigh = sharePriceMainInvestment; // Reset to market high for the next process
                                                        BTCProcessMarketHighWRTZero = currentMainInvestmentValuationWRTZero; // Reset to market high for the next process
                                                        BTOrigCrossFlagBTC = 0;
                                                        STOrigCrossFlagBTC = 0;
                                                        BTCLocalSellEnablementProcessTracker = 0;
                                                        BTCPreemptiveTerminateLocalSellEnablePrcsToggle = 0;

                                                        if (settings.VerboseBTAFlag)
                                                        {
                                                            statusUpdater.UpdateStatus($"EVENT: ResetBTOrig + RBC_T1 + RSC_T1 + BTATrackOFF + Reset[MrktHigh] + LSE_OFF; date={date[day]}, dateNum={dateNum}, BTCProcessMovingAvgIncreaseFromIntraBTCLow: {BTCProcessMovingAvgIncreaseFromIntraBTCLow:0.2f}(Crt={BTCProcessMovingAvgIncreaseFromIntraBTCLow:0.2f}/{settings.BTCBuyThresholdCrossBufferCriteria + (sellThresholdOriginal - buyThresholdOriginal):0.2f}), BTCProcessMarketIncreaseFromIntraBTCLow: {BTCProcessMarketIncreaseFromIntraBTCLow:0.2f}");
                                                        }
                                                    }

                                                    // Local Intra-BTC Uptrend Sell Enable
                                                    bool RBTO7 = BTCProcessMarketIncreaseFromIntraBTCLow >= settings.BTCIncreaseFromIntraBTCProcessMarketLowCriteria && BTCPreemptiveTerminateLocalSellEnablePrcsToggle != 1;
                                                    bool RBTO7a = BTCProcessMarketIncreaseWRTBTCPreemptiveTerminateLow >= settings.BTCIncreaseFromIntraBTCProcessMarketLowCriteria && BTCPreemptiveTerminateLocalSellEnablePrcsToggle == 1 && BTCLocalSellEnablementProcessMarketHighWRTZeroDay > BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZeroDay;
                                                    bool RBTO8 = STOrigCrossFlagBTC == 0; // Did not cross
                                                    bool RBTO9 = BTCLocalSellEnablementProcessTracker != 1;
                                                    bool RBTO10 = currentMainInvestmentValuationWRTZero < buyThreshold - 5.0;
                                                    bool RBTO11 = BTCProcessMovingAvgIncreaseFromIntraBTCLow >= settings.MovingAverageKnockDownFactor * settings.BTCIncreaseFromIntraBTCProcessMarketLowCriteria;

                                                    if (BTCBuyThresholdAdjustmentTracker == 1 && (RBTO7 || RBTO7a) && RBTO8 && RBTO9 && RBTO10 && RBTO11)
                                                    {
                                                        BTCLocalSellEnablementProcessTracker = 1;
                                                        BTCPreemptiveTerminateLocalSellEnablePrcsToggle = 0;
                                                        BTCLocalSellEnablementProcessMarketHighWRTZero = currentMainInvestmentValuationWRTZero;
                                                        BTCLocalSellEnablementProcessMarketHighWRTZeroDay = day;

                                                        // FUNCTION CALL: Reset Sell Criteria T1
                                                        BackTestUtilities.ResetSellCriteria(ref sellCriteria, ref lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, criteriaDaysSinceLastSellTransactionAtSameLevel, day, date, dateNum, 1, out lastTransactionSellLevelCrit1, out actualMainInvestmentValuationWRTSellThresholdAtLastTransaction, out sellResetType1Flag, out sellResetType2Flag);
                                                    }

                                                    // Reset Sell Criteria for Threshold Crossovers
                                                    if (BTCBuyThresholdAdjustmentTracker == 1 && BTOrigCrossFlagBTC == 1 && STOrigCrossFlagBTC == -1 && BTCProcessMovingAvgIncreaseFromIntraBTCLow > settings.BTCBuyThresholdCrossBufferCriteria)
                                                    {
                                                        // FUNCTION CALL: Reset Sell Criteria T1
                                                        BackTestUtilities.ResetSellCriteria(ref sellCriteria, ref lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, criteriaDaysSinceLastSellTransactionAtSameLevel, day, date, dateNum, 1, out lastTransactionSellLevelCrit1, out actualMainInvestmentValuationWRTSellThresholdAtLastTransaction, out sellResetType1Flag, out sellResetType2Flag);

                                                        if (settings.VerboseBTAFlag)
                                                        {
                                                            statusUpdater.UpdateStatus($"EVENT: STOrig&BTOrigCrossAbove, ResetSellCrit; date={date[day]}, dateNum={dateNum}, BTCProcessMovingAvgIncreaseFromIntraBTCLow: {BTCProcessMovingAvgIncreaseFromIntraBTCLow:0.2f} (Crt={settings.BTCBuyThresholdCrossBufferCriteria:0.2f}), BTCProcessMarketIncreaseFromIntraBTCLow: {BTCProcessMarketIncreaseFromIntraBTCLow:0.2f}");
                                                        }
                                                    }

                                                    // Initialize BTCVariablesArray as a List of object arrays.
                                                    var BTCVariablesArray = new List<object[]>();

                                                    // Initialize BTCVarCount to track rows dynamically.
                                                    int BTCVarCount = 0;

                                                    if (settings.WriteToBTCVariablesXLSFileFlag)
                                                    {
                                                        BTCVarCount++;

                                                        // Add a new row to BTCVariablesArray
                                                        BTCVariablesArray.Add(new object[54]);

                                                        BTCVariablesArray[BTCVarCount - 1][0] = day;
                                                        BTCVariablesArray[BTCVarCount - 1][1] = date[day]; // Assuming date is a List<string>
                                                        BTCVariablesArray[BTCVarCount - 1][2] = dateNum;
                                                        BTCVariablesArray[BTCVarCount - 1][3] = regressionPriceMainInvestment;
                                                        BTCVariablesArray[BTCVarCount - 1][4] = sharePriceMainInvestment;
                                                        BTCVariablesArray[BTCVarCount - 1][5] = currentMainInvestmentValuationWRTZero;
                                                        BTCVariablesArray[BTCVarCount - 1][6] = marketHigh;
                                                        BTCVariablesArray[BTCVarCount - 1][7] = marketHighWRTZero;
                                                        BTCVariablesArray[BTCVarCount - 1][8] = marketHighDay;
                                                        BTCVariablesArray[BTCVarCount - 1][9] = marketCorrectionFromHigh;
                                                        BTCVariablesArray[BTCVarCount - 1][10] = marketLow;
                                                        BTCVariablesArray[BTCVarCount - 1][11] = marketLowWRTZero;
                                                        BTCVariablesArray[BTCVarCount - 1][12] = marketLowDay;
                                                        BTCVariablesArray[BTCVarCount - 1][13] = marketCorrectionFromLow;
                                                        BTCVariablesArray[BTCVarCount - 1][14] = BTCProcessMarketHigh;
                                                        BTCVariablesArray[BTCVarCount - 1][15] = BTCProcessMarketHighWRTZero;
                                                        BTCVariablesArray[BTCVarCount - 1][16] = BTCProcessMarketHighDay;
                                                        BTCVariablesArray[BTCVarCount - 1][17] = BTCProcessMarketLow;
                                                        BTCVariablesArray[BTCVarCount - 1][18] = BTCProcessMarketLowWRTZero;
                                                        BTCVariablesArray[BTCVarCount - 1][19] = BTCProcessMarketIncreaseFromIntraBTCLowALT;
                                                        BTCVariablesArray[BTCVarCount - 1][20] = BTCProcessMarketIncreaseFromIntraBTCLow;
                                                        BTCVariablesArray[BTCVarCount - 1][21] = BTC_STCMovingAvg;
                                                        BTCVariablesArray[BTCVarCount - 1][22] = BTC_STCMovingAvgWRTZero;
                                                        BTCVariablesArray[BTCVarCount - 1][23] = BTCProcessMovingAvgHigh;
                                                        BTCVariablesArray[BTCVarCount - 1][24] = BTCProcessMovingAvgHighWRTZero;
                                                        BTCVariablesArray[BTCVarCount - 1][25] = BTCProcessMovingAvgLow;
                                                        BTCVariablesArray[BTCVarCount - 1][26] = BTCProcessMovingAvgLowWRTZero;
                                                        BTCVariablesArray[BTCVarCount - 1][27] = BTCProcessMovingAvgIncreaseFromIntraBTCLowALT;
                                                        BTCVariablesArray[BTCVarCount - 1][28] = BTCProcessMovingAvgIncreaseFromIntraBTCLow;
                                                        BTCVariablesArray[BTCVarCount - 1][29] = sellResetType1Flag;
                                                        BTCVariablesArray[BTCVarCount - 1][30] = sellResetType2Flag;
                                                        BTCVariablesArray[BTCVarCount - 1][31] = buyResetType1Flag;
                                                        BTCVariablesArray[BTCVarCount - 1][32] = buyResetType2Flag;
                                                        BTCVariablesArray[BTCVarCount - 1][33] = BTCBuyThresholdAdjustmentTracker;
                                                        BTCVariablesArray[BTCVarCount - 1][34] = sellThresholdOriginal;
                                                        BTCVariablesArray[BTCVarCount - 1][35] = sellThreshold;
                                                        BTCVariablesArray[BTCVarCount - 1][36] = buyThresholdOriginal;
                                                        BTCVariablesArray[BTCVarCount - 1][37] = buyThreshold;
                                                        BTCVariablesArray[BTCVarCount - 1][38] = BTOrigCrossFlagBTC;
                                                        BTCVariablesArray[BTCVarCount - 1][39] = STOrigCrossFlagBTC;
                                                        BTCVariablesArray[BTCVarCount - 1][40] = BTCLocalSellEnablementProcessTracker;
                                                        BTCVariablesArray[BTCVarCount - 1][41] = RBTO1.ToString();
                                                        BTCVariablesArray[BTCVarCount - 1][42] = RBTO2.ToString();
                                                        BTCVariablesArray[BTCVarCount - 1][43] = RBTO3.ToString();
                                                        BTCVariablesArray[BTCVarCount - 1][44] = RBTO4.ToString();
                                                        BTCVariablesArray[BTCVarCount - 1][45] = RBTO5.ToString();
                                                        BTCVariablesArray[BTCVarCount - 1][46] = RBTO7.ToString();
                                                        BTCVariablesArray[BTCVarCount - 1][47] = RBTO8.ToString();
                                                        BTCVariablesArray[BTCVarCount - 1][48] = RBTO9.ToString();
                                                        BTCVariablesArray[BTCVarCount - 1][49] = RBTO10.ToString();
                                                        BTCVariablesArray[BTCVarCount - 1][50] = RBTO11.ToString();
                                                        BTCVariablesArray[BTCVarCount - 1][51] = lastTransactionBuyLevelCrit1;
                                                        BTCVariablesArray[BTCVarCount - 1][52] = lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution;
                                                        BTCVariablesArray[BTCVarCount - 1][53] = actualMainInvestmentValuationWRTSellThresholdAtLastTransaction;
                                                    }
                                                }
                                            }

                                            //======================================================================================
                                            // Reset Buy/Sell Transaction Variables for this loop
                                            //======================================================================================
                                            // Re-initialize Sell order variables
                                            //hypotheticalMainInvestmentPriceAtSellThreshold = 0;
                                            bool sellOrderTrigger = false;
                                            int sellOrderTriggerAdjustFlag = 0;
                                            bool sellOrderTriggerPrelim = false;
                                            int sellByPassFlagLegacy = 0;
                                            int sellByPassCount = 0;
                                            int adjustmentsToSellOrderCount = 0;
                                            int skipSellTransaction = 0;
                                            double absoluteSellLevel = sellThreshold + lastTransactionSellLevelCrit1; // Used in ResultsDetail.xls file
                                            currentTransactionSellLevelCrit1 = sellCriteria.Min(x => x[0]);
                                            double currentTransactionSellExecutionPctCrit2 = 0.0;
                                            int dayOfLastSellOrderForThisCrit3 = 0;
                                            int daysSinceLastSellOrderForThisCrit3 = 0;
                                            double currentTransactionSellExecutionPctCrit2Orig = 0.0;
                                            double nextTransactionSellLevelCrit1 = 0.0;
                                            bool missedSellOrderFound = false;
                                            int confirmMatchingShareBuy = 0;
                                            bool A0s = false, B0s = false, C0s = false, H0s = false, J0s = false, K0s = false, L0s = false;
                                            bool Bs1 = false, Bs2 = false, Bs3 = false, Bs4 = false, Bs5 = false;
                                            bool Bs = false, Es = false, Fs = false, Gs = false, Hs = false, Ks = false, Ls = false, Ms = false;
                                            bool sellByPassFlag = false;
                                            bool sellOrderExcludeGs = false;
                                            double sellCriteria_jm1 = 0.0;
                                            actualTransactionSellLevelCrit1 = currentTransactionSellLevelCrit1;
                                            actualTransactionSellExecutionPctCrit2 = currentTransactionSellExecutionPctCrit2;
                                            int actualDaysSinceLastSellOrderForThisCrit3 = 0;
                                            double actualTransactionSellExecutionPctCrit2Last = 0.0;

                                            // Re-initialize Buy order variables
                                            bool buyOrderTrigger = false;
                                            int buyOrderTriggerAdjustFlag = 0;
                                            bool buyOrderTriggerPrelim = false;
                                            int buyByPassFlagLegacy = 0;
                                            int buyByPassCount = 0;
                                            int adjustmentsToBuyOrderCount = 0;
                                            double fundsNeededToCompleteBuyTransaction = 0.0;
                                            double absoluteBuyLevelMax = buyThreshold - lastTransactionBuyLevelCrit1; // Used in ResultsDetail.xls file
                                            currentTransactionBuyLevelCrit1 = buyCriteria.Max(x => x[0]);
                                            double currentTransactionBuyExecutionPctCrit2 = 0.0;
                                            double nextTransactionBuyLevelCrit1 = 0.0;
                                            int dayOfLastBuyOrderForThisCrit3 = 0;
                                            int daysSinceLastBuyOrderForThisCrit3 = 0;
                                            double currentTransactionBuyExecutionPctCrit2Orig = 0.0;
                                            bool missedBuyOrderFound = false;
                                            bool A0b = false, B0b = false, C0b = false, H0b = false, D0b = false, I0b = false, K0b = false, L0b = false;
                                            //bool J0b = false;
                                            bool Bb1 = false, Bb2 = false, Bb3 = false, Bb4 = false, Bb5 = false;
                                            bool Bb = false, Eb = false, Fb = false, Gb = false, Hb = false, Kb = false, Lb = false, Mb = false;
                                            bool CritIb = false, CritIIb = false, CritIIIb = false;
                                            bool buyByPassFlag = false;
                                            bool buyOrderExcludeGb = false;
                                            double buyCriteria_jm1 = 0.0;
                                            actualTransactionBuyLevelCrit1 = 0.0;
                                            actualTransactionBuyExecutionPctCrit2 = 0.0;
                                            int actualDaysSinceLastBuyOrderForThisCrit3 = 0;
                                            double actualTransactionBuyExecutionPctCrit2Last = 0.0;

                                            // Re-initialize general buy/sell variables
                                            int violationsMinCashCount = 0;
                                            int violationsMinShareCount = 0;

                                            double cashRatio = 0.0;
                                            double deltaShares = 0.0;
                                            double deltaCash = 0.0;
                                            double deltaSharesComplimentaryInvestment = 0.0;
                                            double deltaFundsComplimentaryInvestment = 0.0;

                                            double deltaSharesLast = 0.0;
                                            double deltaCashLast = 0.0;
                                            double deltaSharesComplimentaryInvestmentLast = 0.0;
                                            double deltaFundsComplimentaryInvestmentLast = 0.0;

                                            double residualShares = 0.0;

                                            //======================================================================================
                                            // Check for Preliminary Sell Trigger
                                            //======================================================================================
                                            // Calculate hypothetical main investment price at sell threshold
                                            hypotheticalMainInvestmentPriceAtSellThreshold = currentMainInvestmentRegressionValue * (1.0 + sellThreshold / 100.0);
                                            // Determine current main investment valuation with respect to sell threshold
                                            if (!settings.UseMIPriceInsteadOfMIValuationForTransactionControl)
                                            {
                                                currentMainInvestmentValuationWRTSellThreshold = currentMainInvestmentValuationWRTZero - sellThreshold;
                                            }
                                            else if (settings.UseMIPriceInsteadOfMIValuationForTransactionControl)
                                            {
                                                currentMainInvestmentValuationWRTSellThreshold = (sharePriceMainInvestment - hypotheticalMainInvestmentPriceAtSellThreshold) / hypotheticalMainInvestmentPriceAtSellThreshold * 100.0;
                                            }

                                            // Debugging: Collect data if enabled
                                            List<double[]> debugArraySell = new List<double[]>();
                                            debugArraySell.Add(new[] { dateNum, BTCBuyThresholdAdjustmentTracker, STCSellThresholdAdjustmentTracker ? 1.0 : 0.0, buyThreshold, sellThreshold, lastTransactionSellLevelCrit1, lastTransactionSellExecutionPctCrit2, currentMainInvestmentValuationWRTZero, currentMainInvestmentValuationWRTSellThreshold, lastMainInvestmentValuationWRTSellThreshold, lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, actualMainInvestmentValuationWRTSellThresholdAtLastTransaction });

                                            // Criteria for sellOrderTriggerPrelim
                                            A0s = movingAverageRateOfChangeWRTZero > settings.SellRateOfChangeValue;
                                            B0s = currentMainInvestmentValuationWRTSellThreshold > lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn;
                                            C0s = currentMainInvestmentValuationWRTSellThreshold >= sellCriteria.Min(x => x[0]);
                                            H0s = strategy != 20;

                                            // Determine preliminary sell order trigger
                                            sellOrderTriggerPrelim = false;

                                            if (!settings.EliminateDoldrumsFlag)
                                            {
                                                sellOrderTriggerPrelim = A0s && B0s && C0s && H0s;
                                            }
                                            else if (settings.EliminateDoldrumsFlag)
                                            {
                                                J0s = lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution < -30.0 && (currentMainInvestmentValuationWRTBuyThreshold - lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution) > 20.0;
                                                sellOrderTriggerPrelim = A0s && B0s && C0s && H0s && J0s;
                                            }

                                            // Verbose Logging
                                            if (settings.VerboseSharesBalanceFlag)
                                            {
                                                statusUpdater.UpdateStatus($"MSG SO0: Day={day}, Date={date[day]}, SellTriggerPrelim={sellOrderTriggerPrelim}, MarketLowWRTZero={marketLowWRTZero:F2}, MovingAverageRateOfChangeWRTZero={movingAverageRateOfChangeWRTZero:F2}, LastMIWRTSellThresholdAfterLastSellXctn={lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn:F2}");
                                            }

                                            if (settings.VerboseSellOrderFlag)
                                            {
                                                statusUpdater.UpdateStatus($"SO0: Day={day}, Date={date[day]}, SellTrigPrelim={sellOrderTriggerPrelim}, MovingAveWRTZeroRateOfChg={movingAverageRateOfChangeWRTZero:F2}, CurrentMIValWRTST={currentMainInvestmentValuationWRTSellThreshold:F2}, LastMI_WRT_STA_AfterLastSellXctn={lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn:F2}, MinSellCrit={sellCriteria.Min(x => x[0]):F2}");
                                            }

                                            if (settings.VerboseSellOrderFlag && settings.VerboseTransactionsFlag)
                                            {
                                                statusUpdater.UpdateStatus($"Sell INFO. Day={day}, Date={date[day]}, DateNum={dateNum}, SellTrigPre={sellOrderTriggerPrelim}, MovAvgWRT0ROC={movingAverageRateOfChangeWRTZero:F2}, CurValWRT0={currentMainInvestmentValuationWRTZero:F2}, MinSellCrit={sellCriteria.Min(x => x[0]):F2}, LastXctnSellLevCrit1={lastTransactionSellLevelCrit1:F2}, ActValWRTSTAtLastSellXctn={actualMainInvestmentValuationWRTSellThresholdAtLastTransaction:F2}, LastVal_WRTSTAfterLastSellXctn={lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn:F2}");
                                            }

                                            if (marketTrend > 5.5 && sellOrderTriggerPrelim)
                                            {
                                                statusUpdater.UpdateStatus($"INFO: Abnormal upward market movement. Date={date[day]}. MarketTrend(>5.5%) = {marketTrend:F4}");
                                            }


                                            //======================================================================================
                                            // Check for Preliminary Buy Trigger
                                            //======================================================================================
                                            // Calculate hypothetical main investment price at buy threshold
                                            hypotheticalMainInvestmentPriceAtBuyThreshold = currentMainInvestmentRegressionValue * (1.0 + buyThreshold / 100.0);

                                            // Determine current main investment valuation with respect to buy threshold
                                            if (!settings.UseMIPriceInsteadOfMIValuationForTransactionControl)
                                            {
                                                currentMainInvestmentValuationWRTBuyThreshold = currentMainInvestmentValuationWRTZero - buyThreshold;
                                            }
                                            else if (settings.UseMIPriceInsteadOfMIValuationForTransactionControl)
                                            {
                                                currentMainInvestmentValuationWRTBuyThreshold = (sharePriceMainInvestment - hypotheticalMainInvestmentPriceAtBuyThreshold) / hypotheticalMainInvestmentPriceAtBuyThreshold * 100.0;
                                            }

                                            // Log debug data if calculations are enabled
                                            if (fileSettings.RunCalculation == 1)
                                            {
                                                List<double[]> debugArrayBuy = new List<double[]>();
                                                debugArrayBuy.Add(new[] { dateNum, BTCBuyThresholdAdjustmentTracker, STCSellThresholdAdjustmentTracker ? 1.0 : 0.0, buyThreshold, sellThreshold, lastTransactionBuyLevelCrit1, lastTransactionBuyExecutionPctCrit2, currentMainInvestmentValuationWRTZero, currentMainInvestmentValuationWRTBuyThreshold, lastMainInvestmentValuationWRTBuyThreshold, lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction });
                                            }

                                            // Calculate price decrease from the last sell order
                                            if (actualMainInvestmentSharePriceAtLastSellTransaction == 0)
                                            {
                                                priceDecreaseFromLastSellOrder = 0;
                                            }
                                            else
                                            {
                                                priceDecreaseFromLastSellOrder = (sharePriceMainInvestment - actualMainInvestmentSharePriceAtLastSellTransaction) / actualMainInvestmentSharePriceAtLastSellTransaction * 100.0;
                                            }

                                            // Evaluate criteria for preliminary buy order trigger
                                            A0b = movingAverageRateOfChangeWRTZero < settings.BuyRateOfChangeValue;
                                            B0b = currentMainInvestmentValuationWRTBuyThreshold < lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution;
                                            C0b = currentMainInvestmentValuationWRTBuyThreshold <= buyCriteria.Max(x => x[0]);
                                            D0b = actualMainInvestmentSharePriceAtLastSellTransaction == 0 || (actualMainInvestmentSharePriceAtLastSellTransaction > 0 && Math.Abs(priceDecreaseFromLastSellOrder) >= settings.MinimumPricePercentageDropFromPreviousSellToEnableBuy);
                                            H0b = strategy == 20;
                                            I0b = cashInfusionFlag == 1;

                                            // Determine preliminary buy order trigger
                                            buyOrderTriggerPrelim = (A0b && B0b && C0b && D0b) || (H0b && I0b);

                                            // Log additional debug information if verbose mode is enabled
                                            if (settings.VerboseBuyOrderFlag)
                                            {
                                                statusUpdater.UpdateStatus($"MSG BO0: Day={day}, date={date[day]}, BuyTriggerPrelim={buyOrderTriggerPrelim}, CurrentBT={buyThreshold:0.00}, currentMIWRTZero={currentMainInvestmentValuationWRTZero:0.00}, currentMIWRTBuyThreshold={currentMainInvestmentValuationWRTBuyThreshold:0.00}, lastMIWRTBuyThresholdAfterLastBuyExecution={lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution:0.00}, lastTransactionBuyLevelCrit1={lastTransactionBuyLevelCrit1:0.00}");
                                            }

                                            // Handle abnormal downward market movement
                                            if (marketTrend < -5.5 && buyOrderTriggerPrelim == true)
                                            {
                                                // Log abnormal market movement if needed
                                                statusUpdater.UpdateStatus($"INFO: Abnormal downward market movement. date={date[day]}, marketTrend(< -5.5%)={marketTrend:0.0000}");
                                            }

                                            //======================================================================================
                                            // Sell Order Check
                                            //======================================================================================
                                            if (sellOrderTriggerPrelim && !buyOrderTriggerPrelim) // Market is in an uptrend.  Could be time to sell.  Check to see if sell criteria is met.
                                            {
                                                for (int j = 0; j < sellCriteria.Count; j++) // Run through each sell criteria setting to see if a sell order is generated
                                                {
                                                    // Extract criteria for this iteration
                                                    currentTransactionSellLevelCrit1 = sellCriteria[j][0];
                                                    currentTransactionSellExecutionPctCrit2 = sellCriteria[j][1];
                                                    dayOfLastSellOrderForThisCrit3 = (int)sellCriteria[j][2];

                                                    // Exit loop if transaction market level termination limit exceeded
                                                    if (currentTransactionSellLevelCrit1 + sellThreshold >= settings.TransactionMarketLevelTerminationLimit)
                                                    {
                                                        break;
                                                    }

                                                    // Determine next transaction sell level
                                                    if (j + 1 < sellCriteria.Count)
                                                    {
                                                        nextTransactionSellLevelCrit1 = sellCriteria[j + 1][0];
                                                    }
                                                    else
                                                    {
                                                        nextTransactionSellLevelCrit1 = sellCriteria[j][0] + 5.0;
                                                    }

                                                    // Boolean checks
                                                    // Check whether currentTransactionSellLevelCrit1 is between lastMainInvestmentValuationWRTSellThreshold and currentMainInvestmentValuationWRTSellThreshold
                                                    missedSellOrderFound = false;
                                                    if (!settings.FindAndIncludeMissedOrdersFlag)
                                                    {
                                                        Bs = lastMainInvestmentValuationWRTSellThreshold < currentTransactionSellLevelCrit1 && currentTransactionSellLevelCrit1 <= currentMainInvestmentValuationWRTSellThreshold;
                                                    }
                                                    else if (settings.FindAndIncludeMissedOrdersFlag)
                                                    {
                                                        Bs1 = lastMainInvestmentValuationWRTSellThreshold < currentTransactionSellLevelCrit1 && currentTransactionSellLevelCrit1 <= currentMainInvestmentValuationWRTSellThreshold;

                                                        deltaMainInvestmentWRTSellThreshold = currentMainInvestmentValuationWRTSellThreshold - lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn;

                                                        if (j == 0)
                                                        {
                                                            Bs = Bs1;
                                                            sellCriteria_jm1 = sellCriteria[j][0];
                                                        }
                                                        else
                                                        {
                                                            sellCriteria_jm1 = sellCriteria[j - 1][0];
                                                            Bs2 = lastMainInvestmentValuationWRTSellThreshold > sellCriteria[j - 1][0];
                                                            Bs3 = currentMainInvestmentValuationWRTSellThreshold < currentTransactionSellLevelCrit1;
                                                            Bs4 = lastMainInvestmentValuationWRTSellThreshold < currentMainInvestmentValuationWRTSellThreshold;

                                                            deltaSellCriteria = sellCriteria[j][0] - sellCriteria[j - 1][0];
                                                            Bs5 = deltaMainInvestmentWRTSellThreshold >= deltaSellCriteria; //Checks to make sure that the different between the curMIValtn and lastMIValtn at the last Sell exceeds delta SellCrit1 in order to prevent another sell too soon after a previous sell

                                                            Bs = (Bs1 || (Bs2 && Bs3 && Bs4)) && Bs5;
                                                            missedSellOrderFound = (Bs2 && Bs3 && Bs4 && Bs5);

                                                            if (missedSellOrderFound && settings.VerboseSellOrderFlag)
                                                            {
                                                                statusUpdater.UpdateStatus($"Missed Sell Order Found: Day={day}, Date={date[day]}, CurrentSellCrit1={currentTransactionSellLevelCrit1:F2}, LastValWRTSellThreshold={lastMainInvestmentValuationWRTSellThreshold:F2}, CurrentValWRTSellThreshold={currentMainInvestmentValuationWRTSellThreshold:F2}, DateNum={dateNum}, j={j}");
                                                            }
                                                        }
                                                    }

                                                    // Check whether the level of the current transaction is greater than the level of the last transaction
                                                    Es = currentTransactionSellLevelCrit1 >= lastTransactionSellLevelCrit1;

                                                    // Calculate days since the last sell order for this criterion
                                                    daysSinceLastSellOrderForThisCrit3 = day - dayOfLastSellOrderForThisCrit3;
                                                    Fs = daysSinceLastSellOrderForThisCrit3 >= settings.CriteriaDaysSinceLastSellTransactionAtSameLevelDefault;

                                                    // Check whether the minimum share criteria is met
                                                    Gs = sharesMainInvestmentAvailableToSell > 0.0 || sellByPassFlagLegacy >= 1;
                                                    if (sharesMainInvestmentAvailableToSell <= 0.0 && sellByPassFlagLegacy >= 1) //This alert will be generated if criteria is met
                                                    {
                                                        deltaShares = deltaSharesLast;
                                                        deltaCash = deltaCashLast;
                                                        actualTransactionSellExecutionPctCrit2 = actualTransactionSellExecutionPctCrit2Last;

                                                        if (settings.VerboseSellOrderFlag)
                                                        {
                                                            statusUpdater.UpdateStatus($"ALERT: Ran out of available shares during a multiple sell transaction. Day={day}, Date={date[day]}, DateNum={dateNum}, SellByPassFlag={sellByPassFlag}, SellByPassFlagLegacy={sellByPassFlagLegacy}, SellOrderTrigger={sellOrderTrigger}, BP={buyProfile}, SP={sellProfile}, ST={strategy}, j={j}, Bs={Bs}, Es={Es}, Fs={Fs}, Gs={Gs}, Hs={Hs}");
                                                        }

                                                        break;
                                                    }

                                                    // Check whether currentTransactionSellExecutionPctCrit2 is greater than zero
                                                    if (!settings.SpuriousSellFlag)
                                                    {
                                                        Hs = currentTransactionSellExecutionPctCrit2 > 0.0 || sellByPassFlagLegacy >= 1;
                                                    }
                                                    else if (settings.SpuriousSellFlag)
                                                    {
                                                        Hs = true;
                                                    }

                                                    // Technical analysis checks
                                                    Ks = relativeStrengthIndex >= settings.RelativeStrengthIndexSellCrit;
                                                    Ls = BBRatioStandardDeviations >= settings.BBRatioSTDDevSellCrit;
                                                    Ms = lastMainInvestmentValuationWRTSellThreshold - currentMainInvestmentValuationWRTSellThreshold >= 5;

                                                    // Determine if sell order should be executed
                                                    if (settings.UseTechnicalAnalysisCriteriaSellSideFlag)
                                                    {
                                                        sellOrderTrigger = Bs && Es && Fs && Gs && Hs && Ks && Ls;
                                                    }
                                                    else
                                                    {
                                                        sellOrderTrigger = Bs && Es && Fs && Gs && Hs;
                                                    }

                                                    // Check if the market moved up so fast that it bypassed a legitimate sell order
                                                    sellByPassFlag = Bs && nextTransactionSellLevelCrit1 < currentMainInvestmentValuationWRTSellThreshold;

                                                    // Check to see whether a legit sell order was ignored due to lack of funds.  Record as violationsMinShareCount
                                                    if (settings.UseTechnicalAnalysisCriteriaSellSideFlag)
                                                    {
                                                        sellOrderExcludeGs = Bs && Es && Fs && Hs && Ks && Ls;
                                                    }
                                                    else
                                                    {
                                                        sellOrderExcludeGs = Bs && Es && Fs && Hs;
                                                    }

                                                    if (sellOrderExcludeGs && !Gs)
                                                    {
                                                        violationsMinShareCount = 1;
                                                        if (settings.VerboseSharesBalanceFlag || settings.VerboseSellOrderFlag)
                                                        {
                                                            statusUpdater.UpdateStatus($"!MSG SO1: Potential sell order skipped due to min share balance issue. Day={day}, Date={date[day]}, DateNum={dateNum}, j={j}, NoVios={violationsMinShareCount}, SharesATS={sharesMainInvestmentAvailableToSell:F2}, ByPass={sellByPassFlag}, ByPassLegacy={sellByPassFlagLegacy}");
                                                        }
                                                    }

                                                    // TODO: This routine was not working.  Need to fix
                                                    //if (settings.EliminateSimilarSellsAtSameMainInvestmentValueFlag == 1)
                                                    //{
                                                    //    double lastSellDay = -1.0;
                                                    //    double sellFound = 0.0;

                                                    //    if (sellOrderTrigger)
                                                    //    {
                                                    //        for (lastSellDay = day - 1; lastSellDay >= day - settings.CriteriaDaysSinceLastSellTransactionAtSameMainInvestmentValue; lastSellDay--)
                                                    //        {
                                                    //            if (lastSellDay <= 0)
                                                    //            {
                                                    //                sellFound = 0.0;
                                                    //                break;
                                                    //            }
                                                    //            else if (aTransactions.Count > (int)lastSellDay && aTransactions[(int)lastSellDay][3] == -1.0)
                                                    //            {
                                                    //                sellFound = 1.0;
                                                    //                break;
                                                    //            }
                                                    //        }

                                                    //        if (sellFound == 1.0)
                                                    //        {
                                                    //            double criteriaSellMainInvestmentDollarDifferenceTolerance = mainInvestmentClosePrice[startingMarketDayThisRun + day - 1] * settings.CriteriaSellMainInvestmentDollarDifferenceTolerancePct / 100.0;
                                                    //            double mainInvestmentClosePriceDifference = Math.Abs(mainInvestmentClosePrice[startingMarketDayThisRun + day - 1] - aTransactions[(int)lastSellDay][2]);

                                                    //            if (mainInvestmentClosePriceDifference <= criteriaSellMainInvestmentDollarDifferenceTolerance)
                                                    //            {
                                                    //                sellOrderTrigger = false;
                                                    //                aTransactions[day][11] = 1; // Sell skipped flag
                                                    //            }
                                                    //            else
                                                    //            {
                                                    //                aTransactions[day][11] = 0; // Sell not skipped
                                                    //            }
                                                    //        }
                                                    //        aTransactions[day][12] = sellByPassFlagLegacy;

                                                    //        if (settings.VerboseSellOrderFlag == 1 && !sellOrderTrigger)
                                                    //        {
                                                    //            statusUpdater.UpdateStatus($"!MSG SO1a: Day={day}. Sell order skipped due to eliminateSimilarSellsAtSameMainInvestmentValueFlag.");
                                                    //        }
                                                    //    }
                                                    //}

                                                    // In the event of an issue with the bypass algorithm, this will close out the multiple sell transaction, and an alert will be generated.
                                                    if (sellByPassFlagLegacy >= 1 && !sellOrderTrigger)
                                                    {
                                                        deltaShares = deltaSharesLast;
                                                        deltaCash = deltaCashLast;
                                                        actualTransactionSellExecutionPctCrit2 = actualTransactionSellExecutionPctCrit2Last;

                                                        if (settings.VerboseSellOrderFlag)
                                                        {
                                                            statusUpdater.UpdateStatus($"ALERT: In the midst of a multiple Sell transaction, sellOrderTrigger=0 when sellByPassFlagLegacy>=1. No worries, your last sell order was closed out successfully, but not via the normal process. Day={day}, Date={date[day]}, DateNum={dateNum}, SellByPassFlag={sellByPassFlag}, SellByPassFlagLegacy={sellByPassFlagLegacy}, SellOrderTrigger={sellOrderTrigger}");
                                                        }

                                                        break;
                                                    }

                                                    // Used for Debug only
                                                    if (settings.WriteToSellLoopDetailsCSVFileFlag && dateNum >= settings.DateNumStartDebug && dateNum <= settings.DateNumEndDebug)
                                                    {
                                                        string debugDetails = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10:F2}, {11:F2}, {12:F2}, {13:F2}, {14:F2}, {15:F2}, {16:F2}, {17:F2}, {18}, {19}, {20}, {21:F4}, {22:F4}, {23:F1}", dateNum, j, Bs ? 1 : 0, Es ? 1 : 0, Fs ? 1 : 0, Gs ? 1 : 0, Hs ? 1 : 0, sellOrderTrigger ? 1 : 0, sellByPassFlag, sellByPassFlagLegacy, currentMainInvestmentValuationWRTSellThreshold, Math.Round(currentMainInvestmentValuationWRTSellThreshold), currentTransactionSellLevelCrit1, lastMainInvestmentValuationWRTSellThreshold, lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, lastTransactionSellLevelCrit1, nextTransactionSellLevelCrit1, currentTransactionSellExecutionPctCrit2, dayOfLastSellOrderForThisCrit3, daysSinceLastSellOrderForThisCrit3, buyFlag, sharesMainInvestment, shareBalanceMainInvestmentMinCriteria, sharePriceMainInvestment);

                                                        //if (csvSellLoopDetailsFileID != null)
                                                        //{
                                                        //    csvSellLoopDetailsFileID.WriteLine(debugDetails);
                                                        //}
                                                    }


                                                    //======================================================================================
                                                    //Execute Sell Order
                                                    //======================================================================================
                                                    if (sellOrderTrigger) // If sellOrderTrigger = 1 (true), Execute sell order
                                                    {
                                                        currentTransactionSellExecutionPctCrit2Orig = currentTransactionSellExecutionPctCrit2; // Save this for comparison with the final value.

                                                        if (settings.FindAndIncludeMissedOrdersFlag && missedSellOrderFound)
                                                        {
                                                            double adjustedCurrentTransactionSellExecutionPctCrit2;
                                                            // Adjust currentTransactionSellExecutionPctCrit2 to correspond to the current level of currentMainInvestmentValuationWRTSellThreshold
                                                            // Interpolation formula: y = y1 + ((x – x1) / (x2 – x1)) *(y2 – y1)
                                                            if (j == 0)
                                                            {
                                                                adjustedCurrentTransactionSellExecutionPctCrit2 = currentTransactionSellExecutionPctCrit2;
                                                            }
                                                            else
                                                            {
                                                                double x = currentMainInvestmentValuationWRTSellThreshold;
                                                                double x1 = sellCriteria[j - 1][0];
                                                                double x2 = currentTransactionSellLevelCrit1;
                                                                double y1 = sellCriteria[j - 1][1];
                                                                double y2 = currentTransactionSellExecutionPctCrit2;
                                                                adjustedCurrentTransactionSellExecutionPctCrit2 = y1 + ((x - x1) / (x2 - x1)) * (y2 - y1);

                                                                if (fileSettings.RunCalculation == 1 && settings.VerboseTransactionsFlag)
                                                                {
                                                                    statusUpdater.UpdateStatus($"Missed Transaction: Day={day}, Date={date[day]}, DateNum={dateNum}, currentTransactionSellExecutionPctCrit2Orig={currentTransactionSellExecutionPctCrit2Orig:F2}, adjustedCurrentTransactionSellExecutionPctCrit2={adjustedCurrentTransactionSellExecutionPctCrit2:F2}");
                                                                }

                                                                if (adjustedCurrentTransactionSellExecutionPctCrit2 < 0)
                                                                {
                                                                    statusUpdater.UpdateStatus($"adjustedCurrentTransactionSellExecutionPctCrit2={adjustedCurrentTransactionSellExecutionPctCrit2:F2} is less than 0. Day={day}, Date={date[day]}, DateNum={dateNum}, X1={x1:F2}, X={x:F2}, X2={x2:F2}, Y1={y1:F2}, Y={adjustedCurrentTransactionSellExecutionPctCrit2:F2}, Y2={y2:F2}");
                                                                }
                                                            }

                                                            currentTransactionSellExecutionPctCrit2 = adjustedCurrentTransactionSellExecutionPctCrit2;
                                                        }

                                                        if (sharesMainInvestmentAvailableToSell > 0.0) // If the estimated shares is greater than shareBalanceMainInvestmentMinCriteria then complete the transaction, otherwise skip the sell transaction
                                                            // Transaction calculation: Execute a Sell order with a percentage of your available shares
                                                        {
                                                            deltaShares = currentTransactionSellExecutionPctCrit2 / 100.0 * sharesMainInvestmentAvailableToSell;

                                                            //If limitTransactionAmountFlag is enabled, then, if necessary, reduce deltaShares to a lesser number
                                                            if (settings.LimitTransactionAmountFlag == 1)
                                                            {
                                                                if (deltaShares * sharePriceMainInvestment > maxAllowedTransactionAmountCurrentYrSellOrder)
                                                                {
                                                                    deltaShares = maxAllowedTransactionAmountCurrentYrSellOrder / sharePriceMainInvestment;
                                                                    currentTransactionSellExecutionPctCrit2 = deltaShares / sharesMainInvestmentAvailableToSell * 100.0;
                                                                }
                                                            }
                                                            else if (settings.LimitTransactionAmountFlag == 2)
                                                            {
                                                                if (deltaShares * sharePriceMainInvestment > maxAllowedTransactionAmountCurrentYrSellOrder * currentTransactionSellExecutionPctCrit2 / 100.0)
                                                                {
                                                                    double preModDeltaShares = deltaShares;
                                                                    double deltaSharesLTAF1 = maxAllowedTransactionAmountCurrentYrSellOrder / sharePriceMainInvestment;
                                                                    deltaShares = (maxAllowedTransactionAmountCurrentYrSellOrder * currentTransactionSellExecutionPctCrit2 / 100.0) / sharePriceMainInvestment;
                                                                    currentTransactionSellExecutionPctCrit2 = deltaShares / sharesMainInvestmentAvailableToSell * 100.0;

                                                                    if (fileSettings.RunCalculation == 1 && settings.VerboseTransactionsFlag)
                                                                    {
                                                                        statusUpdater.UpdateStatus($"Limit Transaction=2 SELL Alert. Day={day}, Date={date[day]}, DateNum={dateNum}, maxAllowedTransactionAmountCurrentYrSellOrder={maxAllowedTransactionAmountCurrentYrSellOrder:F0}, currentTransactionSellExecutionPctCrit2Orig={currentTransactionSellExecutionPctCrit2Orig:F0}, currentTransactionSellExecutionPctCrit2={currentTransactionSellExecutionPctCrit2:F1}, preModDeltaShares={preModDeltaShares:F2}, deltaSharesLTAF1={deltaSharesLTAF1:F2}, deltaShares={deltaShares:F2}");
                                                                    }
                                                                }
                                                            }

                                                            if (settings.SuperChargeFlag)
                                                            {
                                                                cashRatio = Math.Min(cashBalanceMinCriteria / cash[day], 10.0); // Limit cashRatio to 10
                                                                if (cashRatio >= settings.CashRatioThreshold)
                                                                {
                                                                    double preModDeltaShares = deltaShares;
                                                                    double SCFactor = 1.0 + (settings.SuperchargeDeltaSharesIncreasePct / 100.0 * cashRatio);
                                                                    deltaShares *= SCFactor;
                                                                    currentTransactionSellExecutionPctCrit2 = deltaShares / sharesMainInvestmentAvailableToSell * 100.0;

                                                                    if (fileSettings.RunCalculation == 1 && settings.VerboseTransactionsFlag)
                                                                    {
                                                                        statusUpdater.UpdateStatus($"SuperCharge Transaction SELL Alert. Day={day}, Date={date[day]}, DateNum={dateNum}, cashBalanceMinCriteria={cashBalanceMinCriteria:F0}, TotalCash={cash[day]:F0}, cashRatio={cashRatio:F2}, SCFactor={SCFactor:F2}, preModDeltaShares={preModDeltaShares:F2}, deltaShares={deltaShares:F2}, currentTransactionSellExecutionPctCrit2Orig={currentTransactionSellExecutionPctCrit2Orig:F2}, currentTransactionSellExecutionPctCrit2={currentTransactionSellExecutionPctCrit2:F2}");
                                                                    }
                                                                }
                                                            }

                                                            // Check whether the deltaShares calculated above correspond, quantity-wise, to shares purchased at a lesser value, thus resulting is a positive capital gain.
                                                            // If not, adjust deltaShares lesser quantity, or if delta shares = 0 cancel this sell order.
                                                            if (settings.OptimizeCapitalGainFlag)
                                                            {
                                                                //TODO: make sure that the conversion below is correct.  Does sellByPassFlag > 0 really mean sellByPassFlag =1
                                                                //if (settings.VerboseCapitalGainCalcFlag == 1 && sellByPassFlag > 0)
                                                                if (settings.VerboseCapitalGainCalcFlag && sellByPassFlag)
                                                                {
                                                                    statusUpdater.UpdateStatus($"SELLBYPASSFLAG={sellByPassFlag}, Day={day}, Date={date[day]}, DateNum={dateNum}");
                                                                }

                                                                if (settings.VerboseCapitalGainCalcFlag && sellByPassFlagLegacy > 0)
                                                                {
                                                                    statusUpdater.UpdateStatus($"SELLBYPASSFLAGLEGACY={sellByPassFlagLegacy}, Day={day}, Date={date[day]}, DateNum={dateNum}");
                                                                }

                                                                if (sellByPassCount == 0)
                                                                {
                                                                    //capitalGainArrayCopy = new List<double[]>(capitalGainArray); // Create copy of capitalGainArray unless in the mist of a sellByPass loop. If in sellByPass loop use the updated capitalGainArrayCopy from the last Sale-On-Same-Day
                                                                    capitalGainArrayCopy = capitalGainArray.Select(arr => arr.ToArray()).ToList(); // Create copy of capitalGainArray unless in the mist of a sellByPass loop. If in sellByPass loop use the updated capitalGainArrayCopy from the last Sale-On-Same-Day
                                                                }

                                                                double gainCalculation = 0.0;
                                                                residualShares = -deltaShares; //  Number of shares remaining after the capital gain calculation (want calculation to equal 0). Note that deltaShares of Sell are negative while deltaShares of Buy are positive
                                                                for (int q = 0; q <= cg; q++) // Loop through all the previous transactions to look for residual shares that to sell against
                                                                {
                                                                    if (capitalGainArrayCopy[q][6] > 0.0) // Check residual share quantity of a previous purchase to ensure there are shares to sell against. Need a positive number of residual shares, otherwise skip to next Buy transaction.
                                                                    {
                                                                        if (capitalGainArrayCopy[q][3] < sharePriceMainInvestment / (settings.MinGainRequirement / 100.0 + 1.0)) //The share price of a preceding Buy must be less than the minGain-adjusted share price of the impending Sell
                                                                        {
                                                                            if (Math.Round(capitalGainArrayCopy[q][6], 8) < -Math.Round(residualShares, 8)) //Evaluate the case where impending residual shares exceed those of previous Buy transaction
                                                                            {
                                                                                double sharesAllocatedToSellTransaction = capitalGainArrayCopy[q][6];
                                                                                gainCalculation += sharesAllocatedToSellTransaction * (sharePriceMainInvestment - capitalGainArrayCopy[q][3]);
                                                                                capitalGainArrayCopy[q][6] = 0.0; // Adjust the residual shares of the Buy transaction. In this case, all shares were used.
                                                                                residualShares += sharesAllocatedToSellTransaction; //Adjust the residual shares of impending Sell transaction
                                                                            }
                                                                            else if (Math.Round(capitalGainArrayCopy[q][6], 8) == -Math.Round(residualShares, 8)) //Evaluate the case where impending residual shares equal those of previous Buy transaction
                                                                            {
                                                                                double sharesAllocatedToSellTransaction = capitalGainArrayCopy[q][6];
                                                                                gainCalculation += sharesAllocatedToSellTransaction * (sharePriceMainInvestment - capitalGainArrayCopy[q][3]); //Calculate gain
                                                                                capitalGainArrayCopy[q][6] = 0.0; // Adjust the residual shares of the Buy transaction. In this case, all shares were used.
                                                                                residualShares = 0.0; //Adjust the residual shares of impending Sell transaction. All impending residual shares where accounted for.
                                                                                break;
                                                                            }
                                                                            else if (Math.Round(capitalGainArrayCopy[q][6], 8) > -Math.Round(residualShares, 8)) //Evaluate the case where impending residual shares are less than those of previous Buy transaction
                                                                            {
                                                                                double sharesAllocatedToSellTransaction = -residualShares;
                                                                                gainCalculation += sharesAllocatedToSellTransaction * (sharePriceMainInvestment - capitalGainArrayCopy[q][3]); //Calculate gain
                                                                                capitalGainArrayCopy[q][6] -= sharesAllocatedToSellTransaction; // Adjust the residual shares of the Buy transaction. In this case, only a portion of the shares were used.
                                                                                residualShares = 0.0; //Adjust the residual shares of impending Sell transaction. All impending residual shares where accounted for.
                                                                                break;
                                                                            }
                                                                        }
                                                                    }
                                                                }

                                                                if (Math.Round(residualShares, 8) != 0.0) //If after going through all the previous transactions, there are still residual share remaining, then need to adjust the impending deltaShares count
                                                                {
                                                                    if (settings.VerboseCapitalGainCalcFlag)
                                                                    {
                                                                        statusUpdater.UpdateStatus($"ADJUSTMENT TO SELL ORDER. Adjusted deltaShares: From: {deltaShares:F2}, To: {deltaShares + residualShares:F2}, Day={day}, Date={date[day]}, DateNum={dateNum}");
                                                                    }

                                                                    deltaShares += residualShares;

                                                                    //if (sellByPassFlag > 0)
                                                                    if (sellByPassFlag) //Since there weren't enough residual shares to cover this Sell, then need to terminate the SellByPass loop
                                                                    {
                                                                        sellByPassFlag = false;
                                                                        if (settings.VerboseCapitalGainCalcFlag)
                                                                        {
                                                                            statusUpdater.UpdateStatus($"SELL BYPASS LOOP TRUNCATED. Not enough residual shares for Same-Day-Sell. Day={day}, Date={date[day]}, DateNum={dateNum}, MI={ticker}, SELLPROFILE={settings.SellProfileLowEndTruncateLevel}, BUYPROFILE={settings.BuyProfileLowEndTruncateLevel}, STRATEGY={settings.STR[0]}");
                                                                        }
                                                                    }

                                                                    if (deltaShares <= 0.0)
                                                                    {
                                                                        deltaShares = 0.0;
                                                                        sellOrderTrigger = false;
                                                                        if (settings.VerboseCapitalGainCalcFlag)
                                                                        {
                                                                            statusUpdater.UpdateStatus($"SELL ORDER CANCELLED! Residual Shares exceed deltaShares. Day={day}, Date={date[day]}, DateNum={dateNum}");
                                                                        }

                                                                        break;
                                                                    }

                                                                    currentTransactionSellExecutionPctCrit2 = deltaShares / sharesMainInvestmentAvailableToSell * 100.0;
                                                                }

                                                                if (gainCalculation < 0) // Should never get this condition, but this will stop the calculation if it exists
                                                                {
                                                                    statusUpdater.UpdateStatus($"ERROR IN CG CALC: Gain is less than zero. CapitalGain={gainCalculation:F2}, Day={day}, Date={date[day]}, DateNum={dateNum}, MI={ticker}, SELLPROFILE={settings.SellProfileLowEndTruncateLevel}, BUYPROFILE={settings.BuyProfileLowEndTruncateLevel}, STRATEGY={settings.STR[0]}");
                                                                    throw new InvalidOperationException("Your gain is less than zero. You sold shares at a lesser value than you bought them.");
                                                                }
                                                            }

                                                            sharesMainInvestment -= deltaShares; // Update number of shares owned (decrease)
                                                            if (sharesMainInvestment < 0.0)
                                                            {
                                                                statusUpdater.UpdateStatus($"WARNING: Calculated number of shares is less than zero. Setting Shares=0: sharesMainInvestmentAvailableToSell={sharesMainInvestmentAvailableToSell:F2}, currentTransactionSellExecutionPctCrit2={currentTransactionSellExecutionPctCrit2:F2}, shares={sharesMainInvestment:F2}, Day={day}, date={date[day]}, dateNum={dateNum}, buyByPassFlag={buyByPassFlag}, buyByPassFlagLegacy={buyByPassFlagLegacy}, buyOrderTrigger={buyOrderTrigger}, SP={sellProfile}, BP={buyProfile}, STR={strategy}, j={j}, Bb={Bb}, Eb={Eb}, Fb={Fb}, Gb={Gb}, Hb={Hb}");
                                                                sharesMainInvestment = 0.0;
                                                            }

                                                            marketValueMainInvestmentShares = sharesMainInvestment * sharePriceMainInvestment; // Current market value of shares in your account

                                                            deltaCash = Math.Round(deltaShares * sharePriceMainInvestment, 8); // Cash gained from transaction
                                                            cash[day] += deltaCash; // Update cash balance (increase)

                                                            // Update Transaction details
                                                            deltaShares = -deltaShares; // Correct the sign

                                                            actualTransactionSellLevelCrit1 = currentTransactionSellLevelCrit1;
                                                            actualTransactionSellExecutionPctCrit2 = currentTransactionSellExecutionPctCrit2;
                                                            actualDaysSinceLastSellOrderForThisCrit3 = daysSinceLastSellOrderForThisCrit3;
                                                            sellCriteria[j][2] = day;
                                                            dayOfLastSell = day;
                                                            actualMainInvestmentValuationWRTSellThresholdAtLastTransaction = currentMainInvestmentValuationWRTSellThreshold;
                                                            actualMainInvestmentSharePriceAtLastSellTransaction = sharePriceMainInvestment;

                                                            // This parameter, nextSellLevelCrit1, is the parameter that gets printed-out on the time history plot that indicates the next sell level.
                                                            if (strategy == 20)
                                                            {
                                                                nextSellLevelCrit1 = 0.0;
                                                            }
                                                            else
                                                            {
                                                                if (j + 1 < sellCriteria.Count)
                                                                {
                                                                    nextSellLevelCrit1 = sellCriteria[j + 1][0];
                                                                }
                                                                else
                                                                {
                                                                    nextSellLevelCrit1 = sellCriteria[j][0]; // This will print out the wrong answer but it is better than a crash
                                                                }

                                                                if (nextSellLevelCrit1 == 110.0) // This is needed because some SPs, like SP 6, terminate at low levels
                                                                {
                                                                    nextSellLevelCrit1 = sellCriteria[j][0];
                                                                }
                                                            }

                                                            // These parameters are used to signify that a Sell order was executed in this transaction.
                                                            // It is used to make sure that you don't start a new buy series unless sellFlag = 1 before the start of the new buy series
                                                            sellFlag = 1;
                                                            buyFlag = 0;

                                                            if (settings.VerboseSellOrderFlag)
                                                            {
                                                                statusUpdater.UpdateStatus($"SELL WAS EXECUTED: Day={day}, ExecutionLevel={currentTransactionSellLevelCrit1:F1}, ExecutionPct={currentTransactionSellExecutionPctCrit2:F1}, DelShares={deltaShares:F2}, SharesMI={sharesMainInvestment:F1}, PriceMI={sharePriceMainInvestment:F1}");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            // Skip the sell transaction
                                                            deltaShares = 0.0;
                                                            deltaCash = 0.0;
                                                            actualTransactionSellLevelCrit1 = currentTransactionSellLevelCrit1;
                                                            actualTransactionSellExecutionPctCrit2 = 0.0;
                                                            violationsMinShareCount += 1;
                                                            skipSellTransaction = 1;

                                                            statusUpdater.UpdateStatus($"EVENT-MSG SO2: A potential sell order was skipped due to min share balance issue. Day={day}, date={date[day]}, dateNum={dateNum}, SP={sellProfile}, BP={buyProfile}, STR={strategy}, NoVios={violationsMinShareCount}, sharesAFS={sharesMainInvestmentAvailableToSell:F2}");
                                                        }

                                                        if (!sellByPassFlag)
                                                        {
                                                            if (sellByPassFlagLegacy >= 1)
                                                            {
                                                                deltaShares += deltaSharesLast;
                                                                deltaCash += deltaCashLast;
                                                                actualTransactionSellLevelCrit1 = currentTransactionSellLevelCrit1;
                                                                actualTransactionSellExecutionPctCrit2 += actualTransactionSellExecutionPctCrit2Last;
                                                            }

                                                            break; // Exit the sell loop after executing a sell order
                                                        }
                                                        else if (sellByPassFlag)
                                                        {
                                                            // Record data from the ByPass Sell execution
                                                            sellByPassFlagLegacy += 1;
                                                            deltaSharesLast += deltaShares;
                                                            deltaCashLast += deltaCash;
                                                            actualTransactionSellExecutionPctCrit2Last += actualTransactionSellExecutionPctCrit2;
                                                            lastTransactionSellLevelCrit1 = actualTransactionSellLevelCrit1;
                                                            sharesMainInvestmentAvailableToSell = sharesMainInvestment - shareBalanceMainInvestmentMinCriteria;
                                                            sellByPassCount += 1;

                                                            if (settings.VerboseFlag)
                                                            {
                                                                statusUpdater.UpdateStatus($"THE SELLBYPASSFLAG HAS BEEN ACTIVATED: Day={day}, ByPassCount={sellByPassCount}");
                                                            }

                                                            continue; // Perform another Sell execution at the next level
                                                        }
                                                    }
                                                }
                                            }

                                            // If no sell order was triggered, need to reset variables below
                                            if (!sellOrderTrigger)
                                            {
                                                actualTransactionSellLevelCrit1 = 0.0;
                                                actualTransactionSellExecutionPctCrit2 = 0.0; // No order execution
                                                actualDaysSinceLastSellOrderForThisCrit3 = 0; // No order execution
                                            }

                                            // Sell Order Results
                                            resultsSellOrder.Add(new double[]
                                            {
                                                dateNum, dateNum, sellResetType1Flag, sellResetType2Flag, +lastMainInvestmentValuationWRTZero, currentMainInvestmentValuationWRTZero, hypotheticalMainInvestmentPriceAtSellThreshold, currentMainInvestmentRegressionValue, sellThreshold, movingAverageRateOfChangeWRTZero, settings.SellRateOfChangeValue, A0s ? 1 : 0, lastMainInvestmentValuationWRTSellThreshold, currentMainInvestmentValuationWRTSellThreshold, lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, B0s ? 1 : 0, sellCriteria.Min(s => s[0]), // Assuming sellCriteria is List<double[]>
                                                C0s ? 1 : 0, strategy, H0s ? 1 : 0, J0s ? 1 : 0, K0s ? 1 : 0, L0s ? 1 : 0, sellOrderTriggerPrelim ? 1 : 0, buyOrderTriggerPrelim ? 1 : 0, currentTransactionSellLevelCrit1, currentMainInvestmentValuationWRTSellThreshold, lastMainInvestmentValuationWRTSellThreshold, Bs1 ? 1 : 0, sellCriteria_jm1, Bs2 ? 1 : 0, Bs3 ? 1 : 0, Bs4 ? 1 : 0, currentMainInvestmentValuationWRTSellThreshold, lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, deltaMainInvestmentWRTSellThreshold, Bs5 ? 1 : 0, missedSellOrderFound ? 1 : 0, Bs ? 1 : 0, currentTransactionSellLevelCrit1, lastTransactionSellLevelCrit1, Es ? 1 : 0, daysSinceLastSellOrderForThisCrit3, day, dayOfLastSellOrderForThisCrit3, criteriaDaysSinceLastSellTransactionAtSameLevel, Fs ? 1 : 0, sharesMainInvestmentAvailableToSell, sellByPassFlagLegacy, Gs ? 1 : 0, settings.SpuriousSellFlag ? 1 : 0, currentTransactionSellExecutionPctCrit2, sellByPassFlagLegacy, Hs ? 1 : 0, relativeStrengthIndex, settings.RelativeStrengthIndexSellCrit, Ks ? 1 : 0, BBRatioStandardDeviations, settings.BBRatioSTDDevSellCrit, Ls ? 1 : 0, Bs ? 1 : 0, currentMainInvestmentValuationWRTSellThreshold, nextTransactionSellLevelCrit1, sellByPassFlag ? 1 : 0, violationsMinShareCount, sellOrderTrigger ? 1 : 0, sharesMainInvestmentAvailableToSell, settings.FindAndIncludeMissedOrdersFlag ? 1 : 0, missedSellOrderFound ? 1 : 0, settings.LimitTransactionAmountFlag, maxAllowedTransactionAmountCurrentYr, settings.SuperChargeFlag ? 1 : 0, settings.CashRatioThreshold, cashRatio, cashBalanceMinCriteria, settings.SuperchargeDeltaSharesIncreasePct, settings.OptimizeCapitalGainFlag ? 1 : 0, residualShares, currentTransactionSellExecutionPctCrit2Orig, currentTransactionSellExecutionPctCrit2, deltaShares, deltaCash, actualTransactionSellLevelCrit1, actualTransactionSellExecutionPctCrit2, actualDaysSinceLastSellOrderForThisCrit3, actualMainInvestmentValuationWRTSellThresholdAtLastTransaction, actualMainInvestmentSharePriceAtLastSellTransaction, sellByPassFlagLegacy, sellByPassCount, sellFlag, nextSellLevelCrit1
                                            });

                                            //======================================================================================
                                            // Buy Order Check
                                            //======================================================================================
                                            // Buy Order
                                            if (buyOrderTriggerPrelim && !sellOrderTriggerPrelim) // Market is in a downtrend. Could be time to buy. Check to see if buy criteria are met.
                                            {
                                                for (int j = 0; j < buyCriteria.Count; j++) // Iterate over each buy criteria setting to check if it generates a buy order
                                                {
                                                    // Establish criteria for this iteration
                                                    currentTransactionBuyLevelCrit1 = buyCriteria[j][0]; // buyProfileRelativeMrktLevel
                                                    currentTransactionBuyExecutionPctCrit2 = buyCriteria[j][1]; // buyProfilePctOfAvailFunds
                                                    dayOfLastBuyOrderForThisCrit3 = (int)buyCriteria[j][2];

                                                    // Exit loop if exceeds transaction market level termination limit
                                                    if (currentTransactionBuyLevelCrit1 + buyThreshold <= -settings.TransactionMarketLevelTerminationLimit)
                                                    {
                                                        statusUpdater.UpdateStatus($"Buy loop termination. BuyCrit={currentTransactionBuyLevelCrit1:F1} currentMainInvestmentValuationWRTZero={currentMainInvestmentValuationWRTZero:F1}");
                                                        break;
                                                    }

                                                    // Determine nextTransactionBuyLevelCrit1 for use in bypass algorithm
                                                    if (j + 1 < buyCriteria.Count)
                                                    {
                                                        nextTransactionBuyLevelCrit1 = buyCriteria[j + 1][0];
                                                    }
                                                    else
                                                    {
                                                        nextTransactionBuyLevelCrit1 = buyCriteria[j][0] - 5.0;
                                                    }

                                                    // Boolean Checks
                                                    missedBuyOrderFound = false;
                                                    if (!settings.FindAndIncludeMissedOrdersFlag)
                                                    {
                                                        Bb = (currentMainInvestmentValuationWRTBuyThreshold <= currentTransactionBuyLevelCrit1) && (currentTransactionBuyLevelCrit1 < lastMainInvestmentValuationWRTBuyThreshold);
                                                    }
                                                    else if (settings.FindAndIncludeMissedOrdersFlag)
                                                    {
                                                        Bb1 = (currentMainInvestmentValuationWRTBuyThreshold <= currentTransactionBuyLevelCrit1) && (currentTransactionBuyLevelCrit1 < lastMainInvestmentValuationWRTBuyThreshold);

                                                        deltaMainInvestmentWRTBuyThreshold = currentMainInvestmentValuationWRTBuyThreshold - lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution;

                                                        if (j == 0)
                                                        {
                                                            Bb = Bb1;
                                                            buyCriteria_jm1 = buyCriteria[j][0];
                                                        }
                                                        else
                                                        {
                                                            buyCriteria_jm1 = buyCriteria[j - 1][0];
                                                            Bb2 = lastMainInvestmentValuationWRTBuyThreshold < buyCriteria[j - 1][0];
                                                            Bb3 = currentMainInvestmentValuationWRTBuyThreshold > currentTransactionBuyLevelCrit1;
                                                            Bb4 = lastMainInvestmentValuationWRTBuyThreshold > currentMainInvestmentValuationWRTBuyThreshold;

                                                            deltaBuyCriteria = buyCriteria[j][0] - buyCriteria[j - 1][0];
                                                            Bb5 = deltaMainInvestmentWRTBuyThreshold <= deltaBuyCriteria;

                                                            // Combine conditions to determine if buy criteria are met
                                                            Bb = (Bb1 || (Bb2 && Bb3 && Bb4)) && Bb5;
                                                            missedBuyOrderFound = (Bb2 && Bb3 && Bb4 && Bb5);

                                                            // Debug output for missed buy order
                                                            if (missedBuyOrderFound && settings.VerboseBuyOrderFlag)
                                                            {
                                                                statusUpdater.UpdateStatus($"Missed Buy order found. LastCrit1={buyCriteria[j - 1][0]:F1}, LastMIValtn={lastMainInvestmentValuationWRTBuyThreshold:F1}, CurMIValtn={currentMainInvestmentValuationWRTBuyThreshold:F1}, CurCrit1={currentTransactionBuyLevelCrit1:F1}, Day={day}, Date={date[day]}, DateNum={dateNum}, j={j}");
                                                            }
                                                        }
                                                    }

                                                    // Checks whether the level of the current transaction is less than the level of the last transaction
                                                    Eb = currentTransactionBuyLevelCrit1 <= lastTransactionBuyLevelCrit1;

                                                    // Calculate days since the last buy order for this criterion
                                                    daysSinceLastBuyOrderForThisCrit3 = day - dayOfLastBuyOrderForThisCrit3;
                                                    Fb = daysSinceLastBuyOrderForThisCrit3 >= settings.CriteriaDaysSinceLastBuyTransactionAtSameLevelDefault;

                                                    // Check if minimum cash criteria are met
                                                    Gb = Math.Max(cashAvailableForBuy, potentialBuyingPower) > 0.0 || buyByPassFlagLegacy >= 1;
                                                    if (Math.Max(cashAvailableForBuy, potentialBuyingPower) <= 0.0 && buyByPassFlagLegacy >= 1)
                                                    {
                                                        // Alert for insufficient funds during a multiple buy transaction
                                                        deltaShares = deltaSharesLast;
                                                        deltaSharesComplimentaryInvestment = deltaSharesComplimentaryInvestmentLast;
                                                        deltaCash = deltaCashLast;
                                                        deltaFundsComplimentaryInvestment = deltaFundsComplimentaryInvestmentLast;
                                                        actualTransactionBuyExecutionPctCrit2 = actualTransactionBuyExecutionPctCrit2Last;

                                                        statusUpdater.UpdateStatus($"ALERT: Ran out of AVAILABLE funds during a multiple Buy transaction. Last buy order was closed successfully via bypass. Day={day}, Date={date[day]}, DateNum={dateNum}, BuyByPassFlag={buyByPassFlag}, BuyByPassFlagLegacy={buyByPassFlagLegacy}, BuyOrderTrigger={buyOrderTrigger}, SP={sellProfile}, BP={buyProfile}, ST={strategy}, j={j}, Bb={Bb}, Eb={Eb}, Fb={Fb}, Gb={Gb}, Hb={Hb}");
                                                        break;
                                                    }

                                                    // Check whether currentTransactionBuyExecutionPctCrit2 is greater than zero
                                                    Hb = currentTransactionBuyExecutionPctCrit2 > 0.0 || buyByPassFlagLegacy >= 1;

                                                    // Technical analysis checks
                                                    Kb = relativeStrengthIndex <= settings.RelativeStrengthIndexBuyCrit;
                                                    Lb = BBRatioStandardDeviations <= settings.BBRatioSTDDevBuyCrit;

                                                    // Most Recent High Or Low Of Consequence Check
                                                    Mb = mostRecentHighOrLowOfConsequence.LevelType == "High" && mostRecentHighOrLowOfConsequence.GainOrLossSinceLastHighOrLowOfConsequence <= -5.0;

                                                    CritIb = (Bb && Eb && Fb && Gb && Hb);
                                                    CritIIb = (Bb && Eb && Fb && Gb && Hb && Kb && Lb);
                                                    CritIIIb = (Bb && Eb && Fb && Gb && Hb && Mb);

                                                    // Determine if buy order should be executed
                                                    if (settings.UseTechnicalAnalysisCriteriaBuySideFlag)
                                                    {
                                                        //buyOrderTrigger = (Bb && Eb && Fb && Gb && Hb && Kb && Lb) || (strategy == 20 && cashInfusionFlag == 1);
                                                        buyOrderTrigger = CritIIb || CritIIIb || (strategy == 20 && cashInfusionFlag == 1);
                                                    }
                                                    else
                                                    {
                                                        buyOrderTrigger = (Bb && Eb && Fb && Gb && Hb) || (strategy == 20 && cashInfusionFlag == 1);
                                                    }

                                                    // Check if market dropped so fast it bypassed a legitimate buy order
                                                    buyByPassFlag = Bb && nextTransactionBuyLevelCrit1 > currentMainInvestmentValuationWRTBuyThreshold;

                                                    // Record violations if a legitimate buy order was ignored due to lack of funds
                                                    if (settings.UseTechnicalAnalysisCriteriaBuySideFlag)
                                                    {
                                                        //buyOrderExcludeGb = Bb && Eb && Fb && Hb && Kb && Lb;
                                                        buyOrderExcludeGb = CritIIb || CritIIIb;
                                                    }
                                                    else
                                                    {
                                                        buyOrderExcludeGb = Bb && Eb && Fb && Hb;
                                                    }

                                                    if (buyOrderExcludeGb && !Gb)
                                                    {
                                                        violationsMinCashCount++;
                                                        if (settings.VerboseCashBalanceFlag)
                                                        {
                                                            statusUpdater.UpdateStatus($"MSG BO1: DateNum={dateNum}. A potential buy order was skipped due to insufficient funds. Violation recorded. j={j}, Violations={violationsMinCashCount}, Cash={cash[day]:F2}, CashAvailable={cashAvailableForBuy:F2}, PotentialBuyingPower={potentialBuyingPower:F2}");
                                                        }
                                                    }

                                                    // TODO: This routine was not working.  Need to fix
                                                    //if (settings.EliminateSimilarBuysAtSameMainInvestmentValueFlag == 1 && buyOrderTrigger)
                                                    //{
                                                    //    double lastBuyDay = -1.0;
                                                    //    double buyFound = 0.0;

                                                    //    for (lastBuyDay = day - 1; lastBuyDay >= day - settings.CriteriaDaysSinceLastBuyTransactionAtSameMainInvestmentValue; lastBuyDay--)
                                                    //    {
                                                    //        if (lastBuyDay <= 0)
                                                    //        {
                                                    //            buyFound = 0.0;
                                                    //            break;
                                                    //        }
                                                    //        else if (aTransactions.Count > (int)lastBuyDay && aTransactions[(int)lastBuyDay][3] == 1.0)
                                                    //        {
                                                    //            buyFound = 1.0;
                                                    //            break;
                                                    //        }
                                                    //    }

                                                    //    if (buyFound == 1.0)
                                                    //    {
                                                    //        double tolerance = mainInvestmentClosePrice[startingMarketDayThisRun + day - 1] * settings.CriteriaBuyMainInvestmentDollarDifferenceTolerancePct / 100.0;
                                                    //        double priceDifference = Math.Abs(mainInvestmentClosePrice[startingMarketDayThisRun + day - 1] - aTransactions[(int)lastBuyDay][2]);

                                                    //        if (priceDifference <= tolerance)
                                                    //        {
                                                    //            buyOrderTrigger = false;
                                                    //            aTransactions[day][11] = 1; // Buy skipped flag
                                                    //        }
                                                    //        else
                                                    //        {
                                                    //            aTransactions[day][11] = 0; // Buy not skipped
                                                    //        }
                                                    //    }
                                                    //    aTransactions[day][12] = buyByPassFlagLegacy;

                                                    //    if (settings.VerboseCashBalanceFlag == 1 && !buyOrderTrigger)
                                                    //    {
                                                    //        statusUpdater.UpdateStatus($"!MSG BO1a: Day={day}. Buy order skipped due to eliminateSimilarBuysAtSameMainInvestmentValueFlag.");
                                                    //    }
                                                    //}

                                                    // Handle bypass issues in a multiple buy transaction
                                                    if (buyByPassFlagLegacy >= 1 && !buyOrderTrigger)
                                                    {
                                                        deltaShares = deltaSharesLast;
                                                        deltaSharesComplimentaryInvestment = deltaSharesComplimentaryInvestmentLast;
                                                        deltaCash = deltaCashLast;
                                                        deltaFundsComplimentaryInvestment = deltaFundsComplimentaryInvestmentLast;
                                                        actualTransactionBuyExecutionPctCrit2 = actualTransactionBuyExecutionPctCrit2Last;

                                                        statusUpdater.UpdateStatus($"ALERT: Multiple Buy transaction encountered bypass issue. Day={day}, Date={date[day]}, DateNum={dateNum}, BuyByPassFlag={buyByPassFlag}, BuyByPassFlagLegacy={buyByPassFlagLegacy}, BuyOrderTrigger={buyOrderTrigger}, BP={buyProfile}, SP={sellProfile}, ST={strategy:F1}, j={j}, Bb={Bb}, Eb={Eb}, Fb={Fb}, Gb={Gb}, Hb={Hb}");
                                                        break;
                                                    }

                                                    // Debugging details
                                                    if (settings.WriteToBuyLoopDetailsCSVFileFlag && dateNum >= settings.DateNumStartDebug && dateNum <= settings.DateNumEndDebug)
                                                    {
                                                        string debugDetails = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11:F4}, {12:F4}, {13:F4}, {14:F4}, {15:F4}, {16:F4}, {17:F4}, {18:F4}, {19}, {20}, {21}, {22:F4}, {23:F4}, {24:F4}, {25:F4}, {26:F4}, {27:F4}, {28:F4}, {29:F4}, {30:F4}, {31:F4}, {32:F4}, {33:F4}, {34:F4}, {35:F4}, {36:F4}", 1, dateNum, j, Bb, Eb, Fb, Gb, Hb, buyOrderTrigger ? 1 : 0, buyByPassFlag, buyByPassFlagLegacy, currentMainInvestmentValuationWRTBuyThreshold, Math.Round(currentMainInvestmentValuationWRTBuyThreshold), currentTransactionBuyLevelCrit1, lastMainInvestmentValuationWRTBuyThreshold, lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, lastTransactionBuyLevelCrit1, nextTransactionBuyLevelCrit1, currentTransactionBuyExecutionPctCrit2, dayOfLastBuyOrderForThisCrit3, daysSinceLastBuyOrderForThisCrit3, sellFlag, cash[day], cashBalanceMinCriteria, cashAvailableForBuy, potentialBuyingPower, fundsNeededToCompleteBuyTransaction, deltaCash, sharePriceComplementaryInvestment, deltaFundsComplimentaryInvestment, deltaSharesComplimentaryInvestment, sharesComplementaryInvestment, marketValueComplimentaryInvestment, sharePriceMainInvestment, deltaShares, sharesMainInvestment, marketValueMainInvestmentShares);

                                                        //csvBuyLoopDetailsFileID.WriteLine(debugDetails);
                                                    }

                                                    //======================================================================================
                                                    //Execute Buy Order
                                                    //======================================================================================
                                                    // Execute Buy Order
                                                    if (buyOrderTrigger) // If buyOrderTrigger is true, execute the buy order
                                                    {
                                                        statusUpdater.UpdateStatus($"Date: {date[day]}, LevelType: {mostRecentHighOrLowOfConsequence.LevelType},Gain/Loss Since: {mostRecentHighOrLowOfConsequence.GainOrLossSinceLastHighOrLowOfConsequence:F2}%, buyOrderTrigger: {buyOrderTrigger}, CritIIb: {CritIIb}, CritIIIb: {CritIIIb}, j: {j}");

                                                        double adjustedCurrentTransactionBuyExecutionPctCrit2;
                                                        currentTransactionBuyExecutionPctCrit2Orig = currentTransactionBuyExecutionPctCrit2; // Save original value for comparison

                                                        if (settings.FindAndIncludeMissedOrdersFlag && missedBuyOrderFound)
                                                        {
                                                            // Adjust currentTransactionBuyExecutionPctCrit2 based on currentMainInvestmentValuationWRTBuyThreshold
                                                            if (j == 0)
                                                            {
                                                                adjustedCurrentTransactionBuyExecutionPctCrit2 = currentTransactionBuyExecutionPctCrit2;
                                                            }
                                                            else
                                                            {
                                                                double x = currentMainInvestmentValuationWRTBuyThreshold;
                                                                double x1 = buyCriteria[j - 1][0];
                                                                double x2 = currentTransactionBuyLevelCrit1;
                                                                double y1 = buyCriteria[j - 1][1];
                                                                double y2 = currentTransactionBuyExecutionPctCrit2;
                                                                adjustedCurrentTransactionBuyExecutionPctCrit2 = y1 + ((x - x1) / (x2 - x1)) * (y2 - y1);

                                                                if (adjustedCurrentTransactionBuyExecutionPctCrit2 < 0)
                                                                {
                                                                    statusUpdater.UpdateStatus($"INFO: adjustedCurrentTransactionBuyExecutionPctCrit2={adjustedCurrentTransactionBuyExecutionPctCrit2:F2} is less than 0. Day={day}, DateNum={dateNum}, X1={x1:F2}, X={x:F2}, X2={x2:F2}, Y1={y1:F2}, Y={adjustedCurrentTransactionBuyExecutionPctCrit2:F2}, Y2={y2:F2}");
                                                                }
                                                            }

                                                            currentTransactionBuyExecutionPctCrit2 = adjustedCurrentTransactionBuyExecutionPctCrit2;
                                                        }

                                                        if (cashAvailableForBuy > 0.0 || potentialBuyingPower > 0.0) // Ensure sufficient funds are available
                                                        {
                                                            confirmMatchingShareBuy = 0;

                                                            // Enable "buy the dip" strategy
                                                            if (settings.EnableMatchingShareBuyFlag && currentTransactionBuyLevelCrit1 == buyCriteria[0][0] && sellFlag == 1 && BTCBuyThresholdAdjustmentTracker == 1 && (sharePriceMainInvestment - actualMainInvestmentSharePriceAtLastSellTransaction) / actualMainInvestmentSharePriceAtLastSellTransaction * 100.0 <= settings.MatchingShareBuyMISharePricePctDifferential)
                                                            {
                                                                double fundsToPurchaseSameNumberShares = -1.0 * lastTransactionSellExecutionDeltaShares * sharePriceMainInvestment * settings.MatchingShareExecutionPctCrit2AdjustFactor;
                                                                currentTransactionBuyExecutionPctCrit2 = Math.Min(settings.MaxAllowedBuyExecutionPctCrit2ForMatchingShareBuy, (fundsToPurchaseSameNumberShares / Math.Max(cashAvailableForBuy, potentialBuyingPower) * 100.0));
                                                                cntEnableMatchingBuy++;
                                                                confirmMatchingShareBuy = 1;

                                                                if (settings.VerboseTransactionsFlag && fileSettings.RunCalculation == 1)
                                                                {
                                                                    statusUpdater.UpdateStatus($"MATCHING SHARE BUY DETAILS: DateNum={dateNum}, MrktLevelWRTZero={currentMainInvestmentValuationWRTZero:F1}, PctSharePriceDelta={(sharePriceMainInvestment - actualMainInvestmentSharePriceAtLastSellTransaction) / actualMainInvestmentSharePriceAtLastSellTransaction * 100:F2}, LastSellExecutionPct={lastTransactionSellExecutionPctCrit2:F2}, NewBuyExecutionPct={currentTransactionBuyExecutionPctCrit2:F2}");
                                                                }
                                                            }

                                                            // Transaction calculation
                                                            fundsNeededToCompleteBuyTransaction = currentTransactionBuyExecutionPctCrit2 / 100.0 * Math.Max(cashAvailableForBuy, potentialBuyingPower);

                                                            // Limit transaction amount if applicable
                                                            if (settings.LimitTransactionAmountFlag == 1 && fundsNeededToCompleteBuyTransaction > maxAllowedTransactionAmountCurrentYr)
                                                            {
                                                                fundsNeededToCompleteBuyTransaction = maxAllowedTransactionAmountCurrentYr;
                                                                currentTransactionBuyExecutionPctCrit2 = fundsNeededToCompleteBuyTransaction / Math.Max(cashAvailableForBuy, potentialBuyingPower) * 100.0;
                                                            }
                                                            else if (settings.LimitTransactionAmountFlag == 2 && fundsNeededToCompleteBuyTransaction > maxAllowedTransactionAmountCurrentYr * currentTransactionBuyExecutionPctCrit2 / 100.0)
                                                            {
                                                                double preModFundsNeededToCompleteBuyTransaction = fundsNeededToCompleteBuyTransaction;
                                                                fundsNeededToCompleteBuyTransaction = maxAllowedTransactionAmountCurrentYr * currentTransactionBuyExecutionPctCrit2 / 100.0;
                                                                currentTransactionBuyExecutionPctCrit2 = fundsNeededToCompleteBuyTransaction / Math.Max(cashAvailableForBuy, potentialBuyingPower) * 100.0;

                                                                if (settings.VerboseTransactionsFlag && fileSettings.RunCalculation == 1)
                                                                {
                                                                    statusUpdater.UpdateStatus($"BUY ALERT-LIMIT TRANSACTION=2: Day={day}, DateNum={dateNum}, MaxAllowedTransactionAmount={maxAllowedTransactionAmountCurrentYr:F0}, NewBuyExecutionPct={currentTransactionBuyExecutionPctCrit2:F1}");
                                                                }
                                                            }

                                                            if (settings.SuperChargeFlag)
                                                            {
                                                                cashRatio = Math.Min(cashBalanceMinCriteria / cash[day], 10.0);
                                                                if (cashRatio >= settings.CashRatioThreshold)
                                                                {
                                                                    double preModFundsNeededToCompleteBuyTransaction = fundsNeededToCompleteBuyTransaction;
                                                                    double SCFactor = 1.0 - (settings.SuperchargeFundsDecreasePct / 100.0 * cashRatio);
                                                                    fundsNeededToCompleteBuyTransaction *= SCFactor;
                                                                    currentTransactionBuyExecutionPctCrit2 = fundsNeededToCompleteBuyTransaction / Math.Max(cashAvailableForBuy, potentialBuyingPower) * 100.0;

                                                                    if (settings.VerboseTransactionsFlag && fileSettings.RunCalculation == 1)
                                                                    {
                                                                        statusUpdater.UpdateStatus($"BUY ALERT-SUPERCHARGE TRANSACTION. Day={day}, DateNum={dateNum}, CashBalanceMinCriteria={cashBalanceMinCriteria:F0}, TotalCash={cash[day]:F0}, CashRatio={cashRatio:F2}, SCFactor={SCFactor:F2}, FundsNeeded={fundsNeededToCompleteBuyTransaction:F2}");
                                                                    }
                                                                }
                                                            }

                                                            deltaCash = Math.Min(fundsNeededToCompleteBuyTransaction, cashAvailableForBuy);

                                                            if (settings.ComplementaryInvestmentFlag == 1)
                                                            {
                                                                deltaFundsComplimentaryInvestment = fundsNeededToCompleteBuyTransaction - deltaCash;
                                                                deltaSharesComplimentaryInvestment = deltaFundsComplimentaryInvestment / sharePriceComplementaryInvestment;
                                                                sharesComplementaryInvestment -= deltaSharesComplimentaryInvestment;
                                                                marketValueComplimentaryInvestment = sharesComplementaryInvestment * sharePriceComplementaryInvestment;
                                                            }
                                                            else
                                                            {
                                                                deltaFundsComplimentaryInvestment = 0.0;
                                                                deltaSharesComplimentaryInvestment = 0.0;
                                                            }

                                                            deltaShares = (deltaCash + deltaFundsComplimentaryInvestment) / sharePriceMainInvestment;
                                                            sharesMainInvestment += deltaShares;
                                                            marketValueMainInvestmentShares = sharesMainInvestment * sharePriceMainInvestment;

                                                            // Update transaction details
                                                            deltaCash = -deltaCash;
                                                            deltaFundsComplimentaryInvestment = -deltaFundsComplimentaryInvestment;
                                                            deltaSharesComplimentaryInvestment = -deltaSharesComplimentaryInvestment;

                                                            actualTransactionBuyLevelCrit1 = currentTransactionBuyLevelCrit1;
                                                            actualTransactionBuyExecutionPctCrit2 = currentTransactionBuyExecutionPctCrit2;
                                                            actualDaysSinceLastBuyOrderForThisCrit3 = daysSinceLastBuyOrderForThisCrit3;
                                                            buyCriteria[j][2] = day;
                                                            dayOfLastBuy = day;
                                                            actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction = currentMainInvestmentValuationWRTBuyThreshold;
                                                            actualMainInvestmentSharePriceAtLastBuyTransaction = sharePriceMainInvestment;

                                                            if (strategy == 20)
                                                            {
                                                                nextBuyLevelCrit1 = 0.0;
                                                            }
                                                            else
                                                            {
                                                                nextBuyLevelCrit1 = j + 1 < buyCriteria.Count ? buyCriteria[j + 1][0] : buyCriteria[j][0];
                                                                if (nextBuyLevelCrit1 == -110.0)
                                                                {
                                                                    nextBuyLevelCrit1 = buyCriteria[j][0];
                                                                }
                                                            }

                                                            buyFlag = 1;
                                                            sellFlag = 0;

                                                            if (settings.VerboseBuyOrderFlag)
                                                            {
                                                                statusUpdater.UpdateStatus($"BUY ORDER EXECUTED: Day={day}, BuyExecutionLevel={currentTransactionBuyLevelCrit1:F2}, ExecutionPct={currentTransactionBuyExecutionPctCrit2:F2}, Cash={cash[day]:F2}, DeltaCash={deltaCash:F2}, DeltaShares={deltaShares:F2}");
                                                            }
                                                        }
                                                        else if (cashAvailableForBuy <= 0 || potentialBuyingPower <= 0)
                                                        {
                                                            fundsNeededToCompleteBuyTransaction = 0.0;
                                                            deltaShares = 0.0;
                                                            deltaSharesComplimentaryInvestment = 0.0;
                                                            deltaCash = 0.0;
                                                            deltaFundsComplimentaryInvestment = 0.0;
                                                            actualTransactionBuyLevelCrit1 = currentTransactionBuyLevelCrit1;
                                                            actualTransactionBuyExecutionPctCrit2 = 0.0;
                                                            violationsMinCashCount++;

                                                            statusUpdater.UpdateStatus($"MSG BO2: Day={day}, DateNum={dateNum}, A potential buy order was skipped due to min cash balance issue. Violations={violationsMinCashCount}, CashAvailable={cashAvailableForBuy:F2}");
                                                        }

                                                        if (!buyByPassFlag)
                                                        {
                                                            if (buyByPassFlagLegacy >= 1)
                                                            {
                                                                // Update cash balance (decrease)
                                                                cash[day] = Math.Round(cash[day] + deltaCash, 8);
                                                                if (cash[day] < 0.0)
                                                                {
                                                                    statusUpdater.UpdateStatus($"WARNING: Cash account value is less than zero. Setting Cash=0: Day={day} Cash={cash[day]:F0}.");
                                                                    cash[day] = 0.0;
                                                                }

                                                                deltaShares += deltaSharesLast;
                                                                deltaSharesComplimentaryInvestment += deltaSharesComplimentaryInvestmentLast;
                                                                deltaCash += deltaCashLast;
                                                                deltaFundsComplimentaryInvestment += deltaFundsComplimentaryInvestmentLast;
                                                                actualTransactionBuyLevelCrit1 = currentTransactionBuyLevelCrit1;
                                                                actualTransactionBuyExecutionPctCrit2 += actualTransactionBuyExecutionPctCrit2Last;

                                                                if (settings.WriteToBuyLoopDetailsCSVFileFlag && dateNum >= settings.DateNumStartDebug && dateNum <= settings.DateNumEndDebug)
                                                                {
                                                                    string debugDetails = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11:F4}, {12:F4}, {13:F4}, {14:F4}, {15:F4}, {16:F4}, {17:F4}, {18:F4}, {19}, {20}, {21}, {22:F4}, {23:F4}, {24:F4}, {25:F4}, {26:F4}, {27:F4}, {28:F4}, {29:F4}, {30:F4}, {31:F4}, {32:F4}, {33:F4}, {34:F4}, {35:F4}, {36:F4}", 3, dateNum, j, Bb ? 1 : 0, Eb ? 1 : 0, Fb ? 1 : 0, Gb ? 1 : 0, Hb ? 1 : 0, buyOrderTrigger ? 1 : 0, buyByPassFlag, buyByPassFlagLegacy, currentMainInvestmentValuationWRTBuyThreshold, Math.Round(currentMainInvestmentValuationWRTBuyThreshold, 4), currentTransactionBuyLevelCrit1, lastMainInvestmentValuationWRTBuyThreshold, lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, lastTransactionBuyLevelCrit1, nextTransactionBuyLevelCrit1, currentTransactionBuyExecutionPctCrit2, dayOfLastBuyOrderForThisCrit3, daysSinceLastBuyOrderForThisCrit3, sellFlag, cash[day], cashBalanceMinCriteria, cashAvailableForBuy, potentialBuyingPower, fundsNeededToCompleteBuyTransaction, deltaCash, sharePriceComplementaryInvestment, deltaFundsComplimentaryInvestment, deltaSharesComplimentaryInvestment, sharesComplementaryInvestment, marketValueComplimentaryInvestment, sharePriceMainInvestment, deltaShares, sharesMainInvestment, marketValueMainInvestmentShares);
                                                                    // Log debugDetails to CSV file (if implemented)
                                                                }
                                                            }
                                                            else if (buyByPassFlagLegacy == 0)
                                                            {
                                                                // Update cash balance (decrease)
                                                                cash[day] = Math.Round(cash[day] + deltaCash, 8);
                                                                if (cash[day] < 0.0)
                                                                {
                                                                    statusUpdater.UpdateStatus($"WARNING: Cash account value is less than zero. Setting Cash=0: Day={day} Cash={cash[day]:F0}.");
                                                                    cash[day] = 0.0;
                                                                }
                                                            }

                                                            // Exit the buy loop
                                                            break;
                                                        }
                                                        else if (buyByPassFlag)
                                                        {
                                                            // Record data from the ByPass Buy execution
                                                            buyByPassFlagLegacy++;
                                                            deltaSharesLast += deltaShares;
                                                            deltaSharesComplimentaryInvestmentLast += deltaSharesComplimentaryInvestment;
                                                            deltaCashLast += deltaCash;
                                                            deltaFundsComplimentaryInvestmentLast += deltaFundsComplimentaryInvestment;
                                                            actualTransactionBuyExecutionPctCrit2Last += actualTransactionBuyExecutionPctCrit2;
                                                            lastTransactionBuyLevelCrit1 = actualTransactionBuyLevelCrit1;
                                                            buyByPassCount++;

                                                            if (settings.VerboseFlag)
                                                            {
                                                                statusUpdater.UpdateStatus($"THE BUYBYPASSFLAG HAS BEEN ACTIVATED: Day={day} ByPassCount={buyByPassCount}.");
                                                            }

                                                            if (settings.WriteToBuyLoopDetailsCSVFileFlag && dateNum >= settings.DateNumStartDebug && dateNum <= settings.DateNumEndDebug)
                                                            {
                                                                string debugDetails = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11:F4}, {12:F4}, {13:F4}, {14:F4}, {15:F4}, {16:F4}, {17:F4}, {18:F4}, {19}, {20}, {21}, {22:F4}, {23:F4}, {24:F4}, {25:F4}, {26:F4}, {27:F4}, {28:F4}, {29:F4}, {30:F4}, {31:F4}, {32:F4}, {33:F4}, {34:F4}, {35:F4}, {36:F4}", 4, dateNum, j, Bb ? 1 : 0, Eb ? 1 : 0, Fb ? 1 : 0, Gb ? 1 : 0, Hb ? 1 : 0, buyOrderTrigger ? 1 : 0, buyByPassFlag, buyByPassFlagLegacy, currentMainInvestmentValuationWRTBuyThreshold, Math.Round(currentMainInvestmentValuationWRTBuyThreshold, 4), currentTransactionBuyLevelCrit1, lastMainInvestmentValuationWRTBuyThreshold, lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, lastTransactionBuyLevelCrit1, nextTransactionBuyLevelCrit1, currentTransactionBuyExecutionPctCrit2, dayOfLastBuyOrderForThisCrit3, daysSinceLastBuyOrderForThisCrit3, sellFlag, cash[day], cashBalanceMinCriteria, cashAvailableForBuy, potentialBuyingPower, fundsNeededToCompleteBuyTransaction, deltaCash, sharePriceComplementaryInvestment, deltaFundsComplimentaryInvestment, deltaSharesComplimentaryInvestment, sharesComplementaryInvestment, marketValueComplimentaryInvestment, sharePriceMainInvestment, deltaShares, sharesMainInvestment, marketValueMainInvestmentShares);
                                                                // Log debugDetails to CSV file (if implemented)
                                                            }

                                                            // Update cash balance (decrease)
                                                            cash[day] = Math.Round(cash[day] + deltaCash, 8);
                                                            if (cash[day] < 0.0)
                                                            {
                                                                statusUpdater.UpdateStatus($"WARNING: Cash account value is less than zero. Setting Cash=0: Day={day} Cash={cash[day]:F0}.");
                                                                cash[day] = 0.0;
                                                            }

                                                            cashAvailableForBuy = Math.Round(cash[day] - cashBalanceMinCriteria, 8);
                                                            potentialBuyingPower = Math.Round(cashAvailableForBuy + marketValueComplimentaryInvestment, 8);

                                                            // Continue to next buy level
                                                            continue;
                                                        }
                                                    }
                                                }
                                            }

                                            // If no buy order was triggered, need to reset variables below
                                            if (!buyOrderTrigger)
                                            {
                                                actualTransactionBuyLevelCrit1 = 0.0;
                                                actualTransactionBuyExecutionPctCrit2 = 0.0; // No order execution
                                                actualDaysSinceLastBuyOrderForThisCrit3 = 0; // No order execution
                                            }

                                            // Write results to ResultsBuyOrder array
                                            resultsBuyOrder.Add(new double[]
                                            {
                                                dateNum, dateNum, buyResetType1Flag, buyResetType2Flag, lastMainInvestmentValuationWRTZero, currentMainInvestmentValuationWRTZero, hypotheticalMainInvestmentPriceAtBuyThreshold, currentMainInvestmentRegressionValue, buyThreshold, movingAverageRateOfChangeWRTZero, settings.BuyRateOfChangeValue, A0b ? 1 : 0, lastMainInvestmentValuationWRTBuyThreshold, currentMainInvestmentValuationWRTBuyThreshold, lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, B0b ? 1 : 0, buyCriteria.Max(criteria => criteria[0]), // Assuming buyCriteria is List<double[]>
                                                C0b ? 1 : 0, actualMainInvestmentSharePriceAtLastSellTransaction, priceDecreaseFromLastSellOrder, settings.MinimumPricePercentageDropFromPreviousSellToEnableBuy, D0b ? 1 : 0, strategy, H0b ? 1 : 0, cashInfusionFlag, I0b ? 1 : 0, K0b ? 1 : 0, L0b ? 1 : 0, buyOrderTriggerPrelim ? 1 : 0, sellOrderTriggerPrelim ? 1 : 0, currentTransactionBuyLevelCrit1, currentMainInvestmentValuationWRTBuyThreshold, lastMainInvestmentValuationWRTBuyThreshold, Bb1 ? 1 : 0, buyCriteria_jm1, Bb2 ? 1 : 0, Bb3 ? 1 : 0, Bb4 ? 1 : 0, currentMainInvestmentValuationWRTBuyThreshold, lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, deltaMainInvestmentWRTBuyThreshold, Bb5 ? 1 : 0, missedBuyOrderFound ? 1 : 0, Bb ? 1 : 0, currentTransactionBuyLevelCrit1, lastTransactionBuyLevelCrit1, Eb ? 1 : 0, day, dayOfLastBuyOrderForThisCrit3, daysSinceLastBuyOrderForThisCrit3, criteriaDaysSinceLastBuyTransactionAtSameLevel, Fb ? 1 : 0, cashAvailableForBuy, potentialBuyingPower, buyByPassFlagLegacy, Gb ? 1 : 0, currentTransactionBuyExecutionPctCrit2, buyByPassFlagLegacy, Hb ? 1 : 0, relativeStrengthIndex, settings.RelativeStrengthIndexBuyCrit, Kb ? 1 : 0, BBRatioStandardDeviations, settings.BBRatioSTDDevBuyCrit, Lb ? 1 : 0, Bb ? 1 : 0, nextTransactionBuyLevelCrit1, currentMainInvestmentValuationWRTBuyThreshold, buyByPassFlag ? 1 : 0, violationsMinCashCount, buyOrderTrigger ? 1 : 0, cashAvailableForBuy, potentialBuyingPower, settings.FindAndIncludeMissedOrdersFlag ? 1 : 0, missedBuyOrderFound ? 1 : 0, confirmMatchingShareBuy, settings.EnableMatchingShareBuyFlag ? 1 : 0, confirmMatchingShareBuy, settings.LimitTransactionAmountFlag, maxAllowedTransactionAmountCurrentYr, settings.SuperChargeFlag ? 1 : 0, settings.CashRatioThreshold, cashRatio, cashBalanceMinCriteria, settings.SuperchargeFundsDecreasePct, currentTransactionBuyExecutionPctCrit2Orig, currentTransactionBuyExecutionPctCrit2, settings.ComplementaryInvestmentFlag, deltaFundsComplimentaryInvestment, deltaSharesComplimentaryInvestment, fundsNeededToCompleteBuyTransaction, deltaShares, deltaCash, actualTransactionBuyLevelCrit1, actualTransactionBuyExecutionPctCrit2, actualDaysSinceLastBuyOrderForThisCrit3, actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction, actualMainInvestmentSharePriceAtLastBuyTransaction, buyByPassFlagLegacy, buyByPassCount, buyFlag, nextBuyLevelCrit1
                                            });

                                            // Close-out Sell and Buy Orders
                                            if (settings.VerboseSellOrderFlag)
                                            {
                                                statusUpdater.UpdateStatus($"MSG S9: Day={day}, date={date[day]}, sellOrderTrigger={sellOrderTrigger}, sellByPassCount={sellByPassCount}, CumSellXtionPct={actualTransactionSellExecutionPctCrit2:F2}, deltaCash={deltaCash:F2}, deltaShares={deltaShares:F2}, deltaFundsCI={deltaFundsComplimentaryInvestment:F2}, deltaSharesCI={deltaSharesComplimentaryInvestment:F2}");
                                            }

                                            if (fileSettings.RunCalculation == 1) // Write out array for debugging the missing orders routine
                                            {
                                                // Declare and initialize debug arrays
                                                List<double[]> debugArrayActualSellLevel = new List<double[]>();
                                                debugArrayActualSellLevel.Add(new[] { dateNum, missedSellOrderFound ? 1 : 0, sellOrderTriggerPrelim ? 1 : 0, sellOrderTrigger ? 1 : 0, sellThreshold, sharePriceMainInvestment, hypotheticalMainInvestmentPriceAtSellThreshold, currentMainInvestmentValuationWRTZero, lastMainInvestmentValuationWRTSellThreshold, currentTransactionSellLevelCrit1, currentMainInvestmentValuationWRTSellThreshold, actualMainInvestmentValuationWRTSellThresholdAtLastTransaction, lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, deltaMainInvestmentWRTSellThreshold, deltaSellCriteria, lastTransactionSellLevelCrit1, lastTransactionSellExecutionPctCrit2, marketTrend });

                                                List<double[]> debugArrayActualBuyLevel = new List<double[]>();
                                                debugArrayActualBuyLevel.Add(new[] { dateNum, missedBuyOrderFound ? 1 : 0, buyOrderTriggerPrelim ? 1 : 0, buyOrderTrigger ? 1 : 0, buyThreshold, sharePriceMainInvestment, hypotheticalMainInvestmentPriceAtBuyThreshold, currentMainInvestmentValuationWRTZero, currentMainInvestmentValuationWRTBuyThreshold, currentTransactionBuyLevelCrit1, lastMainInvestmentValuationWRTBuyThreshold, actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction, lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, deltaMainInvestmentWRTBuyThreshold, deltaBuyCriteria, lastTransactionBuyLevelCrit1, lastTransactionBuyExecutionPctCrit2, marketTrend });
                                            }

                                            if (!sellOrderTrigger && !buyOrderTrigger)
                                            {
                                                deltaCash = 0.0;
                                                deltaShares = 0.0;
                                                deltaFundsComplimentaryInvestment = 0.0;
                                                deltaSharesComplimentaryInvestment = 0.0;
                                            }

                                            // Related to STC process. This needs to be located here (after the Buy Order routine) so that a buyFlag=1 gets recognized in the case that in occurs on the on the same day as the newly generated STCProcessSellThresholdAdjustmentMarker.
                                            if (STCProcessSellThresholdAdjustmentMarker == 1 && buyFlag == 1)
                                            {
                                                //FUNCTION CALL: Reset Sell Criteria T1
                                                BackTestUtilities.ResetSellCriteria(ref sellCriteria, ref lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, criteriaDaysSinceLastSellTransactionAtSameLevel, day, date, dateNum, 1, out lastTransactionSellLevelCrit1, out actualMainInvestmentValuationWRTSellThresholdAtLastTransaction, out sellResetType1Flag, out sellResetType2Flag);
                                            }

                                            // Related to BTC process. This needs to be located here (Sell Order routine) so that a sellFlag=1 gets recognized in the case that in occurs on the on the same day as the newly generated BTCProcessBuyThresholdAdjustmentMarker.
                                            if (BTCProcessBuyThresholdAdjustmentMarker == 1 && sellFlag == 1)
                                            {
                                                // FUNCTION CALL: Reset Buy Criteria T1
                                                BackTestUtilities.ResetBuyCriteria(ref buyCriteria, ref lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, criteriaDaysSinceLastBuyTransactionAtSameLevel, day, date, dateNum, 1, out lastTransactionBuyLevelCrit1, out actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction, out buyResetType1Flag, out buyResetType2Flag);
                                            }

                                            if (fileSettings.RunCalculation == 1 && settings.VerboseBTAFlag && sellOrderTrigger)
                                            {
                                                statusUpdater.UpdateStatus($"EVENT-SELLORDERTRIGGER; date={date[day]}, dateNum={dateNum}, deltaCash=${deltaCash:F2}, deltaShares={deltaShares:F1}, actualTransactionSellLevelCrit1={actualTransactionSellLevelCrit1:F2}, actualTransactionSellExecutionPctCrit2={actualTransactionSellExecutionPctCrit2:F2}");
                                            }

                                            if (fileSettings.RunCalculation == 1 && settings.VerboseBTAFlag && buyOrderTrigger)
                                            {
                                                statusUpdater.UpdateStatus($"EVENT-BUYORDERTRIGGER; date={date[day]}, dateNum={dateNum}, deltaCash=${deltaCash:F2}, deltaShares={deltaShares:F1}, actualTransactionBuyLevelCrit1={actualTransactionBuyLevelCrit1:F2}, actualTransactionBuyExecutionPctCrit2={actualTransactionBuyExecutionPctCrit2:F2}");
                                            }

                                            if (STCSellThresholdAdjustmentTracker || BTCBuyThresholdAdjustmentTracker == 1)
                                            {
                                                sellOrderTriggerAdjustFlag = 0; // Skip the reset of the buy criteria (below)
                                            }
                                            else if (!STCSellThresholdAdjustmentTracker)
                                            {
                                                if (!settings.SellBuyOrderTriggerAdjustForShareCashViolationFlag && sellOrderTrigger)
                                                {
                                                    sellOrderTriggerAdjustFlag = 1; //Reset the buy criteria (below)
                                                }
                                                else if (settings.SellBuyOrderTriggerAdjustForShareCashViolationFlag)
                                                {
                                                    // This will set sellOrderTriggerAdjustFlag = 1 only if the sellOrderTrigger = 1 and there are no violations.  Otherwise it will set sellOrderTriggerAdjustFlag = 0 and skip the reset of the buy criteria (below).

                                                    if (sellOrderTrigger && violationsMinShareCount == 0)
                                                    {
                                                        sellOrderTriggerAdjustFlag = 1;
                                                    }
                                                    else if (sellOrderTrigger && violationsMinShareCount >= 1)
                                                    {
                                                        sellOrderTriggerAdjustFlag = 0;
                                                        if (settings.VerboseSellOrderFlag)
                                                        {
                                                            statusUpdater.UpdateStatus($"MSG TAS1: Day={day}, TAS={sellOrderTriggerAdjustFlag}, SellTrig={sellOrderTrigger}, NoVios={violationsMinShareCount}. A Sell violation was recorded. Ignore the sell trigger and don't reset the buy criteria.");
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                sellOrderTriggerAdjustFlag = 0;
                                            }

                                            if (sellOrderTriggerAdjustFlag == 1) // A sell order has occurred.  Reset the Buy criteria, so that buy orders will not have knowledge of the past
                                            {
                                                //FUNCTION CALL: Reset Buy Criteria T2 - When buy criteria is reset due to execution of a sell order or a sell threshold reset
                                                BackTestUtilities.ResetBuyCriteria(ref buyCriteria, ref lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, criteriaDaysSinceLastBuyTransactionAtSameLevel, day, date, dateNum, 2, out lastTransactionBuyLevelCrit1, out actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction, out buyResetType1Flag, out buyResetType2Flag);
                                            }

                                            if (BTCBuyThresholdAdjustmentTracker == 1 || STCSellThresholdAdjustmentTracker)
                                            {
                                                buyOrderTriggerAdjustFlag = 0; //Skip the reset of the sell criteria (below).
                                            }
                                            else if (BTCBuyThresholdAdjustmentTracker == 0)
                                            {
                                                if (!settings.SellBuyOrderTriggerAdjustForShareCashViolationFlag && buyOrderTrigger)
                                                {
                                                    buyOrderTriggerAdjustFlag = 1; //Reset the sell criteria (below)
                                                }
                                                else if (settings.SellBuyOrderTriggerAdjustForShareCashViolationFlag)
                                                {
                                                    // This will set buyOrderTriggerAdjustFlag = 1 only if the buyOrderTrigger = 1 and there are no violations.  Otherwise it will set buyOrderTriggerAdjustFlag = 0 and skip the reset of the sell criteria (below).
                                                    if (buyOrderTrigger && violationsMinCashCount == 0)
                                                    {
                                                        buyOrderTriggerAdjustFlag = 1;
                                                    }
                                                    else if (buyOrderTrigger && violationsMinCashCount >= 1)
                                                    {
                                                        buyOrderTriggerAdjustFlag = 0;
                                                        if (settings.VerboseCashBalanceFlag)
                                                        {
                                                            statusUpdater.UpdateStatus($"MSG 01TAB: Day={day}, TAB={buyOrderTriggerAdjustFlag}, BuyTrig={buyOrderTrigger}, NoVios={violationsMinCashCount}. A Buy violation was recorded. Ignore the buy trigger and don't reset the sell criteria.");
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                buyOrderTriggerAdjustFlag = 0;
                                            }

                                            if (buyOrderTriggerAdjustFlag == 1) // A buy order has occurred.  Reset the Sell criteria so that sell orders will not have knowledge of the past
                                            {
                                                // FUNCTION CALL: Reset Sell Criteria T2 -When sell criteria is reset due to execution of a buy order or a buy threshold reset
                                                BackTestUtilities.ResetSellCriteria(ref sellCriteria, ref lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, criteriaDaysSinceLastSellTransactionAtSameLevel, day, date, dateNum, 2, out lastTransactionSellLevelCrit1, out actualMainInvestmentValuationWRTSellThresholdAtLastTransaction, out sellResetType1Flag, out sellResetType2Flag);
                                            }

                                            // Debug output
                                            //statusUpdater.UpdateStatus($"{dateNum}, {sharePriceMainInvestment}, {sellOrderTrigger}, {buyOrderTrigger}, {actualMainInvestmentSharePriceAtLastSellTransaction:F1}, {actualMainInvestmentSharePriceAtLastBuyTransaction:F1}, {priceDecreaseFromLastSellOrder:F1}");

                                            //====================================================================================================
                                            // Update Cash Account end-of-day Value based on cashInfusionAmount and interestAmountOnCashAccount
                                            //====================================================================================================
                                            double cashAccountShortFallCheck = 0.0;
                                            double deltaSharesToCoverCashShortFall = 0.0;

                                            if (settings.BackTestApproachFlag)
                                            {
                                                if ((dateNum - (lastCashInfusionDateNum + 1)) >= 30)
                                                {
                                                    cashInfusionFlag = 1;
                                                    cashInfusionAmount = savingsCurrentYr / 12.0;
                                                    cashWithdrawalFlag = 0;
                                                    cashWithdrawalAmount = 0.0;

                                                    int dayStart = mainInvestmentCloseDateNumber.FindIndex(x => x == lastCashInfusionDateNum) + 1;
                                                    int dayEnd = mainInvestmentCloseDateNumber.FindIndex(x => x == dateNum);
                                                    int deltaDays = dayEnd - dayStart;

                                                    cashAccountBalancesLast30Days = cash.Skip(day - deltaDays).Take(deltaDays).ToList();
                                                    interestAmountOnCashAccount = (interestRateCurrentYear / 1200.0) * cashAccountBalancesLast30Days.Average();

                                                    cash[day] = Math.Round(cash[day] + cashInfusionAmount + interestAmountOnCashAccount, 8);
                                                    lastCashInfusionDateNum = dateNum;
                                                }
                                                else
                                                {
                                                    cashInfusionFlag = 0;
                                                    cashInfusionAmount = 0.0;
                                                    cashWithdrawalFlag = 0;
                                                    cashWithdrawalAmount = 0.0;
                                                    interestAmountOnCashAccount = 0.0;
                                                }
                                            }
                                            else if (!settings.BackTestApproachFlag)
                                            {
                                                if ((dateNum - (lastInterestCalcDate + 1)) >= 30)
                                                {
                                                    cashInfusionFlag = 0;
                                                    cashInfusionAmount = 0.0;
                                                    cashWithdrawalFlag = 1;
                                                    cashWithdrawalAmount = cashWithdrawalCurrentYr / 12.0;

                                                    int dayStart = mainInvestmentCloseDateNumber.FindIndex(x => x == lastInterestCalcDate) + 1;
                                                    int dayEnd = mainInvestmentCloseDateNumber.FindIndex(x => x == dateNum);
                                                    int deltaDays = dayEnd - dayStart;

                                                    cashAccountBalancesLast30Days = cash.Skip(day - deltaDays).Take(deltaDays).ToList();
                                                    interestAmountOnCashAccount = (interestRateCurrentYear / 1200.0) * cashAccountBalancesLast30Days.Average();

                                                    // Check cash account value to make sure there is enough to handle the cash withdrawal. If not, sell the necessary amount of shares.
                                                    cashAccountShortFallCheck = cash[day] - cashWithdrawalAmount + interestAmountOnCashAccount;

                                                    if (cashAccountShortFallCheck >= 0)
                                                    {
                                                        cash[day] = Math.Round(cash[day] - cashWithdrawalAmount + interestAmountOnCashAccount, 8);
                                                        deltaCash += -cashWithdrawalAmount + interestAmountOnCashAccount;
                                                    }
                                                    else //Sell the necessary amount of shares to cover the shortfall
                                                    {
                                                        deltaSharesToCoverCashShortFall = cashAccountShortFallCheck / sharePriceMainInvestment;
                                                        cash[day] = Math.Round(cash[day] - cashWithdrawalAmount - cashAccountShortFallCheck + interestAmountOnCashAccount, 8);
                                                        sharesMainInvestment += deltaSharesToCoverCashShortFall;

                                                        deltaShares += deltaSharesToCoverCashShortFall; // Calculation this so that the deltasShares parameter in the results array can updated.
                                                        deltaCash += -cashWithdrawalAmount - cashAccountShortFallCheck + interestAmountOnCashAccount; // Calculation this so that the deltaCash parameter in the results array can updated
                                                    }

                                                    lastInterestCalcDate = dateNum;
                                                }
                                                else
                                                {
                                                    cashInfusionFlag = 0;
                                                    cashInfusionAmount = 0.0;
                                                    cashWithdrawalFlag = 0;
                                                    cashWithdrawalAmount = 0.0;
                                                    interestAmountOnCashAccount = 0.0;
                                                }
                                            }


                                            //==========================================================================================================
                                            // Re-balance Cash and Complementary Investment Shares based on status of current day's Sell Transaction:
                                            //==========================================================================================================
                                            // This is necessary when there is extra cash in the cash account due to a sell order execution during current day.
                                            // If there was a sell transaction during current day, it generated some cash.If there are extra cash funds above and beyond
                                            // the cashBalanceMinCriteria(i.e., cashAvailableForBuyInterim, below), then use those extra funds to buy complementary investment shares.
                                            double cashAvailableForBuyInterim = 0.0;
                                            double shareQtyPotential;
                                            double investedPct;
                                            double deltaSharesFullyInvestedMI;

                                            double fullyInvestedMIMarketValue;
                                            confirmCIBuy = 0;

                                            if (!settings.RebalanceCashCIAfterSellOrderFlag)
                                            {
                                                if (sellOrderTrigger && skipSellTransaction == 0)
                                                {
                                                    // Limit cashAvailableForBuyInterim to the minimum of available cash and maxAllowedFundsForCIPurchaseCurrentYr
                                                    cashAvailableForBuyInterim = Math.Min(Math.Round(cash[day] - cashBalanceMinCriteria, 8), maxAllowedFundsForCIPurchaseCurrentYr);

                                                    if (!settings.BackTestApproachFlag) // Retirement Approach
                                                    {
                                                        if (settings.ComplementaryInvestmentFlag == 1 && cashAvailableForBuyInterim >= 0)
                                                        {
                                                            if (!settings.EnableCIBuysBasedOnMarketLevel || (settings.EnableCIBuysBasedOnMarketLevel && currentMainInvestmentValuationWRTZero >= settings.MarketLevelForCIBuyAuthorization))
                                                            {
                                                                deltaSharesComplimentaryInvestment = cashAvailableForBuyInterim / sharePriceComplementaryInvestment;
                                                                deltaFundsComplimentaryInvestment = deltaSharesComplimentaryInvestment * sharePriceComplementaryInvestment;
                                                                sharesComplementaryInvestment += deltaSharesComplimentaryInvestment;
                                                                cash[day] -= cashAvailableForBuyInterim;
                                                                confirmCIBuy = 1;
                                                            }
                                                        }
                                                    }
                                                    else if (settings.BackTestApproachFlag) // External Income/Dollar Cost Averaging Approach
                                                    {
                                                        if (settings.ComplementaryInvestmentFlag == 1 && cashAvailableForBuyInterim >= 0)
                                                        {
                                                            // TODO: Update this section for Complimentary Investment
                                                            throw new InvalidOperationException("Need to update this section for Complimentary Investment.");
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    // No changes
                                                    cashAvailableForBuyInterim = 0;
                                                    confirmCIBuy = 0;
                                                }
                                            }
                                            else if (settings.RebalanceCashCIAfterSellOrderFlag)
                                            {
                                                if (sellOrderTrigger && skipSellTransaction == 0)
                                                {
                                                    // Limit cashAvailableForBuyInterim to the minimum of available cash, deltaCash, and maxAllowedFundsForCIPurchaseCurrentYr
                                                    cashAvailableForBuyInterim = Math.Min(Math.Min(Math.Round(cash[day] - cashBalanceMinCriteria, 8), deltaCash), maxAllowedFundsForCIPurchaseCurrentYr);

                                                    if (!settings.BackTestApproachFlag) // Retirement Approach
                                                    {
                                                        if (settings.ComplementaryInvestmentFlag == 1 && cashAvailableForBuyInterim >= 0)
                                                        {
                                                            if (!settings.EnableCIBuysBasedOnMarketLevel || (settings.EnableCIBuysBasedOnMarketLevel && currentMainInvestmentValuationWRTZero >= settings.MarketLevelForCIBuyAuthorization))
                                                            {
                                                                deltaSharesComplimentaryInvestment = cashAvailableForBuyInterim / sharePriceComplementaryInvestment;
                                                                deltaFundsComplimentaryInvestment = deltaSharesComplimentaryInvestment * sharePriceComplementaryInvestment;
                                                                sharesComplementaryInvestment += deltaSharesComplimentaryInvestment;
                                                                cash[day] -= cashAvailableForBuyInterim;
                                                                confirmCIBuy = 1;
                                                            }
                                                        }
                                                    }
                                                    else if (settings.BackTestApproachFlag) // External Income/Dollar Cost Averaging Approach
                                                    {
                                                        if (settings.ComplementaryInvestmentFlag == 1 && cashAvailableForBuyInterim >= 0)
                                                        {
                                                            // TODO: Update this section for Complimentary Investment
                                                            throw new InvalidOperationException("Need to update this section for Complimentary Investment.");
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    // No changes
                                                    cashAvailableForBuyInterim = 0.0;
                                                    confirmCIBuy = 0;
                                                }
                                            }

                                            if (fileSettings.RunCalculation == 1)
                                            {
                                                // Add debugging data for Complimentary Investment
                                                List<double[]> debugArrayCI = new List<double[]>();
                                                debugArrayCI.Add(new double[] { day, dateNum, confirmCIBuy, cashAvailableForBuyInterim, deltaFundsComplimentaryInvestment, deltaSharesComplimentaryInvestment, sharePriceComplementaryInvestment, sharesComplementaryInvestment });
                                            }


                                            //==================================================================
                                            // Update portfolio market value components
                                            //==================================================================
                                            marketValueMainInvestmentShares = sharesMainInvestment * sharePriceMainInvestment; // Current market value of MI shares in your account
                                            marketValueComplimentaryInvestment = sharesComplementaryInvestment * sharePriceComplementaryInvestment;

                                            // Update portfolio value
                                            marketValuePortfolio = cash[day] + marketValueMainInvestmentShares + marketValueComplimentaryInvestment - marginAccountBalance;
                                            shareQtyPotential = marketValuePortfolio / sharePriceMainInvestment; // Total number of shares that could be acquired if all funds were used to buy shares on any given day

                                            // Calculate invested percentage
                                            investedPct = (1.0 - (cash[day] / marketValuePortfolio)) * 100.0;


                                            //==================================================================
                                            // Update the Fully Invested MI portfolio based on current day's transactions
                                            //==================================================================
                                            // These calculations are based on the values of the cashWithdrawalFlag and cashInfusionFlag from the current market day
                                            // These updates are necessary to ensure compatibility with buy and sell transactions.

                                            if (!settings.BackTestApproachFlag) // Retirement Approach
                                            {
                                                // Calculate shares to sell from Fully Invested Investment portfolio
                                                deltaSharesFullyInvestedMI = -cashWithdrawalAmount / sharePriceMainInvestment;
                                                sharesFullyInvestedMI += deltaSharesFullyInvestedMI;
                                                fullyInvestedMIMarketValue = sharePriceMainInvestment * sharesFullyInvestedMI;
                                            }
                                            else //if (settings.BackTestApproachFlag == 1) // External Income/Dollar Cost Averaging Approach
                                            {
                                                // Calculate shares to buy for Fully Invested Investment portfolio
                                                deltaSharesFullyInvestedMI = cashInfusionAmount / sharePriceMainInvestment;
                                                sharesFullyInvestedMI += deltaSharesFullyInvestedMI;
                                                fullyInvestedMIMarketValue = sharePriceMainInvestment * sharesFullyInvestedMI;
                                            }

                                            //==================================================================
                                            // Evaluate capitalGainArray
                                            //==================================================================
                                            if (settings.OptimizeCapitalGainFlag)
                                            {
                                                if (day == 0 || deltaShares != 0.0)
                                                {
                                                    cg++; // Counter

                                                    double cgaDeltaShares;
                                                    double cgaResidualShares;
                                                    if (day == 0 && !settings.ExcludeStartingShares)
                                                    {
                                                        cgaDeltaShares = Math.Round(startingShares, 8); // Starting Shares
                                                    }
                                                    else
                                                    {
                                                        cgaDeltaShares = Math.Round(deltaShares, 8); // Residual Shares
                                                    }

                                                    cgaResidualShares = cgaDeltaShares; //Residual Shares (set equal to deltaShares initially)
                                                    double cgaInitialCapitalGain = 0.0; //Initialize Capital Gain

                                                    capitalGainArray.Add(new double[] { cg, day, dateNum, sharePriceMainInvestment, deltaSharesToCoverCashShortFall, cgaDeltaShares, cgaResidualShares, cgaInitialCapitalGain });

                                                    // Check for Sell Transaction (negative deltaShares)
                                                    if (deltaShares < 0.0) //Look for a negative value of deltaShares. This indicates a Sell Transaction. Calculate the capital gain
                                                    {
                                                        //for (int q = 0; q < cg - 1; q++) // On day 0 cg will equal 0, so this loop will not execute. Loop through all the previous transactions to look for residual shares to sell against
                                                        for (int q = 0; q <= cg; q++) // On day 0 cg will equal 0, so this loop will not execute. Loop through all the previous transactions to look for residual shares to sell against
                                                        {
                                                            if (capitalGainArray[q][6] > 0.0) // Check residual share quantity of a previous purchase to ensure there are shares to sell against. Need a positive number of residual shares, otherwise skip to next Buy transaction.
                                                            {
                                                                if (capitalGainArray[q][3] <= capitalGainArray[cg][3] / (settings.MinGainRequirement / 100.0 + 1.0)) //The share price of a preceding Buy must be less than the minGain-adjusted share price of the impending Sell.
                                                                {
                                                                    if (Math.Round(capitalGainArray[q][6], 8) < -Math.Round(capitalGainArray[cg][6], 8)) //Check residual share quantity of a previous purchase to ensure there are shares to sell against. Need a positive number of residual shares, otherwise skip to next Buy transaction.
                                                                    {
                                                                        double sharesAllocatedToSellTransaction = capitalGainArray[q][6];
                                                                        capitalGainArray[cg][7] = capitalGainArray[cg][7] + sharesAllocatedToSellTransaction * (capitalGainArray[cg][3] - capitalGainArray[q][3]);
                                                                        capitalGainArray[q][6] = 0.0; //Adjust the residual shares of the Buy transaction. In this case, all shares were used.
                                                                        capitalGainArray[cg][6] = capitalGainArray[cg][6] + sharesAllocatedToSellTransaction; //Adjust the residual shares of impending Sell transaction
                                                                    }
                                                                    else if (Math.Round(capitalGainArray[q][6], 8) == -Math.Round(capitalGainArray[cg][6], 8)) //Evaluate the case where impending residual shares equal those of previous Buy transaction
                                                                    {
                                                                        double sharesAllocatedToSellTransaction = capitalGainArray[q][6];
                                                                        capitalGainArray[cg][7] = capitalGainArray[cg][7] + sharesAllocatedToSellTransaction * (capitalGainArray[cg][3] - capitalGainArray[q][3]); // Calculate gain
                                                                        capitalGainArray[q][6] = 0.0; //Adjust the residual shares of the Buy transaction. In this case, all shares were used.
                                                                        capitalGainArray[cg][6] = 0.0; //Adjust the residual shares of impending Sell transaction. All impending residual shares where accounted for
                                                                        break;
                                                                    }
                                                                    else if (Math.Round(capitalGainArray[q][6], 8) > -Math.Round(capitalGainArray[cg][6], 8)) //Check whether there are residual shares to sell from previous buy transaction
                                                                    {
                                                                        double sharesAllocatedToSellTransaction = -capitalGainArray[cg][6];
                                                                        capitalGainArray[cg][7] = capitalGainArray[cg][7] + sharesAllocatedToSellTransaction * (capitalGainArray[cg][3] - capitalGainArray[q][3]); //Calculate gain
                                                                        capitalGainArray[q][6] = capitalGainArray[q][6] - sharesAllocatedToSellTransaction; //Adjust the residual shares of the Buy transaction. In this case, only a portion of the shares were used
                                                                        capitalGainArray[cg][6] = 0.0; //Adjust the residual shares of impending Sell transaction. All impending residual shares where accounted for
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        if (Math.Round(capitalGainArray[cg][6], 8) != 0.0) //If after going through all the previous transactions, there are still residual shares remaining, then there is a calculation error
                                                        {
                                                            if (deltaSharesToCoverCashShortFall != 0.0)
                                                            {
                                                                statusUpdater.UpdateStatus($"WARNING-RESIDUAL SHARES NOT = ZERO: Unable to satisfy deltaSharesToCoverCashShortFall of: {deltaSharesToCoverCashShortFall:F2}, ResidShares={Math.Round(capitalGainArray[cg][6], 8):F2}, day={day}, dateNum={dateNum}, MI={ticker}, SELLPROFILE={sellProfile}, BUYPROFILE={buyProfile}, STRATEGY={strategy}");
                                                            }
                                                            else
                                                            {
                                                                statusUpdater.UpdateStatus($"ERROR-RESIDUAL SHARES = ZERO: ResidShares={Math.Round(capitalGainArray[cg][6], 8):F2}, day={day}, dateNum={dateNum}, MI={ticker}, SELLPROFILE={sellProfile}, BUYPROFILE={buyProfile}, STRATEGY={strategy}");
                                                                throw new InvalidOperationException("Residual shares != 0. Capital gain calculation error.");
                                                            }
                                                        }

                                                        if (Math.Round(capitalGainArray[cg][7], 8) < 0.0) //Should never get this condition, but this will stop the calculation if it exists
                                                        {
                                                            statusUpdater.UpdateStatus($"ERROR-GAIN IS LESS THAN ZERO: CapitalGain={Math.Round(capitalGainArray[cg][7], 8):F2}, day={day}, dateNum={dateNum}, MI={ticker}, SELLPROFILE={sellProfile}, BUYPROFILE={buyProfile}, STRATEGY={strategy}");
                                                            throw new InvalidOperationException("Gain is less than zero. Sold shares at a lesser value than purchased.");
                                                        }
                                                    }
                                                }
                                            }

                                            resultsGeneral.Add(new double[] { dateNum, dateNum, sharePriceMainInvestment, sharesMainInvestment, shareQtyPotential, shareBalanceMainInvestmentMinCriteria, marketValueMainInvestmentShares, Math.Round(cash[day], 8), marketValuePortfolio, currentMainInvestmentRegressionValue, savingsCurrentYr, investedPct, interestRateCurrentYear, sharePriceComplementaryInvestment, sharesComplementaryInvestment, marketValueComplimentaryInvestment, sharesFullyInvestedMI, fullyInvestedMIMarketValue, deltaSharesFullyInvestedMI });

                                            resultsTechAnal.Add(new double[] { dateNum, dateNum, marketLow, marketLowWRTZero, marketCorrectionFromLow, marketHigh, marketHighWRTZero, marketCorrectionFromHigh, movingAverageWRTZeroLast, movingAverageWRTZero, movingAverageRateOfChangeWRTZero, movingAverage5WRTZero, movingAverage5RateOfChangeWRTZero, movingAverage20, movingAverage50, movingAverage100, movingAverage200 });

                                            resultsUpdate.Add(new double[] { dateNum, dateNum, interestAmountOnCashAccount, cashInfusionFlag, cashInfusionAmount, cashAccountShortFallCheck, deltaSharesToCoverCashShortFall, cashWithdrawalFlag, cashWithdrawalAmount, cashWithdrawalCurrentYr, confirmCIBuy });

                                            resultsSTC.Add(new double[]
                                            {
                                                dateNum, dateNum, BTC_STCMovingAvgWRTZero, STCSellThresholdAdjustmentTracker ? 1.0 : 0.0, STCLocalBuyEnablementProcessTracker, 0.0, //STCProcessMarketHighWRTZero (placeholder)
                                                0.0, // STCProcessMarketLowWRTZero (placeholder)
                                                0.0, // STCProcessMovingAvgHighWRTZero (placeholder)
                                                0.0, // STCProcessMovingAvgLowWRTZero (placeholder)
                                                0.0, // STCPreemptiveTerminateLocalSellEnablePrcsToggle (placeholder)
                                                0.0 // STCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZero (placeholder)
                                            });

                                            resultsBTC.Add(new double[] { dateNum, dateNum, BTCBuyThresholdAdjustmentTracker, BTCLocalSellEnablementProcessTracker, BTCProcessMarketHighWRTZero, BTCProcessMarketLowWRTZero, BTCProcessMovingAvgHighWRTZero, BTCProcessMovingAvgLowWRTZero, BTCPreemptiveTerminateLocalSellEnablePrcsToggle, BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZero });

                                            results.Add(new double[]
                                            {
                                                dateNum, dateNum, marketValuePortfolio, Math.Round(cash[day], 8), marketValueMainInvestmentShares, sharesMainInvestment, sharePriceMainInvestment, currentMainInvestmentRegressionValue, lastMainInvestmentValuationWRTZero, currentMainInvestmentValuationWRTZero, marketLow, marketLowWRTZero, marketCorrectionFromLow, marketHigh, marketHighWRTZero, marketCorrectionFromHigh, sellOrderTriggerPrelim ? 1.0 : 0.0, sellOrderTrigger ? 1.0 : 0.0, sellByPassCount, violationsMinShareCount, adjustmentsToSellOrderCount, buyFlag, lastMainInvestmentValuationWRTSellThreshold, currentMainInvestmentValuationWRTSellThreshold, actualTransactionSellLevelCrit1, actualTransactionSellExecutionPctCrit2, actualDaysSinceLastSellOrderForThisCrit3, lastTransactionSellLevelCrit1, lastTransactionSellExecutionPctCrit2, buyOrderTriggerPrelim ? 1.0 : 0.0, buyOrderTrigger ? 1.0 : 0.0, buyByPassCount, violationsMinCashCount, adjustmentsToBuyOrderCount, // Obsolete
                                                sellFlag, marketCorrectNegative ? 1.0 : 0.0, absoluteBuyLevelMax, absoluteSellLevel, lastMainInvestmentValuationWRTBuyThreshold, currentMainInvestmentValuationWRTBuyThreshold, actualTransactionBuyLevelCrit1, actualTransactionBuyExecutionPctCrit2, actualDaysSinceLastBuyOrderForThisCrit3, lastTransactionBuyLevelCrit1, lastTransactionBuyExecutionPctCrit2, // Unimportant Variable
                                                deltaCash, deltaShares, marketAccentRate, marketDecentRate, cashInfusionAmount, interestAmountOnCashAccount, cashInfusionFlag, savingsCurrentYr, investedPct, cashBalanceMinCriteria, shareBalanceMainInvestmentMinCriteria, marginAccountBalance, cashAvailableForBuy, sharesMainInvestmentAvailableToSell, cashWithdrawalFlag, cashWithdrawalCurrentYr, cashWithdrawalAmount, cashAccountShortFallCheck, deltaSharesToCoverCashShortFall, deltaSharesFullyInvestedMI, sharesFullyInvestedMI, fullyInvestedMIMarketValue, movingAverageWRTZeroLast, movingAverageWRTZero, movingAverageRateOfChangeWRTZero, buyThreshold, nextBuyLevelCrit1, interestRateCurrentYear, potentialBuyingPower, cashAvailableForBuyInterim, cashAvailableForBuy, deltaCash, deltaFundsComplimentaryInvestment, deltaShares, fundsNeededToCompleteBuyTransaction, deltaSharesComplimentaryInvestment, sharesComplementaryInvestment, sharePriceComplementaryInvestment, marketValueComplimentaryInvestment, sellThreshold, STCLocalBuyEnablementProcessTracker, BTC_STCMovingAvgWRTZero, STCSellThresholdAdjustmentTracker ? 1.0 : 0.0, BTCBuyThresholdAdjustmentTracker, sellResetType1Flag, sellResetType2Flag, buyResetType1Flag, buyResetType2Flag, BTCProcessMarketHighWRTZero, BTCProcessMarketLowWRTZero, BTCProcessMovingAvgHighWRTZero, BTCProcessMovingAvgLowWRTZero, BTCLocalSellEnablementProcessTracker, confirmCIBuy, movingAverage200, movingAverage20, movingAverage100, movingAverage50, shareQtyPotential, movingAverage5RateOfChangeWRTZero, BTCPreemptiveTerminateLocalSellEnablePrcsToggle, movingAverage5WRTZero, BTCPreemptiveTerminateLocalSellEnablePrcsToggleLowWRTZero, relativeStrengthIndex, BBRatioStandardDeviations, lowerBollingerBand, upperBollingerBand
                                            });

                                            // This routine not working.  Need to fix. Also see associated parts of this routine in sell and buy loops
                                            //totalDays = calculatedDurationInCalendarDays;

                                            //if (sellOrderTrigger && ((DateTime.Now - DateTime.FromOADate(dateNum)).TotalDays < totalDays) && settings.EliminateSimilarSellsAtSameMainInvestmentValueFlag == 1)
                                            //{
                                            //    aTransactions.Add(new double[] { day, sharePriceMainInvestment, -1, currentMainInvestmentValuationWRTSellThreshold, lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, investedPct, sharesMainInvestment, cash[day], marketValuePortfolio, dateNum });
                                            //}
                                            //else if (buyOrderTrigger && ((DateTime.Now - DateTime.FromOADate(dateNum)).TotalDays < totalDays) && settings.EliminateSimilarBuysAtSameMainInvestmentValueFlag == 1)
                                            //{
                                            //    aTransactions.Add(new double[] { day, sharePriceMainInvestment, 1, currentMainInvestmentValuationWRTSellThreshold, lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, investedPct, sharesMainInvestment, cash[day], marketValuePortfolio, dateNum });
                                            //}
                                            //else if (settings.EliminateSimilarBuysAtSameMainInvestmentValueFlag == 1 || settings.EliminateSimilarSellsAtSameMainInvestmentValueFlag == 1)
                                            //{
                                            //    aTransactions.Add(new double[] { day, sharePriceMainInvestment, 0, currentMainInvestmentValuationWRTSellThreshold, lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, investedPct, sharesMainInvestment, cash[day], marketValuePortfolio, dateNum });
                                            //}


                                            // ==================================================================================================
                                            // Write to Transactions Tab. Record Transactions that occurred starting "daysProirToLastMarketClose" days prior to today
                                            // ==================================================================================================
                                            if ((fileSettings.UserName.Equals("joe") || fileSettings.UserName.Equals("stick")) && fileSettings.RunCalculation < 5)
                                            {
                                                if (settings.WriteToResultsXLSFileTransactionsTabFlag > 0 && day == 0)
                                                {
                                                    FileInfo fileInfo = new FileInfo(resultsXLSFile);

                                                    if (fileInfo.Exists)
                                                    {
                                                        // Polyform Noncommercial license for EPPlus
                                                        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                                                        using (ExcelPackage package = new ExcelPackage(fileInfo))
                                                        {
                                                            ExcelWorksheet worksheet = package.Workbook.Worksheets["Transactions"];
                                                            if (worksheet == null)
                                                            {
                                                                worksheet = package.Workbook.Worksheets.Add("Transactions");
                                                            }

                                                            // Find the first empty row
                                                            int rowIndex = worksheet.Dimension?.End.Row + 1 ?? 1;

                                                            // Populate rows with data
                                                            worksheet.Cells[rowIndex, 1].Value = investmentData.MainInvestmentName;
                                                            worksheet.Cells[rowIndex, 2].Value = date[day];
                                                            worksheet.Cells[rowIndex, 3].Value = dateNum.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 4].Value = sellProfile.ToString();
                                                            worksheet.Cells[rowIndex, 5].Value = buyProfile.ToString();
                                                            worksheet.Cells[rowIndex, 6].Value = strategy.ToString();
                                                            worksheet.Cells[rowIndex, 7].Value = "Day1";
                                                            worksheet.Cells[rowIndex, 8].Value = currentMainInvestmentRegressionValue.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 9].Value = ""; // Empty cells for data consistency
                                                            worksheet.Cells[rowIndex, 10].Value = ""; // Empty cells for data consistency
                                                            worksheet.Cells[rowIndex, 11].Value = sharePriceMainInvestment.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 12].Value = currentMainInvestmentValuationWRTZero.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 13].Value = "";
                                                            worksheet.Cells[rowIndex, 14].Value = "";
                                                            worksheet.Cells[rowIndex, 15].Value = "";
                                                            worksheet.Cells[rowIndex, 16].Value = "";
                                                            worksheet.Cells[rowIndex, 17].Value = "";
                                                            worksheet.Cells[rowIndex, 18].Value = "";
                                                            worksheet.Cells[rowIndex, 19].Value = shareBalanceMainInvestmentMinCriteria.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 20].Value = fundsAvailableToBuyAtStartOfDay.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 21].Value = cashBalanceMinCriteria.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 22].Value = "";
                                                            worksheet.Cells[rowIndex, 23].Value = "";
                                                            worksheet.Cells[rowIndex, 24].Value = "";
                                                            worksheet.Cells[rowIndex, 25].Value = "";
                                                            worksheet.Cells[rowIndex, 26].Value = investedPct.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 27].Value = cash[day].ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 28].Value = sharePriceComplementaryInvestment.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 29].Value = "";
                                                            worksheet.Cells[rowIndex, 30].Value = "";
                                                            worksheet.Cells[rowIndex, 31].Value = sharesComplementaryInvestment.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 32].Value = marketValueComplimentaryInvestment.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 33].Value = sharesMainInvestment.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 34].Value = marketValueMainInvestmentShares.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 35].Value = marketValuePortfolio.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 36].Value = ((marketValueComplimentaryInvestment + cash[day]) / marketValuePortfolio * 100.0).ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 37].Value = (marketValueMainInvestmentShares / marketValuePortfolio * 100.0).ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 38].Value = sharesFullyInvestedMI.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 39].Value = fullyInvestedMIMarketValue.ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 40].Value = (sharesMainInvestment / sharesFullyInvestedMI * 100.0).ToString(CultureInfo.InvariantCulture);
                                                            worksheet.Cells[rowIndex, 41].Value = (marketValuePortfolio / fullyInvestedMIMarketValue * 100.0).ToString(CultureInfo.InvariantCulture);

                                                            package.Save();
                                                            dateNumLast = dateNum; // Record dateNum of last transaction
                                                        }
                                                    }
                                                    else
                                                    {
                                                        throw new FileNotFoundException("The specified Excel file does not exist.");
                                                    }
                                                }

                                                if (sellOrderTrigger && violationsMinShareCount == 0 && (DateTime.Now - HelperMethods.DateFromNumber(dateNum)).TotalDays < settings.DaysPriorToLastMarketClose)
                                                {
                                                    if (settings.WriteToResultsXLSFileTransactionsTabFlag > 0)
                                                    {
                                                        if (fileSettings.UserName.Equals("joe"))
                                                        {
                                                            if (settings.WriteToResultsXLSFileTransactionsTabFlag == 2)
                                                            {
                                                                string csvFilePath = Path.Combine(Path.GetDirectoryName((resultsXLSFile)), $"{investmentData.MainInvestmentName} - Transactions.csv");
                                                                using (StreamWriter writer = new StreamWriter(csvFilePath, true))
                                                                {
                                                                    string csvLine = string.Join(",", new string[] { caseNo.ToString(), investmentData.MainInvestmentName, date[day].ToString(), sharePriceMainInvestment.ToString(CultureInfo.InvariantCulture), sellProfile.ToString(), buyProfile.ToString(), strategy.ToString(), "Sell", currentMainInvestmentValuationWRTZero.ToString(CultureInfo.InvariantCulture), actualTransactionSellExecutionPctCrit2.ToString(CultureInfo.InvariantCulture), investedPct.ToString(CultureInfo.InvariantCulture), sharesMainInvestment.ToString(CultureInfo.InvariantCulture), cash[day].ToString(CultureInfo.InvariantCulture), marketValuePortfolio.ToString(CultureInfo.InvariantCulture) });
                                                                    writer.WriteLine(csvLine);
                                                                    dateNumLast = dateNum; // Record dateNum of last transaction
                                                                }
                                                            }
                                                            else // if fileSettings.UserName = "stick"
                                                            {
                                                                FileInfo fileInfo = new FileInfo(resultsXLSFile);

                                                                if (fileInfo.Exists)
                                                                {
                                                                    using (ExcelPackage package = new ExcelPackage(fileInfo))
                                                                    {
                                                                        // Polyform Noncommercial license for EPPlus
                                                                        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                                                                        ExcelWorksheet worksheet = package.Workbook.Worksheets["Transactions"];
                                                                        if (worksheet == null)
                                                                        {
                                                                            worksheet = package.Workbook.Worksheets.Add("Transactions");
                                                                        }

                                                                        // Find the first empty row
                                                                        int rowIndex = worksheet.Dimension?.End.Row + 1 ?? 1;

                                                                        // Populate rows with data
                                                                        worksheet.Cells[rowIndex, 1].Value = investmentData.MainInvestmentName;
                                                                        worksheet.Cells[rowIndex, 2].Value = date[day];
                                                                        worksheet.Cells[rowIndex, 3].Value = dateNum.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 4].Value = sellProfile.ToString();
                                                                        worksheet.Cells[rowIndex, 5].Value = buyProfile.ToString();
                                                                        worksheet.Cells[rowIndex, 6].Value = strategy.ToString();
                                                                        worksheet.Cells[rowIndex, 7].Value = "Sell";
                                                                        worksheet.Cells[rowIndex, 8].Value = currentMainInvestmentRegressionValue.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 9].Value = sellThreshold.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 10].Value = hypotheticalMainInvestmentPriceAtSellThreshold.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 11].Value = sharePriceMainInvestment.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 12].Value = currentMainInvestmentValuationWRTZero.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 13].Value = (currentMainInvestmentValuationWRTZero - sellThreshold).ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 14].Value = currentMainInvestmentValuationWRTSellThreshold.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 15].Value = actualTransactionSellLevelCrit1.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 16].Value = actualMainInvestmentValuationWRTSellThresholdAtLastTransaction.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 17].Value = actualTransactionSellExecutionPctCrit2.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 18].Value = sharesMainInvestmentAvailableToSellStartOfDay.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 19].Value = shareBalanceMainInvestmentMinCriteria.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 20].Value = fundsAvailableToBuyAtStartOfDay.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 21].Value = cashBalanceMinCriteria.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 22].Value = sellByPassCount.ToString();
                                                                        worksheet.Cells[rowIndex, 23].Value = "";
                                                                        worksheet.Cells[rowIndex, 24].Value = deltaShares.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 25].Value = deltaCash.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 26].Value = investedPct.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 27].Value = cash[day].ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 28].Value = sharePriceComplementaryInvestment.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 29].Value = deltaSharesComplimentaryInvestment.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 30].Value = deltaFundsComplimentaryInvestment.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 31].Value = sharesComplementaryInvestment.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 32].Value = marketValueComplimentaryInvestment.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 33].Value = sharesMainInvestment.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 34].Value = marketValueMainInvestmentShares.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 35].Value = marketValuePortfolio.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 36].Value = ((marketValueComplimentaryInvestment + cash[day]) / marketValuePortfolio * 100.0).ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 37].Value = (marketValueMainInvestmentShares / marketValuePortfolio * 100.0).ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 38].Value = sharesFullyInvestedMI.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 39].Value = fullyInvestedMIMarketValue.ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 40].Value = (sharesMainInvestment / sharesFullyInvestedMI * 100.0).ToString(CultureInfo.InvariantCulture);
                                                                        worksheet.Cells[rowIndex, 41].Value = (marketValuePortfolio / fullyInvestedMIMarketValue * 100.0).ToString(CultureInfo.InvariantCulture);

                                                                        package.Save();
                                                                        dateNumLast = dateNum; // Record dateNum of last transaction
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    throw new FileNotFoundException("The specified Excel file does not exist.");
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            //=========================================================================================================
                                            // Record "current" variables to "last" variables for use on the next day
                                            //=========================================================================================================
                                            lastMainInvestmentValuationWRTZero = currentMainInvestmentValuationWRTZero;
                                            lastMainInvestmentValuationWRTSellThreshold = currentMainInvestmentValuationWRTSellThreshold;
                                            lastMainInvestmentValuationWRTBuyThreshold = currentMainInvestmentValuationWRTBuyThreshold;

                                            // Check conditions for sell order triggers and update relevant variables
                                            if (sellOrderTrigger && violationsMinShareCount == 0)
                                            {
                                                lastTransactionSellExecutionDeltaShares = deltaShares;
                                            }

                                            if (sellOrderTrigger || violationsMinShareCount >= 1)
                                            {
                                                lastTransactionSellLevelCrit1 = actualTransactionSellLevelCrit1;
                                                lastTransactionSellExecutionPctCrit2 = actualTransactionSellExecutionPctCrit2;
                                                lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn = actualMainInvestmentValuationWRTSellThresholdAtLastTransaction;
                                                lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution = actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction;
                                            }

                                            if (buyOrderTrigger || violationsMinCashCount >= 1)
                                            {
                                                lastTransactionBuyLevelCrit1 = actualTransactionBuyLevelCrit1;
                                                lastTransactionBuyExecutionPctCrit2 = actualTransactionBuyExecutionPctCrit2; // Unimportant variable
                                                lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution = actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction;
                                                lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn = actualMainInvestmentValuationWRTSellThresholdAtLastTransaction;
                                            }
                                        } // ***END SECONDARY BACKTEST LOOP***

                                        statusUpdater.UpdateStatus($"Secondary BackTest Loop Complete");


                                        //=========================================================================================================
                                        // Calculate Performance Results for this Run
                                        // ===========================================================================
                                        // Calculate total buy and sell orders
                                        int numberOfSellOrders = results.Sum(r => (int)r[17]); // Cast each element to int
                                        int numberOfBuyOrders = results.Sum(r => (int)r[30]); // Cast each element to int

                                        DateTime startDateRegression = mainInvestmentCloseDate[0];
                                        DateTime endDateRegression = mainInvestmentCloseDate[mainInvestmentCloseDate.Count - 1];

                                        //=========================================================================================================
                                        // Evaluate Portfolio and Fully Invested MI Performance
                                        // ===========================================================================
                                        double startingMrktValuePortfolio = results[0][2];
                                        double endingMrktValuePortfolio = results[results.Count - 1][2];

                                        double portfolioAnnualizedReturn = (Math.Pow(endingMrktValuePortfolio / startingMrktValuePortfolio, 1.0 / (calculatedDurationInCalendarDays / 365.0)) - 1.0) * 100.0;
                                        double fullInvestedMIAnnualizedReturn = (Math.Pow(results[results.Count - 1][66] / results[0][66], 1.0 / (calculatedDurationInCalendarDays / 365.0)) - 1.0) * 100.0;
                                        double deltaAnnualReturn = portfolioAnnualizedReturn - fullInvestedMIAnnualizedReturn;

                                        double returnInDollarPerDay = (endingMrktValuePortfolio - startingMrktValuePortfolio) / calculatedDurationInCalendarDays;

                                        //=========================================================================================================
                                        // Evaluate Portfolio and Fully Invested MI Volatility
                                        // ===========================================================================
                                        // Regression Fit - Portfolio Daily Returns
                                        double portfolioDailyReturnStdev;
                                        List<double> portfolioDailyReturn = new List<double> { 0 }; // Preallocate with first value as 0
                                        for (int i = 1; i < results.Count; i++)
                                        {
                                            portfolioDailyReturn.Add((results[i][2] - results[i - 1][2]) / results[i][2] * 100.0);
                                        }

                                        portfolioDailyReturnStdev = BackTestUtilities.StandardDeviation(portfolioDailyReturn.Skip(1).ToArray());
                                        double portfolioDailyReturnAverage = portfolioDailyReturn.Skip(1).Average();

                                        // Fully Invested MI Daily Returns
                                        double fullyInvestedMIDailyReturnStdev;
                                        List<double> fullyInvestedMIDailyReturn = new List<double> { 0 }; // Preallocate with first value as 0

                                        for (int i = 1; i < results.Count; i++)
                                        {
                                            fullyInvestedMIDailyReturn.Add((results[i][66] - results[i - 1][66]) / results[i][66] * 100.0);
                                        }

                                        fullyInvestedMIDailyReturnStdev = BackTestUtilities.StandardDeviation(fullyInvestedMIDailyReturn.Skip(1).ToArray());
                                        double fullyInvestedMIDailyReturnAverage = fullyInvestedMIDailyReturn.Skip(1).Average();

                                        //=========================================================================================================
                                        // Write to Transactions Tab. Extrapolate MI regression line for next "extrapolateNumberOfDaysIntoFuture" days.
                                        //=========================================================================================================
                                        if (settings.WriteToResultsXLSFileTransactionsTabFlag == 1)
                                        {
                                            if (settings.VerboseFlag)
                                            {
                                                statusUpdater.UpdateStatus("      INFO: Writing data to resultsXLSFile, Tab: Transactions...");
                                            }

                                            // Extrapolate MI regression line for next "extrapolateNumberOfDaysIntoFuture" days
                                            double dateNumFuture = dateNumLast + 1;
                                            double endDayFuture = dateNumFuture + settings.ExtrapolateNumberOfDaysIntoFuture - 1;
                                            double[] dateNumFutureArray = Enumerable.Range(0, settings.ExtrapolateNumberOfDaysIntoFuture).Select(i => dateNumFuture + i).ToArray();

                                            double[] mainInvestmentRegressionValueFutureLog10 = dateNumFutureArray.Select(d => coeffsCloseValueLog10[0] * d + coeffsCloseValueLog10[1]).ToArray();

                                            double[] mainInvestmentRegressionValueFuture = mainInvestmentRegressionValueFutureLog10.Select(val => Math.Pow(10, val)).ToArray();

                                            FileInfo fileInfo = new FileInfo(resultsXLSFile);

                                            // Write to Transactions Tab
                                            if (fileInfo.Exists)
                                            {
                                                // Polyform Noncommercial license for EPPlus
                                                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                                                using (ExcelPackage package = new ExcelPackage(fileInfo))
                                                {
                                                    ExcelWorksheet transactionsWorksheet = package.Workbook.Worksheets["Transactions"];
                                                    if (transactionsWorksheet == null)
                                                    {
                                                        transactionsWorksheet = package.Workbook.Worksheets.Add("Transactions");
                                                    }

                                                    // Find the first empty row
                                                    int rowIndex = transactionsWorksheet.Dimension?.End.Row + 1 ?? 1;

                                                    for (int j = 0; j < settings.ExtrapolateNumberOfDaysIntoFuture; j++)
                                                    {
                                                        transactionsWorksheet.Cells[rowIndex, 1].Value = investmentData.MainInvestmentName;
                                                        transactionsWorksheet.Cells[rowIndex, 2].Value = dateNumFutureArray[j];
                                                        transactionsWorksheet.Cells[rowIndex, 3].Value = dateNumFutureArray[j];
                                                        transactionsWorksheet.Cells[rowIndex, 4].Value = sellProfile;
                                                        transactionsWorksheet.Cells[rowIndex, 5].Value = buyProfile;
                                                        transactionsWorksheet.Cells[rowIndex, 6].Value = strategy;
                                                        transactionsWorksheet.Cells[rowIndex, 7].Value = "n/a";
                                                        transactionsWorksheet.Cells[rowIndex, 8].Value = mainInvestmentRegressionValueFuture[j];
                                                        rowIndex++;
                                                    }

                                                    //=========================================================================================================
                                                    // Write profiles to Transactions Tab
                                                    //=========================================================================================================
                                                    transactionsWorksheet.Cells["AQ4"].Value = sellProfileResult.SellCriteriaReset[0][0];
                                                    transactionsWorksheet.Cells["AR4"].Value = sellProfileResult.SellCriteriaReset[0][0] - sellThresholdOriginal;
                                                    transactionsWorksheet.Cells["AS4"].Value = sellProfileResult.SellCriteriaReset[0][1];
                                                    transactionsWorksheet.Cells["AT4"].Value = buyProfileResult.BuyCriteriaReset[0][0];
                                                    transactionsWorksheet.Cells["AU4"].Value = buyProfileResult.BuyCriteriaReset[0][0] - buyThresholdOriginal;
                                                    transactionsWorksheet.Cells["AV4"].Value = buyProfileResult.BuyCriteriaReset[0][1];

                                                    package.Save();
                                                }
                                            }
                                            else
                                            {
                                                throw new FileNotFoundException("The specified Excel file does not exist.");
                                            }
                                        }

                                        // Write to CapitalGainArray Tab
                                        if (settings.OptimizeCapitalGainFlag && fileSettings.RunCalculation == 1)
                                        {
                                            FileInfo fileInfo = new FileInfo(resultsXLSFile);

                                            if (fileInfo.Exists)
                                            {
                                                // Polyform Noncommercial license for EPPlus
                                                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                                                using (ExcelPackage package = new ExcelPackage(fileInfo))
                                                {
                                                    ExcelWorksheet capitalGainArrayWorksheet = package.Workbook.Worksheets["CapitalGainArray"];
                                                    if (capitalGainArrayWorksheet == null)
                                                    {
                                                        capitalGainArrayWorksheet = package.Workbook.Worksheets.Add("CapitalGainArray");
                                                    }

                                                    for (int i = 0; i < capitalGainArray.Count; i++) // Use Count for the number of rows
                                                    {
                                                        for (int j = 0; j < capitalGainArray[i].Length; j++) // Use Length for the number of columns in each row
                                                        {
                                                            capitalGainArrayWorksheet.Cells[i + 3, j + 1].Value = capitalGainArray[i][j]; // Write the value
                                                        }
                                                    }

                                                    package.Save();
                                                }
                                            }
                                            else
                                            {
                                                throw new FileNotFoundException("The specified Excel file does not exist.");
                                            }
                                        }

                                        //=========================================================================================================
                                        // Write to DeferredTransactionXLSFile
                                        //=========================================================================================================
                                        //if ((settings.EliminateSimilarBuysAtSameMainInvestmentValueFlag == 1 || settings.EliminateSimilarSellsAtSameMainInvestmentValueFlag == 1) && settings.WriteToDeferredTransactionXLSFileFlag > 0)
                                        //{
                                        //    // TODO: Solve situation with aTransactions prior to trying to write to this array

                                        //    if (settings.WriteToDeferredTransactionXLSFileFlag == 2)
                                        //    {
                                        //        string resFile = fileSettings.DeferredTransactionsXLSFile + " - " + investmentData.MainInvestmentName + ".csv";

                                        //        using (StreamWriter writer = new StreamWriter(resFile, true))
                                        //        {
                                        //            writer.WriteLine($"NEW RESULTS:  MainInvestment = {investmentData.MainInvestmentName},  CaseNo = {caseNo},  sellProfile = {sellProfile},  buyProfile = {buyProfile},  Strategy = {strategy}");
                                        //            // Assuming aTransactions is a 2D array or list of lists
                                        //            foreach (var transaction in aTransactions)
                                        //            {
                                        //                writer.WriteLine(string.Join(",", transaction));
                                        //            }
                                        //        }
                                        //    }
                                        //    else
                                        //    {
                                        //        FileInfo fileInfo = new FileInfo(deferredTransactionsXLSFile);

                                        //        if (fileInfo.Exists)
                                        //        {
                                        //            // Polyform Noncommercial license for EPPlus
                                        //            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                                        //            using (ExcelPackage package = new ExcelPackage(fileInfo))
                                        //            {
                                        //                ExcelWorksheet worksheet = package.Workbook.Worksheets["DeferredTransactions"];
                                        //                if (worksheet == null)
                                        //                {
                                        //                    worksheet = package.Workbook.Worksheets.Add("DeferredTransactions");
                                        //                }

                                        //                int rowIndex = worksheet.Dimension?.End.Row + 1 ?? 1; // Start at the next available row or row 1
                                        //                worksheet.Cells[rowIndex, 1].Value = investmentData.MainInvestmentName;
                                        //                worksheet.Cells[rowIndex, 2].Value = caseNo;
                                        //                worksheet.Cells[rowIndex, 3].Value = sellProfile;
                                        //                worksheet.Cells[rowIndex, 4].Value = buyProfile;
                                        //                worksheet.Cells[rowIndex, 5].Value = strategy;

                                        //                // Assuming aTransactions is a 2D array or list of lists
                                        //                foreach (var transaction in aTransactions)
                                        //                {
                                        //                    rowIndex++;
                                        //                    for (int col = 0; col < transaction.Count; col++)
                                        //                    {
                                        //                        worksheet.Cells[rowIndex, col + 1].Value = transaction[col];
                                        //                    }
                                        //                }

                                        //                package.Save();
                                        //            }
                                        //        }
                                        //    }
                                        //}

                                        // ===========================================================================
                                        // Evaluate Draw down History for Portfolio Value and Main Investment Price for Later Comparison
                                        // Evaluate draw down History of marketValuePortfolio vector
                                        // ===========================================================================
                                        List<double> marketValuePortfolioHistory = new List<double>();
                                        double maxDrawdownPortfolioValue = 0;
                                        int numberOfPortfolioValueDrawdownPeriods = 0;
                                        if (settings.EvaluatePortfolioValueHistoryFlag == 1 || settings.EvaluatePortfolioValueHistoryFlag == 3)
                                        {
                                            maxDrawdownPortfolioValue = 0;
                                            numberOfPortfolioValueDrawdownPeriods = 0;

                                            marketValuePortfolioHistory = results.Select(r => r[2]).ToList(); // MATLAB index 3 -> C# index 2

                                            // Initialize variables
                                            double marketValuePortfolioLow = marketValuePortfolioHistory[0];
                                            double marketValuePortfolioHigh = marketValuePortfolioHistory[0];
                                            List<double[]> drawDownPortfolioValuePrelim = new List<double[]>(); // Preallocate with flexible size
                                            int drawdownCount = 0;

                                            for (int i = 0; i < marketValuePortfolioHistory.Count; i++)
                                            {
                                                if (marketValuePortfolioHistory[i] > marketValuePortfolioHigh)
                                                {
                                                    marketValuePortfolioHigh = marketValuePortfolioHistory[i]; // Update new high
                                                    marketValuePortfolioLow = marketValuePortfolioHistory[i]; // Reset low for new high
                                                }
                                                else if (marketValuePortfolioHistory[i] <= marketValuePortfolioHigh)
                                                {
                                                    if (marketValuePortfolioHistory[i] < marketValuePortfolioLow)
                                                    {
                                                        marketValuePortfolioLow = marketValuePortfolioHistory[i]; // Update new low
                                                        drawdownCount++;

                                                        // Add draw down details
                                                        drawDownPortfolioValuePrelim.Add(new double[]
                                                        {
                                                            i, // Day index
                                                            marketValuePortfolioHigh, // Portfolio High
                                                            marketValuePortfolioLow, // Portfolio Low
                                                            marketValuePortfolioLow - marketValuePortfolioHigh, // Absolute draw down
                                                            (-1.0) * (marketValuePortfolioLow / marketValuePortfolioHigh - 1.0) * 100.0 // Percentage draw down
                                                        });
                                                    }
                                                }
                                            }

                                            // Reduce to actual size
                                            drawDownPortfolioValuePrelim = drawDownPortfolioValuePrelim.Take(drawdownCount).ToList();

                                            // Determine unique draw down values and calculate max draw downs
                                            var uniqueHighs = drawDownPortfolioValuePrelim.Select(row => row[1]).Distinct().ToList();
                                            List<double[]> drawDownPortfolioValue = new List<double[]>();

                                            foreach (var high in uniqueHighs)
                                            {
                                                var rowsForHigh = drawDownPortfolioValuePrelim.Where(row => row[1] == high).ToList();
                                                double maxDrawdownForHigh = rowsForHigh.Max(row => row[4]); // Max draw down percentage for this high
                                                drawDownPortfolioValue.Add(new double[] { high, maxDrawdownForHigh });
                                            }

                                            // Calculate overall max draw down
                                            maxDrawdownPortfolioValue = drawDownPortfolioValue.Max(row => row[1]);

                                            // Count draw down periods exceeding a given magnitude
                                            numberOfPortfolioValueDrawdownPeriods = drawDownPortfolioValue.Count(row => row[1] >= settings.MaxPortfolioDrawdownMagnitudeCriteria);

                                            if (settings.VerboseFlag)
                                            {
                                                statusUpdater.UpdateStatus($"Max Draw down Portfolio Value: {maxDrawdownPortfolioValue:F2}%, Number of Draw down Periods: {numberOfPortfolioValueDrawdownPeriods}");
                                            }
                                        }

                                        //=======================================================================================
                                        // Calculate stats and regression line for the marketValuePortfolio vector
                                        //=======================================================================================
                                        if (settings.EvaluatePortfolioValueHistoryFlag == 2 || settings.EvaluatePortfolioValueHistoryFlag == 3) //Calculate the statistical properties of the marketValuePortfolio vector
                                        {
                                            List<double> dateNumberHistory = results.Select(r => r[1]).ToList(); // MATLAB index 2 -> C# index 1
                                            List<double> marketValuePortfolioHistoryLog10 = marketValuePortfolioHistory.Select(Math.Log10).ToList();

                                            // Calculate regression line coefficients
                                            List<double> coeffsTotalPortfolioValueHistoryLog10 = BackTestUtilities.PerformLinearRegression(dateNumberHistory, marketValuePortfolioHistoryLog10);

                                            // Evaluate regression line and calculate totalPortfolioWRTZero
                                            List<double> fitTotalPortfolioValueHistory = BackTestUtilities.EvaluatePolynomial(coeffsTotalPortfolioValueHistoryLog10, dateNumberHistory);

                                            List<double> totalPortfolioRegressionValue = fitTotalPortfolioValueHistory.Select(fitValue => Math.Pow(10, fitValue)).ToList();

                                            List<double> totalPortfolioWRTZero = marketValuePortfolioHistory.Select((val, index) => ((val - totalPortfolioRegressionValue[index]) / totalPortfolioRegressionValue[index]) * 100.0).ToList();

                                            // stdDev = std(totalPortfolioWRTZero);
                                            // meanValue = mean(totalPortfolioWRTZero);
                                            // minRange = min(totalPortfolioWRTZero);
                                            // maxRange = max(totalPortfolioWRTZero);
                                            // Histogram can be implemented here if needed
                                        }

                                        //=======================================================================================
                                        // Evaluate Main Investment Price History
                                        //=======================================================================================
                                        List<double> sharePriceHistoryMainInvestment = results.Select(r => r[6]).ToList(); // MATLAB index 7 -> C# index 6
                                        List<double[]> drawDownMainInvestmentPricePrelim = new List<double[]>(); // Preallocate
                                        // Calculate all the draw down periods in the sharePriceHistoryMainInvestment vector
                                        int k = 0;
                                        // Calculate the draw down from each of the MainInvestment highs in this back test run
                                        double sharePriceLowMainInvestment = sharePriceHistoryMainInvestment[0]; //Set initial value
                                        double sharePriceHighMainInvestment = 0.0;
                                        double highSharePriceDateNumber = 0;
                                        double sellOrderExecuted = 0;
                                        double lowSharePriceDateNumber;
                                        double buyOrderExecuted;

                                        for (int i = 0; i < sharePriceHistoryMainInvestment.Count; i++)
                                        {
                                            if (sharePriceHistoryMainInvestment[i] > sharePriceHighMainInvestment) //mrktDay 1 through mrktDay final
                                            {
                                                sharePriceHighMainInvestment = sharePriceHistoryMainInvestment[i]; // Record new high
                                                highSharePriceDateNumber = results[i][1];
                                                sellOrderExecuted = results[i][17];
                                                sharePriceLowMainInvestment = sharePriceHistoryMainInvestment[i]; // Set the baseline low for the above new high
                                            }
                                            else if (sharePriceHistoryMainInvestment[i] <= sharePriceHighMainInvestment) // Indicates that in the midst of a downtrend
                                            {
                                                if (sharePriceHistoryMainInvestment[i] < sharePriceLowMainInvestment) // Indicates that MainInvestment has gone even lower
                                                {
                                                    sharePriceLowMainInvestment = sharePriceHistoryMainInvestment[i]; // Record the new MainInvestment low
                                                    lowSharePriceDateNumber = results[i][1];
                                                    buyOrderExecuted = results[i][30];
                                                    k++;
                                                    drawDownMainInvestmentPricePrelim.Add(new double[]
                                                    {
                                                        i, // Market day
                                                        sharePriceHighMainInvestment, // High
                                                        highSharePriceDateNumber, // High date
                                                        sellOrderExecuted, // Sell orders
                                                        sharePriceLowMainInvestment, // Low
                                                        lowSharePriceDateNumber, // Low date
                                                        buyOrderExecuted, // Buy orders
                                                        sharePriceLowMainInvestment - sharePriceHighMainInvestment, // Absolute draw down
                                                        (-1.0) * (sharePriceLowMainInvestment / sharePriceHighMainInvestment - 1.0) * 100.0 // Draw down percentage
                                                    });
                                                }
                                            }
                                        }

                                        drawDownMainInvestmentPricePrelim = drawDownMainInvestmentPricePrelim.Take(k).ToList(); // Truncate the preliminary draw down list

                                        // Calculate the max raw down for this back test run (Note: Draw down was converted to a positive number in the previous calculation)
                                        var uniqueDrawdownSharePriceMainInvestment = drawDownMainInvestmentPricePrelim.Select(row => row[1]).Distinct().ToList(); // Find the indexes of all the unique sharePriceHighMainInvestment values (periods) in drawDownMainInvestmentPricePrelim(j,2)
                                        List<double[]> drawDownMainInvestmentPrice = new List<double[]>();

                                        foreach (var uniqueHigh in uniqueDrawdownSharePriceMainInvestment) // Loop through every unique index value
                                        {
                                            var rows = drawDownMainInvestmentPricePrelim.Where(row => row[1] == uniqueHigh).ToList(); // Find all the rows in drawDownMainInvestmentPricePrelim(:,2) that contain the current uniqueDrawdownSharePriceMainInvestment
                                            double maxDrawdown = rows.Max(row => row[8]); // Determine the max draw down for all the rows that were identified.
                                            double numberOfSellOrdersDD = rows.Sum(row => row[3]);
                                            double numberOfBuyOrdersDD = rows.Sum(row => row[6]);

                                            drawDownMainInvestmentPrice.Add(new double[]
                                            {
                                                uniqueHigh, // High price
                                                maxDrawdown, // Max draw down
                                                numberOfSellOrdersDD, // Total sell orders
                                                numberOfBuyOrdersDD // Total buy orders
                                            });
                                        }

                                        // Calculate max draw down for this back test run. Calculate max draw down statistics. 
                                        double maxMainInvestmentPriceDrawdown = 0.0;
                                        int numberOfMainInvestmentPriceDrawdownPeriods = 0;
                                        int numberOfMainInvestmentPriceDrawdownPeriodsGreaterThan10 = 0;
                                        int numberOfMIPriceDrawdownPeriodsGreaterThan10WithBuyXcute = 0;
                                        if (!drawDownMainInvestmentPrice.Any())
                                        {
                                            maxMainInvestmentPriceDrawdown = 0;
                                            numberOfMainInvestmentPriceDrawdownPeriods = 0;
                                            numberOfMainInvestmentPriceDrawdownPeriodsGreaterThan10 = 0;
                                            numberOfMIPriceDrawdownPeriodsGreaterThan10WithBuyXcute = 0;
                                        }
                                        else
                                        {
                                            maxMainInvestmentPriceDrawdown = drawDownMainInvestmentPrice.Max(row => row[1]);

                                            var maxMainInvestmentPriceDrawdownPeriodsIdx = drawDownMainInvestmentPrice.Where(row => row[1] >= 0).ToList();
                                            numberOfMainInvestmentPriceDrawdownPeriods = maxMainInvestmentPriceDrawdownPeriodsIdx.Count;

                                            var maxDrawdownGreaterThan10Idx = drawDownMainInvestmentPrice.Where(row => row[1] >= settings.MaxMainInvestmentPriceDrawdownMagnitudeCriteria).ToList();
                                            numberOfMainInvestmentPriceDrawdownPeriodsGreaterThan10 = maxDrawdownGreaterThan10Idx.Count;

                                            var periodsWithBuyExecute = maxDrawdownGreaterThan10Idx.Where(row => row[3] >= 1).ToList();
                                            numberOfMIPriceDrawdownPeriodsGreaterThan10WithBuyXcute = periodsWithBuyExecute.Count;
                                        }

                                        // Calculate ratio of draw down periods with buy execution
                                        double ratioMainInvestmentPriceDrawdownPeriodsGT10WithBuyExecute = 0.0;
                                        if (numberOfMainInvestmentPriceDrawdownPeriodsGreaterThan10 == 0)
                                        {
                                            ratioMainInvestmentPriceDrawdownPeriodsGT10WithBuyExecute = 0.0;
                                        }
                                        else
                                        {
                                            ratioMainInvestmentPriceDrawdownPeriodsGT10WithBuyExecute = (numberOfMIPriceDrawdownPeriodsGreaterThan10WithBuyXcute / (double)numberOfMainInvestmentPriceDrawdownPeriodsGreaterThan10) * 100.0;
                                        }

                                        //============================================================================================================
                                        // Write to the resultsCSVFile
                                        // ===================================================================================================
                                        string[] resultsCSVFileArray = new string[122];
                                        resultsCSVFileArray[0] = caseNo.ToString();
                                        resultsCSVFileArray[1] = investmentData.Ticker;
                                        resultsCSVFileArray[2] = complementaryInvestmentData.Ticker;
                                        resultsCSVFileArray[3] = "Run-Code";
                                        resultsCSVFileArray[4] = startingMrktValuePortfolio.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[5] = endingMrktValuePortfolio.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[6] = portfolioAnnualizedReturn.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[7] = fullInvestedMIAnnualizedReturn.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[8] = deltaAnnualReturn.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[9] = portfolioDailyReturnAverage.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[10] = fullyInvestedMIDailyReturnAverage.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[11] = portfolioDailyReturnStdev.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[12] = fullyInvestedMIDailyReturnStdev.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[13] = sellProfile.ToString();
                                        resultsCSVFileArray[14] = buyProfile.ToString();
                                        resultsCSVFileArray[15] = strategy.ToString();
                                        resultsCSVFileArray[16] = sellThresholdOriginal.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[17] = buyThresholdOriginal.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[18] = settings.BackTestApproachFlag.ToString();
                                        resultsCSVFileArray[19] = settings.UseMIPriceInsteadOfMIValuationForTransactionControl.ToString();

                                        resultsCSVFileArray[20] = startDateAnalysisThisRun.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[21] = endDateAnalysisThisRun.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[22] = calculatedDurationInCalendarDays.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[23] = startingCashPercentForThisRun.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[24] = startingCash.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[25] = cashAvailableForInitialCIPurchasePercent.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[26] = startingSharesComplementaryInvestment.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[27] = buyPctInitial.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[28] = startingShares.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[29] = settings.UseUltimateCashBalanceMinCriteriaFlag.ToString();

                                        resultsCSVFileArray[30] = !settings.CashBalanceMinCriteriaFlag ? settings.CashBalanceMinCriteriaDollarAmt.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[31] = settings.CashBalanceMinCriteriaFlag ? settings.CashBalanceMinCriteriaPct.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[32] = settings.CashBalanceMinCriteriaCalcMethodFlag.ToString();
                                        resultsCSVFileArray[33] = settings.CashBalanceMinCriteriaDynamicFlag.ToString();
                                        resultsCSVFileArray[34] = settings.ShareBalanceMainInvestmentMinCriteriaPct.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[35] = settings.UseUltimateShareBalanceMainInvestmentMinCriteriaFlag.ToString();

                                        resultsCSVFileArray[36] = settings.UseUltimateShareBalanceMainInvestmentMinCriteriaFlag ? marketValueMISharesMinCriteriaDollarAmt.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[37] = settings.ShareBalanceMainInvestmentMinCriteriaCalcMethodFlag.ToString();
                                        resultsCSVFileArray[38] = settings.ShareBalanceMainInvestmentMinCriteriaDynamicFlag.ToString();
                                        resultsCSVFileArray[39] = settings.LimitTransactionAmountFlag.ToString();
                                        resultsCSVFileArray[40] = settings.LimitTransactionAmountFlag == 1 ? settings.MaxAllowedTransactionAmount.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[41] = settings.CIInitialSetupFlag.ToString();
                                        resultsCSVFileArray[42] = settings.RebalanceCashCIAfterSellOrderFlag.ToString();
                                        resultsCSVFileArray[43] = settings.EnableCIBuysBasedOnMarketLevel.ToString();

                                        resultsCSVFileArray[44] = settings.EnableCIBuysBasedOnMarketLevel ? settings.MarketLevelForCIBuyAuthorization.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[45] = settings.EnableMatchingShareBuyFlag.ToString();
                                        resultsCSVFileArray[46] = settings.EnableMatchingShareBuyFlag ? settings.MatchingShareBuyMISharePricePctDifferential.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[47] = settings.EnableMatchingShareBuyFlag ? settings.MaxAllowedBuyExecutionPctCrit2ForMatchingShareBuy.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[48] = settings.EnableMatchingShareBuyFlag ? settings.MatchingShareExecutionPctCrit2AdjustFactor.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[48] = settings.CriteriaDaysSinceLastSellTransactionAtSameLevelDefault.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[49] = settings.CriteriaDaysSinceLastSellTransactionAtSameLevelDuringSTAdjust.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[50] = settings.CriteriaDaysSinceLastBuyTransactionAtSameLevelDefault.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[51] = settings.CriteriaDaysSinceLastBuyTransactionAtSameLevelDuringBTAdjust.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[52] = settings.SellBuyOrderTriggerAdjustForShareCashViolationFlag.ToString();
                                        resultsCSVFileArray[53] = inflationRateAverageEntireTimePeriod.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[54] = settings.TransactionMarketLevelTerminationLimit.ToString(CultureInfo.InvariantCulture);

                                        resultsCSVFileArray[55] = settings.EliminateSimilarSellsAtSameMainInvestmentValueFlag ? settings.CriteriaDaysSinceLastSellTransactionAtSameMainInvestmentValue.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[56] = settings.EliminateSimilarSellsAtSameMainInvestmentValueFlag ? settings.CriteriaSellMainInvestmentDollarDifferenceTolerancePct.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[57] = settings.EliminateSimilarBuysAtSameMainInvestmentValueFlag ? settings.CriteriaDaysSinceLastBuyTransactionAtSameMainInvestmentValue.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[58] = settings.EliminateSimilarBuysAtSameMainInvestmentValueFlag ? settings.CriteriaBuyMainInvestmentDollarDifferenceTolerancePct.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[59] = settings.SellProfileLowEndTruncateFlag ? settings.SellProfileLowEndTruncateLevel.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[60] = settings.BuyProfileLowEndTruncateFlag ? settings.BuyProfileLowEndTruncateLevel.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[61] = settings.SellProfileHighEndTruncateFlag ? settings.SellProfileHighEndTruncateLevel.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[62] = settings.BuyProfileHighEndTruncateFlag ? settings.BuyProfileHighEndTruncateLevel.ToString(CultureInfo.InvariantCulture) : "n/a";

                                        resultsCSVFileArray[63] = settings.MovingAverageBSThresholdControlLookBackDaysInitial.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[64] = settings.MovingAverageKnockDownFactor.ToString(CultureInfo.InvariantCulture);

                                        resultsCSVFileArray[65] = settings.SellThresholdControlFlag ? settings.STCAdjustmentTriggerLevelCriteria.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[66] = settings.SellThresholdControlFlag ? settings.STCMrktLowOffsetCriteria.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[67] = settings.SellThresholdControlFlag ? settings.STCUpdateOffsetCriteria.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[68] = settings.SellThresholdControlFlag ? settings.STCAdjustmentTypeCriteria.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[69] = settings.SellThresholdControlFlag ? settings.STCSellThresholdCrossBufferCriteria.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[70] = settings.SellThresholdControlFlag ? settings.STCDecreaseFromIntraSTCProcessMarketHighCriteria.ToString(CultureInfo.InvariantCulture) : "n/a";

                                        resultsCSVFileArray[71] = settings.BuyThresholdControlFlag ? settings.BTCAdjustmentTriggerLevelCriteria.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[72] = settings.BuyThresholdControlFlag ? settings.BTCMrktHighOffsetCriteria.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[73] = settings.BuyThresholdControlFlag ? settings.BTCUpdateOffsetCriteria.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[74] = settings.BuyThresholdControlFlag ? settings.BTCAdjustmentTypeCriteria.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[75] = settings.BuyThresholdControlFlag ? settings.BTCBuyThresholdCrossBufferCriteria.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[76] = settings.BuyThresholdControlFlag ? settings.BTCIncreaseFromIntraBTCProcessMarketLowCriteria.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[77] = settings.BuyThresholdControlFlag ? settings.BTCResetLastMIValuationWRTSellThresholdFlag.ToString() : "n/a";

                                        resultsCSVFileArray[78] = settings.SellRateOfChangeValue.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[79] = settings.BuyRateOfChangeValue.ToString(CultureInfo.InvariantCulture);

                                        resultsCSVFileArray[80] = settings.SpuriousSellFlag ? string.Join(";", settings.SpuriousSellCriteria.Select(array => string.Join(",", array))) : "n/a";

                                        resultsCSVFileArray[81] = settings.EliminateDoldrumsFlag.ToString();
                                        resultsCSVFileArray[82] = "DummyFlag1";
                                        resultsCSVFileArray[83] = "DummyFlag2";
                                        resultsCSVFileArray[84] = "DummyFlag3";
                                        resultsCSVFileArray[85] = settings.MainInvestmentFilterFlag.ToString();

                                        resultsCSVFileArray[86] = startDateRegression.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[87] = endDateRegression.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[88] = settings.MainInvestmentFilterFlag ? settings.NumberOfFilteringStdDevs.ToString(CultureInfo.InvariantCulture) : "n/a";
                                        resultsCSVFileArray[89] = settings.MainInvestmentFilterFlag ? settings.NumberOfFilteringIterations.ToString(CultureInfo.InvariantCulture) : "n/a";

                                        resultsCSVFileArray[90] = stdDevFinal.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[91] = meanValueFinal.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[92] = minRangeFinal.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[93] = maxRangeFinal.ToString(CultureInfo.InvariantCulture);

                                        resultsCSVFileArray[94] = settings.MaxPortfolioDrawdownMagnitudeCriteria.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[95] = settings.MaxMainInvestmentPriceDrawdownMagnitudeCriteria.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[96] = string.Join(";", settings.ScoringCriteria2.Select(array => string.Join(",", array)));

                                        resultsCSVFileArray[97] = settings.TopRunCodeCriteriaPct.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[98] = settings.TopRunCodeCriteriaQty.ToString(CultureInfo.InvariantCulture);

                                        resultsCSVFileArray[99] = numberOfSellOrders.ToString();
                                        resultsCSVFileArray[100] = numberOfBuyOrders.ToString();
                                        resultsCSVFileArray[101] = results.Sum(r => r[18]).ToString(CultureInfo.InvariantCulture); // MATLAB index 19 -> C# index 18
                                        resultsCSVFileArray[102] = results.Sum(r => r[31]).ToString(CultureInfo.InvariantCulture); // MATLAB index 32 -> C# index 31

                                        resultsCSVFileArray[103] = results.Sum(r => r[19]).ToString(CultureInfo.InvariantCulture); // MATLAB index 20 -> C# index 19
                                        resultsCSVFileArray[104] = results.Sum(r => r[32]).ToString(CultureInfo.InvariantCulture); // MATLAB index 33 -> C# index 32
                                        resultsCSVFileArray[105] = results.Sum(r => r[20]).ToString(CultureInfo.InvariantCulture); // MATLAB index 21 -> C# index 20
                                        resultsCSVFileArray[106] = results.Sum(r => r[33]).ToString(CultureInfo.InvariantCulture); // MATLAB index 34 -> C# index 33
                                        resultsCSVFileArray[107] = results.Sum(r => r[21]).ToString(CultureInfo.InvariantCulture); // MATLAB index 22 -> C# index 21
                                        resultsCSVFileArray[108] = results.Sum(r => r[34]).ToString(CultureInfo.InvariantCulture); // MATLAB index 35 -> C# index 34
                                        resultsCSVFileArray[109] = results.Sum(r => r[49]).ToString(CultureInfo.InvariantCulture); // MATLAB index 50 -> C# index 49
                                        resultsCSVFileArray[110] = results.Sum(r => r[50]).ToString(CultureInfo.InvariantCulture); // MATLAB index 51 -> C# index 50
                                        resultsCSVFileArray[111] = returnInDollarPerDay.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[112] = maxDrawdownPortfolioValue.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[113] = numberOfPortfolioValueDrawdownPeriods.ToString();
                                        resultsCSVFileArray[114] = maxMainInvestmentPriceDrawdown.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[115] = numberOfMainInvestmentPriceDrawdownPeriods.ToString();
                                        resultsCSVFileArray[116] = numberOfMainInvestmentPriceDrawdownPeriodsGreaterThan10.ToString();
                                        resultsCSVFileArray[117] = numberOfMIPriceDrawdownPeriodsGreaterThan10WithBuyXcute.ToString();
                                        resultsCSVFileArray[118] = ratioMainInvestmentPriceDrawdownPeriodsGT10WithBuyExecute.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[119] = settings.MaxPortfolioDrawdownMagnitudeCriteria.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[120] = settings.MaxMainInvestmentPriceDrawdownMagnitudeCriteria.ToString(CultureInfo.InvariantCulture);
                                        resultsCSVFileArray[121] = string.Join(";", settings.ScoringCriteria2.Select(array => string.Join(",", array)));

                                        using (StreamWriter writer = new StreamWriter(resultsCSVFile, true))
                                        {
                                            string csvLine = string.Join(",", resultsCSVFileArray.Select(field => field ?? "n/a"));
                                            writer.WriteLine(csvLine);
                                        }


                                        //============================================================================================
                                        // Write to the resultsDetailXLSFile
                                        //============================================================================================
                                        if (settings.WriteToResultsDetailXLSFileFlag > 0)
                                        {
                                            if (settings.VerboseFlag)
                                            {
                                                statusUpdater.UpdateStatus("      Writing results array to ResultsDetailXLSFile...");
                                            }

                                            // Polyform Noncommercial license for EPPlus
                                            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                                            FileInfo fileInfo = new FileInfo(resultsDetailXLSFile);

                                            if (fileSettings.UserName.Equals("joe"))
                                            {
                                                if (settings.WriteToResultsDetailXLSFileFlag == 2)
                                                {
                                                    string resFile = fileSettings.ResultsDetailXLSFile.Replace(".xlsx", $" - {investmentData.MainInvestmentName}.csv");

                                                    using (StreamWriter writer = new StreamWriter(resFile, true))
                                                    {
                                                        writer.WriteLine($"NEW RESULTS:, MainInvestment = {investmentData.MainInvestmentName}, Strategy = {strategy}, sellProfile = {sellProfile}, buyProfile = {buyProfile}");
                                                        foreach (var row in results)
                                                        {
                                                            writer.WriteLine(string.Join(",", row));
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (fileInfo.Exists)
                                                    {
                                                        using (ExcelPackage package = new ExcelPackage(fileInfo))
                                                        {
                                                            ExcelWorksheet worksheet = package.Workbook.Worksheets["Results"];
                                                            if (worksheet == null)
                                                            {
                                                                worksheet = package.Workbook.Worksheets.Add("Results");
                                                            }

                                                            worksheet.Cells["A4"].Value = $"NEW RESULTS: MainInvestment = {investmentData.MainInvestmentName}, Strategy = {strategy}, sellProfile = {sellProfile}, buyProfile = {buyProfile}, SellThreshold = {sellThresholdOriginal}, BuyThreshold = {buyThresholdOriginal}";
                                                            for (int i = 0; i < results.Count; i++)
                                                            {
                                                                for (int j = 0; j < results[i].Length; j++)
                                                                {
                                                                    worksheet.Cells[i + 5, j + 1].Value = results[i][j]; // Write starting at cell (A5)
                                                                }
                                                            }

                                                            package.Save();
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (fileInfo.Exists)
                                                {
                                                    using (ExcelPackage package = new ExcelPackage(fileInfo))
                                                    {
                                                        ExcelWorksheet worksheet = package.Workbook.Worksheets["Results"];
                                                        if (worksheet == null)
                                                        {
                                                            worksheet = package.Workbook.Worksheets.Add("Results");
                                                        }

                                                        worksheet.Cells["A4"].Value = $"NEW RESULTS: MainInvestment = {investmentData.MainInvestmentName}, Strategy = {strategy}, sellProfile = {sellProfile}, buyProfile = {buyProfile}, SellThreshold = {sellThresholdOriginal}, BuyThreshold = {buyThresholdOriginal}";
                                                        for (int i = 0; i < results.Count; i++)
                                                        {
                                                            for (int j = 0; j < results[i].Length; j++)
                                                            {
                                                                worksheet.Cells[i + 5, j + 1].Value = results[i][j]; // Write starting at cell (A5)
                                                            }
                                                        }

                                                        package.Save();
                                                    }
                                                }
                                            }

                                            // Additional result sections

                                            //BackTestUtilities.WriteResultsDetailSection(resultsDetailXLSFile, resultsGeneral, "ResultsGeneral", investmentData.MainInvestmentName, strategy, sellProfile, buyProfile, sellThresholdOriginal, buyThresholdOriginal);
                                            //BackTestUtilities.WriteResultsDetailSection(resultsDetailXLSFile, resultsTechAnal, "ResultsTechAnal", investmentData.MainInvestmentName, strategy, sellProfile, buyProfile, sellThresholdOriginal, buyThresholdOriginal);
                                            //BackTestUtilities.WriteResultsDetailSection(resultsDetailXLSFile, resultsSellOrder, "ResultsSellOrder", investmentData.MainInvestmentName, strategy, sellProfile, buyProfile, sellThresholdOriginal, buyThresholdOriginal);
                                            //BackTestUtilities.WriteResultsDetailSection(resultsDetailXLSFile, resultsBuyOrder, "ResultsBuyOrder", investmentData.MainInvestmentName, strategy, sellProfile, buyProfile, sellThresholdOriginal, buyThresholdOriginal);
                                            //BackTestUtilities.WriteResultsDetailSection(resultsDetailXLSFile, resultsUpdate, "ResultsUpdate", investmentData.MainInvestmentName, strategy, sellProfile, buyProfile, sellThresholdOriginal, buyThresholdOriginal);
                                            //BackTestUtilities.WriteResultsDetailSection(resultsDetailXLSFile, resultsSTC, "ResultsSTC", investmentData.MainInvestmentName, strategy, sellProfile, buyProfile, sellThresholdOriginal, buyThresholdOriginal);
                                            //BackTestUtilities.WriteResultsDetailSection(resultsDetailXLSFile, resultsBTC, "ResultsBTC", investmentData.MainInvestmentName, strategy, sellProfile, buyProfile, sellThresholdOriginal, buyThresholdOriginal);

                                            // Calculate and display statistics for the results array
                                            var valuationColumn = results.Select(r => r[9]).ToArray(); // MATLAB column 10 -> C# index 9
                                            double stdDev = BackTestUtilities.StandardDeviation(valuationColumn);
                                            double meanValue = valuationColumn.Average();
                                            double minRange = valuationColumn.Min();
                                            double maxRange = valuationColumn.Max();

                                            if (settings.VerboseFlag)
                                            {
                                                statusUpdater.UpdateStatus($"      Properties of MainInvestment Data used in BackCheck Run ==> MainInvestment: {investmentData.MainInvestmentName}, Data Points: {valuationColumn.Length}, Mean: {meanValue:F1}, Stdev: {stdDev:F1}, Valuation Range: Min {minRange:F1}, Max {maxRange:F1}");
                                                statusUpdater.UpdateStatus($"      BackCheck Run Properties ==> Start Date: {startDateAnalysisThisRun:MM-dd-yyyy}, End Date: {endDateAnalysisThisRun:MM-dd-yyyy}, Total Days: {runDurationInMarketDays}");
                                            }
                                        }

                                        //=================================================
                                        // Plots
                                        //=================================================
                                        string plot5File = null;
                                        if (settings.PlotFlag)
                                        {
                                            PlotUtility.PlotBackTestCharts(settings.PlotFlag1, settings.PlotFlag2, settings.PlotFlag3, settings.PlotFlag4, settings.PlotFlag5, settings.PlotFlag5a, settings.PlotFlag5b, settings.PlotFlag5c, settings.PlotFlag6, settings.PlotFlag7, settings.PlotFlag8, settings.PlotFlag9, settings.PlotFlag10, settings.PlotFlag11, settings.PlotFlag12, settings.PlotFlag13, runDurationInMarketDays, startingMarketDayThisRun, results, sellProfile, numberOfSellOrders, sellFlag, nextSellLevelCrit1, sellThreshold, buyProfile, numberOfBuyOrders, buyFlag, nextBuyLevelCrit1, buyThreshold, strategy, investmentData.MainInvestmentName, mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10, fitCloseValueMinus60Pct, fitCloseValueMinus50Pct, fitCloseValueMinus40Pct, fitCloseValueMinus30Pct, fitCloseValueMinus20Pct, fitCloseValueMinus10Pct, fitCloseValue, fitCloseValuePlus10Pct, fitCloseValuePlus20Pct, fitCloseValuePlus30Pct, fitCloseValuePlus40Pct, fitCloseValuePlus50Pct, fitCloseValuePlus60Pct, plot5File, currentMainInvestmentRegressionValue, statusUpdater);
                                        }

                                        // Clear Arrays
                                        if (fileSettings.RunCalculation <= settings.MaxRuns)
                                        {
                                            if (deltaAnnualReturn > maxDeltaReturn)
                                            {
                                                maxDeltaReturn = deltaAnnualReturn;
                                            }

                                            if (deltaAnnualReturn < minDeltaReturn)
                                            {
                                                minDeltaReturn = deltaAnnualReturn;
                                            }

                                            sumDeltaAnnualReturn += deltaAnnualReturn;
                                            countRuns++;

                                            statusUpdater.UpdateStatus($"RESULTS FOR: SP={sellProfile}, BP={buyProfile}, STR={strategy}, DeltaReturn={deltaAnnualReturn:F2}, DeltaVolatility={(fullyInvestedMIDailyReturnStdev - portfolioDailyReturnStdev):F2}, NumSellOrdrs={numberOfSellOrders}, NumBuyOrdrs={numberOfBuyOrders}, NumMinShrVios={results.Sum(r => r[19])}, NumMinFundsVios={results.Sum(r => r[32])}");
                                        }
                                    } // ***End Primary Back test loop ***

                                    statusUpdater.UpdateStatus($"Primary BackTest Loop Complete");
                                } // Strategy Loop

                                statusUpdater.UpdateStatus($"Strategy Loop Complete");
                            } // Buy Profile Loop

                            statusUpdater.UpdateStatus($"Buy Profile Loop Complete");
                        } //Sell Profile Loop

                        statusUpdater.UpdateStatus($"Sell Profile Loop complete");


                        if (fileSettings.RunCalculation <= settings.MaxRuns)
                        {
                            if (fileSettings.RunCalculation > 1)
                            {
                                statusUpdater.UpdateStatus($"  AvgDeltaReturn={sumDeltaAnnualReturn / countRuns:F2}, MinDeltaReturn={minDeltaReturn:F2}, MaxDeltaReturn={maxDeltaReturn:F2}");
                            }
                        }

                        if (fileSettings.RunCalculation == 1)
                        {
                            // Market status details
                            statusUpdater.UpdateStatus("{Environment.NewLine} Market Status:");

                            DateTime marketHighDate;
                            DateTime marketLowDate;
                            DateTime endDate;

                            //if ( DateTime.TryParse(date[marketHighDay], out marketHighDate) && DateTime.TryParse(date[marketLowDay], out marketLowDate) && DateTime.TryParse(date[day], out endDate))
                            if (DateTime.TryParse(date[marketHighDay].ToString(), out marketHighDate) && DateTime.TryParse(date[marketLowDay].ToString(), out marketLowDate) && DateTime.TryParse(date[day - 1].ToString(), out endDate)) // Use day-1 to avoid index out-of-range
                            {
                                statusUpdater.UpdateStatus($"  marketHighDay={marketHighDay}, marketHighDate={HelperMethods.ConvertFromDateToExcelDateNumber(marketHighDate)}, Price={marketHigh:F2}, EndDay={day}, EndDate={HelperMethods.ConvertFromDateToExcelDateNumber(endDate)}, Price={sharePriceMainInvestment:F2}, EndDateMarketCorrectionFromHigh={marketCorrectionFromHigh:F2}");
                                statusUpdater.UpdateStatus($"  marketLowDay={marketLowDay}, marketLowDate={HelperMethods.ConvertFromDateToExcelDateNumber(marketLowDate)}, EndDay={day},  EndDate={HelperMethods.ConvertFromDateToExcelDateNumber(endDate)}, EndDateMarketCorrectionFromLow={marketCorrectionFromLow:F2}");
                            }
                            else
                            {
                                statusUpdater.UpdateStatus("   Error: Invalid date format in the date list.");
                            }
                        }
                    } // End Time Period Loop

                    statusUpdater.UpdateStatus($"Time Period Loop Complete");
                } // End MI Loop

                statusUpdater.UpdateStatus($"BackTest Analysis is Complete");

                // Report elapsed time                        
                stopwatch.Stop(); // Stop the stopwatch after the analysis is complete
                double elapsedTime = stopwatch.Elapsed.TotalSeconds; // Convert elapsed time to seconds                      
                statusUpdater.UpdateStatus($"Elapsed time: {elapsedTime:F2} seconds"); // Report elapsed time
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during BackTest Analysis: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    } //End BackTestFunctions

    public static class BackTestUtilities
    {
        public static void ValidateInputFlagSettings(BackTestSettings settings)
        {
            // Ensure that flag settings will not cause conflicts
            if (settings.InflationEndDateNumCalcFlag == 1 && (!settings.EndDateAnalysisFlag || settings.EndDateRegressionFlag == 0))
            {
                throw new InvalidOperationException("When InflationEndDateNumCalcFlag is set to 1, EndDateAnalysisFlag must be 1 and EndDateRegressionFlag must be 1 or 2.");
            }

            if (settings.CashBalanceMinCriteriaCalcMethodFlag && !settings.CashBalanceMinCriteriaFlag)
            {
                throw new InvalidOperationException("CashBalanceMinCriteriaCalcMethodFlag is 1, but CashBalanceMinCriteriaFlag is 0. Adjust the settings appropriately.");
            }
        }

        public static int[] SellProfileCompatibilityCheck(BackTestSettings settings, FileControlSettings fileSettings, IStatusUpdater statusUpdater)
        {
            // Initialize an updated SellProfile list
            List<int> SPUpdate = new List<int>();

            // If STC Reduce Sell Execution flag is enabled
            if (settings.STCReduceSellExecutionPctCrit2DuringSTCProcessFlag)
            {
                foreach (var sellProfile in settings.SP)
                {
                    // Generate sell profile
                    var sellProfileResult = SellProfileGeneration(settings, sellProfile, statusUpdater);
                    var sellProfileRelativeMrktLevel = sellProfileResult.SellProfileRelativeMrktLevel;

                    // Check compatibility
                    List<double> deltaLevel = new List<double>();
                    bool isCompatible = true;

                    for (int r = 1; r < sellProfileRelativeMrktLevel.Count; r++)
                    {
                        deltaLevel.Add(sellProfileRelativeMrktLevel[r] - sellProfileRelativeMrktLevel[r - 1]);

                        if (r > 1 && deltaLevel[r - 1] != deltaLevel[r - 2])
                        {
                            statusUpdater.UpdateStatus($"  SellProfile={sellProfile} is not compatible. Skipping to next SP.");
                            isCompatible = false;
                            break;
                        }
                    }

                    // If compatible, add to updated list
                    if (isCompatible)
                    {
                        SPUpdate.Add(sellProfile);
                    }
                }

                // Log if the SP list is updated
                if (settings.SP.Length != SPUpdate.Count)
                {
                    statusUpdater.UpdateStatus($"  Some SellProfiles are not compatible. Updated SellProfile list: {string.Join(",", SPUpdate)}.");
                }
                else
                {
                    statusUpdater.UpdateStatus("  All SellProfiles are compatible.");
                }
            }

            // Return updated SP as an array
            return SPUpdate.ToArray();
        }

        public static SellProfileResult SellProfileGeneration(BackTestSettings settings, int sellProfile, IStatusUpdater statusUpdater)
        {
            // Initialize parameters of the SellProfileResult Class
            List<double> sellProfileRelativeMrktLevel = new List<double>();
            List<double> sellProfilePctOfAvailShares = new List<double>();
            List<double[]> sellCriteriaReset = new List<double[]>(); // Criteria used to define sell orders

            // Define sell profiles
            switch (sellProfile)
            {
                case 1:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        20,
                        30,
                        40,
                        50,
                        60,
                        70,
                        80,
                        90,
                        100,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2,
                        4,
                        7,
                        16,
                        32,
                        64,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 2:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        5,
                        10,
                        16,
                        23,
                        31,
                        41,
                        53,
                        65,
                        80,
                        95,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 3:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2,
                        4,
                        8,
                        16,
                        32,
                        64,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 4:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        1,
                        2,
                        3,
                        4,
                        5,
                        6,
                        7,
                        8,
                        9,
                        10,
                        11,
                        12,
                        13,
                        14,
                        15,
                        16,
                        17,
                        18,
                        19,
                        20,
                        21,
                        22,
                        23
                    };
                    break;
                case 5:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2,
                        8,
                        18,
                        29,
                        42,
                        54,
                        65,
                        75,
                        83,
                        89,
                        93,
                        96,
                        98,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 6:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        2,
                        4,
                        6,
                        8,
                        10,
                        12,
                        14,
                        16,
                        18,
                        20,
                        22,
                        24,
                        26,
                        28,
                        28.1,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        5,
                        10,
                        20,
                        40,
                        80,
                        90,
                        93,
                        95,
                        95,
                        95,
                        95,
                        95,
                        95,
                        95,
                        0,
                        0
                    };
                    break;
                case 7:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        4,
                        8,
                        12,
                        16,
                        20,
                        24,
                        28,
                        32,
                        36,
                        40,
                        44,
                        48,
                        52,
                        56,
                        60,
                        64,
                        68,
                        72,
                        76,
                        80,
                        84,
                        88,
                        92,
                        96,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2,
                        6,
                        14,
                        33,
                        61,
                        89,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 8:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        4,
                        8,
                        12,
                        16,
                        20,
                        24,
                        28,
                        32,
                        36,
                        40,
                        44,
                        48,
                        52,
                        56,
                        60,
                        64,
                        68,
                        72,
                        76,
                        80,
                        84,
                        88,
                        92,
                        96,
                        100,
                        104,
                        108,
                        112
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2,
                        7,
                        30,
                        61,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 9:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        4.5,
                        8,
                        11.5,
                        15,
                        18.5,
                        22,
                        25.5,
                        29,
                        32.5,
                        36,
                        39.5,
                        43,
                        46.5,
                        49,
                        51.5,
                        54,
                        56.5,
                        59,
                        61.5,
                        64,
                        66.5,
                        69,
                        75,
                        80,
                        80.1,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        3.25,
                        7,
                        14,
                        28,
                        56,
                        80,
                        97.5,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        0,
                        0
                    };
                    break;
                case 10:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        3.5,
                        10,
                        26,
                        54,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 11:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        3.5,
                        7,
                        18,
                        36,
                        64,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 12:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2,
                        8,
                        16,
                        23,
                        30,
                        35,
                        39,
                        43,
                        45,
                        47,
                        48,
                        49,
                        49,
                        50,
                        50,
                        50,
                        50,
                        50,
                        50,
                        50,
                        50,
                        50,
                        50
                    };
                    break;
                case 23:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        3,
                        4,
                        5,
                        6,
                        8,
                        10,
                        12,
                        14,
                        16,
                        20,
                        23,
                        27,
                        30,
                        35,
                        40,
                        45,
                        50,
                        50,
                        75,
                        75,
                        100,
                        125,
                        150
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        1,
                        2,
                        3,
                        4,
                        5,
                        10,
                        10,
                        10,
                        10,
                        10,
                        20,
                        20,
                        20,
                        20,
                        20,
                        30,
                        30,
                        30,
                        30,
                        40,
                        50,
                        75,
                        100
                    };
                    break;
                case 24:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        2,
                        4,
                        6,
                        8,
                        10,
                        12,
                        14,
                        16,
                        18,
                        20,
                        22,
                        24,
                        26,
                        28,
                        30,
                        30.1,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        5,
                        10,
                        20,
                        30,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        95,
                        0,
                        0
                    };
                    break;
                case 25:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2,
                        3.75,
                        7.25,
                        14.25,
                        25,
                        42,
                        68,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 26:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2,
                        3.5,
                        6.5,
                        12.5,
                        21,
                        33,
                        49,
                        72,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 27:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2,
                        3.25,
                        5.75,
                        10.75,
                        18,
                        28,
                        40,
                        56,
                        76,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 28:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2,
                        3,
                        5,
                        9,
                        15,
                        23.5,
                        33.5,
                        46,
                        61,
                        79,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 29:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2,
                        2.75,
                        4.25,
                        7.25,
                        12,
                        18,
                        26,
                        36,
                        48,
                        63,
                        80,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 34:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2.00,
                        5.00,
                        10.50,
                        19.50,
                        32.00,
                        50.00,
                        65.50,
                        76.00,
                        83.50,
                        89.00,
                        92.75,
                        95.50,
                        95.50,
                        95.50,
                        95.50,
                        95.50,
                        95.50,
                        95.50,
                        95.50,
                        95.50,
                        95.50,
                        95.50,
                        95.50
                    };
                    break;
                case 35:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2.00,
                        4.55,
                        9.25,
                        17.05,
                        27.80,
                        43.10,
                        56.30,
                        65.40,
                        71.90,
                        76.70,
                        80.00,
                        82.40,
                        82.40,
                        82.40,
                        82.40,
                        82.40,
                        82.40,
                        82.40,
                        82.40,
                        82.40,
                        82.40,
                        82.40,
                        82.40
                    };
                    break;
                case 36:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2.00,
                        4.10,
                        8.00,
                        14.60,
                        23.60,
                        36.20,
                        47.10,
                        54.80,
                        60.30,
                        64.40,
                        67.25,
                        69.30,
                        69.30,
                        69.30,
                        69.30,
                        69.30,
                        69.30,
                        69.30,
                        69.30,
                        69.30,
                        69.30,
                        69.30,
                        69.30
                    };
                    break;
                case 37:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2.00,
                        3.65,
                        6.75,
                        12.15,
                        19.40,
                        29.30,
                        37.90,
                        44.20,
                        48.70,
                        52.10,
                        54.50,
                        56.20,
                        56.20,
                        56.20,
                        56.20,
                        56.20,
                        56.20,
                        56.20,
                        56.20,
                        56.20,
                        56.20,
                        56.20,
                        56.20
                    };
                    break;
                case 38:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2.00,
                        3.20,
                        5.50,
                        9.70,
                        15.20,
                        22.40,
                        28.70,
                        33.60,
                        37.10,
                        39.80,
                        41.75,
                        43.10,
                        43.10,
                        43.10,
                        43.10,
                        43.10,
                        43.10,
                        43.10,
                        43.10,
                        43.10,
                        43.10,
                        43.10,
                        43.10
                    };
                    break;
                case 39:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2.00,
                        2.75,
                        4.25,
                        7.25,
                        11.00,
                        15.50,
                        19.50,
                        23.00,
                        25.50,
                        27.50,
                        29.00,
                        30.00,
                        30.00,
                        30.00,
                        30.00,
                        30.00,
                        30.00,
                        30.00,
                        30.00,
                        30.00,
                        30.00,
                        30.00,
                        30.00
                    };
                    break;
                case 40:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        15,
                        20,
                        25,
                        30,
                        35,
                        40,
                        45,
                        50,
                        55,
                        60,
                        65,
                        70,
                        75,
                        80,
                        85,
                        90,
                        95,
                        100,
                        105,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        0.0,
                        15.7,
                        30.9,
                        44.8,
                        57.3,
                        67.9,
                        76.6,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0
                    };
                    break;
                case 41:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        5.0,
                        10.0,
                        15.0,
                        20.0,
                        25.0,
                        30.0,
                        35.0,
                        40.0,
                        45.0,
                        50.0,
                        55.0,
                        60.0,
                        65.0,
                        70.0,
                        75.0,
                        80.0,
                        85.0,
                        90.0,
                        95.0,
                        100.0,
                        105.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        0.0,
                        20.0,
                        40.0,
                        60.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0
                    };
                    break;
                case 42:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        2,
                        4,
                        6,
                        8,
                        10,
                        12,
                        14,
                        16,
                        18,
                        20,
                        24,
                        26,
                        28,
                        30,
                        32,
                        34,
                        36,
                        38,
                        40,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        5,
                        7.5,
                        10,
                        12.5,
                        15,
                        1,
                        1,
                        1,
                        1,
                        20,
                        22.5,
                        25,
                        1,
                        1,
                        50,
                        1,
                        1,
                        1,
                        75,
                        1
                    };
                    break;
                case 43:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        2.5,
                        5,
                        7.5,
                        10,
                        12.5,
                        15,
                        20,
                        22.5,
                        25,
                        27.5,
                        30,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        10,
                        20,
                        40,
                        80,
                        80,
                        80,
                        80,
                        80,
                        80,
                        80,
                        80,
                        0
                    };
                    break;
                case 44:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        2.5,
                        5.0,
                        7.5,
                        10.0,
                        12.0,
                        14.0,
                        16.0,
                        18.0,
                        20.0,
                        22.0,
                        24.0,
                        26.0,
                        27.5,
                        29.0,
                        30.5,
                        32.0,
                        33.5,
                        35.0,
                        36.5,
                        38.0,
                        39.5,
                        41.0,
                        42.5,
                        44.0,
                        45.5,
                        47.0,
                        48.5,
                        50.0,
                        51.5,
                        53.0,
                        54.5,
                        56.0,
                        57.5,
                        59.0,
                        60.5,
                        62.0,
                        63.5,
                        65.0,
                        66.5,
                        68.0,
                        69.5,
                        71.0,
                        72.5,
                        74.0,
                        75.5,
                        77.0,
                        78.5,
                        80.0,
                        81.5,
                        83.0,
                        84.5,
                        86.0,
                        87.5,
                        89.0,
                        90.5,
                        92.0,
                        93.5,
                        95.0,
                        96.5,
                        98.0,
                        99.5,
                        101.0,
                        102.5,
                        104.0,
                        105.5,
                        107.0,
                        108.5,
                        110.0,
                        111.5,
                        113.0,
                        114.5,
                        116.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        5.0,
                        6.0,
                        7.0,
                        8.0,
                        9.0,
                        10.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0
                    };
                    break;
                case 45:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        5.0,
                        10.0,
                        15.0,
                        20.0,
                        25.0,
                        30.0,
                        35.0,
                        40.0,
                        45.0,
                        50.0,
                        55.0,
                        60.0,
                        65.0,
                        70.0,
                        75.0,
                        80.0,
                        85.0,
                        90.0,
                        95.0,
                        100.0,
                        105.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        24.4,
                        12.0,
                        9.0,
                        6.5,
                        4.2,
                        3.0,
                        2.4,
                        2.0,
                        1.8,
                        1.5,
                        1.3,
                        1.3,
                        1.3,
                        1.3,
                        1.3,
                        1.3,
                        1.3,
                        1.3,
                        1.3,
                        1.3,
                        1.3,
                        1.3,
                        1.3
                    };
                    break;
                case 46:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        5.0,
                        10.0,
                        15.0,
                        20.0,
                        25.0,
                        30.0,
                        35.0,
                        40.0,
                        45.0,
                        50.0,
                        55.0,
                        60.0,
                        65.0,
                        70.0,
                        75.0,
                        80.0,
                        85.0,
                        90.0,
                        95.0,
                        100.0,
                        105.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        24.4,
                        12.0,
                        9.0,
                        6.5,
                        7.0,
                        7.5,
                        8.0,
                        8.5,
                        9.0,
                        9.5,
                        10.0,
                        10.5,
                        11.0,
                        11.5,
                        12.0,
                        12.5,
                        13.0,
                        13.5,
                        14.0,
                        14.5,
                        15.0,
                        15.5,
                        16.0
                    };
                    break;
                case 47:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        5.0,
                        10.0,
                        15.0,
                        20.0,
                        25.0,
                        30.0,
                        35.0,
                        40.0,
                        45.0,
                        50.0,
                        55.0,
                        60.0,
                        65.0,
                        70.0,
                        75.0,
                        80.0,
                        85.0,
                        90.0,
                        95.0,
                        100.0,
                        105.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        8.3,
                        9.1,
                        10.0,
                        11.1,
                        12.5,
                        14.3,
                        16.7,
                        20.0,
                        25.0,
                        33.3,
                        50.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0
                    };
                    break;
                case 48:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        5.0,
                        10.0,
                        15.0,
                        20.0,
                        25.0,
                        30.0,
                        35.0,
                        40.0,
                        45.0,
                        50.0,
                        55.0,
                        60.0,
                        65.0,
                        70.0,
                        75.0,
                        80.0,
                        85.0,
                        90.0,
                        95.0,
                        100.0,
                        105.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        14.0,
                        16.0,
                        18.2,
                        20.2,
                        21.8,
                        22.8,
                        22.8,
                        22.0,
                        20.4,
                        18.4,
                        16.3,
                        14.2,
                        12.3,
                        10.7,
                        9.3,
                        8.1,
                        7.1,
                        6.2,
                        5.5,
                        4.8,
                        4.3,
                        3.9,
                        3.5
                    };
                    break;
                case 49:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        3.0,
                        5.0,
                        8.0,
                        10.0,
                        12.0,
                        14.0,
                        16.0,
                        18.0,
                        20.0,
                        22.0,
                        24.0,
                        26.0,
                        28.0,
                        30.0,
                        32.0,
                        34.0,
                        36.0,
                        39.0,
                        42.0,
                        45.0,
                        48.0,
                        51.0,
                        54.0,
                        57.0,
                        60.0,
                        63.0,
                        66.0,
                        69.0,
                        72.0,
                        75.0,
                        78.0,
                        81.0,
                        84.0,
                        87.0,
                        90.0,
                        93.0,
                        96.0,
                        99.0,
                        102.0,
                        105.0,
                        108.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        40.00,
                        23.00,
                        14.00,
                        7.00,
                        5.00,
                        4.25,
                        3.50,
                        3.00,
                        2.50,
                        2.25,
                        2.00,
                        1.65,
                        1.50,
                        1.25,
                        1.10,
                        0.90,
                        0.87,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83,
                        0.83
                    };
                    break;
                case 50:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        5,
                        10,
                        20,
                        30,
                        40,
                        50,
                        60,
                        70,
                        80,
                        90,
                        100,
                        110
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0
                    };
                    break;
                case 55:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        5.0,
                        10.0,
                        15.0,
                        20.0,
                        25.0,
                        30.0,
                        35.0,
                        40.0,
                        45.0,
                        50.0,
                        55.0,
                        60.0,
                        65.0,
                        70.0,
                        75.0,
                        80.0,
                        85.0,
                        90.0,
                        95.0,
                        100.0,
                        105.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        1.0,
                        5.0,
                        10.0,
                        15.0,
                        20.0,
                        25.0,
                        30.0,
                        35.0,
                        40.0,
                        45.0,
                        50.0,
                        55.0,
                        60.0,
                        65.0,
                        70.0,
                        75.0,
                        80.0,
                        85.0,
                        90.0,
                        95.0,
                        100.0,
                        100.0,
                        100.0
                    };
                    break;
                case 56:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        5.0,
                        10.0,
                        20.0,
                        30.0,
                        40.0,
                        50.0,
                        60.0,
                        70.0,
                        80.0,
                        90.0,
                        100.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2.0,
                        4.0,
                        6.0,
                        8.0,
                        11.1,
                        12.5,
                        14.3,
                        16.7,
                        20.0,
                        25.0,
                        33.3,
                        50.0,
                        100.0
                    };
                    break;
                case 57:
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        5.0,
                        10.0,
                        15.0,
                        20.0,
                        25.0,
                        30.0,
                        35.0,
                        40.0,
                        45.0,
                        50.0,
                        55.0,
                        60.0,
                        65.0,
                        70.0,
                        75.0,
                        80.0,
                        85.0,
                        90.0,
                        95.0,
                        100.0,
                        105.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        6.0,
                        6.38,
                        6.8,
                        7.3,
                        7.9,
                        8.6,
                        9.4,
                        10.3,
                        11.5,
                        13.0,
                        15.0,
                        17.6,
                        21.4,
                        27.3,
                        37.5,
                        60.0,
                        0.0,
                        0.0,
                        0.0,
                        0.0,
                        0.0,
                        0.0,
                        0.0
                    };
                    break;
                case 58: // Modification of SP43
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        2.5,
                        5.0,
                        7.5,
                        10.0,
                        12.5,
                        15.0,
                        17.5,
                        20.0,
                        22.5,
                        25.0,
                        27.5,
                        30.0,
                        32.5,
                        35.0,
                        37.5,
                        40.0,
                        42.5,
                        45.0,
                        47.5,
                        50.0,
                        52.5,
                        55.0,
                        57.5,
                        60.0,
                        62.5,
                        65.0,
                        67.5,
                        70.0,
                        72.5,
                        75.0,
                        77.5,
                        80.0,
                        82.5,
                        85.0,
                        87.5,
                        90.0,
                        92.5,
                        95.0,
                        97.5,
                        100.0,
                        102.5,
                        105.0,
                        107.5,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        0.0,
                        10.0,
                        20.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0
                    };
                    break;

                case 59: // SP43 but extended to eliminate bug
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        2.5,
                        5.0,
                        7.5,
                        10.0,
                        12.5,
                        15.0,
                        17.5,
                        20.0,
                        22.5,
                        25.0,
                        27.5,
                        30.0,
                        32.5,
                        35.0,
                        37.5,
                        40.0,
                        42.5,
                        45.0,
                        47.5,
                        50.0,
                        52.5,
                        55.0,
                        57.5,
                        60.0,
                        62.5,
                        65.0,
                        67.5,
                        70.0,
                        72.5,
                        75.0,
                        77.5,
                        80.0,
                        82.5,
                        85.0,
                        87.5,
                        90.0,
                        92.5,
                        95.0,
                        97.5,
                        100.0,
                        102.5,
                        105.0,
                        107.5,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        0.0,
                        10.0,
                        20.0,
                        40.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0
                    };
                    break;

                case 60: // Modification of SP58
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        2.5,
                        5.0,
                        7.5,
                        10.0,
                        12.5,
                        15.0,
                        17.5,
                        20.0,
                        22.5,
                        25.0,
                        27.5,
                        30.0,
                        32.5,
                        35.0,
                        37.5,
                        40.0,
                        42.5,
                        45.0,
                        47.5,
                        50.0,
                        52.5,
                        55.0,
                        57.5,
                        60.0,
                        62.5,
                        65.0,
                        67.5,
                        70.0,
                        72.5,
                        75.0,
                        77.5,
                        80.0,
                        82.5,
                        85.0,
                        87.5,
                        90.0,
                        92.5,
                        95.0,
                        97.5,
                        100.0,
                        102.5,
                        105.0,
                        107.5,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        5.0,
                        10.0,
                        20.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        40.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0,
                        10.0
                    };
                    break;

                case 61: // SP59 but change first y-entry from 0 to 5
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        2.5,
                        5.0,
                        7.5,
                        10.0,
                        12.5,
                        15.0,
                        17.5,
                        20.0,
                        22.5,
                        25.0,
                        27.5,
                        30.0,
                        32.5,
                        35.0,
                        37.5,
                        40.0,
                        42.5,
                        45.0,
                        47.5,
                        50.0,
                        52.5,
                        55.0,
                        57.5,
                        60.0,
                        62.5,
                        65.0,
                        67.5,
                        70.0,
                        72.5,
                        75.0,
                        77.5,
                        80.0,
                        82.5,
                        85.0,
                        87.5,
                        90.0,
                        92.5,
                        95.0,
                        97.5,
                        100.0,
                        102.5,
                        105.0,
                        107.5,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        5.0,
                        10.0,
                        20.0,
                        40.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        80.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0,
                        1.0
                    };
                    break;

                case 62: // SP1 but changed to 5% increments
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        5.0,
                        10.0,
                        15.0,
                        20.0,
                        25.0,
                        30.0,
                        35.0,
                        40.0,
                        45.0,
                        50.0,
                        55.0,
                        60.0,
                        65.0,
                        70.0,
                        75.0,
                        80.0,
                        85.0,
                        90.0,
                        95.0,
                        100.0,
                        105.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2.0,
                        4.0,
                        6.0,
                        8.0,
                        10.5,
                        14.0,
                        20.0,
                        29.0,
                        40.0,
                        60.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0
                    };
                    break;

                case 63: // Constant profile
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        5.0,
                        10.0,
                        15.0,
                        20.0,
                        25.0,
                        30.0,
                        35.0,
                        40.0,
                        45.0,
                        50.0,
                        55.0,
                        60.0,
                        65.0,
                        70.0,
                        75.0,
                        80.0,
                        85.0,
                        90.0,
                        95.0,
                        100.0,
                        105.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0,
                        7.0
                    };
                    break;
                case 64: // Same depletion rate at SP62, but change to 2.5% sell increments
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        2.5,
                        5.0,
                        7.5,
                        10.0,
                        12.5,
                        15.0,
                        17.5,
                        20.0,
                        22.5,
                        25.0,
                        27.5,
                        30.0,
                        32.5,
                        35.0,
                        37.5,
                        40.0,
                        42.5,
                        45.0,
                        47.5,
                        50.0,
                        52.5,
                        55.0,
                        57.5,
                        60.0,
                        62.5,
                        65.0,
                        67.5,
                        70.0,
                        72.5,
                        75.0,
                        77.5,
                        80.0,
                        82.5,
                        85.0,
                        87.5,
                        90.0,
                        92.5,
                        95.0,
                        97.5,
                        100.0,
                        102.5,
                        105.0,
                        107.5,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        1.4,
                        1.9,
                        2.4,
                        3.0,
                        3.5,
                        4.0,
                        4.5,
                        5.0,
                        5.7,
                        6.5,
                        8.0,
                        9.5,
                        12.0,
                        15.5,
                        19.0,
                        23.0,
                        27.5,
                        33.0,
                        40.0,
                        50.0,
                        60.0,
                        77.0,
                        95.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0
                    };
                    break;

                case 65: // Same depletion rate at SP5, but change to 2.5% sell increments
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        2.5,
                        5.0,
                        7.5,
                        10.0,
                        12.5,
                        15.0,
                        17.5,
                        20.0,
                        22.5,
                        25.0,
                        27.5,
                        30.0,
                        32.5,
                        35.0,
                        37.5,
                        40.0,
                        42.5,
                        45.0,
                        47.5,
                        50.0,
                        52.5,
                        55.0,
                        57.5,
                        60.0,
                        62.5,
                        65.0,
                        67.5,
                        70.0,
                        72.5,
                        75.0,
                        77.5,
                        80.0,
                        82.5,
                        85.0,
                        87.5,
                        90.0,
                        92.5,
                        95.0,
                        97.5,
                        100.0,
                        102.5,
                        105.0,
                        107.5,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        1.2,
                        3.1,
                        4.9,
                        8.0,
                        11.1,
                        14.5,
                        17.9,
                        21.9,
                        25.9,
                        29.6,
                        33.3,
                        36.7,
                        40.1,
                        43.2,
                        46.3,
                        48.8,
                        51.2,
                        53.1,
                        54.9,
                        56.2,
                        57.4,
                        58.3,
                        59.3,
                        59.9,
                        60.5,
                        61.1,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7,
                        61.7
                    };
                    break;

                case 66: // Same depletion rate at SP8, but change to 2.5% sell increments
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        2.5,
                        5.0,
                        7.5,
                        10.0,
                        12.5,
                        15.0,
                        17.5,
                        20.0,
                        22.5,
                        25.0,
                        27.5,
                        30.0,
                        32.5,
                        35.0,
                        37.5,
                        40.0,
                        42.5,
                        45.0,
                        47.5,
                        50.0,
                        52.5,
                        55.0,
                        57.5,
                        60.0,
                        62.5,
                        65.0,
                        67.5,
                        70.0,
                        72.5,
                        75.0,
                        77.5,
                        80.0,
                        82.5,
                        85.0,
                        87.5,
                        90.0,
                        92.5,
                        95.0,
                        97.5,
                        100.0,
                        102.5,
                        105.0,
                        107.5,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        0.7,
                        4.5,
                        10.0,
                        20.0,
                        32.0,
                        52.1,
                        70.0,
                        80.0,
                        85.7,
                        90.0,
                        93.5,
                        96.4,
                        98.5,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0
                    };
                    break;

                case 67: // Same depletion rate at SP8, but change to 2% sell increments
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        2.0,
                        4.0,
                        6.0,
                        8.0,
                        10.0,
                        12.0,
                        14.0,
                        16.0,
                        18.0,
                        20.0,
                        22.0,
                        24.0,
                        26.0,
                        28.0,
                        30.0,
                        32.0,
                        34.0,
                        36.0,
                        38.0,
                        40.0,
                        42.0,
                        44.0,
                        46.0,
                        48.0,
                        50.0,
                        52.0,
                        54.0,
                        56.0,
                        58.0,
                        60.0,
                        62.0,
                        64.0,
                        66.0,
                        68.0,
                        70.0,
                        72.0,
                        74.0,
                        76.0,
                        78.0,
                        80.0,
                        82.0,
                        84.0,
                        86.0,
                        88.0,
                        90.0,
                        92.0,
                        94.0,
                        96.0,
                        98.0,
                        100.0,
                        102.0,
                        104.0,
                        106.0,
                        108.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2.0,
                        4.0,
                        7.0,
                        12.0,
                        19.5,
                        30.5,
                        43.0,
                        61.5,
                        80.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0
                    };
                    break;

                case 68: // SP59 but modify end of curve from 1 to 100
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        2.5,
                        5.0,
                        7.5,
                        10.0,
                        12.5,
                        15.0,
                        17.5,
                        20.0,
                        22.5,
                        25.0,
                        27.5,
                        30.0,
                        32.5,
                        35.0,
                        37.5,
                        40.0,
                        42.5,
                        45.0,
                        47.5,
                        50.0,
                        52.5,
                        55.0,
                        57.5,
                        60.0,
                        62.5,
                        65.0,
                        67.5,
                        70.0,
                        72.5,
                        75.0,
                        77.5,
                        80.0,
                        82.5,
                        85.0,
                        87.5,
                        90.0,
                        92.5,
                        95.0,
                        97.5,
                        100.0,
                        102.5,
                        105.0,
                        107.5,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        0.0,
                        10.0,
                        20.0,
                        40.0,
                        80.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0
                    };
                    break;
                case 69: // SP with gradual increments and stable plateau
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        1.0,
                        2.0,
                        3.0,
                        4.0,
                        5.0,
                        6.0,
                        7.0,
                        8.0,
                        9.0,
                        10.0,
                        11.0,
                        12.0,
                        13.0,
                        14.0,
                        15.0,
                        16.0,
                        17.0,
                        18.0,
                        19.0,
                        20.0,
                        21.0,
                        22.0,
                        23.0,
                        24.0,
                        25.0,
                        26.0,
                        27.0,
                        28.0,
                        29.0,
                        30.0,
                        31.0,
                        32.0,
                        33.0,
                        34.0,
                        35.0,
                        36.0,
                        37.0,
                        38.0,
                        39.0,
                        40.0,
                        41.0,
                        42.0,
                        43.0,
                        44.0,
                        45.0,
                        46.0,
                        47.0,
                        48.0,
                        49.0,
                        50.0,
                        51.0,
                        52.0,
                        53.0,
                        54.0,
                        55.0,
                        56.0,
                        57.0,
                        58.0,
                        59.0,
                        60.0,
                        61.0,
                        62.0,
                        63.0,
                        64.0,
                        65.0,
                        66.0,
                        67.0,
                        68.0,
                        69.0,
                        70.0,
                        71.0,
                        72.0,
                        73.0,
                        74.0,
                        75.0,
                        76.0,
                        77.0,
                        78.0,
                        79.0,
                        80.0,
                        81.0,
                        82.0,
                        83.0,
                        84.0,
                        85.0,
                        86.0,
                        87.0,
                        88.0,
                        89.0,
                        90.0,
                        91.0,
                        92.0,
                        93.0,
                        94.0,
                        95.0,
                        96.0,
                        97.0,
                        98.0,
                        99.0,
                        100.0,
                        101.0,
                        102.0,
                        103.0,
                        104.0,
                        105.0,
                        106.0,
                        107.0,
                        108.0,
                        109.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        25.0,
                        25.0,
                        25.0,
                        25.0,
                        25.0,
                        50.0,
                        50.0,
                        50.0,
                        50.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0
                    };
                    break;

                case 71: // Constant distribution rate
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        1.0,
                        2.0,
                        3.0,
                        4.0,
                        5.0,
                        6.0,
                        7.0,
                        8.0,
                        9.0,
                        10.0,
                        11.0,
                        12.0,
                        13.0,
                        14.0,
                        15.0,
                        16.0,
                        17.0,
                        18.0,
                        19.0,
                        20.0,
                        21.0,
                        22.0,
                        23.0,
                        24.0,
                        25.0,
                        26.0,
                        27.0,
                        28.0,
                        29.0,
                        30.0,
                        31.0,
                        32.0,
                        33.0,
                        34.0,
                        35.0,
                        36.0,
                        37.0,
                        38.0,
                        39.0,
                        40.0,
                        41.0,
                        42.0,
                        43.0,
                        44.0,
                        45.0,
                        46.0,
                        47.0,
                        48.0,
                        49.0,
                        50.0,
                        51.0,
                        52.0,
                        53.0,
                        54.0,
                        55.0,
                        56.0,
                        57.0,
                        58.0,
                        59.0,
                        60.0,
                        61.0,
                        62.0,
                        63.0,
                        64.0,
                        65.0,
                        66.0,
                        67.0,
                        68.0,
                        69.0,
                        70.0,
                        71.0,
                        72.0,
                        73.0,
                        74.0,
                        75.0,
                        76.0,
                        77.0,
                        78.0,
                        79.0,
                        80.0,
                        81.0,
                        82.0,
                        83.0,
                        84.0,
                        85.0,
                        86.0,
                        87.0,
                        88.0,
                        89.0,
                        90.0,
                        91.0,
                        92.0,
                        93.0,
                        94.0,
                        95.0,
                        96.0,
                        97.0,
                        98.0,
                        99.0,
                        100.0,
                        101.0,
                        102.0,
                        103.0,
                        104.0,
                        105.0,
                        106.0,
                        107.0,
                        108.0,
                        109.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>(Enumerable.Repeat(5.0, 111)); // Constant value of 5.0
                    break;
                case 72: // 2.5% sell increments
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        2.5,
                        5.0,
                        7.5,
                        10.0,
                        12.5,
                        15.0,
                        17.5,
                        20.0,
                        22.5,
                        25.0,
                        27.5,
                        30.0,
                        32.5,
                        35.0,
                        37.5,
                        40.0,
                        42.5,
                        45.0,
                        47.5,
                        50.0,
                        52.5,
                        55.0,
                        57.5,
                        60.0,
                        62.5,
                        65.0,
                        67.5,
                        70.0,
                        72.5,
                        75.0,
                        77.5,
                        80.0,
                        82.5,
                        85.0,
                        87.5,
                        90.0,
                        92.5,
                        95.0,
                        97.5,
                        100.0,
                        102.5,
                        105.0,
                        107.5,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2.5,
                        2.5,
                        2.5,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0
                    };
                    break;

                case 73: // Linear profile with consistent shares
                    sellProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        5.0,
                        10.0,
                        15.0,
                        20.0,
                        25.0,
                        30.0,
                        35.0,
                        40.0,
                        45.0,
                        50.0,
                        55.0,
                        60.0,
                        65.0,
                        70.0,
                        75.0,
                        80.0,
                        85.0,
                        90.0,
                        95.0,
                        100.0,
                        105.0,
                        110.0
                    };
                    sellProfilePctOfAvailShares = new List<double>
                    {
                        2.5,
                        2.5,
                        2.5,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0,
                        5.0
                    };
                    break;

                // Repeat similar logic for profiles 10 through 73
                default:
                    throw new ArgumentException($"Sell profile {sellProfile} is not supported.");
            }

            if (settings.VerboseFlag)
            {
                statusUpdater.UpdateStatus($"    *** EVALUATING SELL PROFILE NO: {sellProfile} ***");
            }

            // Truncate Sell Profile PctOfAvailShares above a specified level
            if (settings.SellProfileHighEndTruncateFlag)
            {
                for (int sp = 0; sp < sellProfilePctOfAvailShares.Count; sp++)
                {
                    if (sellProfilePctOfAvailShares[sp] > settings.SellProfileHighEndTruncateLevel)
                    {
                        sellProfilePctOfAvailShares[sp] = settings.SellProfileHighEndTruncateLevel;
                    }
                }
            }

            // Truncate Sell Profile PctOfAvailShares below a specified level
            if (settings.SellProfileLowEndTruncateFlag)
            {
                for (int i = sellProfileRelativeMrktLevel.Count - 1; i >= 0; i--)
                {
                    if (sellProfileRelativeMrktLevel[i] <= settings.SellProfileLowEndTruncateLevel)
                    {
                        // The .RemoveAt method is used to remove an element from a collection (such as a List<T>) at a specified index.  It modifies the list by removing the element at the specified index and shifting all subsequent elements one position to the left.
                        sellProfileRelativeMrktLevel.RemoveAt(i);
                        sellProfilePctOfAvailShares.RemoveAt(i);
                    }
                }
            }

            // Create/Reset Sell Criteria Array
            for (int i = 0; i < sellProfileRelativeMrktLevel.Count; i++)
            {
                sellCriteriaReset.Add(new double[] { sellProfileRelativeMrktLevel[i], sellProfilePctOfAvailShares[i], -settings.CriteriaDaysSinceLastSellTransactionAtSameLevelDefault });
            }

            // Introduce fake/CNN sell criteria into the front end of the sellCriteria Array to alter the sell regime
            if (settings.SpuriousSellFlag)
            {
                var spuriousSellCriteriaList = new List<double[]>();
                foreach (var spurious in settings.SpuriousSellCriteria)
                {
                    spuriousSellCriteriaList.Add(Array.ConvertAll(spurious, item => (double)item));
                }

                // Insert the spuriousSellCriteria at the beginning of the sellCriteriaReset list
                sellCriteriaReset.InsertRange(0, spuriousSellCriteriaList);
            }

            return new SellProfileResult { SellProfileRelativeMrktLevel = sellCriteriaReset.Select(criteria => criteria[0]).ToList(), SellProfilePctOfAvailShares = sellCriteriaReset.Select(criteria => criteria[1]).ToList(), SellCriteriaReset = sellCriteriaReset };
        }

        public static BuyProfileResult BuyProfileGeneration(BackTestSettings settings, int buyProfile, IStatusUpdater statusUpdater)
        {
            var buyProfileRelativeMrktLevel = new List<double>();
            var buyProfilePctOfAvailFunds = new List<double>();
            var buyCriteriaReset = new List<double[]>();

            // Define buy profiles
            switch (buyProfile)
            {
                case 1:
                    buyProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        -5,
                        -10,
                        -20,
                        -30,
                        -40,
                        -50,
                        -60,
                        -70,
                        -80,
                        -90,
                        -100,
                        -110
                    };
                    buyProfilePctOfAvailFunds = new List<double>
                    {
                        2,
                        4,
                        7,
                        16,
                        32,
                        64,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 2:
                    buyProfileRelativeMrktLevel = new List<double>
                    {
                        0,
                        -5,
                        -10,
                        -15,
                        -20,
                        -25,
                        -30,
                        -35,
                        -40,
                        -45,
                        -50,
                        -55,
                        -60,
                        -65,
                        -70,
                        -75,
                        -80,
                        -85,
                        -90,
                        -95,
                        -100,
                        -105,
                        -110
                    };
                    buyProfilePctOfAvailFunds = new List<double>
                    {
                        5,
                        10,
                        16,
                        23,
                        31,
                        41,
                        53,
                        65,
                        80,
                        95,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100,
                        100
                    };
                    break;
                case 64:
                    buyProfileRelativeMrktLevel = new List<double>
                    {
                        0.0,
                        -2.5,
                        -5.0,
                        -7.5,
                        -10.0,
                        -12.5,
                        -15.0,
                        -17.5,
                        -20.0,
                        -22.5,
                        -25.0,
                        -27.5,
                        -30.0,
                        -32.5,
                        -35.0,
                        -37.5,
                        -40.0,
                        -42.5,
                        -45.0,
                        -47.5,
                        -50.0,
                        -52.5,
                        -55.0,
                        -57.5,
                        -60.0,
                        -62.5,
                        -65.0,
                        -67.5,
                        -70.0,
                        -72.5,
                        -75.0,
                        -77.5,
                        -80.0,
                        -82.5,
                        -85.0,
                        -87.5,
                        -90.0,
                        -92.5,
                        -95.0,
                        -97.5,
                        -100.0,
                        -102.5,
                        -105.0,
                        -107.5,
                        -110.0
                    };
                    buyProfilePctOfAvailFunds = new List<double>
                    {
                        1.4,
                        1.9,
                        2.4,
                        3.0,
                        3.5,
                        4.0,
                        4.5,
                        5.0,
                        5.7,
                        6.5,
                        8.0,
                        9.5,
                        12.0,
                        15.5,
                        19.0,
                        23.0,
                        27.5,
                        33.0,
                        40.0,
                        50.0,
                        60.0,
                        77.0,
                        95.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0,
                        100.0
                    };
                    break;
                default:
                    throw new ArgumentException($"Buy profile {buyProfile} is not supported.");
            }

            // Log verbose output if enabled
            if (settings.VerboseFlag)
            {
                statusUpdater.UpdateStatus($"    *** EVALUATING BUY PROFILE NO: {buyProfile} ***");
            }

            // Truncate buy profile percentages above a specified level
            if (settings.BuyProfileHighEndTruncateFlag)
            {
                for (int i = 0; i < buyProfilePctOfAvailFunds.Count; i++)
                {
                    if (buyProfilePctOfAvailFunds[i] > settings.BuyProfileHighEndTruncateLevel)
                    {
                        buyProfilePctOfAvailFunds[i] = settings.BuyProfileHighEndTruncateLevel;
                    }
                }
            }

            // Truncate buy profile percentages below a specified level
            if (settings.BuyProfileLowEndTruncateFlag)
            {
                for (int i = buyProfileRelativeMrktLevel.Count - 1; i >= 0; i--)
                {
                    if (buyProfileRelativeMrktLevel[i] >= settings.BuyProfileLowEndTruncateLevel)
                    {
                        buyProfileRelativeMrktLevel.RemoveAt(i);
                        buyProfilePctOfAvailFunds.RemoveAt(i);
                    }
                }
            }

            // Populate buy criteria reset
            for (int i = 0; i < buyProfileRelativeMrktLevel.Count; i++)
            {
                buyCriteriaReset.Add(new double[] { buyProfileRelativeMrktLevel[i], buyProfilePctOfAvailFunds[i], -settings.CriteriaDaysSinceLastBuyTransactionAtSameLevelDefault });
            }

            return new BuyProfileResult { BuyProfileRelativeMrktLevel = buyProfileRelativeMrktLevel, BuyProfilePctOfAvailFunds = buyProfilePctOfAvailFunds, BuyCriteriaReset = buyCriteriaReset };
        }

        public static StrategyResult StrategyGeneration(BackTestSettings settings, int strategy20Flag, int strategy, List<double[]> sellCriteriaReset, List<double[]> buyCriteriaReset, IStatusUpdater statusUpdater)
        {
            double sellThreshold = 0;
            double buyThreshold = 0;
            //var sellCriteria = new List<double[]>(sellCriteriaReset);
            //var buyCriteria = new List<double[]>(buyCriteriaReset);
            var sellCriteria = sellCriteriaReset.Select(arr => arr.ToArray()).ToList();
            var buyCriteria = buyCriteriaReset.Select(arr => arr.ToArray()).ToList();

            // Log verbose output if enabled
            if (settings.VerboseFlag)
            {
                statusUpdater.UpdateStatus($"    *** EVALUATING STRATEGY NO: {strategy} ***");
            }

            switch (strategy)
            {
                case 1:
                    sellThreshold = 0.0;
                    buyThreshold = sellThreshold - 5.0;
                    break;
                case 2:
                    sellThreshold = 5.0;
                    buyThreshold = sellThreshold - 5.0;
                    break;
                case 20:
                    sellThreshold = 0.0;
                    buyThreshold = sellThreshold - 5.0;
                    sellCriteria = new List<double[]> { new double[] { 0.0, 0.0, -settings.CriteriaDaysSinceLastBuyTransactionAtSameLevelDefault } };
                    buyCriteria = new List<double[]> { new double[] { 0.0, 100.0, -settings.CriteriaDaysSinceLastBuyTransactionAtSameLevelDefault } };
                    strategy20Flag++;
                    break;
                case 40:
                    sellThreshold = -10.0;
                    buyThreshold = sellThreshold - 5.0;
                    break;
                default:
                    throw new ArgumentException($"Strategy {strategy} is not supported.");
            }

            return new StrategyResult
            {
                SellThreshold = sellThreshold,
                BuyThreshold = buyThreshold,
                SellCriteria = sellCriteria,
                BuyCriteria = buyCriteria,
                Strategy20Flag = strategy20Flag
            };
        }

        public static List<InflationData> ReadInflationData(string inflationDataFile, string sheetName, string range, IStatusUpdater statusUpdater)
        {
            List<InflationData> inflationDataList = new List<InflationData>();

            // Polyform Noncommercial license for EPPlus
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            // EPPlus requires ExcelPackage to be created within a 'using' block to handle cleanup
            using (var package = new ExcelPackage(new FileInfo(inflationDataFile)))
            {
                var worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet == null)
                {
                    throw new Exception("Sheet not found.");
                }

                string[] rangeParts = range.Split(':');
                var startCell = rangeParts[0];
                var endCell = rangeParts[1];

                CellIndex startCellPoint = GetRowColumnIndex(startCell);
                CellIndex endCellPoint = GetRowColumnIndex(endCell);

                for (int row = startCellPoint.Row; row <= endCellPoint.Row; row++)
                {
                    InflationData data = new InflationData();

                    if (worksheet.Cells[row, 1].Value != null)
                    {
                        data.Year = Convert.ToInt32(worksheet.Cells[row, 1].Value);
                    }

                    if (worksheet.Cells[row, 2].Value != null)
                    {
                        data.CPI = Convert.ToDouble(worksheet.Cells[row, 2].Value);
                    }

                    if (worksheet.Cells[row, 14].Value != null)
                    {
                        data.ValueOfDollar = Convert.ToDouble(worksheet.Cells[row, 14].Value);
                    }

                    if (worksheet.Cells[row, 18].Value != null)
                    {
                        data.AnnualSavingsAmount = Convert.ToDouble(worksheet.Cells[row, 18].Value);
                    }

                    if (data.Year != 0)
                    {
                        inflationDataList.Add(data);
                    }
                }
            }

            return inflationDataList;
        }

        public static CellIndex GetRowColumnIndex(string cellReference)
        {
            int rowIndex = 0;
            int colIndex = 0;

            // Split the reference into letters (column) and digits (row)
            int splitIndex = 0;
            while (splitIndex < cellReference.Length && char.IsLetter(cellReference[splitIndex]))
            {
                splitIndex++;
            }

            // Extract column letters and convert to number
            string colPart = cellReference.Substring(0, splitIndex);
            foreach (char c in colPart)
            {
                colIndex = colIndex * 26 + (c - 'A' + 1);
            }

            // Extract row numbers and convert to integer
            string rowPart = cellReference.Substring(splitIndex);
            if (!string.IsNullOrEmpty(rowPart))
            {
                rowIndex = int.Parse(rowPart);
            }

            return new CellIndex { Row = rowIndex, Column = colIndex };
        }

        public class CellIndex
        {
            public int Row { get; set; }
            public int Column { get; set; }
        }

        public static void EnsureArrayCompatibility(ref List<double> mainInvestmentCloseDateNumber, ref List<double> complementaryInvestmentCloseDateNumber, ref List<double> mainInvestmentClosePrice, ref List<double> complementaryInvestmentClosePrice, ref List<DateTime> mainInvestmentCloseDate, ref List<DateTime> complementaryInvestmentCloseDate, ref List<int> mainInvestmentYearArray, ref List<int> complementaryInvestmentYearArray, IStatusUpdater statusUpdater)
        {
            List<int> idxOfElementsToRemoveMI;
            List<int> idxOfElementsToRemoveCI;

            if (mainInvestmentCloseDateNumber[0] <= complementaryInvestmentCloseDateNumber[0])
            {
                RectifyDateCompatibilityOfOverlappingDataSets(ref mainInvestmentCloseDateNumber, ref complementaryInvestmentCloseDateNumber, out idxOfElementsToRemoveMI, out idxOfElementsToRemoveCI);
            }
            else
            {
                RectifyDateCompatibilityOfOverlappingDataSets(ref complementaryInvestmentCloseDateNumber, ref mainInvestmentCloseDateNumber, out idxOfElementsToRemoveCI, out idxOfElementsToRemoveMI);
            }

            statusUpdater.UpdateStatus($"DATENUM SYNC RESULTS - Number of data points removed; MI: {idxOfElementsToRemoveMI.Count}, CI: {idxOfElementsToRemoveCI.Count}");

            // Update related arrays for CI
            UpdateRelatedArrays(ref complementaryInvestmentCloseDate, idxOfElementsToRemoveCI);
            UpdateRelatedArrays(ref complementaryInvestmentClosePrice, idxOfElementsToRemoveCI);
            UpdateRelatedArrays(ref complementaryInvestmentYearArray, idxOfElementsToRemoveCI);

            // Update related arrays for MI
            UpdateRelatedArrays(ref mainInvestmentCloseDate, idxOfElementsToRemoveMI);
            UpdateRelatedArrays(ref mainInvestmentClosePrice, idxOfElementsToRemoveMI);
            UpdateRelatedArrays(ref mainInvestmentYearArray, idxOfElementsToRemoveMI);
        }

        private static void RectifyDateCompatibilityOfOverlappingDataSets(ref List<double> A, ref List<double> B, out List<int> idxOfElementsToRemoveFromA, out List<int> idxOfElementsToRemoveFromB)
        {
            idxOfElementsToRemoveFromA = new List<int>();
            idxOfElementsToRemoveFromB = new List<int>();

            // Identify common and non-common elements
            var commonElements = new HashSet<double>(A.Intersect(B));

            idxOfElementsToRemoveFromA = A.Select((val, idx) => commonElements.Contains(val) ? -1 : idx).Where(idx => idx != -1).ToList();

            idxOfElementsToRemoveFromB = B.Select((val, idx) => commonElements.Contains(val) ? -1 : idx).Where(idx => idx != -1).ToList();

            // Remove non-common elements
            // Create local variables to store the indexes to remove, avoiding direct usage of 'ref' or 'out' in LINQ
            List<int> localIdxOfElementsToRemoveFromA = idxOfElementsToRemoveFromA;
            List<int> localIdxOfElementsToRemoveFromB = idxOfElementsToRemoveFromB;

            // Update A by filtering out elements at the specified indexes
            A = A.Where((_, idx) => !localIdxOfElementsToRemoveFromA.Contains(idx)).ToList();

            // Update B by filtering out elements at the specified indexes
            B = B.Where((_, idx) => !localIdxOfElementsToRemoveFromB.Contains(idx)).ToList();
        }

        private static void UpdateRelatedArrays<T>(ref List<T> list, List<int> indexesToRemove)
        {
            list = list.Where((_, idx) => !indexesToRemove.Contains(idx)).ToList();
        }

        private static List<T> FilterByIndexes<T>(List<T> list, List<int> indexesToRemove)
        {
            return list.Where((_, index) => !indexesToRemove.Contains(index)).ToList();
        }

        public static void AdjustStartAndEndDateCriteria(ref List<DateTime> mainInvestmentCloseDate, ref List<double> mainInvestmentCloseDateNumber, ref List<double> mainInvestmentClosePrice, BackTestSettings settings, DateTime startDateAnalysis, DateTime complementaryInvestmentStartDateAnalysis, out double startDateNumberAnalysis, out double endDateNumberAnalysis, out double startDateNumberRegression, out double endDateNumberRegression, IStatusUpdater statusUpdater)
        {
            //=================================================================================================
            // Adjust startDateNumberAnalysis based on user input.
            // TODO: This routine should work but it is "redundant". The routine may not be needed and
            // is related to the start date value specified in the Asset_Profile.xlsx.  That value is probably not needed
            //=================================================================================================
            startDateNumberAnalysis = mainInvestmentCloseDateNumber[0];

            if (!settings.StartDateAnalysisFlag) //Calculate startDateNumberAnalysis based on current value of startDateAnalysis
            {
                //if (startDateNumberAnalysis != 0) 
                //{
                startDateNumberAnalysis = HelperMethods.ConvertFromDateToExcelDateNumber(startDateAnalysis);
                //}
                //else
                //{
                //    startDateNumberAnalysis = mainInvestmentCloseDateNumber[0];
                //}
            }
            else if (settings.StartDateAnalysisFlag) // Set to user defined start date. Set startDateNumberAnalysis to startDateNumberAnalysisUserOverride
            {
                double startDateNumberAnalysisUserOverride = HelperMethods.ConvertFromDateToExcelDateNumber(settings.StartDateAnalysisUserOverride);
                if (startDateNumberAnalysisUserOverride < startDateNumberAnalysis)
                {
                    throw new Exception("startDateAnalysisUserOverride is before available MainInvestment data.");
                }
                else
                {
                    startDateNumberAnalysis = startDateNumberAnalysisUserOverride;
                }
            }
            else
            {
                throw new ArgumentException("Invalid StartDateAnalysisFlag value.");
            }

            // In the case that user specified a date that in on a holiday or weekend, run this routine adjust it.
            int idx = FindNearestIndex(mainInvestmentCloseDateNumber, startDateNumberAnalysis);
            startDateNumberAnalysis = mainInvestmentCloseDateNumber[idx];
            statusUpdater.UpdateStatus($"  INFO: startDateNumberAnalysis set to: {mainInvestmentCloseDate[idx]:MM-dd-yyyy}");

            //=================================================================================================
            // Adjustments startDateNumberAnalysis based on Complimentary Investment data
            //=================================================================================================            
            if (settings.ComplementaryInvestmentFlag == 1 && HelperMethods.ConvertFromDateToExcelDateNumber(complementaryInvestmentStartDateAnalysis) > startDateNumberAnalysis)
            {
                startDateNumberAnalysis = HelperMethods.ConvertFromDateToExcelDateNumber(complementaryInvestmentStartDateAnalysis);
                statusUpdater.UpdateStatus($"  INFO: startDateNumberAnalysis adjusted to: {complementaryInvestmentStartDateAnalysis:MM-dd-yyyy}");
            }

            //=================================================================================================
            // Adjust endDateNumberAnalysis based on user inputs
            //=================================================================================================
            endDateNumberAnalysis = mainInvestmentCloseDateNumber[mainInvestmentCloseDateNumber.Count - 1];

            if (settings.EndDateAnalysisFlag) // Use user defined end date
            {
                // Use user selected end date
                double endDateNumberAnalysisUserOverride = HelperMethods.ConvertFromDateToExcelDateNumber(settings.EndDateAnalysisUserOverride);
                if (endDateNumberAnalysisUserOverride > endDateNumberAnalysis)
                {
                    throw new Exception("endDateAnalysisUserOverride is after available MainInvestment data."); // Do not change current value for endDateNumberAnalysis assigned in the mainInvestmentToRun loop. Warn the user of his mistake.
                }
                else
                {
                    endDateNumberAnalysis = endDateNumberAnalysisUserOverride; // Set to user defined start date.
                }
            }

            // In the case that user specified a date that in on a holiday or weekend, run this routine adjust it.
            idx = FindNearestIndex(mainInvestmentCloseDateNumber, endDateNumberAnalysis);
            endDateNumberAnalysis = mainInvestmentCloseDateNumber[idx];
            statusUpdater.UpdateStatus($"  INFO: endDateNumberAnalysis set to: {mainInvestmentCloseDate[idx]:MM-dd-yyyy}");

            //=================================================================================================
            // Adjust startDateRegressionFlag based on user inputs
            //================================================================================================
            if (settings.StartDateRegressionFlag == 0) // Retain start date from the original MainInvestment data.  No additional calculations needed
            {
                startDateNumberRegression = mainInvestmentCloseDateNumber[0];
            }
            else if (settings.StartDateRegressionFlag == 1) // Use same start date as the start date for the analysis
            {
                startDateNumberRegression = startDateNumberAnalysis;
            }
            else if (settings.StartDateRegressionFlag == 2) // Use user specified regression start date
            {
                double regressionOverride = HelperMethods.ConvertFromDateToExcelDateNumber(settings.StartDateRegressionUserOverride);
                if (regressionOverride > startDateNumberAnalysis)
                {
                    startDateNumberRegression = startDateNumberAnalysis;
                }
                else
                {
                    startDateNumberRegression = regressionOverride;
                }
            }
            else
            {
                throw new ArgumentException("Invalid StartDateRegressionFlag value.");
            }

            // In the case that user specified a date that in on a holiday or weekend, run this routine adjust it.
            idx = FindNearestIndex(mainInvestmentCloseDateNumber, startDateNumberRegression);
            startDateNumberRegression = mainInvestmentCloseDateNumber[idx];
            statusUpdater.UpdateStatus($"  INFO: startDateNumberRegression set to: {mainInvestmentCloseDate[idx]:MM-dd-yyyy}");

            //=================================================================================================
            // Adjust endDateRegressionFlag based on user inputs
            //=================================================================================================
            if (settings.EndDateRegressionFlag == 0) // Retain end date from the original MainInvestment data.  No additional calculations needed
            {
                endDateNumberRegression = mainInvestmentCloseDateNumber[mainInvestmentCloseDateNumber.Count - 1];
            }
            else if (settings.EndDateRegressionFlag == 1) // Use same end date as the end date for the analysis
            {
                endDateNumberRegression = endDateNumberAnalysis;
            }
            else if (settings.EndDateRegressionFlag == 2) // Use user specified regression end date
            {
                double regressionOverride = HelperMethods.ConvertFromDateToExcelDateNumber(settings.EndDateRegressionUserOverride);
                if (regressionOverride < endDateNumberAnalysis)
                {
                    endDateNumberRegression = endDateNumberAnalysis;
                }
                else
                {
                    endDateNumberRegression = regressionOverride;
                }
            }
            else
            {
                throw new ArgumentException("Invalid EndDateRegressionFlag value.");
            }

            // In the case that user specified a date that in on a holiday or weekend, run this routine adjust it.
            idx = FindNearestIndex(mainInvestmentCloseDateNumber, endDateNumberRegression);
            endDateNumberRegression = mainInvestmentCloseDateNumber[idx];
            statusUpdater.UpdateStatus($"  INFO: endDateNumberRegression set to: {mainInvestmentCloseDate[idx]:MM-dd-yyyy}");

            statusUpdater.UpdateStatus($"  MainInvestment original data properties ===================> Data Points: {mainInvestmentCloseDate.Count}, Start Date: {mainInvestmentCloseDate.First():MM-dd-yyyy}, End Date: {mainInvestmentCloseDate.Last():MM-dd-yyyy}");

            // Truncate the MainInvestment data based on the startDateNumberRegression and endDateNumberRegression calculated above
            // Delete the rows that are before specified startDateNumberRegression.
            // Delete the rows  that are after specified endDateNumberRegression.
            TruncateData(ref mainInvestmentCloseDate, ref mainInvestmentCloseDateNumber, ref mainInvestmentClosePrice, startDateNumberRegression, endDateNumberRegression);

            statusUpdater.UpdateStatus($"  MainInvestment data properties after date range adjustments  ==> Data Points: {mainInvestmentCloseDate.Count}, Start Date: {mainInvestmentCloseDate.First():MM-dd-yyyy}, End Date: {mainInvestmentCloseDate.Last():MM-dd-yyyy}");
        }

        private static int FindNearestIndex(List<double> dateNumList, double targetDateNum)
        {
            if (dateNumList == null || dateNumList.Count == 0)
                throw new ArgumentException("dateNums cannot be null or empty.");

            return dateNumList.Select((date, index) => new { Date = date, Index = index }).OrderBy(item => Math.Abs(item.Date - targetDateNum)).First().Index;
        }

        private static void TruncateData(ref List<DateTime> maininvestmentCloseDate, ref List<double> mainInvestmentCloseDateNumber, ref List<double> mainInvestmentClosePrice, double startDateNum, double endDateNum)
        {
            int startIndex = mainInvestmentCloseDateNumber.FindIndex(date => date >= startDateNum);
            int endIndex = mainInvestmentCloseDateNumber.FindLastIndex(date => date <= endDateNum);

            maininvestmentCloseDate = maininvestmentCloseDate.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
            mainInvestmentCloseDateNumber = mainInvestmentCloseDateNumber.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
            mainInvestmentClosePrice = mainInvestmentClosePrice.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
        }

        public static List<double> InvestmentDataFilteringAlgorithm(bool mainInvestmentFilterFlag, bool plotFlagHistogram, bool verboseFlag, string mainInvestmentName, List<double> mainInvestmentCloseDateNumber, List<double> mainInvestmentClosePrice, int numberOfFilteringIterations, double numberOfFilteringStdDevs, IStatusUpdater statusUpdater)
        {
            if (!mainInvestmentFilterFlag)
            {
                numberOfFilteringStdDevs = 0.0;
                numberOfFilteringIterations = 0;
                return mainInvestmentClosePrice;
            }
            else if (mainInvestmentFilterFlag)
            {
                if (verboseFlag)
                {
                    statusUpdater.UpdateStatus($"   You requested MainInvestment Filtering...");
                }

                var mainInvestmentValuationWRTZero = new List<double>(new double[mainInvestmentCloseDateNumber.Count]);

                for (int j = 0; j < numberOfFilteringIterations; j++)
                {
                    var mainInvestmentClosePriceLog10 = mainInvestmentClosePrice.Select(x => Math.Log10(x)).ToList();
                    var coeffsCloseValueLog10 = PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10);
                    var fitCloseValue = EvaluatePolynomial(coeffsCloseValueLog10, mainInvestmentCloseDateNumber);

                    for (int i = 0; i < mainInvestmentCloseDateNumber.Count; i++)
                    {
                        mainInvestmentValuationWRTZero[i] = ((mainInvestmentClosePrice[i] - Math.Pow(10, fitCloseValue[i])) / Math.Pow(10, fitCloseValue[i])) * 100.0;
                    }

                    double stdDev = CalculateStandardDeviation(mainInvestmentValuationWRTZero);
                    double meanValue = mainInvestmentValuationWRTZero.Average();
                    double minRange = mainInvestmentValuationWRTZero.Min();
                    double maxRange = mainInvestmentValuationWRTZero.Max();

                    if (verboseFlag)
                        statusUpdater.UpdateStatus($"    {mainInvestmentName} MainInvestment Filtering Iteration No: {j}");

                    double lowSideFilter = -numberOfFilteringStdDevs * stdDev;
                    double highSideFilter = numberOfFilteringStdDevs * stdDev;

                    for (int i = 0; i < mainInvestmentValuationWRTZero.Count; i++)
                    {
                        if (mainInvestmentValuationWRTZero[i] < lowSideFilter)
                            mainInvestmentValuationWRTZero[i] = lowSideFilter;
                        else if (mainInvestmentValuationWRTZero[i] > highSideFilter)
                            mainInvestmentValuationWRTZero[i] = highSideFilter;
                    }

                    for (int i = 0; i < mainInvestmentCloseDateNumber.Count; i++)
                    {
                        mainInvestmentClosePrice[i] = (mainInvestmentValuationWRTZero[i] / 100.0 * Math.Pow(10, fitCloseValue[i])) + Math.Pow(10, fitCloseValue[i]);
                    }
                }

                return mainInvestmentClosePrice;
            }

            throw new ArgumentException("Invalid mainInvestmentFilterFlag value.");
        }

        //public static void Regression(int verboseFlag, string mainInvestmentName, List<double> mainInvestmentCloseDateNumber, List<double> mainInvestmentClosePrice, out List<double> mainInvestmentValuationWRTZero, out List<double> mainInvestmentRegressionValue, out List<double> mainInvestmentClosePriceLog10, out double stdDevFinal, out double meanValueFinal, out double minRangeFinal, out double maxRangeFinal, out List<double> fitCloseValue, out List<double> coeffsCloseValueLog10, IStatusUpdater statusUpdater)
        //{
        //    mainInvestmentClosePriceLog10 = mainInvestmentClosePrice.Select(x => Math.Log10(x)).ToList();

        //    coeffsCloseValueLog10 = PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10);
        //    fitCloseValue = EvaluatePolynomial(coeffsCloseValueLog10, mainInvestmentCloseDateNumber);

        //    // Use temporary lists to calculate and then assign the values
        //    List<double> tempRegressionValues = new List<double>();
        //    List<double> tempValuationWRTZero = new List<double>();

        //    // Calculate mainInvestmentRegressionValue
        //    foreach (var x in fitCloseValue)
        //    {
        //        tempRegressionValues.Add(Math.Pow(10, x));
        //    }

        //    // Calculate mainInvestmentValuationWRTZero
        //    for (int i = 0; i < mainInvestmentClosePrice.Count; i++)
        //    {
        //        double valuation = ((mainInvestmentClosePrice[i] - tempRegressionValues[i]) / tempRegressionValues[i]) * 100.0;
        //        tempValuationWRTZero.Add(valuation);
        //    }

        //    // Assign the results to the ref parameters
        //    mainInvestmentRegressionValue = tempRegressionValues;
        //    mainInvestmentValuationWRTZero = tempValuationWRTZero;

        //    stdDevFinal = CalculateStandardDeviation(mainInvestmentValuationWRTZero);
        //    meanValueFinal = mainInvestmentValuationWRTZero.Average();
        //    minRangeFinal = mainInvestmentValuationWRTZero.Min();
        //    maxRangeFinal = mainInvestmentValuationWRTZero.Max();

        //    if (verboseFlag == 1)
        //    {
        //        statusUpdater.UpdateStatus($"     {mainInvestmentName} MainInvestment Data Properties ==> Data Points: {mainInvestmentClosePrice.Count}, Mean: {meanValueFinal:F1}, Stdev: {stdDevFinal:F1}, Valuation Range: Min {minRangeFinal:F1}, Max {maxRangeFinal:F1}");
        //    }
        //}

        public static void Regression(bool verboseFlag, string mainInvestmentName, List<double> mainInvestmentCloseDateNumber, List<double> mainInvestmentClosePrice, out List<double> mainInvestmentValuationWRTZero, out List<double> mainInvestmentRegressionValue, out List<double> mainInvestmentClosePriceLog10, out double stdDevFinal, out double meanValueFinal, out double minRangeFinal, out double maxRangeFinal, out List<double> fitCloseValue, out List<double> fitCloseValueMinus60Pct, out List<double> fitCloseValueMinus50Pct, out List<double> fitCloseValueMinus40Pct, out List<double> fitCloseValueMinus30Pct, out List<double> fitCloseValueMinus20Pct, out List<double> fitCloseValueMinus10Pct, out List<double> fitCloseValuePlus10Pct, out List<double> fitCloseValuePlus20Pct, out List<double> fitCloseValuePlus30Pct, out List<double> fitCloseValuePlus40Pct, out List<double> fitCloseValuePlus50Pct, out List<double> fitCloseValuePlus60Pct, out List<double> coeffsCloseValueLog10, IStatusUpdater statusUpdater)
        {
            mainInvestmentClosePriceLog10 = mainInvestmentClosePrice.Select(x => Math.Log10(x)).ToList();

            List<double> mainInvestmentClosePriceLog10Minus60Pct = mainInvestmentClosePrice.Select(x => Math.Log10(x * 0.4)).ToList();
            List<double> mainInvestmentClosePriceLog10Minus50Pct = mainInvestmentClosePrice.Select(x => Math.Log10(x * 0.5)).ToList();
            List<double> mainInvestmentClosePriceLog10Minus40Pct = mainInvestmentClosePrice.Select(x => Math.Log10(x * 0.6)).ToList();
            List<double> mainInvestmentClosePriceLog10Minus30Pct = mainInvestmentClosePrice.Select(x => Math.Log10(x * 0.7)).ToList();
            List<double> mainInvestmentClosePriceLog10Minus20Pct = mainInvestmentClosePrice.Select(x => Math.Log10(x * 0.8)).ToList();
            List<double> mainInvestmentClosePriceLog10Minus10Pct = mainInvestmentClosePrice.Select(x => Math.Log10(x * 0.9)).ToList();
            List<double> mainInvestmentClosePriceLog10Plus10Pct = mainInvestmentClosePrice.Select(x => Math.Log10(x * 1.1)).ToList();
            List<double> mainInvestmentClosePriceLog10Plus20Pct = mainInvestmentClosePrice.Select(x => Math.Log10(x * 1.2)).ToList();
            List<double> mainInvestmentClosePriceLog10Plus30Pct = mainInvestmentClosePrice.Select(x => Math.Log10(x * 1.3)).ToList();
            List<double> mainInvestmentClosePriceLog10Plus40Pct = mainInvestmentClosePrice.Select(x => Math.Log10(x * 1.4)).ToList();
            List<double> mainInvestmentClosePriceLog10Plus50Pct = mainInvestmentClosePrice.Select(x => Math.Log10(x * 1.5)).ToList();
            List<double> mainInvestmentClosePriceLog10Plus60Pct = mainInvestmentClosePrice.Select(x => Math.Log10(x * 1.6)).ToList();

            coeffsCloseValueLog10 = PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10);
            fitCloseValue = EvaluatePolynomial(coeffsCloseValueLog10, mainInvestmentCloseDateNumber);

            fitCloseValueMinus60Pct = EvaluatePolynomial(PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10Minus60Pct), mainInvestmentCloseDateNumber);
            fitCloseValueMinus50Pct = EvaluatePolynomial(PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10Minus50Pct), mainInvestmentCloseDateNumber);
            fitCloseValueMinus40Pct = EvaluatePolynomial(PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10Minus40Pct), mainInvestmentCloseDateNumber);
            fitCloseValueMinus30Pct = EvaluatePolynomial(PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10Minus30Pct), mainInvestmentCloseDateNumber);
            fitCloseValueMinus20Pct = EvaluatePolynomial(PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10Minus20Pct), mainInvestmentCloseDateNumber);
            fitCloseValueMinus10Pct = EvaluatePolynomial(PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10Minus10Pct), mainInvestmentCloseDateNumber);
            fitCloseValuePlus10Pct = EvaluatePolynomial(PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10Plus10Pct), mainInvestmentCloseDateNumber);
            fitCloseValuePlus20Pct = EvaluatePolynomial(PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10Plus20Pct), mainInvestmentCloseDateNumber);
            fitCloseValuePlus30Pct = EvaluatePolynomial(PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10Plus30Pct), mainInvestmentCloseDateNumber);
            fitCloseValuePlus40Pct = EvaluatePolynomial(PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10Plus40Pct), mainInvestmentCloseDateNumber);
            fitCloseValuePlus50Pct = EvaluatePolynomial(PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10Plus50Pct), mainInvestmentCloseDateNumber);
            fitCloseValuePlus60Pct = EvaluatePolynomial(PerformLinearRegression(mainInvestmentCloseDateNumber, mainInvestmentClosePriceLog10Plus60Pct), mainInvestmentCloseDateNumber);

            List<double> tempRegressionValues = fitCloseValue.Select(x => Math.Pow(10, x)).ToList();
            List<double> tempValuationWRTZero = new List<double>();

            for (int i = 0; i < mainInvestmentClosePrice.Count; i++)
            {
                double valuation = ((mainInvestmentClosePrice[i] - tempRegressionValues[i]) / tempRegressionValues[i]) * 100.0;
                tempValuationWRTZero.Add(valuation);
            }

            mainInvestmentRegressionValue = tempRegressionValues;
            mainInvestmentValuationWRTZero = tempValuationWRTZero;

            stdDevFinal = CalculateStandardDeviation(mainInvestmentValuationWRTZero);
            meanValueFinal = mainInvestmentValuationWRTZero.Average();
            minRangeFinal = mainInvestmentValuationWRTZero.Min();
            maxRangeFinal = mainInvestmentValuationWRTZero.Max();

            if (verboseFlag)
            {
                statusUpdater.UpdateStatus($"     {mainInvestmentName} MainInvestment Data Properties ==> Data Points: {mainInvestmentClosePrice.Count}, Mean: {meanValueFinal:F1}, Stdev: {stdDevFinal:F1}, Valuation Range: Min {minRangeFinal:F1}, Max {maxRangeFinal:F1}");
            }
        }

        public static List<double> PerformLinearRegression(List<double> xValues, List<double> yValues)
        {
            int n = xValues.Count;
            double xMean = xValues.Average();
            double yMean = yValues.Average();
            double xySum = xValues.Zip(yValues, (x, y) => (x - xMean) * (y - yMean)).Sum();
            double xxSum = xValues.Sum(x => Math.Pow(x - xMean, 2));

            double slope = xySum / xxSum;
            double intercept = yMean - slope * xMean;

            return new List<double> { slope, intercept };
        }

        public static List<double> EvaluatePolynomial(List<double> coeffs, List<double> xValues)
        {
            return xValues.Select(x => coeffs[0] * x + coeffs[1]).ToList();
        }

        private static double CalculateStandardDeviation(List<double> values)
        {
            double mean = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));
        }

        public static void SetPredeterminedTimeRanges(ref int numberOfTimePeriodLoops, string predeterminedTimeRangesFile, List<DateTime> dateLog, List<double> mainInvestmentCloseDateNumber, DateTime startDateNumberAnalysis, DateTime endDateNumberAnalysis, string user, IStatusUpdater statusUpdater)
        {
            // Read predetermined time ranges from file (stubbed since file handling varies)
            var predeterminedTimeRangesRaw = ReadExcelPredeterminedTimeRanges(predeterminedTimeRangesFile, "TimeRanges");

            int validCount = 0;
            var predeterminedTimeRangesRawFiltered = new List<TimeRange>(predeterminedTimeRangesRaw.Count);

            foreach (var range in predeterminedTimeRangesRaw)
            {
                if (range.StartDate == null)
                    continue;

                DateTime startDate = range.StartDate.Value;
                DateTime endDate = range.EndDate.Value;

                if (startDate < startDateNumberAnalysis)
                {
                    if (endDate <= startDateNumberAnalysis)
                        continue;
                    else
                        startDate = startDateNumberAnalysis;
                }

                if (endDate > endDateNumberAnalysis)
                    endDate = endDateNumberAnalysis;

                predeterminedTimeRangesRawFiltered.Add(new TimeRange(startDate, endDate));
                validCount++;
            }

            predeterminedTimeRangesRaw = predeterminedTimeRangesRawFiltered;

            // Add additional time frames
            for (int split = 1; split <= 4; split++)
            {
                AddAdditionalTimeFrames(predeterminedTimeRangesRaw, mainInvestmentCloseDateNumber, dateLog, startDateNumberAnalysis, endDateNumberAnalysis, split);
            }

            if (user.ToLower() == "joe")
            {
                AddAdditionalTimeFrames(predeterminedTimeRangesRaw, mainInvestmentCloseDateNumber, dateLog, startDateNumberAnalysis, endDateNumberAnalysis, 12);
            }

            if (predeterminedTimeRangesRaw.Count < numberOfTimePeriodLoops)
            {
                numberOfTimePeriodLoops = predeterminedTimeRangesRaw.Count;
            }

            statusUpdater.UpdateStatus("      Time Periods to be Run:");
            for (int i = 0; i < numberOfTimePeriodLoops; i++)
            {
                statusUpdater.UpdateStatus($"         From: {predeterminedTimeRangesRaw[i].StartDate:MM-dd-yyyy} To: {predeterminedTimeRangesRaw[i].EndDate:MM-dd-yyyy}");
            }
        }

        private static void AddAdditionalTimeFrames(List<TimeRange> predeterminedTimeRangesRaw, List<double> mainInvestmentCloseDateNumber, List<DateTime> dateLog, DateTime startDateNumberAnalysis, DateTime endDateNumberAnalysis, int numberOfSplits)
        {
            int currentCount = predeterminedTimeRangesRaw.Count;
            double dateNumSplitIncrement = (endDateNumberAnalysis.ToOADate() - startDateNumberAnalysis.ToOADate()) / numberOfSplits;

            for (int i = 0; i < numberOfSplits; i++)
            {
                DateTime? start = null;
                DateTime? end = null;

                if (i == 0)
                {
                    start = startDateNumberAnalysis;
                }
                else
                {
                    start = predeterminedTimeRangesRaw[currentCount + i - 1].EndDate;
                }

                if (i == numberOfSplits - 1)
                {
                    end = endDateNumberAnalysis;
                }
                else
                {
                    double targetDate = startDateNumberAnalysis.ToOADate() + dateNumSplitIncrement * (i + 1);
                    int idx = FindNearestIndexII(mainInvestmentCloseDateNumber, targetDate);
                    end = dateLog[idx];
                }

                predeterminedTimeRangesRaw.Add(new TimeRange(start, end));
            }
        }

        private static List<TimeRange> ReadExcelPredeterminedTimeRanges(string filePath, string sheetName)
        {
            // Stubbed method for reading Excel ranges (replace with actual SpreadsheetLight or OpenXML logic)
            return new List<TimeRange> { new TimeRange(new DateTime(2020, 1, 1), new DateTime(2021, 1, 1)), new TimeRange(new DateTime(2021, 1, 2), new DateTime(2022, 1, 1)) };
        }

        public class TimeRange
        {
            public DateTime? StartDate { get; set; }

            public DateTime? EndDate { get; set; }

            public TimeRange(DateTime? startDate, DateTime? endDate)
            {
                StartDate = startDate;
                EndDate = endDate;
            }
        }

        public static void RunTimePeriodAnalysis(
            //string resultsSummaryCSVFile,
            int timePeriod, BackTestSettings settings,
            //FileControlSettings fileSettings,
            double startDateNumberAnalysis, double endDateNumberAnalysis, double endDateNumberRegression, List<double> mainInvestmentCloseDateNumber, List<DateTime> dateLog, List<InflationData> inflationData,
            //ref int caseNo,
            out int runDurationInMarketDays, out DateTime startDateAnalysisThisRun, out DateTime endDateAnalysisThisRun, out double inflationRateAverageEntireTimePeriod, out int startingMarketDayThisRun, out int endingMarketDayThisRun, out double calculatedDurationInCalendarDaysForInflationCalc, out double calculatedDurationInCalendarDays)
        {
            // Use settings and fileSettings fields directly
            //string user = fileSettings.UserName;
            var numberOfTimePeriodLoops = settings.NumberOfTimePeriodLoops;
            var usePredeterminedTimeRangesFlag = settings.UsePredeterminedTimeRangesFlag;
            var inflationEndDateNumCalcFlag = settings.InflationEndDateNumCalcFlag;
            var endDateInflationCalcUserOverride = settings.EndDateInflationCalcUserOverride;
            var inflationRateFlag = settings.InflationRateFlag;
            var minTimePeriodDurationInCalendarDays = settings.MinTimePeriodDurationInCalendarDays;
            var maxRetirementDurationInCalendarDays = settings.MaxRetirementDurationInCalendarDays;
            var screenOutRangeFlag = settings.ScreenOutRangeFlag;
            var dateNumStayOutRangeStart = settings.DateNumStayOutRangeStart;
            var dateNumStayOutRangeEnd = settings.DateNumStayOutRangeEnd;
            //string caseTrackingFile = fileSettings.CaseTrackingFile;

            // Initialize out parameters
            startingMarketDayThisRun = -1;
            endingMarketDayThisRun = -1;
            startDateAnalysisThisRun = DateTime.MinValue;
            endDateAnalysisThisRun = DateTime.MinValue;
            runDurationInMarketDays = 0;
            //inflationRateAverageEntireTimePeriod = 0.0;

            var startDateNumberAnalysisThisRun = startDateNumberAnalysis;
            var endDateNumberAnalysisThisRun = endDateNumberAnalysis;
            calculatedDurationInCalendarDays = 0;

            if (numberOfTimePeriodLoops == 1)
            {
                startingMarketDayThisRun = FindNearestIndexII(mainInvestmentCloseDateNumber, startDateNumberAnalysis);
                endingMarketDayThisRun = FindNearestIndexII(mainInvestmentCloseDateNumber, endDateNumberAnalysis);
                calculatedDurationInCalendarDays = endDateNumberAnalysis - startDateNumberAnalysis + 1;
            }
            else if (numberOfTimePeriodLoops > 1 && !usePredeterminedTimeRangesFlag)
            {
                var random = new Random();
                var randomCalcIters = 0;
                calculatedDurationInCalendarDays = minTimePeriodDurationInCalendarDays - 1;

                while (calculatedDurationInCalendarDays < minTimePeriodDurationInCalendarDays || calculatedDurationInCalendarDays > maxRetirementDurationInCalendarDays)
                {
                    double startDateNumberAnalysisRandom, endDateNumberAnalysisRandom;

                    if (!screenOutRangeFlag)
                    {
                        startDateNumberAnalysisRandom = RandomDoubleInRange(random, startDateNumberAnalysis, endDateNumberAnalysis);
                        endDateNumberAnalysisRandom = RandomDoubleInRange(random, startDateNumberAnalysisRandom, endDateNumberAnalysis);
                    }
                    else
                    {
                        startDateNumberAnalysisRandom = RandomDoubleInRange(random, startDateNumberAnalysis, endDateNumberAnalysis);
                        while (startDateNumberAnalysisRandom > dateNumStayOutRangeStart && startDateNumberAnalysisRandom < dateNumStayOutRangeEnd)
                            startDateNumberAnalysisRandom = RandomDoubleInRange(random, startDateNumberAnalysis, endDateNumberAnalysis);

                        if (startDateNumberAnalysisRandom < dateNumStayOutRangeStart)
                            endDateNumberAnalysisRandom = RandomDoubleInRange(random, startDateNumberAnalysisRandom, dateNumStayOutRangeStart);
                        else
                            endDateNumberAnalysisRandom = RandomDoubleInRange(random, startDateNumberAnalysisRandom, endDateNumberAnalysis);
                    }

                    startingMarketDayThisRun = FindNearestIndexII(mainInvestmentCloseDateNumber, startDateNumberAnalysisRandom);
                    endingMarketDayThisRun = FindNearestIndexII(mainInvestmentCloseDateNumber, endDateNumberAnalysisRandom);
                    startDateNumberAnalysisThisRun = mainInvestmentCloseDateNumber[startingMarketDayThisRun];
                    endDateNumberAnalysisThisRun = mainInvestmentCloseDateNumber[endingMarketDayThisRun];
                    calculatedDurationInCalendarDays = endDateNumberAnalysisThisRun - startDateNumberAnalysisThisRun + 1;
                    randomCalcIters++;
                }
            }

            if (startingMarketDayThisRun >= 0 && endingMarketDayThisRun >= 0)
            {
                startDateAnalysisThisRun = dateLog[startingMarketDayThisRun];
                endDateAnalysisThisRun = dateLog[endingMarketDayThisRun];
                runDurationInMarketDays = endingMarketDayThisRun - startingMarketDayThisRun + 1;
            }
            else
            {
                throw new Exception("Failed to calculate valid start and end days for this run.");
            }

            // Calculate Inflation Parameters
            double endDateNumForInflationCalcInCalendarDays = 0;
            if (inflationEndDateNumCalcFlag == 0)
                endDateNumForInflationCalcInCalendarDays = DateTime.Now.ToOADate();
            else if (inflationEndDateNumCalcFlag == 1)
                endDateNumForInflationCalcInCalendarDays = endDateNumberRegression;
            else if (inflationEndDateNumCalcFlag == 2)
                endDateNumForInflationCalcInCalendarDays = HelperMethods.ConvertFromDateToExcelDateNumber(endDateInflationCalcUserOverride);

            calculatedDurationInCalendarDaysForInflationCalc = endDateNumForInflationCalcInCalendarDays - startDateNumberAnalysisThisRun + 1;
            inflationRateAverageEntireTimePeriod = CalculateInflationRate(inflationRateFlag, inflationData, startDateAnalysisThisRun.Year, DateTime.Now.Year);

            //// Update Case Number
            //if (user == "Owner" || user == "stick" || user == "joe")
            //{
            //    //TODO: Change this so that it writes to an excel file 
            //    caseNo++;
            //    WriteCaseToFile(caseTrackingFile, caseNo);
            //}

            //// Create logMessage and write to the .csv file
            //string logMessage = string.Format("    >{0}> RUNNING CASE {1} over date range {2:MM-dd-yyyy} to {3:MM-dd-yyyy}, Market Days: {4}, Market Yrs: {5:F1}, Start Year Analysis: {6}, Current Yr: {7}, Inflation Rate Average: {8:F2}",
            //    timePeriod,
            //    caseNo,
            //    startDateAnalysisThisRun,
            //    endDateAnalysisThisRun,
            //    runDurationInMarketDays,
            //    runDurationInMarketDays / 253.0,
            //    startDateAnalysisThisRun.Year,
            //    DateTime.Now.Year,
            //    inflationRateAverageEntireTimePeriod);

            //statusUpdater.UpdateStatus(logMessage);

            //using (var writer = new StreamWriter(resultsSummaryCSVFile, append: false))
            //{
            //    writer.WriteLine(logMessage);
            //}
        }

        private static int FindNearestIndexII(List<double> values, double target)
        {
            double minDiff = double.MaxValue;
            int idx = 0;

            for (int i = 0; i < values.Count; i++)
            {
                double diff = Math.Abs(values[i] - target);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    idx = i;
                }
            }

            return idx;
        }

        private static double RandomDoubleInRange(Random random, double min, double max)
        {
            return random.NextDouble() * (max - min) + min;
        }

        private static double CalculateInflationRate(int inflationRateFlag, List<InflationData> inflationData, int startYear, int currentYear)
        {
            if (inflationRateFlag == 0)
            {
                return 3.0;
            }
            else if (inflationRateFlag == 1)
            {
                if (startYear == currentYear)
                {
                    var yearlyData = inflationData.Where(i => i.Year >= startYear && i.Year <= currentYear).Select(i => i.CPI);
                    if (yearlyData.Any())
                    {
                        return yearlyData.Average();
                    }
                    else
                    {
                        return 3.0;
                    }
                }
                else
                {
                    InflationData startData = inflationData.FirstOrDefault(i => i.Year == startYear);
                    InflationData endData = inflationData.FirstOrDefault(i => i.Year == currentYear);

                    if (startData != null && endData != null)
                    {
                        double startDollarValue = startData.ValueOfDollar;
                        double endDollarValue = endData.ValueOfDollar;
                        return Math.Pow(endDollarValue / startDollarValue, 1.0 / (currentYear - startYear)) - 1.0;
                    }
                    else
                    {
                        return 3.0; // Default inflation rate
                    }
                }
            }
            else
            {
                return 0.0;
            }
        }

        public static void UpdateCaseNumber(FileControlSettings fileSettings, ref int caseNo, IStatusUpdater statusUpdater)
        {
            string caseTrackingFile = fileSettings.CaseTrackingFile;
            string user = fileSettings.UserName;

            // Update Case Number
            if (user == "stick" || user == "joe")
            {
                try
                {
                    // Ensure the file exists
                    if (!File.Exists(caseTrackingFile))
                    {
                        throw new FileNotFoundException($"The file {caseTrackingFile} does not exist.");
                    }

                    // Open the Excel file and read caseNo from cell A3
                    using (var package = new ExcelPackage(new FileInfo(caseTrackingFile)))
                    {
                        var worksheet = package.Workbook.Worksheets["Case"]; // Access worksheet by name

                        // Check if the worksheet exists
                        if (worksheet == null)
                        {
                            throw new Exception("The worksheet 'Case' does not exist in the workbook.");
                        }

                        // Declare parsedValue before using it
                        int parsedValue;

                        // Read the value from cell A3
                        if (worksheet.Cells["A3"].Value != null && int.TryParse(worksheet.Cells["A3"].Value.ToString(), out parsedValue))
                        {
                            caseNo = parsedValue;
                        }
                        else
                        {
                            statusUpdater.UpdateStatus("Cell A3 is empty or invalid. Defaulting caseNo to 0.");
                        }

                        // Increment the case number
                        caseNo++;

                        // Write the updated case number back to cell A3
                        worksheet.Cells["A3"].Value = caseNo;

                        // Save the file
                        package.Save();
                    }

                    //statusUpdater.UpdateStatus($"Updated case number: {caseNo}");
                }
                catch (Exception ex)
                {
                    statusUpdater.UpdateStatus($"An error occurred: {ex.Message}");
                }
            }
            else
            {
                statusUpdater.UpdateStatus("User does not have permission to update the case number.");
            }
        }

        public static void MovingAverage(ref double movingAverageCurrent, ref double movingAverageWRTZeroCurrent, out double movingAverageRateOfChange, out double movingAverageRateOfChangeWRTZero, int movingAverageLookBackDaysSpecified, int currentIndex, int currentDay, List<double> mainInvestmentClosePrice, List<double> mainInvestmentValuationWRTZero)
        {
            // The incoming movingAverageCurrent and movingAverageWRTZeroCurrent are values for the previous day.
            double movingAverageLast = movingAverageCurrent;
            double movingAverageWRTZeroLast = movingAverageWRTZeroCurrent;

            // Adjust look back period for early days
            int movingAverageLookBackDays = Math.Min(currentIndex + 1, movingAverageLookBackDaysSpecified);

            // Calculate the moving average
            double closePriceSum = 0;
            double closePriceWRTZeroSum = 0;

            for (int ma = 0; ma < movingAverageLookBackDays; ma++)
            {
                closePriceSum += mainInvestmentClosePrice[currentIndex - ma];
                closePriceWRTZeroSum += mainInvestmentValuationWRTZero[currentIndex - ma];
            }

            movingAverageCurrent = closePriceSum / movingAverageLookBackDays;
            movingAverageWRTZeroCurrent = closePriceWRTZeroSum / movingAverageLookBackDays;

            // Calculate rate of change
            if (currentDay == 0) // First day
            {
                movingAverageRateOfChange = 0;
                movingAverageRateOfChangeWRTZero = 0;
            }
            else
            {
                movingAverageRateOfChange = movingAverageCurrent - movingAverageLast;
                movingAverageRateOfChangeWRTZero = movingAverageWRTZeroCurrent - movingAverageWRTZeroLast;
            }
        }

        public static void BollingerBands(double movingAverage, List<double> mainInvestmentClosePrice, int currentIndex, int movingAverageLookBackDaysSpecified, double numberOfStandardDeviations, out double lowerBollingerBand, out double upperBollingerBand, out double standardDeviation, out double ratioStandardDeviations, IStatusUpdater statusUpdater)
        {
            // Adjust look back period for early days
            int movingAverageLookBackDays = Math.Min(currentIndex + 1, movingAverageLookBackDaysSpecified);

            // Calculate average price over look back period
            double closePriceSum = 0.0;
            for (int j = 0; j < movingAverageLookBackDays; j++)
            {
                closePriceSum += mainInvestmentClosePrice[currentIndex - j];
            }

            double closePriceAverage = closePriceSum / movingAverageLookBackDays;

            // Calculate standard deviation
            double closePriceDeviationSquared = 0.0;
            for (int j = 0; j < movingAverageLookBackDays; j++)
            {
                double deviation = mainInvestmentClosePrice[currentIndex - j] - closePriceAverage;
                closePriceDeviationSquared += Math.Pow(deviation, 2);
            }

            if (movingAverageLookBackDays <= 1)
            {
                standardDeviation = Math.Sqrt(closePriceDeviationSquared / movingAverageLookBackDays);
            }
            else
            {
                standardDeviation = Math.Sqrt(closePriceDeviationSquared / (movingAverageLookBackDays - 1));
            }

            // Calculate BOLLINGER Bands
            lowerBollingerBand = movingAverage - numberOfStandardDeviations * standardDeviation;
            upperBollingerBand = movingAverage + numberOfStandardDeviations * standardDeviation;

            // Calculate BOLLINGER Band Ratio of Standard Deviations
            double sharePriceMainInvestment = mainInvestmentClosePrice[currentIndex];
            ratioStandardDeviations = ((sharePriceMainInvestment - movingAverage) / standardDeviation) / numberOfStandardDeviations;

            //statusUpdater.UpdateStatus($"BOLLINGER: Price: {sharePriceMainInvestment}, LBB: {lowerBollingerBand}, UBB: {upperBollingerBand}, SD: {standardDeviation}, SDRatio: {ratioStandardDeviations}");
        }

        public static void RelativeStrengthIndex(ref double averagePriceGain, ref double averagePriceLoss, List<double> mainInvestmentClosePrice, int i, int day, int lookBackDaysSpecified, out double relativeStrengthIndex)
        {
            double averagePriceGainPrevious = averagePriceGain;
            double averagePriceLossPrevious = averagePriceLoss;

            int lookBackDays = lookBackDaysSpecified;
            int dayEff = day + 1; // Create a effective day parameter so as to correspond with MATALB code

            // Calculate RSI
            if (dayEff <= lookBackDays)
            {
                averagePriceGain = 0.0;
                averagePriceLoss = 0.0;
                relativeStrengthIndex = 50.0;
            }
            else if (dayEff == lookBackDays + 1)
            {
                double sumPriceGain = 0.0; // Initialize
                double sumPriceLoss = 0.0; // Initialize

                for (int j = 0; j < lookBackDays; j++)
                {
                    // Calculate price change
                    double priceChange = mainInvestmentClosePrice[i - j] - mainInvestmentClosePrice[i - j - 1];

                    if (priceChange > 0.0)
                    {
                        sumPriceGain += priceChange;
                        sumPriceLoss += 0.0;
                    }
                    else if (priceChange < 0.0)
                    {
                        sumPriceGain += 0.0;
                        sumPriceLoss += Math.Abs(priceChange);
                    }
                    else
                    {
                        sumPriceGain += 0.0;
                        sumPriceLoss += 0.0;
                    }
                }

                averagePriceGain = sumPriceGain / lookBackDays;
                averagePriceLoss = sumPriceLoss / lookBackDays;
                relativeStrengthIndex = 100.0 - (100.0 / (1.0 + averagePriceGain / averagePriceLoss));
            }
            else if (dayEff > lookBackDays + 1)
            {
                // Calculate price change
                double priceChange = mainInvestmentClosePrice[i] - mainInvestmentClosePrice[i - 1];

                if (priceChange > 0.0)
                {
                    averagePriceGain = (averagePriceGainPrevious * (lookBackDays - 1) + priceChange) / lookBackDays;
                    averagePriceLoss = (averagePriceLossPrevious * (lookBackDays - 1) + 0.0) / lookBackDays;
                }
                else if (priceChange < 0.0)
                {
                    averagePriceGain = (averagePriceGainPrevious * (lookBackDays - 1) + 0.0) / lookBackDays;
                    averagePriceLoss = (averagePriceLossPrevious * (lookBackDays - 1) + Math.Abs(priceChange)) / lookBackDays;
                }
                else
                {
                    averagePriceGain = (averagePriceGainPrevious * (lookBackDays - 1) + 0.0) / lookBackDays;
                    averagePriceLoss = (averagePriceLossPrevious * (lookBackDays - 1) + 0.0) / lookBackDays;
                }

                relativeStrengthIndex = 100.0 - (100.0 / (1.0 + averagePriceGain / averagePriceLoss));
            }
            else
            {
                relativeStrengthIndex = 0.0;
            }
        }

        public static void ResetSellCriteria(ref List<double[]> sellCriteria, ref double lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn, int criteriaDaysSinceLastTransactionAtSameLevel, int day, List<DateTime> date, double dateNum, int type, out double lastTransactionSellLevelCrit1, out double actualMainInvestmentValuationWRTSellThresholdAtLastTransaction, out int sellResetType1Flag, out int sellResetType2Flag)
        {
            // Initialize output variables
            sellResetType1Flag = 0;
            sellResetType2Flag = 0;

            lastTransactionSellLevelCrit1 = sellCriteria.Min(x => x[0]) - 0.01;
            actualMainInvestmentValuationWRTSellThresholdAtLastTransaction = sellCriteria.Min(x => x[0]) - 0.01;

            for (int i = 0; i < sellCriteria.Count; i++)
            {
                sellCriteria[i][2] = -criteriaDaysSinceLastTransactionAtSameLevel; // Reset sellCriteria column 3 to baseline
            }

            if (type == 1)
            {
                lastMainInvestmentValuationWRTSellThresholdAfterLastSellXctn = actualMainInvestmentValuationWRTSellThresholdAtLastTransaction;
                sellResetType1Flag = 1;
            }
            else if (type == 2)
            {
                sellResetType2Flag = 1;
            }
        }

        public static void ResetBuyCriteria(ref List<double[]> buyCriteria, ref double lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution, int criteriaDaysSinceLastTransactionAtSameLevel, int day, List<DateTime> date, double dateNum, int type, out double lastTransactionBuyLevelCrit1, out double actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction, out int buyResetType1Flag, out int buyResetType2Flag)
        {
            // Initialize output variables
            buyResetType1Flag = 0;
            buyResetType2Flag = 0;

            lastTransactionBuyLevelCrit1 = buyCriteria.Max(x => x[0]) + 0.01;
            actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction = buyCriteria.Max(x => x[0]) + 0.01;

            for (int i = 0; i < buyCriteria.Count; i++)
            {
                buyCriteria[i][2] = -criteriaDaysSinceLastTransactionAtSameLevel; // Reset buyCriteria column 3 to baseline
            }

            if (type == 1)
            {
                lastMainInvestmentValuationWRTBuyThresholdAfterLastBuyExecution = actualMainInvestmentValuationWRTBuyThresholdAtLastTransaction;
                buyResetType1Flag = 1;
            }
            else if (type == 2)
            {
                buyResetType2Flag = 1;
            }
        }

        public static double StandardDeviation(double[] values)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("The values array cannot be null or empty.");
            }

            double average = values.Average();
            double sumOfSquaresOfDifferences = values.Select(val => Math.Pow(val - average, 2)).Sum();
            return Math.Sqrt(sumOfSquaresOfDifferences / values.Length);
        }
    } //End BackTestUtilities

    public class HelperMethodsBT
    {
    }


    public class SellProfileResult
    {
        public List<double> SellProfileRelativeMrktLevel { get; set; }
        public List<double> SellProfilePctOfAvailShares { get; set; }
        public List<double[]> SellCriteriaReset { get; set; }
    }

    public class BuyProfileResult
    {
        public List<double> BuyProfileRelativeMrktLevel { get; set; }
        public List<double> BuyProfilePctOfAvailFunds { get; set; }
        public List<double[]> BuyCriteriaReset { get; set; }
    }

    public class StrategyResult
    {
        public double SellThreshold { get; set; }
        public double BuyThreshold { get; set; }
        public List<double[]> SellCriteria { get; set; }
        public List<double[]> BuyCriteria { get; set; }
        public int Strategy20Flag { get; set; }
    }

    public class InflationData
    {
        public int Year { get; set; }
        public double CPI { get; set; }
        public double ValueOfDollar { get; set; }
        public double AnnualSavingsAmount { get; set; }
    }
}