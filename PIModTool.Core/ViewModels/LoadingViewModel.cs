using MvvmCross.Navigation;
using MvvmCross.Plugin.Messenger;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Core.ViewModels
{
    public class LoadingParams
    {
        public string Message { get; set; } = "LOADING";
        public Func<Task> Action { get; set; } = async () => await Task.CompletedTask;
    }

    public class LoadingViewModel : MvxViewModel<LoadingParams>
    {
        private readonly IMvxNavigationService _navigationService;
        public LoadingViewModel(IMvxNavigationService navigationService) {
            _navigationService = navigationService;
        }

        private string _message;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private LoadingParams? _parameter;
        public override void Prepare(LoadingParams parameter)
        {
            _parameter = parameter;
            Message = parameter.Message;
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            if (_parameter?.Action != null)
            {
                try
                {
                    await _parameter.Action.Invoke();
                }
                finally
                {
                    // Once done, close this loading screen
                    await _navigationService.Close(this);
                }
            }
        }
    }
}
