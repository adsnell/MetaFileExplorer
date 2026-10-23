using System;

namespace MetaFileExplorer
{
    public partial class App : System.Windows.Application
    {
        private void Application_Startup(object sender, System.Windows.StartupEventArgs e)
        {
            try
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Startup Exception:\n\n" + ex.ToString(), "MetaFileExplorer Crash", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}