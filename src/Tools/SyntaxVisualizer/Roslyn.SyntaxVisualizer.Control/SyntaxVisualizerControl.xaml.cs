// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;

namespace Roslyn.SyntaxVisualizer.Control
{
    public enum SyntaxCategory
    {
        None,
        SyntaxNode,
        SyntaxToken,
        SyntaxTrivia
    }

    // A control for visually displaying the contents of a SyntaxTree.
    public partial class SyntaxVisualizerControl : UserControl
    {
        // Instances of this class are stored in the Tag field of each item in the treeview.
        private class SyntaxTag
        {
            internal TextSpan Span { get; set; }
            internal TextSpan FullSpan { get; set; }
            internal TreeViewItem ParentItem { get; set; }
            internal string Kind { get; set; }
            internal SyntaxNode SyntaxNode { get; set; }
            internal SyntaxToken SyntaxToken { get; set; }
            internal SyntaxTrivia SyntaxTrivia { get; set; }
            internal SyntaxCategory Category { get; set; }
        }

        #region Private State
        private TreeViewItem _currentSelection;
        private bool _isNavigatingFromSourceToTree;
        private bool _isNavigatingFromTreeToSource;
        private readonly System.Windows.Forms.PropertyGrid _propertyGrid;
        private readonly System.Windows.Forms.RichTextBox _textBox;
        private readonly TabStopPanel _tabStopPanel;
        private static readonly Thickness s_defaultBorderThickness = new Thickness(1);

        /// <remarks>
        /// Every unselected item in the TreeView has a foreground (indicating if it is a node/
        /// token/trivia) and background color (indicating the presence of diagnostics). When an
        /// item is selected (active or inactive), we want to ensure that it looks obviously 
        /// selected while maintaining contrasting colors.
        /// 
        /// To that end, we want to use these custom colors when unselected and the ControlTemplate
        /// colors when selected. Unfortunately, our use of hard-coded color values to instantiate
        /// TreeViewItems in code makes this very difficult to accomplish declaratively. We should
        /// remove the hard-coded color approach in favor of a data class with a DataTemplate in
        /// the future.
        /// 
        /// Instead, we listen for when items are selected/unselected and manually swap colors 
        /// around. With the goal of using custom colors when unselected and the ControlTemplate 
        /// when selected, we handle the colors by:
        ///
        ///   - Background colors: The item's control template hides the specified background color
        /// when it is selected (active or inactive) by overlaying a Border colored by the 
        /// highlight brush. When the item becomes unselected again, the added Border is hidden, 
        /// allowing the originally specified background color to show again, so we don't need any
        /// custom handling.
        /// 
        ///   - Foreground colors: The item's control template does *not* override the specified
        /// foreground when it is selected. To use the control templates correctly themed defaults,
        /// we temporarily clear the specified foreground color and restore it when the item is
        /// unselected. This field is used to save and restore that foreground color.
        /// </remarks>
        private Brush _currentSelectionUnselectedForeground;
        #endregion

        #region Public Properties, Events
        public SyntaxTree SyntaxTree { get; private set; }
        public SemanticModel SemanticModel { get; private set; }
        public bool IsLazy { get; private set; }

        public delegate void SyntaxNodeDelegate(SyntaxNode node);
        public event SyntaxNodeDelegate SyntaxNodeDirectedGraphRequested;
        public event SyntaxNodeDelegate SyntaxNodeNavigationToSourceRequested;

        public delegate void SyntaxTokenDelegate(SyntaxToken token);
        public event SyntaxTokenDelegate SyntaxTokenDirectedGraphRequested;
        public event SyntaxTokenDelegate SyntaxTokenNavigationToSourceRequested;

        public delegate void SyntaxTriviaDelegate(SyntaxTrivia trivia);
        public event SyntaxTriviaDelegate SyntaxTriviaDirectedGraphRequested;
        public event SyntaxTriviaDelegate SyntaxTriviaNavigationToSourceRequested;
        #endregion

        #region Public Methods
        public SyntaxVisualizerControl()
        {
            InitializeComponent();

            _propertyGrid = new System.Windows.Forms.PropertyGrid
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                PropertySort = System.Windows.Forms.PropertySort.Alphabetical,
                HelpVisible = false,
                ToolbarVisible = false,
                CommandsVisibleIfAvailable = false
            };
            _tabStopPanel = new TabStopPanel(windowsFormsHost)
            {
                PropertyGrid = _propertyGrid,
                BorderStyle = System.Windows.Forms.BorderStyle.None,
                Padding = System.Windows.Forms.Padding.Empty,
                Margin = System.Windows.Forms.Padding.Empty
            };
            windowsFormsHost.Child = _tabStopPanel;
            _textBox = new System.Windows.Forms.RichTextBox()
            {
                Multiline = true
            };
        }

        public void Clear()
        {
            treeView.Items.Clear();
            _propertyGrid.SelectedObject = null;
            typeTextLabel.Visibility = Visibility.Hidden;
            kindTextLabel.Visibility = Visibility.Hidden;
            typeValueLabel.Content = string.Empty;
            kindValueLabel.Content = string.Empty;
            legendButton.Visibility = Visibility.Hidden;
        }

