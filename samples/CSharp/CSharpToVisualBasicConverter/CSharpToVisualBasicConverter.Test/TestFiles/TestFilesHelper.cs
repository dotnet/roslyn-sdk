// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Reflection;

namespace CSharpToVisualBasicConverter.UnitTests.TestFiles
{
    internal class TestFilesHelper
    {
        public static string GetFile(string fileName)
        {
            string fullName = "Roslyn.Samples.CSharp.UnitTests.TestFiles." + fileName;
            Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName);
            using (StreamReader streamReader = new StreamReader(resourceStream))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}
