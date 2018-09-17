using SolcNet.DataDescription.Output;
using System;

namespace Meadow.CoverageReport
{
    public struct SourceRange
    {
        public readonly int Offset;
        public readonly int OffsetEnd;
        public readonly int Length;
        public readonly int SourceIndex;

        public override string ToString()
        {
            return $"Index: {SourceIndex}, Offset: {Offset}, Length: {Length}";
        }

        public bool Contains(SourceRange other)
        {
            return SourceIndex == other.SourceIndex && Offset <= other.Offset && OffsetEnd >= other.OffsetEnd;
        }

        public bool Contains(SourceMapEntry other)
        {
            return SourceIndex == other.Index && Offset <= other.Offset && OffsetEnd >= (other.Offset + other.Length);
        }

        public override int GetHashCode()
        {
            return (Offset, Length, SourceIndex).GetHashCode();
        }

        public bool Equals(SourceRange other)
        {
            return other.Offset == Offset && other.Length == Length && other.SourceIndex == SourceIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is SourceRange other && Equals(other);
        }

        public SourceRange(string str)
        {
            var parts = str.AsSpan();
            int colonIndex = parts.IndexOf(':');
            if (!int.TryParse(parts.Slice(0, colonIndex), out Offset))
            {
                throw new Exception("Source range parse failed: " + parts.ToString());
            }

            parts = parts.Slice(colonIndex + 1);
            colonIndex = parts.IndexOf(':');
            if (!int.TryParse(parts.Slice(0, colonIndex), out Length))
            {
                throw new Exception("Source range parse failed: " + parts.ToString());
            }

            parts = parts.Slice(colonIndex + 1);
            if (!int.TryParse(parts, out SourceIndex))
            {
                throw new Exception("Source range parse failed: " + parts.ToString());
            }

            OffsetEnd = Offset + Length;
        }

        public static bool operator ==(SourceRange left, SourceRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SourceRange left, SourceRange right)
        {
            return !(left == right);
        }
    }

}

