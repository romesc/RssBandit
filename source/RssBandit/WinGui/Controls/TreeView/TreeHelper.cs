#region CVS Version Header

/*
 * $Id$
 * Last modified by $Author$
 * Last modified at $Date$
 * $Revision$
 */

#endregion

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Serialization;
using Infragistics.Win;
using Infragistics.Win.UltraWinToolTip;
using Infragistics.Win.UltraWinTree;
using NewsComponents;
using NewsComponents.Feed;
using NewsComponents.Utils;
using RssBandit.Common.Logging;
using RssBandit.WinGui.Interfaces;
using RssBandit.WinGui.Utility;
using RssBandit.Xml;
using SortOrder=System.Windows.Forms.SortOrder;
using RssBandit.Resources;
using System.Collections.Generic;

namespace RssBandit.WinGui.Controls
{

    #region TreeHelper

    internal class TreeHelper
    {
        #region CreateCategoryHive

        /// <summary>
        /// Traverse down the tree on the path defined by 'category' 
        /// starting with 'startNode' and create nodes of type FeedNodeType.Category.
        /// </summary>
        /// <param name="startNode">FeedTreeNodeBase to start with</param>
        /// <param name="category">A category path, e.g. 'Category1\SubCategory1'.</param>
        /// <param name="contextMenu">Category Node context Menu</param>
        /// <returns>The top category node.</returns>
        /// <remarks>If one category in the path is not found, it will be created.</remarks>
        public static TreeFeedsNodeBase CreateCategoryHive(TreeFeedsNodeBase startNode, string category,
                                                           ContextMenu contextMenu)
        {
            return CreateCategoryHive(startNode, category, contextMenu, FeedNodeType.Category);
        }

        /// <summary>
        /// Traverse down the tree on the path defined by 'category' 
        /// starting with 'startNode'.
        /// </summary>
        /// <param name="startNode">FeedTreeNodeBase to start with</param>
        /// <param name="category">A category path, e.g. 'Category1\SubCategory1'.</param>
        /// <param name="contextMenu">Category Node context Menu</param>
        /// <param name="categoryNodeType">FeedNodeType (should be one of the category types)</param>
        /// <returns>The top category node.</returns>
        /// <remarks>If one category in the path is not found, it will be created.</remarks>
        public static TreeFeedsNodeBase CreateCategoryHive(TreeFeedsNodeBase startNode, string category,
                                                           ContextMenu contextMenu, FeedNodeType categoryNodeType)
        {
            if (string.IsNullOrEmpty(category) || startNode == null) return startNode;

            string[] catHives = category.Split(FeedSource.CategorySeparator.ToCharArray());
            bool wasNew = false;
            int nodeImageIndex, expandedNodeImageIndex;

            switch (categoryNodeType)
            {
                case FeedNodeType.FinderCategory:
                    nodeImageIndex = Resource.SubscriptionTreeImage.FinderCategory;
                    expandedNodeImageIndex = Resource.SubscriptionTreeImage.FinderCategoryExpanded;
                    break;
                default:
                    nodeImageIndex = Resource.SubscriptionTreeImage.SubscriptionsCategory;
                    expandedNodeImageIndex = Resource.SubscriptionTreeImage.SubscriptionsCategoryExpanded;
                    break;
            }

            foreach (var catHive in catHives)
            {
                TreeFeedsNodeBase n = !wasNew ? FindChildNode(startNode, catHive, categoryNodeType) : null;

                if (n == null)
                {
                    switch (categoryNodeType)
                    {
                        case FeedNodeType.FinderCategory:
                            n = new FinderCategoryNode(catHive, nodeImageIndex, expandedNodeImageIndex, contextMenu);
                            break;
                        default:
                            n = new CategoryNode(catHive, nodeImageIndex, expandedNodeImageIndex, contextMenu);
                            break;
                    }

                    startNode.Nodes.Add(n);
                    wasNew = true; // shorten search
                }

                startNode = n;
            } //foreach

            return startNode;
        }

        #endregion

		/// <summary>
		/// Gets the topmost of Parents of the node.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns></returns>
		public static TreeFeedsNodeBase ParentRootNode(TreeFeedsNodeBase node)
		{
			if (node == null)
				return null;

			while (node.Parent != null)
				node = node.Parent;

			return node;
		}

        /// <summary>
        /// Find a direct child node.
        /// </summary>
        /// <param name="n"></param>
        /// <param name="text"></param>
        /// <param name="nType"></param>
        /// <returns></returns>
        public static TreeFeedsNodeBase FindChildNode(TreeFeedsNodeBase n, string text, FeedNodeType nType)
        {
            if (n == null || text == null) return null;
            text = text.Trim();

            for (TreeFeedsNodeBase t = n.FirstNode; t != null; t = t.NextNode)
            {
                if (t.Type == nType && String.Compare(t.Text, text, false, CultureInfo.CurrentUICulture) == 0)
                    // node names are usually english or client locale
                    return t;
            }
            return null;
        }

        /// <summary>
        /// Locates a category node and returns null if it wasn't found. 
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="category"></param>
        /// <returns>The requested category node or null if it was not found. </returns>
        public static TreeFeedsNodeBase FindCategoryNode(TreeFeedsNodeBase startNode, string category)
        {
            if (string.IsNullOrEmpty(category) || startNode == null) return startNode;

            string[] catHives = category.Split(FeedSource.CategorySeparator.ToCharArray());

            foreach (var catHive in catHives)
            {
                if (startNode != null)
                {
                    startNode = FindChildNode(startNode, catHive, FeedNodeType.Category);
                }
                else
                {
                    startNode = null;
                    break;
                }
            } //foreach

            return startNode;
        }

