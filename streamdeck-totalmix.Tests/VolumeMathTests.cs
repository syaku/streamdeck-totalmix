using streamdeck_totalmix;

namespace streamdeck_totalmix.Tests;

public class VolumeMathTests
{
    [Fact]
    public void PositiveTicksRaiseByTwoHundredthsPerTick()
    {
        Assert.Equal(0.06M, VolumeMath.Increment(ticks: 3, multiplier: 1));
    }

    [Fact]
    public void NegativeTicksLowerByTheSameAmount()
    {
        Assert.Equal(-0.06M, VolumeMath.Increment(ticks: -3, multiplier: 1));
    }

    [Fact]
    public void PositiveMultiplierScalesTheIncrementUp()
    {
        Assert.Equal(0.04M, VolumeMath.Increment(ticks: 1, multiplier: 2));
    }

    [Fact]
    public void NegativeMultiplierDividesTheIncrement()
    {
        Assert.Equal(0.01M, VolumeMath.Increment(ticks: 1, multiplier: -2));
    }

    [Fact]
    public void TheMostNegativeMultiplierStillRaisesOnPositiveTicks()
    {
        Assert.True(VolumeMath.Increment(ticks: 1, multiplier: int.MinValue) > 0M);
    }

    [Fact]
    public void ZeroMultiplierIsTreatedAsOne()
    {
        Assert.Equal(0.02M, VolumeMath.Increment(ticks: 1, multiplier: 0));
    }

    [Fact]
    public void NextValueClampsAtTheUpperBound()
    {
        Assert.Equal(1.0M, VolumeMath.NextValue(current: 0.99M, ticks: 5, multiplier: 1));
    }

    [Fact]
    public void NextValueClampsAtTheLowerBound()
    {
        Assert.Equal(0.0M, VolumeMath.NextValue(current: 0.01M, ticks: -5, multiplier: 1));
    }

    [Fact]
    public void NormalizedValueBecomesADisplayValueOutOfOneHundred()
    {
        Assert.Equal(82, VolumeMath.ToDisplay(0.82M));
    }

    [Fact]
    public void DisplayValueBecomesANormalizedValue()
    {
        Assert.Equal(0.82M, VolumeMath.FromDisplay(82));
    }
}
