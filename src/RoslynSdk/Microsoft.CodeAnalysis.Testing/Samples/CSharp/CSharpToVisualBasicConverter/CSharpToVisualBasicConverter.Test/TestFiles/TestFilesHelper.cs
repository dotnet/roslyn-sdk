// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Reflection;

namespace CSharpToVisualBasicConverter.UnitTests.TestFiles
{
    internal class TestFilesHelper
    {
        public static string GetFile(string fileName)
        {
            var fullName = "CSharpToVisualBasicConverter.Test.TestFiles." + fileName;
            var resourceStream = Assembly.GetAssembly(typeof(TestFilesHelper)).GetManifestResourceStream(fullName);
            using (var streamReader = new StreamReader(resourceStream))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}
