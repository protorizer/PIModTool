using MvvmCross.Platforms.Wpf.Views;
using PIModTool.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PIModTool.Wpf.Views
{
    /// <summary>
    /// Interaction logic for YukEditorView.xaml
    /// </summary>
    public partial class YukEditorView : MvxWpfView
    {
        private bool isScrubbing = false;
        private DispatcherTimer playbackTimer;
        public YukEditorView()
        {
            InitializeComponent();
        }

        private async void StartUnwreck(object sender, RoutedEventArgs e)
        {
            await (DataContext as YukEditorViewModel).FadeToStem(3, 0.5f);
        }

        private async void EndUnwreck(object sender, RoutedEventArgs e)
        {
            await (DataContext as YukEditorViewModel).FadeToStem((DataContext as YukEditorViewModel).PositionStem, 1.0f);
        }

        private async void StartBoost(object sender, RoutedEventArgs e)
        {
            await (DataContext as YukEditorViewModel).FadeToStem(4, 0.5f);
        }

        // TODO: Update this to move logic to the viewmodel
        private async void EndBoost(object sender, RoutedEventArgs e)
        {
            if((DataContext as YukEditorViewModel).StemVolumes[4].Volume == (DataContext as YukEditorViewModel).MixVolumes[4])
            {
                await (DataContext as YukEditorViewModel).FadeToStem((DataContext as YukEditorViewModel).PositionStem, 3.0f);
            }
            else
            {
                await (DataContext as YukEditorViewModel).FadeToStem((DataContext as YukEditorViewModel).LastStem, 3.0f);
            }
        }

        private void Scrubber_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isScrubbing = true;
        }

        private void Scrubber_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isScrubbing = false;
        }

        private void StartPlaybackTimer(object sender, RoutedEventArgs e)
        {
            playbackTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(33) }; // 30fps
            playbackTimer.Tick += (_, _) =>
            {
                if (isScrubbing) { return; }
                (DataContext as YukEditorViewModel).UpdatePlaybackTimer();
            };
            playbackTimer.Start();
        }
    }
}
