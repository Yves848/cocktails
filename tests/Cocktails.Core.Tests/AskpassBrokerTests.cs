using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cocktails.Core.Process;
using Cocktails.Core.Sudo;

namespace Cocktails.Core.Tests;

/// <summary>
/// Tests du courtier askpass. Ils exécutent <b>réellement</b> le script helper généré
/// (comme le ferait <c>sudo -A</c>) et vérifient ce qu'il écrit sur sa sortie standard :
/// c'est exactement le contrat que sudo consomme.
/// </summary>
public class AskpassBrokerTests : IDisposable
{
    private readonly string _dir = Path.Combine("/tmp", "ckt-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>Joue le rôle de sudo : lance le helper et rend (code de sortie, sortie).</summary>
    private static async Task<(int ExitCode, string Output)> AskAsync(AskpassBroker broker)
    {
        var result = await new ProcessRunner().RunAsync(broker.HelperPath!, []);
        return (result.ExitCode, result.StandardOutput);
    }

    [Fact]
    public async Task Helper_WritesTypedPasswordToStdout()
    {
        await using var broker = new AskpassBroker(_dir);
        broker.Handler = (_, _) => Task.FromResult<PasswordReply?>(new PasswordReply("hunter2", Remember: false));
        await broker.StartAsync();

        var (exitCode, output) = await AskAsync(broker);

        Assert.Equal(0, exitCode);
        Assert.Equal("hunter2", output.Trim());
    }

    [Fact]
    public async Task Cancelling_MakesHelperFail_SoSudoGivesUp()
    {
        await using var broker = new AskpassBroker(_dir);
        broker.Handler = (_, _) => Task.FromResult<PasswordReply?>(null);
        await broker.StartAsync();

        var (exitCode, output) = await AskAsync(broker);

        Assert.NotEqual(0, exitCode);
        Assert.Empty(output.Trim());
    }

    [Fact]
    public async Task RememberedPassword_IsReusedInLaterOperation_WithoutAskingAgain()
    {
        var asked = 0;
        await using var broker = new AskpassBroker(_dir);
        broker.Handler = (_, _) =>
        {
            asked++;
            return Task.FromResult<PasswordReply?>(new PasswordReply("hunter2", Remember: true));
        };
        await broker.StartAsync();

        broker.BeginOperation();
        await AskAsync(broker);
        broker.BeginOperation();
        var (exitCode, output) = await AskAsync(broker);

        Assert.Equal(1, asked);
        Assert.Equal(0, exitCode);
        Assert.Equal("hunter2", output.Trim());
    }

    [Fact]
    public async Task PasswordNotRemembered_IsAskedAgainInLaterOperation()
    {
        var asked = 0;
        await using var broker = new AskpassBroker(_dir);
        broker.Handler = (_, _) =>
        {
            asked++;
            return Task.FromResult<PasswordReply?>(new PasswordReply("hunter2", Remember: false));
        };
        await broker.StartAsync();

        broker.BeginOperation();
        await AskAsync(broker);
        broker.BeginOperation();
        await AskAsync(broker);

        Assert.Equal(2, asked);
    }

    [Fact]
    public async Task SecondRequestWithinSameOperation_DropsCacheAndSignalsRetry()
    {
        var prompts = new List<PasswordPrompt>();
        await using var broker = new AskpassBroker(_dir);
        broker.Handler = (prompt, _) =>
        {
            prompts.Add(prompt);
            return Task.FromResult<PasswordReply?>(new PasswordReply("wrong", Remember: true));
        };
        await broker.StartAsync();

        // sudo redemande dans la foulée : la saisie précédente a été refusée.
        broker.BeginOperation();
        await AskAsync(broker);
        await AskAsync(broker);

        // Deux appels au handler : le mot de passe gardé n'a pas été resservi tel quel.
        Assert.Equal(2, prompts.Count);
        Assert.False(prompts[0].IsRetry);
        Assert.True(prompts[1].IsRetry);
    }

    [Fact]
    public async Task RememberedPassword_IsPurgedAfterItsLifetime()
    {
        var now = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        var asked = 0;
        await using var broker = new AskpassBroker(_dir, () => now) { CacheLifetime = TimeSpan.FromMinutes(60) };
        broker.Handler = (_, _) =>
        {
            asked++;
            return Task.FromResult<PasswordReply?>(new PasswordReply("hunter2", Remember: true));
        };
        await broker.StartAsync();

        broker.BeginOperation();
        await AskAsync(broker);

        now = now.AddMinutes(61);
        broker.BeginOperation();
        await AskAsync(broker);

        Assert.Equal(2, asked);
    }

    [Fact]
    public async Task InfiniteLifetime_KeepsThePasswordForTheWholeSession()
    {
        var now = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        var asked = 0;
        await using var broker = new AskpassBroker(_dir, () => now)
        {
            CacheLifetime = Timeout.InfiniteTimeSpan,
        };
        broker.Handler = (_, _) =>
        {
            asked++;
            return Task.FromResult<PasswordReply?>(new PasswordReply("hunter2", Remember: true));
        };
        await broker.StartAsync();

        broker.BeginOperation();
        await AskAsync(broker);

        now = now.AddDays(30);
        broker.BeginOperation();
        var (_, output) = await AskAsync(broker);

        Assert.Equal(1, asked);
        Assert.Equal("hunter2", output.Trim());
    }

    [Fact]
    public async Task ForgetPassword_DropsTheRememberedPassword()
    {
        await using var broker = new AskpassBroker(_dir);
        broker.Handler = (_, _) => Task.FromResult<PasswordReply?>(new PasswordReply("hunter2", Remember: true));
        await broker.StartAsync();

        broker.BeginOperation();
        await AskAsync(broker);
        Assert.True(broker.HasRememberedPassword);

        broker.ForgetPassword();

        Assert.False(broker.HasRememberedPassword);
    }
}
