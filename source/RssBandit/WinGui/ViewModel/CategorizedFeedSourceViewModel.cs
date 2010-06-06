﻿#region Version Info Header
/*
 * $Id$
 * $HeadURL$
 * Last modified by $Author$
 * Last modified at $Date$
 * $Revision$
 */
#endregion


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using NewsComponents;
using NewsComponents.Feed;
using NewsComponents.Utils;

namespace RssBandit.WinGui.ViewModel
{
    public class CategorizedFeedSourceViewModel: ViewModelBase
    {
        private readonly FeedSourceEntry _entry;
        private ObservableCollection<TreeNodeViewModelBase> _children;

        public CategorizedFeedSourceViewModel(FeedSourceEntry feedSource)
        {
            _entry = feedSource;
        }

        public string Name {
            get { return _entry.Name; }
            set { _entry.Name = value; }
        }

        public ObservableCollection<TreeNodeViewModelBase> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = new ObservableCollection<TreeNodeViewModelBase>();

                    _entry.Source.LoadFeedlist();

                    if (_entry.Source.FeedsListOK)
                    {
                        ICollection<INewsFeedCategory> categories = _entry.Source.GetCategories().Values;
                        
                        var categoryTable = new Dictionary<string, FolderViewModel>();
                        var categoryList = new List<INewsFeedCategory>(categories);

                        foreach (var f in _entry.Source.GetFeeds().Values)
                        {                          

                            string category = (f.category ?? String.Empty);
                            if (String.IsNullOrEmpty(category))
                            {
                                _children.Add(new FeedViewModel(f, null, this));
                            }
                            else
                            {
                                FolderViewModel catnode;
                                if (!categoryTable.TryGetValue(category, out catnode))
                                {
                                    catnode = CreateHive(category,_children, categoryTable, this);
                                } 
                                
                                catnode.Children.Add(new FeedViewModel(f, catnode, this));
                            }

                            for (int i = 0; i < categoryList.Count; i++)
                            {
                                if (categoryList[i].Value.Equals(category))
                                {
                                    categoryList.RemoveAt(i);
                                    break;
                                }
                            }
                        }

                        //add categories, we not already have
                        foreach (var c in categoryList)
                        {
                            CreateHive(c.Value, _children, categoryTable, this);
                        }

                    }
                    else
                    {
                        //TODO: indicate the error in the UI
                    }
                }

                return _children;
            }
            set { _children = value; }
        }

        /// <summary>
        /// Creates a FolderViewModel that represents a feed category in the tree view
        /// </summary>
        /// <param name="pathName">Full path or category name</param>
        /// <param name="childNodes">Child nodes of the root node in the tree view</param>
        /// <param name="knownFolders">List of known FolderViewModel objects encountered thus far</param>
        /// <param name="source">The feed source that owns the folder</param>
        /// <returns></returns>
        static FolderViewModel CreateHive(string pathName, ICollection<TreeNodeViewModelBase> childNodes, Dictionary<string, FolderViewModel> knownFolders, CategorizedFeedSourceViewModel source)
        {
            pathName.ExceptionIfNullOrEmpty("pathName");

            FolderViewModel startNode = null, previous = null;

            if (!knownFolders.TryGetValue(pathName, out startNode))
            {
                List<string> catHives = new List<string>(pathName.Split(FeedSource.CategorySeparator.ToCharArray()));
                bool wasNew = false;              
                StringBuilder path = new StringBuilder(pathName.Length);

                for (int i = 0; i < catHives.Count; i++)
                {
                    startNode  = null;

                    if (!wasNew)                    
                        startNode = (FolderViewModel)childNodes.FirstOrDefault(
                          n => (n is FolderViewModel && n.Name.Equals(catHives[i], StringComparison.CurrentCulture)));
                    

                    if (startNode == null)
                    {
                        startNode = new FolderViewModel(catHives[i], previous, source);
                        childNodes.Add(startNode);
                        
                        path.AppendFormat("{1}{0}", catHives[i], path.Length > 0 ? FeedSource.CategorySeparator: String.Empty);
                        if (!knownFolders.ContainsKey(path.ToString()))
                            knownFolders.Add(path.ToString(), startNode);

                        wasNew = true;
                    }

                    previous = startNode;
                    childNodes = startNode.Children;                    
                }
            }

            return startNode;
        }


    }

    
}