        /// <summary>
        /// Determines whether <paramref name="nodeToTest"/> is a child node of the specified parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeToTest">The node to test.</param>
        /// <returns>
        /// 	<c>true</c> if [is child node] [the specified parent]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsChildNode(TreeFeedsNodeBase parent, TreeFeedsNodeBase nodeToTest)
        {
            if (parent != null && nodeToTest != null)
            {
                return parent.IsAncestorOf(nodeToTest);
            }
            return false;
        }

		/// <summary>
		/// Finds the node.
		/// </summary>
		/// <param name="startNodes">The start nodes.</param>
		/// <param name="f">The f.</param>
		/// <returns></returns>
		public static TreeFeedsNodeBase FindNode(IEnumerable<TreeFeedsNodeBase> startNodes, INewsFeed f)
		{
			if (f == null) return null;
			TreeFeedsNodeBase assocFeedsNode = f.Tag as TreeFeedsNodeBase;
			if (assocFeedsNode != null)
				return assocFeedsNode;
			foreach (TreeFeedsNodeBase node in startNodes)
			{
				assocFeedsNode = FindNode(node, f);
				if (assocFeedsNode != null)
					return assocFeedsNode;
			}
			return null;
		}

        /// <summary>
        /// A helper method that locates the tree node containing the feed 
        /// that an INewsItem object belongs to. 
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="item">The RSS item</param>
        /// <returns>The tree node this object belongs to or null if 
        /// it can't be found</returns>
        public static TreeFeedsNodeBase FindNode(TreeFeedsNodeBase startNode, INewsItem item)
        {
            return FindNode(startNode, item.Feed);
        }

        /// <summary>
        /// Overloaded helper method that locates the tree node containing the feed. 
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="f">The FeedsFeed</param>
        /// <returns>The tree node this object belongs to or null if 
        /// it can't be found</returns>
        public static TreeFeedsNodeBase FindNode(TreeFeedsNodeBase startNode, INewsFeed f)
        {
            var assocFeedsNode = f.Tag as TreeFeedsNodeBase;
            if (assocFeedsNode != null)
                return assocFeedsNode;

            return FindNode(startNode, f.link);
        }

        /// <summary>
        /// Overloaded helper method that locates the tree node containing the feed. 
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="feedUrl">The Feed Url</param>
        /// <returns>The tree node this object belongs to or null if 
        /// it can't be found</returns>
        public static TreeFeedsNodeBase FindNode(TreeFeedsNodeBase startNode, string feedUrl)
        {
            if (feedUrl == null || feedUrl.Trim().Length == 0)
                return null;

            TreeFeedsNodeBase ownernode = null;

            if (startNode != null)
            {
                if (feedUrl.Equals(startNode.DataKey))
                {
                    return startNode;
                }

                foreach (TreeFeedsNodeBase t in startNode.Nodes)
                {
                    if (feedUrl.Equals(t.DataKey) &&
                        (t.Type != FeedNodeType.Root && t.Type != FeedNodeType.Category))
                    {
                        ownernode = t;
                        break;
                    }

                    if (t.Nodes.Count > 0)
                    {
                        ownernode = FindNode(t, feedUrl);
                        if (ownernode != null)
                            break;
                    }
                }
            }
            return ownernode;
        }

        #region ActivateNode 

        /// <summary>
        /// Gets the new node to activate. We assume, the current node
        /// provided is about to be removed/set invisible.
        /// </summary>
        /// <param name="current">The current node.</param>
        /// <returns></returns>
        /// <remarks>Used algorithm:
        /// We try to select the next sibling of current node. If there is no one,
        /// we take the previous sibling. If there is again no one, we take the parent
        /// node of current. If we are at root, and there is no parent, we
        /// return the next visible node of the current.</remarks>
        internal static TreeFeedsNodeBase GetNewNodeToActivate(TreeFeedsNodeBase current)
        {
            if (current == null)
                return null;
            UltraTreeNode newActive = null, n = current, sibling;
            while (null != (sibling = n.GetSibling(NodePosition.Next)))
            {
                // there are maybe some invisible nodes at the node level:
                if (sibling.Visible)
                {
                    newActive = sibling;
                    break;
                }

                n = sibling;
            }

            if (newActive == null)
            {
                // no next sibling:
                n = current;
                while (null != (sibling = n.GetSibling(NodePosition.Previous)))
                {
                    // there are maybe some invisible nodes at the node level:
                    if (sibling.Visible)
                    {
                        newActive = sibling;
                        break;
                    }

                    n = sibling;
                }
            }

            if (newActive == null) // no (visible) siblings at all:
                newActive = current.Parent;
            if (newActive == null) // no parent, we are root:
                newActive = current.NextVisibleNode;

            return (TreeFeedsNodeBase) newActive;
        }

        #endregion

        #region Converters

        /// <summary>
        /// Converts to defaultable boolean.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns></returns>
        public static DefaultableBoolean ConvertToDefaultableBoolean(bool value)
        {
            if (value)
                return DefaultableBoolean.True;

            return DefaultableBoolean.False;
        }

        /// <summary>
        /// Converts the sort enums.
        /// </summary>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns></returns>
        public static SortType ConvertToSortType(SortOrder sortOrder)
        {
            switch (sortOrder)
            {
                case SortOrder.None:
                    return SortType.None;
                case SortOrder.Ascending:
                    return SortType.Ascending;
                case SortOrder.Descending:
                    return SortType.Descending;
                default:
                    return SortType.Default;
            }
        }

        #endregion

        #region CopyNodes

