/*
 * KGStopLoss.cs
 * 
 * Original Author:     NinjaTrader_Jesse posted the original code that eventually became these classes:
 *                      https://forum.ninjatrader.com/forum/ninjatrader-8/indicator-development/92806-dynamic-properties                      
 * 
 * Summary:             A series of classes used to create a single Property Type that allows the user to select a Stop Loss type
 *                      then refreshes, dynamically displaying only the properties pertinent to the selected Stop Loss.
 *                      Please forgive the naming convention as it was only ever intended to be used internally and the KG prefix
 *                      helps me to distinguish my custom classes from default NinjaScript types.
 *                      
 * Class Descriptions:  KGStopLoss
 *                      - A sealed class that holds the Stop Loss types found in the drop box. This class can be 
 *                        treated similarly to an enum as it hold static references of itself for each stop loss.
 *                      
 *                      KGStopLossEnumConverter
 *                      - Inheriting from TypeConverter that translates the stop loss objects for the drop down                       
 *                        descriptor property used to select a stop loss type.                        
 *                        
 *                      KGStopLossProperties
 *                      - The actual Type added to strategies. This class holds the property descriptor that make
 *                        up the entire Stop Loss section.
 *                        
 *                      KGStopLossPropertyConverter
 *                      - This class works in tandem with KGStopLossProperties. It contains the logic that determines
 *                        which of the property descriptors to return so only the desired properties are displayed.
 * 
 */

/*///========= TO ADD THE DYNAMIC STOP LOSS SELECTOR TO A STRATEGY ===========================================================================\\\*\
 *  
 *      Insert the following code into the State.SetDefaults setion of OnStateChange():
 *
            KGSLProperties = new KGStopLossProperties();         
 *      
 *      Insert the following code into the strategy PROPERTIES section:
 *      
            [NinjaScriptProperty]
            [Display( Name = "KGStopLossProperties", Order = 0, GroupName = "Position Parameters", Description = "KGStopLossProperties" )]
            public KGStopLossProperties KGSLProperties { get; set; }
 *
\*\\\=========================================================================================================================================///*/

