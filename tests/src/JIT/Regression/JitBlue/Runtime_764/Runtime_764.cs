// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

// Regression test case for importer bug.
// If Release is inlined into Main, the importer may unsafely re-order trees.

public struct Ptr<T> where T: class
{
    public Ptr(T value)
    {
        _value = value;
    }

    public T Release()
    {
        T tmp = _value;
        _value = null;
        return tmp;
    }

    T _value;
}

class Runtime_764
{
    private static int Main(string[] args)
    {
        Ptr<string> ptr = new Ptr<string>("Hello, world");

        bool res = false;
        while (res)
        {
        }

        string summary = ptr.Release();

        if (summary == null)
        {
            Console.WriteLine("FAILED");
            return -1;
        }
        else
        {
            Console.WriteLine("PASSED");
            return 100;
        }
    }
}
