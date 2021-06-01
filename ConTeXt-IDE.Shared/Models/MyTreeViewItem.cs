using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace ConTeXt_IDE.Models
{
    class MyTreeViewItem : TreeViewItem
    {
        protected override async void OnDragOver(DragEventArgs e)
        {
            try
            {

                var draggedOverItem = DataContext as FileItem;

                if (MainPage.DraggedItems.Count > 0)
                {
                    var draggedItem = MainPage.DraggedItems[0];
                    if (draggedItem.File is StorageFolder)
                    {
                        e.AcceptedOperation = DataPackageOperation.None;
                        e.DragUIOverride.Caption = "Cannot move folders";
                    }
                    else if (draggedItem.File is StorageFile sf && draggedOverItem.File is StorageFolder fold)
                    {
                        if (draggedItem.FileFolder == fold.Path)
                        {
                            e.AcceptedOperation = DataPackageOperation.None;
                            e.DragUIOverride.Caption = "Cannot paste to the same folder";
                        }
                        else
                        {
                            e.AcceptedOperation = DataPackageOperation.Move;
                            if (draggedOverItem.Type == FileItem.ExplorerItemType.Folder)
                            {
                                e.DragUIOverride.Caption = "Move to folder " + draggedOverItem.FileName;
                            }
                            else if (draggedOverItem.Type == FileItem.ExplorerItemType.ProjectRootFolder)
                            {
                                e.DragUIOverride.Caption = "Move to the root folder";
                            }
                        }
                    }
                    else
                    {
                        e.AcceptedOperation = DataPackageOperation.None;
                    }
                }
                else if (e.DataView.Contains(StandardDataFormats.StorageItems) && draggedOverItem.File is StorageFolder)
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                    e.DragUIOverride.Caption = "Paste to folder " + draggedOverItem.FileName;
                }
                else
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                e.Handled = true;
               // base.OnDragOver(e);
            }
            catch (Exception ex)
            {
                App.VM.Log("Error at DragOver: " + ex.Message);
            }
        }
        protected override async void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            try
            {
                var data = DataContext as FileItem;
                if (data.File is StorageFolder fold)
                {
                    if (MainPage.DraggedItems.Count > 0)
                    {
                        foreach (FileItem fi in MainPage.DraggedItems)
                        {
                            if (fi.File is StorageFile fil)
                            {
                                var parent = await fil.GetParentAsync();
                                if (parent.Path != fold.Path)
                                {
                                    await fil.MoveAsync(fold, fil.Name, NameCollisionOption.GenerateUniqueName);
                                    fi.FileFolder = Path.GetDirectoryName(fil.Path);
                                    //  fi.FilePath = fil.Path;
                                    App.VM.Log("Moved " + fil.Name + " from " + parent.Name + " to " + fold.Name);
                                    if (data.Type == FileItem.ExplorerItemType.ProjectRootFolder)
                                    {
                                        fi.Level = 0;
                                    } 
                                    else
                                    {
                                        fi.Level = 1;
                                        fi.IsRoot = false;
                                    }
                                }
                            }
                        }
                        MainPage.DraggedItems.Clear();
                    }
                    else if (e.DataView.Contains(StandardDataFormats.StorageItems))
                    {
                        foreach (StorageFile file in await e.DataView.GetStorageItemsAsync())
                        {
                            if (await fold.TryGetItemAsync(file.Name) == null)
                            {
                                var newfile = await fold.CreateFileAsync(file.Name);
                                var bytes = await FileIO.ReadBufferAsync(file);
                                await FileIO.WriteBytesAsync(newfile, bytes.ToArray());
                                var fi = new FileItem(newfile) { Type = FileItem.ExplorerItemType.File };
                                if (data.Type == FileItem.ExplorerItemType.ProjectRootFolder)
                                {
                                    fi.Level = 0;
                                }
                                else
                                {
                                    fi.Level = 1;
                                }
                                data.Children.Add(fi);
                            }
                            else
                                App.VM.Log(file.Name + " does already exist.");
                        }
                    }
                }
               
                e.Handled = true;
            }
            catch (Exception ex)
            {
                App.VM.Log("Error at Drop: " + ex.Message);
            }
        }
    }

    public class ExplorerItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FileTemplate { get; set; }
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate ProjectFolderTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item != null && item is FileItem)
            {
                var explorerItem = (FileItem)item;
                switch (explorerItem.Type)
                {
                    case FileItem.ExplorerItemType.File: return FileTemplate;
                    case FileItem.ExplorerItemType.Folder: return FolderTemplate;
                    case FileItem.ExplorerItemType.ProjectRootFolder: return ProjectFolderTemplate;
                    default: return FileTemplate;
                }
            }
            else return FileTemplate;
        }
    }
}