        // If lazy is true then treeview items are populated on-demand. In other words, when lazy is true
        // the children for any given item are only populated when the item is selected. If lazy is
        // false then the entire tree is populated at once (and this can result in bad performance when
        // displaying large trees).
        public void DisplaySyntaxTree(SyntaxTree tree, SemanticModel model = null, bool lazy = true)
        {
            if (tree != null)
            {
                IsLazy = lazy;
                SyntaxTree = tree;
                SemanticModel = model;
                AddNode(null, SyntaxTree.GetRoot());
                legendButton.Visibility = Visibility.Visible;
            }
        }

        // If lazy is true then treeview items are populated on-demand. In other words, when lazy is true
        // the children for any given item are only populated when the item is selected. If lazy is
        // false then the entire tree is populated at once (and this can result in bad performance when
        // displaying large trees).
        public void DisplaySyntaxNode(SyntaxNode node, SemanticModel model = null, bool lazy = true)
        {
            if (node != null)
            {
                IsLazy = lazy;
                SyntaxTree = node.SyntaxTree;
                SemanticModel = model;
                AddNode(null, node);
                legendButton.Visibility = Visibility.Visible;
            }
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose position best matches the supplied position.
        public bool NavigateToBestMatch(int position, string kind = null,
            SyntaxCategory category = SyntaxCategory.None,
            bool highlightMatch = false, string highlightLegendDescription = null)
        {
            TreeViewItem match = null;

            if (treeView.HasItems && !_isNavigatingFromTreeToSource)
            {
                _isNavigatingFromSourceToTree = true;
                match = NavigateToBestMatch((TreeViewItem)treeView.Items[0], position, kind, category);
                _isNavigatingFromSourceToTree = false;
            }

            var matchFound = match != null;

            if (highlightMatch && matchFound)
            {
                match.Background = Brushes.Yellow;
                match.BorderBrush = Brushes.Black;
                match.BorderThickness = s_defaultBorderThickness;
                highlightLegendTextLabel.Visibility = Visibility.Visible;
                highlightLegendDescriptionLabel.Visibility = Visibility.Visible;
                if (!string.IsNullOrWhiteSpace(highlightLegendDescription))
                {
                    highlightLegendDescriptionLabel.Content = highlightLegendDescription;
                }
            }

            return matchFound;
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose span best matches the supplied span.
        public bool NavigateToBestMatch(int start, int length, string kind = null,
            SyntaxCategory category = SyntaxCategory.None,
            bool highlightMatch = false, string highlightLegendDescription = null)
        {
            return NavigateToBestMatch(new TextSpan(start, length), kind, category, highlightMatch, highlightLegendDescription);
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose span best matches the supplied span.
        public bool NavigateToBestMatch(TextSpan span, string kind = null,
            SyntaxCategory category = SyntaxCategory.None,
            bool highlightMatch = false, string highlightLegendDescription = null)
        {
            TreeViewItem match = null;

            if (treeView.HasItems && !_isNavigatingFromTreeToSource)
            {
                _isNavigatingFromSourceToTree = true;
                match = NavigateToBestMatch((TreeViewItem)treeView.Items[0], span, kind, category);
                _isNavigatingFromSourceToTree = false;
            }

            var matchFound = match != null;

            if (highlightMatch && matchFound)
            {
                match.Background = Brushes.Yellow;
                match.BorderBrush = Brushes.Black;
                match.BorderThickness = s_defaultBorderThickness;
                highlightLegendTextLabel.Visibility = Visibility.Visible;
                highlightLegendDescriptionLabel.Visibility = Visibility.Visible;
                if (!string.IsNullOrWhiteSpace(highlightLegendDescription))
                {
                    highlightLegendDescriptionLabel.Content = highlightLegendDescription;
                }
            }

            return matchFound;
        }

        public bool TryHandleEscape()
        {
            if (legendPopup.IsOpen)
            {
                legendPopup.IsOpen = false;
                legendButton.Focus();
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Private Helpers - TreeView Navigation
        // Collapse all items in the treeview except for the supplied item. The supplied item
        // is also expanded, selected and scrolled into view.
        private void CollapseEverythingBut(TreeViewItem item)
        {
            if (item != null)
            {
                DeepCollapse((TreeViewItem)treeView.Items[0]);
                ExpandPathTo(item);
                item.IsSelected = true;
                item.BringIntoView();
            }
        }

        // Collapse the supplied treeview item including all its descendants.
        private void DeepCollapse(TreeViewItem item)
        {
            if (item != null)
            {
                item.IsExpanded = false;
                foreach (TreeViewItem child in item.Items)
                {
                    DeepCollapse(child);
                }
            }
        }

        // Ensure that the supplied treeview item and all its ancestors are expanded.
        private void ExpandPathTo(TreeViewItem item)
        {
            if (item != null)
            {
                item.IsExpanded = true;
                ExpandPathTo(((SyntaxTag)item.Tag).ParentItem);
            }
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose position best matches the supplied position.
        private TreeViewItem NavigateToBestMatch(TreeViewItem current, int position, string kind = null,
            SyntaxCategory category = SyntaxCategory.None)
        {
            TreeViewItem match = null;

            if (current != null)
            {
                var currentTag = (SyntaxTag)current.Tag;
                if (currentTag.FullSpan.Contains(position))
                {
                    CollapseEverythingBut(current);

                    foreach (TreeViewItem item in current.Items)
                    {
                        match = NavigateToBestMatch(item, position, kind, category);
                        if (match != null)
                        {
                            break;
                        }
                    }

                    if (match == null && (kind == null || currentTag.Kind == kind) &&
                       (category == SyntaxCategory.None || category == currentTag.Category))
                    {
                        match = current;
                    }
                }
            }

            return match;
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose span best matches the supplied span.
        private TreeViewItem NavigateToBestMatch(TreeViewItem current, TextSpan span, string kind = null,
            SyntaxCategory category = SyntaxCategory.None)
        {
            TreeViewItem match = null;

            if (current != null)
            {
                var currentTag = (SyntaxTag)current.Tag;
                if (currentTag.FullSpan.Contains(span))
                {
                    if ((currentTag.Span == span || currentTag.FullSpan == span) && (kind == null || currentTag.Kind == kind))
                    {
                        CollapseEverythingBut(current);
                        match = current;
                    }
                    else
                    {
                        CollapseEverythingBut(current);

                        foreach (TreeViewItem item in current.Items)
                        {
                            match = NavigateToBestMatch(item, span, kind, category);
                            if (match != null)
                            {
                                break;
                            }
                        }

                        if (match == null && (kind == null || currentTag.Kind == kind) &&
                           (category == SyntaxCategory.None || category == currentTag.Category))
                        {
                            match = current;
                        }
                    }
                }
            }

            return match;
        }
        #endregion

        #region Private Helpers - TreeView Population
        // Helpers for populating the treeview.

        private void AddNodeOrToken(TreeViewItem parentItem, SyntaxNodeOrToken nodeOrToken)
        {
            if (nodeOrToken.IsNode)
            {
                AddNode(parentItem, nodeOrToken.AsNode());
            }
            else
            {
                AddToken(parentItem, nodeOrToken.AsToken());
            }
        }

        private void AddNode(TreeViewItem parentItem, SyntaxNode node)
        {
            var kind = node.GetKind();
            var tag = new SyntaxTag()
            {
                SyntaxNode = node,
                Category = SyntaxCategory.SyntaxNode,
                Span = node.Span,
                FullSpan = node.FullSpan,
                Kind = kind,
                ParentItem = parentItem
            };

            var item = new TreeViewItem()
            {
                Tag = tag,
                IsExpanded = false,
                Foreground = Brushes.Blue,
                Background = node.ContainsDiagnostics ? Brushes.Pink : Brushes.White,
                Header = tag.Kind + " " + node.Span.ToString()
            };

            if (SyntaxTree != null && node.ContainsDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(node))
                {
                    item.ToolTip += diagnostic.ToString() + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                _isNavigatingFromTreeToSource = true;

                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = node.GetType().Name;
                kindValueLabel.Content = kind;
                _propertyGrid.SelectedObject = node;

                item.IsExpanded = true;

                if (!_isNavigatingFromSourceToTree && SyntaxNodeNavigationToSourceRequested != null)
                {
                    SyntaxNodeNavigationToSourceRequested(node);
                }

                _isNavigatingFromTreeToSource = false;
                e.Handled = true;
            });

            item.Expanded += new RoutedEventHandler((sender, e) =>
            {
                if (item.Items.Count == 1 && item.Items[0] == null)
                {
                    // Remove placeholder child and populate real children.
                    item.Items.RemoveAt(0);
                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        AddNodeOrToken(item, child);
                    }
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
            }
            else
            {
                parentItem.Items.Add(item);
            }

            if (node.ChildNodesAndTokens().Count > 0)
            {
                if (IsLazy)
                {
                    // Add placeholder child to indicate that real children need to be populated on expansion.
                    item.Items.Add(null);
                }
                else
                {
                    // Recursively populate all descendants.
                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        AddNodeOrToken(item, child);
                    }
                }
            }
        }

        private void AddToken(TreeViewItem parentItem, SyntaxToken token)
        {
            var kind = token.GetKind();
            var tag = new SyntaxTag()
            {
                SyntaxToken = token,
                Category = SyntaxCategory.SyntaxToken,
                Span = token.Span,
                FullSpan = token.FullSpan,
                Kind = kind,
                ParentItem = parentItem
            };

            var item = new TreeViewItem()
            {
                Tag = tag,
                IsExpanded = false,
                Foreground = Brushes.DarkGreen,
                Background = token.ContainsDiagnostics ? Brushes.Pink : Brushes.White,
                Header = tag.Kind + " " + token.Span.ToString()
            };

            if (SyntaxTree != null && token.ContainsDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(token))
                {
                    item.ToolTip += diagnostic.ToString() + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                _isNavigatingFromTreeToSource = true;

                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = token.GetType().Name;
                kindValueLabel.Content = kind;
                _propertyGrid.SelectedObject = token;

                item.IsExpanded = true;

                if (!_isNavigatingFromSourceToTree && SyntaxTokenNavigationToSourceRequested != null)
                {
                    SyntaxTokenNavigationToSourceRequested(token);
                }

                _isNavigatingFromTreeToSource = false;
                e.Handled = true;
            });

            item.Expanded += new RoutedEventHandler((sender, e) =>
            {
                if (item.Items.Count == 1 && item.Items[0] == null)
                {
                    // Remove placeholder child and populate real children.
                    item.Items.RemoveAt(0);
                    foreach (var trivia in token.LeadingTrivia)
                    {
                        AddTrivia(item, trivia, true);
                    }

                    foreach (var trivia in token.TrailingTrivia)
                    {
                        AddTrivia(item, trivia, false);
                    }
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
            }
            else
            {
                parentItem.Items.Add(item);
            }

            if (token.HasLeadingTrivia || token.HasTrailingTrivia)
            {
                if (IsLazy)
                {
                    // Add placeholder child to indicate that real children need to be populated on expansion.
                    item.Items.Add(null);
                }
                else
                {
                    // Recursively populate all descendants.
                    foreach (var trivia in token.LeadingTrivia)
                    {
                        AddTrivia(item, trivia, true);
                    }

                    foreach (var trivia in token.TrailingTrivia)
                    {
                        AddTrivia(item, trivia, false);
                    }
                }
            }
        }

        private void AddTrivia(TreeViewItem parentItem, SyntaxTrivia trivia, bool isLeadingTrivia)
        {
            var kind = trivia.GetKind();
            var tag = new SyntaxTag()
            {
                SyntaxTrivia = trivia,
                Category = SyntaxCategory.SyntaxTrivia,
                Span = trivia.Span,
                FullSpan = trivia.FullSpan,
                Kind = kind,
                ParentItem = parentItem
            };

            var item = new TreeViewItem()
            {
                Tag = tag,
                IsExpanded = false,
                Foreground = Brushes.Maroon,
                Background = trivia.ContainsDiagnostics ? Brushes.Pink : Brushes.White,
                Header = (isLeadingTrivia ? "Lead: " : "Trail: ") + tag.Kind + " " + trivia.Span.ToString()
            };

            if (SyntaxTree != null && trivia.ContainsDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(trivia))
                {
                    item.ToolTip += diagnostic.ToString() + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                _isNavigatingFromTreeToSource = true;

                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = trivia.GetType().Name;
                kindValueLabel.Content = kind;
                _propertyGrid.SelectedObject = trivia;

                item.IsExpanded = true;

                if (!_isNavigatingFromSourceToTree && SyntaxTriviaNavigationToSourceRequested != null)
                {
                    SyntaxTriviaNavigationToSourceRequested(trivia);
                }

                _isNavigatingFromTreeToSource = false;
                e.Handled = true;
            });

            item.Expanded += new RoutedEventHandler((sender, e) =>
            {
                if (item.Items.Count == 1 && item.Items[0] == null)
                {
                    // Remove placeholder child and populate real children.
                    item.Items.RemoveAt(0);
                    AddNode(item, trivia.GetStructure());
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
                typeTextLabel.Visibility = Visibility.Hidden;
                kindTextLabel.Visibility = Visibility.Hidden;
                typeValueLabel.Content = string.Empty;
                kindValueLabel.Content = string.Empty;
            }
            else
            {
                parentItem.Items.Add(item);
            }

            if (trivia.HasStructure)
            {
                if (IsLazy)
                {
                    // Add placeholder child to indicate that real children need to be populated on expansion.
                    item.Items.Add(null);
                }
                else
                {
                    // Recursively populate all descendants.
                    AddNode(item, trivia.GetStructure());
                }
            }
        }
        #endregion

        #region Private Helpers - Other
        private void DisplaySymbolInPropertyGrid(ISymbol symbol)
        {
            if (symbol == null)
            {
                typeTextLabel.Visibility = Visibility.Hidden;
                kindTextLabel.Visibility = Visibility.Hidden;
                typeValueLabel.Content = string.Empty;
                kindValueLabel.Content = string.Empty;
            }
            else
            {
                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = symbol.GetType().Name;
                kindValueLabel.Content = symbol.Kind.ToString();
            }

            _propertyGrid.SelectedObject = symbol;
        }

        private static TreeViewItem FindTreeViewItem(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            return (TreeViewItem)source;
        }
        #endregion

        #region Event Handlers
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Restore the regular colors on the newly unselected item
            var previousSelection = _currentSelection;
            if (previousSelection != null)
            {
                previousSelection.Foreground = _currentSelectionUnselectedForeground;
            }

            // Remember the newly selected item's specified foreground color and then clear it to
            // allow the ControlTemplate to correctly set contrasting colors.
            _currentSelection = (TreeViewItem)treeView.SelectedItem;
            if (_currentSelection != null)
            {
                _currentSelectionUnselectedForeground = _currentSelection.Foreground;
                _currentSelection.ClearValue(TreeViewItem.ForegroundProperty);
            }
            windowsFormsHost.Child = _tabStopPanel;
        }

        private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindTreeViewItem((DependencyObject)e.OriginalSource);

            if (item != null)
            {
                item.Focus();
            }
        }

        private void TreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_currentSelection == null)
            {
                e.Handled = true;
                return;
            }

            var directedSyntaxGraphEnabled =
                (SyntaxNodeDirectedGraphRequested != null) &&
                (SyntaxTokenDirectedGraphRequested != null) &&
                (SyntaxTriviaDirectedGraphRequested != null);

            var symbolDetailsEnabled =
                (SemanticModel != null) &&
                (((SyntaxTag)_currentSelection.Tag).Category == SyntaxCategory.SyntaxNode);

            if ((!directedSyntaxGraphEnabled) && (!symbolDetailsEnabled))
            {
                e.Handled = true;
            }
            else
            {
                directedSyntaxGraphMenuItem.Visibility = directedSyntaxGraphEnabled ? Visibility.Visible : Visibility.Collapsed;
                symbolDetailsMenuItem.Visibility = symbolDetailsEnabled ? Visibility.Visible : Visibility.Collapsed;
                typeSymbolDetailsMenuItem.Visibility = symbolDetailsMenuItem.Visibility;
                convertedTypeSymbolDetailsMenuItem.Visibility = symbolDetailsMenuItem.Visibility;
                aliasSymbolDetailsMenuItem.Visibility = symbolDetailsMenuItem.Visibility;
                constantValueDetailsMenuItem.Visibility = symbolDetailsMenuItem.Visibility;
                menuItemSeparator1.Visibility = symbolDetailsMenuItem.Visibility;
                menuItemSeparator2.Visibility = symbolDetailsMenuItem.Visibility;
            }
        }

        private void DirectedSyntaxGraphMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection != null)
            {
                var currentTag = (SyntaxTag)_currentSelection.Tag;

                if (currentTag.Category == SyntaxCategory.SyntaxNode && SyntaxNodeDirectedGraphRequested != null)
                {
                    SyntaxNodeDirectedGraphRequested(currentTag.SyntaxNode);
                }
                else if (currentTag.Category == SyntaxCategory.SyntaxToken && SyntaxTokenDirectedGraphRequested != null)
                {
                    SyntaxTokenDirectedGraphRequested(currentTag.SyntaxToken);
                }
                else if (currentTag.Category == SyntaxCategory.SyntaxTrivia && SyntaxTriviaDirectedGraphRequested != null)
                {
                    SyntaxTriviaDirectedGraphRequested(currentTag.SyntaxTrivia);
                }
            }
        }

        private void SymbolDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection == null)
            {
                e.Handled = true;
                return;
            }

            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var symbol = SemanticModel.GetSymbolInfo(currentTag.SyntaxNode).Symbol;
                if (symbol == null)
                {
                    symbol = SemanticModel.GetDeclaredSymbol(currentTag.SyntaxNode);
                }

                if (symbol == null)
                {
                    symbol = SemanticModel.GetPreprocessingSymbolInfo(currentTag.SyntaxNode).Symbol;
                }

                DisplaySymbolInPropertyGrid(symbol);
            }
        }

        private void TypeSymbolDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection == null)
            {
                e.Handled = true;
                return;
            }

            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var symbol = SemanticModel.GetTypeInfo(currentTag.SyntaxNode).Type;
                DisplaySymbolInPropertyGrid(symbol);
            }
        }

        private void ConvertedTypeSymbolDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection == null)
            {
                e.Handled = true;
                return;
            }

            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var symbol = SemanticModel.GetTypeInfo(currentTag.SyntaxNode).ConvertedType;
                DisplaySymbolInPropertyGrid(symbol);
            }
        }

        private void AliasSymbolDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection == null)
            {
                e.Handled = true;
                return;
            }

            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var symbol = SemanticModel.GetAliasInfo(currentTag.SyntaxNode);
                DisplaySymbolInPropertyGrid(symbol);
            }
        }

        private void ConstantValueDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection == null)
            {
                e.Handled = true;
                return;
            }

            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var value = SemanticModel.GetConstantValue(currentTag.SyntaxNode);
                kindTextLabel.Visibility = Visibility.Hidden;
                kindValueLabel.Content = string.Empty;

                if (!value.HasValue)
                {
                    typeTextLabel.Visibility = Visibility.Hidden;
                    typeValueLabel.Content = string.Empty;
                    _propertyGrid.SelectedObject = null;
                }
                else
                {
                    typeTextLabel.Visibility = Visibility.Visible;
                    typeValueLabel.Content = value.Value?.GetType().Name ?? "<null>";
                    _propertyGrid.SelectedObject = value;
                }
            }
        }

        private void LegendButton_Click(object sender, RoutedEventArgs e)
        {
            legendPopup.IsOpen = !legendPopup.IsOpen;
        }

        private void GenerateIOperationTestMenuItem_Click(object sender, RoutedEventArgs e)
        {
            GenerateTest(false);
        }

        private void GenerateDataflowTestMenuItem_Click(object sender, RoutedEventArgs e)
        {
            GenerateTest(true);
        }

        private void GenerateTest(bool useDataflow)
        {
            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var syntaxNode = currentTag.SyntaxNode;
                var syntaxTree = syntaxNode.SyntaxTree;
                string testText;
                try
                {
                    if (syntaxNode.Language == LanguageNames.CSharp)
                    {
                        testText = CreateCSharpIOperationUnitTest(SemanticModel, syntaxTree, syntaxNode, useDataflow);
                    }
                    else
                    {
                        testText = CreateVBIOperationUnitTest(SemanticModel, syntaxTree, syntaxNode, useDataflow);
                    }
                }
                catch (Exception ex)
                {
                    testText = ex.ToString();
                }

                _textBox.Text = testText;
                windowsFormsHost.Child = _textBox;
            }
        }

        #endregion



        #region IOperation Test Generation

        private string CreateCSharpIOperationUnitTest(SemanticModel model,
                                                      SyntaxTree syntaxTree,
                                                      SyntaxNode syntaxNode,
                                                      bool useDataflow)
        {
            string sourceCode = syntaxTree.GetText().ToString();
            TextSpan nodeSpan = syntaxNode.Span;
            int newSpanStart = nodeSpan.Start;
            int newSpanLength = nodeSpan.Length;

            const string bindStartComment = "/*<bind>*/";
            const string bindEndComment = "/*</bind>*/";
            sourceCode = sourceCode.Insert(nodeSpan.End, bindEndComment);
            sourceCode = sourceCode.Insert(nodeSpan.Start, bindStartComment);
            newSpanStart += bindStartComment.Length;
            sourceCode = Environment.NewLine + sourceCode; // preprend a new line.
            newSpanStart += Environment.NewLine.Length;

            // Replace escaped quotes with unescaped ones.
            var newTree = syntaxTree.WithChangedText(SourceText.From(sourceCode));
            var compilation = model.Compilation.ReplaceSyntaxTree(syntaxTree, newTree);
            model = compilation.GetSemanticModel(newTree);

            // Find the given syntax node in the newTree using the new span and original node's syntaxKind
            var newSpan = new TextSpan(newSpanStart, newSpanLength);
            syntaxNode = FindNode(newTree, newSpan, syntaxNode.RawKind);

            var expectedOutput = GetExpectedOutput(model, syntaxNode, useDataflow);

            // filter out hidden diagnostics and CS5001 (no entry point)
            var expectedDiagnostics = model.Compilation.GetDiagnostics().WhereAsArray(d => d.Severity != DiagnosticSeverity.Hidden && d.Id != "CS5001");

            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
            string outerIndent = "        ";
            string indent = outerIndent + "    ";
            string diagnosticIndent = indent + "    ";

            writer.Write(outerIndent);
            writer.WriteLine($"[CompilerTrait(CompilerFeature.IOperation{(useDataflow ?", CompilerFeature.Dataflow" : "")})]");
            writer.Write(outerIndent);
            writer.WriteLine("[Fact]");
            writer.Write(outerIndent);
            writer.WriteLine("public void XXXX()");
            writer.Write(outerIndent);
            writer.WriteLine("{");

            writer.Write(indent);
            writer.Write("string source = @\"");
            writer.WriteLine(sourceCode.Replace("\"", "\"\"")); // escape quotes.
            writer.WriteLine("\";");

            writer.Write(indent);
            if (expectedDiagnostics.IsEmpty)
            {
                writer.WriteLine("var expectedDiagnostics = DiagnosticDescription.None;");
            }
            else
            {
                writer.WriteLine("var expectedDiagnostics = new DiagnosticDescription[] {");
                writer.WriteLine(GetCSharpDiagnosticDescriptionString(expectedDiagnostics, diagnosticIndent));
                writer.Write(indent);
                writer.WriteLine("};");
            }
            writer.WriteLine();

            var variableName = useDataflow ? "expectedFlowGraph" : "expectedOperationTree";
            var functionCall = useDataflow ? "VerifyFlowGraphAndDiagnosticsForTest" : "VerifyOperationTreeAndDiagnosticsForTest";

            writer.Write(indent);
            writer.WriteLine($"string {variableName} = @\"");
            writer.Write(expectedOutput.Replace("\"", "\"\"")); // escape quotes.
            writer.WriteLine("\";");

            writer.Write(indent);
            writer.WriteLine($"{functionCall}<{syntaxNode.GetType().Name}>(source, {variableName}, expectedDiagnostics);");

            writer.Write(outerIndent);
            writer.WriteLine("}");
            writer.WriteLine();

            return writer.ToString();
        }

        private string CreateVBIOperationUnitTest(
            SemanticModel model,
            SyntaxTree syntaxTree,
            SyntaxNode syntaxNode,
            bool useDataflow)
        {
            string sourceCode = syntaxTree.GetText().ToString();
            TextSpan nodeSpan = syntaxNode.Span;
            int newSpanStart = nodeSpan.Start;
            int newSpanLength = nodeSpan.Length;

            string bindText = syntaxNode.ToString();

            // For multiline syntax nodes, we only include the first text line of the node in the bind comment.
            int indexOfEolInBindText = bindText.IndexOf("\r");
            if (indexOfEolInBindText > 0)
            {
                bindText = bindText.Substring(0, indexOfEolInBindText);
            }

            var bindCommentText = "'BIND:\"" + bindText + "\"";
            int indexOfEoln = sourceCode.IndexOf("\r", nodeSpan.Start);
            sourceCode = sourceCode.Insert(indexOfEoln, bindCommentText);
            if (indexOfEolInBindText > 0)
            {
                // Bind comment was added within the given syntaxNode, so the span length of the syntaxNode in newText should be adjusted.
                newSpanLength += bindCommentText.Length;
            }

            sourceCode = Environment.NewLine + sourceCode; // prepend a new line.
            newSpanStart += Environment.NewLine.Length;

            var newTree = syntaxTree.WithChangedText(SourceText.From(sourceCode));
            var compilation = model.Compilation.ReplaceSyntaxTree(syntaxTree, newTree);
            model = compilation.GetSemanticModel(newTree);

            // Find the given syntax node in the newTree using the new span and original node's syntaxKind
            var newSpan = new TextSpan(newSpanStart, newSpanLength);
            syntaxNode = FindNode(newTree, newSpan, syntaxNode.RawKind);

            var expectedOutput = GetExpectedOutput(model, syntaxNode, useDataflow);

            // Filter out hidden diagnostics and BC30420 (no entry point)
            var expectedDiagnostics = model.Compilation.GetDiagnostics().WhereAsArray(d => d.Severity != DiagnosticSeverity.Hidden && d.Id != "BC30420");

            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
            string outerIndent = "        ";
            string indent = outerIndent + "    ";
            string diagnosticIndent = indent + "    ";

            writer.Write(outerIndent);
            writer.WriteLine($"<CompilerTrait(CompilerFeature.IOperation{(useDataflow ? ", CompilerFeature.Dataflow" : "")})>");
            writer.Write(outerIndent);
            writer.WriteLine("<Fact()>");
            writer.Write(outerIndent);
            writer.WriteLine("Public Sub XXXX()");

            writer.Write(indent);
            writer.Write("Dim source = <![CDATA[");
            writer.Write(sourceCode);
            writer.WriteLine("]]>.Value");
            writer.WriteLine();

            writer.Write(indent);
            if (expectedDiagnostics.IsDefaultOrEmpty)
            {
                writer.WriteLine("Dim expectedDiagnostics = String.Empty");
            }
            else
            {
                writer.WriteLine("Dim expectedDiagnostics = <![CDATA[");
                var expectedDiagnosticsString = GetAssertTheseDiagnosticsString(expectedDiagnostics, suppressInfos: true);
                writer.Write(expectedDiagnosticsString);
                writer.WriteLine("]]>.Value");
            }

            var variableName = useDataflow ? "expectedFlowGraph" : "expectedOperationTree";
            var functionCall = useDataflow ? "VerifyFlowGraphAndDiagnosticsForTest" : "VerifyOperationTreeAndDiagnosticsForTest";

            writer.WriteLine();
            writer.Write(indent);
            writer.WriteLine($"Dim {variableName} = <![CDATA[");
            writer.Write(expectedOutput);
            writer.WriteLine("]]>.Value");
            writer.WriteLine();

            writer.Write(indent);
            writer.WriteLine($"{functionCall}(Of {syntaxNode.GetType().Name})(source, {variableName}, expectedDiagnostics)");

            writer.Write(outerIndent);
            writer.WriteLine("End Sub");
            writer.WriteLine();

            return writer.ToString();
        }

        private string GetCSharpDiagnosticDescriptionString(ImmutableArray<Diagnostic> diagnostics, string diagnosticIndent)
        {
            var builder = new StringBuilder();
            var first = true;
            foreach (var diagnostic in diagnostics)
            {
                if (!first)
                {
                    builder.AppendLine(",");
                }

                first = false;

                var comment = GetCSharpDiagnosticMessageComment(diagnostic, diagnosticIndent);
                builder.AppendLine(diagnosticIndent + comment);

                // Append diagnostic
                var diagnosticDescription = new DiagnosticDescription(diagnostic, errorCodeOnly: false);
                builder.Append(diagnosticIndent);
                builder.Append(diagnosticDescription.ToString());
            }

            return builder.ToString();
        }

        private static string GetCSharpDiagnosticMessageComment(Diagnostic diagnostic, string diagnosticIndent)
        {
            // Append diagnostic message as comment.
            var message = diagnostic.GetMessage();
            var index = message.IndexOf(Environment.NewLine);
            if (index > 0)
            {
                message = message.Substring(0, index);
            }

            var comment = $"// {diagnostic.Id}: {message}";
            var location = diagnostic.Location;
            if (location != Location.None && location.IsInSource)
            {
                var sourceLine = location.SourceTree.GetText().Lines.GetLineFromPosition(location.SourceSpan.Start).ToString();
                comment += $"{Environment.NewLine}{diagnosticIndent}// {sourceLine}";
            }

            return comment;
        }

        private SyntaxNode FindNode(SyntaxTree tree, TextSpan span, int rawSyntaxKind)
        {
            // There might be multiple nodes with the given span, so lets get with the innermost node if there is a tie.
            var syntaxNode = tree.GetRoot().FindNode(span, getInnermostNodeForTie: true);
            Debug.Assert(syntaxNode != null);

            while (syntaxNode.RawKind != rawSyntaxKind && syntaxNode.Parent != null)
            {
                syntaxNode = syntaxNode.Parent;
            }

            Debug.Assert(syntaxNode != null);
            Debug.Assert(syntaxNode.RawKind == rawSyntaxKind);
            return syntaxNode;
        }

        private static string GetExpectedOutput(SemanticModel semanticModel, SyntaxNode syntaxNode, bool flowGraph)
        {
            var operation = semanticModel.GetOperation(syntaxNode);
            if (operation == null)
            {
                return string.Empty;
            }
            else if (flowGraph)
            {
                ControlFlowGraph graph;
                switch (operation.Kind)
                {
                    case OperationKind.MethodBodyOperation:
                        graph = SemanticModel.GetControlFlowGraph((IMethodBodyOperation)operation);
                        break;

                    case OperationKind.FieldInitializer:
                        graph = SemanticModel.GetControlFlowGraph((IFieldInitializerOperation)operation);
                        break;

                    case OperationKind.PropertyInitializer:
                        graph = SemanticModel.GetControlFlowGraph((IPropertyInitializerOperation)operation);
                        break;

                    case OperationKind.ParameterInitializer:
                        graph = SemanticModel.GetControlFlowGraph((IParameterInitializerOperation)operation);
                        break;

                    case OperationKind.ConstructorBodyOperation:
                        graph = SemanticModel.GetControlFlowGraph((IConstructorBodyOperation)operation);
                        break;

                    case OperationKind.Block:
                        graph = SemanticModel.GetControlFlowGraph((IBlockOperation)operation);
                        break;

                    default:
                        throw new ArgumentException($"Cannot create graph for node {operation.Kind} - {operation.Syntax.ToString()}");
                }
                return ControlFlowGraphVerifier.GetFlowGraph(semanticModel.Compilation, graph);
            }
            else
            {
                return OperationTreeVerifier.GetOperationTree(semanticModel.Compilation, operation);
            }
        }

        private static string GetAssertTheseDiagnosticsString(ImmutableArray<Diagnostic> allDiagnostics, bool suppressInfos)
        {
            var diagsAndIndexes = new(Diagnostic diagnostic, int index)[allDiagnostics.Length];
            for (var i = 0; i < allDiagnostics.Length; i++)
            {
                diagsAndIndexes[i] = (allDiagnostics[i], i);
            }

            Array.Sort(diagsAndIndexes, CompareErrors);

            var builder = new StringBuilder();
            foreach (var diag in diagsAndIndexes)
            {
                if (!suppressInfos || diag.diagnostic.Severity > DiagnosticSeverity.Info)
                {
                    builder.Append(ErrorText(diag.diagnostic));
                }
            }

            return builder.ToString();
        }

        private static int CompareErrors((Diagnostic diagnostic, int index) diagAndIndex1, (Diagnostic diagnostic, int index) diagAndIndex2)
        {
            var diag1 = diagAndIndex1.diagnostic;
            var diag2 = diagAndIndex2.diagnostic;
            var loc1 = diag1.Location;
            var loc2 = diag2.Location;

            if (!(loc1.IsInSource || loc1.IsInMetadata))
            {
                if (!(loc2.IsInSource || loc2.IsInMetadata))
                {
                    if (diag1.Id != diag2.Id) return diag1.Id.CompareTo(diag2.Id);
                    return diag1.GetMessage(EnsureEnglishUICulture.PreferredOrNull).CompareTo(diag2.GetMessage(EnsureEnglishUICulture.PreferredOrNull));
                }
                else return -1;
            }
            else if (!(loc2.IsInSource || loc2.IsInMetadata))
            {
                return 1;
            }
            else if (loc1.IsInSource && loc2.IsInSource)
            {
                var sourceTree1 = loc1.SourceTree;
                var sourceTree2 = loc2.SourceTree;

                if (sourceTree1.FilePath != sourceTree2.FilePath)
                {
                    return sourceTree1.FilePath.CompareTo(sourceTree2.FilePath);
                }
                else if (loc1.SourceSpan.Start < loc2.SourceSpan.Start) return -1;
                else if (loc1.SourceSpan.Start > loc2.SourceSpan.Start) return 1;
                else if (loc2.SourceSpan.Length < loc2.SourceSpan.Length) return -1;
                else if (loc2.SourceSpan.Length > loc2.SourceSpan.Length) return 1;
                else if (diag1.Id != diag2.Id) return diag1.Id.CompareTo(diag2.Id);
                else return diag1.GetMessage(EnsureEnglishUICulture.PreferredOrNull).CompareTo(diag2.GetMessage(EnsureEnglishUICulture.PreferredOrNull));
            }
            else if (loc1.IsInMetadata && loc2.IsInMetadata)
            {
                var name1 = loc1.MetadataModule.ContainingAssembly.Name;
                var name2 = loc2.MetadataModule.ContainingAssembly.Name;
                if (name1 != name2) return name1.CompareTo(name2);
                else if (diag1.Id != diag2.Id) return diag1.Id.CompareTo(diag2.Id);
                else return diag1.GetMessage(EnsureEnglishUICulture.PreferredOrNull).CompareTo(diag2.GetMessage(EnsureEnglishUICulture.PreferredOrNull));
            }
            else if (loc1.IsInSource)
            {
                return -1;
            }
            else if (loc2.IsInSource)
            {
                return 1;
            }
            else
            {
                return diagAndIndex1.index - diagAndIndex2.index;
            }
        }

        private static string ErrorText(Diagnostic e)
        {
            (string, int) GetLineText(SourceText text, int position)
            {
                var textLine = text.Lines.GetLineFromPosition(position);
                var offsetInLine = position - textLine.Start;
                return (textLine.ToString(), offsetInLine);
            }

            var message = e.Id + ": " + e.GetMessage(EnsureEnglishUICulture.PreferredOrNull);
            if (e.Location.IsInSource)
            {
                var sourceLocation = e.Location;
                var (lineText, offsetInLine) = GetLineText(sourceLocation.SourceTree.GetText(), sourceLocation.SourceSpan.Start);
                return message + Environment.NewLine +
                    lineText + Environment.NewLine +
                    new string(' ', offsetInLine) +
                    new string('~', Math.Max(Math.Min(sourceLocation.SourceSpan.Length, lineText.Length - offsetInLine + 1), 1)) + Environment.NewLine;
            }
            else if (e.Location.IsInMetadata)
            {
                return message + Environment.NewLine +
                    string.Format($"in metadata assembly '{e.Location.MetadataModule.ContainingAssembly.Identity.Name}'") + Environment.NewLine;
            }
            else return message + Environment.NewLine;
        }
        #endregion
    }

    public static class ImmutableArrayExtensions
    {
        public static ImmutableArray<T> WhereAsArray<T>(this ImmutableArray<T> array, Func<T, bool> predicate)
        {
            Debug.Assert(!array.IsDefault);

            var builder = ImmutableArray.CreateBuilder<T>();

            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                var a = array[i];
                if (predicate(a))
                {
                    builder.Add(a);
                }
            }

            if (builder != null)
            {
                return builder.ToImmutableArray();
            }
            else
            {
                return ImmutableArray<T>.Empty;
            }
        }
    }
}