        /// <summary>
        /// Copies the nodes of different treeview controls.
        /// Source are UltraTreeNodes (UltraTreeView), destination is
        /// the common MS TreeView.
        /// </summary>
        /// <param name="nodes">The nodes.</param>
        /// <param name="destinationTree">The destination tree.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        public static void CopyNodes(TreeFeedsNodeBase[] nodes, System.Windows.Forms.TreeView destinationTree, bool isChecked)
        {
            if (nodes == null) return;
            if (destinationTree == null) return;

            destinationTree.BeginUpdate();
            destinationTree.Nodes.Clear();
            foreach (var n in nodes)
            {
                int imgIdx = Resource.SubscriptionTreeImage.Feed,
                    selImgIdx = Resource.SubscriptionTreeImage.FeedSelected;

                if (destinationTree.ImageList != null)
                {
                    if (destinationTree.ImageList.Images.Count > n.ImageIndex && n.ImageIndex != 0)
                        imgIdx = n.ImageIndex;
                    else if (RssHelper.IsNntpUrl(n.DataKey))
                        imgIdx = Resource.SubscriptionTreeImage.Nntp;

                    if (destinationTree.ImageList.Images.Count > n.SelectedImageIndex && n.SelectedImageIndex != 0)
                        selImgIdx = n.SelectedImageIndex;
                    else if (RssHelper.IsNntpUrl(n.DataKey))
                        selImgIdx = Resource.SubscriptionTreeImage.NntpSelected;
                }

                var tn = new TreeNode(n.Text, imgIdx, selImgIdx);
                int i = destinationTree.Nodes.Add(tn);
                destinationTree.Nodes[i].Tag = n.DataKey;
                destinationTree.Nodes[i].Checked = isChecked;
                if (n.Nodes.Count > 0)
                    CopyNodes(n.Nodes, destinationTree.Nodes[i], isChecked);
            }
            destinationTree.EndUpdate();
        }

        /// <summary>
        /// Copies the nodes to the destination treeview control.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="destinationTree">The destination tree.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
		public static void CopyNodes(TreeFeedsNodeBase node, System.Windows.Forms.TreeView destinationTree, bool isChecked)
        {
            if (node == null) return;
            if (destinationTree == null) return;

            destinationTree.BeginUpdate();
            destinationTree.Nodes.Clear();
            int imgIdx = Resource.SubscriptionTreeImage.Feed,
                selImgIdx = Resource.SubscriptionTreeImage.FeedSelected;

            if (destinationTree.ImageList != null)
            {
                if (destinationTree.ImageList.Images.Count > node.ImageIndex)
                    imgIdx = node.ImageIndex;
                else if (RssHelper.IsNntpUrl(node.DataKey))
                    imgIdx = Resource.SubscriptionTreeImage.Nntp;

                if (destinationTree.ImageList.Images.Count > node.SelectedImageIndex)
                    selImgIdx = node.SelectedImageIndex;
                else if (RssHelper.IsNntpUrl(node.DataKey))
                    selImgIdx = Resource.SubscriptionTreeImage.NntpSelected;
            }

            var tn = new TreeNode(node.Text, imgIdx, selImgIdx);
            int i = destinationTree.Nodes.Add(tn);
            destinationTree.Nodes[i].Tag = node.DataKey;
            destinationTree.Nodes[i].Checked = isChecked;
            if (node.Nodes.Count > 0)
                CopyNodes(node.Nodes, destinationTree.Nodes[i], isChecked);
            destinationTree.EndUpdate();
        }

        private static void CopyNodes(TreeNodesCollection nodes, TreeNode parent, bool isChecked)
        {
            var destinationTree = parent.TreeView;

            foreach (TreeFeedsNodeBase n in nodes)
            {
                int imgIdx = Resource.SubscriptionTreeImage.Feed,
                    selImgIdx = Resource.SubscriptionTreeImage.FeedSelected;

                if (destinationTree.ImageList != null)
                {
                    if (destinationTree.ImageList.Images.Count > n.ImageIndex && n.ImageIndex != 0)
                        imgIdx = n.ImageIndex;
                    else if (RssHelper.IsNntpUrl(n.DataKey))
                        imgIdx = Resource.SubscriptionTreeImage.Nntp;

                    if (destinationTree.ImageList.Images.Count > n.SelectedImageIndex && n.SelectedImageIndex != 0)
                        selImgIdx = n.SelectedImageIndex;
                    else if (RssHelper.IsNntpUrl(n.DataKey))
                        selImgIdx = Resource.SubscriptionTreeImage.NntpSelected;
                }

                var tn = new TreeNode(n.Text, imgIdx, selImgIdx);
                int i = parent.Nodes.Add(tn);
                parent.Nodes[i].Tag = n.DataKey;
                parent.Nodes[i].Checked = isChecked;
                if (n.Nodes.Count > 0)
                    CopyNodes(n.Nodes, parent.Nodes[i], isChecked);
            }
        }

        #endregion

        #region GetCheckedNodes/SetCheckedNodes

        /// <summary>
        /// We assume: leave nodes are nodes with a Tag != null.
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="folders"></param>
        /// <param name="leaveNodes"></param>
        public static void GetCheckedNodes(TreeNode startNode, ArrayList folders, ArrayList leaveNodes)
        {
            if (startNode == null)
                return;

            if (startNode.Checked)
            {
                if (startNode.Tag == null)
                {
                    folders.Add(startNode);
                    return; // all childs are checked. They will be resolved later dynamically
                }

                leaveNodes.Add(startNode);
            }

            foreach (TreeNode n in startNode.Nodes)
            {
                GetCheckedNodes(n, folders, leaveNodes);
            }
        }

        /// <summary>
        /// Fills the checkedNodes list with all nodes that are checked.
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="checkedNodes">List will be filled with nodes that are checked</param>
        public static void GetCheckedNodes(TreeNode startNode, ArrayList checkedNodes)
        {
            if (startNode == null)
                return;

            if (startNode.Checked)
            {
                if (startNode.Tag != null)
                    checkedNodes.Add(startNode);
            }

            if (startNode.Nodes.Count > 0)
            {
                foreach (TreeNode n in startNode.Nodes)
                {
                    GetCheckedNodes(n, checkedNodes);
                }
            }
        }

