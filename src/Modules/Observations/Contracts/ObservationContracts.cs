using System.Collections.Immutable;
using wAInery.BuildingBlocks;

namespace wAInery.Modules.Observations.Contracts;

public enum ObservationValueKind { Numeric, Narrative, Boolean, Categorical }
public enum IntakeQuality { Unchecked, Valid, Flagged, Quarantined }
public enum ObservationQuality { Valid, Suspect }

public sealed record ObservationValue
{
    public ObservationValueKind Kind { get; }
    public decimal? NumericValue { get; }
    public string? TextValue { get; }
    public bool? BooleanValue { get; }
    public string? Unit { get; }
    public string? Method { get; }
    private ObservationValue(ObservationValueKind kind, decimal? number, string? text, bool? boolean, string? unit, string? method)
        => (Kind, NumericValue, TextValue, BooleanValue, Unit, Method) = (kind, number, text, boolean, unit, method);
    public static ObservationValue Number(decimal value, string unit, string? method = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);
        return new(ObservationValueKind.Numeric, value, null, null, unit, method);
    }
    public static ObservationValue Narrative(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new(ObservationValueKind.Narrative, null, value, null, null, null);
    }
    public static ObservationValue Boolean(bool value) => new(ObservationValueKind.Boolean, null, null, value, null, null);
    public static ObservationValue Categorical(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new(ObservationValueKind.Categorical, null, value, null, null, null);
    }
}

public sealed record RecordObservationCommand : SubjectCommand
{
    public string ObservationType { get; }
    public ObservationValue OriginalValue { get; }
    public string SourceId { get; }
    public DateTimeOffset ObservedAt { get; }
    public DateTimeOffset ReceivedAt { get; }
    public IntakeQuality Quality { get; }
    public string? SourceExternalKey { get; }
    public RecordObservationCommand(CommandContext context, SubjectRef subject, string observationType,
        ObservationValue originalValue, string sourceId, DateTimeOffset observedAt, DateTimeOffset receivedAt,
        IntakeQuality quality = IntakeQuality.Unchecked, string? sourceExternalKey = null) : base(context, subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(observationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(originalValue);
        if (!Enum.IsDefined(quality)) throw new ArgumentOutOfRangeException(nameof(quality));
        (ObservationType, OriginalValue, SourceId, ObservedAt, ReceivedAt, Quality, SourceExternalKey) =
            (observationType, originalValue, sourceId, observedAt.ToUniversalTime(), receivedAt.ToUniversalTime(), quality, sourceExternalKey);
    }
}

public sealed record CorrectObservationCommand(CommandContext Context, SubjectRef Subject, string CorrectsId,
    string ObservationType, ObservationValue OriginalValue, string SourceId, DateTimeOffset ObservedAt,
    DateTimeOffset ReceivedAt) : SubjectCommand(Context, Subject);
public sealed record MeasurementTypeDto(string Code, string Version, ObservationValueKind Kind, string? CanonicalUnit,
    ImmutableArray<string> PermittedSubjectKinds, bool MethodRequired);
public sealed record RegisterMeasurementTypeCommand(CommandContext Context, MeasurementTypeDto Definition);
public sealed record ObservationDto(string Id, SubjectRef Subject, string ObservationType, ObservationValue OriginalValue,
    ObservationValue? CanonicalValue, string SourceId, DateTimeOffset ObservedAt, DateTimeOffset ReceivedAt,
    DateTimeOffset RecordedAt, ObservationQuality Quality, string? CorrectsId, long Version);
public sealed record ObservationWindowDto(SubjectRef Subject, DateTimeOffset AsOf, DateTimeOffset KnownAt,
    ImmutableArray<ObservationDto> Items, string? NextCursor);

public sealed record ObservationQuery
{
    public SubjectRef Subject { get; }
    public ImmutableArray<string> Types { get; }
    public DateTimeOffset AsOf { get; }
    public int MaxCount { get; }
    public string? Cursor { get; }
    public ObservationQuery(SubjectRef subject, IEnumerable<string> types, DateTimeOffset asOf, int maxCount, string? cursor = null)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(types);
        if (!subject.HasValidScope) throw new ArgumentException("A scoped subject is required.");
        if (maxCount is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(maxCount));
        var snapshot = types.ToImmutableArray();
        if (snapshot.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Measurement type cannot be blank.");
        (Subject, Types, AsOf, MaxCount, Cursor) = (subject, snapshot, asOf.ToUniversalTime(), maxCount, cursor);
    }
}

public interface IObservationReadPort
{
    Task<Result<ObservationWindowDto>> QueryAsync(ObservationQuery query, CancellationToken ct);
    Task<Result<MeasurementTypeDto>> GetMeasurementTypeAsync(string typeCode, CancellationToken ct);
}

public interface IObservationCommandPort
{
    Task<Result<CommandReceipt>> RecordAsync(RecordObservationCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> CorrectAsync(CorrectObservationCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> RegisterMeasurementTypeAsync(RegisterMeasurementTypeCommand command, CancellationToken ct);
}
