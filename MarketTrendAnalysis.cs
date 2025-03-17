using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MarketTrendAnalysis
{
    public class PriceData
    {
        public DateTime Date { get; set; }
        public double Close { get; set; }
    }

    public class TrendAnalysisResultII
    {
        public DateTime Date { get; set; }
        public double ClosePrice { get; set; }
        public string TrendType { get; set; }
        public string TrendPhase { get; set; }
        public string TrendEvent { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public string PercentGain { get; set; }
        public string PercentLoss { get; set; }
        public int MarketDay { get; set; }
        public double DailyReturnRate { get; set; }
        public double ReturnRate { get; set; }
        public int MarketLevel { get; set; }
    }

    public class TrendAnalResult
    {
        public int Day { get; set; }
        public DateTime CloseDate { get; set; }
        public double SharePrice { get; set; }
    }

    public class HighLowResult
    {
        public double High { get; set; }
        public double Low { get; set; }
    }

    public class FindMarketSwingPeriodsResult
    {
        public List<DrawDownPeriod> DrawDownPeriods { get; set; }
        public int NumberOfDrawdownPeriods { get; set; }
        public double MaxDrawdown { get; set; }
        public List<DrawUpPeriod> DrawUpPeriods { get; set; }
        public List<PriceData> HighClosePrices { get; set; }
    }

    public class DrawDownPeriod
    {
        public DateTime HighDate { get; set; }
        public DateTime LowDate { get; set; }
        public double HighPrice { get; set; }
        public double LowPrice { get; set; }
        public double DrawdownPercentage { get; set; }
        public int CalendarDays { get; set; }
    }

    public class DrawUpPeriod
    {
        public DateTime LowDate { get; set; }
        public DateTime HighDate { get; set; }
        public double LowPrice { get; set; }
        public double HighPrice { get; set; }
        public int CalendarDays { get; set; }
        public double DrawUpPercentage { get; set; }
    }

    public class DrawDownData
    {
        public int Index { get; set; }
        public double ClosePriceHigh { get; set; }
        public double ClosePriceLow { get; set; }
        public double Drawdown { get; set; }
        public double DrawdownPercentage { get; set; }
        public DateTime CloseDateHigh { get; set; }
        public DateTime CloseDateLow { get; set; }
        public int CalendarDays { get; set; }
    }


    public class TrendPatternResults
    {
        public List<TrendAnalResult> ResultsTrendAnalysis { get; set; }
        public List<TrendPoint> UpTrend { get; set; }
        public List<TrendPoint> DownTrend { get; set; }
        public List<Trend> Trend { get; set; }
    }

    public class TrendPoint
    {
        public int Day { get; set; }
        public double Value { get; set; }
        public int Milestone { get; set; }
    }

    public class Trend
    {
        public string Mode { get; set; }
        public string Submode { get; set; }
        public string Milestone { get; set; }
    }

    public class PatternEvaluationResult
    {
        public string Mode { get; set; }
        public string Submode { get; set; }
    }
    
    public interface IStatusUpdater
    {
        void UpdateStatus(string message);
    }

    public class TrendAnalysisFunctions
    {
        public static void RunTrendAnalysis(List<string> tickersToRunMTA, IStatusUpdater statusUpdater, PathDefinitions pathDefinitions, int closePriceColumn)
        {
            statusUpdater.UpdateStatus($"Market Trend Analysis       {DateTime.Now:MM-dd-yyyy HH:mm:ss}\n");

            // Remove duplicate tickers
            tickersToRunMTA = tickersToRunMTA.Distinct().ToList();

            // Remove unwanted tickers
            List<string> tickersToRemove = new List<string> { "^W5000", "FZOLX", "GEV", "IBIT", "NUKZ", "QQQI" }; // W5000 has multiple issues, the others don't have enough data
            tickersToRunMTA = tickersToRunMTA.Except(tickersToRemove).ToList();

            int tickerCount = tickersToRunMTA.Count;

            InvestmentData investmentData = new InvestmentData();
            //DateTime oneYearAgo = DateTime.Now.AddYears(-1);
            int dataMinimumInNumberOfMarketDays = 82;
            int maxDrawdownMagnitudeCriteria = 2;// %

            foreach (var ticker in tickersToRunMTA)
            {
                // Read in the selected ticker data 
                investmentData = InvestmentProcessor.GetInvestmentData(pathDefinitions, ticker, closePriceColumn, false);

                statusUpdater.UpdateStatus($"Ticker being analyzed: {investmentData.Ticker}");

                // Prepare a list that contains Date vs Close price history for use in the market trend analysis methods
                List<PriceData> priceDataList = new List<PriceData>();

                // Capture the market data over the range of startIndex to endIndex.
                int startIndex = Math.Max(0, investmentData.DateLog.Count - dataMinimumInNumberOfMarketDays);
                //int startIndex = 0;
                //int endIndex = Math.Max(0, investmentData.DateLog.Count - dataMinimumInNumberOfMarketDays + 30);
                int endIndex = investmentData.DateLog.Count;

                for (int i = startIndex; i < endIndex; i++)
                //for (int i = startIndex; i < investmentData.DateLog.Count; i++)
                {
                    priceDataList.Add(new PriceData
                    {
                        Date = investmentData.DateLog[i],
                        Close = investmentData.MainInvestmentPriceArray[i][closePriceColumn]
                    });
                }

                using (var writer = new StreamWriter(Path.Combine(pathDefinitions.ResultFilesBasePathMarketTrendAnalysis, "PriceData.csv")))
                {
                    // Write header
                    writer.WriteLine("Date,Close");

                    // Write rows
                    foreach (var result in priceDataList)
                    {
                        writer.WriteLine($"{result.Date:yyyy-MM-dd},{result.Close:F2}");
                    }
                }

                //Make a copy of the priceDataList because the PriceHistoryAnalysis2 method will make changes to it, and we need the original array future use.
                List<PriceData> priceDataListCopy = new List<PriceData>(priceDataList);
                var trendAnalysisResultII = PriceHistoryAnalysis2(priceDataListCopy);

                List<PriceData> priceDataListFiltered = new List<PriceData>();
                foreach (var result in trendAnalysisResultII)
                {
                    DateTime date = result.Date;
                    double close = result.ClosePrice;

                    if (result.High > 0.0 || result.Low > 0.0)
                    {
                        PriceData priceData = new PriceData
                        {
                            Date = date,
                            Close = close
                        };
                        priceDataListFiltered.Add(priceData);
                    }
                }

                // Call the new TrendPatternEvaluation method
                var trendPatternResults = TrendPatternEvaluation(priceDataListFiltered);           
            }
        }

        public static TrendPatternResults TrendPatternEvaluation(List<PriceData> priceDataListFiltered)
        {
            var resultsTrendAnalysis = new List<TrendAnalResult>();
            var upTrend = new List<TrendPoint>();
            var downTrend = new List<TrendPoint>();
            var trend = new List<Trend>();

            int day = 0;
            string mode = "Undetermined";
            string submode = "Undetermined";
            string modePrediction = mode;
            double previousDayClosePrice = 0;
            double currentHigherHigh = 0;
            int dayCurrentHigherHigh = 1;
            double currentHigherLow = 0;
            int dayCurrentHigherLow = 1;
            double interimMarketHigh = 0;
            double marketHigh = 0;
            double currentLowerHigh = 0;
            int dayCurrentLowerHigh = 1;
            double currentLowerLow = 0;
            int dayCurrentLowerLow = 1;
            double interimMarketLow = 0;
            double marketLow = 0;

            int[,] marketLevel = new int[priceDataListFiltered.Count, 4];

            for (int i = 0; i < priceDataListFiltered.Count; i++)
            {
                day++;
                //double dateNum = priceDataListFiltered[i].Date.ToOADate();
                DateTime closeDate = priceDataListFiltered[i].Date;
                double closePrice = priceDataListFiltered[i].Close;

                string mileStone = "none";

                if (day == 1)
                {
                    previousDayClosePrice = closePrice;
                    currentHigherHigh = closePrice / 1000;
                    currentHigherLow = closePrice / 1000;
                    interimMarketHigh = closePrice / 1000;
                    marketHigh = closePrice * 1000;
                    currentLowerHigh = closePrice * 1000;
                    currentLowerLow = closePrice * 1000;
                    interimMarketLow = closePrice * 1000;
                    marketLow = closePrice / 1000;
                }
                else if (day == 2)
                {
                    var patternEvalResult = PatternEval(priceDataListFiltered[day - 2].Close, priceDataListFiltered[day - 1].Close, priceDataListFiltered[day].Close, priceDataListFiltered[day + 1].Close);
                    mode = patternEvalResult.Mode;
                    submode = patternEvalResult.Submode;
                    modePrediction = mode;
                    // Case 1 - Market moving up -Establish UpTrend and checkfor HigherHigh
                    if (mode == "UpTrend")
                    {
                        if (marketLevel[day, 3] == 2)
                        {
                            mileStone = "HigherLow";
                            submode = "UpTrendUndetermined";
                            currentHigherLow = closePrice;
                            dayCurrentHigherLow = 2;
                            interimMarketLow = closePrice;
                            currentHigherHigh = previousDayClosePrice;
                            dayCurrentHigherHigh = 1;
                            upTrend.Add(new TrendPoint { Day = day - 1, Value = 1 });
                            interimMarketHigh = previousDayClosePrice;
                        }
                        else if (marketLevel[day, 3] == 1)
                        {
                            mileStone = "HigherHigh";
                            submode = "UpTrendUndetermined";
                            currentHigherHigh = closePrice;
                            dayCurrentHigherHigh = 2;
                            interimMarketHigh = closePrice;
                            currentHigherLow = previousDayClosePrice;
                            dayCurrentHigherLow = 1;
                            upTrend.Add(new TrendPoint { Day = day - 1, Value = 2 });
                            interimMarketLow = previousDayClosePrice;
                        }
                        else
                        {
                            string message = "Day 2: UpTrend Conumdrum.\n\nClick in cmd window > press CTRL-C > press OK.";
                            string title = "Settings Issue";
                            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

                            submode = "UpTrendUndetermined";
                        }
                        previousDayClosePrice = closePrice;
                    }
                    else if (mode == "DownTrend") // Case 2 - Market moving down
                    {
                        if (marketLevel[day, 3] == 1) // Check for Lower High
                        {
                            mileStone = "LowerHigh";
                            submode = "DownTrendUndetermined";
                            currentLowerHigh = closePrice;
                            dayCurrentLowerHigh = 2;
                            interimMarketHigh = closePrice;
                            currentLowerLow = previousDayClosePrice;
                            dayCurrentLowerLow = 1;
                            downTrend.Add(new TrendPoint { Day = day - 1, Value = 2 }); // LL - Make correction
                            interimMarketLow = previousDayClosePrice;
                        }
                        else if (marketLevel[day, 3] == 2) // Check for Lower Low
                        {
                            mileStone = "LowerLow";
                            submode = "DownTrendUndetermined";
                            currentLowerLow = closePrice;
                            dayCurrentLowerLow = 2;
                            interimMarketLow = closePrice;
                            currentLowerHigh = previousDayClosePrice;
                            dayCurrentLowerHigh = 1;
                            downTrend.Add(new TrendPoint { Day = day - 1, Value = 1 }); // LH - Make correction
                            interimMarketHigh = previousDayClosePrice;
                        }
                        else
                        {
                            submode = "DownTrendUndetermined";

                            string message = "Day 2: DownTrend Conumdrum.\n\nClick in cmd window > press CTRL-C > press OK.";
                            string title = "Settings Issue";
                            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        previousDayClosePrice = closePrice;
                    }
                    else // Case 3 - Market unchanged
                    {
                        mode = "Undetermined";
                        submode = "Undetermined";
                        previousDayClosePrice = closePrice;
                    }
                }
                else if (day > 2 && mode == "UpTrend") // Analyze the Uptrend
                {
                    var patternEvalResult = PatternEval(priceDataListFiltered[day - 2].Close, priceDataListFiltered[day - 1].Close, priceDataListFiltered[day].Close, priceDataListFiltered[day + 1].Close);
                    modePrediction = patternEvalResult.Mode;

                    if (closePrice > marketLow && (closePrice > interimMarketHigh || closePrice > currentHigherLow) && closePrice > previousDayClosePrice) // Case 1 - Market Up Day - Moving up from Higher Low - Check for UpTrendImpulse and/or Higher High
                    {
                        if (closePrice > currentHigherHigh) // Subcase 1a
                        {

                            if (marketLevel[day, 3] == 1 && dayCurrentHigherHigh <= dayCurrentHigherLow) // // Subcase 1A1 - Check for Higher High
                            {
                                mileStone = "HigherHigh";
                                currentHigherHigh = closePrice;
                                dayCurrentHigherHigh = day;
                                submode = "UpTrendImpulse";
                            }
                            else if (marketLevel[day, 3] == 1 && dayCurrentHigherHigh >= dayCurrentHigherLow && submode == "UpTrendReversal") //  Check for Higher High
                            {
                                mileStone = "HigherHigh";
                                currentHigherHigh = closePrice;
                                dayCurrentHigherHigh = day;
                                currentHigherLow = previousDayClosePrice;
                                upTrend[day - 1].Value = 1; // HH - Make correction
                                dayCurrentHigherLow = day - 1;
                                submode = "UpTrendImpulse";
                            }
                            else if (submode == "UpTrendReversal") // Check for Higher High
                            {
                                currentHigherLow = previousDayClosePrice;
                                upTrend[day - 1].Value = 1; // HH - Make correction
                                dayCurrentHigherLow = day - 1;
                                submode = "UpTrendImpulse";
                            }
                            else if (submode == "UpTrendUndetermined" || submode == "UpTrendImpulse") // Check for DownTrendImpulse
                            {
                                submode = "UpTrendImpulse";
                            }
                            else
                            {
                                submode = "UpTrendUndetermined";
                            }
                        }
                        else // Subcase 1B
                        {
                            submode = "UpTrendUndetermined";
                        }
                        previousDayClosePrice = closePrice;
                        interimMarketHigh = closePrice;
                    }
                    else if (closePrice > marketLow && closePrice < interimMarketHigh && closePrice <= previousDayClosePrice) // Case 2 - Market Down Day - Check for UpTrendPullback and/or HigherLow
                    {
                        previousDayClosePrice = closePrice;
                        if (submode == "UpTrendUndetermined" || submode == "UpTrendImpulse") // Subcase 2A - Check for UpTrendPullback, HigherLow, or reversal during UpTrendImpulse when sharePriceMainInvestment < interimMarketHigh && sharePriceMainInvestment <= previousDaysharePriceMainInvestment
                        {
                            if (closePrice < currentHigherHigh && closePrice > currentHigherLow) // Subcase 2A1 - Check for UpTrendPullback and/or HigherLow during UpTrendImpulse when sharePriceMainInvestment is between currentHigherHigh and currentHigherLow
                            {
                                interimMarketLow = closePrice;
                                submode = "UpTrendPullback";
                                if (marketLevel[day, 3] == 2 && dayCurrentHigherHigh >= dayCurrentHigherLow) // Subcase 2A1A - Check for Higher Low - May not need this
                                {
                                    mileStone = "HigherLow";
                                    currentHigherLow = closePrice;
                                    dayCurrentHigherLow = day;
                                }
                            }
                            else if (closePrice < currentHigherHigh && closePrice < currentHigherLow && modePrediction == "UpTrend") // Subcase 2A1 - Check for Reversal sharePriceMainInvestment is less than currentHigherHigh and currentHigherLow
                            {
                                interimMarketLow = closePrice;
                                submode = "UpTrendPullback";
                                if (marketLevel[day, 3] == 2 && dayCurrentHigherHigh >= dayCurrentHigherLow) //  Check for Higher Low
                                {
                                    mileStone = "HigherLow";
                                    currentHigherLow = closePrice;
                                    dayCurrentHigherLow = day;
                                }
                            }
                            else if (closePrice < currentHigherHigh && closePrice < currentHigherLow && modePrediction == "DownTrend") // Subcase 2A1A - Check for Reversal sharePriceMainInvestment is less than currentHigherHigh and currentHigherLow
                            {
                                mode = "DownTrend";
                                submode = "DownTrendUndetermined";
                                currentHigherHigh = closePrice / 1000;
                                dayCurrentHigherHigh = 1;
                                currentHigherLow = closePrice / 1000;
                                dayCurrentHigherLow = 1;
                                interimMarketHigh = closePrice / 1000;
                                marketHigh = closePrice * 1000;
                                if (marketLevel[day, 3] == 2) // Check for Lower Low
                                {
                                    mileStone = "LowerLow";
                                    currentLowerLow = closePrice;
                                    dayCurrentLowerLow = day;
                                }
                            }
                            else if (closePrice > currentHigherHigh) //Subcase 2A2 - May not need this
                            {
                                submode = "UpTrendPullback";
                                interimMarketHigh = closePrice;
                                if (marketLevel[day, 3] == 2 && dayCurrentHigherHigh >= dayCurrentHigherLow) // Subcase 2A2A
                                {
                                    mileStone = "HigherLow";
                                    currentHigherLow = closePrice;
                                    dayCurrentHigherLow = day;
                                }
                            }
                        }
                        else if (submode == "UpTrendPullback" && closePrice < currentHigherHigh && closePrice > currentHigherLow) // Subcase 2b - Check for UpTrendPullback and/or HigherLow during UpTrendPullback when sharePriceMainInvestment >= currentHigherLow
                        {
                            interimMarketLow = closePrice;
                            submode = "UpTrendPullback";
                            if (marketLevel[day, 3] == 2 && dayCurrentHigherHigh >= dayCurrentHigherLow) // Subcase 2b1
                            {
                                mileStone = "HigherLow";
                                currentHigherLow = closePrice;
                                dayCurrentHigherLow = day;
                            }
                        }
                        else if ((submode == "UpTrendPullback" || submode == "UpTrendReversal") && closePrice < currentHigherLow) // Subcase 2C - Check for Start of DownTrend during UpTrendPullback when sharePriceMainInvestment < currentHigherLow
                        {
                            interimMarketLow = closePrice;
                            mode = "DownTrend";
                            submode = "DownTrendUndetermined";
                            currentHigherHigh = closePrice / 1000;
                            dayCurrentHigherHigh = 1;
                            currentHigherLow = closePrice / 1000;
                            dayCurrentHigherLow = 1;
                            interimMarketHigh = closePrice / 1000;
                            marketHigh = closePrice * 1000;
                            if (marketLevel[day, 3] == 2) // Subcase 2C1 - Check for Lower Low
                            {
                                mileStone = "LowerLow";
                                currentLowerLow = closePrice;
                                dayCurrentLowerLow = day;
                            }
                        }
                        else // Subcase 2D
                        {
                            submode = "UpTrendReversal";

                            string message = "Error. UpTrend; Cant decide on submode type.\n\nClick in cmd window > press CTRL-C > press OK.";
                            string title = "Settings Issue";
                            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

                        }
                    }
                    else if (closePrice > marketLow && closePrice < interimMarketHigh && closePrice > previousDayClosePrice) // Case 3 - Market-up Day - Check for UpTrendImpulse and/or HigherHigh during UpTrendPullback when sharePriceMainInvestment < interimMarketHigh && sharePriceMainInvestment > previousDaysharePriceMainInvestment
                    {
                        previousDayClosePrice = closePrice;
                        interimMarketHigh = closePrice;
                        if (submode == "UpTrendUndetermined" || submode == "UpTrendImpulse")
                        {
                            string message = "rror. Submode issue.\n\nClick in cmd window > press CTRL-C > press OK.";
                            string title = "Settings Issue";
                            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

                            if (marketLevel[day, 3] == 1 && dayCurrentHigherHigh <= dayCurrentHigherLow && closePrice > currentHigherHigh) // Subcase 3A1 - Check for Higher High
                            {
                                mileStone = "HigherHigh";
                                submode = "UpTrendImpulse";
                                currentHigherHigh = closePrice;
                                dayCurrentHigherHigh = day;
                            }
                            else // Subcase 3A2
                            {
                                submode = "UpTrendUndetermined";
                            }
                        }
                        else if (submode == "UpTrendPullback")// Subcase 3A
                        {
                            interimMarketHigh = closePrice;
                            if (marketLevel[day, 3] == 1 && dayCurrentHigherHigh <= dayCurrentHigherLow && closePrice > currentHigherHigh) // Subcase 3A1 - Check for Higher High
                            {
                                mileStone = "HigherHigh";
                                submode = "UpTrendImpulse";
                                currentHigherHigh = closePrice;
                                dayCurrentHigherHigh = day;
                            }
                            else // Subcase 3A2
                            {
                                submode = "UpTrendUndetermined";
                            }
                        }
                        else // Subcase 3B
                        {
                            submode = "UpTrendUndetermined";
                        }
                    }
                    else if (closePrice < marketLow && closePrice < interimMarketHigh && closePrice < previousDayClosePrice) // Case 4
                    {
                        previousDayClosePrice = closePrice;
                        mode = "DownTrend";
                        submode = "DownTrendUndetermined";
                        currentHigherHigh = closePrice / 1000;
                        dayCurrentHigherHigh = 1;
                        currentHigherLow = closePrice / 1000;
                        dayCurrentHigherLow = 1;
                        interimMarketHigh = closePrice / 1000;
                        marketHigh = closePrice * 1000;
                    }
                    else // Case 5 - Market unchanged
                    {
                        previousDayClosePrice = closePrice;
                        mode = "Undetermined";
                        submode = "Undetermined";
                    }
                }
                else if (day > 2 && mode == "DownTrend") // Analyze the Downtrend
                {
                    var patternEvalResult = PatternEval(priceDataListFiltered[day - 2].Close, priceDataListFiltered[day - 1].Close, priceDataListFiltered[day].Close, priceDataListFiltered[day + 1].Close);
                    modePrediction = patternEvalResult.Mode;
                    if (closePrice < marketHigh && (closePrice < interimMarketLow || closePrice < currentLowerHigh) && closePrice < previousDayClosePrice) // Case 1
                    {
                        if (closePrice < currentLowerLow) // Subcase 1a
                        {
                            if (marketLevel[day, 3] == 2 && dayCurrentLowerLow <= dayCurrentLowerHigh) // Check for Lower Low
                            {
                                mileStone = "LowerLow";
                                currentLowerLow = closePrice;
                                dayCurrentLowerLow = day;
                                submode = "DownTrendImpulse";
                            }
                            else if (marketLevel[day, 3] == 2 && dayCurrentLowerLow >= dayCurrentLowerHigh && submode == "DownTrendReversal") // Check for LowerLow
                            {
                                mileStone = "LowerLow";
                                currentLowerLow = closePrice;
                                dayCurrentLowerLow = day;
                                currentLowerHigh = previousDayClosePrice;
                                downTrend[day - 1].Value = 2; // LL - Make correction
                                dayCurrentLowerHigh = day - 1;
                                submode = "DownTrendImpulse";
                            }
                            else if (submode == "DownTrendReversal") // Check for LowerLow
                            {
                                currentLowerHigh = previousDayClosePrice;
                                downTrend[day - 1].Value = 2; // LL - Make correction
                                dayCurrentLowerHigh = day - 1;
                                submode = "DownTrendImpulse";
                            }
                            else if (submode == "DownTrendUndetermined" || submode == "DownTrendImpulse") // Check for DownTrendImpulse
                            {
                                submode = "DownTrendImpulse";
                            }
                            else
                            {
                                submode = "DownTrendUndetermined";
                            }
                        }
                        else // Subcase 1b
                        {
                            submode = "DownTrendUndetermined";
                        }
                        previousDayClosePrice = closePrice;
                        interimMarketLow = closePrice; // New interim low
                    }
                    else if (closePrice < marketHigh && closePrice > interimMarketLow && closePrice > previousDayClosePrice) // Case 2
                    {
                        previousDayClosePrice = closePrice;
                        if (submode == "DownTrendUndetermined" || submode == "DownTrendImpulse") // Subcase 2A
                        {
                            if (closePrice > currentLowerLow && closePrice < currentLowerHigh) // Subcase 2A1
                            {
                                interimMarketHigh = closePrice;
                                submode = "DownTrendPullback";
                                if (marketLevel[day, 3] == 1 && dayCurrentLowerLow >= dayCurrentLowerHigh) // Subcase 2A1A - May not need this
                                {
                                    mileStone = "LowerHigh";
                                    currentLowerHigh = closePrice;
                                    dayCurrentLowerHigh = day;
                                }
                            }
                            else if (closePrice > currentLowerLow && closePrice > currentLowerHigh && modePrediction == "DownTrend")
                            {
                                interimMarketHigh = closePrice;
                                submode = "DownTrendPullback";
                                if (marketLevel[day, 3] == 1 && dayCurrentLowerLow >= dayCurrentLowerHigh)
                                {
                                    mileStone = "LowerHigh";
                                    currentLowerHigh = closePrice;
                                    dayCurrentLowerHigh = day;
                                }
                            }
                            else if (closePrice > currentLowerLow && closePrice > currentLowerHigh && modePrediction == "UpTrend") // Subcase 2a1a
                            {
                                mode = "UpTrend";
                                submode = "UpTrendUndetermined";
                            }
                        }
                        else if (submode == "DownTrendPullback" && closePrice > currentLowerLow && closePrice < currentLowerHigh)
                        {
                            interimMarketHigh = closePrice;
                            submode = "DownTrendPullback";
                            if (marketLevel[day, 3] == 1 && dayCurrentLowerLow >= dayCurrentLowerHigh)
                            {
                                mileStone = "LowerHigh";
                                currentLowerHigh = closePrice;
                                dayCurrentLowerHigh = day;
                            }
                        }
                        else if ((submode == "DownTrendPullback" || submode == "DownTrendReversal") && closePrice > currentLowerHigh) // Subcase 2a1a
                        {
                            interimMarketHigh = closePrice;
                            mode = "UpTrend";
                            submode = "UpTrendUndetermined";
                            currentLowerHigh = closePrice * 1000;
                            dayCurrentLowerHigh = 1;
                            currentLowerLow = closePrice * 1000;
                            dayCurrentLowerLow = 1;
                            interimMarketLow = closePrice * 1000;
                            marketLow = closePrice / 1000;
                            if (marketLevel[day, 3] == 1)
                            {
                                mileStone = "HigherHigh";
                                currentHigherHigh = closePrice;
                                dayCurrentHigherHigh = day;
                            }
                        }
                        else
                        {
                            submode = "DownTrendReversal";
                        }
                    }
                    else if (closePrice < marketHigh && closePrice > interimMarketLow && closePrice < previousDayClosePrice)
                    {
                        previousDayClosePrice = closePrice;
                        interimMarketLow = closePrice;
                        if (submode == "DownTrendUndetermined" || submode == "DownTrendImpulse")
                        {
                            if (marketLevel[day, 3] == 2 && closePrice < currentLowerLow)
                            {
                                mileStone = "LowerLow";
                                submode = "DownTrendImpulse";
                                currentLowerLow = closePrice;
                                dayCurrentLowerLow = day;
                            }
                            else
                            {
                                submode = "DownTrendUndetermined";
                            }
                        }
                        else if (submode == "DownTrendPullback")
                        {
                            interimMarketLow = closePrice;
                            if (marketLevel[day, 3] == 2 && dayCurrentLowerLow <= dayCurrentLowerHigh && closePrice < currentLowerLow)
                            {
                                mileStone = "LowerLow";
                                submode = "DownTrendImpulse";
                                currentLowerLow = closePrice;
                                dayCurrentLowerLow = day;
                            }
                            else
                            {
                                submode = "DownTrendUndetermined";
                            }
                        }
                        else
                        {
                            submode = "DownTrendUndetermined";
                        }
                    }
                    else if (closePrice > marketHigh && closePrice > interimMarketLow && closePrice > previousDayClosePrice)
                    {
                        previousDayClosePrice = closePrice;
                        mode = "UpTrend";
                        submode = "UpTrendUndetermined";
                        currentLowerLow = closePrice * 1000;
                        dayCurrentLowerLow = 1;
                        currentLowerHigh = closePrice * 1000;
                        dayCurrentLowerHigh = 1;
                        interimMarketLow = closePrice * 1000;
                        marketLow = closePrice / 1000;
                    }
                    else
                    {
                        previousDayClosePrice = closePrice;
                        mode = "Undetermined";
                        submode = "Undetermined";
                    }
                }

                resultsTrendAnalysis.Add(new TrendAnalResult
                {
                    Day = day,
                    CloseDate = closeDate,
                    SharePrice = closePrice,
                });


                // Record HH and HL
                if (mileStone == "HigherHigh")
                {
                    upTrend.Add(new TrendPoint { Day = day, Value = closePrice, Milestone = 1 }); // HH
                }
                else if (mileStone == "HigherLow")
                {
                    upTrend.Add(new TrendPoint { Day = day, Value = closePrice, Milestone = 2 }); // HL
                }
                else
                {
                    upTrend.Add(new TrendPoint { Day = day, Value = closePrice, Milestone = 3 }); // No Milestone
                }

                // Record LH and LL
                if (mileStone == "LowerHigh")
                {
                    downTrend.Add(new TrendPoint { Day = day, Value = closePrice, Milestone = 1 }); // LH
                }
                else if (mileStone == "LowerLow")
                {
                    downTrend.Add(new TrendPoint { Day = day, Value = closePrice, Milestone = 2 }); // LL
                }
                else
                {
                    downTrend.Add(new TrendPoint { Day = day, Value = closePrice, Milestone = 3 }); // No Milestone

                    // Trend array (record end-of-day Results)
                    trend.Add(new Trend
                {
                    Mode = mode,
                    Submode = submode,
                    Milestone = mileStone
                });
            }

            return new TrendPatternResults
            {
                ResultsTrendAnalysis = resultsTrendAnalysis,
                UpTrend = upTrend,
                DownTrend = downTrend,
                Trend = trend
            };
        }

        private static PatternEvaluationResult PatternEval(double p1, double p2, double p3, double p4)
        {
            // Dummy implementation of PatternEval, replace with actual logic
            string mode = "Undetermined";
            string submode = "Undetermined";

            if (p2 > p1 && p3 > p2)
            {
                mode = "UpTrend";
            }
            else if (p2 < p1 && p3 < p2)
            {
                mode = "DownTrend";
            }

            return new PatternEvaluationResult { Mode = mode, Submode = submode };
        }

        public static List<TrendAnalysisResultII> PriceHistoryAnalysis2(List<PriceData> priceDataList)
        {
            // This routine runs through several stages (routines) to extract the desired information.

            //====================================================================================================
            // Stage 1 - Eliminate data points that have the same slope (1st derivative) value (positive or negative) on either side of a close price point.
            // These points cannot be highs or lows, because a high or low would have opposite signed slopes on either side.
            //====================================================================================================
            var trendAnalysisResultsStage1 = new List<TrendAnalysisResultII>();
            var indicesOfDataPointsToRemove = new List<int>(); // Initialize and set to empty.
            int arraySize = priceDataList.Count;

            for (int i = 0; i < arraySize; i++)
            {
                int dayCurrent = i + 1;
                DateTime dateCurrent = priceDataList[i].Date;
                double sharePriceCurrentDay = priceDataList[i].Close;

                double sharePriceNextDay = 0.0;
                if (i < arraySize - 1) // If not the last point in the array
                {
                    sharePriceNextDay = priceDataList[i + 1].Close;
                }
                else if (i == arraySize - 1) // Last data point
                {
                    sharePriceNextDay = sharePriceCurrentDay;
                }

                // Calculate first derivative -  dy/dx = (p2 - p1) / (d2 - d1), where d2-d1 = 1, therefore dy/dx = (p2 - p1)
                double sharePricePreviousDay = 0.0;
                double dydxCurrentDay = 0.0;
                double dydxNextDay = 0.0;

                if (i == 0) // Day 1
                {
                    dydxCurrentDay = 0.0;
                    dydxNextDay = (sharePriceNextDay - sharePriceCurrentDay);
                }
                else // All other days
                {
                    sharePricePreviousDay = priceDataList[i - 1].Close;
                    dydxCurrentDay = (sharePriceCurrentDay - sharePricePreviousDay);
                    dydxNextDay = (sharePriceNextDay - sharePriceCurrentDay);
                }

                // Identify points that meet the criteria (i.e., have the same slope value (positive or negative) on either side of the point), and add
                // that point to the indicesOfDataPointsToRemove array for future deletion.
                if ((dydxCurrentDay > 0 && dydxNextDay > 0) || (dydxCurrentDay < 0 && dydxNextDay < 0))
                {
                    indicesOfDataPointsToRemove.Add(i); // If sign is positive add its index of the previous point to the idxOfDataPointsToRemove so that it can be removed later
                }
            }

            // Remove the data rows that violate the dailyReturnRateThresholdCriteria criteria
            foreach (int idx in indicesOfDataPointsToRemove.OrderByDescending(v => v))
            {
                priceDataList.RemoveAt(idx);
            }

            // Transfer priceDataList data to trendAnalysisResultsStage1 list for use in the next stage
            foreach (var priceData in priceDataList)
            {
                trendAnalysisResultsStage1.Add(new TrendAnalysisResultII
                {
                    Date = priceData.Date,
                    ClosePrice = priceData.Close,
                });
            }

            //=========================================================================================================
            // Stage 2 - Run through the trendAnalysisResultsStage1 list and identify High and Low points by looking at slope on both sides of the point.
            // A positive slope on the left side of point/negative slope on the right side of point indicates a High.
            // Vice versa for a Low
            //=========================================================================================================
            var trendAnalysisResultsStage2 = IdentifyHighAndLowPointsViaSlope(trendAnalysisResultsStage1);

            //=========================================================================================================
            // Stage 3 - Run through the trendAnalysisResultsStage2 list and eliminate points with delta return rate <= minReturnCriteria
            //=========================================================================================================
            double minReturnCriteria = 1.0; // %
            indicesOfDataPointsToRemove = new List<int>(); // Re-initialize and set array to empty.

            for (int i = 0; i < trendAnalysisResultsStage2.Count; i++)
            {
                DateTime dateCurrent = trendAnalysisResultsStage2[i].Date;
                double sharePriceCurrentDay = trendAnalysisResultsStage2[i].ClosePrice;

                double sharePriceTwoDaysAgo = 0.0;
                double sharePricePreviousDay = 0.0;
                double deltaReturnRate = 0.0;
                if (i == 0) // Day 1
                {
                    sharePriceTwoDaysAgo = 0.0;
                    sharePricePreviousDay = 0.0;
                    deltaReturnRate = 0.0;
                }
                else if (i == 1)
                {
                    sharePriceTwoDaysAgo = 0.0;
                    sharePricePreviousDay = trendAnalysisResultsStage2[i - 1].ClosePrice;
                    deltaReturnRate = 0.0;
                }
                else if (i >= 2)
                {
                    sharePriceTwoDaysAgo = trendAnalysisResultsStage2[i - 2].ClosePrice;
                    sharePricePreviousDay = trendAnalysisResultsStage2[i - 1].ClosePrice;
                    deltaReturnRate = Math.Abs((sharePriceCurrentDay - sharePricePreviousDay) / sharePricePreviousDay);
                }

                if (i >= 2 && deltaReturnRate <= minReturnCriteria / 100)
                {
                    if (trendAnalysisResultsStage2[i].Low > 0 && trendAnalysisResultsStage2[i].Low > sharePriceTwoDaysAgo)
                    {
                        indicesOfDataPointsToRemove.Add(i);
                    }
                    else if (trendAnalysisResultsStage2[i].High > 0 && trendAnalysisResultsStage2[i].High < sharePriceTwoDaysAgo)
                    {
                        indicesOfDataPointsToRemove.Add(i); // add its index of the point to the idxOfDataPointsToRemove so that it can be removed later
                    }
                }
            }
            var trendAnalysisResultsStage3 = new List<TrendAnalysisResultII>(trendAnalysisResultsStage2); // Create a copy of the original for use in next stage

            // Remove the data rows that violate the dailyReturnRateThresholdCriteria criteria
            foreach (int idx in indicesOfDataPointsToRemove.OrderByDescending(v => v))
            {
                trendAnalysisResultsStage3.RemoveAt(idx);
            }

            //=========================================================================================================
            // Stage 4 - Run through the trendAnalysisResultsStage2 list and identify High and Low points by looking at slope on either side of the close price point
            //=========================================================================================================
            var trendAnalysisResultsStage4 = IdentifyHighAndLowPointsViaSlope(trendAnalysisResultsStage3);

            return trendAnalysisResultsStage4;
        }

    }
}
