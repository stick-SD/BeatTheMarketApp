namespace BeatTheMarketApp.InvestmentEvaluation
{
    public class PostProcessEODHDAPI
    {
        //=========================================================================================================================
        // Contains all the methods associated with reading-in the API data and writing-it-out to the Investment Evaluation Results excel file
        //=========================================================================================================================
        public static APIAssetFundamentalDataResult ReadAPIFundamentalData(List<InvestmentData> listInvestmentData, List<string> tickerList, UserInputs userInputs, IStatusUpdater statusUpdater)
        {
            //=========================================================================================================================
            // This method reads the Concatenated API data file and maps the data to the InvestmentData fields
            //=========================================================================================================================
            statusUpdater.UpdateStatus($"{Environment.NewLine}Executing ReadAPIFundamentalData method ");

            string APIConcatenatedFilePath = Path.Combine(PathDefinitions.APIDataFolder, "AssetFinancialDataConcatenated.xlsx");

            using (ExcelPackage package = new ExcelPackage(new FileInfo(APIConcatenatedFilePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets["FinancialData"];

                var columnMapping = IdentifyCorrespondingAPIExcelFileHeaderColumnNumbers(worksheet, statusUpdater);
                statusUpdater.UpdateStatus($"  API file mapping summary:");
                foreach (var mapping in columnMapping)
                {
                    statusUpdater.UpdateStatus($"   Field '{mapping.Key}' to Column '{mapping.Value}'");
                }

                // Map the all the Ticker symbols in file AssetFinancialDataConcatenated.xlsx to their corresponding row
                var tickerRowMapping = new Dictionary<string, int>();
                int row = 3; // API data starts at row 3
                while (!string.IsNullOrEmpty(worksheet.Cells[row, 1].Text))
                {
                    tickerRowMapping[worksheet.Cells[row, 1].Text] = row;
                    row++;
                }

                statusUpdater.UpdateStatus($"  API file mapping complete{Environment.NewLine}.");

                // Map the tick
                foreach (var investmentData in listInvestmentData)
                {
                    statusUpdater.UpdateStatus($"API data mapping for Ticker: {investmentData.Ticker}.");
                    int tickerRow;
                    if (tickerRowMapping.TryGetValue(investmentData.Ticker, out tickerRow))
                    {
                        MapAPIDataToInvestmentDataFields(columnMapping, worksheet, tickerRow, investmentData, statusUpdater);
                        //statusUpdater.UpdateStatus($" API data mapping complete for Ticker {investmentData.Ticker}.");
                    }
                    else
                    {
                        statusUpdater.UpdateStatus($" The Ticker {investmentData.Ticker} was not found in the API data list.");
                    }
                }
            }

            return new APIAssetFundamentalDataResult { ListInvestmentData = listInvestmentData };
        }

        public static Dictionary<string, int> IdentifyCorrespondingAPIExcelFileHeaderColumnNumbers(ExcelWorksheet worksheet, IStatusUpdater statusUpdater)
        {
            var columnMapping = new Dictionary<string, int>();

            // Run through the headers in columns 1 through end and checks for match and assign column number
            int headerRow = 2; // Headers are in row 2 of the API input file
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                var header = worksheet.Cells[headerRow, col].Text.Trim();

                // Match header to APIHeader in the FieldMappings
                var mapping = FieldMappings.Mappings(statusUpdater).FirstOrDefault(m => string.Equals(m.APIHeader, header, StringComparison.OrdinalIgnoreCase));
                if (mapping != null)
                {
                    columnMapping[mapping.FieldName] = col;
                }
            }

            return columnMapping;
        }

        public static void MapAPIDataToInvestmentDataFields(Dictionary<string, int> columnMapping, ExcelWorksheet worksheet, int row, InvestmentData investmentData, IStatusUpdater statusUpdater)
        {
            foreach (var mapping in FieldMappings.Mappings(statusUpdater))
            {
                int columnIndex;
                if (columnMapping.TryGetValue(mapping.FieldName, out columnIndex))
                {
                    object cellValue = worksheet.Cells[row, columnIndex].Value;
                    try
                    {
                        if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                        {
                            mapping.AssignValue(investmentData, cellValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        statusUpdater.UpdateStatus($"  MapAPIDataToInvestmentDataFields - Error processing field '{mapping.FieldName}': {ex.Message}");
                    }
                }
            }
        }

        public static void WriteInvestmentEvaluationFile(List<InvestmentData> listInvestmentData, List<string> tickerList, UserInputs userInputs, IStatusUpdater statusUpdater)
        {
            statusUpdater.UpdateStatus($"{Environment.NewLine}Executing WriteInvestmentEvaluationFile method");

            // Define results xlsx file
            PathDefinitions pathDefinitions = new PathDefinitions(Environment.UserName);
            string formattedDate = DateTime.Now.ToString("MM-dd-yyyy_HH-mm-ss");
            string investmentEvaluationFilePath = Path.Combine(pathDefinitions.ResultFilesBasePathInvestmentEvaluation, $"InvestmentEvaluationResultsEODAPI-{formattedDate}.xlsx");

            string resultsTemplateFile = Path.Combine(PathDefinitions.TemplateFilesPath, "InvestmentEvaluationResultsTemplateEODAPI.xlsx");

            if (!ExcelUtilities.GenerateExcelWorkbookFromTemplate(resultsTemplateFile, investmentEvaluationFilePath))
            {
                statusUpdater.UpdateStatus($"Error occurred in GenerateExcelWorkbookFromTemplate method {Environment.NewLine}");
                return;
            }

            using (ExcelPackage package = new ExcelPackage(new FileInfo(investmentEvaluationFilePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets["PerformanceSummary"];

                // ==========================================================================================================
                //  Map resultsFile columns to the listInvestmentData fields
                // ==========================================================================================================

                // columnMappingI - Map the resultsFile columns to the listInvestmentData fields in the FieldMapping class
                var columnMappingI = IdentifyCorrespondingResultsExcelFileHeaderColumnNumbers(worksheet, statusUpdater);
                statusUpdater.UpdateStatus($" Output File Mapping Summary for Column Mapping Type I:");
                foreach (var mapping in columnMappingI) // Debug: Log the mapped fields and their columns
                {
                    statusUpdater.UpdateStatus($"  Field '{mapping.Key}' to Column '{mapping.Value}'");
                }

                // columnMappingII - Map the resultsFile columns to the listInvestmentData array based fields
                int bullMarketPerformancePeriodsCount = listInvestmentData[0].BullMarketPerformancePeriodsCount;
                int bearMarketPerformancePeriodsCount = listInvestmentData[0].BearMarketPerformancePeriodsCount;
                int numberOfBullPerformancePeriodHeaders = 13; // Number of column headers for bull performance periods
                int numberOfBearPerformancePeriodHeaders = 13; // Number of column headers for bear performance periods

                var columnMappingII = IdentifyCorrespondingResultsExcelFileHeaderColumnNumbersII(worksheet, bullMarketPerformancePeriodsCount, bearMarketPerformancePeriodsCount, numberOfBullPerformancePeriodHeaders, numberOfBearPerformancePeriodHeaders, statusUpdater);
                statusUpdater.UpdateStatus($"{Environment.NewLine} Output File Mapping Summary for Column Mapping Type II:");
                foreach (var mapping in columnMappingII) // Debug: Log the mapped fields and their columns
                {
                    statusUpdater.UpdateStatus($"  Field '{mapping.Key}' to Column '{mapping.Value}'");
                }

                // ==========================================================================================================
                // Write specified listInvestmentData field to the results file
                // ==========================================================================================================
                statusUpdater.UpdateStatus($"{Environment.NewLine}Writing data to resultsFile");

                worksheet.Cells[8, 1].Value = DateTime.Now.ToString("MM/dd/yyyy"); // Write current date to Title

                // Use columnMappingI & columnMappingII to write the listInvestmentData fields to the resultsFile
                int row = 10; // The first row to start writing data is row 10
                foreach (var investmentData in listInvestmentData)
                {
                    statusUpdater.UpdateStatus($" Writing data for ticker to resultsFile: {investmentData.Ticker}");

                    // Map the columnMappingI fields
                    MapInvestmentDataFieldsToOutputFile(columnMappingI, worksheet, row, investmentData, statusUpdater);

                    // Map the columnMappingII fields
                    // Assign arrays for Performance, Beta, and Volatility
                    for (int i = 1; i <= 6; i++)
                    {
                        MapInvestmentDataFieldsToOutputFileCells(columnMappingII, $"Performance[{i}]", worksheet, row, investmentData.Performance.ElementAtOrDefault(i - 1));
                        MapInvestmentDataFieldsToOutputFileCells(columnMappingII, $"Beta[{i}]", worksheet, row, investmentData.Beta.ElementAtOrDefault(i - 1));
                        MapInvestmentDataFieldsToOutputFileCells(columnMappingII, $"Volatility[{i}]", worksheet, row, investmentData.Volatility.ElementAtOrDefault(i - 1));
                    }

                    // Assign BullMarketPerformance array
                    for (int i = 0; i <= numberOfBullPerformancePeriodHeaders; i++)
                    {
                        int bullIndex = i + (bullMarketPerformancePeriodsCount - numberOfBullPerformancePeriodHeaders);
                        MapInvestmentDataFieldsToOutputFileCells(columnMappingII, $"BullMarketPerformance[{i}]", worksheet, row, investmentData.BullMarketPerformance.ElementAtOrDefault(bullIndex));
                    }

                    // Assign BearMarketPerformance array
                    for (int i = 0; i <= numberOfBearPerformancePeriodHeaders; i++)
                    {
                        int bearIndex = i + (bearMarketPerformancePeriodsCount - numberOfBearPerformancePeriodHeaders);
                        MapInvestmentDataFieldsToOutputFileCells(columnMappingII, $"BearMarketPerformance[{i}]", worksheet, row, investmentData.BearMarketPerformance.ElementAtOrDefault(bearIndex));
                    }

                    row++;
                }

                // Save excel file
                package.Save();
                statusUpdater.UpdateStatus($" Results file saved.");
            }

            if (userInputs.EnableIEResultsFileAutoOpen) //Open the results file
            {
                Process.Start(new ProcessStartInfo { FileName = investmentEvaluationFilePath, UseShellExecute = true });
            }
        }

        public static Dictionary<string, int> IdentifyCorrespondingResultsExcelFileHeaderColumnNumbers(ExcelWorksheet worksheet, IStatusUpdater statusUpdater)
        {
            var columnMapping = new Dictionary<string, int>();

            int headerRow = 9; // Headers are in row 9 of the output file
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                var header = worksheet.Cells[headerRow, col].Text.Trim();
                //statusUpdater.UpdateStatus($"  Output Mapping: Header: {header} Column: {col}");

                // Match header to OutputHeader in the FieldMappings
                var mapping = FieldMappings.Mappings(statusUpdater).FirstOrDefault(m => string.Equals(m.OutputHeader, header, StringComparison.OrdinalIgnoreCase));
                if (mapping != null)
                {
                    columnMapping[mapping.FieldName] = col;
                    //statusUpdater.UpdateStatus($"   Output Mapping - Mapping successful for Header:  {header}  Column: {col}");
                }
                else
                {
                    //statusUpdater.UpdateStatus($"   Output Mapping - No mapping found for Header:  {header};  Column: {col}");
                }
            }

            return columnMapping;
        }

        public static void MapInvestmentDataFieldsToOutputFile(Dictionary<string, int> columnMapping, ExcelWorksheet worksheet, int row, InvestmentData investmentData, IStatusUpdater statusUpdater)
        {
            //statusUpdater.UpdateStatus($"  Output File write-out Mapping");

            foreach (var mapping in FieldMappings.Mappings(statusUpdater))
            {
                int columnIndex;
                if (columnMapping.TryGetValue(mapping.FieldName, out columnIndex))
                {
                    // Retrieve the value dynamically using the GetValue delegate
                    object value = mapping.GetValue(investmentData);

                    // Apply sentence case conversion for specific fields
                    if (mapping.FieldName == "SectorAPI" || mapping.FieldName == "IndustryAPI")
                    {
                        var stringValue = value as string;
                        if (stringValue != null && !string.IsNullOrWhiteSpace(stringValue))
                        {
                            value = ConvertToSentenceCase(stringValue);
                        }
                    }

                    // Apply Title case conversion for specific fields
                    if (mapping.FieldName == "MainInvestmentName")
                    {
                        var stringValue = value as string;
                        if (stringValue != null && !string.IsNullOrWhiteSpace(stringValue))
                        {
                            value = TitleCase(stringValue);
                        }
                    }

                    // Write the value to the worksheet
                    worksheet.Cells[row, columnIndex].Value = value ?? string.Empty;
                    //statusUpdater.UpdateStatus($"   Writing Field: {mapping.FieldName}, Value: {value}, Row: {row}, Column: {columnIndex}");
                }
                else
                {
                    statusUpdater.UpdateStatus($"   Field '{mapping.FieldName}' not found in columnMapping. Skipping...");
                }
            }
        }

        private static string ConvertToSentenceCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            input = input.Trim().ToLower(); // Ensure the string is in lowercase
            return char.ToUpper(input[0]) + input.Substring(1); // Capitalize the first letter
        }

        private static string TitleCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(input.ToLower());
        }

        public static Dictionary<string, int> IdentifyCorrespondingResultsExcelFileHeaderColumnNumbersII(ExcelWorksheet worksheet, int bullMarketPerformancePeriodsCount, int bearMarketPerformancePeriodsCount, int numberOfBullPerformancePeriodHeaders, int numberOfBearPerformancePeriodHeaders, IStatusUpdater statusUpdater)
        {
            //statusUpdater.UpdateStatus( $"Establishing mapping of InvestmentData properties to their corresponding headers in the results file" );

            // Create a dictionary to hold the mapping of InvestmentData fields to ResultsSummaryFile columns
            var columnMapping = new Dictionary<string, int>();

            // Mappings: List of InvestmentData properties and their corresponding headers in the results file. 
            // Performance mappings
            var performanceMapping = new Dictionary<int, string>
            {
                { 1, "Anlized Rtn 1Yr" },
                { 2, "Anlized Rtn 3Yr" },
                { 3, "Anlized Rtn 5Yr" },
                { 4, "Anlized Rtn 10Yr" },
                { 5, "Anlized Rtn 15Yr" },
                { 6, "Anlized Rtn 20Yr" }
            };

            // Beta mappings
            var betaMapping = new Dictionary<int, string>
            {
                { 1, "Beta 1Yr" },
                { 2, "Beta 3Yr" },
                { 3, "Beta 5Yr" },
                { 4, "Beta 10Yr" },
                { 5, "Beta 15Yr" },
                { 6, "Beta 20Yr" }
            };

            // Volatility mappings
            var volatilityMapping = new Dictionary<int, string>
            {
                { 1, "Volatility 1Yr" },
                { 2, "Volatility 3Yr" },
                { 3, "Volatility 5Yr" },
                { 4, "Volatility 10Yr" },
                { 5, "Volatility 15Yr" },
                { 6, "Volatility 20Yr" }
            };

            // Mappings for BullMarketPerformancePeriods 
            var bullMarketPerformancePeriodMapping = new Dictionary<int, string>();
            for (int i = 0; i < numberOfBullPerformancePeriodHeaders; i++) // Create the headers.
            {
                bullMarketPerformancePeriodMapping[i] = $"Up Mrkt #{i + 1}"; //Up Mrkt #1, Up Mrkt #2, Up Mrkt #3, Up Mrkt #4, Up Mrkt #5, Up Mrkt #6, Up Mrkt #7, Up Mrkt #8, Up Mrkt #9, Up Mrkt #10, Up Mrkt #11, Up Mrkt #12, Up Mrkt #13
                //statusUpdater.UpdateStatus($"BullMrkPerf Mapping: Relative Column {i} = UpMrkt#{i + 1}");
            }

            // Mappings for BearMarketPerformancePeriods
            var bearMarketPerformancePeriodMapping = new Dictionary<int, string>();
            for (int i = 0; i < numberOfBearPerformancePeriodHeaders; i++) // Create the headers.
            {
                bearMarketPerformancePeriodMapping[i] = $"Dn Mrkt #{i + 1}";
                //statusUpdater.UpdateStatus($"BearMrkPerf Mapping: Relative Column {i} = DnMrkt#{i + 1}");
            }

            // The following loop works as follows:
            // It consecutively reads in the header for each of the columns in the resultsFile. 
            // For a given column header, it runs through each of the foreach loops and until it finds a match for the current column header.
            // If a match is found it will save the column number corresponding to the matched set,
            // It will then set the matchFound bool flag to true and break out of the foreach loop.
            // The match found flag/continue statement will then send it to the beginning of the loop to read the next column header.           
            int headerRow = 9; // Header is at row 9. Read the header for each column one by one from the resultsFile and map column per the mapping definined above 
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                var header = worksheet.Cells[headerRow, col].Text.Trim();

                bool matchFound = false; // Flag to indicate if a match is found

                //// Map standard fields
                //foreach (var field in fieldToHeaderMap)
                //{
                //    if (string.Equals(header, field.Value, StringComparison.OrdinalIgnoreCase))
                //    {
                //        columnMapping[field.Key] = col;
                //        matchFound = true;
                //        break; // Exit this foreach loop
                //    }
                //}
                //if (matchFound) continue; // Skip to the next column if a match was found

                // Map Performance array fields
                foreach (var performanceField in performanceMapping)
                {
                    if (string.Equals(header, performanceField.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        columnMapping[$"Performance[{performanceField.Key}]"] = col;
                        matchFound = true;
                        break; // Exit this foreach loop
                    }
                }

                if (matchFound) continue; // Skip to the next column if a match was found

                // Map Beta array fields
                foreach (var betaField in betaMapping)
                {
                    if (string.Equals(header, betaField.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        columnMapping[$"Beta[{betaField.Key}]"] = col;
                        matchFound = true;
                        break; // Exit this foreach loop
                    }
                }

                if (matchFound) continue; // Skip to the next column if a match was found

                // Map Volatility array fields
                foreach (var volatilityField in volatilityMapping)
                {
                    if (string.Equals(header, volatilityField.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        columnMapping[$"Volatility[{volatilityField.Key}]"] = col;
                        matchFound = true;
                        break; // Exit this foreach loop
                    }
                }

                if (matchFound) continue; // Skip to the next column if a match was found

                // Map BullMarketPerformance fields
                for (int i = 0; i < numberOfBullPerformancePeriodHeaders; i++) // Headers: Up Mrkt #1, Up Mrkt #2, ..., Up Mrkt #13 
                {
                    if (string.Equals(header, bullMarketPerformancePeriodMapping[i], StringComparison.OrdinalIgnoreCase))
                    {
                        columnMapping[$"BullMarketPerformance[{i}]"] = col;
                        //statusUpdater.UpdateStatus($"columnRelativeMapLocation {i} | Header: {header} | BullMarketPerformance {i} | Column {col}");
                        matchFound = true;
                        break; // Exit this loop
                    }
                }

                if (matchFound) continue; // Skip to the next column if a match was found

                // Map BearMarketPerformance fields
                for (int i = 0; i < numberOfBearPerformancePeriodHeaders; i++) // Headers: Dn Mrkt #1, Dn Mrkt #2, ..., Dn Mrkt #13
                {
                    if (string.Equals(header, bearMarketPerformancePeriodMapping[i], StringComparison.OrdinalIgnoreCase))
                    {
                        columnMapping[$"BearMarketPerformance[{i}]"] = col;
                        //statusUpdater.UpdateStatus($"columnRelativeMapLocation {i} | Header: {header} | BearMarketPerformance {i} | Column {col}");
                        matchFound = true;
                        break; // Exit this loop
                    }
                }
            }

            return columnMapping;
        }

        private static void MapInvestmentDataFieldsToOutputFileCells(Dictionary<string, int> columnMapping, string fieldName, ExcelWorksheet worksheet, int row, object value)
        {
            int columnNumber;
            if (columnMapping.TryGetValue(fieldName, out columnNumber))
            {
                worksheet.Cells[row, columnNumber].Value = value ?? string.Empty; // Assign the value or an empty string if null
            }
        }

        public class FieldMapping<T>
        {
            public string FieldName { get; set; } // Property name in InvestmentData
            public string APIHeader { get; set; } // Header in the API Excel file
            public string OutputHeader { get; set; } // Header in the output Excel file
            public Action<InvestmentData, T> AssignValue { get; set; } // Action to assign value to InvestmentData
            public Func<InvestmentData, object> GetValue { get; set; } // Function to get value for output file
        }

        public static List<string[]> MappingList = new List<string[]>
        {
            // Define Mapping parameters: InvestmentData.fieldName, EODHD API Fundamentals Array Element, EODHD API Data Excel File Header, EODHD Investment Evaluation Results Excel File Header
            // Example Format: new string[] { "InvestmentData.fieldName", "EODHD API Fundamentals Array Element", "EODHD API Data Excel File Header", "EODHD Investment Evaluation Results Excel File Header" },
            new string[] { "Ticker_EODAPI", "General.Code", "Code", "Ticker EODAPI" },
            new string[] { "AssetType_EODAPI", "General.Type", "Type", "Asset Type EODAPI" },
            new string[] { "AssetName_EODAPI", "General.Name", "Name", "Asset Name EODAPI" },
            new string[] { "Description_EODAPI", "General.Description", "Description", "Description EODAPI" },
            new string[] { "NotUsed_EODAPI", "General.Exchange", "Exchange", "Exchange EODAPI" },
            new string[] { "UpdatedAt_EODAPI", "General.UpdatedAt", "UpdatedAt", "Updated At EODAPI" },
            new string[] { "NotUsed_EODAPI", "ETF_Data.Company_Name", "Company_Name", "None" },
            new string[] { "NotUsed_EODAPI", "ETF_Data.Company_URL", "Company_URL", "None" },
            new string[] { "NotUsed_EODAPI", "ETF_Data.ETF_URL", "ETF_URL", "None" },
            new string[] { "NotUsed_EODAPI", "ETF_Data.Domicile", "Domicile", "None" },
            new string[] { "InceptionDate_EODAPI", "ETF_Data.Inception_Date", "Inception_Date", "Inception Date EODAPI" },
            new string[] { "Category_EODAPI", "General.Category", "Category", "Category EODAPI" },
            new string[] { "Sector_EODAPI", "General.Sector", "Sector", "Sector EODAPI" },
            new string[] { "Industry_EODAPI", "General.Industry", "Industry", "Industry EODAPI" },
            new string[] { "NotUsed_EODAPI", "General.GicSector", "GicSector", "None" },
            new string[] { "NotUsed_EODAPI", "General.GicGroup", "GicGroup", "None" },
            new string[] { "NotUsed_EODAPI", "General.GicIndustry", "GicIndustry", "None" },
            new string[] { "NotUsed_EODAPI", "General.GicSubIndustry", "GicSubIndustry", "None" },
            new string[] { "NotUsed_EODAPI", "General.HomeCategory", "HomeCategory", "None" },
            new string[] { "NotUsed_EODAPI", "General.InternationalDomestic", "InternationalDomestic", "None" },
            new string[] { "NotUsed_EODAPI", "Highlights.MarketCapitalization", "MarketCapitalization", "None" },
            new string[] { "MarketCapitalizationMillion_EODAPI", "Highlights.MarketCapitalizationMln", "MarketCapitalizationMln", "Market Capitalization Million EODAPI" },
            new string[] { "AverageMktCapMillion_EODAPI", "ETF_Data.Average_Mkt_Cap_Mil", "Average_Mkt_Cap_Mil", "Average Mkt Cap Million EODAPI" },
            new string[] { "TotalAssets_EODAPI", "ETF_Data.TotalAssets", "TotalAssets", "Total Assets EODAPI" },
            new string[] { "NotUsed_EODAPI", "Highlights.MostRecentQuarter", "MostRecentQuarter", "None" },
            new string[] { "TrailingPE_EODAPI", "Valuation.TrailingPE", "TrailingPE", "Trailing PE EODAPI" },
            new string[] { "NotUsed_EODAPI", "Highlights.PERatio", "PERatio", "None" },
            new string[] { "ForwardPE_EODAPI", "Valuation.ForwardPE", "ForwardPE", "Forward PE EODAPI" },
            new string[] { "PriceProspectiveEarnings_EODAPI", "ETF_Data.Valuations_Growth.Valuations_Rates_Portfolio.PriceProspectiveEarnings", "PriceProspectiveEarnings", "Price Prospective Earnings EODAPI" },
            new string[] { "PEGRatio_EODAPI", "Highlights.PEGRatio", "PEGRatio", "PEG Ratio EODAPI" },
            new string[] { "QuarterlyEarningsGrowthYOY_EODAPI", "Highlights.QuarterlyEarningsGrowthYOY", "QuarterlyEarningsGrowthYOY", "Quarterly Earnings Growth YOY EODAPI" },
            new string[] { "HistoricalEarningsGrowth_EODAPI", "ETF_Data.Valuations_Growth.Growth_Rates_Portfolio.HistoricalEarningsGrowth", "HistoricalEarningsGrowth", "Historical Earnings Growth EODAPI" },
            new string[] { "LongTermProjectedEarningsGrowth_EODAPI", "ETF_Data.Valuations_Growth.Growth_Rates_Portfolio.LongTermProjectedEarningsGrowth", "LongTermProjectedEarningsGrowth", "Long Term Projected Earnings Growth EODAPI" },
            new string[] { "NotUsed_EODAPI", "Highlights.EarningsShare", "EarningsShare", "None" },
            new string[] { "NotUsed_EODAPI", "Highlights.DilutedEpsTTM", "DilutedEpsTTM", "None" },
            new string[] { "NotUsed_EODAPI", "Highlights.EPSEstimateCurrentQuarter", "EPSEstimateCurrentQuarter", "None" },
            new string[] { "NotUsed_EODAPI", "Highlights.EPSEstimateCurrentYear", "EPSEstimateCurrentYear", "None" },
            new string[] { "NotUsed_EODAPI", "Highlights.EPSEstimateNextQuarter", "EPSEstimateNextQuarter", "None" },
            new string[] { "NotUsed_EODAPI", "Highlights.EPSEstimateNextYear", "EPSEstimateNextYear", "None" },
            new string[] { "NotUsed_EODAPI", "Highlights.EBITDA", "EBITDA", "None" },
            new string[] { "EnterpriseValueEbitda_EODAPI", "Valuation.EnterpriseValueEbitda", "EnterpriseValueEbitda", "Enterprise Value Ebitda EODAPI" },
            new string[] { "DividendYieldTTM_EODAPI", "Highlights.DividendYield", "DividendYield", "Dividend Yield TTM EODAPI" },
            new string[] { "DividendYieldFwd_EODAPI", "SplitsDividends.ForwardAnnualDividendYield", "ForwardAnnualDividendYield", "Dividend Yield Fwd EODAPI" },
            new string[] { "NotUsed_EODAPI", "SplitsDividends.ForwardAnnualDividendRate", "ForwardAnnualDividendRate", "None" },
            new string[] { "DistributionYieldTTM_EODAPI", "ETF_Data.Yield", "Yield", "Distribution Yield TTM EODAPI" },
            new string[] { "NotUsed_EODAPI", "ETF_Data.Valuations_Growth.Valuations_Rates_To_Category.DividendYieldFactor", "DividendYieldFactor", "None" },
            new string[] { "PayoutRatio_EODAPI", "SplitsDividends.PayoutRatio", "PayoutRatio", "Payout Ratio EODAPI" },
            new string[] { "DividendPayingFrequency_EODAPI", "ETF_Data.Dividend_Paying_Frequency", "Dividend_Paying_Frequency", "Dividend Paying Frequency EODAPI" },
            new string[] { "NotUsed_EODAPI", "Highlights.DividendShare", "DividendShare", "Dividend Per Share TTM EODAPI" },
            new string[] { "DividendDate_EODAPI", "SplitsDividends.DividendDate", "DividendDate", "Dividend Date EODAPI" },
            new string[] { "ExDividendDate_EODAPI", "SplitsDividends.ExDividendDate", "ExDividendDate", "Ex Dividend Date EODAPI" },
            new string[] { "ProfitMarginTTM_EODAPI", "Highlights.ProfitMargin", "ProfitMargin", "Profit Margin TTM EODAPI" },
            new string[] { "OperatingMarginTTM_EODAPI", "Highlights.OperatingMarginTTM", "OperatingMarginTTM", "Operating Margin TTM EODAPI" },
            new string[] { "NotUsed_EODAPI", "Highlights.GrossProfitTTM", "GrossProfitTTM", "None" },
            new string[] { "NotUsed_EODAPI", "Highlights.RevenuePerShareTTM", "RevenuePerShareTTM", "Revenue Per Share TTM EODAPI" },
            new string[] { "NotUsed_EODAPI", "Highlights.RevenueTTM", "RevenueTTM", "None" },
            new string[] { "QuarterlyRevenueGrowthYOY_EODAPI", "Highlights.QuarterlyRevenueGrowthYOY", "QuarterlyRevenueGrowthYOY", "Quarterly Revenue Growth YOY EODAPI" },
            new string[] { "EnterpriseValueRevenue_EODAPI", "Valuation.EnterpriseValueRevenue", "EnterpriseValueRevenue", "Enterprise Value Revenue EODAPI" },
            new string[] { "PriceBookMRQ_EODAPI", "Valuation.PriceBookMRQ", "PriceBookMRQ", "Price Book MRQ EODAPI" },
            new string[] { "PriceBook_EODAPI", "ETF_Data.Valuations_Growth.Valuations_Rates_To_Category.PriceBook", "PriceBook", "Price Book EODAPI" },
            new string[] { "BookValue_EODAPI", "Highlights.BookValue", "BookValue", "Book Value EODAPI" },
            new string[] { "BookValueGrowth_EODAPI", "ETF_Data.Valuations_Growth.Growth_Rates_Portfolio.BookValueGrowth", "BookValueGrowth", "Book Value Growth EODAPI" },
            new string[] { "NotUsed_EODAPI", "Valuation.EnterpriseValue", "EnterpriseValue", "None" },
            new string[] { "PriceSalesTTM_EODAPI", "Valuation.PriceSalesTTM", "PriceSalesTTM", "Price Sales TTM EODAPI" },
            new string[] { "PriceSales_EODAPI", "ETF_Data.Valuations_Growth.Valuations_Rates_Portfolio.PriceSales", "PriceSales", "Price Sales EODAPI" },
            new string[] { "SalesGrowth_EODAPI", "ETF_Data.Valuations_Growth.Growth_Rates_Portfolio.SalesGrowth", "SalesGrowth", "Sales Growth EODAPI" },
            new string[] { "PriceCashFlow_EODAPI", "ETF_Data.Valuations_Growth.Valuations_Rates_Portfolio.PriceCashFlow", "PriceCashFlow", "Price Cash Flow EODAPI" },
            new string[] { "CashFlowGrowth_EODAPI", "ETF_Data.Valuations_Growth.Growth_Rates_Portfolio.CashFlowGrowth", "CashFlowGrowth", "Cash Flow Growth EODAPI" },
            new string[] { "Beta_EODAPI", "Technicals.Beta", "Beta", "Beta EODAPI" },
            new string[] { "Volatility1y_EODAPI", "ETF_Data.Performance.Volatility1y", "Volatility1y", "Volatility 1y EODAPI" },
            new string[] { "Volatility3y_EODAPI", "ETF_Data.Performance.Volatility3y", "Volatility3y", "Volatility 3y EODAPI" },
            new string[] { "ReturnOnAssetsTTM_EODAPI", "Highlights.ReturnOnAssetsTTM", "ReturnOnAssetsTTM", "Return On Assets TTM EODAPI" },
            new string[] { "ReturnOnEquityTTM_EODAPI", "Highlights.ReturnOnEquityTTM", "ReturnOnEquityTTM", "Return On Equity TTM EODAPI" },
            new string[] { "ExpectedReturn3y_EODAPI", "ETF_Data.Performance.ExpReturn3y", "ExpReturn3y", "Expected Return 3y EODAPI" },
            new string[] { "SharpRatio3y_EODAPI", "ETF_Data.Performance.SharpRatio3y", "SharpRatio3y", "Sharp Ratio 3y EODAPI" },
            new string[] { "AnnualizedReturnYTD_EODAPI", "ETF_Data.Performance.Returns_YTD", "Returns_YTD", "Annualized Return YTD EODAPI" },
            new string[] { "AnnualizedReturn1Y_EODAPI", "ETF_Data.Performance.Returns_1Y", "Returns_1Y", "Annualized Return 1Y EODAPI" },
            new string[] { "AnnualizedReturn3Y_EODAPI", "ETF_Data.Performance.Returns_3Y", "Returns_3Y", "Annualized Return 3Y EODAPI" },
            new string[] { "AnnualizedReturn5Y_EODAPI", "ETF_Data.Performance.Returns_5Y", "Returns_5Y", "Annualized Return 5Y EODAPI" },
            new string[] { "AnnualizedReturn10Y_EODAPI", "ETF_Data.Performance.Returns_10Y", "Returns_10Y", "Annualized Return 10Y EODAPI" },
            new string[] { "AnnualHoldingsTurnover_EODAPI", "ETF_Data.AnnualHoldingsTurnover", "AnnualHoldingsTurnover", "Annual Holdings Turnover EODAPI" },
            new string[] { "HoldingsCount_EODAPI", "ETF_Data.Holdings_Count", "Holdings_Count", "Holdings Count EODAPI" },
            new string[] { "NotUsed_EODAPI", "ETF_Data.Max_Annual_Mgmt_Charge", "Max_Annual_Mgmt_Charge", "None" },
            new string[] { "NetExpenseRatio_EODAPI", "ETF_Data.NetExpenseRatio", "NetExpenseRatio", "Net Expense Ratio EODAPI" },
            new string[] { "NotUsed_EODAPI", "ETF_Data.Ongoing_Charge", "Ongoing_Charge", "None" },
            new string[] { "NotUsed_EODAPI", "SplitsDividends.LastSplitDate", "LastSplitDate", "None" },
            new string[] { "NotUsed_EODAPI", "SplitsDividends.LastSplitFactor", "LastSplitFactor", "None" },
            new string[] { "NotUsed_EODAPI", "AnalystRatings.Buy", "Buy", "None" },
            new string[] { "NotUsed_EODAPI", "AnalystRatings.Hold", "Hold", "None" },
            new string[] { "AnalystRating_EODAPI", "AnalystRatings.Rating", "Rating", "Analyst Rating EODAPI" },
            new string[] { "NotUsed_EODAPI", "AnalystRatings.Sell", "Sell", "None" },
            new string[] { "NotUsed_EODAPI", "AnalystRatings.StrongBuy", "StrongBuy", "None" },
            new string[] { "NotUsed_EODAPI", "AnalystRatings.StrongSell", "StrongSell", "None" },
            new string[] { "AnalystTargetPrice_EODAPI", "AnalystRatings.TargetPrice", "TargetPrice", "Analyst Target Price EODAPI" },
            new string[] { "WallStreetTargetPrice_EODAPI", "Highlights.WallStreetTargetPrice", "WallStreetTargetPrice", "Wall Street Target Price EODAPI" },
            new string[] { "Category_Benchmark_EODAPI", "ETF_Data.MorningStar.Category_Benchmark", "Category_Benchmark", "Category Benchmark EODAPI" },
            new string[] { "MorningStarRating_EODAPI", "ETF_Data.MorningStar.Ratio", "Ratio", "Morning Star Rating EODAPI" },
            new string[] { "NotUsed_EODAPI", "ETF_Data.MorningStar.Sustainability_Ratio", "Sustainability_Ratio", "None" },
            new string[] { "NotUsed_EODAPI", "SharesStats.SharesFloat", "SharesFloat", "None" },
            new string[] { "NotUsed_EODAPI", "SharesStats.SharesOutstanding", "SharesOutstanding", "None" },
            new string[] { "ShareShortPercentOfFloat_EODAPI", "SharesStats.ShortPercentFloat", "ShortPercentFloat", "Share Short Percent Of Float EODAPI" },
            new string[] { "NotUsed_EODAPI", "Technicals.SharesShort", "SharesShort", "None" },
            new string[] { "NotUsed_EODAPI", "Technicals.SharesShortPriorMonth", "SharesShortPriorMonth", "None" },
            new string[] { "NotUsed_EODAPI", "Technicals.ShortRatio", "ShortRatio", "None" },
            new string[] { "NotUsed_EODAPI", "Technicals.ShortPercent", "ShortPercent", "None" },
            new string[] { "PercentInsiders_EODAPI", "SharesStats.PercentInsiders", "PercentInsiders", "Percent Insiders EODAPI" },
            new string[] { "PercentInstitutions_EODAPI", "SharesStats.PercentInstitutions", "PercentInstitutions", "Percent Institutions EODAPI" },
            new string[] { "PercentMrktCapMega_EODAPI", "ETF_Data.Market_Capitalisation.Mega", "Mega", "Percent Mrkt Cap Mega EODAPI" },
            new string[] { "PercentMrktCapBig_EODAPI", "ETF_Data.Market_Capitalisation.Big", "Big", "Percent Mrkt Cap Big EODAPI" },
            new string[] { "PercentMrktCapMedium_EODAPI", "ETF_Data.Market_Capitalisation.Medium", "Medium", "Percent Mrkt Cap Medium EODAPI" },
            new string[] { "PercentMrktCapSmall_EODAPI", "ETF_Data.Market_Capitalisation.Small", "Small", "Percent Mrkt Cap Small EODAPI" },
            new string[] { "PercentMrktCapMicro_EODAPI", "ETF_Data.Market_Capitalisation.Micro", "Micro", "Percent Mrkt Cap Micro EODAPI" },
            new string[] { "AssetAllocationCashPct_EODAPI", "ETF_Data_Asset_Allocation_Cash_Net_Assets_%", "Asset Allocation Cash, % of Net Assets", "Asset Allocation Cash Pct EODAPI" },
            new string[] { "AssetAllocationNotClassifiedPct_EODAPI", "ETF_Data_Asset_Allocation_NotClassified_Net_Assets_%", "Asset Allocation NotClassified, % of Net Assets", "Asset Allocation Not Classified Pct EODAPI" },
            new string[] { "AssetAllocationStockNonUSPct_EODAPI", "ETF_Data_Asset_Allocation_Stock non-US_Net_Assets_%", "Asset Allocation Stock non-US, % of Net Assets", "Asset Allocation Stock non-US Pct EODAPI" },
            new string[] { "AssetAllocationOtherPct_EODAPI", "ETF_Data_Asset_Allocation_Other_Net_Assets_%", "Asset Allocation Other, % of Net Assets", "Asset Allocation Other Pct EODAPI" },
            new string[] { "AssetAllocationStockUSPct_EODAPI", "ETF_Data_Asset_Allocation_Stock US_Net_Assets_%", "Asset Allocation Stock US, % of Net Assets", "Asset Allocation Stock US Pct EODAPI" },
            new string[] { "AssetAllocationBondPct_EODAPI", "ETF_Data_Asset_Allocation_Bond_Net_Assets_%", "Asset Allocation Bond, % of Net Assets", "Asset Allocation Bond Pct EODAPI" },
            new string[] { "FixedIncomeEffectiveDurationPct_EODAPI", "ETF_Data_Fixed_Income_EffectiveDuration_Fund_%", "Fixed Income EffectiveDuration, % of Fund", "Fixed Income Effective Duration Pct EODAPI" },
            new string[] { "FixedIncomeModifiedDurationPct_EODAPI", "ETF_Data_Fixed_Income_ModifiedDuration_Fund_%", "Fixed Income ModifiedDuration, % of Fund", "Fixed Income Modified Duration Pct EODAPI" },
            new string[] { "FixedIncomeEffectiveMaturityPct_EODAPI", "ETF_Data_Fixed_Income_EffectiveMaturity_Fund_%", "Fixed Income EffectiveMaturity, % of Fund", "Fixed Income Effective Maturity Pct EODAPI" },
            new string[] { "FixedIncomeCreditQualityPct_EODAPI", "ETF_Data_Fixed_Income_CreditQuality_Fund_%", "Fixed Income CreditQuality, % of Fund", "Fixed Income Credit Quality Pct EODAPI" },
            new string[] { "FixedIncomeCouponPct_EODAPI", "ETF_Data_Fixed_Income_Coupon_Fund_%", "Fixed Income Coupon, % of Fund", "Fixed Income Coupon Pct EODAPI" },
            new string[] { "FixedIncomePricePct_EODAPI", "ETF_Data_Fixed_Income_Price_Fund_%", "Fixed Income Price, % of Fund", "Fixed Income Price Pct EODAPI" },
            new string[] { "FixedIncomeYieldToMaturityPct_EODAPI", "ETF_Data_Fixed_Income_YieldToMaturity_Fund_%", "Fixed Income YieldToMaturity, % of Fund", "Fixed Income Yield To Maturity Pct EODAPI" },
            new string[] { "SectorWeightsBasicMaterialsPct_EODAPI", "ETF_Data.Sector_Weights.BasicMaterials.EquityPercent", "Sector Weights Basic Materials, % of Equity", "Sector Weights Basic Materials Pct EODAPI" },
            new string[] { "SectorWeightsConsumerCyclicalsPct_EODAPI", "ETF_Data.Sector_Weights.ConsumerCyclicals.EquityPercent", "Sector Weights Consumer Cyclicals, % of Equity", "Sector Weights Consumer Cyclicals Pct EODAPI" },
            new string[] { "SectorWeightsFinancialServicesPct_EODAPI", "ETF_Data.Sector_Weights.FinancialServices.EquityPercent", "Sector Weights Financial Services, % of Equity", "Sector Weights Financial Services Pct EODAPI" },
            new string[] { "SectorWeightsRealEstatePct_EODAPI", "ETF_Data.Sector_Weights.RealEstate.EquityPercent", "Sector Weights Real Estate, % of Equity", "Sector Weights Real Estate Pct EODAPI" },
            new string[] { "SectorWeightsCommunicationServicesPct_EODAPI", "ETF_Data.Sector_Weights.CommunicationServices.EquityPercent", "Sector Weights Communication Services, % of Equity", "Sector Weights Communication Services Pct EODAPI" },
            new string[] { "SectorWeightsEnergyPct_EODAPI", "ETF_Data.Sector_Weights.Energy.EquityPercent", "Sector Weights Energy, % of Equity", "Sector Weights Energy Pct EODAPI" },
            new string[] { "SectorWeightsIndustrialsPct_EODAPI", "ETF_Data.Sector_Weights.Industrials.EquityPercent", "Sector Weights Industrials, % of Equity", "Sector Weights Industrials Pct EODAPI" },
            new string[] { "SectorWeightsTechnologyPct_EODAPI", "ETF_Data.Sector_Weights.Technology.EquityPercent", "Sector Weights Technology, % of Equity", "Sector Weights Technology Pct EODAPI" },
            new string[] { "SectorWeightsConsumerDefensivePct_EODAPI", "ETF_Data.Sector_Weights.ConsumerDefencive.EquityPercent", "Sector Weights Consumer Defensive, % of Equity", "Sector Weights Consumer Defensive Pct EODAPI" },
            new string[] { "SectorWeightsHealthcarePct_EODAPI", "ETF_Data.Sector_Weights.Healthcare.EquityPercent", "Sector Weights Healthcare, % of Equity", "Sector Weights Healthcare Pct EODAPI" },
            new string[] { "SectorWeightsUtilitiesPct_EODAPI", "ETF_Data.Sector_Weights.Utilities.EquityPercent", "Sector Weights Utilities, % of Equity", "Sector Weights Utilities Pct EODAPI" },
            new string[] { "WorldRegionsNorthAmericaPct_EODAPI", "ETF_Data.World_Regions.NorthAmerica.EquityPercent", "World Regions North America, % of Equity", "World Regions North America Pct EODAPI" },
            new string[] { "WorldRegionsUnitedKingdomPct_EODAPI", "ETF_Data.World_Regions.UnitedKingdom.EquityPercent", "World Regions United Kingdom, % of Equity", "World Regions United Kingdom Pct EODAPI" },
            new string[] { "WorldRegionsEuropeDevelopedPct_EODAPI", "ETF_Data.World_Regions.EuropeDeveloped.EquityPercent", "World Regions Europe Developed, % of Equity", "World Regions Europe Developed Pct EODAPI" },
            new string[] { "WorldRegionsEuropeEmergingPct_EODAPI", "ETF_Data.World_Regions.EuropeEmerging.EquityPercent", "World Regions Europe Emerging, % of Equity", "World Regions Europe Emerging Pct EODAPI" },
            new string[] { "WorldRegionsAfricaMiddleEastPct_EODAPI", "ETF_Data.World_Regions.AfricaMiddleEast.EquityPercent", "World Regions Africa/Middle East, % of Equity", "World Regions Africa/Middle East Pct EODAPI" },
            new string[] { "WorldRegionsJapanPct_EODAPI", "ETF_Data.World_Regions.Japan.EquityPercent", "World Regions Japan, % of Equity", "World Regions Japan Pct EODAPI" },
            new string[] { "WorldRegionsAustralasiaPct_EODAPI", "ETF_Data.World_Regions.Australasia.EquityPercent", "World Regions Australasia, % of Equity", "World Regions Australasia Pct EODAPI" },
            new string[] { "WorldRegionsAsiaDevelopedPct_EODAPI", "ETF_Data.World_Regions.AsiaDeveloped.EquityPercent", "World Regions Asia Developed, % of Equity", "World Regions Asia Developed Pct EODAPI" },
            new string[] { "WorldRegionsAsiaEmergingPct_EODAPI", "ETF_Data.World_Regions.AsiaEmerging.EquityPercent", "World Regions Asia Emerging, % of Equity", "World Regions Asia Emerging Pct EODAPI" },
            new string[] { "WorldRegionsLatinAmericaPct_EODAPI", "ETF_Data.World_Regions.LatinAmerica.EquityPercent", "World Regions Latin America, % of Equity", "World Regions Latin America Pct EODAPI" },
            //new string[] { "MainInvestmentName", "None", "None", "None" },
            //new string[] { "Ticker", "None", "None", "None" },
            //new string[] { "AssetType", "None", "None", "None" },
            //new string[] { "AssetDescription", "None", "None", "None" },
            //new string[] { "DataYears", "None", "None", "Data Years" },
            //new string[] { "StartDateAnalysis", "None", "None", "Data Start Date" },
            //new string[] { "EndDateAnalysis", "None", "None", "Data End Date" },
            //new string[] { "CurrentPrice", "None", "None", "Current Price" },
            //new string[] { "DividendYield", "None", "None", "Dividend Yield TTM" },
            //new string[] { "DividendYieldAverage", "None", "None", "Dividend Yield Average" },
            //new string[] { "DividendPayoutFrequency", "None", "None", "Dividend Payout Freq" },
            //new string[] { "BearMarketPerformanceAverage5Cycles", "None", "None", "Ave Dn Mrkt 5Cyc" },
            //new string[] { "BearMarketPerformanceAverage8Cycles", "None", "None", "Ave Dn Mrkt 8Cyc" },
            //new string[] { "BearMarketPerformanceAverage13Cycles", "None", "None", "Ave Dn Mrkt 13Cyc" },
            //new string[] { "BullMarketPerformanceAverage5Cycles", "None", "None", "Ave Up Mrkt 5Cyc" },
            //new string[] { "BullMarketPerformanceAverage8Cycles", "None", "None", "Ave Up Mrkt 8Cyc" },
            //new string[] { "BullMarketPerformanceAverage13Cycles", "None", "None", "Ave Up Mrkt 13Cyc" },
            //new string[] { "GainSinceBegin2022", "None", "None", "Gain Since Begin 2022" },
            //new string[] { "Ranking", "None", "None", "Ranking" },
            //new string[] { "DividendYieldRank", "None", "None", "Dividend Yield Rank" },
            //new string[] { "DividendYieldAverageRank", "None", "None", "Dividend Yield Average Rank" },
            //new string[] { "AnnualizedReturn10YrRank", "None", "None", "Anlized Rtn 10Yr Rank" },
            //new string[] { "Volatility10YrRank", "None", "None", "Volatility 10Yr Rank" },
            //new string[] { "BearMarketPerformanceAverage5CyclesRank", "None", "None", "Dn Mrkt Average 5Cyc Rank" },
            //new string[] { "BullMarketPerformanceAverage5CyclesRank", "None", "None", "Up Mrkt Average 5Cyc Rank" },
            //new string[] { "GainSinceBegin2022Rank", "None", "None", "Gain Since Begin 2022 Rank" }
        };

        public static class FieldMappings
        {
            public static List<FieldMapping<object>> Mappings(IStatusUpdater statusUpdater) => GenerateFieldMappings(statusUpdater);

            private static List<FieldMapping<object>> GenerateFieldMappings(IStatusUpdater statusUpdater)
            {
                var mappings = new List<FieldMapping<object>>();

                foreach (var mapping in MappingList)
                {
                    string fieldName = mapping[0];
                    string apiHeader = mapping[1];
                    string outputHeader = mapping[2];

                    // Validate that the field exists in InvestmentData
                    var property = typeof(InvestmentData).GetProperty(fieldName);
                    if (property == null && fieldName != "CurrentPrice" && fieldName != "DataYears" && fieldName != "Ranking")
                    {
                        statusUpdater.UpdateStatus($"Warning: Field '{fieldName}' does not exist in InvestmentData. Skipping this mapping.");
                        continue;
                    }

                    mappings.Add(new FieldMapping<object>
                    {
                        FieldName = fieldName,
                        APIHeader = apiHeader,
                        OutputHeader = outputHeader,
                        AssignValue = (investmentData, value) =>
                        {
                            if (property != null && property.PropertyType == typeof(double))
                            {
                                if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                                {
                                    // Normalize the value (remove commas, trim spaces, etc.)
                                    string normalizedValue = value.ToString()?.Replace(",", "").Trim();

                                    // Skip processing for specific invalid values like 'n/a'
                                    if (normalizedValue.Equals("n/a", StringComparison.OrdinalIgnoreCase))
                                    {
                                        //statusUpdater.UpdateStatus($"  Field Mapping - Skipping assignment for 'n/a' value for field '{fieldName}'.");
                                        return; // Skip the assignment process
                                    }

                                    // Attempt to parse the normalized value
                                    double result;
                                    if (double.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                                    {
                                        property.SetValue(investmentData, result);
                                    }
                                    else
                                    {
                                        // Log or handle the error if parsing fails
                                        statusUpdater.UpdateStatus($"  Field Mapping - Unable to parse '{value}' as a double for field '{fieldName}'.");
                                    }
                                }
                                else
                                {
                                    // Log or handle the case where value is null or empty
                                    statusUpdater.UpdateStatus($"  Field Mapping - Value for field '{fieldName}' is null or empty.");
                                }
                            }
                            else if (property?.PropertyType == typeof(string))
                            {
                                property.SetValue(investmentData, Convert.ToString(value));
                            }
                            else if (property?.PropertyType == typeof(DateTime))
                            {
                                if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                                {
                                    // Normalize the value (trim spaces, etc.)
                                    string normalizedValue = value.ToString()?.Trim();

                                    // Skip processing for specific invalid values like 'n/a'
                                    if (normalizedValue.Equals("n/a", StringComparison.OrdinalIgnoreCase))
                                    {
                                        return; // Skip the assignment process
                                    }

                                    // Attempt to parse the normalized value as a DateTime
                                    DateTime result;
                                    if (DateTime.TryParse(normalizedValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                                    {
                                        property.SetValue(investmentData, result);
                                    }
                                    else
                                    {
                                        statusUpdater.UpdateStatus($"  Field Mapping - Unable to parse '{value}' as a DateTime for field '{fieldName}'.");
                                    }
                                }
                                else
                                {
                                    statusUpdater.UpdateStatus($"  Field Mapping - Value for field '{fieldName}' is null or empty.");
                                }
                            }
                        },
                        GetValue = investmentData =>
                        {
                            // Retrieve the value dynamically from the InvestmentData object
                            // Handle special fields like CurrentPrice and DataYears
                            if (fieldName == "CurrentPrice")
                            {
                                return investmentData.MainInvestmentPriceArray?.LastOrDefault()?[4];
                            }
                            else if (fieldName == "DataYears")
                            {
                                return (investmentData.EndDateAnalysis - investmentData.StartDateAnalysis).TotalDays / 365;
                            }

                            // Default behavior for other fields
                            return property?.GetValue(investmentData);
                        }
                    });
                    //statusUpdater.UpdateStatus($"Successfully added mapping for FieldName: {fieldName}, APIHeader: {apiHeader}, OutputHeader: {outputHeader}");
                }

                return mappings;
            }
        }
    }
}










namespace BeatTheMarketApp.InvestmentLibrary
{
    public class InvestmentData
    {
        // Asset Profile.xlsx  Parameters
        public string MainInvestmentName { get; set; }
        public string Ticker { get; set; }
        public string DataFile { get; set; }
        public string ExcelTab { get; set; }
        public DateTime StartDateAnalysis { get; set; }
        public DateTime EndDateAnalysis { get; set; }
        public List<DateTime> DateLog { get; set; } = new List<DateTime>();
        public List<double> MainInvestmentCloseDateNumber { get; set; } = new List<double>();
        public List<double[]> MainInvestmentPriceArray { get; set; } = new List<double[]>();
        public List<double> MainInvestmentClosePrice { get; set; } = new List<double>();
        public List<int> YearArray { get; set; } = new List<int>();
        public string AssetDescription { get; set; }
        public string AssetType { get; set; }

        //IE App Calculated Parameters
        public double Gain { get; set; }
        public double GainSinceBegin2022 { get; set; }
        public int NYears { get; set; }
        public List<DividendHistoryEntry> DividendHistory { get; set; } = new List<DividendHistoryEntry>();
        public List<DividendPayoutAnnualEntry> DividendPayoutAnnual { get; set; } = new List<DividendPayoutAnnualEntry>();
        public double DividendPayoutFrequency { get; set; }
        public double DividendYield { get; set; }
        public double DividendYieldAverage { get; set; }
        public double MarketHigh { get; set; }
        public DateTime MarketHighDate { get; set; }
        public double MarketLow { get; set; }
        public DateTime MarketLowDate { get; set; }
        public double MarketCorrectionFromHigh { get; set; }
        public double MarketCorrectionFromLow { get; set; }
        public double CompoundedAnnualReturnRateOverall { get; set; }
        public List<double> AnnualReturnRates { get; set; } = new List<double>();
        public double[] Performance { get; set; }
        public double[] CompoundedAnnualReturn { get; set; }
        public List<double> Volatility { get; set; } = new List<double>();
        public DailyReturnRate DailyReturnRate { get; set; }
        public List<double> AverageDailyReturnRate { get; set; } = new List<double>();
        public List<double> BearMarketPerformance { get; set; } = new List<double>();
        public int BearMarketPerformancePeriodsCount { get; set; }
        public double BearMarketPerformanceAverage5Cycles { get; set; }
        public double BearMarketPerformanceAverage8Cycles { get; set; }
        public double BearMarketPerformanceAverage13Cycles { get; set; }
        public List<double> BullMarketPerformance { get; set; } = new List<double>();
        public int BullMarketPerformancePeriodsCount { get; set; }
        public double BullMarketPerformanceAverage5Cycles { get; set; }
        public double BullMarketPerformanceAverage8Cycles { get; set; }
        public double BullMarketPerformanceAverage13Cycles { get; set; }
        public List<DrawDownPeriod> DrawDownPeriods { get; set; } = new List<DrawDownPeriod>();
        public int NumberOfDrawdownPeriods { get; set; }
        public double MaxDrawdown { get; set; }
        public List<DrawUpPeriod> DrawUpPeriods { get; set; } = new List<DrawUpPeriod>();
        public string Category { get; set; } = string.Empty;

        // AlphaVantage API Parameters
        public string AssetTypeAPI { get; set; } = string.Empty;
        public string NameAPI { get; set; } = string.Empty;
        public string DescriptionAPI { get; set; } = string.Empty;
        public string SectorAPI { get; set; } = string.Empty;
        public string IndustryAPI { get; set; } = string.Empty;
        public double MarketCapAPI { get; set; } = double.NaN;
        public double PriceEarningsTrailingAPI { get; set; } = double.NaN;
        public double PriceEarningsForwardAPI { get; set; } = double.NaN;
        public double PEGRatioAPI { get; set; } = double.NaN;
        public double DividendYieldTTMAPI { get; set; } = double.NaN;
        public double DividendYieldETFTTMAPI { get; set; } = double.NaN;
        public double PriceToSalesRatioTTMAPI { get; set; } = double.NaN;
        public double PriceToBookRatioAPI { get; set; } = double.NaN;
        public double BetaAPI { get; set; } = double.NaN;
        public double ProfitMarginPctAPI { get; set; } = double.NaN;
        public double OperatingMarginTTMPctAPI { get; set; } = double.NaN;
        public double ReturnOnAssetsTTMPctAPI { get; set; } = double.NaN;
        public double ReturnOnEquityTTMPctAPI { get; set; } = double.NaN;
        public double QuarterlyEarningsGrowthYOYPctAPI { get; set; } = double.NaN;
        public double QuarterlyRevenueGrowthYOYPctAPI { get; set; } = double.NaN;
        public double EnterpriseValueToRevenueAPI { get; set; } = double.NaN;
        public double EnterpriseValueToEBITDAAPI { get; set; } = double.NaN;
        public double AnalystTargetPriceAPI { get; set; } = double.NaN;
        public DateTime DividendDateAPI { get; set; } = DateTime.MinValue;
        public DateTime ExDividendDateAPI { get; set; } = DateTime.MinValue;
        public double NetAssetsAPI { get; set; } = double.NaN;
        public double ExpenseRatioAPI { get; set; } = double.NaN;
        public double PortfolioTurnoverPctAPI { get; set; } = double.NaN;
        public DateTime InceptionDateAPI { get; set; } = DateTime.MinValue;
        public double TotalReturn5YearAPI { get; set; } = double.NaN;
        public double AssetsInTop10HoldingsPctAPI { get; set; } = double.NaN;
        public double NumberOfHoldingsAPI { get; set; } = double.NaN;
        public double Beta3YearAPI { get; set; } = double.NaN;
        public double Alpha3YearAPI { get; set; } = double.NaN;

        // EODHD API Parameters
        public string Ticker_EODAPI { get; set; } = string.Empty;
        public string AssetType_EODAPI { get; set; } = string.Empty;
        public string AssetName_EODAPI { get; set; } = string.Empty;
        public string Description_EODAPI { get; set; } = string.Empty;
        public DateTime UpdatedAt_EODAPI { get; set; } = DateTime.MinValue;
        public DateTime InceptionDate_EODAPI { get; set; } = DateTime.MinValue;
        public string Category_EODAPI { get; set; } = string.Empty;
        public string Sector_EODAPI { get; set; } = string.Empty;
        public string Industry_EODAPI { get; set; } = string.Empty;
        public double MarketCapitalizationMillion_EODAPI { get; set; } = double.NaN;
        public double AverageMktCapMillion_EODAPI { get; set; } = double.NaN;
        public double TotalAssets_EODAPI { get; set; } = double.NaN;
        public double TrailingPE_EODAPI { get; set; } = double.NaN;
        public double ForwardPE_EODAPI { get; set; } = double.NaN;
        public double PriceProspectiveEarnings_EODAPI { get; set; } = double.NaN;
        public double PEGRatio_EODAPI { get; set; } = double.NaN;
        public double QuarterlyEarningsGrowthYOY_EODAPI { get; set; } = double.NaN;
        public double HistoricalEarningsGrowth_EODAPI { get; set; } = double.NaN;
        public double LongTermProjectedEarningsGrowth_EODAPI { get; set; } = double.NaN;
        public double EnterpriseValueEbitda_EODAPI { get; set; } = double.NaN;
        public double DividendYieldTTM_EODAPI { get; set; } = double.NaN;
        public double DividendYieldFwd_EODAPI { get; set; } = double.NaN;
        public double DistributionYieldTTM_EODAPI { get; set; } = double.NaN;
        public double PayoutRatio_EODAPI { get; set; } = double.NaN;
        public double DividendPayingFrequency_EODAPI { get; set; } = double.NaN;
        public double DividendDate_EODAPI { get; set; } = double.NaN;
        public double ExDividendDate_EODAPI { get; set; } = double.NaN;
        public double ProfitMarginTTM_EODAPI { get; set; } = double.NaN;
        public double OperatingMarginTTM_EODAPI { get; set; } = double.NaN;
        public double QuarterlyRevenueGrowthYOY_EODAPI { get; set; } = double.NaN;
        public double EnterpriseValueRevenue_EODAPI { get; set; } = double.NaN;
        public double PriceBookMRQ_EODAPI { get; set; } = double.NaN;
        public double PriceBook_EODAPI { get; set; } = double.NaN;
        public double BookValue_EODAPI { get; set; } = double.NaN;
        public double BookValueGrowth_EODAPI { get; set; } = double.NaN;
        public double PriceSalesTTM_EODAPI { get; set; } = double.NaN;
        public double PriceSales_EODAPI { get; set; } = double.NaN;
        public double SalesGrowth_EODAPI { get; set; } = double.NaN;
        public double PriceCashFlow_EODAPI { get; set; } = double.NaN;
        public double CashFlowGrowth_EODAPI { get; set; } = double.NaN;
        public double Beta_EODAPI { get; set; } = double.NaN;
        public double Volatility1y_EODAPI { get; set; } = double.NaN;
        public double Volatility3y_EODAPI { get; set; } = double.NaN;
        public double ReturnOnAssetsTTM_EODAPI { get; set; } = double.NaN;
        public double ReturnOnEquityTTM_EODAPI { get; set; } = double.NaN;
        public double ExpectedReturn3y_EODAPI { get; set; } = double.NaN;
        public double SharpRatio3y_EODAPI { get; set; } = double.NaN;
        public double AnnualizedReturnYTD_EODAPI { get; set; } = double.NaN;
        public double AnnualizedReturn1Y_EODAPI { get; set; } = double.NaN;
        public double AnnualizedReturn3Y_EODAPI { get; set; } = double.NaN;
        public double AnnualizedReturn5Y_EODAPI { get; set; } = double.NaN;
        public double AnnualizedReturn10Y_EODAPI { get; set; } = double.NaN;
        public double AnnualHoldingsTurnover_EODAPI { get; set; } = double.NaN;
        public double HoldingsCount_EODAPI { get; set; } = double.NaN;
        public double NetExpenseRatio_EODAPI { get; set; } = double.NaN;
        public double AnalystRating_EODAPI { get; set; } = double.NaN;
        public double AnalystTargetPrice_EODAPI { get; set; } = double.NaN;
        public double WallStreetTargetPrice_EODAPI { get; set; } = double.NaN;
        public string Category_Benchmark_EODAPI { get; set; } = string.Empty;
        public double MorningStarRating_EODAPI { get; set; } = double.NaN;
        public double ShareShortPercentOfFloat_EODAPI { get; set; } = double.NaN;
        public double PercentInsiders_EODAPI { get; set; } = double.NaN;
        public double PercentInstitutions_EODAPI { get; set; } = double.NaN;
        public double PercentMrktCapMega_EODAPI { get; set; } = double.NaN;
        public double PercentMrktCapBig_EODAPI { get; set; } = double.NaN;
        public double PercentMrktCapMedium_EODAPI { get; set; } = double.NaN;
        public double PercentMrktCapSmall_EODAPI { get; set; } = double.NaN;
        public double PercentMrktCapMicro_EODAPI { get; set; } = double.NaN;
        public double AssetAllocationCashPct_EODAPI { get; set; } = double.NaN;
        public double AssetAllocationNotClassifiedPct_EODAPI { get; set; } = double.NaN;
        public double AssetAllocationStockNonUSPct_EODAPI { get; set; } = double.NaN;
        public double AssetAllocationOtherPct_EODAPI { get; set; } = double.NaN;
        public double AssetAllocationStockUSPct_EODAPI { get; set; } = double.NaN;
        public double AssetAllocationBondPct_EODAPI { get; set; } = double.NaN;
        public double FixedIncomeEffectiveDurationPct_EODAPI { get; set; } = double.NaN;
        public double FixedIncomeModifiedDurationPct_EODAPI { get; set; } = double.NaN;
        public double FixedIncomeEffectiveMaturityPct_EODAPI { get; set; } = double.NaN;
        public double FixedIncomeCreditQualityPct_EODAPI { get; set; } = double.NaN;
        public double FixedIncomeCouponPct_EODAPI { get; set; } = double.NaN;
        public double FixedIncomePricePct_EODAPI { get; set; } = double.NaN;
        public double FixedIncomeYieldToMaturityPct_EODAPI { get; set; } = double.NaN;
        public double SectorWeightsBasicMaterialsPct_EODAPI { get; set; } = double.NaN;
        public double SectorWeightsConsumerCyclicalsPct_EODAPI { get; set; } = double.NaN;
        public double SectorWeightsFinancialServicesPct_EODAPI { get; set; } = double.NaN;
        public double SectorWeightsRealEstatePct_EODAPI { get; set; } = double.NaN;
        public double SectorWeightsCommunicationServicesPct_EODAPI { get; set; } = double.NaN;
        public double SectorWeightsEnergyPct_EODAPI { get; set; } = double.NaN;
        public double SectorWeightsIndustrialsPct_EODAPI { get; set; } = double.NaN;
        public double SectorWeightsTechnologyPct_EODAPI { get; set; } = double.NaN;
        public double SectorWeightsConsumerDefensivePct_EODAPI { get; set; } = double.NaN;
        public double SectorWeightsHealthcarePct_EODAPI { get; set; } = double.NaN;
        public double SectorWeightsUtilitiesPct_EODAPI { get; set; } = double.NaN;
        public double WorldRegionsNorthAmericaPct_EODAPI { get; set; } = double.NaN;
        public double WorldRegionsUnitedKingdomPct_EODAPI { get; set; } = double.NaN;
        public double WorldRegionsEuropeDevelopedPct_EODAPI { get; set; } = double.NaN;
        public double WorldRegionsEuropeEmergingPct_EODAPI { get; set; } = double.NaN;
        public double WorldRegionsAfricaMiddleEastPct_EODAPI { get; set; } = double.NaN;
        public double WorldRegionsJapanPct_EODAPI { get; set; } = double.NaN;
        public double WorldRegionsAustralasiaPct_EODAPI { get; set; } = double.NaN;
        public double WorldRegionsAsiaDevelopedPct_EODAPI { get; set; } = double.NaN;
        public double WorldRegionsAsiaEmergingPct_EODAPI { get; set; } = double.NaN;
        public double WorldRegionsLatinAmericaPct_EODAPI { get; set; } = double.NaN;

        // Ranking Parameters
        public double DividendYieldRank { get; set; } = double.NaN;
        public double DividendYieldAverageRank { get; set; } = double.NaN;
        public double AnnualizedReturn10YrRank { get; set; } = double.NaN;
        public double Volatility10YrRank { get; set; } = double.NaN;
        public double BearMarketPerformanceAverage5CyclesRank { get; set; } = double.NaN;
        public double BullMarketPerformanceAverage5CyclesRank { get; set; } = double.NaN;
        public double GainSinceBegin2022Rank { get; set; } = double.NaN;
        public List<double[]> Performance10kArray { get; set; } = new List<double[]>(); // Updated to store multiple durations
        public List<DateTime[]> Performance10kDateLog { get; set; } = new List<DateTime[]>(); // New field for date log to capture the date arry for each of the Performance10kArray durations
        public double[] Beta { get; set; } = Array.Empty<double>();
        public string EndingDate { get; set; } = string.Empty;
    }
}