        /// <summary>
        /// We assume: leave nodes are nodes with a Tag != null.
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="folders"></param>
        /// <param name="leaveNodeTags"></param>
        public static void SetCheckedNodes(TreeNode startNode, ArrayList folders, ArrayList leaveNodeTags)
        {
            if (startNode == null)
                return;

            if (startNode.Tag == null)
            {
                string catName = BuildCategoryStoreName(startNode);
                if (catName != null && folders != null)
                {
                    foreach (string folder in folders)
                    {
                        if (catName.Equals(folder))
                        {
                            startNode.Checked = true;
                            PerformOnCheckStateChanged(startNode);
                            break;
                        }
                    }
                }
            }
            else
            {
                if (leaveNodeTags != null)
                {
                    foreach (string tagString in leaveNodeTags)
                    {
                        if (tagString != null && tagString.Equals(startNode.Tag))
                        {
                            startNode.Checked = true;
                            PerformOnCheckStateChanged(startNode);
                            break;
                        }
                    }
                }
            }

            foreach (TreeNode n in startNode.Nodes)
            {
                SetCheckedNodes(n, folders, leaveNodeTags);
            }
        }

        #endregion

        /// <summary>
        /// Helper that builds the full path trimmed category name (without root caption)
        /// </summary>
        /// <param name="theNode">a TreeNode</param>
        /// <returns>Category name in this form: 
        /// 'Main Category\Sub Category\...\catNode Category' without the root node caption.
        /// </returns>
        public static string BuildCategoryStoreName(TreeNode theNode)
        {
            if (theNode != null)
                return BuildCategoryStoreName(theNode.FullPath);

            return null;
        }

        /// <summary>
        /// Helper that builds the full path trimmed category name (without root caption)
        /// </summary>
        /// <param name="fullPathName">The full Path Name of a TreeNode</param>
        /// <returns>Category name in this form: 
        /// 'Main Category\Sub Category\...\catNode Category' without the root node caption.
        /// </returns>
        public static string BuildCategoryStoreName(string fullPathName)
        {
            if (string.IsNullOrEmpty(fullPathName))
                return null;

            string s = fullPathName.Trim();
            string[] a = s.Split(FeedSource.CategorySeparator.ToCharArray());

            if (a.GetLength(0) > 1)
                return String.Join(FeedSource.CategorySeparator, a, 1, a.GetLength(0) - 1);

            return null;
        }

        /// <summary>
        /// Helper that builds the full path trimmed category name (without root caption)
        /// </summary>
        /// <param name="fullPathName">The full Path Name of a TreeNode</param>
        /// <param name="ignoreLeaveNode">if set to <c>true</c>, the leave node name is ignored.</param>
        /// <returns>
        /// If input (fullPathName) was:
        /// 'Root\Main Category\Sub Category\...\catNode Category\LeaveNode Name'
        /// It returns in case ignoreLeaveNode was <c>false</c>:
        /// 'Main Category\Sub Category\...\catNode Category\LeaveNode Name' without the root node caption.
        /// If ignoreLeaveNode is set to <c>true</c>, the result will be:
        /// 'Main Category\Sub Category\...\catNode Category' without the root node caption
        /// and LeaveNode caption.
        /// </returns>
        public static string[] BuildCategoryStoreNameArray(string fullPathName, bool ignoreLeaveNode)
        {
            if (string.IsNullOrEmpty(fullPathName))
                return new string[] {};

            string s = fullPathName.Trim();
            var a = new ArrayList(s.Split(FeedSource.CategorySeparator.ToCharArray()));

            if (ignoreLeaveNode)
            {
                if (a.Count > 2)
                {
                    var ret = new string[a.Count - 2];
                    a.CopyTo(1, ret, 0, a.Count - 2);
                    return ret;
                }
            }
            else
            {
                if (a.Count > 1)
                {
                    var ret = new string[a.Count - 1];
                    a.CopyTo(1, ret, 0, a.Count - 1);
                    return ret;
                }
            }

            return new string[] {};
        }

        public static void PerformOnCheckStateChanged(TreeNode node)
        {
            if (node != null)
            {
                if (node.Nodes.Count > 0)
                {
                    CheckChildNodes(node, node.Checked);
                }
                CheckParentNodes(node, node.Checked);
            }
        }

        public static void CheckChildNodes(TreeNode parent, bool isChecked)
        {
            foreach (TreeNode child in parent.Nodes)
            {
                child.Checked = isChecked;
                if (child.Nodes.Count > 0)
                    CheckChildNodes(child, isChecked);
            }
        }

        public static void CheckParentNodes(TreeNode node, bool isChecked)
        {
            if (node == null) return;
            if (node.Parent == null) return;
            foreach (TreeNode child in node.Parent.Nodes)
            {
                if (child.Checked != isChecked)
                {
                    node.Parent.Checked = false;
                    CheckParentNodes(node.Parent, isChecked);
                    return; // not all childs have the same state
                }
            }
            node.Parent.Checked = isChecked;
            CheckParentNodes(node.Parent, isChecked);
        }

        #region Reflection helpers

        private static readonly Type TreeType = typeof (UltraTree);
        private static readonly Type ScrollbarControlType = typeof (ScrollbarControl);
        private static readonly Type UltraTreeNodeType = typeof (UltraTreeNode);

