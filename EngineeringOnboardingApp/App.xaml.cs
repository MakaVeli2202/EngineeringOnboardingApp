using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace EngineeringOnboardingApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!IsElevated() && !Debugger.IsAttached)
            {
                RelaunchElevated();
                Shutdown();
                return;
            }

            RegisterGlobalExceptionHandlers();
        }

        private void RegisterGlobalExceptionHandlers()
        {
            DispatcherUnhandledException += (_, args) =>
            {
                try
                {
                    EngineeringOnboardingApp.Services.LogService.LogException(args.Exception, "UI");
                }
                catch { }
                args.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                try
                {
                    EngineeringOnboardingApp.Services.LogService.LogException(args.Exception, "Task");
                }
                catch { }
                args.SetObserved();
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                try
                {
                    EngineeringOnboardingApp.Services.LogService.LogException(
                        args.ExceptionObject as Exception, "AppDomain");
                }
                catch { }
            };
        }

        private static bool IsElevated()
        {
            using var identity = WindowsIdentity.GetCurrent();

            return new WindowsPrincipal(identity)
                .IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RelaunchElevated()
        {
            string exe = Environment.ProcessPath
                ?? Process.GetCurrentProcess().MainModule?.FileName
                ?? string.Empty;

            if (string.IsNullOrEmpty(exe))
            {
                return;
            }

            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = string.Join(' ', Environment.GetCommandLineArgs().Skip(1)),
                    UseShellExecute = true,
                    Verb = "runAs"
                });
            }
            catch
            {
            }
        }
    }
}
