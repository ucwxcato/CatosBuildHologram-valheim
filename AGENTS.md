# CatosBuildHologram development

## Scope and source of truth

CatosBuildHologram is a dual-mode Valheim/BepInEx blueprint and ordered-build
system. Its vanilla-compatible client-only mode works without server changes;
its enhanced server-authoritative mode adds shared persistence, multiplayer
visibility, and authoritative autobuild. The canonical behavior, network
contract, persistence rules, and delivery gates are in
[DOCS/devplan.md](DOCS/devplan.md).
The sibling client-only display companion is
[`../CatosBuildSight/DOCS/devplan.md`](../CatosBuildSight/DOCS/devplan.md).
Shared Valheim setup rules are in [`../world-setup.md`](../world-setup.md).

The repository now contains the Phase 2 client-only interception skeleton.
Do not claim that blueprint visuals, persistence, networking, installation, or
gameplay is complete until the corresponding runtime evidence, network tests,
persistence checks, and manual gameplay tests exist. A successful build is not
gameplay verification.

## Product and authority boundaries

The product has two deliberately different authority tiers:

- **Vanilla-server client-only mode:** local holograms and local plans only.
  Support colors are predictions, other players cannot see the plan, and the
  server remains fully authoritative for every real build. The safe default
  build experience is guided native placement; any optional native-input
  assist must use ordinary native build actions and stop on uncertainty.
- **Server-authoritative mode:** requires the server role and client companion.
  The server owns shared blueprint records, status colors, ordering, and the
  material/build transaction. This is the only mode that can promise shared
  holograms or authoritative “green means safe” status.

- In server-authoritative mode, the server is authoritative for blueprint
  creation, identity, transform, support/buildability status, ownership,
  permissions, material costs, inventory consumption, construction order, and
  real-piece creation.
- Full shared functionality requires the server artifact on the dedicated
  server and the client companion on connecting clients. Package them as
  explicit server/client roles; never assume a client DLL is a server plugin.
- The client must never be trusted for piece IDs, transforms, support colors,
  material costs, ownership, permissions, or build completion.
- In server-authoritative mode, blueprint placement consumes no build materials,
  but it creates persistent server-owned blueprint state and therefore requires
  server validation, ownership, quotas, and cleanup rules. In client-only mode,
  the equivalent record is local-only and must never be presented as shared.
- Red or unknown holograms may be retained for planning, but autobuild must
  build only a freshly server-validated supported piece.
- In server-authoritative mode, green means the server's current validation says
  the piece is supported and buildable. In client-only mode, the label must say
  `Predicted` because it cannot guarantee server acceptance or stability.
- Autobuild is one-piece-at-a-time, server-paced, dependency-aware, and
  revalidated immediately before construction. It must pause when support,
  range, permissions, tools, materials, or native validity fail.
- Never bypass native placement, stability, permission, ward, collision, or
  inventory rules merely to make a hologram build.
- Do not use fake network Pieces as an authority shortcut. Holograms are
  client-rendered representations of server-owned detached blueprint records.
- Never send a gameplay build RPC from the client without server validation and
  an idempotent request path.

## Phase 0 native findings

The installed client assembly metadata confirms the native preview boundaries:
`Player.m_placementGhost`, `Player.m_placementStatus`,
`Player.UpdatePlacementGhost(bool)`, `Player.GetPlacementStatus()`,
`Player.TryPlacePiece(Piece)`, `Player.PlacePiece(...)`, and
`Player.FindClosestSnapPoints(...)`. `Piece.GetSnapPoints(...)` supplies native
snap transforms. `WearNTear.GetSupport()`, `HaveSupport()`, and
`GetSupportColorValue()` are available for real-piece stability inspection.

The normal native flow creates the real piece in `PlacePiece` after
`TryPlacePiece` validates `m_placementStatus`, then `Player.UpdatePlacement`
calls `ConsumeResources(...)`. This split is not an atomic transaction. Treat
server autobuild as disabled until runtime evidence proves a safe rollback or
equivalent no-loss/no-duplication transaction. Never call `PlacePiece` from a
client packet as an authority shortcut.

The client assembly contains 1,311 types and the dedicated-server assembly
contains 1,312; their `assembly_valheim.dll` files also differ in size. Use
role-specific references/build checks and revalidate both processes before
release. The protocol and BuildSight detached interop contract are defined in
`DOCS/protocol.md`.

## Phase 2 native interception boundary

The client patch targets `Player.TryPlacePiece(Piece)` only. When explicitly
enabled, it accepts only the already-native `PlacementStatus.Valid` preview,
copies the piece name and transform into a detached local record, and returns
`false` from the prefix so Valheim does not instantiate the real piece. The
verified native caller then skips its `ConsumeResources` and build-stamina
branch for that attempt. The patch is disabled by default, does nothing in
server-authoritative mode until a real request transport exists, and must never
write inventory, world, or network state itself.

