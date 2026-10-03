# INT-004 Throw Carried Object Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow the player to throw a carried physics object in the camera look direction with tunable impulse force, ensuring heavy objects (e.g. 25 kg cement bag) naturally travel much shorter than light objects (1 kg), and guarding left-mouse input so that clicking to re-lock the cursor never triggers a throw.

**Architecture:** Extend `PlayerMovement` with cursor re-lock frame detection, and extend `PlayerGrab` with separated intent/execution methods (`RequestThrow` and `ExecuteThrow`) applying a physical impulse (`ForceMode.Impulse`) in the camera's forward direction while inheriting horizontal player velocity and reusing the established detachment and depenetration pipeline.

**Tech Stack:** Unity 6000.3.25f1 LTS, Unity Input System 1.20.0, PhysX, NUnit / Unity Test Framework (`InputTestFixture`, `UnityPlatform PlayMode`).

**Spec:** [GitHub Issue #12 ([INT-004] Throw carried object)](https://github.com/GhazaGG/Ngecor/issues/12), TL PR #50 review note on cursor re-lock, and [Keputusan Proyek (`docs/DECISIONS.md`)](file:///D:/coding/Ngecor/docs/DECISIONS.md).

## Global Constraints

- **Unity Version & Engine:** Unity 6000.3.25f1 LTS (Unity 6.3 LTS). No new packages or dependencies.
- **Input System:** Input System package (`com.unity.inputsystem`). Keyboard + Mouse only. Throw binding maps to Left Mouse Button (`Attack` action).
- **Separation of Intent & Execution:** Split into `RequestThrow()` (local intent validation) and `ExecuteThrow()` (world execution), offline for M1, preparing for host-authoritative routing in `NET-002`/`NET-003`.
- **No Text Editing of Serialized Assets:** Do not edit `.prefab`, `.asset`, `.unity`, or `.meta` files using text editors ([`AGENTS.md`](file:///D:/coding/Ngecor/AGENTS.md) Rule 4). Expose `[SerializeField]` fields with fallback resolution.
- **Performance Budget:** Zero per-frame heap allocations in `Update`/`LateUpdate` (`docs/DECISIONS.md#anggaran-performa`).
- **Physics Reality:** Use standard impulse physics (`ForceMode.Impulse`) so that $v = \frac{J}{m}$ naturally makes 25 kg cement bags feel heavy without hardcoded mass logic.

## Review Focus

1. **Cursor Re-lock Guard:** Left-clicking while the cursor is unlocked (e.g., after pressing Escape) locks the cursor back, but MUST NOT trigger a throw.
2. **Empty-Hand Safety:** Calling `RequestThrow()` or `ExecuteThrow()` while hands are empty returns `false` with zero errors or side-effects.
3. **Mass-Dependent Distance:** Applying the same throw force to a 25 kg object produces $\approx 1/25$ of the velocity imparted to a 1 kg object, making the cement bag plop down close by.
4. **Velocity Inheritance:** Throwing while moving horizontally inherits player movement velocity along with the forward throw impulse.
5. **Post-Throw Physics & Collision:** After throwing, the object's `isKinematic` becomes `false`, `useGravity` becomes `true`, and player collision is properly re-enabled without immediate pinch-launching.

---

### Task 1: Expose Cursor Lock State and Re-lock Frame Guard in `PlayerMovement`

**Files:**
- Modify: [`Assets/Game/Scripts/Player/PlayerMovement.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Player/PlayerMovement.cs)
- Test: [`Assets/Game/Tests/Player/PlayerMovementTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Player/PlayerMovementTests.cs)

**Interfaces:**
- Consumes: `_cursorLocked` state, `Mouse.current.leftButton`, `Keyboard.current.escapeKey`.
- Produces:
  - `public bool IsCursorLocked => _cursorLocked;`
  - `public bool CursorRelockedThisFrame { get; private set; }`

- [ ] **Step 1: Write the failing tests in `PlayerMovementTests.cs`**

Add tests verifying that:
1. `IsCursorLocked` reflects the internal `_cursorLocked` status.
2. When the cursor is unlocked and left-click is pressed, `CursorRelockedThisFrame` is `true` during that frame and resets to `false` in the next frame.

```csharp
[UnityTest]
public IEnumerator CursorRelock_SetsCursorRelockedThisFrame_ForSingleFrame()
{
    _movement.SetLocalPlayer(true);
    yield return null;
    Assert.IsTrue(_movement.IsCursorLocked);

    Press(Keyboard.current.escapeKey);
    yield return null;
    Assert.IsFalse(_movement.IsCursorLocked);

    Press(Mouse.current.leftButton);
    yield return null;

    Assert.IsTrue(_movement.IsCursorLocked);
    Assert.IsTrue(_movement.CursorRelockedThisFrame);

    yield return null;
    Assert.IsFalse(_movement.CursorRelockedThisFrame);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: Unity PlayMode batchmode test for `Ngecor.Player.Tests`.
Expected: Compilation failure or assertion failure because `IsCursorLocked` / `CursorRelockedThisFrame` do not exist.

- [ ] **Step 3: Implement cursor state properties in `PlayerMovement.cs`**

In [`PlayerMovement.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Player/PlayerMovement.cs):
- Add `public bool IsCursorLocked => _cursorLocked;`
- Add `public bool CursorRelockedThisFrame { get; private set; }`
- At the start of `Update()`, reset `CursorRelockedThisFrame = false;`
- In `HandleCursorInput()`, when `!_cursorLocked` and LMB was pressed:
  ```csharp
  SetCursorLocked(true);
  CursorRelockedThisFrame = true;
  ```

- [ ] **Step 4: Run test to verify it passes**

Run: Unity PlayMode batchmode test for `Ngecor.Player.Tests`.
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Game/Scripts/Player/PlayerMovement.cs Assets/Game/Tests/Player/PlayerMovementTests.cs
git commit -m "feat(player): expose cursor lock state and relock frame flag"
```

---

### Task 2: Implement Core Throw Logic and Mass Sensitivity in `PlayerGrab`

**Files:**
- Modify: [`Assets/Game/Scripts/Interaction/PlayerGrab.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Interaction/PlayerGrab.cs)
- Test: [`Assets/Game/Tests/Interaction/PlayerGrabTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Interaction/PlayerGrabTests.cs)

**Interfaces:**
- Consumes: `CarriedObject`, `DetachObject()`, `_playerMovement.LocalCamera`, `ForceMode.Impulse`.
- Produces:
  - `public float ThrowForce { get; set; }`
  - `public bool RequestThrow()`
  - `public bool ExecuteThrow()`

- [ ] **Step 1: Write the failing tests in `PlayerGrabTests.cs`**

Add tests covering:
1. `ExecuteThrow_WithCarriedObject_AppliesImpulseInLookDirection`: 1 kg object launched forward, Rigidbody restored to dynamic.
2. `ExecuteThrow_EmptyHands_ReturnsFalseAndDoesNotThrow`: returns false, no errors.
3. `ExecuteThrow_HeavyObjectVsLightObject_HeavyObjectTravelsFarShorter`: 25 kg object gets 25x less velocity change than 1 kg object.
4. `ExecuteThrow_WhileMoving_InheritsPlayerHorizontalVelocity`: verifies horizontal velocity addition.

```csharp
[UnityTest]
public IEnumerator ExecuteThrow_AppliesImpulseInLookDirection_AndRestoresPhysics()
{
    var target = CreateGrabbable("LightProp", new Vector3(0f, 1.4f, 2f));
    target.Rigidbody.mass = 1f;
    _playerGrab.ExecuteGrab(target);
    Assert.IsTrue(_playerGrab.IsCarrying);

    bool thrown = _playerGrab.ExecuteThrow();
    Assert.IsTrue(thrown);
    Assert.IsFalse(_playerGrab.IsCarrying);
    Assert.IsFalse(target.Rigidbody.isKinematic);
    Assert.IsTrue(target.Rigidbody.useGravity);

    // Forward velocity should reflect impulse / mass (default force 10 -> velocity 10)
    Assert.Greater(target.Rigidbody.linearVelocity.z, 5f);
    yield return null;
}

[UnityTest]
public IEnumerator ExecuteThrow_HeavyObject_ReceivesSignificantlyLowerVelocityThanLightObject()
{
    var light = CreateGrabbable("Light1kg", new Vector3(0f, 1.4f, 2f));
    light.Rigidbody.mass = 1f;
    _playerGrab.ExecuteGrab(light);
    _playerGrab.ExecuteThrow();
    float lightSpeed = light.Rigidbody.linearVelocity.magnitude;

    var heavy = CreateGrabbable("Heavy25kg", new Vector3(0f, 1.4f, 2f));
    heavy.Rigidbody.mass = 25f;
    _playerGrab.ExecuteGrab(heavy);
    _playerGrab.ExecuteThrow();
    float heavySpeed = heavy.Rigidbody.linearVelocity.magnitude;

    Assert.Greater(lightSpeed, heavySpeed * 15f);
    yield return null;
}

[Test]
public void ExecuteThrow_WithEmptyHands_ReturnsFalseWithoutErrors()
{
    Assert.IsFalse(_playerGrab.IsCarrying);
    bool result = _playerGrab.ExecuteThrow();
    Assert.IsFalse(result);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: Unity PlayMode batchmode test for `Ngecor.Interaction.Tests`.
Expected: FAIL with missing `ExecuteThrow` / `ThrowForce`.

- [ ] **Step 3: Implement `RequestThrow()`, `ExecuteThrow()`, and `ThrowForce` in `PlayerGrab.cs`**

In [`PlayerGrab.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Interaction/PlayerGrab.cs):
- Add `[SerializeField, Min(0f)] private float _throwForce = 10f;`
- Add property `public float ThrowForce { get => _throwForce; set => _throwForce = Mathf.Max(0f, value); }`
- Implement `RequestThrow()`:
  ```csharp
  public bool RequestThrow()
  {
      // Offline M1: Direct local execution.
      // NET-002/003: Will route throw intent to host.
      return ExecuteThrow();
  }
  ```
- Implement `ExecuteThrow()`:
  ```csharp
  public bool ExecuteThrow()
  {
      if (!IsCarrying)
          return false;

      var target = _carriedObject;
      var rb = target.Rigidbody;

      if (_playerMovement == null)
          _playerMovement = GetComponent<PlayerMovement>();

      var camera = _playerMovement != null ? _playerMovement.LocalCamera : null;
      Vector3 throwDir = camera != null ? camera.transform.forward : transform.forward;

      DetachObject();

      if (rb != null && !rb.isKinematic)
      {
          rb.AddForce(throwDir * _throwForce, ForceMode.Impulse);
      }

      return true;
  }
  ```

- [ ] **Step 4: Run test to verify it passes**

Run: Unity PlayMode batchmode test for `Ngecor.Interaction.Tests`.
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Game/Scripts/Interaction/PlayerGrab.cs Assets/Game/Tests/Interaction/PlayerGrabTests.cs
git commit -m "feat(interaction): implement RequestThrow and ExecuteThrow with impulse physics"
```

---

### Task 3: Input Binding and Cursor Re-lock Guard in `PlayerGrab.Update`

**Files:**
- Modify: [`Assets/Game/Scripts/Interaction/PlayerGrab.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Interaction/PlayerGrab.cs)
- Test: [`Assets/Game/Tests/Interaction/PlayerGrabTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Interaction/PlayerGrabTests.cs)

**Interfaces:**
- Consumes: `_playerMovement.IsCursorLocked`, `_playerMovement.CursorRelockedThisFrame`, `InputActionReference _throwAction` (Action `Attack` / LMB).
- Produces: `public InputActionReference ThrowAction { get; set; }`

- [ ] **Step 1: Write the failing tests in `PlayerGrabTests.cs`**

Add tests covering:
1. `ThrowInput_WhenCursorAlreadyLocked_TriggersThrow`: pressing LMB while carrying and cursor locked throws the object.
2. `ThrowInput_WhenClickReLocksCursor_DoesNotTriggerThrow`: pressing LMB when cursor was unlocked re-locks the cursor, but does NOT throw the carried object.

```csharp
[UnityTest]
public IEnumerator ThrowInput_WhenClickReLocksCursor_DoesNotTriggerThrow()
{
    var target = CreateGrabbable("Target", new Vector3(0f, 1.4f, 2f));
    _playerGrab.ExecuteGrab(target);
    Assert.IsTrue(_playerGrab.IsCarrying);

    // Unlock cursor via Escape
    Press(Keyboard.current.escapeKey);
    yield return null;
    Assert.IsFalse(_playerMovement.IsCursorLocked);

    // Click LMB to re-lock cursor: MUST NOT THROW
    Press(Mouse.current.leftButton);
    yield return null;

    Assert.IsTrue(_playerMovement.IsCursorLocked);
    Assert.IsTrue(_playerGrab.IsCarrying); // Still carrying!

    // Next click when already locked: SHOULD THROW
    Press(Mouse.current.leftButton);
    yield return null;

    Assert.IsFalse(_playerGrab.IsCarrying);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: Unity PlayMode batchmode test for `Ngecor.Interaction.Tests`.
Expected: FAIL because click that re-locks cursor also triggers throw.

- [ ] **Step 3: Implement input binding and cursor guard in `PlayerGrab.cs`**

In [`PlayerGrab.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Interaction/PlayerGrab.cs):
- Add `[SerializeField] private InputActionReference _throwAction;`
- Add property `public InputActionReference ThrowAction { get => _throwAction; set => _throwAction = value; }`
- In `OnEnable()`: enable `_throwAction.action` if present.
- In `Update()`:
  ```csharp
  bool canProcessThrow = _playerMovement.IsCursorLocked && !_playerMovement.CursorRelockedThisFrame;
  if (IsCarrying && canProcessThrow)
  {
      bool throwPressed = false;
      if (_throwAction != null && _throwAction.action != null)
      {
          if (!_throwAction.action.enabled)
              _throwAction.action.Enable();
          throwPressed = _throwAction.action.WasPressedThisFrame();
      }
      else if (Mouse.current != null)
      {
          throwPressed = Mouse.current.leftButton.wasPressedThisFrame;
      }

      if (throwPressed)
      {
          RequestThrow();
      }
  }
  ```

- [ ] **Step 4: Run test to verify it passes**

Run: Unity PlayMode batchmode test for `Ngecor.Interaction.Tests`.
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Game/Scripts/Interaction/PlayerGrab.cs Assets/Game/Tests/Interaction/PlayerGrabTests.cs
git commit -m "feat(interaction): add throw input handling with cursor relock guard"
```

---

### Task 4: Full Automated Verification and Development History Report

**Files:**
- Create: `.development-history/YYYY-MM-DD-HH-mm-int-004-throw-carried-object.md`
- Verify: All tests in `Ngecor.Interaction.Tests`, `Ngecor.Player.Tests`, `Ngecor.Material.Tests`.

- [ ] **Step 1: Run the full PlayMode test suite across all assemblies**

Command:
```powershell
rtk powershell -Command "& 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe' -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults PlayModeResults.xml -logfile UnityTest.log; cat PlayModeResults.xml | Select-String '<test-run|<test-suite.*result=|test-case.*result='"
```
Expected: 100% tests passed across all assemblies (target $\ge 60$ passed, 0 failed).

- [ ] **Step 2: Verify git status and check for unapproved asset changes**

Command:
```powershell
rtk git status
```
Expected: Only C# scripts, tests, and documentation changed. No `.meta`, `.unity`, or `.asset` touched without Unity Editor.

- [ ] **Step 3: Create `.development-history` report**

Write `.development-history/YYYY-MM-DD-HH-mm-int-004-throw-carried-object.md` following the required schema in [`AGENTS.md`](file:///D:/coding/Ngecor/AGENTS.md):
- Task summary
- Relevant previous context (INT-002, INT-003 PR #75, TL PR #50 review note)
- Changes made
- Files affected
- Technical decisions (Impulse physics $v=J/m$, cursor re-lock frame guard)
- Verification performed
- Final result
- Known limitations & out of scope
- Unresolved issues or follow-up work

- [ ] **Step 4: Commit documentation**

```bash
git add .development-history/
git commit -m "docs: record INT-004 throw carried object verification and history"
```

---

## Execution Handoff

Plan complete and saved to [`docs/superpowers/plans/2026-10-04-int-004-throw-carried-object.md`](file:///D:/coding/Ngecor/docs/superpowers/plans/2026-10-04-int-004-throw-carried-object.md). Please review the plan. Which execution approach would you prefer?

- **Subagent-driven** - A fresh subagent implements each task and a fresh reviewer checks it before the next one starts, then a whole-branch review at the end. Most thorough; costs a fresh context per task and per review.
- **Native** - I implement every task myself in this session, the way this harness runs work, then one fresh reviewer on the most capable model checks the whole branch. Cheapest and fastest; no independent review until the end.

**For this plan I recommend Native**, because the changes are tightly scoped across two closely coupled files (`PlayerMovement.cs` and `PlayerGrab.cs`) and can be executed quickly with immediate automated PlayMode verification in this session.

Does the plan capture what you want, and which approach should we use?
