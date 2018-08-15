﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Roslyn.UnitTestFramework
{
    internal static class DictionaryExtensions
    {
        // Copied from ConcurrentDictionary since IDictionary doesn't have this useful method
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> function)
        {
            if (!dictionary.TryGetValue(key, out TValue value))
            {
                value = function(key);
                dictionary.Add(key, value);
            }

            return value;
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> function)
            => dictionary.GetOrAdd(key, _ => function());

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }
}
