using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PIModTool.Core.Types;
using PIModTool.Lib;
using PIModTool.Lib.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PIModTool.Core.ViewModels
{
    public class MainViewModel : MvxViewModel
    {
        private readonly IMvxNavigationService _navigationService;

        public async Task ChangeView<TViewModel>() where TViewModel : MvxViewModel
        {
            await _navigationService.Navigate<TViewModel>();
        }

        public MainViewModel(IMvxNavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        // TODO: Refactor this, and the entire LoadingScreen. It shouldn't be doing tasks.
        public async Task<int> OpenSmallF(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) {
                return 1;
            };

            await _navigationService.Navigate<LoadingViewModel, LoadingParams>(new LoadingParams
            {
                Message = "LOADING",
                Action = async () =>
                {
                    // Parse smallf stuff and pass to other view
                    List<SmallFFile>? files = await Task.Run(() => SmallFHandler.ReadSmallF(filePath));
                    if (files == null) // Error during read
                    {
                        await _navigationService.Close(this);
                        var messageService = Mvx.IoCProvider.Resolve<IMessageService>();
                        await messageService.ShowErrorAsync("Selected file is not a valid SmallF.dat.");
                        return;
                    }

                    SmallFEditorParams editorParams = new SmallFEditorParams();
                    editorParams.FileName = filePath;
                    editorParams.Files = files;
                    _navigationService.Navigate<SmallFEditorViewModel, SmallFEditorParams>(editorParams);
                }
            });

            return 0;
        }

        public int PackSmallF(string? folderPath, string? filePath)
        {
            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(filePath))
            {
                return 1; // User didn't select a folder or file
            }

            // Get all the files in the folder
            List<GenericFile>? files = GenericHandler.OpenAllFilesInFolder(folderPath);
            if (files == null)
            {
                return 2; // Couldn't read folder contents, or folder is empty
            }

            try
            {
                SmallFHandler.SaveSmallF(files, filePath);
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
                return 3;
            }

            return 0;
        }

    }
}
