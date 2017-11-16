// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

public partial class RoslynSDKChildTemplateWizard : IWizard
{
    /// <summary>
    /// Only one wizard will be run by the project system.  
    /// This means our wizard needs to load and call the nuget wizard for every function.
    /// </summary>
    private IWizard NugetWizard => lazyWizard.Value;

    private Lazy<IWizard> lazyWizard = new Lazy<IWizard>(() =>
    {
        var asm = Assembly.Load("NuGet.VisualStudio.Interop, Version=1.0.0.0, Culture=Neutral, PublicKeyToken=b03f5f7f11d50a3a");
        return (IWizard)asm.CreateInstance("NuGet.VisualStudio.TemplateWizard");
    });

    public void BeforeOpeningFile(ProjectItem projectItem) => NugetWizard.BeforeOpeningFile(projectItem);
    public void ProjectFinishedGenerating(Project project)
    {
        NugetWizard.ProjectFinishedGenerating(project);
        OnProjectFinishedGenerating(project);
    }
    public void RunFinished() => NugetWizard.RunFinished();
    public bool ShouldAddProjectItem(string filePath) => NugetWizard.ShouldAddProjectItem(filePath);
    public void ProjectItemFinishedGenerating(ProjectItem projectItem) => NugetWizard.ProjectItemFinishedGenerating(projectItem);
    public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
    {
        NugetWizard.RunStarted(automationObject, replacementsDictionary, runKind, customParams);
        OnRunStarted(automationObject as DTE, replacementsDictionary, runKind, customParams);
    }
}