        public static bool InvokeDoVerticalScroll(UltraTree tree, int delta)
        {
            if (tree == null || tree.TopNode == null)
                return false;
            // This is in general the implementation of the UltraTree.OnMouseWheel() method.
            // Because we cannot call this protected method "OnMouseWheel()" by reflection,
            // it contains a check for "this.Focused"!
            try
            {
                if (!InvokeAllVisibleNodesAreInView(tree) && (InvokeCanScrollDown(tree) || delta >= 0))
                {
                    int visTopIndex = InvokeGetVisibleIndex(tree.TopNode);
                    int num1;
                    // this calc is make wheel scrolling work also for the topmost nodes:
                    if (visTopIndex <= 3)
                        num1 = delta/120;
                    else
                        // makes scrolling a little bit faster:
                        num1 = delta/30;

                    int num2 = visTopIndex - num1;
                    ScrollEventType type = (num1 < 0) ? ScrollEventType.SmallIncrement : ScrollEventType.SmallIncrement;
                    var args = new ScrollEventArgs(type, num2);
                    // now we call tree.ScrollbarControl.OnVerticalScroll(null, args):
                    var scrollCtrl = (ScrollbarControl) TreeType.InvokeMember("ScrollbarControl",
                                                                              BindingFlags.Instance |
                                                                              BindingFlags.NonPublic |
                                                                              BindingFlags.GetProperty, null, tree, null);
                    if (scrollCtrl != null)
                    {
                        ScrollbarControlType.InvokeMember("OnVerticalScroll",
                                                          BindingFlags.Instance | BindingFlags.NonPublic |
                                                          BindingFlags.InvokeMethod, null, scrollCtrl,
                                                          new object[] {null, args});

                        return true;
                    }
                }
            }
            catch (Exception reflectionIssue)
            {
                Log.Error("InvokeDoVerticalScroll() reflection issue", reflectionIssue);
            }
            // we did handled the event:
            return true;
        }

        private static bool InvokeAllVisibleNodesAreInView(UltraTree tree)
        {
            if (tree == null)
                return true;
            return (bool) TreeType.InvokeMember("AllVisibleNodesAreInView",
                                                BindingFlags.Instance | BindingFlags.NonPublic |
                                                BindingFlags.GetProperty,
                                                null, tree, null);
        }

        private static bool InvokeCanScrollDown(UltraTree tree)
        {
            if (tree == null)
                return false;
            return (bool) TreeType.InvokeMember("CanScrollDown",
                                                BindingFlags.Instance | BindingFlags.NonPublic |
                                                BindingFlags.GetProperty,
                                                null, tree, null);
        }

        private static int InvokeGetVisibleIndex(UltraTreeNode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return (int) UltraTreeNodeType.InvokeMember("VisibleIndex",
                                                        BindingFlags.Instance | BindingFlags.NonPublic |
                                                        BindingFlags.GetProperty,
                                                        null, node, null);
        }

        #endregion
    }

    #endregion

    #region TreeNodesSortHelper

    // not currently in use:
    internal class TreeNodesSortHelper : IComparer
    {
        private SortOrder _sortOrder;

        public TreeNodesSortHelper() : this(SortOrder.Ascending)
        {
        }

        public TreeNodesSortHelper(SortOrder sortOrder)
        {
            _sortOrder = sortOrder;
        }

        public void InitFromConfig(string section, UiStateSettings reader)
        {
            _sortOrder =
                (SortOrder)
                reader.GetInt32(section + "/SubscribedFeedNodes.Sorter.SortOrder", (int) SortOrder.Descending);
        }

        public void SaveToConfig(string section, UiStateSettings writer)
        {
            writer.SetProperty(section + "/SubscribedFeedNodes.Sorter.SortOrder", (int) _sortOrder);
        }

        public SortOrder Sorting
        {
            get { return _sortOrder; }
            set { _sortOrder = value; }
        }

        #region Implementation of IComparer

        public virtual int Compare(object x, object y)
        {
            if (_sortOrder == SortOrder.None)
                return 0;

            if (ReferenceEquals(x, y))
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            var n1 = (TreeFeedsNodeBase) x;
            var n2 = (TreeFeedsNodeBase) y;

            // root entries are not sorted: they stay there where they are inserted on creation:
            if (n1.Type == FeedNodeType.Root && n2.Type == FeedNodeType.Root)
                return 0;

            if (n1.Type == n2.Type)
                return (_sortOrder == SortOrder.Ascending
                            ? String.Compare(n1.Text, n2.Text)
                            : String.Compare(n2.Text, n1.Text));

            if (n1.Type == FeedNodeType.Category)
                return -1;

            if (n2.Type == FeedNodeType.Category)
                return 1;

            if (n1.Type == FeedNodeType.FinderCategory)
                return -1;

            if (n2.Type == FeedNodeType.FinderCategory)
                return 1;

            return 0;
        }

        #endregion
    }

    #endregion

	#region NodeTooltips 
	internal class UltraToolTipContextHelperForTreeNodes : UltraToolTipContextHelperBase
	{
		#region Constructor
		public UltraToolTipContextHelperForTreeNodes(UltraTree tree, UltraToolTipManager toolTipManager) :
			base(tree, toolTipManager)
		{		
		}
		#endregion Constructor

		#region Overrides

		#region PrepareToolTip
		protected override bool PrepareToolTip(object element, UltraToolTipInfo toolTipInfo) {
			// See if this is a UiElement. 
			if (this.PrepareNodeToolTip(element, toolTipInfo))
				return true;

			// Since the element was not one of the types being checked for, 
			// return false so no tooltip will be shown.
			return false;
		}
		#endregion PrepareToolTip

		#region ElementFromPoint

		protected override object ElementFromPoint(Point p) {
			UltraTree tree = (UltraTree)this.Control;
			return tree.GetNodeFromPoint(p);
		}

		#endregion

		#endregion Overrides

		#region Private Methods

