# CatosBuildHologram protocol v1

Status: Phase 0 design contract. This document defines the wire boundary for
the optional server-authoritative role. It is not evidence that networking is
implemented or gameplay-verified.

## Authority and negotiation

- The client starts in `VanillaClientOnly` mode until a compatible server role
  explicitly negotiates `ServerAuthoritative`.
- The server decides whether enhanced mode is available. A client packet can
  request a capability, but cannot enable the capability by itself.
- Protocol version is an integer major version. Unknown major versions cause a
  clean fallback to vanilla client-only mode; no partial enhanced behavior is
  allowed.
- Every request has a client-generated non-empty request ID. The server must
  make duplicate request handling idempotent for the request lifetime.
- Every mutable blueprint has a server-generated opaque ID, a monotonic
  revision, and a state revision in every response/delta.

## Message envelope

The eventual transport may use the verified Valheim routed-RPC or a BepInEx
network registration boundary. The logical envelope is:

```text
Message
  ProtocolMajor       byte
  MessageType         byte
  RequestId           bounded string, max 64 UTF-8 bytes
  ExpectedRevision    uint32, optional
  Payload             message-specific bounded fields
```

Limits for the first implementation:

```text
MaxMessageBytes       16 KiB
MaxBlueprintsPerPage  128
MaxBlueprintsPerOwner 512
MaxBlueprintsPerWorld 4096
MaxReasonBytes         160 UTF-8 bytes
MaxPieceTypeBytes      128 UTF-8 bytes
MaxPlanGroupBytes      64 UTF-8 bytes
MaxRequestRate         8 requests/second/player, burst 16
```

Transforms must be finite, within the configured builder/world bounds, and
canonicalized by the server. Positions and rotations in records are detached
values; no Unity object or live component is serialized.

## Logical messages

```text
ClientHello
  ProtocolMajor
  ClientBuildVersion
  Capabilities

ServerHello
  ProtocolMajor
  AuthorityMode
  ServerBuildVersion
  WorldSessionId
  FeatureFlags

CreateBlueprintRequest
  RequestId
  PreviewToken
  PieceTypeHint             informational only
  PositionHint, RotationHint informational only

CreateBlueprintResponse
  RequestId
  Accepted
  BlueprintRecord?          present only after server validation
  RejectionCode?            bounded enum

BlueprintPageRequest
  RequestId
  PageCursor?

BlueprintPageResponse
  RequestId
  WorldRevision
  Records[]
  NextPageCursor?

BlueprintDelta
  WorldRevision
  AddedOrChanged[]
  RemovedIds[]

ValidateRequest
  RequestId
  BlueprintId
  ExpectedRevision

ValidateResponse
  RequestId
  BlueprintId
  Revision
  SupportState
  BuildState
  BlockReason

AutobuildStartRequest
  RequestId
  ScopeId or bounded selection IDs
  ExpectedWorldRevision

AutobuildStartResponse
  RequestId
  Accepted
  SessionId?
  RejectionCode?

AutobuildStepResult
  SessionId
  StepSequence
  BlueprintId
  BlueprintRevision
  ResultState
  SupportState
  BlockReason

AutobuildPause/Resume/CancelRequest
  RequestId
  SessionId
```

The server must never accept client-provided support colors, costs, ownership,
permission decisions, build ordering, inventory lists, or completion claims.

## Detached record shared with BuildSight

```text
BuildHologramPreview
  Active
  PreviewToken
  PieceTypeId
  CanonicalPosition
  CanonicalRotation
  AuthorityMode
  NativePlacementState
  SupportState
  BuildState
  BlueprintId?
  Revision?

BuildHologramStatus
  BlueprintId
  Revision
  SupportState
  BuildState
  BlockReason
```

`VanillaClientOnly` means `SupportState` is explicitly predicted or unknown;
BuildSight must label it `Predicted` and must not promote it to server truth.
`ServerAuthoritative` means the displayed status came from a fresh server
revision. Missing, stale, or incompatible values are discarded safely.

## Initial reason/state enums

```text
AuthorityMode: VanillaClientOnly, ServerAuthoritative
SupportState: Supported, Unsupported, Blocked, Unknown, Stale
BuildState: Planned, Queued, Building, Built, Cancelled, Failed
RejectionCode: InvalidRequest, UnsupportedVersion, InvalidPiece,
  InvalidTransform, OutOfRange, NoPermission, QuotaExceeded, StaleRevision,
  NativePlacementInvalid, MissingTool, MissingMaterials, WorldBusy,
  TransactionUnavailable, RateLimited
```

These enums are intentionally small and stable. Human-readable reason text is
localization/UI data, not authority data.
