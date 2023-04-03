/*
 * ChartDragDoubleClick.cs
 * 
 * Summary:             A chart mouse-drag that performs much more smoothly than the default mouse-drag.
 * 
 * Description:         This was originally created to try and improve the default chart ctrl-drag
 *                      functionality. When attempting a ctrl-drag on a chart with a large number of
 *                      drawings the chart can lag tremendously. This doesn't completely remove lag,
 *                      however it greatly improves it.
 */

#region Using declarations
using System.Windows;
using System.Windows.Media;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class ChartDragDoubleClick : Indicator
    {
        private bool dragOn = false;
        private double newY, oldY;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Enables vertical chart movement after performing a double-click-hold drag on the chart.";
                Name        = "Double-Click Chart Drag";
                Panel       = 1;
                IsOverlay   = true;
            }
            else if (State == State.Historical)
            {
                if (ChartControl != null)
                {
                    //  Add our mouse events to the ChartControl
                    ChartControl.PreviewMouseDown += OnMouseDown;
                    ChartControl.PreviewMouseUp += OnMouseUp;
                    ChartControl.PreviewMouseMove += OnMouseMove;
                    ChartControl.MouseLeave += OnMouseLeave;
                }
            }
            else if (State == State.Terminated)
            {
                if (ChartControl != null)
                {
                    //  Remove our mouse events from the ChartControl
                    ChartControl.PreviewMouseDown -= OnMouseDown;
                    ChartControl.PreviewMouseUp -= OnMouseUp;
                    ChartControl.PreviewMouseMove -= OnMouseMove;
                    ChartControl.MouseLeave -= OnMouseLeave;
                }
            }
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (!dragOn)
                return;

            //  Show the user a warning if the chart Y scaling type is not Fixed
            if (chartScale.Properties.YAxisRangeType != YAxisRangeType.Fixed)
            {
                Draw.TextFixed(
                    this,
                    "dragWarning",
                    "[Double-Click Chart Drag]\nWARNING: Chart Y Scale is set to Automatic mode. Vertical drag will NOT work.\n" +
                    "Click-Hold-Drag your mouse on the chart's Y Scale to enable Fixed mode ('F' will appear at the top of the scale)",
                    TextPosition.TopRight,
                    Brushes.Black,
                    new SimpleFont("Arial Narrow", 16),
                    Brushes.Red,
                    Brushes.White,
                    80);
            }
            else if (chartScale.Properties.YAxisRangeType == YAxisRangeType.Fixed)
            {
                if (DrawObjects["dragWarning"] != null)
                    RemoveDrawObject("dragWarning");
            }

            //  Convert the Y coordinate 
            newY = ChartingExtensions.ConvertToVerticalPixels(newY, ChartControl.PresentationSource);

            //  oldY will be 0 if we've just started a drag, if it is then set it to the cursor's Y location
            oldY = oldY == 0 ? newY : oldY;

            //  Calculate the move's delta in pixels and multiply it by the chart's value per pixel
            double moveDelta = (newY - oldY) * (chartScale.MaxMinusMin / chartControl.ActualHeight);

            //  Change the chart's scale based on the calculated delta amount
            chartScale.Properties.FixedScaleMax = chartScale.Properties.FixedScaleMax + moveDelta;
            chartScale.Properties.FixedScaleMin = chartScale.Properties.FixedScaleMin + moveDelta;

            //  Save the updated cursor position for the next mouse move update
            oldY = newY;
        }
        private void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //  If we double clicked then start the drag
            if (e.ClickCount == 2)
                dragOn = true;
        }
        private void OnMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ResetMouseDrag();
        }
        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //  If we're dragging the mouse then save the position and force a chart refresh
            if (dragOn)
            {
                newY = e.GetPosition(ChartPanel as IInputElement).Y;
                ForceRefresh();
            }
        }
        private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ResetMouseDrag();
        }
        protected void ResetMouseDrag()
        {
            //  On the mouse up event reset the drag
            if (dragOn)
            {
                dragOn = false;
                oldY = 0;
                newY = 0;
            }
        }
    }

}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ChartDragDoubleClick[] cacheChartDragDoubleClick;
		public ChartDragDoubleClick ChartDragDoubleClick()
		{
			return ChartDragDoubleClick(Input);
		}

		public ChartDragDoubleClick ChartDragDoubleClick(ISeries<double> input)
		{
			if (cacheChartDragDoubleClick != null)
				for (int idx = 0; idx < cacheChartDragDoubleClick.Length; idx++)
					if (cacheChartDragDoubleClick[idx] != null &&  cacheChartDragDoubleClick[idx].EqualsInput(input))
						return cacheChartDragDoubleClick[idx];
			return CacheIndicator<ChartDragDoubleClick>(new ChartDragDoubleClick(), input, ref cacheChartDragDoubleClick);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ChartDragDoubleClick ChartDragDoubleClick()
		{
			return indicator.ChartDragDoubleClick(Input);
		}

		public Indicators.ChartDragDoubleClick ChartDragDoubleClick(ISeries<double> input )
		{
			return indicator.ChartDragDoubleClick(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ChartDragDoubleClick ChartDragDoubleClick()
		{
			return indicator.ChartDragDoubleClick(Input);
		}

		public Indicators.ChartDragDoubleClick ChartDragDoubleClick(ISeries<double> input )
		{
			return indicator.ChartDragDoubleClick(input);
		}
	}
}

#endregion
