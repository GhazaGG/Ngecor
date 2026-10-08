using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ngecor.Material
{
    public enum MaterialType
    {
        Sand,
        Cement,
        Concrete
    }

    public enum ContainerMode
    {
        SingleType,
        MultipleTypes
    }

    [Serializable]
    public struct MaterialAmount
    {
        [SerializeField] private MaterialType _type;
        [SerializeField, Min(0)] private int _units;

        public MaterialType Type => _type;
        public int Units => _units;

        public MaterialAmount(MaterialType type, int units)
        {
            _type = type;
            _units = units;
        }
    }

    public readonly struct ConcreteRecipe
    {
        public int CementUnits { get; }
        public int SandUnits { get; }
        public int ConcreteUnitsPerBatch => CementUnits + SandUnits;

        public ConcreteRecipe(int cementUnits, int sandUnits)
        {
            if (cementUnits <= 0)
                throw new ArgumentOutOfRangeException(nameof(cementUnits));
            if (sandUnits <= 0)
                throw new ArgumentOutOfRangeException(nameof(sandUnits));
            if ((long)cementUnits + sandUnits > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(sandUnits), "Recipe output exceeds the supported unit count.");

            CementUnits = cementUnits;
            SandUnits = sandUnits;
        }
    }

    public readonly struct ConcreteMixResult
    {
        public int Batches { get; }
        public int ConcreteUnits { get; }
        public int LeftoverCementUnits { get; }
        public int LeftoverSandUnits { get; }

        internal ConcreteMixResult(int batches, int concreteUnits, int leftoverCementUnits,
            int leftoverSandUnits)
        {
            Batches = batches;
            ConcreteUnits = concreteUnits;
            LeftoverCementUnits = leftoverCementUnits;
            LeftoverSandUnits = leftoverSandUnits;
        }
    }

    public static class ConcreteRecipeCalculator
    {
        public static ConcreteMixResult Mix(int cementUnits, int sandUnits, ConcreteRecipe recipe)
        {
            if (cementUnits < 0)
                throw new ArgumentOutOfRangeException(nameof(cementUnits));
            if (sandUnits < 0)
                throw new ArgumentOutOfRangeException(nameof(sandUnits));
            if (recipe.CementUnits <= 0 || recipe.SandUnits <= 0)
                throw new ArgumentException("Recipe values must be positive.", nameof(recipe));

            var batches = Math.Min(cementUnits / recipe.CementUnits, sandUnits / recipe.SandUnits);
            var concrete = (long)batches * recipe.ConcreteUnitsPerBatch;
            if (concrete > int.MaxValue)
                throw new OverflowException("Concrete output exceeds the supported unit count.");

            return new ConcreteMixResult(batches, (int)concrete,
                cementUnits - batches * recipe.CementUnits,
                sandUnits - batches * recipe.SandUnits);
        }
    }

    public sealed class BulkMaterialContainer : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _capacity = 100;
        [SerializeField] private bool _unlimitedCapacity;
        [SerializeField] private ContainerMode _mode;
        [SerializeField] private MaterialType _singleType;
        [SerializeField] private List<MaterialType> _acceptedTypes = new List<MaterialType>();
        [SerializeField] private List<MaterialAmount> _contents = new List<MaterialAmount>();
        [SerializeField, Min(0f)] private float _transferUnitsPerSecond = 10f;
        [SerializeField, Range(0f, 180f)] private float _spillAngleDegrees = 70f;
        [SerializeField, Min(0f)] private float _spillUnitsPerSecond = 5f;
        [SerializeField] private Transform _fillVisual;

        private Rigidbody _body;
        private GroundMaterialDeposit _groundDeposit;
        private bool _transferToGround;
        private BulkMaterialContainer _transferTarget;
        private MaterialType _transferType;
        private double _transferCredit;
        private double _spillCredit;
        private float _spillCosine;
        private Transform _cachedFillVisual;
        private Vector3 _fullFillScale;
        private Vector3 _fullFillPosition;

        public int Capacity => _unlimitedCapacity ? int.MaxValue : _capacity;

        public int TotalUnits
        {
            get
            {
                long total = 0;
                foreach (var amount in _contents)
                    total += Mathf.Max(0, amount.Units);
                return (int)Math.Min(total, int.MaxValue);
            }
        }

        private void Awake()
        {
            _capacity = Mathf.Max(1, _capacity);
            NormalizeContents();
            _body = GetComponent<Rigidbody>();
            _groundDeposit = GetComponent<GroundMaterialDeposit>();
            _spillCosine = Mathf.Cos(_spillAngleDegrees * Mathf.Deg2Rad);
            UpdateFillVisual();
        }

        private void OnValidate()
        {
            _capacity = Mathf.Max(1, _capacity);
            _transferUnitsPerSecond = Mathf.Max(0f, _transferUnitsPerSecond);
            _spillUnitsPerSecond = Mathf.Max(0f, _spillUnitsPerSecond);
            _spillAngleDegrees = Mathf.Clamp(_spillAngleDegrees, 0f, 180f);
            _spillCosine = Mathf.Cos(_spillAngleDegrees * Mathf.Deg2Rad);
            NormalizeContents();
        }

        private void NormalizeContents()
        {
            if (_contents == null)
            {
                _contents = new List<MaterialAmount>();
                return;
            }

            var room = Capacity;
            for (var i = 0; i < _contents.Count; i++)
            {
                var amount = _contents[i];
                var units = Accepts(amount.Type) ? Mathf.Clamp(amount.Units, 0, room) : 0;
                _contents[i] = new MaterialAmount(amount.Type, units);
                room -= units;
            }
        }

        public int GetUnits(MaterialType type)
        {
            long total = 0;
            foreach (var amount in _contents)
            {
                if (amount.Type == type)
                    total += Mathf.Max(0, amount.Units);
            }
            return (int)Math.Min(total, int.MaxValue);
        }

        public bool Accepts(MaterialType type)
        {
            if (_mode == ContainerMode.SingleType)
                return type == _singleType;
            return _acceptedTypes != null && _acceptedTypes.Contains(type);
        }

        public bool ConfigureSingleTypeWhenEmpty(MaterialType type)
        {
            if (TotalUnits != 0 || !Enum.IsDefined(typeof(MaterialType), type))
                return false;
            _mode = ContainerMode.SingleType;
            _singleType = type;
            CancelTransfer();
            return true;
        }

        public bool TryGetMaterialType(out MaterialType type)
        {
            type = default;
            var found = false;
            foreach (var amount in _contents)
            {
                if (amount.Units <= 0)
                    continue;
                if (found && amount.Type != type)
                    return false;
                type = amount.Type;
                found = true;
            }
            return found;
        }

        public int AddUnits(MaterialType type, int requestedUnits)
        {
            if (requestedUnits <= 0 || !Accepts(type))
                return 0;

            var added = Mathf.Min(requestedUnits, Mathf.Max(0, Capacity - TotalUnits));
            if (added == 0)
                return 0;

            for (var i = 0; i < _contents.Count; i++)
            {
                if (_contents[i].Type != type)
                    continue;
                _contents[i] = new MaterialAmount(type, _contents[i].Units + added);
                UpdateFillVisual();
                return added;
            }

            _contents.Add(new MaterialAmount(type, added));
            UpdateFillVisual();
            return added;
        }

        public int RemoveUnits(MaterialType type, int requestedUnits)
        {
            if (requestedUnits <= 0)
                return 0;

            var remaining = requestedUnits;
            for (var i = 0; i < _contents.Count && remaining > 0; i++)
            {
                var amount = _contents[i];
                if (amount.Type != type || amount.Units <= 0)
                    continue;

                var removed = Mathf.Min(remaining, amount.Units);
                _contents[i] = new MaterialAmount(type, amount.Units - removed);
                remaining -= removed;
            }

            var result = requestedUnits - remaining;
            if (result > 0)
                UpdateFillVisual();
            return result;
        }

        public int TransferUnitsTo(BulkMaterialContainer target, MaterialType type, int requestedUnits)
        {
            if (target == null || target == this || requestedUnits <= 0 || !target.Accepts(type))
                return 0;

            var amount = Math.Min(requestedUnits, Math.Min(GetUnits(type),
                Mathf.Max(0, target.Capacity - target.TotalUnits)));
            if (amount == 0)
                return 0;

            // Check the receiver before removing stock, then add exactly the same integer amount.
            var removed = RemoveUnits(type, amount);
            target.AddUnits(type, removed);
            return removed;
        }

        public int TransferForSeconds(BulkMaterialContainer target, MaterialType type, float seconds)
        {
            return TransferForSeconds(target, type, seconds, false);
        }

        public int TransferToGroundForSeconds(MaterialType type, float seconds)
        {
            return TransferForSeconds(null, type, seconds, true);
        }

        public void CancelTransfer()
        {
            _transferCredit = 0;
            _transferTarget = null;
            _transferToGround = false;
        }

        private int TransferForSeconds(BulkMaterialContainer target, MaterialType type, float seconds, bool ground)
        {
            if (target != _transferTarget || type != _transferType || ground != _transferToGround)
            {
                _transferTarget = target;
                _transferType = type;
                _transferToGround = ground;
                _transferCredit = 0;
            }

            if (seconds <= 0f || float.IsNaN(seconds)
                || float.IsInfinity(seconds) || _transferUnitsPerSecond <= 0f
                || GetUnits(type) == 0
                || (ground ? _groundDeposit == null : target == null || target == this
                    || !target.Accepts(type) || target.TotalUnits >= target.Capacity))
            {
                _transferCredit = 0;
                return 0;
            }

            var credit = _transferCredit + (double)_transferUnitsPerSecond * seconds;
            var requested = (int)Math.Min(Math.Floor(credit), int.MaxValue);
            if (requested == 0)
            {
                _transferCredit = credit;
                return 0;
            }

            var moved = ground ? _groundDeposit.Deposit(type, requested)
                : TransferUnitsTo(target, type, requested);
            _transferCredit = moved == requested ? credit - requested : 0;
            return moved;
        }

        private void FixedUpdate()
        {
            if (_body == null || _spillUnitsPerSecond <= 0f || TotalUnits == 0
                || Vector3.Dot(transform.up, Vector3.up) > _spillCosine)
            {
                _spillCredit = 0;
                return;
            }

            var available = TotalUnits;
            var credit = _spillCredit + (double)_spillUnitsPerSecond * Time.fixedDeltaTime;
            var requested = (int)Math.Min(Math.Floor(credit), available);
            if (requested == 0)
            {
                _spillCredit = credit;
                return;
            }

            var remaining = requested;
            if (_groundDeposit != null)
            {
                for (var i = 0; i < _contents.Count && remaining > 0; i++)
                    remaining -= _groundDeposit.Deposit(_contents[i].Type,
                        Mathf.Min(remaining, _contents[i].Units));
            }

            _spillCredit = remaining == 0 && requested < available ? credit - requested : 0;
        }

        private void UpdateFillVisual()
        {
            if (_fillVisual == null)
                return;

            if (_cachedFillVisual != _fillVisual)
            {
                _cachedFillVisual = _fillVisual;
                _fullFillScale = _fillVisual.localScale;
                _fullFillPosition = _fillVisual.localPosition;
            }

            var ratio = Mathf.Clamp01((float)TotalUnits / Capacity);
            var scale = _fullFillScale;
            scale.y *= ratio;
            _fillVisual.localScale = scale;
            var position = _fullFillPosition;
            position.y -= _fullFillScale.y * (1f - ratio) * 0.5f;
            _fillVisual.localPosition = position;
        }
    }
}
