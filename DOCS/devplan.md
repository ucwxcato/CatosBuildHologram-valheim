# CatosBuildHologram — Development Plan

> **Status:** Phase 2 client-only interception and local persistence are
> implemented and build. Phase 3 has a bounded local hologram renderer,
> guided native-placement seam, status-color mapping, and HUD in source. The
> server transport, canonical server feed, support authority, runtime load
> evidence, and gameplay verification remain pending.
>
> **Purpose:** Let players plan construction locally on vanilla servers, or use
> server-validated shared blueprints and support-safe ordered construction when
> the optional server role is installed.
>
> **Authority:** This is the canonical CatosBuildHologram plan. The optional
> client-only measurement companion is
> [`../../CatosBuildSight/DOCS/devplan.md`](../../CatosBuildSight/DOCS/devplan.md).
> Shared environment and test-server rules are in
> [`../../world-setup.md`](../../world-setup.md).
> The required test-world source is
> `C:\Users\magni\Documents\BotsnCoding\Valheim\Dedicated`; use the shared
> document's exact save root, junction, port, and launch contract.
>
> **Target:** Valheim `1.0.7` / network version `39`, Unity
> `6000.0.75.2503836`, BepInExPack Valheim `5.4.2350`, BepInEx `5.4.23.5`,
> .NET Framework `4.8`. These are specified baseline values and must be
> refreshed against installed assemblies before implementation.

## Phase 0 discovery record (2026-09-11)

This section records the first environment and native metadata pass. It is
evidence for planning and adapter boundaries, not gameplay verification.

### Environment evidence

- The client managed assembly was refreshed into the ignored `lib/` directory
  with `scripts/setup-references.ps1`; 11 required Unity, Valheim, BepInEx,
  and Harmony reference DLLs are present.
- The installed client assembly is
  `C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll`,
  2,568,192 bytes, assembly/file version `0.0.0.0`, last written 2026-09-11.
- The installed dedicated-server assembly is
  `C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server\valheim_server_Data\Managed\assembly_valheim.dll`,
  2,560,000 bytes, assembly/file version `0.0.0.0`, last written 2026-09-11.
  The different size means client and server references must be checked
  independently; the client DLL must not be treated as server proof.
- The selected r2modman profile contains BepInEx `5.4.23.5` and the
  `JereKuusela-Server_devcommands` test-harness plugin. It is not a clean
  verification profile because it also contains unrelated mods; clean-profile
  runtime evidence remains required.
- `scripts/inspect-native.ps1` now reads assembly metadata through Mono.Cecil
  from the selected profile, avoiding false conclusions from loading Unity
  assemblies outside the Valheim process.

### Verified native adapter candidates

The client assembly metadata contains 1,311 types and confirms these relevant
members:

```text
Player.m_placementGhost                    GameObject
Player.m_placementStatus                   Player.PlacementStatus
Player.UpdatePlacementGhost(bool)
Player.GetPlacementStatus()                Player.PlacementStatus
Player.TryPlacePiece(Piece)                 bool
Player.PlacePiece(Piece, Vector3, Quaternion, bool, bool)
Player.HaveRequirements(Piece, Player.RequirementMode)
Player.ConsumeResources(Piece.Requirement[], int, int, int)
Player.FindClosestSnapPoints(...)
Piece.GetSnapPoints(List<Transform>)
WearNTear.GetSupport()                      float
WearNTear.HaveSupport()                     bool
WearNTear.GetSupportColorValue()            float
WearNTear.UpdateSupport()
Inventory.CountItems(string, int, bool)     int
Inventory.HaveItem(string, bool)            bool
ZNetView.GetZDO()                            ZDO
ZNetView.InvokeRPC(...)
Hud.m_hoverName                             TextMeshProUGUI
```

`Player.PlacementStatus` exposes `Valid`, `Invalid`, `BlockedbyPlayer`,
`NoBuildZone`, `PrivateZone`, `MoreSpace`, `ExtensionMissingStation`,
`WrongBiome`, `NeedCultivated`, `NeedDirt`, `NotInDungeon`, `DeepSnow`,
`NoSnow`, `NoTeleportArea`, and `NoRayHits`. `Piece.Requirement` exposes
`m_resItem`, `m_amount`, `m_amountPerLevel`, and
`m_extraAmountOnlyOneIngredient`.

The native preview already calculates terrain, collision, snapping, no-build,
private-area, biome, station, and player-blocking conditions inside
`UpdatePlacementGhost`. The adapter must observe this state rather than
reimplementing a competing placement simulation.

### Transaction decision and remaining blocker

Metadata inspection shows the normal native path is ordered as:

```text
UpdatePlacementGhost(true)
-> check Player.m_placementStatus
-> TryPlacePiece(piece)
-> PlacePiece(piece, ghost.position, ghost.rotation, true, cheated)
-> ConsumeResources(piece.m_resources, -1, 0, 1)
```

