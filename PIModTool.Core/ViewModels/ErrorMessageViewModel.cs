using MvvmCross.Navigation;
using MvvmCross.ViewModels;

public class ErrorMessageViewModel : MvxViewModel<string>
{
    private readonly IMvxNavigationService _navigationService;
    private readonly IMessageService _messageService;

    public ErrorMessageViewModel(IMvxNavigationService navigationService, IMessageService messageService)
    {
        _navigationService = navigationService;
        _messageService = messageService;
    }

    private string _message = "";
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public override void Prepare(string parameter)
    {
        Message = parameter;
    }

    public override async Task Initialize()
    {
        await base.Initialize();
        await _messageService.ShowErrorAsync(Message);
        await _navigationService.Close(this);
    }
}