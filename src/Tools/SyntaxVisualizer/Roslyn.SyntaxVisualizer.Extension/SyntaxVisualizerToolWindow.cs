// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Roslyn.SyntaxVisualizer.Extension
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("da7e21aa-da94-452d-8aa1-d1b23f73f576")]
    public class SyntaxVisualizerToolWindow : ToolWindowPane, IOleCommandTarget
    {
        private readonly SyntaxVisualizerContainer _container;

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public SyntaxVisualizerToolWindow() :
            base(null)
        {
            // Set the window title reading it from the resources.
            Caption = Resources.ToolWindowTitle;

            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            BitmapResourceID = 500;
            BitmapIndex = 0;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            _container = new SyntaxVisualizerContainer(this);
            Content = _container;

            VSColorTheme.ThemeChanged += HandleThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                VSColorTheme.ThemeChanged -= HandleThemeChanged;
            }

            base.Dispose(disposing);
        }

        private void HandleThemeChanged(ThemeChangedEventArgs e)
        {
            _container.UpdateThemedColors();
        }

        internal TServiceInterface GetVsService<TServiceInterface, TService>()
            where TServiceInterface : class
            where TService : class
        {
            return (TServiceInterface)GetService(typeof(TService));
        }

        protected override object GetService(Type serviceType)
        {
            if (serviceType == typeof(IOleCommandTarget))
            {
                return this;
            }

            return base.GetService(serviceType);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            var baseTarget = (IOleCommandTarget)base.GetService(typeof(IOleCommandTarget));
            return baseTarget?.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText) ?? (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // Triggered when Escape is pressed in the tool window. If the popup is open, we want to close
            // the popup and return S_OK, so that focus is not set to the main text document window.
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97 &&
                (VSConstants.VSStd97CmdID)nCmdID == VSConstants.VSStd97CmdID.PaneActivateDocWindow &&
                _container.TryHandleEscape())
            {
                return VSConstants.S_OK;
            }

            var baseTarget = (IOleCommandTarget)base.GetService(typeof(IOleCommandTarget));
            return baseTarget?.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut) ?? (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }
    }
}