Do not broaden this patch to `PlacePiece`, `ConsumeResources`, or global input
handlers without a new native-semantics review. A runtime test must confirm
one click creates one local record, creates no real piece, consumes no material
or stamina, and leaves ordinary placement unchanged when the config is off.

## Client/server artifact boundaries

Prefer a package with explicit role-specific entry points:

```text
CatosBuildHologram.Server.dll   dedicated-server authority and persistence
CatosBuildHologram.Client.dll   native preview, input, rendering, UI
CatosBuildContracts.dll         small versioned DTO/API surface, if needed
```

The Phase 1 packaging uses separate client and server entry-point assemblies
plus `CatosBuildContracts.dll`. Shared code must not cause the client renderer
to load server-only types or cause the server to require Unity presentation
objects. Process guards and role isolation still require runtime verification
before release.

## CatosBuildSight integration

CatosBuildSight remains useful without this mod and must remain client-only.
When both are present:

- CatosBuildHologram owns hologram rendering, server status, blueprint records,
  and autobuild UI/state.
- CatosBuildSight owns measurements, alignment guides, and construction-facing
  readouts; it must not duplicate hologram meshes or issue build requests.
- The client integration is optional and versioned. Prefer a tiny shared
  contracts/API assembly or a reflection-safe adapter; never create a hard
  dependency that prevents either plugin from loading alone.
- BuildSight may display server support status and blueprint revision, but must
  not replace a server status with a local guess.
- A missing, disabled, or incompatible BuildSight client degrades Hologram to
  its own bounded UI and does not affect server correctness.
- On a vanilla server, BuildSight consumes the local preview/prediction path
  and must label support as predicted. With the server role, it consumes the
  authoritative status and revision.
- Compatibility is not considered proven until both plugins are tested on a
  clean client against the matching dedicated server.

## Runtime, trust, and persistence rules

- Reconstruct and validate all build costs and native piece identity on the
  server. Never accept client-supplied item lists or cost values.
- Authenticate requests from the server connection/player object, not a
  SteamID or owner value supplied in a packet.
- Validate finite, normalized, bounded transforms and reject malformed or
  out-of-range requests before persistence.
- Use request IDs, expected revisions, per-blueprint locks, and per-player
  queues to prevent duplicate placement, double spending, and race conditions.
- Consume materials only inside a verified native/transaction-safe build path.
  If a failure can consume materials without a confirmed real piece, disable
  autobuild until an atomic or compensating path is proven.
- Persist detached blueprint records through a safe native/plugin persistence
  mechanism. Do not edit Valheim save files directly or retain live Unity
  references across saves/reloads.
- Cap blueprints per world/player, record size, queue size, build rate,
  network payloads, and status broadcasts.
- On restart, recover records idempotently: real pieces win over stale blueprint
  records, in-progress work returns to queued/blocked, and no materials are
  silently consumed during recovery.
- Do not read the test admin list as a client permission source. Server-side
  permissions must use the server's authoritative mechanism.
- Client-only mode must not pretend that local blueprint storage is shared
  persistence. It may save detached local plan data, but it must never rewrite
  Valheim world files or claim that vanilla clients can see those records.

## Build and test rules

Use refreshed references from the installed client and selected client profile.
Do not commit game/BepInEx DLLs under `lib/`; exclude `bin/`, `obj/`, logs,
worlds, credentials, and runtime state from release packages.

Use the shared local test-server contract from
`C:\Users\magni\Documents\BotsnCoding\Valheim\world-setup.md`. The test world
source is `C:\Users\magni\Documents\BotsnCoding\Valheim\Dedicated`; use the
save root, `worlds_local\Dedicated` junction, port, and launch arguments
specified by that document. Verify the client-only mode against an otherwise
vanilla server, then verify the enhanced server role separately: the server
artifact must load on
`valheim_server.exe`, and the client artifact must load on `valheim.exe`. Test
clients with and without BuildSight, vanilla clients where supported, and a
clean client/server smoke path before publishing.

The launchers use `JereKuusela-Server_devcommands` from the selected profile
as a test-harness utility and copy it to the dedicated server for both modes.
It is not a CatosBuildHologram dependency and must not be included in the
release package.

Retain build output, launcher output, client/server BepInEx logs, persistence
evidence, packet/error diagnostics, and manual test notes. Never mark a plan
checkbox complete because code exists or because a packet was observed; the
stated runtime and safety evidence must exist.

## Change discipline

- Keep changes scoped to the current phase in `DOCS/devplan.md`.
- Preserve existing user changes and do not modify the shared source world.
- Do not add unrelated automation, economy features, commands, or admin tools.
- If native build, stability, inventory, save, or networking signatures differ
  from the plan, update the adapter/TBD and acceptance criteria before coding.
- A failed safety gate blocks the affected feature; it is not permission to
  silently fall back to client-authoritative behavior.
