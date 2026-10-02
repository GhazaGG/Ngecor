# [INT-001] Detect Interactable Object Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a first-person raycast-based interactable object detector on the Player with tunable distance, line-of-sight occlusion checks, generic interface support, and dev debug feedback.

**Architecture:** The system consists of an `IInteractable` interface and generic `InteractableObject` component under `Ngecor.Interaction`, along with an `InteractionDetector` component attached to the Player. `InteractionDetector` casts a forward ray from the player's view (via `PlayerMovement.LocalCamera` or assigned origin), searches for `IInteractable` using `GetComponentInParent<IInteractable>()`, respects line-of-sight against solid colliders, ignores triggers, and provides simple debug visualization (`OnGUI` / `Gizmos`).

**Tech Stack:** Unity 6000.3.25f1, Universal Render Pipeline (URP), Unity Input System 1.20.0, Unity Test Framework (NUnit).

**Spec:** GitHub Issue #9 ([INT-001] Detect interactable object), `docs/DECISIONS.md`, `docs/PROJECT_STRUCTURE.md`.

## Global Constraints

- Unity 6000.3.25f1 with Mono backend; no unauthorized packages or plugins.
- Do not edit `.unity`, `.prefab`, `.asset`, or `.meta` files via text editor.
- Scripts must reside under `Assets/Game/Scripts/Interaction/` within `namespace Ngecor.Interaction`.
- Tests must reside under `Assets/Game/Tests/Interaction/` within `namespace Ngecor.Interaction.Tests`.
- No global manager/singleton, event bus, or generic interaction frameworks beyond Acceptance Criteria.
- Strictly offline implementation; no networking code in INT-001 (NGO integration reserved for NET-* issues).
- Local-player isolation: detection and debug feedback only run for the local player instance.

## Review Focus

1. **Ray hitting non-interactable solid object:** Ensure `CurrentTarget` is cleanly set to `null` with no exceptions when hitting ground or obstacles.
2. **Child colliders on composite objects:** Ensure `GetComponentInParent<IInteractable>()` is used so hitting a child collider identifies the parent interactable.
3. **Trigger colliders between player and interactable:** Ensure raycast uses `QueryTriggerInteraction.Ignore` so volume triggers don't block interaction.
4. **Disabled / Non-interactable target:** When `IInteractable.CanInteract()` returns `false`, ensure `InteractionDetector` does not register it as a valid target.
5. **Non-local player guard:** Non-local player instances must not execute camera raycasts or render debug GUI elements.

---

### Task 1: Interaction Assembly Definitions and Core Interface

**Files:**
- Create: `Assets/Game/Scripts/Interaction/Ngecor.Interaction.asmdef`
- Create: `Assets/Game/Scripts/Interaction/IInteractable.cs`
- Create: `Assets/Game/Scripts/Interaction/InteractableObject.cs`
- Create: `Assets/Game/Tests/Interaction/Ngecor.Interaction.Tests.asmdef`
- Test: `Assets/Game/Tests/Interaction/InteractableObjectTests.cs`

**Interfaces:**
- Produces: `Ngecor.Interaction.IInteractable`
  ```csharp
  public interface IInteractable
  {
      string InteractionPrompt { get; }
      bool CanInteract(GameObject interactor);
  }
  ```
- Produces: `Ngecor.Interaction.InteractableObject : MonoBehaviour, IInteractable`
  ```csharp
  public class InteractableObject : MonoBehaviour, IInteractable
  {
      public string InteractionPrompt { get; }
      public bool CanInteract(GameObject interactor);
      public void SetInteractable(bool canInteract);
      public void SetPrompt(string prompt);
  }
  ```

- [ ] **Step 1: Write the failing test for `InteractableObject`**

Create `Assets/Game/Tests/Interaction/InteractableObjectTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

namespace Ngecor.Interaction.Tests
{
    public class InteractableObjectTests
    {
        private GameObject _targetObject;

        [TearDown]
        public void TearDown()
        {
            if (_targetObject != null)
                Object.DestroyImmediate(_targetObject);
        }

        [Test]
        public void InteractableObject_DefaultValues_CanInteractAndReturnsPrompt()
        {
            _targetObject = new GameObject("TestInteractable");
            var interactable = _targetObject.AddComponent<InteractableObject>();

            Assert.That(interactable.CanInteract(null), Is.True);
            Assert.That(interactable.InteractionPrompt, Is.EqualTo("Interact"));
        }

        [Test]
        public void InteractableObject_SetInteractableFalse_CanInteractReturnsFalse()
        {
            _targetObject = new GameObject("TestInteractable");
            var interactable = _targetObject.AddComponent<InteractableObject>();
            interactable.SetInteractable(false);

            Assert.That(interactable.CanInteract(null), Is.False);
        }
    }
}
```

