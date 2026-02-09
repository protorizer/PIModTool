using MvvmCross;
using MvvmCross.Platforms.Wpf.Views;
using PIModTool.Core.ViewModels.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PIModTool.Wpf.Views.Components
{
    /// <summary>
    /// Interaction logic for MenuBoxView.xaml
    /// </summary>
    public partial class MenuBoxView : MvxWpfView
    {
        public static readonly DependencyProperty LabelProperty = 
            DependencyProperty.Register("Label", typeof(string), typeof(MenuBoxView), new PropertyMetadata(""));
        public static readonly DependencyProperty HeaderPaddingProperty =
            DependencyProperty.Register("HeaderPadding", typeof(double), typeof(MenuBoxView), new PropertyMetadata(0.0));

        // This ensures that the UserControl itself can host content
        public static readonly new DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(object), typeof(MenuBoxView), new PropertyMetadata("Default Content"));

        public new object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }
        public MenuBoxView()
        {
            InitializeComponent();
            //this.DataContext = Mvx.IoCProvider.Resolve<MenuBoxViewModel>(); // This is necessary to bind to the ViewModel because MenuBoxView is NOT part of the navigation flow
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public double HeaderPadding
        {
            get => (double)GetValue(HeaderPaddingProperty);
            set => SetValue(HeaderPaddingProperty, value);
        }
    }
}
