﻿using System;
using JetBrains.Annotations;
using KGySoft.CoreLibraries;

namespace Core.Utils.Extensions
{
    public static class StringExtensions
    {
        public static bool IsSome([CanBeNull] this string input) => string.IsNullOrEmpty(input) == false;
        public static bool IsNone([CanBeNull] this string input) => string.IsNullOrEmpty(input);

        [NotNull]
        public static string ToAlphaNumeric([NotNull] this string input)
        {
            int j = 0;
            Span<char> output = stackalloc char[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                if (char.IsLetterOrDigit(input[i]))
                {
                    output[j] = input[i];
                    j++;
                }
            }

            return output[..j].ToString();
        }
        
        [NotNull]
        public static string ToAlphaNumericLower([NotNull] this string input)
        {
            int j = 0;
            Span<char> output = stackalloc char[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsLetterOrDigit(c))
                {
                    output[j] = char.ToLowerInvariant(c);
                    j++;
                }
            }

            return output[..j].ToString();
        }
        
        public static LineSplitEnumerator SplitLines(this string str) =>

            // LineSplitEnumerator is a struct so there is no allocation here
            new(str.AsSpan());

        // Must be a ref struct as it contains a ReadOnlySpan<char>
        public ref struct LineSplitEnumerator
        {
            private ReadOnlySpan<char> _str;

            public LineSplitEnumerator(ReadOnlySpan<char> str)
            {
                _str = str;
                Current = default;
            }

            // Needed to be compatible with the foreach operator
            public LineSplitEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                ReadOnlySpan<char> span = _str;
                if (span.Length == 0) // Reach the end of the string
                    return false;

                int index = span.IndexOfAny('\r', '\n');
                if (index == -1) // The string is composed of only one line
                {
                    _str = ReadOnlySpan<char>.Empty; // The remaining string is an empty string
                    Current = new LineSplitEntry(span, ReadOnlySpan<char>.Empty);
                    return true;
                }

                if (index < span.Length - 1 && span[index] == '\r')
                {
                    // Try to consume the '\n' associated to the '\r'
                    char next = span[index + 1];
                    if (next == '\n')
                    {
                        Current = new LineSplitEntry(span.Slice(0, index), span.Slice(index, 2));
                        _str = span.Slice(index + 2);
                        return true;
                    }
                }

                Current = new LineSplitEntry(span.Slice(0, index), span.Slice(index, 1));
                _str = span.Slice(index + 1);
                return true;
            }

            public LineSplitEntry Current { get; private set; }
        }

        public readonly ref struct LineSplitEntry
        {
            public LineSplitEntry(ReadOnlySpan<char> line, ReadOnlySpan<char> separator)
            {
                Line = line;
                Separator = separator;
            }

            public ReadOnlySpan<char> Line { get; }
            public ReadOnlySpan<char> Separator { get; }

            // This method allow to deconstruct the type, so you can write any of the following code
            // foreach (var entry in str.SplitLines()) { _ = entry.Line; }
            // foreach (var (line, endOfLine) in str.SplitLines()) { _ = line; }
            // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/deconstruct?WT.mc_id=DT-MVP-5003978#deconstructing-user-defined-types
            public void Deconstruct(out ReadOnlySpan<char> line, out ReadOnlySpan<char> separator)
            {
                line = Line;
                separator = Separator;
            }

            // This method allow to implicitly cast the type into a ReadOnlySpan<char>, so you can write the following code
            // foreach (ReadOnlySpan<char> entry in str.SplitLines())
            public static implicit operator ReadOnlySpan<char>(LineSplitEntry entry) => entry.Line;
        }
        
        [NotNull]
        public static string ToStringNonAlloc<T>(this T enumValue) where T : struct, Enum => Enum<T>.ToString(enumValue);
    }
}