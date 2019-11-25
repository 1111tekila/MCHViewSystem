﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.Linq;
public class HierarchyTreeView : TreeView
{
    Transform root;
    List<HierarchyData> hierarchyData = new List<HierarchyData>();
    public HierarchyTreeView(Transform root, TreeViewState treeViewState)
        : base(treeViewState)
    {
        this.root = root;
        CacheItem(root, 0);
        showBorder = true;
        showAlternatingRowBackgrounds = true;
        Reload();
    }

    public void Draw()
    {
        // using (var vertical = new GUILayout.VerticalScope())
        // {
        //     foreach (var item in hierarchyData)
        //     {
        //         using (var horizon = new GUILayout.HorizontalScope())
        //         {
        //             GUILayout.Space(item.layer * padding);
        //             GUILayout.Label(prefabIcon, GUILayout.Width(20), GUILayout.Height(20));
        //             if (GUILayout.Button(item.transform.name, labelStyle))
        //             {
        //                 OnItemClick?.Invoke(item.transform.gameObject);
        //             }
        //             GUILayout.Label(arrowIcon, GUILayout.Width(20), GUILayout.Height(20));
        //         }
        //     }
        // }
    }
    int id = 1;

    void CacheItem(Transform transform, int layer)
    {
        hierarchyData.Add(new HierarchyData(layer, transform, id));
        id++;
        foreach (Transform item in transform)
        {
            CacheItem(item, layer + 1);
        }
    }


    protected override TreeViewItem BuildRoot()
    {
        var treeRoot = new TreeViewItem { id = 0, depth = -1, displayName = this.root.name, icon = Drawer.prefabIcon };

        var allTreeItem = new List<TreeViewItem>();
        int Id = 1;
        foreach (var item in hierarchyData)
        {
            allTreeItem.Add(new TreeViewItem { id = item.id, depth = item.layer, displayName = item.transform.name, icon = Drawer.prefabIcon });
            Id++;
        }
        SetupParentsAndChildrenFromDepths(treeRoot, allTreeItem);

        // Return root of the tree
        return treeRoot;
    }
    protected override void RowGUI(RowGUIArgs args)
    {
        base.RowGUI(args);

    }

    protected override void DoubleClickedItem(int id)
    {
        var select = hierarchyData.SingleOrDefault(x => x.id == id);
        if (select == null)
        {
            return;
        }
        OnItemClick?.Invoke(select.transform.gameObject);
    }
    // protected override void ContextClickedItem(int id)
    // {
    //     base.ContextClickedItem(id);
    //     var select = hierarchyData.SingleOrDefault(x => x.id == id);
    //     if (select == null)
    //     {
    //         return;
    //     }

    //     if (Event.current.type != EventType.MouseDown)
    //     {
    //         return;
    //     }

    //     if (Event.current.button != 1)
    //     {
    //         return;
    //     }
    //     GenericMenu genericMenu = new GenericMenu();

    //     genericMenu.AddItem(new GUIContent("Add Component"), false,
    //         () =>
    //         {
    //             ViewSystemLog.Log("right click");
    //         }
    //     );

    //     genericMenu.ShowAsContext();
    //     Event.current.Use();
    // }
    class HierarchyData
    {
        public HierarchyData(int layer, Transform transform, int id)
        {
            this.layer = layer;
            this.id = id;
            this.transform = transform;
        }
        public int layer;
        public int id;
        public Transform transform;
    }
    public delegate void _OnItemClick(Object target);

    public event _OnItemClick OnItemClick;
}
