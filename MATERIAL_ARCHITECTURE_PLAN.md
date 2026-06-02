# CDT-320 Material Architecture Plan

This document is the baseline plan for future Material-related code work.
All new Material logic should follow this structure unless the equipment
specification changes.

## Goals

- Track every material in the equipment as Cassette -> Wafer -> Die.
- Keep material state as the single source of truth for UI, sequence, logs,
  SECS, and recovery.
- Persist material state during equipment movement so the program can restore
  the last known material state after restart.
- Startup recovery must be a user choice by default. Automatic resume may be
  supported later, but should be admin-controlled and explicitly enabled.

## Cassette Rules

- Input cassettes: Input1 and Input2.
- Good output cassettes: Good1 and Good2.
- NG output cassette: Ng1.
- Input 1-level recipe: Input1 used, Input2 disabled.
- Input 2-level recipe: Input1 and Input2 both used.
- Good 1-level recipe: Good1 used, Good2 disabled.
- Good 2-level recipe: Good1 and Good2 both used.
- NG has one cassette.

## Wafer Rules

- Wafer ID is read from barcode when available.
- If barcode is not available, the equipment generates the Wafer ID.
- Wafer parameter/spec source is `MaterialSpecs.Data.Frames`
  (`TapeFrameSpec`). `WaferMaterial.TapeFrameSpecName` stores the selected
  spec name and all wafer grid/pitch/diameter values are resolved from that
  spec data.
- Input wafer, output Good wafer, and output NG wafer are separate materials.
- Good and NG output stages are separate physical locations and must be
  tracked separately.
- After input cassette mapping, each `WaferMaterial` must store its cassette
  slot position as read-only material data. Manual slot previous/next movement
  should use this saved material position data instead of recalculating the
  position from recipe pitch.

## Die Rules

- Die data must include input/output wafer IDs, input/output bin codes,
  wafer indices, bin indices, vision offsets, and inspection results.
- Inspection data must support multiple measurement values per inspection item.
- Measurement records must be addable and removable while inspection logic
  evolves.

## Persistence Rules

- Material state should be saved after meaningful material events:
  cassette scan, wafer pick/place, barcode read, alignment completion,
  die pickup/place, vision result received, output cassette unload, and lot close.
- Snapshot save must be atomic: write temp, backup previous snapshot, replace.
- Restart recovery must compare saved material state with actual sensors before
  allowing automatic production from the restored state.
- If state and sensors disagree, equipment should enter a recovery-required
  state and block automatic run.

## Implementation Direction

- Add material domain models:
  `CassetteMaterial`, `WaferMaterial`, `DieMaterial`,
  `MaterialLocation`, `MaterialSnapshot`.
- Extend `MaterialStorage` into the central in-memory state holder while keeping
  legacy `Die` / `DieTapeFrame` APIs alive.
- Add `MaterialSnapshotStore` for JSON save/load/backup.
- Add `MaterialStateService` for movement, creation, deletion, inspection
  update, and snapshot save calls.
- Material UI must read/write through `MaterialStorage.State` and
  `MaterialStateService`; grid rows are only a view over the Material data.
- Material detail UI should use reusable grid-based user controls so wafer,
  cassette, and die views share the same interaction model.
- Gradually route Unit movement code through `MaterialStateService`.