- [ ] **Step 2: Create Assembly Definitions**

Create `Assets/Game/Scripts/Interaction/Ngecor.Interaction.asmdef`:
```json
{
  "name": "Ngecor.Interaction",
  "rootNamespace": "Ngecor.Interaction",
  "references": [
    "Ngecor.Player",
    "Unity.InputSystem"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

Create `Assets/Game/Tests/Interaction/Ngecor.Interaction.Tests.asmdef`:
```json
{
  "name": "Ngecor.Interaction.Tests",
  "rootNamespace": "Ngecor.Interaction.Tests",
  "references": [
    "Ngecor.Interaction",
    "Ngecor.Player",
    "Unity.InputSystem",
    "Unity.InputSystem.TestFramework"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false,
  "optionalUnityReferences": [
    "TestAssemblies"
  ]
}
```

- [ ] **Step 3: Implement `IInteractable.cs` and `InteractableObject.cs`**

Create `Assets/Game/Scripts/Interaction/IInteractable.cs`:
```csharp
using UnityEngine;

namespace Ngecor.Interaction
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract(GameObject interactor);
    }
}
```

Create `Assets/Game/Scripts/Interaction/InteractableObject.cs`:
```csharp
using UnityEngine;

namespace Ngecor.Interaction
{
    [DisallowMultipleComponent]
    public class InteractableObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Interact";
        [SerializeField] private bool _canInteract = true;

        public string InteractionPrompt => _prompt;

        public bool CanInteract(GameObject interactor) => _canInteract;

        public void SetInteractable(bool canInteract) => _canInteract = canInteract;

