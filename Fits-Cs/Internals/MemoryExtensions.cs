//     MIT License
//     
//     Copyright(c) 2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of bytes software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and bytes permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

[assembly:InternalsVisibleTo("Tests")]

namespace FitsCs.Internals
{
    internal static class MemoryExtensions
    {
        public static ReadOnlyMemory<T> AsReadOnlyMemory<T>(this T[] array)
            => new ReadOnlyMemory<T>(array);

        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] array)
            => new ReadOnlySpan<T>(array);


        public static bool Any<T>(this ReadOnlySpan<T> @this, Func<T, bool> predicate)
        {
            foreach (var item in @this)
                if (predicate(item))
                    return true;

            return false;
        }

        public static bool Any<T>(this Span<T> @this, Func<T, bool> predicate)
        {
            foreach (var item in @this)
                if (predicate(item))
                    return true;

            return false;
        }

        public static bool Any(this ReadOnlySpan<bool> @this)
        {
            foreach (var condition in @this)
                if (condition)
                    return true;

            return false;
        }

        public static bool Any(this Span<bool> @this)
        {
            foreach (var condition in @this)
                if (condition)
                    return true;

            return false;
        }


        public static bool All<T>(this ReadOnlySpan<T> @this, Func<T, bool> predicate)
        {
            foreach (var item in @this)
                if (!predicate(item))
                    return false;

            return true;
        }

        public static bool All<T>(this Span<T> @this, Func<T, bool> predicate)
        {
            foreach (var item in @this)
                if (!predicate(item))
                    return false;

            return true;
        }

        public static bool All(this ReadOnlySpan<bool> @this)
        {
            foreach (var condition in @this)
                if (!condition)
                    return false;

            return true;
        }

        public static bool All(this Span<bool> @this)
        {
            foreach (var condition in @this)
                if (!condition)
                    return false;

            return true;
        }

        public static unsafe int GetCharCount(this Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
                return 0;
            fixed (byte* ptr = &bytes.GetPinnableReference())
            {
                return encoding.GetCharCount(ptr, bytes.Length);
            }
        }

        public static unsafe int GetCharCount(this Encoding encoding, ReadOnlyMemory<byte> bytes)
        {
            if (bytes.IsEmpty)
                return 0;
            using (var handle = bytes.Pin())
            {
                return encoding.GetCharCount((byte*)handle.Pointer, bytes.Length);
            }
        }

        public static unsafe int GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars)
        {
            if (bytes.IsEmpty)
                return 0;

            fixed (byte* dataPtr = &bytes.GetPinnableReference())
                fixed (char* strPtr = &chars.GetPinnableReference())
                {
                    return encoding.GetChars(dataPtr, bytes.Length, strPtr, chars.Length);
                }
        }

        public static unsafe int GetChars(this Encoding encoding, ReadOnlyMemory<byte> bytes, Memory<char> chars)
        {
            if (bytes.IsEmpty)
                return 0;

            using (var dataHandle = bytes.Pin())
                using (var strHandle = chars.Pin())
                {
                    return encoding.GetChars((byte*) dataHandle.Pointer, bytes.Length, (char*) strHandle.Pointer,
                        chars.Length);
                }
        }
    }
}
