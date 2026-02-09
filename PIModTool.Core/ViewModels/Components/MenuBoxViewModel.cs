using MvvmCross.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Core.ViewModels.Components
{
    public class MenuBoxViewModel: MvxViewModel
    {
        private string _label = "Text";
        private double _boxHeight = 100;
        private double _boxWidth = 100;
        private double _headerPadding = 0;

        public string Label
        {
            get { return _label; }
            set { SetProperty(ref _label, value); }
        }
        public double BoxHeight
        {
            get { return _boxHeight; }
            set { SetProperty(ref _boxHeight, value); }
        }
        public double BoxWidth
        {
            get { return _boxWidth; }
            set { SetProperty(ref _boxWidth, value); }
        }
        public double HeaderPadding
        {
            get { return _headerPadding; }
            set { SetProperty(ref _headerPadding, value); }
        }
    }
}
