using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HandlebarsDotNet.Collections;
using HandlebarsDotNet.EqualityComparers;
using HandlebarsDotNet.Runtime;

namespace HandlebarsDotNet.Compiler.Structure.Path
{
    internal enum WellKnownVariable
    {
        None = -1,
        Index = 0,
        Key = 1,
        Value = 2,
        First = 3,
        Last = 4,
        Root = 5,
        Parent = 6,
        This = 7,
    }
    
    /// <summary>
    /// Represents parts of single <see cref="PathSegment"/> separated with dots.
    /// </summary>
    public sealed partial class ChainSegment : IEquatable<ChainSegment>
    {
        private const string ThisValue = "this";
        private static readonly char[] TrimStart = {'@'};

        private static readonly LookupSlim<string, GcDeferredValue<CreationProperties, ChainSegment>, StringEqualityComparer> Lookup = new LookupSlim<string, GcDeferredValue<CreationProperties, ChainSegment>, StringEqualityComparer>(new StringEqualityComparer(StringComparison.Ordinal));
        
        private static readonly Func<string, WellKnownVariable, GcDeferredValue<CreationProperties, ChainSegment>> ValueFactory = (s, v) =>
        {
            return new GcDeferredValue<CreationProperties, ChainSegment>(new CreationProperties(s, v), properties => new ChainSegment(properties.String, properties.KnownVariable));
        };
        
        public static ChainSegmentEqualityComparer EqualityComparer { get; } = new ChainSegmentEqualityComparer();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChainSegment Create(string value) => Lookup.GetOrAdd(value, ValueFactory, WellKnownVariable.None).Value;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChainSegment Create(object value)
        {
            if (value is ChainSegment segment) return segment;
            return Lookup.GetOrAdd(value as string ?? value.ToString(), ValueFactory, WellKnownVariable.None).Value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ChainSegment Create(string value, WellKnownVariable variable, bool createVariable = false)
        {
            if (createVariable)
            {
                Lookup.GetOrAdd($"@{value}", ValueFactory, variable);
            }
            
            return Lookup.GetOrAdd(value, ValueFactory, variable).Value;
        }

        public static ChainSegment Index { get; } = Create(nameof(Index), WellKnownVariable.Index, true);
        public static ChainSegment First { get; } = Create(nameof(First), WellKnownVariable.First, true);
        public static ChainSegment Last { get; } = Create(nameof(Last), WellKnownVariable.Last, true);
        public static ChainSegment Value { get; } = Create(nameof(Value), WellKnownVariable.Value, true);
        public static ChainSegment Key { get; } = Create(nameof(Key), WellKnownVariable.Key, true);
        public static ChainSegment Root { get; } = Create(nameof(Root), WellKnownVariable.Root, true);
        public static ChainSegment Parent { get; } = Create(nameof(Parent), WellKnownVariable.Parent, true);
        public static ChainSegment This { get; } = Create(nameof(This), WellKnownVariable.This);
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly int _hashCode;
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _value;

        /// <summary>
        ///  
        /// </summary>
        private ChainSegment(string value, WellKnownVariable wellKnownVariable = WellKnownVariable.None)
        {
            WellKnownVariable = wellKnownVariable;

            var isNullOrEmpty = string.IsNullOrEmpty(value);
            var segmentValue = isNullOrEmpty ? ThisValue : value.TrimStart(TrimStart);
            var segmentTrimmedValue = TrimSquareBrackets(segmentValue);

            _value = segmentValue;
            IsThis = isNullOrEmpty || string.Equals(value, ThisValue, StringComparison.OrdinalIgnoreCase);
            IsVariable = !isNullOrEmpty && value.StartsWith("@");
            TrimmedValue = segmentTrimmedValue;
            LowerInvariant = segmentTrimmedValue.ToLowerInvariant();
            
            IsValue = LowerInvariant == "value";

            _hashCode = GetHashCodeImpl();

            if (IsThis) WellKnownVariable = WellKnownVariable.This;
            if (IsValue) WellKnownVariable = WellKnownVariable.Value;
        }

        /// <summary>
        /// Value with trimmed '[' and ']'
        /// </summary>
        public readonly string TrimmedValue;
        
        /// <summary>
        /// Indicates whether <see cref="ChainSegment"/> is part of <c>@</c> variable
        /// </summary>
        public readonly bool IsVariable;
        
        /// <summary>
        /// Indicates whether <see cref="ChainSegment"/> is <c>this</c> or <c>.</c>
        /// </summary>
        public readonly bool IsThis;

        internal readonly string LowerInvariant;
        internal readonly bool IsValue;
        
        internal readonly WellKnownVariable WellKnownVariable;

        /// <summary>
        /// Returns string representation of current <see cref="ChainSegment"/>
        /// </summary>
        public override string ToString() => _value;

        /// <inheritdoc />
        public bool Equals(ChainSegment other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualsImpl(other);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (!(obj is ChainSegment segment)) return false;
            return EqualsImpl(segment);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EqualsImpl(ChainSegment other)
        {
            return _hashCode == other._hashCode 
                   && IsThis == other.IsThis 
                   && LowerInvariant == other.LowerInvariant;
        }

        /// <inheritdoc />
        public override int GetHashCode() => _hashCode;

        private int GetHashCodeImpl()
        {
            unchecked
            {
                var hashCode = IsThis.GetHashCode();
                hashCode = (hashCode * 397) ^ (LowerInvariant.GetHashCode());
                return hashCode;
            }
        }

        /// <inheritdoc cref="Equals(HandlebarsDotNet.Compiler.Structure.Path.ChainSegment)"/>
        public static bool operator ==(ChainSegment a, ChainSegment b) => Equals(a, b);

        /// <inheritdoc cref="Equals(HandlebarsDotNet.Compiler.Structure.Path.ChainSegment)"/>
        public static bool operator !=(ChainSegment a, ChainSegment b) => !Equals(a, b);

        /// <inheritdoc cref="ToString"/>
        public static implicit operator string(ChainSegment segment) => segment._value;
        
        /// <summary>
        /// 
        /// </summary>
        
        public static implicit operator ChainSegment(string segment) => Create(segment);

        private static string TrimSquareBrackets(string key)
        {
            //Only trim a single layer of brackets.
            if (key.StartsWith("[") && key.EndsWith("]"))
            {
                return key.Substring(1, key.Length - 2);
            }

            return key;
        }

        private readonly struct CreationProperties
        {
            public readonly string String;
            public readonly WellKnownVariable KnownVariable;

            public CreationProperties(string @string, WellKnownVariable knownVariable = WellKnownVariable.None)
            {
                String = @string;
                KnownVariable = knownVariable;
            }
        }
    }
}