		#region PrepareNodeToolTip
		private bool PrepareNodeToolTip(object element, UltraToolTipInfo toolTipInfo) {

			TreeNodeUIElement e = element as TreeNodeUIElement;
			if (e == null) return false;

			// See if the element is a UltraTreeNode (TreeFeedsNodeBase is inherited)
			TreeFeedsNodeBase node = ElementFromPoint(e.ControlElement.CurrentMousePosition) as TreeFeedsNodeBase;

			// If it's not, return false.
			if (node == null || !node.Visible)
				return false;

			string text = SR.TreeNodeTooltip_UnreadCountZeroHint;

			if (node.UnreadCount == 1)
				text = String.Format(SR.TreeNodeTooltip_UnreadCountSingleHint, MarkupAsBold(node.UnreadCount));
			else if (node.UnreadCount > 1)
				text = String.Format(SR.TreeNodeTooltip_UnreadCountMultipleHint, MarkupAsBold(node.UnreadCount));

			// If there is info text, display the tooltip.
			if (!string.IsNullOrEmpty(text)) {
				toolTipInfo.ToolTipText = text;
				toolTipInfo.ToolTipTextFormatted = MarkupWithFont(node.Override.NodeAppearance.FontData.Name, text);
				toolTipInfo.ToolTipTitle = node.Text;
				toolTipInfo.ToolTipImage = ToolTipImage.Info;

				// directly assign to the UiElement (class imlements IToolTipItem) would be
				// an alternative, but uses more resources:
				//e.ToolTipItem = new NodeToolTip(toolTipInfo, node.Control.Font);
			
				// Return true to indicate that a tooltip should be displayed when the
				// mouse is over this element:
				return true;
				// we return false to indicate, the tooltipManager should not
				// display tooltip for this element, because we already assigned
				// the tipinfo directly to the node UiElement:
				//return false;
			}

			return false;
		}

		static string MarkupAsBold(int counter)
		{
			return String.Format("<span style=\"font-weight:bold;\">{0}</span>", counter);
		}

		static string MarkupWithFont(string fontName, string content)
		{
			return String.Format("<span style=\"font-family:{0};\">{1}</span>", fontName, content);
		}

		#endregion PrepareNodeToolTip

		#endregion Private Methods

		//struct NodeToolTip : IToolTipItem
		//{
		//    private ToolTipInfo tti;
			
		//    public NodeToolTip(UltraToolTipInfo initializer, Font ownerFont)
		//    {
		//        tti = new ToolTipInfo();
		//        tti.Font = ownerFont;
		//        tti.TitleFont = new Font(ownerFont, FontStyle.Bold);
		//        tti.DisplayStyle = ToolTipDisplayStyle.Office2007;
		//        tti.ToolTipTextStyle = ToolTipTextStyle.Formatted;
		//        tti.ToolTipText = initializer.ToolTipTextFormatted;
		//        tti.Title = initializer.ToolTipTitle;
		//        tti.ToolTipImage = initializer.ToolTipImage;
		//    }

		//    #region IToolTipItem Members

		//    public ToolTipInfo GetToolTipInfo(Point mousePosition, UIElement element, UIElement previousToolTipElement, ToolTipInfo toolTipInfoDefault)
		//    {
		//        // fill the default from the template:
		//        toolTipInfoDefault.DisplayStyle = tti.DisplayStyle;
		//        toolTipInfoDefault.ToolTipTextStyle = tti.ToolTipTextStyle;
		//        toolTipInfoDefault.ToolTipText = tti.ToolTipText;
		//        toolTipInfoDefault.Title = tti.Title;
		//        toolTipInfoDefault.ToolTipImage = tti.ToolTipImage;
		//        return toolTipInfoDefault;
		//    }

		//    #endregion

		//}


	}
	#endregion

	#region TreeFeedsNodeUIElementCreationFilter

	/// <summary> 
    /// Users of Infragistics controls normally don't need an understanding of how UIElements are implemented and most
    /// often have no need for using a UIElement creation filter. This advanced extensibility mechanism is exposed for
    /// developers who want to modify, add to and/or replace the UIElements of a control. 
    /// 
    /// All controls that are based on the PLF (Presentation Layer Framework) expose a UIElement creation/positioning
    /// extensibility mechanism. To customize the size and/or location of UIElements or to add or replace one or more
    /// UIElements of a control you need to implement the IUIElementCreationFilter interface on an object and set the
    /// CreationFilter property of the control to that object at runtime.
    /// 
    /// IUIElementCreationFilter contains two methods: BeforeCreateChildElements and AfterCreateChildElements.
    /// The BeforeCreateChildElements method is called for each UIElement just after it's created. After this, the
    /// UIElement will, if appropriate create other "child" UIElements. After a "parent" has finished creating all of
    /// its child UIElements, the AfterCreateChildElements method is called.
    /// 
    /// We use the creation filter to extend the NodeSelectableArea to get teh unread count
    /// visualization included.
    /// </summary>
    internal class TreeFeedsNodeUIElementCreationFilter : IUIElementCreationFilter
    {
        /// <summary>
        /// Used only to measure strings
        /// </summary>
        private static readonly Graphics cachedGraphics;

        static TreeFeedsNodeUIElementCreationFilter()
        {
            var b = new Bitmap(1, 1);
            cachedGraphics = Graphics.FromImage(b);
        }

        #region IUIElementCreationFilter 

        /// <summary>
        /// BeforeCreateChildElements - called from inside the VerifyChildElements method if the child elements were
        /// marked dirty. This is called before PositionChildElements. Returning true from this method indicates that
        /// the default creation logic should be bypassed and PositionChildElements will not be called. 
        /// </summary>
        bool IUIElementCreationFilter.BeforeCreateChildElements(UIElement parent)
        {
            if (parent is NodeSelectableAreaUIElement)
            {
                var uiElement = parent as NodeSelectableAreaUIElement;
                var node = uiElement.Node as TreeFeedsNodeBase;
                if (node != null)
                {
                    // we measure for a bigger sized text, because we need a fixed global right images size
                    // for all right images
                    SizeF sz = cachedGraphics.MeasureString("(99999)", FontColorHelper.UnreadFont);

                    // extend the node's selectable area right by adding the measured width
                    uiElement.Rect = new Rectangle(uiElement.Rect.X, uiElement.Rect.Y,
                                                   uiElement.Rect.Width + (int) sz.Width, uiElement.Rect.Height);
                }
                return false;
            }
            // Returning false allows the normal creation and positioning logic to execute
            return false;
        }

