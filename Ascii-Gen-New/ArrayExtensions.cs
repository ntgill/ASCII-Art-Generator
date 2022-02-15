using System;

public static class ArrayExtensions
{// extends arrays

    /// <summary>
    /// Returns a chunk of the given array starting at the index
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">Target array</param>
    /// <param name="index">Starting location</param>
    /// <param name="length">Size of chunk</param>
    /// <returns></returns>
    public static T[] Slice<T>(this T[] source, int index, int length)
    {// extension ^  -------------  ^
        T[] slice = new T[length];
        Array.Copy(source, index, slice, 0, length);
        return slice;
    }

}