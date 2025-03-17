function [resultsTrendAnalysis, UpTrend, DownTrend, trend] =  TrendAnalysis(mainInvestmentCloseDateNumber, mainInvestmentClosePrice, marketLevel)

% Initialize parameters
day=0; %Starting Day

% Headings
fprintf('dy  Ps ,      mode,         submode,      mileS, I_ML, I_MH, C_HH, C_HL, C_LH, C_LL, mrktL, mrktH modePred\n')

for i=1:length(mainInvestmentCloseDateNumber) % Run backtesting loop
    day=day+1;
    dateNum = mainInvestmentCloseDateNumber(i,1);
    sharePriceMainInvestment = mainInvestmentClosePrice(i,1); % $/share
    
    % Initialize variables for current day
    mileStone = 'none';
    
    % Deterimine Market Trend and Status
    if day == 1
        mode = 'Undetermined';
        submode = 'Undetermined';
        modePrediction = mode;
        InitiateDownTrend = 0;
        InitiateUpTrend = 0;
        previousDaysharePriceMainInvestment = sharePriceMainInvestment;
        
        %currentHigherHigh = sharePriceMainInvestment*1000;
        currentHigherHigh = sharePriceMainInvestment/1000;
        dayCurrentHigherHigh = 1;
        currentHigherLow = sharePriceMainInvestment/1000;
        dayCurrentHigherLow = 1;
        interimMarketHigh = sharePriceMainInvestment/1000;
        marketHigh=sharePriceMainInvestment*1000;
        
        currentLowerHigh = sharePriceMainInvestment*1000;
        dayCurrentLowerHigh = 1;
        currentLowerLow = sharePriceMainInvestment*1000;
        dayCurrentLowerLow = 1;
        interimMarketLow = sharePriceMainInvestment*1000;
        marketLow=sharePriceMainInvestment/1000;
        
    elseif (day == 2)
        [mode,submode] = Pattern_Eval(mainInvestmentClosePrice(day-1,1),mainInvestmentClosePrice(day,1),mainInvestmentClosePrice(day+1,1),mainInvestmentClosePrice(day+2,1));
        modePrediction = mode;
        % Case 1 - Market moving up - Establish UpTrend and checkfor HigherHigh
        if strcmp(mode,'UpTrend')
            if marketLevel(day,3) == 2 %% Check for Higher low
                mileStone = 'HigherLow';
                submode = 'UpTrendUndetermined';
                currentHigherLow = sharePriceMainInvestment;
                dayCurrentHigherLow = 2;
                interimMarketLow = sharePriceMainInvestment;
                currentHigherHigh = previousDaysharePriceMainInvestment;
                dayCurrentHigherHigh = 1;
                UpTrend(day-1,3) = 1; %HH - Make correction
                interimMarketHigh= previousDaysharePriceMainInvestment;
            elseif marketLevel(day,3) == 1 %% Check for Higher high
                mileStone = 'HigherHigh';
                submode = 'UpTrendUndetermined';
                currentHigherHigh = sharePriceMainInvestment;
                dayCurrentHigherHigh = 2;
                interimMarketHigh = sharePriceMainInvestment;
                currentHigherLow = previousDaysharePriceMainInvestment;
                dayCurrentHigherLow= 1;
                UpTrend(day-1,3) = 2; %HL - Make correction
                interimMarketLow = previousDaysharePriceMainInvestment;
            else
                waitfor(msgbox({'Day 2: UpTrend Conumdrum.';'';'Click in cmd window > press CTRL-C > press OK .'},'Settings Issue'))
                submode = 'UpTrendUndetermined';
            end
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
            % Case 2 - Market moving down
        elseif strcmp(mode,'DownTrend')
            if marketLevel(day,3) == 1 %% Check for Lower High
                mileStone = 'LowerHigh';
                submode = 'DownTrendUndetermined';
                currentLowerHigh = sharePriceMainInvestment;
                dayCurrentLowerHigh = 2;
                interimMarketHigh = sharePriceMainInvestment;
                currentLowerLow = previousDaysharePriceMainInvestment;
                dayCurrentLowerLow = 1;
                DownTrend(day-1,3) = 2; %LL - Make correction
                interimMarketLow = previousDaysharePriceMainInvestment;
            elseif marketLevel(day,3) == 2 %% Check for Lower Low
                mileStone = 'LowerLow';
                submode = 'DownTrendUndetermined';
                currentLowerLow = sharePriceMainInvestment;
                dayCurrentLowerLow = 2;
                interimMarketLow = sharePriceMainInvestment;
                currentLowerHigh = previousDaysharePriceMainInvestment;
                dayCurrentLowerHigh = 1;
                DownTrend(day-1,3) = 1; %LH - Make correction
                interimMarketHigh = previousDaysharePriceMainInvestment;
            else
                waitfor(msgbox({'Day 2: DownTrend Conumdrum.';'';'Click in cmd window > press CTRL-C > press OK .'},'Settings Issue'))
                submode = 'DownTrendUndetermined';
            end
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
            % Case 3 - Market unchanged
        else
            mode = 'Undetermined';
            submode = 'Undetermined';
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
        end
        
        %% Analyze the Uptrend
    elseif (day > 2 && strcmp(mode,'UpTrend'))
        [modePrediction,~] = Pattern_Eval(mainInvestmentClosePrice(day-1,1),mainInvestmentClosePrice(day,1),mainInvestmentClosePrice(day+1,1),mainInvestmentClosePrice(day+2,1));
        %[modePrediction,~] = Pattern_Eval(mainInvestmentClosePrice(day,1),mainInvestmentClosePrice(day+1,1),mainInvestmentClosePrice(day+2,1),mainInvestmentClosePrice(day+3,1));
        % Case 1 - Market Up Day - Moving up from Higher Low - Check for UpTrendImpulse and/or Higher High
        if (sharePriceMainInvestment > marketLow && (sharePriceMainInvestment > interimMarketHigh || sharePriceMainInvestment > currentHigherLow)  && sharePriceMainInvestment > previousDaysharePriceMainInvestment)
            % Subcase 1a
            %if(sharePriceMainInvestment > currentHigherHigh || InitiateUpTrend == 0)
            if(sharePriceMainInvestment > currentHigherHigh)
                sc1a=1
                % Subcase 1a1
                if (marketLevel(day,3) == 1 && dayCurrentHigherHigh <= dayCurrentHigherLow) %% Check for Higher High
                    sc1a1_1=1
                    mileStone = 'HigherHigh';
                    currentHigherHigh = sharePriceMainInvestment;
                    dayCurrentHigherHigh = day;
                    submode = 'UpTrendImpulse';
                elseif (marketLevel(day,3) == 1 && dayCurrentHigherHigh >= dayCurrentHigherLow && strcmp(submode,'UpTrendReversal')) %% Check for Higher High
                    sc1a1_2=1
                    mileStone = 'HigherHigh';
                    currentHigherHigh = sharePriceMainInvestment;
                    dayCurrentHigherHigh = day;
                    currentHigherLow = previousDaysharePriceMainInvestment;
                    UpTrend(day-1,3) = 1; %HH - Make correction
                    dayCurrentHigherLow = day-1;
                    submode = 'UpTrendImpulse';
                elseif strcmp(submode,'UpTrendReversal') %% Check for Higher High
                    sc1a1_3=1
                    currentHigherLow = previousDaysharePriceMainInvestment;
                    UpTrend(day-1,3) = 1; %HH - Make correction
                    dayCurrentHigherLow = day-1;
                    submode = 'UpTrendImpulse';
                elseif (strcmp(submode,'UpTrendUndetermined') || strcmp(submode,'UpTrendImpulse')) %% Check for DownTrendImpulse
                    sc1a1_4=1
                    submode = 'UpTrendImpulse';
                else
                    sc1a1_5=1
                    submode = 'UpTrendUndetermined';
                end
                % Subcase 1b
            else
                sc1b=1
                submode = 'UpTrendUndetermined';
            end
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
            interimMarketHigh = sharePriceMainInvestment; %New interim high
            % Case 2 - Market Down Day - Check for UpTrendPullback and/or HigherLow
        elseif (sharePriceMainInvestment > marketLow && sharePriceMainInvestment < interimMarketHigh && sharePriceMainInvestment <= previousDaysharePriceMainInvestment)
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
            % Subcase 2a - Check for UpTrendPullback, HigherLow, or reversal during UpTrendImpulse when sharePriceMainInvestment < interimMarketHigh && sharePriceMainInvestment <= previousDaysharePriceMainInvestment
            if(strcmp(submode,'UpTrendUndetermined') || strcmp(submode,'UpTrendImpulse'))
                % Subcase 2a1 - Check for UpTrendPullback and/or HigherLow during UpTrendImpulse when sharePriceMainInvestment is between currentHigherHigh and currentHigherLow
                if (sharePriceMainInvestment < currentHigherHigh && sharePriceMainInvestment > currentHigherLow)
                    sc2a1_0=1
                    interimMarketLow = sharePriceMainInvestment;
                    submode = 'UpTrendPullback';
                    % Subcase 2a1a - May not need this
                    if(marketLevel(day,3) == 2 && dayCurrentHigherHigh >= dayCurrentHigherLow) %% Check for Higher Low
                        sc2a1a=1
                        mileStone = 'HigherLow';
                        currentHigherLow = sharePriceMainInvestment;
                        dayCurrentHigherLow = day;
                    end
                    % Subcase 2a1 - Check for Reversal sharePriceMainInvestment is less than currentHigherHigh and currentHigherLow
                elseif (sharePriceMainInvestment < currentHigherHigh && sharePriceMainInvestment < currentHigherLow && strcmp(modePrediction,'UpTrend'))
                    sc2a1_1=1
                    interimMarketLow = sharePriceMainInvestment;
                    %submode = 'UpTrendReversal';
                    submode = 'UpTrendPullback';
                    if(marketLevel(day,3) == 2 && dayCurrentHigherHigh >= dayCurrentHigherLow) %% Check for Higher Low
                        sc2a1_1a=1
                        mileStone = 'HigherLow';
                        currentHigherLow = sharePriceMainInvestment;
                        dayCurrentHigherLow = day;
                    end
                    % Subcase 2a1a - Check for Reversal sharePriceMainInvestment is less than currentHigherHigh and currentHigherLow
                elseif (sharePriceMainInvestment < currentHigherHigh && sharePriceMainInvestment < currentHigherLow && strcmp(modePrediction,'DownTrend'))
                    sc2a1a_1=1
                    mode = 'DownTrend';
                    submode = 'DownTrendUndetermined';
                    currentHigherHigh = sharePriceMainInvestment/1000;
                    dayCurrentHigherHigh = 1;
                    currentHigherLow = sharePriceMainInvestment/1000;
                    dayCurrentHigherLow = 1;
                    interimMarketHigh = sharePriceMainInvestment/1000;
                    marketHigh=sharePriceMainInvestment*1000;
                    if(marketLevel(day,3) == 2) %% Check for Lower Low
                        sc2c_1=1
                        mileStone = 'LowerLow';
                        currentLowerLow = sharePriceMainInvestment;
                        dayCurrentLowerLow = day;
                    end
                    % Subcase 2a2 - May not need this
                elseif (sharePriceMainInvestment > currentHigherHigh)
                    sc2a2=1
                    submode = 'UpTrendPullback';
                    interimMarketHigh = sharePriceMainInvestment;
                    % Subcase 2a2a
                    if (marketLevel(day,3) == 2 && dayCurrentHigherHigh >= dayCurrentHigherLow) %% Check for Higher Low
                        sc2a2a=1
                        mileStone = 'HigherLow';
                        currentHigherLow = sharePriceMainInvestment;
                        dayCurrentHigherLow = day;
                    end
                end
                % Subcase 2b - Check for UpTrendPullback and/or HigherLow during UpTrendPullback when sharePriceMainInvestment >= currentHigherLow
            elseif(strcmp(submode,'UpTrendPullback') && sharePriceMainInvestment < currentHigherHigh && sharePriceMainInvestment > currentHigherLow)
                sc2b=1
                interimMarketLow = sharePriceMainInvestment;
                submode = 'UpTrendPullback';
                % Subcase 2b1
                if(marketLevel(day,3) == 2 && dayCurrentHigherHigh >= dayCurrentHigherLow) %% Check for Higher Low
                    sc2b1=1
                    mileStone = 'HigherLow';
                    currentHigherLow = sharePriceMainInvestment;
                    dayCurrentHigherLow = day;
                end
                % Subcase 2c  - Check for Start of DownTrend during UpTrendPullback when sharePriceMainInvestment < currentHigherLow
            elseif((strcmp(submode,'UpTrendPullback') || strcmp(submode,'UpTrendReversal')) && sharePriceMainInvestment < currentHigherLow)
                sc2c=1
                interimMarketLow = sharePriceMainInvestment;
                % Subcase 2c1
                mode = 'DownTrend';
                submode = 'DownTrendUndetermined';
                %marketHigh = currentHigherHigh;
                currentHigherHigh = sharePriceMainInvestment/1000;
                dayCurrentHigherHigh = 1;
                currentHigherLow = sharePriceMainInvestment/1000;
                dayCurrentHigherLow = 1;
                interimMarketHigh = sharePriceMainInvestment/1000;
                marketHigh=sharePriceMainInvestment*1000;
                if(marketLevel(day,3) == 2) %% Check for Lower Low
                    sc2c_1=1
                    mileStone = 'LowerLow';
                    currentLowerLow = sharePriceMainInvestment;
                    dayCurrentLowerLow = day;
                end
                % Subcase 2d
            else
                submode = 'UpTrendReversal';
                waitfor(msgbox({'Error. UpTrend; Cant decide on submode type';'';'Click in cmd window > press CTRL-C > press OK .'},'Settings Issue'))
            end
            % Case 3 - Market-up Day - Check for UpTrendImpulse and/or HigherHigh during UpTrendPullback when sharePriceMainInvestment < interimMarketHigh && sharePriceMainInvestment > previousDaysharePriceMainInvestment
        elseif (sharePriceMainInvestment > marketLow && sharePriceMainInvestment < interimMarketHigh && sharePriceMainInvestment > previousDaysharePriceMainInvestment)
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
            interimMarketHigh = sharePriceMainInvestment;
            if(strcmp(submode,'UpTrendUndetermined') || strcmp(submode,'UpTrendImpulse'))
                waitfor(msgbox({'Error. Submode issue';'';'Click in cmd window > press CTRL-C > press OK .'},'Settings Issue'))
                % Subcase 3a
            elseif(strcmp(submode,'UpTrendPullback'))
                sc3a=1
                interimMarketHigh = sharePriceMainInvestment;
                % Subcase 3a1
                if (marketLevel(day,3) == 1 && dayCurrentHigherHigh <= dayCurrentHigherLow && sharePriceMainInvestment > currentHigherHigh)%% Check for Higher High
                    sc3a1=1
                    mileStone = 'HigherHigh';
                    submode = 'UpTrendImpulse'; %added 8/6
                    currentHigherHigh = sharePriceMainInvestment;
                    dayCurrentHigherHigh = day;
                    % Subcase 3a2
                else
                    sc3a2=1
                    submode = 'UpTrendUndetermined';
                end
                % Subcase 3b
            else
                sc3b=1
                submode = 'UpTrendUndetermined';
            end
            % Case 4
        elseif (sharePriceMainInvestment < marketLow && sharePriceMainInvestment < interimMarketHigh && sharePriceMainInvestment < previousDaysharePriceMainInvestment)
            c4=1
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
            mode = 'DownTrend';
            submode = 'DownTrendUndetermined';
            currentHigherHigh = sharePriceMainInvestment/1000;
            dayCurrentHigherHigh = 1;
            currentHigherLow = sharePriceMainInvestment/1000;
            dayCurrentHigherLow = 1;
            interimMarketHigh = sharePriceMainInvestment/1000;
            marketHigh=sharePriceMainInvestment*1000;
            % Case 5 - Market unchanged
        else
            c5=1
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
            mode = 'Undetermined';
            submode = 'Undetermined';
        end
        
        %% Analyze the Downtrend
    elseif (day > 2 && strcmp(mode,'DownTrend'))
        [modePrediction,~] = Pattern_Eval(mainInvestmentClosePrice(day-1,1),mainInvestmentClosePrice(day,1),mainInvestmentClosePrice(day+1,1),mainInvestmentClosePrice(day+2,1));
        %[modePrediction,~] = Pattern_Eval(mainInvestmentClosePrice(day,1),mainInvestmentClosePrice(day+1,1),mainInvestmentClosePrice(day+2,1),mainInvestmentClosePrice(day+3,1));
        % Case 1
        if (sharePriceMainInvestment < marketHigh && (sharePriceMainInvestment < interimMarketLow || sharePriceMainInvestment < currentLowerHigh) && sharePriceMainInvestment < previousDaysharePriceMainInvestment)
            % Subcase 1a
            %if(sharePriceMainInvestment < currentLowerLow || InitiateDownTrend == 0)
            if(sharePriceMainInvestment < currentLowerLow)
                sc1a=1
                %InitiateDownTrend = 1;
                if (marketLevel(day,3) == 2 && dayCurrentLowerLow <= dayCurrentLowerHigh) %% Check for Lower Low
                    sc1a1_1=1
                    mileStone = 'LowerLow';
                    currentLowerLow = sharePriceMainInvestment;
                    dayCurrentLowerLow = day;
                    submode = 'DownTrendImpulse';
                elseif (marketLevel(day,3) == 2 && dayCurrentLowerLow >= dayCurrentLowerHigh && strcmp(submode,'DownTrendReversal')) %% Check for LowerLow
                    sc1a1_2=1
                    mileStone = 'LowerLow';
                    currentLowerLow = sharePriceMainInvestment;
                    dayCurrentLowerLow = day;
                    currentLowerHigh = previousDaysharePriceMainInvestment;
                    DownTrend(day-1,3) = 2; %LL - Make correction
                    dayCurrentLowerHigh = day-1;
                    submode = 'DownTrendImpulse';
                elseif strcmp(submode,'DownTrendReversal') %% Check for LowerLow
                    sc1a1_3=1
                    currentLowerHigh = previousDaysharePriceMainInvestment;
                    DownTrend(day-1,3) = 2; %LL - Make correction
                    dayCurrentLowerHigh = day-1;
                    submode = 'DownTrendImpulse';
                elseif (strcmp(submode,'DownTrendUndetermined') || strcmp(submode,'DownTrendImpulse')) %% Check for DownTrendImpulse
                    sc1a1_4=1
                    submode = 'DownTrendImpulse';
                else
                    sc1a1_5=1
                    submode = 'DownTrendUndetermined';
                end
                % Subcase 1b
            else
                sc1b=1
                submode = 'DownTrendUndetermined';
            end
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
            interimMarketLow = sharePriceMainInvestment; %New interim Low
            % Case 2
        elseif (sharePriceMainInvestment < marketHigh && sharePriceMainInvestment > interimMarketLow && sharePriceMainInvestment > previousDaysharePriceMainInvestment)
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
            % Subcase 2a
            if(strcmp(submode,'DownTrendUndetermined') || strcmp(submode,'DownTrendImpulse'))
                % Subcase 2a1
                if (sharePriceMainInvestment > currentLowerLow && sharePriceMainInvestment < currentLowerHigh)
                    sc2a1_0=1
                    interimMarketHigh = sharePriceMainInvestment;
                    submode = 'DownTrendPullback';
                    % Subcase 2a1a - May not need this
                    if (marketLevel(day,3) == 1 && dayCurrentLowerLow >= dayCurrentLowerHigh)%% Check for Lower High
                        sc2a1a=1
                        mileStone = 'LowerHigh';
                        currentLowerHigh = sharePriceMainInvestment;
                        dayCurrentLowerHigh = day;
                    end
                    % Subcase 2a1
                elseif (sharePriceMainInvestment > currentLowerLow && sharePriceMainInvestment > currentLowerHigh && strcmp(modePrediction,'DownTrend'))
                    sc2a1_1=1
                    interimMarketHigh = sharePriceMainInvestment;
                    %submode = 'DownTrendReversal';
                    submode = 'DownTrendPullback';
                    if(marketLevel(day,3) == 1 && dayCurrentLowerLow >= dayCurrentLowerHigh) %% Check for LowerHigh
                        sc2a1_1a=1
                        mileStone = 'LowerHigh';
                        currentLowerHigh = sharePriceMainInvestment;
                        dayCurrentLowerHigh = day;
                    end
                    % Subcase 2a1a
                elseif (sharePriceMainInvestment > currentLowerLow && sharePriceMainInvestment > currentLowerHigh && strcmp(modePrediction,'UpTrend'))
                    sc2a1a_1=1
                    mode = 'UpTrend';
                    submode = 'UpTrendUndetermined';
                    currentLowerHigh = sharePriceMainInvestment*1000;
                    dayCurrentLowerHigh = 1;
                    currentLowerLow = sharePriceMainInvestment*1000;
                    dayCurrentLowerLow = 1;
                    interimMarketLow = sharePriceMainInvestment*1000;
                    marketLow=sharePriceMainInvestment/1000;
                    if(marketLevel(day,3) == 1) %% Check for Higher High
                        sc2c_1=1
                        mileStone = 'HigherHigh';
                        currentHigherHigh = sharePriceMainInvestment;
                        dayCurrentHigherHigh = day;
                    end
                    % Subcase 2a2 - May not need this
                elseif (sharePriceMainInvestment < currentLowerLow )
                    sc2a2=1
                    submode = 'DownTrendPullback';
                    interimMarketLow = sharePriceMainInvestment;
                    % Subcase 2a2a
                    if (marketLevel(day,3) == 1 && dayCurrentLowerLow >= dayCurrentLowerHigh)%% Check for Lower High
                        sc2a2a=1
                        mileStone = 'LowerHigh';
                        currentLowerHigh = sharePriceMainInvestment;
                        dayCurrentLowerHigh = day;
                    end
                end
                % Subcase 2b
            elseif(strcmp(submode,'DownTrendPullback') && sharePriceMainInvestment > currentLowerLow && sharePriceMainInvestment < currentLowerHigh)
                sc2b=1
                interimMarketHigh = sharePriceMainInvestment;
                submode = 'DownTrendPullback';
                % Subcase 2b1
                if (marketLevel(day,3) == 1  && dayCurrentLowerLow >= dayCurrentLowerHigh)%% Check for Lower High
                    sc2b1=1
                    mileStone = 'LowerHigh';
                    currentLowerHigh = sharePriceMainInvestment;
                    dayCurrentLowerHigh = day;
                end
                % Subcase 2c
            elseif((strcmp(submode,'DownTrendPullback') || strcmp(submode,'DownTrendReversal')) && sharePriceMainInvestment > currentLowerHigh)
                sc2c=1
                interimMarketHigh = sharePriceMainInvestment;
                % Subcase 2c1
                mode = 'UpTrend';
                submode = 'UpTrendUndetermined';
                currentLowerHigh = sharePriceMainInvestment*1000;
                dayCurrentLowerHigh = 1;
                currentLowerLow = sharePriceMainInvestment*1000;
                dayCurrentLowerLow = 1;
                interimMarketLow = sharePriceMainInvestment*1000;
                marketLow=sharePriceMainInvestment/1000;
                if(marketLevel(day,3) == 1) %% Check for Higher High
                    sc2c_1=1
                    mileStone = 'HigherHigh';
                    currentHigherHigh = sharePriceMainInvestment;
                    dayCurrentHigherHigh = day;
                end
                % Subcase 2d
            else
                submode = 'DownTrendReversal';
                waitfor(msgbox({'Error. DownTrend; Cant decide on submode type';'';'Click in cmd window > press CTRL-C > press OK .'},'Settings Issue'))
            end
            % Case 3
        elseif (sharePriceMainInvestment < marketHigh && sharePriceMainInvestment > interimMarketLow && sharePriceMainInvestment < previousDaysharePriceMainInvestment)
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
            interimMarketLow = sharePriceMainInvestment;
            if(strcmp(submode,'DownTrendUndetermined') || strcmp(submode,'DownTrendImpulse'))
                waitfor(msgbox({'Error - DownTrend. Submode issue';'';'Click in cmd window > press CTRL-C > press OK .'},'Settings Issue'))
                % Subcase 3a
            elseif(strcmp(submode,'DownTrendPullback'))
                sc3a=1
                interimMarketLow = sharePriceMainInvestment;
                %if (marketLevel(day,3) == 2 && sharePriceMainInvestment < currentLowerLow) %% Check for Lower Low
                % Subcase 3a1
                if (marketLevel(day,3) == 2 && dayCurrentLowerLow <= dayCurrentLowerHigh && sharePriceMainInvestment < currentLowerLow)%% Check for Lower Low
                    sc3a1=1
                    mileStone = 'LowerLow';
                    submode = 'DownTrendImpulse';
                    currentLowerLow = sharePriceMainInvestment;
                    dayCurrentLowerLow = day;
                    % Subcase 3a2
                else
                    sc3a2=1
                    submode = 'DownTrendUndetermined';
                end
                % Subcase 3b
            else
                sc3b=1
                submode = 'DownTrendUndetermined';
            end
            % Case 4
        elseif (sharePriceMainInvestment > marketHigh && sharePriceMainInvestment > interimMarketLow && sharePriceMainInvestment > previousDaysharePriceMainInvestment)
            c4=1
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
            mode = 'UpTrend';
            submode = 'UpTrendUndetermined';
            currentLowerLow = sharePriceMainInvestment*1000;
            dayCurrentLowerLow = 1;
            currentLowerHigh = sharePriceMainInvestment*1000;
            dayCurrenLowerHigh = 1;
            interimMarketLow = sharePriceMainInvestment*1000;
            marketLow=sharePriceMainInvestment/1000;
            % Case 5 - Market unchanged
        else
            c5=1
            previousDaysharePriceMainInvestment = sharePriceMainInvestment;
            mode = 'Undetermined';
            submode = 'Undetermined';
        end
    end
    
    fprintf('%d, %0.0f, %s, %s, %s, %0.0f, %0.0f, %0.0f, %0.0f, %0.0f, %0.0f, %0.0f, %0.0f, %s\n', day,sharePriceMainInvestment,mode,submode,mileStone,interimMarketLow,interimMarketHigh,currentHigherHigh,currentHigherLow,currentLowerHigh,currentLowerLow,marketLow,marketHigh,modePrediction)
    
    % Results array (record end-of-day Results)
    resultsTrendAnalysis(day,1) = day;
    resultsTrendAnalysis(day,2) = dateNum;
    resultsTrendAnalysis(day,3) = sharePriceMainInvestment;
    
    % Record HH and HL
    if (strcmp(mileStone,'HigherHigh'))
        UpTrend(day,1) = day; %day
        UpTrend(day,2) = sharePriceMainInvestment; %HH
        UpTrend(day,3) = 1; %HH
    elseif  (strcmp(mileStone,'HigherLow'))
        UpTrend(day,1) = day; %Day
        UpTrend(day,2) = sharePriceMainInvestment; %HL
        UpTrend(day,3) = 2; %HL
    else
        UpTrend(day,1) = day; %Day
        UpTrend(day,2) = sharePriceMainInvestment;
        UpTrend(day,3) = 3; %No Milestone
    end
    % Record LH and LL
    if (strcmp(mileStone,'LowerHigh'))
        DownTrend(day,1) = day; %day
        DownTrend(day,2) = sharePriceMainInvestment; %LH
        DownTrend(day,3) = 1; %LH
    elseif  (strcmp(mileStone,'LowerLow'))
        DownTrend(day,1) = day; %Day
        DownTrend(day,2) = sharePriceMainInvestment; %LL
        DownTrend(day,3) = 2; %LL
    else
        DownTrend(day,1) = day; %Day
        DownTrend(day,2) = sharePriceMainInvestment;
        DownTrend(day,3) = 3; %No Milestone
    end
    
    % Trend array (record end-of-day Results)
    trend(day,1) = {mode};
    trend(day,2) = {submode};
    trend(day,3) = {mileStone};
    
end