`TryPlacePiece`/`PlacePiece` instantiate and initialize the real piece before
`UpdatePlacement` calls `ConsumeResources`. This is a useful native guided
placement path, but it is not proof of an atomic server autobuild transaction:
the real piece and material charge are separate operations, and the server
does not have a client UI ghost by default. Therefore Phase 5 autobuild stays
disabled until runtime hooks prove a no-duplication/no-loss path, such as a
server-side transaction wrapper with rollback or a verified native operation
that combines creation and cost handling. No client packet may call
`PlacePiece` as a shortcut.

### Phase 0 decisions

- Use role-separated client/server artifacts, with process guards, unless a
  single assembly is proven safe in both processes. The different installed
  client/server assemblies reinforce this boundary.
- Use `Player.UpdatePlacementGhost` and the native `m_placementGhost` as the
  preview/snap source. Use detached position/rotation/piece identity values in
  blueprint records.
- Use `WearNTear` support methods for real-piece status when available. A
  pre-placement hologram status remains predicted/unknown until runtime proves
  that a temporary preview can safely expose equivalent support semantics.
- Use protocol major version 1, request IDs, server-generated blueprint IDs,
  monotonic revisions, bounded records/messages, and the detached BuildSight
  contract defined in [`protocol.md`](protocol.md).

## 0. Outcome

In vanilla-server client-only mode, confirming a normal native build preview
creates a local blueprint instead of a real piece. The local player sees a
hologram and a clearly labelled predicted support status; other players and the
server do not see or persist it. The player can use a guided native build queue
and, only if the native-input assist passes its safety gate, an optional local
assist that submits ordinary native build actions.

In server-authoritative mode, installing the server role and client companion
adds shared persistent blueprints, server-issued status colors, multiplayer
visibility, and authoritative ordered autobuild. Green then means the server
currently confirms supported and buildable; red means unsupported; amber/gray
means blocked or unknown.

Complete when:

```text
client-only mode is used on a vanilla server
-> player previews and confirms a piece
-> local plan stores the snapped transform and predicted status
-> guided native placement builds one selected piece at a time, or a guarded local assist is used
-> server accepts/rejects ordinary native actions and no client-side item mutation occurs

or:

matching server and client roles are loaded
-> player previews and confirms a valid piece
-> server stores and broadcasts a validated blueprint record
-> clients render the snapped hologram and current server status
-> builder starts autobuild with valid tools/materials
-> server revalidates, builds supported pieces in dependency order, and charges exact costs
-> blocked pieces remain blueprints with no unsafe build or item loss
```

## 1. Locked decisions

- **Two capability tiers:** the product ships a client-only vanilla-server mode
  and an enhanced server-authoritative mode. The client-only mode must be a
  useful, honest product rather than a hidden partial server implementation.
- **Server authority:** in enhanced mode, the server owns all shared blueprint
  records, support status, buildability, costs, permissions, ordering,
  inventory changes, and creation of real pieces. Client visuals are never
  authoritative. In vanilla mode, the server still owns every real native
  build; local plans and predictions are not authority.
- **Two-role delivery for enhanced mode:** the product ships explicit client
  and server roles. The enhanced installation requires the server role on the
  dedicated server and the client role on connecting clients. The client-only
  CatosBuildSight mod is an optional companion, not an authority dependency.
- **Blueprint-first placement:** confirming a normal build preview creates a
  blueprint record and hologram, not a real piece. Initial blueprint placement
  consumes no construction materials, but remains subject to server validation,
  permissions, quotas, range, and native placement constraints.
- **Native placement contract:** use verified native build/placement and
  stability behavior wherever possible. Do not accept client coordinates,
  prefab names, support colors, or costs without server-side reconstruction and
  validation.
- **Status semantics:** in enhanced mode, green is server-confirmed supported
  and buildable; red is server-confirmed unsupported or unsafe; amber means
  blocked by a rule such as missing materials, tool, permission, collision, or
  range; gray means unknown/stale/unavailable. In vanilla mode, equivalent
  colors must be prefixed or labelled `Predicted`; a client must not imply
  server confirmation.
- **Red planning is allowed:** a red blueprint can remain as a design plan, but
  autobuild must skip it until a fresh server validation returns green.
- **Ordered autobuild:** build exactly one piece per server transaction/step,
  re-evaluate the dependency graph after each successful build, and never rely
  on client-provided ordering.
- **Material safety:** consume exact native costs only after the server has
  validated the current piece and has a verified atomic or compensating build
  path. If that guarantee cannot be established, autobuild remains disabled.
- **No fake authority objects:** holograms are client-rendered views of
  detached server records, not networked `Piece` objects that can be mistaken
  for real construction.
- **CatosBuildSight contract:** BuildSight owns measurements, guides, and
  construction readouts; BuildHologram owns holograms, server status, blueprint
  lifecycle, and autobuild. Both degrade independently when the other is
  absent or version-incompatible.
- **No automatic blueprint import in MVP:** plans are created through the
  player's normal build-preview confirmation. Existing structures, external
  blueprint files, and copy/paste import are deferred.
- **No fabricated vanilla-server authority:** client-only mode cannot promise
  shared holograms, server persistence, guaranteed support colors, or safe
  remote autobuild. Those promises belong only to enhanced mode.

## 2. Goals and non-goals

### Goals

