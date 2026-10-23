using EnglishAdventure.Application.Authorization.Commands.BootstrapSuperAdmin;
using EnglishAdventure.Application.Authorization.Commands.SyncSystemRoles;
using EnglishAdventure.Application.Common.Exceptions;
using FluentValidation;
using MediatR;

namespace EnglishAdventure.Api.CommandLine;

/// <summary>
/// One-off administration tasks run instead of the web server, for example
/// <c>dotnet run --project src/EnglishAdventure.Api -- bootstrap-superadmin</c>. They are deliberately not HTTP endpoints.
/// </summary>
internal static class CommandLineTasks
{
    public const string SyncRoles = "sync-roles";
    public const string BootstrapSuperAdmin = "bootstrap-superadmin";

    public const string SuperAdminEmailKey = "Bootstrap:SuperAdmin:Email";
    public const string SuperAdminPasswordKey = "Bootstrap:SuperAdmin:Password";

    public static bool IsTask(string[] args) => args.Length > 0 && args[0] is SyncRoles or BootstrapSuperAdmin;

    /// <returns>The process exit code: 0 on success, 1 on failure.</returns>
    public static async Task<int> RunAsync(WebApplication app, string[] args)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("EnglishAdventure.CommandLine");

        try
        {
            return args[0] switch
            {
                SyncRoles => await RunSyncRolesAsync(sender, logger),
                _ => await RunBootstrapSuperAdminAsync(sender, app.Configuration, logger)
            };
        }
        catch (ValidationException exception)
        {
            foreach (var error in exception.Errors)
            {
                logger.LogError("{Property}: {Message}", error.PropertyName, error.ErrorMessage);
            }

            return 1;
        }
        catch (ConflictException exception)
        {
            logger.LogError("{Message}", exception.Message);
            return 1;
        }
    }

    private static async Task<int> RunSyncRolesAsync(ISender sender, ILogger logger)
    {
        var result = await sender.Send(new SyncSystemRolesCommand());

        logger.LogInformation(
            "Roles synced: {PermissionsAdded} permission(s) added, {RolesCreated} role(s) created, {GrantsAdded} grant(s) added.",
            result.PermissionsAdded,
            result.RolesCreated,
            result.GrantsAdded);

        return 0;
    }

    private static async Task<int> RunBootstrapSuperAdminAsync(ISender sender, IConfiguration configuration, ILogger logger)
    {
        var email = configuration[SuperAdminEmailKey];
        var password = configuration[SuperAdminPasswordKey];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            logger.LogError(
                "Set {EmailKey} and {PasswordKey} with dotnet user-secrets (Development) or environment variables " +
                "(Bootstrap__SuperAdmin__Email, Bootstrap__SuperAdmin__Password), then run the command again.",
                SuperAdminEmailKey,
                SuperAdminPasswordKey);
            return 1;
        }

        var result = await sender.Send(new BootstrapSuperAdminCommand(email, password));

        logger.LogInformation(
            "SuperAdmin {Email} created (user {UserId}). Remove the password from user secrets or the environment now.",
            email,
            result.UserId);

        return 0;
    }
}
