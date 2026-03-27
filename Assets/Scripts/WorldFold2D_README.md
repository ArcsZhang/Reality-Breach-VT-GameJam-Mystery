# World Fold 2D (Runtime)

## Added scripts

- `FoldGeometry2D.cs`: Fold math, clipping, and triangulation helpers.
- `FoldableObject2D.cs`: Runtime foldable mesh/collider object.
- `FoldAbilityManager.cs`: Mouse-based fold ability controller (single active fold).

## Setup

1. Add `FoldableObject2D` to every foldable platform/object.
2. Ensure object has:
   - `MeshFilter`
   - `MeshRenderer`
   - `PolygonCollider2D` (optional but recommended)
3. In `FoldableObject2D`, set `Source Points` in local space (clockwise/counter-clockwise both okay).
4. Add `FoldAbilityManager` to an empty scene object.
5. Assign `World Camera` (or leave empty for `Camera.main`).

## Controls (default)

- Hold left mouse button: Start fold draw at press point
- Drag: Preview fold lines and folded strip region
- Release left mouse button: Apply fold
- Click unfold node: Unfold and restore geometry
- `C`: Clear fold and restore original geometry

## Notes

- Only one fold is active at a time.
- While a fold is active, creating a second fold is blocked.
- Folding is based on original source polygon points each time.
- `FoldAbilityManager` includes ability gating (`abilityInputEnabled`, `activeAbility`) for future ability integration.