- Replace confirmed native construction with a server-validated blueprint
  record and snapped client hologram.
- Synchronize bounded blueprint identity, transform, state, and support status
  to eligible clients.
- Make support/buildability status clear without claiming more certainty than
  native validation provides.
- Allow the owner or explicitly authorized server role to cancel, remove, or
  manage blueprints safely.
- Build plans incrementally in support-safe dependency order.
- Consume exact materials from the authenticated builder's authoritative
  inventory and never duplicate or destroy items on failure.
- Pause and recover safely on missing resources, changed terrain/structures,
  disconnects, range loss, permission changes, restarts, or native failures.
- Integrate with CatosBuildSight so the player gets measurements and guides
  around the same preview/blueprint identity without duplicated holograms.

### Explicitly out of scope

- Client-side packet fabrication or client-authoritative building.
- Building for offline players, remote players, or arbitrary coordinates.
- Server-wide automatic construction without a connected authenticated builder.
- Bypassing wards, permissions, workbench/range/tool requirements, native
  collision checks, stability rules, or material costs.
- Free real pieces, resource generation, inventory editing commands, or admin
  economy tools.
- Importing arbitrary prefab names, serialized Unity objects, or untrusted
  client files as blueprints.
- Making local holograms visible to vanilla clients or persisting them on a
  vanilla server.
- A complete blueprint editor, terrain deformation system, or multiplayer
  collaborative editing suite in the first release.

## 3. User experience / operational flow

### 3.0 Mode selection

At startup the client detects whether the CatosBuildHologram server protocol is
available. The UI must make the mode obvious:

| Mode | Server requirement | Shared holograms | Status authority | Build behavior |
|---|---|---|---|---|
| Vanilla client-only | None | No; local player only | Predicted/local | Guided native placement; optional guarded assist |
| Enhanced server | Server role + compatible client | Yes | Server-issued | Server-ordered autobuild |

If protocol negotiation fails, the client must not silently display enhanced
claims. It falls back to local mode or disables blueprint interception based on
configuration.

### 3.0.1 Vanilla-server client-only flow

1. The client enters blueprint mode and reads the normal native build preview.
2. Confirming stores a detached local record and suppresses that one native
   placement locally; no server record, RPC, material charge, or world object
   is created.
3. The client renders a local hologram at the native snapped transform and
   labels support as `Predicted` or `Unavailable`.
4. Guided mode selects the next plan record, returns it to the native preview,
   and asks the player to confirm the real piece normally. A future guarded
   assist may invoke only the ordinary native build action, one piece at a
   time, after local checks; it must pause on any ambiguity.
5. The server accepts or rejects the normal native action. The client removes
   the local record only after observing a confirmed real native piece;
   otherwise it retains the plan and reports the rejection.

This mode is inherently best-effort: another player, world change, server
rules, latency, or hidden native state can invalidate a local prediction.

### 3.1 Enter blueprint mode in enhanced server mode

1. A client with the companion selects a build piece using the normal build UI.
2. The client shows the native snapped preview, with a hologram-mode indicator.
3. The client sends only a request derived from the current preview. It does
   not declare ownership, cost, support, or permission in the request.
4. The server authenticates the connection, reconstructs the piece identity and
   native requirements, validates the transform and current world state, and
   creates a blueprint record if accepted.
5. The server returns the record ID, canonical transform, revision, status, and
   reason code. Clients render the canonical server result.

### 3.2 Hologram status

Each enhanced-mode hologram has a server status revision. Client-only records
use the same visual palette but must display `Predicted` and have no server
revision. Clients render a bounded palette:

| Color | Meaning | Autobuild behavior |
|---|---|---|
| Green | Enhanced: server confirms supported/buildable; vanilla: local prediction only | Enhanced: eligible after fresh validation; vanilla: guided/assist candidate only |
| Red | Enhanced: server confirms unsafe; vanilla: local prediction says unsafe | Enhanced: skip; vanilla: do not assist automatically |
| Amber | Blocked by materials, tool, range, permission, collision, or similar | Pause/resolve blocker |
| Gray | Stale, unknown, unsupported data, or synchronization gap | Never build automatically |

The exact reason is available in the local status panel, for example:
`Unsupported: no stable chain`, `Missing: 2 Fine Wood`, or `Stale: refresh pending`.
The color is a server-issued result, not a client-side support simulation.

### 3.3 Start and run autobuild

1. The authenticated player selects an owned/authorized blueprint set or a
   bounded local plan group and presses the configured autobuild key.
2. The server confirms the player is alive, connected, permitted, within the
   configured builder leash, and has the required native tool/context.
3. The server creates a per-player build session with a request ID and a
   snapshot of eligible blueprint IDs/revisions. The client cannot reorder or
   add arbitrary records after the session starts.
4. The server computes support dependencies and chooses a ready root or the
   next ready piece. It revalidates the selected blueprint immediately before
   construction.
5. The server verifies exact materials and native build conditions, performs
   the verified native build transaction, confirms the real piece exists, and
   only then removes/marks the blueprint built.
6. The server broadcasts the result and recomputes affected neighboring
   blueprints. The next step starts only after the prior step is complete.
