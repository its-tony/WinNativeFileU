using WinNativeFileU.Core;
using Xunit;

namespace WinNativeFileU.Core.Tests;

public sealed class ResidentPolicyTests
{
    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(false, true, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, true, false)]
    public void CloseCannotHideWithoutRecoveryOrDuringExit(bool preference, bool tray, bool exiting, bool expected) =>
        Assert.Equal(expected, ResidentPolicy.HideOnClose(preference, tray, exiting));

    [Fact]
    public void SettingsRejectFutureOrUnknownValues()
    {
        Assert.True(new ResidentSettings().IsValid);
        Assert.False(new ResidentSettings(Version: 2).IsValid);
        Assert.False(new ResidentSettings(Theme: (ResidentTheme)99).IsValid);
        Assert.False(new ResidentSettings(Shortcut: (ResidentShortcut)99).IsValid);
    }

    [Fact]
    public void StartupUsesQuotedExecutableAndOnlyFixedResidentArguments() =>
        Assert.Equal("\"C:\\Program Files\\WinNativeFileU\\app.exe\" --resident --resident-minimized",
            ResidentPolicy.StartupCommand(@"C:\Program Files\WinNativeFileU\app.exe"));

    [Theory]
    [InlineData("app.exe")]
    [InlineData(@"\\server\share\app.exe")]
    [InlineData("C:\\app.exe\" --unexpected")]
    [InlineData("C:\\app\n.exe")]
    [InlineData(@"C:\app.bat")]
    public void InvalidSignInCommandCannotBeRegistered(string executable) =>
        Assert.Throws<ArgumentException>(() => ResidentPolicy.StartupCommand(executable));

    [Fact]
    public void RunCommandLengthIsBounded() => Assert.Throws<ArgumentException>(() => ResidentPolicy.StartupCommand("C:\\" + new string('x', 250) + ".exe"));
}
