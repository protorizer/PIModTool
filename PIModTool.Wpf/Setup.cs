using Microsoft.Extensions.Logging;
using MvvmCross;
using MvvmCross.IoC;
using MvvmCross.Platforms.Wpf.Core;
using PIModTool.Core.Services;
using PIModTool.Wpf.Services;
using Serilog;
using Serilog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Wpf
{
    public class Setup: MvxWpfSetup<Core.App>
    {
        protected override ILoggerProvider CreateLogProvider()
        {
            return new SerilogLoggerProvider();
        }

        protected override ILoggerFactory CreateLogFactory()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .CreateLogger();

            return new SerilogLoggerFactory();
        }

        protected override void InitializeFirstChance(IMvxIoCProvider iocProvider)
        {
            Mvx.IoCProvider.RegisterSingleton<IMessageService>(new WpfMessageService());
            Mvx.IoCProvider.RegisterSingleton<IMeshExportService>(new WpfMeshExportService());

            base.InitializeFirstChance(iocProvider);
        }
    }
}