7. Missing materials, range loss, disconnection, changed support, permission
   loss, or native failure pauses the session with a reason. It never skips
   silently or continues consuming materials.

### 3.4 Cancel, edit, and recovery

- The owner can cancel a blueprint that has not become a real piece, subject to
  server permission and state locks.
- Cancelling or removing a blueprint never refunds construction materials,
  because blueprint placement did not consume them in the first place.
- A building piece cannot be cancelled while its transaction is in progress.
- A disconnect pauses the session and releases locks; it does not build in the
  player's absence.
- On restart, in-progress sessions return to queued/blocked and are never
  replayed automatically without fresh validation.
- In vanilla mode, local cancellation deletes only local plan data. A rejected
  native build leaves the local plan intact for correction or retry.

## 4. Architecture and ownership

The following layout is planned and is not evidence that these files exist:

```text
src/CatosBuildHologram.Shared/
  Contracts/                         versioned DTOs and reason/state enums
  Protocol/                          message IDs and serialization limits
src/CatosBuildHologram.Server/
  ServerPlugin.cs                    server process entry point/lifecycle
  BlueprintRepository.cs             detached records and persistence
  BlueprintRequestService.cs         authenticated request validation
  BlueprintValidator.cs              native identity/transform/permission checks
  SupportResolver.cs                 native support status and bounded graph
  BuildDependencyPlanner.cs          ready roots and deterministic ordering
  AutoBuildService.cs                session state and step execution
  InventoryTransaction.cs            verified material transaction boundary
  ServerPermissions.cs               authoritative permission checks
  NetworkServer.cs                    sync, deltas, request IDs, revisions
  ServerConfig.cs
src/CatosBuildHologram.Client/
  ClientPlugin.cs                    client process entry point/lifecycle
  NativeBuildPreviewController.cs    normal preview and confirmation interception
  HologramRenderer.cs                local visual records and colors
  HologramHud.cs                     status, queue, and failure feedback
  ClientInput.cs                     mode/cancel/autobuild routing
  NetworkClient.cs                   sync and request correlation
  BuildSightInterop.cs               optional companion adapter
  LocalPlanStore.cs                  vanilla-server detached plan records
  GuidedBuildService.cs              safe native placement guidance/assist
  ClientConfig.cs
src/CatosBuildHologram.Tests/
  ProtocolTests/
  DependencyPlannerTests/
  ValidationTests/
  TransactionTests/
scripts/
  setup-references.ps1
  build-release.ps1
  package-release.ps1
TEST_SERVER/
  start_catosbuildhologram_test.bat
DOCS/devplan.md
```

### Ownership table

| Component | Owns | Must not own |
|---|---|---|
| Server plugin | authority, lifecycle, network registration | client rendering or trust in packet claims |
| Repository | detached records, schema, persistence, recovery | live Unity objects or client-owned files |
| Validator | identity, transform, native rules, permissions, quotas | inventory mutation or UI |
| Support resolver | current server support/buildability status | client color overrides |
| Dependency planner | deterministic ready ordering | direct inventory/build writes |
| Autobuild service | session locks, revalidation, step state | client ordering or blind retries |
| Inventory transaction | exact cost and safe consumption boundary | accepting client item lists |
| Client preview controller | native preview display and request creation | server status decisions |
| Hologram renderer | local mesh/marker cleanup and status colors | real `Piece` creation |
| CatosBuildSight adapter | optional measurements/guides/status DTOs | hologram rendering or build requests |
| Local plan store | vanilla-server local records | claiming persistence or server visibility |
| Guided build service | vanilla native-preview guidance/optional assist | packet fabrication or client-side inventory writes |

### Blueprint record contract

The exact serialization is TBD after assembly and network inspection, but the
record must be detached and versioned:

```text
BlueprintRecord
  BlueprintId             server-generated opaque ID
  OwnerPlayerId           server-authenticated stable identity
  PieceTypeId             server-resolved stable piece identifier/hash
  Variant/RotationState   only if required by native piece definition
  Position, Rotation      quantized finite canonical transform
  State                   Planned, Queued, Building, Built, Cancelled, Failed
  SupportState            Supported, Unsupported, Blocked, Unknown
  BlockReason             bounded enum, not arbitrary client text
  Revision                monotonic server revision
  CreatedAt/UpdatedAt     server time or world tick
```

Costs are deliberately not stored as client-provided blueprint authority. The
server resolves the current native recipe at build time and may cache a
versioned display summary only for UI.

### BuildSight integration contract

The optional client contract should expose detached values such as:

```text
BuildHologramPreview
  Active, PreviewToken, PieceTypeId, CanonicalPosition, CanonicalRotation
  ServerValidationState, SupportState, BlueprintId?, Revision?

BuildHologramStatus
  BlueprintId, Revision, SupportState, BuildState, BlockReason
```

BuildSight consumes these values to align its measurement/guides with the same
canonical preview. In enhanced mode it may display server status; in vanilla
mode it must display predicted/local status. It must not infer that a green
client preview is server-green, and it must discard stale revisions.

## 5. Data, lifecycle, and failure handling

