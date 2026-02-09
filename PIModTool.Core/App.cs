using MvvmCross;
using MvvmCross.IoC;
using MvvmCross.ViewModels;
using PIModTool.Core.ViewModels;
using PIModTool.Core.ViewModels.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Core
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            Mvx.IoCProvider.RegisterType<MenuBoxViewModel>();
            RegisterAppStart<MainViewModel>();
        }
    }
}
