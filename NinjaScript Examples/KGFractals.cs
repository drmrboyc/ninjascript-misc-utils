/*
 * KGFractals.cs
 * 
 * Summary:             A series of classes used to find and maintain a list of recent fractal peaks.
 *                      
 *                      There are a PLETHORA of bad coding practices within this file. In fact, I may submit this
 *                      file to educational institutions to serve as both example of everything not to do and an 
 *                      exercise for budding software engineer students. However, It functions correctly and quickly.
 * 
 * Class Descriptions:  - KGFractal - A fractal object
 *                      - KGFractalFactory - Produces lists of KGFractals based on parameters
 *                      - KGFractalExtensions - Extensions for the NT Series<double>
 */



#region Using declarations
using System;
using System.Collections.Generic;
using NinjaTrader.Gui.Chart;
#endregion

namespace NinjaTrader.NinjaScript
{
    public class Candle
    {
        public int Index;
        public double Open;
        public double High;
        public double Low;
        public double Close;

        public Candle (int _idx, double _open, double _high, double _low, double _close)
        {
            Index = _idx;
            Open = _open;
            High = _high;
            Low = _low;
            Close = _close;
        }

        public Candle GetClone()
        {
            return (Candle) this.MemberwiseClone();
        }
    }

    public class KGFractal
    {
        public int CandleIdx = 0;
        public int BarsAgo = 0;

        public double Open = 0;
        public double High = 0;
        public double Low = 0;
        public double Close = 0;

        public int LeftSize = 0;
        public int RightSize = 0;
        public int Size = 0;

        public int LeftSizeToOppositeFractal = 0;
        public int RightSizeOppositeFractal = 0;

        public double LeftPercentOfPreviousSwing = 0;
        public double RightPercentOfPreviousSwing = 0;
        
        private bool isHigh;
        public bool IsFractalHigh() { return isHigh; }
        public bool IsFractalLow() { return !isHigh; }
    }

    public class KGFractalFactory
    {
        private ChartBars chartBars;

        private int FractalCountToFind = 5;

        private int lastFractalHighUpdateBarsCount = 0;
        private int lastFractalLowUpdateBarsCount = 0;

        private List<int>       fractalHighBarIds   = new List<int>();
        private List<int>       fractalHighBarsAgo  = new List<int>();
        private List<double>    fractalHighValues   = new List<double>();

        private List<int>       fractalLowBarIds    = new List<int>();
        private List<int>       fractalLowBarsAgo   = new List<int>();
        private List<double>    fractalLowValues    = new List<double>();

        //private Strategy owner;
        private ISeries<double> highs;
        private ISeries<double> lows;

        private int minSize;
        private bool checkLeft;
        private bool checkRight;
        private bool requireRightMinSize;

        private KGFractalFactory()
        {
        }

        public KGFractalFactory( ISeries<double> _highs, ISeries<double> _lows, int _minFractalSize, bool _checkLeftSide, bool _checkRightSide, bool _requireRightMinSize )
        {
            if ( _highs != null ) highs = _highs;
            if ( _lows != null ) lows = _lows;

            minSize = Math.Max( _minFractalSize, 1 );

            checkLeft = _checkLeftSide;
            checkRight = _checkRightSide;
            requireRightMinSize = _requireRightMinSize;
        }

        public List<int> GetFractalHighIds( int _currentChartBar )
        {
            if ( highs == null ) return null;
            UpdateFractalHighs( _currentChartBar );
            
            return fractalHighBarIds;
        }

        public List<double> GetFractalHighValues( int _currentChartBar )
        {
            if ( highs == null ) return null;
            UpdateFractalHighs( _currentChartBar );

            return fractalHighValues;
        }

        public Dictionary<int, double> GetFractalHighBarsAgoAndValues( int _currentChartBar )
        {
            if ( highs == null ) return null;
            UpdateFractalHighs( _currentChartBar );

            Dictionary<int, double> fractalHighBarsAgoAndValues = new Dictionary<int, double>();

            for( int x = 0; x < fractalHighBarsAgo.Count; x++ )
                fractalHighBarsAgoAndValues.Add( fractalHighBarsAgo[ x ], fractalHighValues[ x ] );

            return fractalHighBarsAgoAndValues;
        }

        public List<int> GetFractalLowIds( int _currentChartBar )
        {
            if ( lows == null ) return null;
            UpdateFractalLows( _currentChartBar );
            
            return fractalLowBarIds;
        }

        public List<double> GetFractalLowValues( int _currentChartBar )
        {
            if ( lows == null ) return null;
            UpdateFractalLows( _currentChartBar );

            return fractalLowValues;
        }

        public Dictionary<int, double> GetFractalLowBarsAgoAndValues(int _currentChartBar)
        {
            if ( lows == null ) return null;
            UpdateFractalLows( _currentChartBar );

            Dictionary<int, double> fractalLowBarsAgoAndValues = new Dictionary<int, double>();

            for ( int x = 0; x < fractalLowBarsAgo.Count; x++ )
                fractalLowBarsAgoAndValues.Add( fractalLowBarsAgo[ x ], fractalLowValues[ x ] );

            return fractalLowBarsAgoAndValues;
        }