### 5.1 State model

Blueprint planning and support are separate dimensions:

```text
Record: Planned -> Queued -> Building -> Built
                     |          |
                     v          v
                  Failed     Planned/Blocked

Support: Supported (green)
       | Unsupported (red)
       | Blocked (amber)
       | Unknown/Stale (gray)
```

`Building` is held under a server lock and must not be concurrently cancelled,
rebuilt, or processed by another session. A failed native transaction must
choose a documented recovery state based on evidence; it must not blindly retry
after an ambiguous inventory mutation.

Client-only mode has a separate local lifecycle:

```text
LocalPlanned -> LocalSelected -> LocalNativeAttempt -> LocalBuilt/LocalRejected
                                      |                    |
                                      v                    v
                                  LocalPaused         LocalPlanned
```

There is no server lock or authoritative status in this mode. A local record is
removed only after the client observes the corresponding real native piece.

### 5.2 Dependency ordering

The planner must:

1. Build a bounded graph from blueprint records and verified nearby existing
   support.
2. Identify roots that are natively grounded or otherwise server-confirmed
   supported.
3. Choose deterministic ready pieces using stable tie-breakers such as depth,
   blueprint creation order, and opaque ID.
4. Recompute after each real piece is confirmed because one build can support
   several dependents or invalidate another.
5. Stop with a diagnostic when a cycle, disconnected component, stale record,
   or ambiguous support chain cannot be resolved.

The client may preview an order for usability, but the server's order is the
only order that can build.

### 5.2.1 Test-harness utility

The local launchers may copy the existing
`JereKuusela-Server_devcommands\ServerDevcommands.dll` plugin folder from the
selected `CatosBuildHologram` r2modman profile to the dedicated server. This
is test-harness support only; it is not part of the Hologram product, protocol,
dependency graph, or release package.

### 5.3 Inventory and transaction rules

- Resolve exact native cost from the server-side piece definition at the moment
  of build.
- Verify the authenticated builder's inventory, tool, workbench, ward, range,
  and other native conditions immediately before the transaction.
- Prefer the native atomic construction path if it exposes a safe server-side
  boundary.
- If native construction and material consumption are separate operations,
  prove a lock/rollback/compensation strategy before enabling autobuild.
- Confirm both inventory outcome and real-piece existence before marking the
  blueprint built or advancing the queue.
- A timeout or disconnect during an ambiguous transaction pauses the session
  and requires reconciliation; it must not automatically charge again.

### 5.4 Persistence and restart

- Persist blueprint records using a native/plugin-supported save lifecycle;
  direct editing of `.db2`, `.fwl2`, or other Valheim save files is forbidden.
- Include a schema version and migration path before persistence is released.
- Save atomically and bound total record count and payload size.
- On load, validate piece identity, finite transform, world identity, and
  record ownership/state. Quarantine malformed records with a server log.
- Detect an already-real piece before retaining a duplicate blueprint.
- Restore `Building` records to `Queued` or `Blocked`, never to an implicitly
  executing state.
- Broadcast a fresh full/delta snapshot after load and after world changes.
- Client-only plans may use a separate bounded local file/config namespace, but
  must be clearly identified as local and must never be written into Valheim's
  world save or treated as server records.

### 5.5 Network lifecycle

- Negotiate a protocol version and feature flags on client connection.
- Use server-generated IDs, monotonic revisions, request IDs, bounded payloads,
  and idempotent request handling.
- Send deltas when possible, but provide a bounded resync path after packet loss
  or revision gaps.
- Remove a disconnected client's transient session state and locks.
- A client with no companion receives normal server behavior only if that
  compatibility mode is explicitly verified; otherwise the server refuses the
  blueprint feature for that connection with a clear reason.

## 6. Configuration, permissions, and integrations

### Proposed server configuration

```text
[Server]
Enabled = true
RequireClientCompanionForEnhancedMode = true
AllowVanillaClientOnlyConnections = true
ProtocolVersion = 1
MaxBlueprintsPerWorld = 2000
MaxBlueprintsPerPlayer = 250
MaxBlueprintsPerRequest = 32

[Blueprints]
AllowUnsupportedPlanning = true
PlacementRangeMeters = 8
StatusRefreshIntervalMs = 250
MaxSupportCandidates = 128
PersistBlueprints = true

[AutoBuild]
Enabled = true
RequireHammer = true
RequireNativeBuildContext = true
BuilderLeashMeters = 12
PieceIntervalMs = 250
MaxPiecesPerMinute = 120
MaxQueuePerPlayer = 250
PauseOnMissingMaterials = true
PauseOnRangeLoss = true

[Permissions]
OwnerMayBuild = true
OwnerMayCancel = true
AllowServerOperators = false

[ClientOnly]
Enabled = true
InterceptNativeConfirm = true
PersistLocalPlans = true
MaxLocalPlans = 250
ShowPredictedSupport = true
GuidedBuildEnabled = true
NativeInputAssistEnabled = false
RequireManualConfirm = true
PauseOnNativeRejection = true
```

