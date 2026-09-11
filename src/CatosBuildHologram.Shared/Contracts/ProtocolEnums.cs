namespace CatosBuildHologram.Shared.Contracts
{
    public enum AuthorityMode : byte
    {
        VanillaClientOnly = 0,
        ServerAuthoritative = 1
    }

    public enum SupportState : byte
    {
        Unknown = 0,
        Supported = 1,
        Unsupported = 2,
        Blocked = 3,
        Stale = 4
    }

    public enum BuildState : byte
    {
        Planned = 0,
        Queued = 1,
        Building = 2,
        Built = 3,
        Cancelled = 4,
        Failed = 5
    }

    public enum NativePlacementState : byte
    {
        Unknown = 0,
        Valid = 1,
        Invalid = 2,
        Blocked = 3
    }

    public enum RejectionCode : byte
    {
        None = 0,
        InvalidRequest = 1,
        UnsupportedVersion = 2,
        InvalidPiece = 3,
        InvalidTransform = 4,
        OutOfRange = 5,
        NoPermission = 6,
        QuotaExceeded = 7,
        StaleRevision = 8,
        NativePlacementInvalid = 9,
        MissingTool = 10,
        MissingMaterials = 11,
        WorldBusy = 12,
        TransactionUnavailable = 13,
        RateLimited = 14
    }

    public enum MessageType : byte
    {
        ClientHello = 1,
        ServerHello = 2,
        CreateBlueprintRequest = 3,
        CreateBlueprintResponse = 4,
        BlueprintPageRequest = 5,
        BlueprintPageResponse = 6,
        BlueprintDelta = 7,
        ValidateRequest = 8,
        ValidateResponse = 9,
        AutobuildStartRequest = 10,
        AutobuildStartResponse = 11,
        AutobuildStepResult = 12,
        AutobuildPauseRequest = 13,
        AutobuildResumeRequest = 14,
        AutobuildCancelRequest = 15
    }
}
