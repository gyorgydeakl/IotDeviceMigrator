using IotDeviceMigrator.Client;

namespace IotDeviceMigrator.Migration;

/// <summary>
/// Represents a single, atomic step in the device migration pipeline.
/// A step encapsulates one action (e.g., validation or a device method call)
/// and decides whether the pipeline should continue or stop after execution.
/// </summary>
public interface IMigrationStep
{
    /// <summary>
    /// Human-readable name of the step, used for logs and diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The IoT client (source or target) this step operates against.
    /// Used for context (e.g., logging the hub/environment name) and for executing operations.
    /// </summary>
    IIotClient HubClient { get; }

    /// <summary>
    /// Executes the step for the specified device.
    /// </summary>
    /// <param name="deviceId">Identifier of the device being migrated.</param>
    /// <returns>
    /// A <see cref="StepSuccessResult"/> indicating whether the migration pipeline should continue.
    /// Set <see cref="StepSuccessResult.Continue"/> to false to stop the pipeline (success completion),
    /// or true to proceed to the next step.
    /// </returns>
    Task<StepSuccessResult> StepAsync(string deviceId);
}

/// <summary>
/// Standardized result of a migration step.
/// </summary>
/// <param name="Continue">
/// Indicates whether the migration process should proceed to the next step.
/// Typical usage:
/// - false: migration is considered complete (no further steps needed).
/// - true: proceed with the next step in the pipeline.
/// </param>
public record StepSuccessResult(bool Continue);