Defaults are proposed and must be tuned after runtime tests. Safer defaults
are preferred: require the client companion, allow red planning, require the
native tool/context, pause on every uncertainty, and keep queues/rates capped.

### Request and permission contract

Requests may contain a request ID, operation, blueprint IDs, expected
revisions, and the current client protocol version. The server derives the
requesting player from the authenticated connection. It checks ownership or
server-authoritative permission, range, live state, and current world rules on
every mutating operation.

Client-only mode sends no blueprint-management or autobuild authority request
to the server. Its guided queue only drives the ordinary local native preview
and, if the separate assist gate passes, the ordinary native build action. It
must never fabricate a build packet, directly call a server-only authority
method, or mutate the client's inventory to simulate success.

### BuildSight behavior

When CatosBuildSight is present on the same client:

- Hologram supplies the canonical preview token, transform, blueprint ID,
  revision, and server status through the optional contract.
- BuildSight displays measurements and alignment guides around that exact
  transform and may show `Server: Green/Red/Blocked/Stale`.
- BuildSight does not render a second hologram, recalculate authoritative
  support, or send autobuild requests.
- If the contract is absent or mismatched, Hologram continues with its own UI
  and BuildSight falls back to its normal native-preview mode.

## 7. Safety, security, and product constraints

- **Packet trust:** reject forged owner IDs, costs, piece IDs, transforms,
  support states, blueprint IDs, revisions, and completion claims.
- **Placement validation:** resolve piece identity from a server allowlist/native
  table; reject NaN/infinite values, invalid quaternions, extreme coordinates,
  malformed variants, and transforms outside the allowed player/build range.
- **Economy integrity:** inventory checks and material consumption occur only
  on the server. No client preview or failed request may charge materials.
- **Dupe prevention:** use per-blueprint and per-player locks, request
  idempotency, revision checks, and post-build reconciliation.
- **Build ordering:** never trust client order. Revalidate support before every
  piece and pause if the graph is ambiguous.
- **Grief controls:** cap records, queue length, status broadcasts, placement
  rate, and plan size. Enforce ownership/permission and safe cancellation.
- **Native rules:** preserve ward, workbench, range, collision, tool, stamina,
  piece, and stability rules unless a specific deviation is separately designed
  and verified. The MVP has no deviations.
- **Persistence integrity:** validate schema and world identity; quarantine
  malformed records; never directly rewrite world save databases.
- **Client safety:** never instantiate holograms as real network Pieces, never
  let a client status change server state, and clean up all renderers on unload.
- **Failure closed:** if native signatures, transaction semantics, or protocol
  negotiation are uncertain, disable the affected operation and preserve the
  blueprint rather than guessing.
- **Auditability:** retain bounded server audit events for blueprint create,
  cancel, build success, pause, failure, and reconciliation without logging
  credentials or excessive player data.

## 8. Verification matrix

| Scenario | Expected result | Evidence required |
|---|---|---|
| Server role starts | Server artifact loads on `valheim_server.exe` without client renderer errors | Server log and launcher output |
| Client role starts | Client artifact loads on `valheim.exe` and negotiates protocol | Client/server logs |
| Vanilla server with client-only mode | Local planning works without server plugin; server sees no blueprint records or protocol errors | Clean vanilla-server test, client/server logs |
| Client without companion connects to enhanced server | Behavior follows explicitly tested fallback/refusal policy; no server exception | Clean-client connection test |
| Valid preview confirmation in enhanced mode | Server creates one canonical blueprint; no real piece or material is consumed | Logs, inventory comparison, world inspection |
| Valid preview confirmation in vanilla mode | Only the local client stores a plan/hologram; server world and inventory remain unchanged | Client-only test, server log, inventory/world comparison |
| Invalid prefab/transform/request | Server rejects safely; no record, build, or item change | Server audit/log and packet test |
| Snap placement | Hologram uses canonical server-accepted snapped transform | Multi-piece manual test and screenshots |
| Supported foundation/root in enhanced mode | Hologram is server-green and eligible after fresh validation | Manual support test and server status log |
| Unsupported elevated piece in enhanced mode | Hologram is server-red; autobuild skips it and consumes nothing | Manual test and inventory evidence |
| Unsupported/elevated piece in vanilla mode | Hologram says predicted/unavailable; native assist does not claim a guarantee | Client-only manual test |
| Missing materials/tool/range | Hologram is amber/blocked; autobuild pauses without item loss | Manual test and server log |
| Unknown/stale status | Hologram is gray or refreshes; it is never built blindly | Revision-gap/resync test |
| Ordered structure build in enhanced mode | Supported roots build first; dependents update and build in safe order | Server event timeline plus manual structure test |
| Guided/local build in vanilla mode | Player confirms ordinary native actions; a rejection leaves the local plan and consumes nothing extra | Manual test and client/server logs |
| Dependency cycle/disconnected plan | Session pauses with actionable reason; no infinite retry | Server log and manual note |
| Build transaction success | Exact native cost is charged once and one real piece replaces one blueprint | Inventory/piece/revision evidence |
| Ambiguous native failure | Session pauses/quarantines; no duplicate charge or blind retry | Fault-injection or controlled failure evidence |
| Duplicate request/retry | Idempotent response; no duplicate blueprint, build, or charge | Protocol test/log |
| Concurrent builders | Locks and ownership prevent double build or double spend | Multiplayer race test |
| Disconnect/reconnect | Session pauses and recovers without replay; records resync | Client/server logs and manual test |
| Server restart/save reload | Records persist, malformed records quarantine, in-progress work does not auto-run | Save/load evidence and logs |
| Blueprint cancel/permission change | Authorized operation works; unauthorized operation is rejected | Multiplayer permission test |
| Quota/rate limits | Excess records, requests, and build rate are refused or paused | Server audit/log |
| CatosBuildSight installed | Measurements/guides align to the same preview; no duplicate hologram or build request | Clean compatibility test |
| CatosBuildSight absent | Hologram feature remains usable with its own UI | Client-only companion test |
| Plugin shutdown | Locks, subscriptions, renderers, and transient queues clean up safely | Logs and manual shutdown test |
| Mutation audit | No unauthorized inventory/world/RPC behavior beyond validated builds | Source audit, logs, and before/after world/inventory checks |