        /// <summary>
        /// AfterCreateChildElements - Called after a "parent" has finished creating all of its child UIElements
        /// and PositionChildElements has been called called.  
        /// </summary>
        void IUIElementCreationFilter.AfterCreateChildElements(UIElement parent)
        {
            if (parent is NodeSelectableAreaUIElement)
            {
//				NodeSelectableAreaUIElement uiElement = parent as NodeSelectableAreaUIElement;
//				TreeFeedsNodeBase node = uiElement.Node as TreeFeedsNodeBase;
//				if (node != null) {
//					// we measure for a bigger sized text, because we need a fixed global right images size
//					// for all right images
//					SizeF  sz = cachedGraphics.MeasureString("(99999)", FontColorHelper.UnreadFont);
//					
//					// extend the node's selectable area right by adding the measured width
//					uiElement.Rect = new Rectangle( uiElement.Rect.X, uiElement.Rect.Y,
//						uiElement.Rect.Width + (int)sz.Width, uiElement.Rect.Height );
//				}
            }
        }

        #endregion
    }

    #endregion

    #region UltraTreeNodeExpansionMemento

    /// <summary>
    /// Class to store and restore <see cref="UltraTree"/> nodes expansion states
    /// </summary>
    [Serializable]
    [XmlType(Namespace = RssBanditNamespace.TreeState)]
    [XmlRoot("treeState", Namespace = RssBanditNamespace.TreeState, IsNullable = false)]
    public class UltraTreeNodeExpansionMemento //: IDisposable 
    {
        #region ivars

        // saved hierarchy infos:
        [XmlArray("expanded")] [XmlArrayItem("node", Type = typeof (String), IsNullable = false)] public ArrayList
            expandedNodes = new ArrayList();

        [XmlArray("selected")] [XmlArrayItem("node", Type = typeof (String), IsNullable = false)] public ArrayList
            selectedNodes = new ArrayList();


        // used in/with IDisposable - to restore
		[NonSerialized]
        private UltraTree _tree;
		[NonSerialized]
		private UltraTreeNode _node;

        #endregion

        #region ctor's