#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Diagnostics.Tracing;
using System.Globalization;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript
{
    #region SEALED CLASS - KGStopLoss (Object definition)

    public sealed class KGStopLoss
    {     
        //  IMPORTANT! ----------------------------------------------------------------------------------------------------------
        //
        //  To ADD a NEW [Stop Loss] to the [Drop Down Box] perform the following steps:
        //
        //      1. Add a new static KGStopLoss to this class respecting the numerical increment id
        //      2. Increment the RANGE Attribute of the 'KGSLType' property below
        //      3. Add a CASE to the switch inside KGStopLossPropertyConverter, in parallel with the new SL id
        //      4. Add the relevant sub-properties for the new Stop Loss Type to the KGStopLossProperties class
        //      5. Set a default value for each new sub-property to the KGStopLossProperties Constructor
        
        public static KGStopLoss None = new KGStopLoss( "None", 1 );
        public static KGStopLoss TicksFromEntry = new KGStopLoss( "Entry - Ticks", 2 );
        public static KGStopLoss ATRFromEntry = new KGStopLoss( "Entry - ATR %", 3 );
        public static KGStopLoss TicksPastPrevCandle = new KGStopLoss( "Prior Bar - Ticks", 4 );
        public static KGStopLoss ATRPastPrevCandle = new KGStopLoss( "Prior Bar - ATR %", 5 );
        public static KGStopLoss TicksPastPrevFractal = new KGStopLoss( "Fractal - Ticks", 6 );
        public static KGStopLoss ATRPastPrevFractal = new KGStopLoss( "Fractal - ATR %", 7 );
        public static KGStopLoss TicksPastMA = new KGStopLoss( "Moving Average - Ticks", 8 );
        public static KGStopLoss ATRPastMA = new KGStopLoss( "Moving Average - ATR %", 9 );
        public static KGStopLoss Custom = new KGStopLoss( "Custom", 10 );

        //  A singleton dictionary containing references to each of the stop losses
        private static Dictionary<string, KGStopLoss> _slDictionary = new Dictionary<string, KGStopLoss>();

        public string Name { get; private set; }
        public int Id { get; private set; }
        
        private KGStopLoss( string _name, int _id )
        {
            Name = _name;
            Id = _id;
            _slDictionary[_name] = this;
        }

        public static KGStopLoss GetIdByName( string _name )
        {
            return _slDictionary[_name];
        }

        public static KGStopLoss GetNameById( int _id )
        {
            return _slDictionary.FirstOrDefault( x => x.Value.Id == _id ).Value;
        }

        public static IEnumerable<KGStopLoss> All() 
        {
            return _slDictionary.Values;
        }
    }

    #endregion

    #region KGStopLoss ENUM Type Converter - Stop Loss Types

    public class KGStopLossEnumConverter : TypeConverter
    {
        public override StandardValuesCollection GetStandardValues( ITypeDescriptorContext _context )
        {
            //  TODO: Implement the two IEnumerable methods needed to make this a .ForEach(Lambda)
            List<string> values = new List<string>();
            foreach ( var sl in KGStopLoss.All() )
                values.Add( sl.Name );

            return new StandardValuesCollection( values );
        }

        //  TODO: Sometimes ConvertFrom and ConvertTo will receive a string/int respectively and sometimes
        //  the entire KGStopLoss object instead. I need to either figure out why and determine if it needs
        //  be fixed, or implement methods to convert KGStopLoss automagically. 

        public override object ConvertFrom( ITypeDescriptorContext _context, CultureInfo _culture, object _fromObj )
        {
            if ( _fromObj is string )
                return KGStopLoss.GetIdByName( _fromObj.ToString() ).Id;
            else if ( _fromObj is KGStopLoss )
                return KGStopLoss.GetIdByName( ( _fromObj as KGStopLoss ).Name ).Id;

            //  Why 1? Because 1 is always first
            return 1;
        }

        public override object ConvertTo( ITypeDescriptorContext _context, CultureInfo _culture, object _toObj, Type _destinationType )
        {
            if ( _toObj is int )
                return KGStopLoss.GetNameById( (int) _toObj ).Name;
            else if ( _toObj is KGStopLoss )
                return KGStopLoss.GetNameById( ( _toObj as KGStopLoss ).Id ).Name;

            return KGStopLoss.All().FirstOrDefault().Name;
        }

        //  These are here because we need them to inherit TypeConverter.
        //  TODO: Figure out exactly how these work and tap into their awesomeness (utilize their obviously important functionality)
        public override bool GetStandardValuesExclusive( ITypeDescriptorContext context ) { return true; }
        public override bool GetStandardValuesSupported( ITypeDescriptorContext context ) { return true; }
        public override bool CanConvertFrom( ITypeDescriptorContext context, Type fromType ) { return true; }
        public override bool CanConvertTo( ITypeDescriptorContext context, Type toType ) { return true; }
    }

    #endregion

    #region DYNAMIC PROPERTY - Stop Loss Property Descriptors

    [TypeConverter( typeof( KGStopLossPropertyConverter ) )]
    public class KGStopLossProperties
    {
        public KGStopLossProperties( )
        {
            KGSLType = KGStopLoss.All().FirstOrDefault().Id;
            SL_ENTRY_TICKS = 12;
            SL_ENTRY_ATR = 200;
            SL_PREV_BAR_BACK = 1;
            SL_PREV_BAR_TICKS = 2;
            SL_PREV_BAR_ATR = 50;
            SL_PREV_FRACTAL_SIZE = 4;
            SL_PREV_FRACTAL_TICKS = 2;
            SL_PREV_FRACTAL_ATR = 50;
            SL_MA_TYPE = 1;
            SL_MA_PERIOD = 20;
            SL_MA_TICKS = 6;
            SL_MA_ATR = 100;
        }

        public override string ToString( )
        {
            return "Select a Stop Loss Type and Parameters";
        }

        [Range( 1, 10 ), NinjaScriptProperty]
        [Display( ResourceType = typeof( Custom.Resource ), Name = "Stop Loss Type", GroupName = "Stop Loss Parameters", Order = 0 )]
        [RefreshProperties( RefreshProperties.All )]
        [TypeConverter( typeof( KGStopLossEnumConverter ) )] // Converts the int to string values
        [PropertyEditor( "NinjaTrader.Gui.Tools.StringStandardValuesEditorKey" )] // Create the combo box on the property grid
        public int KGSLType { get; set; }

        [Range( 1, int.MaxValue ), NinjaScriptProperty]
        [Display( Name = "Ticks From Entry", Order = 1, GroupName = "Stop Loss Parameters", Description = "Ticks from Entry to set the Stop Loss" )]
        public int SL_ENTRY_TICKS { get; set; }

        [Range( 1, int.MaxValue ), NinjaScriptProperty]
        [Display( Name = "ATR % From Entry", Order = 1, GroupName = "Stop Loss Parameters", Description = "ATR Percent (ie.250%) from Entry to set the Stop Loss" )]
        public int SL_ENTRY_ATR { get; set; }

        [Range( 1, int.MaxValue ), NinjaScriptProperty]
        [Display( Name = "Number of Bars Back", Order = 1, GroupName = "Stop Loss Parameters", Description = "Number of Bars Back to find the exact bar used for the Stop Loss" )]
        public int SL_PREV_BAR_BACK { get; set; }

        [Range( 1, int.MaxValue ), NinjaScriptProperty]
        [Display( Name = "Ticks Past Prior Bar", Order = 2, GroupName = "Stop Loss Parameters", Description = "Ticks Past an X number of bars back to set the Stop Loss" )]
        public int SL_PREV_BAR_TICKS { get; set; }

        [Range( 1, int.MaxValue ), NinjaScriptProperty]
        [Display( Name = "ATR % Past Prior Bar", Order = 2, GroupName = "Stop Loss Parameters", Description = "ATR Percent (ie.250%) Past an X number of bars back to set the Stop Loss" )]
        public int SL_PREV_BAR_ATR { get; set; }

        [Range( 1, int.MaxValue ), NinjaScriptProperty]
        [Display( Name = "Min Fractal Size", Order = 1, GroupName = "Stop Loss Parameters", Description = "Minimum size Fractal used for the Stop Loss" )]
        public int SL_PREV_FRACTAL_SIZE { get; set; }

        [Range( 1, int.MaxValue ), NinjaScriptProperty]
        [Display( Name = "Ticks Past Prev Fractal", Order = 2, GroupName = "Stop Loss Parameters", Description = "Ticks Past a Prev Fractal to set the Stop Loss" )]
        public int SL_PREV_FRACTAL_TICKS { get; set; }

        [Range( 1, int.MaxValue ), NinjaScriptProperty]
        [Display( Name = "ATR % Past Prev Fractal", Order = 2, GroupName = "Stop Loss Parameters", Description = "ATR Percent (ie.250%) Past a Prev Fractal to set the Stop Loss" )]
        public int SL_PREV_FRACTAL_ATR { get; set; }

        [Range( 1, 6 ), NinjaScriptProperty]
        [Display( ResourceType = typeof( Custom.Resource ), Name = "Moving Average Type", GroupName = "Stop Loss Parameters", Order = 1, Description = "The Moving Average to use for the Stop Loss" )]
        [RefreshProperties( RefreshProperties.All )]
        [TypeConverter( typeof( MovingAverageEnumConverter ) )]
        [PropertyEditor( "NinjaTrader.Gui.Tools.StringStandardValuesEditorKey" )]
        public int SL_MA_TYPE { get; set; }

        [Range( 1, int.MaxValue ), NinjaScriptProperty]
        [Display( Name = "Moving Average Period", Order = 2, GroupName = "Stop Loss Parameters", Description = "The Period Length for the Moving Average used for the Stop Loss" )]
        public int SL_MA_PERIOD { get; set; }

        [Range( 1, int.MaxValue ), NinjaScriptProperty]
        [Display( Name = "Ticks Past MA", Order = 3, GroupName = "Stop Loss Parameters", Description = "Ticks Past a Prev Fractal to set the Stop Loss" )]
        public int SL_MA_TICKS { get; set; }

        [Range( 1, int.MaxValue ), NinjaScriptProperty]
        [Display( Name = "ATR % Past MA", Order = 3, GroupName = "Stop Loss Parameters", Description = "ATR Percent (ie.250%) Past a Prev Fractal to set the Stop Loss" )]
        public int SL_MA_ATR { get; set; }
    }

    public class KGStopLossPropertyConverter : ExpandableObjectConverter
    {
        public override PropertyDescriptorCollection GetProperties( ITypeDescriptorContext _context, object _component, Attribute[] _attributes )
        {
            //  The properties we will return after remove the invalid sub-properties
            PropertyDescriptorCollection retProps = new PropertyDescriptorCollection( null );

            //  baseProps holds all our properties. From this collection we will remove the unwanted properties (props not relevant to the currently selected SL)
            PropertyDescriptorCollection baseProps;
            if ( base.GetPropertiesSupported( _context ) )
                baseProps = base.GetProperties( _context, _component, _attributes );
            else
                baseProps = TypeDescriptor.GetProperties( _component, _attributes );

            //  slProps contains values set for all our Stop Loss Property controls
            KGStopLossProperties slProps = (KGStopLossProperties) _component;

            //  If this happens the user probably forgot to instantiate the Properties object in State.SetDefaults
            //  TODO: Consider propagating an error instead of just exiting to prevent unwanted functionality in the calling strategy
            if ( slProps == null ) return baseProps;
            
            int selectedStopLoss = slProps.KGSLType;

            //  Loop through each property descriptor and determine if we should be adding it to the             
            foreach ( PropertyDescriptor propertyDescriptor in baseProps )
            {
                //  Always add the Drop Down Box
                if ( propertyDescriptor.Name == "KGSLType" )
                {
                    retProps.Add( propertyDescriptor );
                    continue;
                }

                bool addCurrentControl = false;
                switch ( selectedStopLoss )
                {
                    case 1: //  KGStopLoss.None
                        break;
                    case 2: //  KGStopLoss.TicksFromEntry
                        addCurrentControl = propertyDescriptor.Name == "SL_ENTRY_TICKS";
                        break;
                    case 3: //  KGStopLoss.ATRFromEntry
                        addCurrentControl = propertyDescriptor.Name == "SL_ENTRY_ATR";
                        break;
                    case 4: //  KGStopLoss.TicksPastPrevCandle
                        addCurrentControl = propertyDescriptor.Name == "SL_PREV_BAR_TICKS" || propertyDescriptor.Name == "SL_PREV_BAR_BACK";
                        break;
                    case 5: //  KGStopLoss.ATRPastPrevCandle
                        addCurrentControl = propertyDescriptor.Name == "SL_PREV_BAR_ATR" || propertyDescriptor.Name == "SL_PREV_BAR_BACK";
                        break;
                    case 6: //  KGStopLoss.TicksPastPrevFractal
                        addCurrentControl = propertyDescriptor.Name == "SL_PREV_FRACTAL_TICKS" || propertyDescriptor.Name == "SL_PREV_FRACTAL_SIZE";
                        break;
                    case 7: //  KGStopLoss.ATRPastPrevFractal
                        addCurrentControl = propertyDescriptor.Name == "SL_PREV_FRACTAL_ATR" || propertyDescriptor.Name == "SL_PREV_FRACTAL_SIZE";
                        break;
                    case 8: // KGStopLoss.TicksPastMA
                        addCurrentControl = propertyDescriptor.Name == "SL_MA_TICKS" || propertyDescriptor.Name == "SL_MA_PERIOD" || propertyDescriptor.Name == "SL_MA_TYPE";                        
                        break;
                    case 9: // KGStopLoss.ATRPastMA
                        addCurrentControl = propertyDescriptor.Name == "SL_MA_ATR" || propertyDescriptor.Name == "SL_MA_PERIOD" || propertyDescriptor.Name == "SL_MA_TYPE";
                        break;
                    case 10: // KGStopLoss.Custom
                        break;
                    default:
                        break;
                }

                //  If this loop has a property descriptor we want, add it to the return collection
                if ( addCurrentControl )
                    retProps.Add( propertyDescriptor );
            }
            return retProps;
        }
    }

    #endregion

}