        public void SetPrompt(string prompt) => _prompt = prompt;
    }
}
```

- [ ] **Step 4: Run Unity tests to verify it passes**

Run Unity in batch mode to execute tests:
```powershell
& "D:\Unityhub\Editor\6000.3.25f1\Editor\Unity.exe" -batchmode -projectPath "D:\orca-workspace\Ngecor\int-001-detect-interactable-object" -runTests -testPlatform playmode -testCategory "" -testResults "test_results_task1.xml" -logfile "unity_task1.log"
```
Expected: PASS (0 failures).

- [ ] **Step 5: Commit**

```bash
git add Assets/Game/Scripts/Interaction/ Assets/Game/Tests/Interaction/
git commit -m "feat: add IInteractable interface and InteractableObject component"
```

---

### Task 2: Interaction Detection Core

**Files:**
- Create: `Assets/Game/Scripts/Interaction/InteractionDetector.cs`
- Test: `Assets/Game/Tests/Interaction/InteractionDetectorTests.cs`

**Interfaces:**
- Consumes: `IInteractable`, `PlayerMovement`
- Produces: `InteractionDetector : MonoBehaviour`
  ```csharp
  public class InteractionDetector : MonoBehaviour
  {
      public IInteractable CurrentTarget { get; }
      public RaycastHit CurrentHit { get; }
      public bool HasTarget { get; }
      public float MaxDistance { get; set; }
      public void SetLocalPlayer(bool isLocalPlayer);
      public void SetOrigin(Transform originTransform);
      public IInteractable Detect();
  }
  ```

- [ ] **Step 1: Write the failing tests for `InteractionDetector`**

Create `Assets/Game/Tests/Interaction/InteractionDetectorTests.cs` covering:
1. `DetectsInteractableDirectlyInFront`
2. `IgnoresInteractableBeyondMaxDistance`
3. `BlockedBySolidObstacle`
4. `NullSafeWhenHittingNothingOrNonInteractable`
5. `DetectsParentInteractableFromChildCollider`
6. `IgnoresTriggerCollidersBetweenPlayerAndInteractable`
7. `IgnoresInteractableWhenCanInteractIsFalse`

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ngecor.Interaction.Tests
{
    public class InteractionDetectorTests
    {
        private GameObject _playerObject;
        private InteractionDetector _detector;
        private GameObject _interactableObject;
        private GameObject _obstacleObject;

        [SetUp]
        public void SetUp()
        {
            _playerObject = new GameObject("Player");
            _playerObject.transform.position = Vector3.zero;
            _playerObject.transform.forward = Vector3.forward;

            _detector = _playerObject.AddComponent<InteractionDetector>();
            _detector.SetLocalPlayer(true);
            _detector.MaxDistance = 3f;
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerObject != null) Object.DestroyImmediate(_playerObject);
            if (_interactableObject != null) Object.DestroyImmediate(_interactableObject);
            if (_obstacleObject != null) Object.DestroyImmediate(_obstacleObject);
        }

        [UnityTest]
        public IEnumerator DetectsInteractableDirectlyInFront()
        {
            _interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _interactableObject.transform.position = new Vector3(0f, 0f, 2f);
            var interactable = _interactableObject.AddComponent<InteractableObject>();

            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.True);
            Assert.That(_detector.CurrentTarget, Is.EqualTo(interactable));
        }

        [UnityTest]
        public IEnumerator IgnoresInteractableBeyondMaxDistance()
        {
            _interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _interactableObject.transform.position = new Vector3(0f, 0f, 5f);
            _interactableObject.AddComponent<InteractableObject>();

            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.False);
            Assert.That(_detector.CurrentTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator BlockedBySolidObstacle()
        {
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _obstacleObject.name = "SolidWall";
            _obstacleObject.transform.position = new Vector3(0f, 0f, 1.5f);

            _interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _interactableObject.transform.position = new Vector3(0f, 0f, 2.5f);
            _interactableObject.AddComponent<InteractableObject>();

            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.False);
            Assert.That(_detector.CurrentTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator NullSafeWhenHittingNothing()
        {
            yield return null;
            Assert.DoesNotThrow(() => _detector.Detect());
            Assert.That(_detector.HasTarget, Is.False);
            Assert.That(_detector.CurrentTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator DetectsParentInteractableFromChildCollider()
        {
            _interactableObject = new GameObject("ParentInteractable");
            _interactableObject.transform.position = new Vector3(0f, 0f, 2f);
            var interactable = _interactableObject.AddComponent<InteractableObject>();

            var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.transform.SetParent(_interactableObject.transform, false);

            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.True);
            Assert.That(_detector.CurrentTarget, Is.EqualTo(interactable));
        }

        [UnityTest]
        public IEnumerator IgnoresTriggerCollidersBetweenPlayerAndInteractable()
        {
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _obstacleObject.GetComponent<Collider>().isTrigger = true;
            _obstacleObject.transform.position = new Vector3(0f, 0f, 1.5f);

            _interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _interactableObject.transform.position = new Vector3(0f, 0f, 2.5f);
            var interactable = _interactableObject.AddComponent<InteractableObject>();

            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.True);
            Assert.That(_detector.CurrentTarget, Is.EqualTo(interactable));
        }

        [UnityTest]
        public IEnumerator IgnoresInteractableWhenCanInteractIsFalse()
        {
            _interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _interactableObject.transform.position = new Vector3(0f, 0f, 2f);
            var interactable = _interactableObject.AddComponent<InteractableObject>();
            interactable.SetInteractable(false);

            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.False);
            Assert.That(_detector.CurrentTarget, Is.Null);
        }
    }
}
```

- [ ] **Step 2: Implement `InteractionDetector.cs`**

