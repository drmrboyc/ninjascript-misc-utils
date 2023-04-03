### ChartDragDoubleClick.cs

A chart mouse-drag that performs much more smoothly than the default mouse-drag. 

This was originally created to try and improve the default chart ctrl-drag functionality. When attempting a ctrl-drag on a chart with a large number of drawings the chart can lag tremendously. This doesn't completely remove lag, however it greatly improves it.

---

### DebugWindow.cs

The DebugWindow was created to help me easily and quickly observe multiple values per candle from custom add-ons without having to create temporary public properties for each variable just to view them in the Data Box.

- The class places a WPF button onto the Chart Trader panel (to show/hide) the debug box
- The Debug Window is shown in the top left corner of the chart window

This class was made for personal use and could use several easily implemented customization options.

Examples of these might be:

    1. DebugWindow placement
    2. Toggling on/off of the o/h/l/c/CurrentBar which are displayed by default
    3. Customizable sizing of the DebugWindow
    4. Customizable coloring of the DebugWindow
    5. Additional public methods that accept NinjaScript native objects (ie. Series).
       This could possibly be attached to the DebugWindow once during State.Configure 
       or State.Dataloaded to avoid a call on each new bar
    6. A Public method that accepts a <T> obj which contains an implemented/overriden ToString()
    7. Public method that accepts params object[] _objects or "params string[] _string"

---

### KGAlerts.cs

A chart mouse-drag that performs much more smoothly than the default mouse-drag.

This was originally created to try and improve the default chart ctrl-drag functionality. When attempting a ctrl-drag on a chart with a large number of drawings the chart can lag tremendously. This doesn't completely remove lag, however it is greatly improved.

---

### KGFractals

A series of classes used to find and maintain lists of recent fractal peaks.

*NOTE: This was not built to recalculate every tick or price-change. If you use this file to track fractal peaks only call the update methods on First/Last tick of a bar.*

##### Class Descriptions

- KGFractal
  - A fractal object containing many properties *(needs list interface implementation)*

- KGFractalFactory
  - Produces lists of KGFractals based on constructor parameters

- KGFractalExtensions
  - Extensions for the NT Series< double >

*There are a PLETHORA of bad coding practices within this file. In fact, I may submit this file to educational institutions to serve as both example of everything not to do and an exercise for budding software engineer students. However, It functions correctly and quickly. Just don't tell it, "it's what's inside that counts".*

---

### KGStopLoss.cs

*NinjaTrader_Jesse posted the [original code here](https://forum.ninjatrader.com/forum/ninjatrader-8/indicator-development/92806-dynamic-properties) that eventually became these classes:*

A series of classes used to create a single Property Type that allows the user to select a Stop Loss type then refreshes, dynamically displaying only the properties pertinent to the selected Stop Loss. Please forgive the naming convention as it was only ever intended to be used internally and the KG prefix helps me to distinguish my custom classes from default NinjaScript types.

##### Class Descriptions:

- KGStopLoss
  - A sealed class that holds the Stop Loss types found in the drop box. This class can be treated similarly to an enum as it hold static references of itself for each stop loss.

- KGStopLossEnumConverter
  - Inheriting from TypeConverter that translates the stop loss objects for the drop down descriptor property used to select a stop loss type.

- KGStopLossProperties
  - The actual Type added to strategies. This class holds the property descriptor that make up the entire Stop Loss section.

- KGStopLossPropertyConverter
  - This class works in tandem with KGStopLossProperties. It contains the logic that determines which of the property descriptors to return so only the desired properties are displayed.
