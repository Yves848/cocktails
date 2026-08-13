using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cocktails.Core.Sudo;
using Cocktails.Localization;
using Cocktails.ViewModels;

namespace Cocktails.Core.Tests;

/// <summary>
/// Dialogue de saisie du mot de passe administrateur : le shell traduit une demande du
/// courtier askpass en dialogue, et la réponse de l'utilisateur en <see cref="PasswordReply"/>.
/// </summary>
public class PasswordPromptTests
{
    private static MainViewModel NewShell() => new(new DesignHomebrewService());

    [Fact]
    public async Task Submitting_ReturnsThePasswordAndClosesTheDialog()
    {
        var shell = NewShell();

        var pending = shell.RequestPasswordAsync(new PasswordPrompt(IsRetry: false), CancellationToken.None);

        Assert.NotNull(shell.PasswordRequest);
        shell.PasswordInput = "hunter2";
        shell.RememberPassword = true;
        shell.SubmitPasswordCommand.Execute(null);

        var reply = await pending;

        Assert.Equal("hunter2", reply?.Password);
        Assert.True(reply?.Remember);
        Assert.Null(shell.PasswordRequest);
        Assert.Equal(string.Empty, shell.PasswordInput);
    }

    [Fact]
    public async Task Cancelling_ReturnsNull()
    {
        var shell = NewShell();

        var pending = shell.RequestPasswordAsync(new PasswordPrompt(IsRetry: false), CancellationToken.None);
        shell.PasswordInput = "hunter2";
        shell.CancelPasswordCommand.Execute(null);

        Assert.Null(await pending);
        Assert.Null(shell.PasswordRequest);
        Assert.Equal(string.Empty, shell.PasswordInput);
    }

    [Fact]
    public async Task RetryPrompt_TellsTheUserThePasswordWasRejected()
    {
        var shell = NewShell();

        var pending = shell.RequestPasswordAsync(new PasswordPrompt(IsRetry: true), CancellationToken.None);

        Assert.True(shell.PasswordRequest?.IsRetry);
        Assert.Contains("incorrect", shell.PasswordRequest!.Message, System.StringComparison.OrdinalIgnoreCase);

        shell.CancelPasswordCommand.Execute(null);
        await pending;
    }

    [Fact]
    public void ForgettingThePassword_CallsTheBrokerAndSaysSo()
    {
        var forgotten = false;
        var settings = new SettingsViewModel(
            new DesignHomebrewService(), forgetSudoPassword: () => forgotten = true);

        settings.ForgetSudoPasswordCommand.Execute(null);

        Assert.True(forgotten);
        Assert.Equal(Localization.Strings.Get("Settings.SudoForgotten", Localization.AppLanguage.French),
            settings.StatusMessage);
    }

    [Fact]
    public void ChoosingALifetime_WritesItToTheSettings()
    {
        var settings = new AppSettings();
        var screen = new SettingsViewModel(new DesignHomebrewService(), settings);

        screen.SelectedSudoLifetime = screen.SudoLifetimes.Single(o => o.Minutes == 15);

        Assert.Equal(15, settings.SudoPasswordLifetimeMinutes);
    }

    [Fact]
    public async Task CancellingTheOperation_ClosesTheDialogAndReturnsNull()
    {
        var shell = NewShell();
        using var cts = new CancellationTokenSource();

        var pending = shell.RequestPasswordAsync(new PasswordPrompt(IsRetry: false), cts.Token);
        await cts.CancelAsync();

        Assert.Null(await pending);
        Assert.Null(shell.PasswordRequest);
    }
}
