using System;
using System.Reflection;
using Xunit;
using XuruVoipClient.Models;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class IntercomLogParserTests
{
    private readonly MethodInfo _processLogLineMethod;

    public IntercomLogParserTests()
    {
        _processLogLineMethod = typeof(GameDetectionService).GetMethod(
            "ProcessLogLine",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ProcessLogLine method not found");
    }

    [Fact]
    public void ProcessLogLine_ShieldHit_ShouldSetShieldHitState()
    {
        // GIVEN
        var service = new GameDetectionService();
        IntercomDegradationState? stateChangedTo = null;
        service.IntercomStateChanged += state => stateChangedTo = state;

        // WHEN
        string logLine = "[Vehicle] Shield hit by laser fire";
        _processLogLineMethod.Invoke(service, new object[] { logLine });

        // THEN
        Assert.Equal(IntercomDegradationState.ShieldHit, service.GetCurrentIntercomState());
        Assert.Equal(IntercomDegradationState.ShieldHit, stateChangedTo);
    }

    [Fact]
    public void ProcessLogLine_PowerOffline_ShouldSetCriticalPowerState()
    {
        // GIVEN
        var service = new GameDetectionService();
        IntercomDegradationState? stateChangedTo = null;
        service.IntercomStateChanged += state => stateChangedTo = state;

        // WHEN
        string logLine = "Main power offline due to overload";
        _processLogLineMethod.Invoke(service, new object[] { logLine });

        // THEN
        Assert.Equal(IntercomDegradationState.CriticalPower, service.GetCurrentIntercomState());
        Assert.Equal(IntercomDegradationState.CriticalPower, stateChangedTo);
    }

    [Fact]
    public void ProcessLogLine_PowerOnline_ShouldRestoreNormalState()
    {
        // GIVEN
        var service = new GameDetectionService();
        
        // 1. Trigger power offline
        _processLogLineMethod.Invoke(service, new object[] { "Main power offline" });
        Assert.Equal(IntercomDegradationState.CriticalPower, service.GetCurrentIntercomState());

        // 2. Register for change event
        IntercomDegradationState? stateChangedTo = null;
        service.IntercomStateChanged += state => stateChangedTo = state;

        // WHEN
        _processLogLineMethod.Invoke(service, new object[] { "Main power online" });

        // THEN
        Assert.Equal(IntercomDegradationState.Normal, service.GetCurrentIntercomState());
        Assert.Equal(IntercomDegradationState.Normal, stateChangedTo);
    }

    [Fact]
    public void ProcessLogLine_QuantumStartAndEnd_ShouldTransitionCorrectly()
    {
        // GIVEN
        var service = new GameDetectionService();
        IntercomDegradationState? stateChangedTo = null;
        service.IntercomStateChanged += state => stateChangedTo = state;

        // WHEN: Quantum travel starts
        _processLogLineMethod.Invoke(service, new object[] { "QuantumTravel start" });

        // THEN
        Assert.Equal(IntercomDegradationState.QuantumTravel, service.GetCurrentIntercomState());
        Assert.Equal(IntercomDegradationState.QuantumTravel, stateChangedTo);

        // WHEN: Quantum travel ends
        stateChangedTo = null;
        _processLogLineMethod.Invoke(service, new object[] { "QuantumTravel end" });

        // THEN
        Assert.Equal(IntercomDegradationState.Normal, service.GetCurrentIntercomState());
        Assert.Equal(IntercomDegradationState.Normal, stateChangedTo);
    }

    [Fact]
    public void ProcessLogLine_MultipleEvents_ShouldPrioritizeCorrectly()
    {
        // GIVEN
        var service = new GameDetectionService();

        // 1. Quantum Travel starts (Lowest degradation priority)
        _processLogLineMethod.Invoke(service, new object[] { "QuantumTravel start" });
        Assert.Equal(IntercomDegradationState.QuantumTravel, service.GetCurrentIntercomState());

        // 2. Critical Power Offline occurs (Medium priority, overrides Quantum)
        _processLogLineMethod.Invoke(service, new object[] { "Main power offline" });
        Assert.Equal(IntercomDegradationState.CriticalPower, service.GetCurrentIntercomState());

        // 3. Shield Hit occurs (Highest priority, overrides Power)
        _processLogLineMethod.Invoke(service, new object[] { "[Vehicle] Shield hit" });
        Assert.Equal(IntercomDegradationState.ShieldHit, service.GetCurrentIntercomState());
    }
}