## 9. Phased checklist

### Phase 0 — Native, authority, and protocol discovery

- [x] Confirm repository state and applicable parent instructions.
- [x] Refresh client managed and BepInEx references; inspect the dedicated
  server managed assembly separately so unlike client/server DLLs are not
  mixed into `lib/`.
- [x] Inspect native build preview, piece identity, placement confirmation,
  stability/support, inventory cost, save lifecycle, and networking signatures
  against installed assemblies. Runtime support and transaction verification
  remain explicit follow-up gates.
- [x] Determine whether one package can safely expose separate client/server
  entry points or whether separate artifacts are mandatory. The current
  decision is role-separated artifacts with process guards.
- [ ] Determine a native/transaction-safe path for “build real piece and charge
  exact materials”; metadata confirms the native order but not atomicity, so
  this remains a release-blocking runtime investigation.
- [x] Define protocol version, request IDs, revision semantics, bounded message
  sizes, and CatosBuildSight contract fields in [`protocol.md`](protocol.md).
- [x] Record all currently known signature differences and unresolved
  support/transaction semantics in this plan before implementation.
- [x] **Verify:** assembly notes, transaction decision, protocol document, and
  deployment-role decision exist; the documentation artifacts exist, but the
  transaction safety and gameplay/runtime portions are intentionally not
  complete.

### Phase 1 — Role-separated plugin skeleton

- [x] Create shared contracts with bounded payload limits and no Unity object
  dependencies.
- [x] Create server entry point and client entry point with process guards and
  independent lifecycle handling. Runtime load verification remains pending.
- [x] Add server/client configuration with safe defaults and protocol
  negotiation scaffolding.
- [x] Add authenticated request routing, request IDs, revisions, duplicate
  request rejection, and isolated transport-boundary scaffolding.
- [x] Add reference/build/package scripts that distinguish server and client
  destinations.
- [x] Add an explicit client-only mode that can run without the server artifact,
  with bounded detached local-plan storage and predicted-status labels disabled
  by default where native evidence is insufficient.

### Phase 2 — Blueprint records and native placement interception

- [x] Implement the client native-preview controller and opt-in blueprint-mode
  configuration seam. Visual UX remains a Phase 3 responsibility.
- [x] Implement bounded detached local plan records for vanilla-server mode,
  clearly separate from server-owned records.
- [x] Implement guided native placement that restores one local plan at a time
  and removes it only after observing a matching confirmed real piece. Manual
  in-game confirmation remains pending.
- [x] Investigate the optional native-input assist boundary; it remains disabled
  until ordinary-native-input and safety tests pass.
- [x] Add bounded detached server validation/repository scaffolding with owner,
  transform, state, and quota checks. Native authentication, canonicalization,
  persistence, and transport integration remain pending.
- [ ] Implement server validation of authenticated player, piece identity,
  canonical transform, range, permissions, quotas, and native placement rules.
- [ ] Create server-owned detached blueprint records with revisions and reason
  codes.
- [x] Implement safe atomic persistence for detached client-only plan records,
  including schema/load validation and fail-closed malformed data handling.
  Server-owned persistence and in-progress recovery remain pending.
- [ ] Implement bounded full/delta synchronization and revision-gap resync.
- [ ] **Verify:** confirmed previews become one persistent blueprint with no
  real piece/material mutation in enhanced mode; vanilla mode stores only local
  data and still leaves the server unchanged; reconnect and restart recover
  each mode according to its own persistence contract. Static build/package
  checks pass; runtime interception, persistence, and gameplay evidence remain
  pending.

### Phase 3 — Hologram rendering and server status colors

- [x] Implement a bounded client hologram renderer from detached blueprint
  records, using local native piece prefabs without creating fake network
  Pieces. The canonical server-record feed remains pending.
- [x] Implement green/red/amber/gray status-color mapping and a bounded HUD
  status surface. Server reason/status authority remains pending.
- [ ] Implement status refresh/revision handling and stale-state behavior.
- [x] Implement local cleanup for removed/completed records and plugin
  shutdown, with renderer failure isolation at the per-view boundary. Full
  disconnect, scene-transition, and renderer-failure lifecycle verification
  remains pending.
