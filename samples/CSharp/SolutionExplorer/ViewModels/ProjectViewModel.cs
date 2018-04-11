using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal class ProjectViewModel : HierarchyItemViewModel
    {
        private readonly ProjectId _projectId;
        private readonly Dictionary<string, FolderViewModel> _folders;

        public ProjectViewModel(Workspace workspace, ProjectId projectId)
            : base(workspace, isExpanded: false)
        {
            _projectId = projectId;
            _folders = new Dictionary<string, FolderViewModel>(StringComparer.OrdinalIgnoreCase);

            var solution = workspace.CurrentSolution;
            var project = solution.GetProject(projectId);

            foreach (var documentId in project.DocumentIds)
            {
                var documentViewModel = new DocumentViewModel(workspace, documentId);

                var folders = solution.GetDocument(documentId).Folders;
                if (folders.Count > 0)
                {
                    var folderViewModel = GetOrCreateFolder(workspace, folders);
                    folderViewModel.AddChild(documentViewModel);
                }
                else
                {
                    AddChild(documentViewModel);
                }
            }

            // Add root folders
            foreach (var folder in _folders)
            {
                if (!folder.Key.Contains('\\'))
                {
                    AddChild(folder.Value);
                }
            }

            ReferencesFolderViewModel referencesFolderViewModel = null;

            // Add references
            if (project.MetadataReferences.Count > 0)
            {
                referencesFolderViewModel = new ReferencesFolderViewModel(workspace);

                foreach (var metadataReference in project.MetadataReferences)
                {
                    var metadataReferenceViewModel = new MetadataReferenceViewModel(workspace, metadataReference);
                    referencesFolderViewModel.AddChild(metadataReferenceViewModel);
                }
            }

            if (project.ProjectReferences.Any())
            {
                if (referencesFolderViewModel == null)
                {
                    referencesFolderViewModel = new ReferencesFolderViewModel(workspace);
                }

                foreach (var projectReference in project.ProjectReferences)
                {
                    var projectReferenceViewModel = new ProjectReferenceViewModel(workspace, projectReference);
                    referencesFolderViewModel.AddChild(projectReferenceViewModel);
                }
            }

            if (referencesFolderViewModel != null)
            {
                AddChild(referencesFolderViewModel);
            }
        }

        private FolderViewModel GetOrCreateFolder(Workspace workspace, IReadOnlyList<string> folders)
        {
            var folderPath = string.Join(@"\", folders);

            if (_folders.TryGetValue(folderPath, out var folderViewModel))
            {
                return folderViewModel;
            }

            var currentFolderPath = folderPath;

            foreach (var folder in folders.Reverse())
            {
                var newFolderViewModel = new FolderViewModel(workspace, folder);

                if (folderViewModel != null)
                {
                    newFolderViewModel.AddChild(folderViewModel);
                }

                folderViewModel = newFolderViewModel;
                _folders.Add(currentFolderPath, folderViewModel);

                var lastSlashIndex = currentFolderPath.LastIndexOf('\\');
                if (lastSlashIndex < 0)
                {
                    break;
                }

                var parentFolderPath = currentFolderPath.Substring(0, lastSlashIndex);

                if (_folders.TryGetValue(parentFolderPath, out var parentFolderViewModel))
                {
                    parentFolderViewModel.AddChild(folderViewModel);
                    return folderViewModel;
                }

                currentFolderPath = parentFolderPath;
            }

            return _folders[folderPath];
        }

        protected override string GetDisplayName()
            => Workspace.CurrentSolution.GetProject(_projectId).Name;

        public string Language
            => Workspace.CurrentSolution.GetProject(_projectId).Language;
    }
}
