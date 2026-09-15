using Stage1.Exercises.Ex07_DelegatesAndEvents;

namespace Stage1.Exercises.Tests.Ex07_DelegatesAndEvents;

public class ThermostatTests
{
    [Fact]
    public void NewThermostat_StartsAtInitialTemperature()
    {
        var thermostat = new Thermostat(20);
        Assert.Equal(20, thermostat.CurrentTemperature);
    }

    [Fact]
    public void SettingToADifferentValue_RaisesEventWithOldAndNewValues()
    {
        var thermostat = new Thermostat(20);
        TemperatureChangedEventArgs? received = null;
        int raiseCount = 0;

        thermostat.TemperatureChanged += (sender, args) =>
        {
            raiseCount++;
            received = args;
        };

        thermostat.CurrentTemperature = 25;

        Assert.Equal(1, raiseCount);
        Assert.NotNull(received);
        Assert.Equal(20, received!.OldValue);
        Assert.Equal(25, received.NewValue);
        Assert.Equal(25, thermostat.CurrentTemperature);
    }

    [Fact]
    public void SettingToTheSameValue_DoesNotRaiseEvent()
    {
        var thermostat = new Thermostat(20);
        int raiseCount = 0;
        thermostat.TemperatureChanged += (sender, args) => raiseCount++;

        thermostat.CurrentTemperature = 20;

        Assert.Equal(0, raiseCount);
    }
}