        private void UpdateFractalHighs(int _currentChartBar)
        {
            if ( lastFractalHighUpdateBarsCount >= highs.Count )
                return;

            InitializeFractalHighLists();

            int curBar = 2, curHighFractal = 0;
            while ( curHighFractal < FractalCountToFind )
            {
                //  If we're going to check past the "currentbar" of the chart then break and return that no fractals exist
                if ( curBar + minSize >= _currentChartBar )
                    break;
                
                if ( highs.IsBarsAgoAMinSizeFractalHigh( curBar, minSize, checkLeft, checkRight, requireRightMinSize ) )
                {
                    fractalHighBarsAgo.Add( curBar );
                    fractalHighBarIds.Add( highs.Count - curBar - 1 );
                    fractalHighValues.Add( highs[ curBar ] );
                    curHighFractal++;
                }
                curBar++;
            }
            lastFractalHighUpdateBarsCount = _currentChartBar;
        }

        private void UpdateFractalLows(int _currentChartBar)
        {
            if ( lastFractalLowUpdateBarsCount >= lows.Count )
                return;

            InitializeFractalLowLists();

            int curBar = 2, curLowFractal = 0;
            while ( curLowFractal < FractalCountToFind )
            {
                //  If we're going to check past the "currentbar" of the chart then break and return that no fractals exist
                if ( curBar + minSize >= _currentChartBar )
                    break;

                if ( lows.IsBarsAgoAMinSizeFractalLow( curBar, minSize, checkLeft, checkRight, requireRightMinSize ) )
                {
                    fractalLowBarsAgo.Add( curBar );
                    fractalLowBarIds.Add( lows.Count - curBar - 1 );
                    fractalLowValues.Add( lows[ curBar ] );
                    curLowFractal++;
                }
                curBar++;
            }
            lastFractalLowUpdateBarsCount = _currentChartBar;
        }

        private void InitializeFractalHighLists()
        {
            fractalHighBarIds = new List<int>();
            fractalHighBarsAgo = new List<int>();
            fractalHighValues = new List<double>();
        }

        private void InitializeFractalLowLists()
        {
            fractalLowBarIds = new List<int>();
            fractalLowBarsAgo = new List<int>();
            fractalLowValues = new List<double>();
        }
    }

    /*
     * 	    THREE RULES THAT EXCLUSIVELY VALIDATE A FRACTAL
     *  -------------------------------------------------------------------------------------------------------
     *
	 *          [GIVEN: Most recent fractal's right side count == left side count if price has not broken]
	 *          [GIVEN: Fractal sides are counted up to 50 candles]
     *
	 *      ANY of the following 1, 2, or 3 make a valid fractal:
     *
	 *          1. # of candles on each side must be >= 5
     *
	 *              OR
     *
	 *          2. # of candles on both sides >= # of candles on both sides of fractals before and after
     *
	 *              OR
     *
	 *          3a. # of candles on smaller side must be >= 1/2 # of candles on larger side
	 *              AND
	 *          3b. # of candles on both sides must be >= 10
	 *          
	 *          FIBONNACCI
     *
     *          23.6
     *          38.2
     *          50.0
     *          61.8
     */

    public static class KGFractalExtensions
    {
        public static bool IsBarsAgoAMinSizeFractalHigh( this ISeries<double> _highs, int _barsAgo, int _minSize = 1, bool _checkLeft = true, bool _checkRight = true, bool _requireRightMinSize = false )
        {
            if ( _barsAgo <= 0 ) return false;

            //  If we require candles on the right side of the fractal that have not formed, then it cannot yet be a fractal
            if ( _barsAgo <= _minSize && _requireRightMinSize )
                return false;

            double highInTest = _highs[ _barsAgo ];

            //  Check the left side of the potential fractal
            if ( _checkLeft )
            {
                for ( int l = _barsAgo + 1; l <= _barsAgo + _minSize; l++ )
                {
                    if ( highInTest < _highs[ l ] )
                        return false;
                }
            }

            //  Check the right side of the potential fractal
            if ( _checkRight )
            {
                //  Enforce checking contraints if the candle is more recent than the _minSize
                int rightBarsToCheck = Math.Min( _barsAgo, _minSize );

                for ( int r = _barsAgo - 1; r >= _barsAgo - rightBarsToCheck; r-- )
                {
                    //  Reinforce constraints to prevent an invalid _highs index reference
                    if ( r < 0 ) break;

                    if ( highInTest <= _highs[ r ] )
                        return false;
                }
            }

            //  We've passed all left and right side checks so this high IS a fractal high of the given _minSize
            return true;
        }

        public static bool IsBarsAgoAMinSizeFractalLow( this ISeries<double> _lows, int _barsAgo, int _minSize = 1, bool _checkLeft = true, bool _checkRight = true, bool _requireRightMinSize = false )
        {
            if ( _barsAgo <= 0 ) return false;

            //  If we require candles on the right side of the fractal that have not formed, then it cannot yet be a fractal
            if ( _barsAgo <= _minSize && _requireRightMinSize )
                return false;

            double lowInTest = _lows[ _barsAgo ];

            //  Check the left side of the potential fractal
            if ( _checkLeft )
            {
                for ( int l = _barsAgo + 1; l <= _barsAgo + _minSize; l++ )
                {
                    if ( lowInTest > _lows[ l ] )
                        return false;                        
                }
            }

            //  Check the right side of the potential fractal
            if ( _checkRight )
            {
                //  Enforce checking contraints if the candle is more recent than the _minSize
                int rightBarsToCheck = Math.Min( _barsAgo, _minSize );

                for ( int r = _barsAgo - 1; r >= _barsAgo - rightBarsToCheck; r-- )
                {
                    //  Reinforce constraints to prevent an invalid _lows index reference
                    if (r < 0 ) break;

                    if ( lowInTest >= _lows[ r ] )
                        return false;
                }
            }

            //  We've passed all left and right side checks so this low IS a fractal low of the given _minSize
            return true;
        }
    }
}
