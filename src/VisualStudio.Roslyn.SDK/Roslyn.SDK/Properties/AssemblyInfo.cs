﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell;

[assembly: ProvideBindingRedirection(CodeBase = "Roslyn.SDK.Template.Wizard.dll", OldVersionLowerBound = "1.0.0.0")]
[assembly: ProvideCodeBase(AssemblyName = "Roslyn.ComponentDebugger", CodeBase = "$PackageFolder$\\Roslyn.ComponentDebugger.dll")]
