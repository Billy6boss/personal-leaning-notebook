namespace Stage1.Exercises.Ex07_DelegatesAndEvents;

/// <summary>
/// Stage 1 主題：Delegates 與 events。
///
/// 任務：
/// 實作下面的 TemperatureChangedEventArgs（event payload）與
/// Thermostat class。
///
/// TemperatureChangedEventArgs 的需求：
/// - Constructor：TemperatureChangedEventArgs(double oldValue, double newValue)
/// - OldValue 與 NewValue（double）可以從外部讀取。
///
/// Thermostat 的需求：
/// - 有一個 public event：event EventHandler&lt;TemperatureChangedEventArgs&gt;? TemperatureChanged;
/// - 有一個屬性 CurrentTemperature（double），一開始的值是傳入
///   constructor 的值：Thermostat(double initialTemperature)。
/// - 把 CurrentTemperature 設為一個「新」的值（跟目前值不同）時，
///   必須恰好觸發一次 TemperatureChanged，並且 OldValue 與 NewValue
///   要設定正確。
/// - 把 CurrentTemperature 設為跟目前「相同」的值時，不能觸發事件。
/// </summary>
public class TemperatureChangedEventArgs : EventArgs
{
    // TODO: 實作 OldValue、NewValue，以及 constructor
    // （把下面會拋出例外的成員換掉）。
    public double OldValue  { get; set; }

    public double NewValue { get; set; }

    public TemperatureChangedEventArgs(double oldValue, double newValue)
    {
        this.OldValue = oldValue;
        this.NewValue = newValue;
    }
}

public class Thermostat
{
    public Thermostat(double initialTemperature)
    {
        this.CurrentTemperature = initialTemperature;
    }

    public event EventHandler<TemperatureChangedEventArgs>? TemperatureChanged;

    public double CurrentTemperature
    {
        get => throw new NotImplementedException();
        set => TemperatureChanged.Invoke(this, new TemperatureChangedEventArgs(value, value));
    }
}
