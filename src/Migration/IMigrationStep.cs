namespace IotDeviceMigrator.Migration;

public record MigrationStep
{
    public required string Name { get; init; }
    public required Func<Task<MigrationResult?>> StepAsync { get; init; }
}
