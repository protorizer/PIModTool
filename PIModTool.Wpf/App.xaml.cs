using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.Core;
using MvvmCross;
using PIModTool.Wpf.Utilities;

namespace PIModTool.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : MvxApplication
    {
        protected override void RegisterSetup()
        {
            this.RegisterSetupType<Setup>();
        }
    }
}
