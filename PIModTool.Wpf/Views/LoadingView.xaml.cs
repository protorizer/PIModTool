using MvvmCross.Platforms.Wpf.Views;
using System.Windows;

namespace PIModTool.Wpf.Views
{
    /// <summary>
    /// Interaction logic for LoadingView.xaml
    /// </summary>
    public partial class LoadingView : MvxWpfView
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(LoadingView), new PropertyMetadata("LOADING"));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public LoadingView()
        {
            InitializeComponent();
        }
    }
}
