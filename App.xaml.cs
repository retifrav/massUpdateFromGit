using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace updateFromGit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(
            object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e
            )
        {
            MessageBox.Show(                
                string.Format("Произошла неизвестная ошибка.{1}Подробности: {0}",
                    e.Exception.Message, Environment.NewLine),
                "Неизвестная ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error
                );
            e.Handled = true;
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            bool startWithScenario = false;
            if (e.Args.Length > 0)
            {
                if (e.Args.Length != 2)
                {
                    MessageBox.Show(
                        "Неверное количество параметров запуска",
                        "Ошибка при запуске",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );
                }
                else
                {
                    if (e.Args[0] != "-s")
                    {
                        MessageBox.Show(
                            "Неизвестные параметры запуска",
                            "Ошибка при запуске",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                            );
                    }
                    else
                    {
                        startWithScenario = true;
                    }
                }
            }

            if (!checkGit())
            {
                MessageBox.Show(
                    "В переменной PATH отсутствует директория с git.exe",
                    "Не найден Git",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );

                App.Current.Shutdown(-1);
                return;
            }
            
            MainWindow mainWindow = new MainWindow();
            if (startWithScenario)
            {
                mainWindow.loadScenario(e.Args[1]);
            }
            
            mainWindow.Show();
            mainWindow.changeSavedStatus(true);
            
        }

        public bool checkGit()
        {
            using (Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = System.IO.Path.Combine(
                        "git.exe"
                        ),
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            })
            {
                proc.Start();
                if (containsIgnoreCase(getProcessOutput(proc), "git version")) { return true; }
            }

            return false;
        }

        public static string getProcessOutput(Process proc)
        {
            StringBuilder output = new StringBuilder();
            while (!proc.StandardOutput.EndOfStream)
            {
                output.Append(proc.StandardOutput.ReadLine());
                output.Append(Environment.NewLine);
            }

            byte[] bytes = Encoding.Default.GetBytes(output.ToString());
            string outputEncoded = Encoding.GetEncoding(866).GetString(bytes);

            return outputEncoded;
        }

        /// <summary>
        /// Содержит ли строка указанную подстроку независимо от регистра
        /// </summary>
        /// <param name="source">исходная строка, от неё вызывается эта функция</param>
        /// <param name="toCheck">подстрока, вхождение которой надо проверить</param>
        /// <returns>
        /// true - содержит
        /// false - не содержит
        /// </returns>
        public static bool containsIgnoreCase(string source, string toCheck)
        {
            return source.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
