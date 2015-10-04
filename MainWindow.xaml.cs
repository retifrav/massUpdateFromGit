using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using System.Windows.Threading;
using System.Xml.Linq;
using IWshRuntimeLibrary;
using Ookii.Dialogs.Wpf;
using updateFromGit.wins;

namespace updateFromGit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// имя каталога со сценариями
        /// </summary>
        private string dirOfScenaries = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            Properties.Settings.Default.dirOfScenaries
            );
        /// <summary>
        /// Текущий сценарий
        /// </summary>
        private Scenario currentScenario;
        /// <summary>
        ///  Наличие сценария в работе
        /// </summary>
        private bool isActive = false;
        /// <summary>
        /// Текущий сценарий сохранён
        /// </summary>
        private bool isSaved = true;
        /// <summary>
        /// Выполняется обновление
        /// </summary>
        private bool isUpdating = false;
        /// <summary>
        /// Регулярное выражение проверки ссылки на Git репозиторий
        /// </summary>
        //private Regex gitlink = new Regex(
        //    string.Format(@"^http://{0}/scm/git/\w*$", Properties.Settings.Default.scm_server)
        //    );
        /// <summary>
        /// Заголовок окна по умолчанию
        /// </summary>
        private string windowDefaultTitle = "massUpdateFromGit";

        private CancellationTokenSource cts;

        public MainWindow()
        {
            InitializeComponent();

            this.Closing += windowClosing;

            // создание директории, если нету
            if (!Directory.Exists(dirOfScenaries))
            {
                Directory.CreateDirectory(dirOfScenaries);
            }

            activateWindow(false);
        }
        public void windowClosing(object sender, CancelEventArgs e)
        {
            if (!checkBeforeExit()) { e.Cancel = true; }
        }


        /// <summary>
        /// Отображение интерфейса в зависимости от наличия сценария
        /// </summary>
        /// <param name="activate">наличие сценария, созданного или открытого</param>
        private void activateWindow(bool activate)
        {
            if (activate)
            {
                isActive = true;

                greetGrid.Visibility = System.Windows.Visibility.Hidden;
                mainGrid.Visibility = System.Windows.Visibility.Visible;

                crntScenName.Content = currentScenario.name;
                crntScenName.Foreground = new SolidColorBrush(Colors.Black);

                mn_update.IsEnabled = true;
                mn_shortcut.IsEnabled = true;

                this.Title = windowDefaultTitle;
                this.Title = string.Format("{0} | {1}", this.Title, currentScenario.name);
            }
            else
            {
                isActive = false;

                greetGrid.Visibility = System.Windows.Visibility.Visible;
                mainGrid.Visibility = System.Windows.Visibility.Hidden;

                crntScenName.Content = "неизвестно";
                crntScenName.Foreground = new SolidColorBrush(Colors.Red);

                mn_save.IsEnabled = false;
                mn_update.IsEnabled = false;
                mn_shortcut.IsEnabled = false;

                this.Title = windowDefaultTitle;
            }
        }

        /// <summary>
        /// Окно "О программе"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void showAbout(object sender, RoutedEventArgs e)
        {
            HelpWindow about = new HelpWindow();
            about.ShowDialog();
        }

        /// <summary>
        /// Выход из приложения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void shutdownApp(object sender, RoutedEventArgs e)
        {
            if (checkBeforeExit())
            {
                App.Current.Shutdown(0);
            }
        }

        private bool checkBeforeExit()
        {
            //if (!isActive) { return true; }
            if (currentScenario != null && !isSaved)
            {
                MessageBoxResult what2do = MessageBox.Show(
                    "В текущем сценарии имеются несохранённые изменения. Вы хотите их сохранить?",
                    "Изменения не сохранены",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                    );
                if (what2do == MessageBoxResult.Yes)
                {
                    saveScenario();

                    isActive = false;
                    return true;
                }
                else
                {
                    if (what2do == MessageBoxResult.No)
                    {
                        isActive = false;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void createNew_clicked(object sender, RoutedEventArgs e)
        {
            if (!checkBeforeExit()) { return; }

            try
            {
                NewScenario newscen = new NewScenario();
                if (newscen.ShowDialog() == true)
                {
                    currentScenario = new Scenario(
                        newscen.NameOfScenario,
                        System.IO.Path.Combine(dirOfScenaries, newscen.NameOfScenario + ".xml")
                        );
                    if (!currentScenario.exportScenario2XML(dirOfScenaries))
                    {
                        MessageBox.Show(
                            "Не удалось экспортировать сценарий в XML. Однако, вы можете продолжать работать с ним.",
                            "Ошибка экспорта сценария",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                            );
                    }

                    gitRepo.Clear();
                    lb_paths.Items.Clear();

                    activateWindow(true);
                    changeSavedStatus(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при создании файла сценария. " + ex.Message,
                    "Ошибка создания нового сценария",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
            }
        }

        private void save_clicked(object sender, RoutedEventArgs e)
        {
            saveScenario();
        }

        private bool saveScenario()
        {
            List<string> paths = new List<string>();
            foreach (var item in lb_paths.Items) { paths.Add(item.ToString()); }

            try
            {
                currentScenario.updateScenario(
                    gitRepo.Text.Trim(),
                    tb_localRepo.Text.Trim(),
                    tb_branch.Text.Trim(),
                    paths
                    );

                changeSavedStatus(true);

                return true;

                //MessageBox.Show(
                //    string.Format("Сценарий {0} успешно сохранён.", currentScenario.name),
                //    "Сценарий сохранён",
                //    MessageBoxButton.OK,
                //    MessageBoxImage.Information
                //    );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при сохранении сценария в файл. " + ex.Message,
                    "Ошибка сохранения сценария",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
                
                return false;
            }
        }

        private void open_clicked(object sender, RoutedEventArgs e)
        {
            if (!checkBeforeExit()) { return; }

            string path = string.Empty;
            VistaOpenFileDialog dialog = new VistaOpenFileDialog();
            dialog.Multiselect = false;
            dialog.Filter = "XML file (*.xml)|*.xml";
            if ((bool)dialog.ShowDialog(this)) { path = dialog.FileName; } else { return;}

            loadScenario(path);
        }

        public void loadScenario(string path)
        {
            try { currentScenario = new Scenario(path); }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при открытии сценария. Возможно, этот файл не является сценарием для обновления. Перехваченное исключение: " + ex.Message,
                    "Не удалось открыть сценарий",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
                return;
            }

            crntScenName.Content = currentScenario.name;
            gitRepo.Text = currentScenario.gitLink;
            tb_localRepo.Text = currentScenario.gitLocal;
            tb_branch.Text = currentScenario.gitBranch;
            

            lb_paths.Items.Clear();
            foreach (string p2s in currentScenario.paths2servers) { lb_paths.Items.Add(p2s); }

            activateWindow(true); // if (!isActive) { activateWindow(true); }
            changeSavedStatus(true);
        }

        private void update_clicked(object sender, RoutedEventArgs e)
        {
            if (!updateORstop(isUpdating)) { return; }

            if (!isUpdating)
            {
                // запуск процесса обновления без блокировки GUI с возможностью его отмены
                cts = new CancellationTokenSource();
                var task = Task.Run(() => doTheShit(currentScenario, cts.Token), cts.Token);
                Task uiTask = task.ContinueWith((continuation) =>
                {
                    updateORstop(true);
                    isUpdating = false;
                    MessageBox.Show(
                        "Выполнение задания по сценарию обновления закончено.",
                        "Процесс завершён"
                        );
                }, TaskScheduler.FromCurrentSynchronizationContext());
                
                isUpdating = true;
            }
        }

        private void doTheShit(Scenario currentScenario, CancellationToken cancellationToken)
        {
            int max = currentScenario.paths2servers.Count;
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                pb_updating.Value = 0;
                pb_updating.Maximum = max;
            }
            ));

            try
            {
                foreach (string path2update in currentScenario.paths2servers)
                {
                    string path = path2update.TrimEnd('\\');
                    // [debug]
                    //Thread.Sleep(1500);

                    // получить из центрального репозитория последнюю инфу
                    using (Process proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git.exe",
                            Arguments = string.Format("--git-dir=\"{0}\" --work-tree=\"{1}\" fetch --all",
                                System.IO.Path.Combine(path, ".git"),
                                path
                                ),
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    })
                    {
                        proc.Start();
                        while (!proc.HasExited) { }
                    }

                    // выкатить в репозиторий свежую ветку
                    using (Process proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git.exe",
                            Arguments = string.Format("--git-dir=\"{0}\" --work-tree=\"{1}\" reset --hard {2}",
                                System.IO.Path.Combine(path, ".git"),
                                path,
                                currentScenario.gitBranch
                                ),
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    })
                    {
                        proc.Start();
                        while (!proc.HasExited) { }
                    }

                    // прогресс
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        pb_updating.Value++;
                    }
                    ));

                    // проверяем, не был ли процесс отменён 
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    MessageBox.Show(
                        "Во время обновления произошла ошибка. Перехваченное исключение: " + ex.Message,
                        "Ошибка обновления репозиториев на серверах",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );
                }
                ));
            }
        }

        private bool updateORstop(bool isUpdating)
        {
            if (isUpdating)
            {
                cts.Cancel();

                mn_create.IsEnabled = true;
                mn_open.IsEnabled = true;
                mn_shortcut.IsEnabled = true;

                dp_repo.Visibility = System.Windows.Visibility.Visible;
                pb_updating.Visibility = System.Windows.Visibility.Hidden;

                mn_update.Header = "Выполнить обновление";
                mn_update.ToolTip = "Выполнить обновление на серверах по текущему сценарию";
                mn_update.Icon = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri("/reses/update.ico", UriKind.Relative)),
                    Width = 16
                };

                btn_doTheShit.Content = FindResource("update");

                return true;
            }
            else
            {
                // сохранение сценария перед его выполнением
                if (!saveScenario())
                {
                    MessageBox.Show(
                        "Не удалось сохранить сценарий перед обновлением.",
                        "Обновление отменено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );

                    return false;
                }

                // проверка на отсутствующую ссылку на репозиторий
                //if (string.IsNullOrEmpty(currentScenario.gitLink)
                //    || !gitlink.IsMatch(currentScenario.gitLink)
                //    )
                //{
                //    MessageBox.Show(
                //        "В сценарии не указан корректный Git-репозиторий проекта.",
                //        "Обновление отменено",
                //        MessageBoxButton.OK,
                //        MessageBoxImage.Error
                //        );

                //    return false;
                //}

                // проверка на пустой список путей обновления
                if (currentScenario.paths2servers.Count == 0)
                {
                    MessageBox.Show(
                        "В сценарии отсутствуют пути обновления проекта на серверах.",
                        "Обновление отменено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );

                    return false;
                }

                // проверка существования локального репозитория
                if (!Directory.Exists(System.IO.Path.Combine(tb_localRepo.Text.Trim(), ".git")))
                {
                    MessageBox.Show(
                        "Не найден локальный репозиторий по указанному пути.",
                        "Обновление отменено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );

                    return false;
                }

                // проверка существования указанной ветви
                if (string.IsNullOrEmpty(tb_branch.Text.Trim()))
                {
                    MessageBox.Show(
                        "Не указана ветвь для обновления.",
                        "Обновление отменено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );

                    return false;
                }

                bool branchIsOk = false;
                try
                {
                    using (Process proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git.exe",
                            Arguments = string.Format("--git-dir={0} branch -r",
                                System.IO.Path.Combine(tb_localRepo.Text.Trim(), ".git")),
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    })
                    {
                        proc.Start();
                        string output = App.getProcessOutput(proc);
                        string[] outputs = output.Split(new string[] {"\r\n", " "}, StringSplitOptions.None);
                        foreach (string o in outputs)
                        {
                            if (o == tb_branch.Text.Trim()) { branchIsOk = true; break; }
                        }
                    }
                }
                catch { branchIsOk = false; }
                if (!branchIsOk)
                {
                    MessageBox.Show(
                        "В репозитории отсутствует указанная ветвь.",
                        "Обновление отменено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );

                    return false;
                }

                mn_create.IsEnabled = false;
                mn_open.IsEnabled = false;
                mn_shortcut.IsEnabled = false;

                dp_repo.Visibility = System.Windows.Visibility.Hidden;
                pb_updating.Visibility = System.Windows.Visibility.Visible;

                mn_update.Header = "Остановить обновление";
                mn_update.ToolTip = "Остановить процесс обновления (не рекомендуется)";
                mn_update.Icon = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri("/reses/stop.ico", UriKind.Relative)),
                    Width = 16
                };

                btn_doTheShit.Content = FindResource("stop");

                return true;
            }
        }

        private void shortcut_clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                object shDesktop = (object)"Desktop";
                WshShell shell = new WshShell();
                string shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + string.Format(@"\Сценарий обновления {0}.lnk", currentScenario.name);
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
                shortcut.Description = "Открытие сценария обновления";
                shortcut.TargetPath = Assembly.GetExecutingAssembly().Location;
                shortcut.Arguments =  string.Format("-s {0}", currentScenario.path2save);
                shortcut.IconLocation = Assembly.GetExecutingAssembly().Location + ", 0 ";
                shortcut.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при создании ярлыка. Перехваченное исключение: " + ex.Message,
                    "Не удалось создать ярлык",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
                return;
            }
                        
            MessageBox.Show(
                "Ярлык для запуска приложения с текущим сценарием создан на вашем Рабочем столе",
                "Ярлык успешно создан",
                MessageBoxButton.OK,
                MessageBoxImage.Information
                );
        }

        private void btn_addPath_clicked(object sender, RoutedEventArgs e)
        {
            NewPath newpath = new NewPath();
            if (newpath.ShowDialog() == true)
            {
                if (!lb_paths.Items.Contains(newpath.path2dir))
                {
                    lb_paths.Items.Add(newpath.path2dir);
                    changeSavedStatus(false);
                }
                else
                {
                    MessageBox.Show(
                        "Этот путь уже содержится в списке",
                        "Повтор",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                        );
                }
            }
        }

        private void btn_delPath_clicked(object sender, RoutedEventArgs e)
        {
            if (lb_paths.SelectedIndex >= 0)
            {
                lb_paths.Items.RemoveAt(lb_paths.SelectedIndex);
                changeSavedStatus(false);
            }
        }

        /// <summary>
        /// Меняет статус сценария и включает или отключает пункт сохранения
        /// </summary>
        /// <param name="saved">новое значение статуса</param>
        public void changeSavedStatus(bool saved)
        {
            isSaved = saved;
            mn_save.IsEnabled = !saved;
            if (saved) { this.Title = this.Title.Replace("*", string.Empty); }
            else { if (!this.Title.Contains("*")) { this.Title = this.Title + "*"; } }
        }

        private void gitRepo_changed(object sender, TextChangedEventArgs e)
        {
            //if (gitlink.IsMatch(gitRepo.Text.Trim()))
            //{
            //    gitRepo.BorderBrush = Brushes.Green;
            //}
            //else
            //{
            //    gitRepo.BorderBrush = Brushes.Red;
            //}
            if (currentScenario != null) { changeSavedStatus(false); }
        }

        private void gitLocal_changed(object sender, TextChangedEventArgs e)
        {
            if (currentScenario != null) { changeSavedStatus(false); }
        }

        private void gitBranch_changed(object sender, TextChangedEventArgs e)
        {
            if (currentScenario != null) { changeSavedStatus(false); }
        }

        private void btn_dir_clicked(object sender, RoutedEventArgs e)
        {
            NewPath newpath = new NewPath();
            if (newpath.ShowDialog() == true)
            {
                tb_localRepo.Text = newpath.path2dir;
            }
        }
    }
}
