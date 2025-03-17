 function [mode,submode] = Pattern_Eval(priceYesterday,priceToday,priceTommorrow,priceDayAfterTommorrow)
 
 if (priceYesterday < priceToday && priceTommorrow > priceToday && priceYesterday < priceTommorrow)
     mode = 'UpTrend';
     submode = 'UpTrendUndetermined';
     
 elseif (priceYesterday < priceToday && priceTommorrow < priceToday && priceYesterday < priceTommorrow)
     mode = 'UpTrend';
     submode = 'UpTrendUndetermined';
     
 elseif (priceYesterday < priceToday && priceTommorrow < priceToday && priceYesterday > priceTommorrow)
     if( priceDayAfterTommorrow > priceTommorrow && priceDayAfterTommorrow > priceToday)
         mode = 'UpTrend';
         submode = 'UpTrendUndetermined';
     else
         mode = 'DownTrend';
         submode = 'DownTrendUndetermined';
     end
     
 elseif (priceYesterday > priceToday && priceTommorrow < priceToday && priceYesterday > priceTommorrow)
     mode = 'DownTrend';
     submode = 'DownTrendUndetermined';
     
 elseif (priceYesterday > priceToday && priceTommorrow > priceToday && priceYesterday > priceTommorrow)
     mode = 'DownTrend';
     submode = 'DownTrendUndetermined';
     
 elseif (priceYesterday > priceToday && priceTommorrow > priceToday && priceYesterday < priceTommorrow)
     if( priceDayAfterTommorrow < priceTommorrow && priceDayAfterTommorrow < priceToday)
         mode = 'DownTrend';
         submode = 'DownTrendUndetermined';
     else
         mode = 'UpTrend';
         submode = 'UpTrendUndetermined';
     end
 
 else
   waitfor(msgbox({'Pattern Eval Conumdrum.';'';'Click in cmd window > press CTRL-C > press OK .'},'Settings Issue'))  
 
 end