- [x] Implement explicit client-only/local predicted-mode labelling and
  local-only rendering when no server protocol is available.
- [ ] **Verify:** enhanced mode shows the same canonical transform/status to
  multiple clients; vanilla mode remains local-only; red/unknown holograms are
  not assisted automatically and no stale mesh remains.

### Phase 4 — Native support and buildability resolver

- [ ] Implement server-side support graph discovery using verified native
  stability semantics and bounded nearby candidates.
- [ ] Define the exact supported/unsupported/blocked/unknown reason taxonomy.
- [ ] Recompute affected records after a blueprint change, real build, real
  world change, reconnect, and periodic refresh.
- [ ] Require server confirmation for green; default uncertain cases to gray or
  amber according to the documented reason.
- [ ] **Verify:** grounded, chained, unsupported, blocked, rotated, collision,
  and high-density structures produce status results that match native behavior.

### Phase 5 — Dependency planner and safe autobuild

- [ ] Implement authenticated autobuild session creation with ownership,
  permissions, builder leash, tool/context, plan-size, and queue checks.
- [ ] Implement deterministic support-root discovery and dependency ordering.
- [ ] Implement per-blueprint/per-player locks and one-piece-at-a-time state.
- [ ] Implement exact server-side cost resolution and the proven atomic or
  compensating inventory/build transaction boundary.
- [ ] Revalidate support, transform, permissions, range, tool, collision, and
  materials immediately before every build.
- [ ] Confirm real-piece creation and inventory outcome before marking one
  blueprint built and advancing the queue.
- [ ] Pause safely on missing materials, range loss, disconnect, changed
  support, native failure, ambiguity, cycles, and stale revisions.
- [ ] Implement cancel/resume behavior without refund or duplicate charge.
- [ ] **Verify:** build a multi-level structure and demonstrate roots before
  dependents, exact one-time costs, safe pauses, retry idempotency, and no
  broken pieces from order mistakes.

### Phase 6 — CatosBuildSight seamless integration

- [ ] Implement the optional client adapter using the agreed versioned shared
  contract or reflection-safe boundary.
- [ ] Expose canonical hologram preview token, transform, blueprint ID,
  revision, support/build state, and reason without exposing live references.
- [ ] Make BuildSight measurements and guides attach to the same canonical
  preview without duplicating hologram rendering.
- [ ] Ensure BuildSight never overrides server status or issues autobuild
  requests, and ensure Hologram does not require BuildSight to function.
- [ ] Test absent, disabled, compatible, mismatched, and load-order variants.
- [ ] **Verify:** both mods work seamlessly on a clean client/server profile;
  status, measurements, guides, and cleanup remain consistent in both vanilla
  client-only mode and enhanced server mode.

### Phase 7 — Security, performance, and release verification

- [ ] Add packet fuzz/invalid-input, duplicate-request, concurrent-builder,
  quota, and malformed-persistence tests.
- [ ] Profile status updates, support graph work, network payloads, renderer
  counts, and autobuild rate under a large bounded plan.
- [ ] Audit all server writes to inventory/world/network state and all client
  Harmony patches for unintended mutation.
- [ ] Complete the shared launcher checks for executable, references, world,
  profile, BepInEx freshness, build output, and role-specific deployment.
- [ ] Verify both launchers source `JereKuusela-Server_devcommands` from the
  selected r2modman profile and deploy it only to the dedicated server test
  plugin directory.
- [ ] Run the full verification matrix with logs and manual notes retained.
- [ ] Produce a release package with explicit server/client artifacts and no
  runtime state or world data.
- [ ] **Verify:** clean client/server smoke test, failure-path checks,
  persistence/restart checks, multiplayer race checks, and BuildSight
  integration all pass before release.

## 10. Open tuning points

- Exact native interception point for replacing confirm-build with blueprint
  creation. **Owner:** phase 0.
- Whether a client without the companion can use local blueprint mode while
  connected to the server, or whether native building must remain untouched.
  **Owner:** phase 0/2; must be decided from native confirmation behavior.
- Native persistence mechanism and migration format. **Owner:** phase 0/2.
- Native support semantics available before a real piece exists. **Owner:**
  phase 0/4; determines status confidence.
- Atomicity of native build plus inventory consumption. **Owner:** phase 0/5;
  this is a release blocker, not a cosmetic tuning point.
- Whether vanilla-server local mode can safely provide native-input assist or
  must remain manual-confirmation guided mode. **Owner:** phase 0/2; never
  solve uncertainty with packet fabrication.
- Builder leash, required tool/context, queue scope, and whether a plan can
  span disconnected areas. **Owner:** multiplayer usability testing.
- Whether blueprint placement should require workbench/ward checks even though
  it consumes no materials. **Owner:** server safety review before Phase 2.
- Exact shared contract packaging for CatosBuildSight. **Owner:** phase 0/6.
- Status colors, opacity, marker density, refresh rate, quotas, and build rate.
  **Owner:** performance and usability testing after the MVP.
