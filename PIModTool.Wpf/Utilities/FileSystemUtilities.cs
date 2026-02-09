using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Wpf.Utilities
{
    public static class FileSystemUtilities
    {
        public static string? OpenFile(string title, string filter)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = title;
            dialog.Filter = filter;

            if(dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            else
            {
                return null;
            }
        }

        public static string? OpenFolder(string title)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.Title = title;

            if (dialog.ShowDialog() == true)
            {
                return dialog.FolderName;
            }
            else
            {
                return null;
            }
        }

        public static string? SaveFile(string title, string fileName, string filter)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = title;
            dialog.FileName = fileName;
            dialog.Filter = filter;

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            else
            {
                return null;
            }
        }
    }
}
