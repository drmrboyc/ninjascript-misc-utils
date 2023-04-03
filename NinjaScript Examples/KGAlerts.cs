/*
 * KGAlerts.cs
 * 
 * Summary:             A thread-safe class that allows for other addons to perform an alert on the chart 
 *                      by flashing the background and/or playing a sound.
 * 
 * Description:         This class was not designed to be shared publicly. I may revisit and
 *                      improve the scoping / functionality / customization.
 */

#region Using declarations
using System;
using System.Windows.Media;
using System.Windows.Threading;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript
{
	public class KGAlerts
	{
        private bool IsFlashing = false;
        private int numFlashes = 0;
        private int maxFlashes = 0;
        private DispatcherTimer flashTimer;
        private Brush flashColor;
        private Indicator owner;
        private RegionHighlightX flashRegion;

        //  Constants
        private int RegionOpacity = 100;

        private void flashTimer_Tick( object sender, EventArgs e)
        {
            if ( numFlashes < maxFlashes)
            {
                numFlashes++;
                owner.TriggerCustomEvent( o =>
                {
                    if ( flashRegion != null )
                        flashRegion.IsVisible = numFlashes % 2 == 0 ? true : false;

                    if (owner != null)
                        owner.ForceRefresh();

                }, null );
            }
            else if (numFlashes >= maxFlashes)
            {
                flashTimer.Stop();                
                StopFlashing();
            }            
        }

        private void StopFlashing()
        {
            flashTimer.Tick -= flashTimer_Tick;
            flashTimer = null;
            numFlashes = 0;
            IsFlashing = false;

            owner.TriggerCustomEvent( o =>
            {
                if ( owner != null && owner.DrawObjects[ "flashObj" ] != null )
                    owner.RemoveDrawObject( "flashObj" );
            }, null );
        }

        private void FlashScreen( Indicator _owner, Brush _flashColor, int _flashInterval = 50, int _maxFlashes = 20 )
        {
            if ( IsFlashing )
                return;

            owner = _owner;
            flashColor = _flashColor;
            maxFlashes = _maxFlashes;
            numFlashes = 0;
            IsFlashing = true;

            owner.TriggerCustomEvent( o =>
            {   
                flashRegion = Draw.RegionHighlightX( owner, "flashObj", 150, 0, flashColor, flashColor, RegionOpacity );

                flashTimer = new DispatcherTimer();
                flashTimer.Interval = TimeSpan.FromMilliseconds( _flashInterval );
                flashTimer.Tick += flashTimer_Tick;
                flashTimer.Start();
            }, null );
        }

        private void FlashTimer_Tick( object sender, EventArgs e )
        {
            throw new NotImplementedException();
        }

        public void PlayNewGreenZoneAlert( Indicator _owner )
        {
            if ( _owner != null && _owner.ChartControl != null )
            {
                _owner.ChartControl.Dispatcher.InvokeAsync( new Action( () =>
                {
                    NinjaTrader.Core.Globals.PlaySound( NinjaTrader.Core.Globals.InstallDir + @"\sounds\AlrtNwGr.wav" );
                    FlashScreen( _owner, Brushes.LightGreen, 462, 12 );
                } ) );
            }
        }

        public void PlayNewRedZoneAlert( Indicator _owner )
        {
            if ( _owner != null && _owner.ChartControl != null )
            {
                _owner.ChartControl.Dispatcher.InvokeAsync( new Action( () =>
                {
                    NinjaTrader.Core.Globals.PlaySound( NinjaTrader.Core.Globals.InstallDir + @"\sounds\AlrtNwRd.wav" );
                    FlashScreen( _owner, Brushes.Red, 462, 12 );
                } ) );
            }
        }

        public void PlayZoneEndAlert( ChartControl _chartControl )
        {
            if ( _chartControl != null )
            {
                _chartControl.Dispatcher.InvokeAsync( new Action( () =>
                {
                    NinjaTrader.Core.Globals.PlaySound( NinjaTrader.Core.Globals.InstallDir + @"\sounds\ZoneEnd.wav" );
                } ) );
            }
        }
    }
}