Create `Assets/Game/Scripts/Interaction/InteractionDetector.cs`:
```csharp
using UnityEngine;
using Ngecor.Player;

namespace Ngecor.Interaction
{
    [DisallowMultipleComponent]
    public class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _originTransform;
        [SerializeField, Min(0.1f)] private float _maxDistance = 3f;
        [SerializeField] private LayerMask _layerMask = ~0;
        [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField] private bool _isLocalPlayer = true;
        [SerializeField] private bool _showDebugFeedback = true;

        private PlayerMovement _playerMovement;

        public IInteractable CurrentTarget { get; private set; }
        public RaycastHit CurrentHit { get; private set; }
        public bool HasTarget => CurrentTarget != null;

        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = Mathf.Max(0.1f, value);
        }

        public void SetLocalPlayer(bool isLocalPlayer)
        {
            _isLocalPlayer = isLocalPlayer;
            if (!_isLocalPlayer)
            {
                CurrentTarget = null;
                CurrentHit = default;
            }
        }

        public void SetOrigin(Transform originTransform)
        {
            _originTransform = originTransform;
        }

        public void SetCamera(Camera camera)
        {
            _camera = camera;
        }

        private void Awake()
        {
            _playerMovement = GetComponent<PlayerMovement>();
            ResolveOrigin();
        }

        private void Start()
        {
            ResolveOrigin();
        }

        private void ResolveOrigin()
        {
            if (_camera == null && _playerMovement != null && _playerMovement.LocalCamera != null)
            {
                _camera = _playerMovement.LocalCamera;
            }
            if (_camera == null && _originTransform == null)
            {
                _camera = GetComponentInChildren<Camera>(true);
            }
        }

        private void Update()
        {
            if (!_isLocalPlayer)
                return;

            Detect();
        }

        public IInteractable Detect()
        {
            if (!_isLocalPlayer)
            {
                CurrentTarget = null;
                CurrentHit = default;
                return null;
            }

            Transform origin = _camera != null ? _camera.transform : (_originTransform != null ? _originTransform : transform);
            Ray ray = new Ray(origin.position, origin.forward);

            CurrentTarget = null;
            CurrentHit = default;

            if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _layerMask, _triggerInteraction))
            {
                CurrentHit = hit;
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract(gameObject))
                {
                    CurrentTarget = interactable;
                }
            }

            return CurrentTarget;
        }

        private void OnGUI()
        {
            if (!_showDebugFeedback || !_isLocalPlayer)
                return;

            if (HasTarget)
            {
                string targetName = (CurrentTarget as Component)?.gameObject.name ?? "Interactable";
                string prompt = CurrentTarget.InteractionPrompt ?? "Interact";
                GUI.Box(new Rect(10, 10, 240, 50), $"[Interaction]\nTarget: {targetName}\nPrompt: {prompt} (Distance: {CurrentHit.distance:F1}m)");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = _camera != null ? _camera.transform : (_originTransform != null ? _originTransform : transform);
            if (origin == null)
                return;

            Gizmos.color = HasTarget ? Color.green : Color.red;
            float distance = HasTarget ? CurrentHit.distance : _maxDistance;
            Gizmos.DrawRay(origin.position, origin.forward * distance);
        }
    }
}
```

- [ ] **Step 3: Run Unity tests to verify all pass**

Run Unity in batch mode:
```powershell
& "D:\Unityhub\Editor\6000.3.25f1\Editor\Unity.exe" -batchmode -projectPath "D:\orca-workspace\Ngecor\int-001-detect-interactable-object" -runTests -testPlatform playmode -testCategory "" -testResults "test_results_task2.xml" -logfile "unity_task2.log"
```
Expected: PASS (All tests pass, 0 failures).

- [ ] **Step 4: Commit**

```bash
git add Assets/Game/Scripts/Interaction/ Assets/Game/Tests/Interaction/
git commit -m "feat: add InteractionDetector with distance tuning and line-of-sight occlusion"
```

---

### Task 3: Dev Scene Test Setup & Player Hookup

**Files:**
- Test: Play Mode verification in `Dev_Mery.unity` or automated test scene verification.
- Documentation: Create `.development-history/2026-10-02-20-00-int-001-detect-interactable-object.md`

**Interfaces:**
- Validates player detection integration in Play Mode.

- [ ] **Step 1: Run complete automated test suite**

Run both `Ngecor.Player.Tests` and `Ngecor.Interaction.Tests`:
```powershell
& "D:\Unityhub\Editor\6000.3.25f1\Editor\Unity.exe" -batchmode -projectPath "D:\orca-workspace\Ngecor\int-001-detect-interactable-object" -runTests -testPlatform playmode -testResults "test_results_all.xml" -logfile "unity_task3.log"
```
Verify exit code 0 and all tests pass.

- [ ] **Step 2: Write development history report**

Create `.development-history/2026-10-02-20-00-int-001-detect-interactable-object.md` following `AGENTS.md` guidelines (English, task summary, previous context, changes made, files affected, technical decisions, verification performed, final result, known limitations, follow-up work).

- [ ] **Step 3: Commit and push**

```bash
git add .development-history/
git commit -m "docs: add development history report for INT-001"
git push -u origin feat/interaction-detect
```

- [ ] **Step 4: Open Pull Request for INT-001**

Open draft PR via `gh pr create` with body referencing `Closes #9` and following `.github/PULL_REQUEST_TEMPLATE.md`.