        /// <summary>
        /// Initializes a new instance of the <see cref="UltraTreeNodeExpansionMemento"/> class.
        /// </summary>
        public UltraTreeNodeExpansionMemento()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UltraTreeNodeExpansionMemento"/> class
        /// and store the nodes expansion states.
        /// </summary>
        /// <param name="tree">The tree.</param>
        protected UltraTreeNodeExpansionMemento(UltraTree tree)
        {
            this._tree = tree;
            expandedNodes = new ArrayList();
            selectedNodes = new ArrayList(1);
            if (tree != null)
            {
                if (tree.SelectedNodes.Count > 0)
                {
                    foreach (TreeFeedsNodeBase n in tree.SelectedNodes)
                    {
                        selectedNodes.Add(n.TypedRootFullPath);
                    }
                }
                foreach (TreeFeedsNodeBase n in tree.Nodes)
                {
                    if (n.Expanded)
                    {
                        expandedNodes.Add(n.TypedRootFullPath);
                        AddNodesRecursive(n, expandedNodes);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UltraTreeNodeExpansionMemento"/> class
        /// and store the nodes expansion states.
        /// </summary>
        /// <param name="node">The node.</param>
        protected UltraTreeNodeExpansionMemento(TreeFeedsNodeBase node)
        {
            this._node = node;
            expandedNodes = new ArrayList();
            selectedNodes = new ArrayList(1);

            if (node != null && node.Control != null)
            {
                if (node.Control.SelectedNodes.Count > 0)
                {
                    foreach (TreeFeedsNodeBase n in node.Control.SelectedNodes)
                    {
                        selectedNodes.Add(n.TypedRootFullPath);
                    }
                }
                if (node.Expanded)
                {
                    expandedNodes.Add(node.TypedRootFullPath);
                    AddNodesRecursive(node, expandedNodes);
                }
            }
        }

        #endregion

        #region public members

        /// <summary>
        /// Restores the specified tree node expansion states.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public void Restore(UltraTree tree)
        {
            if (tree == null || tree.Nodes.Count == 0 ||
                expandedNodes == null || expandedNodes.Count == 0)
                return;

            try
            {
                tree.BeginUpdate();

                var nodes = new HybridDictionary(expandedNodes.Count);
                for (int i = 0; i < expandedNodes.Count; i++)
                {
                    var path = (string) expandedNodes[i];
                    if (!nodes.Contains(path))
                        nodes.Add(path, null);
                }

                var selNodes = new HybridDictionary(selectedNodes.Count);
                for (int i = 0; i < selectedNodes.Count; i++)
                {
                    var path = (string) selectedNodes[i];
                    if (!selNodes.Contains(path))
                        selNodes.Add(path, null);
                }

                foreach (TreeFeedsNodeBase n in tree.Nodes)
                {
                    if (! n.Expanded && nodes.Contains(n.TypedRootFullPath))
                    {
                        n.Expanded = true; // should raise the event(s) and load data (if required)!!!

                        // just to speed up further lookups:
                        nodes.Remove(n.FullPath);
                    }
                    // restore childs:
                    ExpandNodesRecursive(n, nodes, selNodes);
                    if (selNodes.Contains(n.FullPath))
                    {
                        SelectNode(n);
                    }
                }
            }
            finally
            {
                tree.EndUpdate();
            }
        }

        private static void SelectNode(TreeFeedsNodeBase n)
        {
            if (n != null && n.Control != null)
            {
                if (n.Control.Visible)
                {
                    n.Selected = true;
                    if (n.Control.SelectedNodes.Count == 1)
                    {
                        n.Control.ActiveNode = n;
                        n.BringIntoView(true);
                    }
                }
            }
        }

        /// <summary>
        /// Restores the specified tree node expansion states.
        /// </summary>
        /// <param name="node">The node.</param>
        public void Restore(TreeFeedsNodeBase node)
        {
            if (node == null || !node.HasNodes ||
                expandedNodes == null || expandedNodes.Count == 0)
                return;
            if (node.Control == null)
                return;

            try
            {
                node.Control.BeginUpdate();

                var nodes = new HybridDictionary(expandedNodes.Count);
                for (int i = 0; i < expandedNodes.Count; i++)
                {
                    var path = (string) expandedNodes[i];
                    if (!nodes.Contains(path))
                        nodes.Add(path, null);
                }

                var selNodes = new HybridDictionary(selectedNodes.Count);
                for (int i = 0; i < selectedNodes.Count; i++)
                {
                    var path = (string) selectedNodes[i];
                    if (!selNodes.Contains(path))
                        selNodes.Add(path, null);
                }

                foreach (TreeFeedsNodeBase n in node.Nodes)
                {
                    if (! n.Expanded && nodes.Contains(n.TypedRootFullPath))
                    {
                        n.Expanded = true; // should raise the event(s) and load data (if required)!!!

                        // just to speed up further lookups:
                        nodes.Remove(n.FullPath);
                    }
                    // restore childs:
                    ExpandNodesRecursive(n, nodes, selNodes);
                    if (selNodes.Contains(n.FullPath))
                    {
                        SelectNode(n);
                    }
                }
            }
            finally
            {
                node.Control.EndUpdate();
            }
        }

        /// <summary>
        /// Saves the UltraTreeNodeExpansionMemento instance to specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="tree"></param>
        public static void Save(Stream stream, UltraTree tree)
        {
            var m = new UltraTreeNodeExpansionMemento(tree);
            XmlSerializer serializer = XmlHelper.SerializerCache.GetSerializer(
                typeof (UltraTreeNodeExpansionMemento), RssBanditNamespace.TreeState);
            serializer.Serialize(stream, m);
        }

        /// <summary>
        /// Loads the UltraTreeNodeExpansionMemento from specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public static UltraTreeNodeExpansionMemento Load(Stream stream)
        {
            XmlSerializer serializer = XmlHelper.SerializerCache.GetSerializer(
                typeof (UltraTreeNodeExpansionMemento), RssBanditNamespace.TreeState);
            return (UltraTreeNodeExpansionMemento) serializer.Deserialize(stream);
        }

        #endregion

        #region private members

        /// <summary>
        /// Adds the expanded nodes recursive.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="nodes"></param>
        private static void AddNodesRecursive(TreeFeedsNodeBase node, IList nodes)
        {
            if (node == null)
                return;

            foreach (TreeFeedsNodeBase n in node.Nodes)
            {
                if (n.Expanded)
                {
                    nodes.Add(n.TypedRootFullPath);
                    AddNodesRecursive(n, nodes);
                }
            }
        }

        /// <summary>
        /// Expands the nodes recursive.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="expanded">The expanded.</param>
        /// <param name="selected"></param>
        private static void ExpandNodesRecursive(TreeFeedsNodeBase node, IDictionary expanded, IDictionary selected)
        {
            if (node == null)
                return;

            foreach (TreeFeedsNodeBase n in node.Nodes)
            {
                if (! n.Expanded && expanded.Contains(n.TypedRootFullPath))
                {
                    n.Expanded = true; // should raise the event(s) and load data (if required)!!!
                    // just to speed up further lookups:
                    expanded.Remove(n.FullPath);
                    // restore childs:
                    ExpandNodesRecursive(n, expanded, selected);
                }

                if (selected.Contains(n.TypedRootFullPath))
                    SelectNode(n);
            }
        }

        #endregion

        #region IDisposable Members

//		public void Dispose() {
//			if (this.tree != null) {
//				Restore(this.tree);
//			} else if (this.node != null) {
//				Restore(this.node);
//			}
//			this.tree = null;
//			this.node = null;
//		}

        #endregion
    }

    #endregion

    #region NodeInfoManager (for ref.)

    /// <summary>
    /// Not in yet in use, but shows a way to dynamically
    /// create bitmaps and append that as additional infos to
    /// the node's RightImages collection. 
    /// </summary>
    internal class NodeInfoManager
    {
        /// <summary>
        /// Used only to measure strings
        /// </summary>
        private static readonly Graphics cachedGraphics;

        static NodeInfoManager()
        {
            var b = new Bitmap(1, 1);
            cachedGraphics = Graphics.FromImage(b);
        }

        /// <summary>
        /// Updates the unread info. This seems to be the only
        /// workaround to get the unread counter display area included
        /// in the mouse sensitive/clickable/selectable range.
        /// </summary>
        /// <param name="node">The node.</param>
        public static void UpdateUnreadInfo(TreeFeedsNodeBase node)
        {
            if (node == null)
                return;

            if (node.RightImages.Count > 0)
            {
                node.RightImages.RemoveAt(0);
            }

            if (node.UnreadCount > 0)
            {
                string st = String.Format("({0})", node.UnreadCount);
                // we measure for a bigger sized text, because we need a fixed global right images size
                // for all right images
                SizeF sz = cachedGraphics.MeasureString("(99999)", FontColorHelper.UnreadFont);
                var gz = new Size((int) sz.Width, (int) sz.Height);

                // adjust global sizes
                if (! node.Control.RightImagesSize.Equals(gz))
                    node.Control.RightImagesSize = gz;

                var bmp = new Bitmap(gz.Width, gz.Height);
                using (Graphics painter = Graphics.FromImage(bmp))
                {
                    painter.SmoothingMode = SmoothingMode.AntiAlias;
                    if (Win32.IsOSAtLeastWindowsXP)
                    {
                        painter.TextRenderingHint = TextRenderingHint.SystemDefault;
                    }
                    else
                    {
                        painter.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    }
                    using (Brush unreadColorBrush = new SolidBrush(FontColorHelper.UnreadColor))
                    {
                        painter.DrawString(st, FontColorHelper.UnreadFont, unreadColorBrush,
                                           0, 0, StringFormat.GenericDefault);
                    }
                }

                node.RightImages.Add(bmp);
            }
        }
    }

    #endregion
}
