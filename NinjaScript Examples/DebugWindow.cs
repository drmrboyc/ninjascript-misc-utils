/*
 * DebugWindow.cs
 * 
 * Summary:           A debug window placed on a chart that can display data from multiple add-ons per bar.
 * 
 * Description:       The DebugWindow was created to help me easily and quickly observe multiple values 
 *                    per candle from custom add-ons without having to create temporary public properties
 *                    for each variable just to view them in the Data Box.                    
 */

#region Using declarations
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Input;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class DebugWindow : Indicator
	{
        #region Class Properties

        private System.Windows.Controls.RowDefinition debugToggleRow;
        private Gui.Chart.ChartTab chartTab;
        private Gui.Chart.Chart chartWindow;
        private System.Windows.Controls.Button debugToggleButton;
        private System.Windows.Controls.Grid chartTraderGrid, chartTraderButtonsGrid, upperButtonsGrid;
        private System.Windows.Controls.TabItem tabItem;
        private bool panelActive;
        private DispatcherTimer panelRefreshTimer;

        private bool debugPanelEnabled = true;
        private Point cursorPoint = new Point();
        private ChartControl chartControl;
        private int barUnderCursor = 0;

        private Dictionary<int, List<string>> debugLines;

        private SimpleFont debugFont = new SimpleFont("Courier New", 12);
        private string linePrefix = "  ";

        private static bool isLoaded = false;
        private static DebugWindow instance;

        #endregion

        #region Override Methods (OnStateChange & OnBarUpdate)

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Provides a window displaying various candle information that can also be utilized by other indicators, strategies, or addons to display custom debug information.";
				Name										= "DebugWindow";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;

                EnableDebugWindow = true;
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.Historical)
            {
                Calculate = Calculate.OnBarClose;

                //  Add the mousemove event to the chart control and add the show debug window button to the ChartTrader panel
                if (ChartControl != null && debugPanelEnabled)
                {
                    ChartControl.PreviewMouseMove += MouseMoved;
                    ChartControl.Dispatcher.InvokeAsync(() =>
                    {
                        CreateWPFControls();
                        isLoaded = true;
                    });
                }
            }
            else if (State == State.DataLoaded)
            {
                //  Instantiate the data variables
                debugLines = new Dictionary<int, List<string>>();

                //  Create a static (singleton) copy of this instance
                instance = this;

                //	Create the chart refresh timer and attach the mouse move event to the chart
                if (ChartControl != null && debugPanelEnabled)
                {
                    ChartControl.Dispatcher.InvokeAsync((Action)(() =>
                    {
                        panelRefreshTimer = new DispatcherTimer();
                        panelRefreshTimer.Tick += panelRefreshTimer_Tick;
                        panelRefreshTimer.Interval = new TimeSpan(0, 0, 0, 0, 150);
                        if (debugPanelEnabled)
                            panelRefreshTimer.Start();                        
                    }));
                }
            }
            else if (State == State.Terminated)
            {
                //  Dispose of the static copy of this indicator's instance
                instance = null;

                //	Dispose of the chart refresh timer and remove the mouse move event from the chart
                if (ChartControl != null && debugPanelEnabled)
                {
                    ChartControl.Dispatcher.InvokeAsync((Action)(() =>
                    {
                        ChartControl.PreviewMouseMove -= MouseMoved;
                        if (panelRefreshTimer != null)
                        {
                            panelRefreshTimer.Stop();
                            panelRefreshTimer.Tick -= panelRefreshTimer_Tick;
                            panelRefreshTimer = null;
                        }
                        DisposeWPFControls();
                        isLoaded = false;
                    }));
                }
            }
        }

		protected override void OnBarUpdate()
		{ /*\  These aren't the droids you're looking for...move along \*/ }

        #endregion

        #region Public Methods (adding information to the debug window)


        /// <summary>
        /// Mainly used for cosmetic formatting (ie. separating sections). Adds a single blank line to the DebugWindow.
        /// </summary>
        /// <param name="_barIdx">The index (usually CurrentBar) of the bar being edited</param>
        public static void AddBlankLine(int _barIdx)
        {
            if (!IsIndicatorLoaded())
                return;

            instance.PrivateAddDebugLine(_barIdx, " ");
        }

        /// <summary>
        /// Adds a Header line to the DebugWindow. While not necessary this help separate debug information when multiple indicators/strategies are all writing to the DebugWindow
        /// </summary>
        /// <param name="_barIdx">The index (usually CurrentBar) of the bar being edited</param>
        /// <param name="_line">A string value of the Header line being added to the DebugWindow</param>
        public static void AddHeader(int _barIdx, string _line)
        {
            if (!IsIndicatorLoaded() || String.IsNullOrEmpty(_line))
                return;

            instance.PrivateAddDebugLine(_barIdx, _line, true);
        }

        /// <summary>
        /// Adds a line to the DebugWindow.
        /// </summary>
        /// <param name="_barIdx">The index (usually CurrentBar) of the bar being edited</param>
        /// <param name="_line">A string value of the line being added to the DebugWindow</param>
        public static void AddLine(int _barIdx, string _line)
        {
            if (!IsIndicatorLoaded() || String.IsNullOrEmpty(_line))
                return;

            instance.PrivateAddDebugLine(_barIdx, _line);
        }

        /// <summary>
        /// Adds several contiguous lines to the DebugWindow
        /// </summary>
        /// <param name="_barIdx">The index (usually CurrentBar) of the bar being edited</param>
        /// <param name="_lines">A List of strings to be added to the DebugWindow"</param>
        public static void AddLines(int _barIdx, List<string> _lines)
        {
            //  If this indicator isn't loaded then don't try to add any custom debug information
            if (!isLoaded || instance == null || _lines == null || _lines.Count == 0)
                return;

            foreach (string line in _lines)
                instance.PrivateAddDebugLine(_barIdx, line);
        }

        #endregion

        #region Indicator State

        private static bool IsIndicatorLoaded( )
        {
            return isLoaded && instance != null;
        }

        #endregion

        #region Debug Info & Print

        private void PrivateAddDebugLine(int _barIdx, string _line, bool _isHeader = false)
        {
            string prefix = _isHeader ? "" : linePrefix;

            if (debugLines.ContainsKey(_barIdx))
                debugLines[_barIdx].Add(prefix + _line);
            else
                debugLines.Add(_barIdx, new List<string>{ prefix + _line });
             //[0] += linePrefix + _line + "\n";
        }

        private void PrintDebugString(int _barIdx, bool _printPriceInfo, bool _printTrendCalcInfo, bool _printMiscDebugInfo)
        {
            if (Bars == null || chartControl == null)
                return;

            string debugOutput = ""; // "BAR INFORMATION:\n";

            if (_printPriceInfo)
            {
                debugOutput += "O: " + Open.GetValueAt(_barIdx).ToString("#.00", CultureInfo.InvariantCulture) + "  H: " + High.GetValueAt(_barIdx).ToString("#.00", CultureInfo.InvariantCulture) +
                              "  L: " + Low.GetValueAt(_barIdx).ToString("#.00", CultureInfo.InvariantCulture) + "  C: " + Close.GetValueAt(_barIdx).ToString("#.00", CultureInfo.InvariantCulture) + "  \n";

                debugOutput += "Bar Index: " + _barIdx + "\n\n";
            }
            
            if (_printMiscDebugInfo && debugLines != null && debugLines.ContainsKey(_barIdx))
            {
                string miscInfo = String.Join("\n", debugLines[_barIdx]);
                if (miscInfo != "")
                    debugOutput += miscInfo;
            }

            Draw.TextFixed(this, "debugInfo", debugOutput, TextPosition.TopLeft, Brushes.Black, debugFont, Brushes.Red, Brushes.White, 85, DashStyleHelper.Solid, 2, false, string.Empty);
        }

        #endregion

        #region Mouse Move / OnRender / DebugTimer

        protected override void OnRender(ChartControl _chartControl, ChartScale _chartScale)
        {
            chartControl = _chartControl;
            if (debugPanelEnabled)
            {
                TriggerCustomEvent(o =>
                {
                    // ChartingExtensions.ConvertToHorizontalPixels(cursorPoint.X, _chartControl.PresentationSource));
                    int barIdx = ChartBars.GetBarIdxByX(_chartControl, (int)cursorPoint.X);
                    if (barIdx != barUnderCursor)
                    {
                        barUnderCursor = barIdx;
                        double cursorPrice = _chartScale.GetValueByYWpf(cursorPoint.Y);

                        PrintDebugString(barIdx, true, true, true);
                    }
                }, null);
            }
        }

        protected void MouseMoved(object sender, MouseEventArgs e)
        {
            if (!debugPanelEnabled || chartControl == null)
                return;

            cursorPoint.X = ChartingExtensions.ConvertToHorizontalPixels(e.GetPosition(ChartPanel as IInputElement).X, chartControl);
            cursorPoint.Y = ChartingExtensions.ConvertToVerticalPixels(e.GetPosition(ChartPanel as IInputElement).Y, chartControl);

            return;
        }

        private void panelRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (!debugPanelEnabled)
                panelRefreshTimer.Stop();

            TriggerCustomEvent(o =>
            {
                ChartControl.InvalidateVisual();
            }, null);
        }

        #endregion

        #region WPF Controls & Events

        protected void DebugToggleClick(object sender, RoutedEventArgs e)
        {
            debugPanelEnabled = !debugPanelEnabled;

            if (ChartControl != null)
            {
                ChartControl.Dispatcher.InvokeAsync((Action)(() =>
                {
                    debugToggleButton.Background = debugPanelEnabled ? Brushes.DarkGreen : Brushes.DarkRed;
                    debugToggleButton.Content = (debugPanelEnabled ? "Hide" : "Show") + " Debug Info";
                    if (debugPanelEnabled)
                    {
                        panelRefreshTimer.Start();
                    }
                    else
                    {
                        panelRefreshTimer.Stop();
                        if (DrawObjects["debugInfo"] != null)
                            RemoveDrawObject("debugInfo");
                    }
                }));
            }

            ForceRefresh();
        }

        protected void CreateWPFControls()
        {
            chartWindow = Window.GetWindow(ChartControl.Parent) as Gui.Chart.Chart;

            if (chartWindow == null) return;

            chartTraderGrid = (chartWindow.FindFirst("ChartWindowChartTraderControl") as Gui.Chart.ChartTrader).Content as System.Windows.Controls.Grid;

            //  The existing Chart Trader Buttons
            chartTraderButtonsGrid = chartTraderGrid.Children[0] as System.Windows.Controls.Grid;

            //  This grid is a grid i'm adding to a new row (at the bottom) in the grid that contains bid and ask prices and order controls (chartTraderButtonsGrid)
            upperButtonsGrid = new System.Windows.Controls.Grid();
            System.Windows.Controls.Grid.SetColumnSpan(upperButtonsGrid, 3);

            upperButtonsGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());
            debugToggleRow = new System.Windows.Controls.RowDefinition() { Height = new GridLength(40) };
            Style basicButtonStyle = Application.Current.FindResource("BasicEntryButton") as Style;

            debugToggleButton = new System.Windows.Controls.Button()
            {
                Content = string.Format("Hide Debug Info"),
                Height = 30,
                Margin = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0),
                Style = basicButtonStyle,
                Background = Brushes.DarkGreen,
                BorderBrush = Brushes.DimGray,
            };

            debugToggleButton.Click += DebugToggleClick;
            System.Windows.Controls.Grid.SetColumn(debugToggleButton, 4);
            upperButtonsGrid.Children.Add(debugToggleButton);

            if (TabSelected())
                InsertWPFControls();

            chartWindow.MainTabControl.SelectionChanged += TabChangedHandler;
        }

        public void DisposeWPFControls()
        {
            if (chartWindow != null)
                chartWindow.MainTabControl.SelectionChanged -= TabChangedHandler;

            if (debugToggleButton != null)
                debugToggleButton.Click -= DebugToggleClick;

            RemoveWPFControls();
        }

        public void InsertWPFControls()
        {
            if (panelActive) return;
            if (chartTraderButtonsGrid != null || upperButtonsGrid != null)
            {
                chartTraderButtonsGrid.RowDefinitions.Add(debugToggleRow);
                System.Windows.Controls.Grid.SetRow(upperButtonsGrid, (chartTraderButtonsGrid.RowDefinitions.Count - 1));
                chartTraderButtonsGrid.Children.Add(upperButtonsGrid);
            }
            panelActive = true;
        }

        protected void RemoveWPFControls()
        {
            if (!panelActive) return;
            if (chartTraderButtonsGrid != null || upperButtonsGrid != null)
            {
                chartTraderButtonsGrid.Children.Remove(upperButtonsGrid);
                chartTraderButtonsGrid.RowDefinitions.Remove(debugToggleRow);
            }
            panelActive = false;
        }

        private bool TabSelected()
        {
            bool tabSelected = false;
            //  Loop through each tab and see if the tab this indicator is added to is the selected item
            foreach (System.Windows.Controls.TabItem tab in chartWindow.MainTabControl.Items)
                if ((tab.Content as Gui.Chart.ChartTab).ChartControl == ChartControl && tab == chartWindow.MainTabControl.SelectedItem)
                    tabSelected = true;
            return tabSelected;
        }

        private void TabChangedHandler(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
                return;

            tabItem = e.AddedItems[0] as System.Windows.Controls.TabItem;
            if (tabItem == null)
                return;

            chartTab = tabItem.Content as Gui.Chart.ChartTab;
            if (chartTab == null)
                return;

            if (TabSelected())
                InsertWPFControls();
            else
                RemoveWPFControls();
        }

        #endregion

        #region Properties

        [NinjaScriptProperty]
        [Display(Name = "Enable DebugWindow", Description = "Shows the debug window", Order = 10, GroupName = "==== DebugWindow Properties ==========")]
        public bool EnableDebugWindow
        { get; set; }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DebugWindow[] cacheDebugWindow;
		public DebugWindow DebugWindow(bool enableDebugWindow)
		{
			return DebugWindow(Input, enableDebugWindow);
		}

		public DebugWindow DebugWindow(ISeries<double> input, bool enableDebugWindow)
		{
			if (cacheDebugWindow != null)
				for (int idx = 0; idx < cacheDebugWindow.Length; idx++)
					if (cacheDebugWindow[idx] != null && cacheDebugWindow[idx].EnableDebugWindow == enableDebugWindow && cacheDebugWindow[idx].EqualsInput(input))
						return cacheDebugWindow[idx];
			return CacheIndicator<DebugWindow>(new DebugWindow(){ EnableDebugWindow = enableDebugWindow }, input, ref cacheDebugWindow);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DebugWindow DebugWindow(bool enableDebugWindow)
		{
			return indicator.DebugWindow(Input, enableDebugWindow);
		}

		public Indicators.DebugWindow DebugWindow(ISeries<double> input , bool enableDebugWindow)
		{
			return indicator.DebugWindow(input, enableDebugWindow);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DebugWindow DebugWindow(bool enableDebugWindow)
		{
			return indicator.DebugWindow(Input, enableDebugWindow);
		}

		public Indicators.DebugWindow DebugWindow(ISeries<double> input , bool enableDebugWindow)
		{
			return indicator.DebugWindow(input, enableDebugWindow);
		}
	}
}

#endregion
