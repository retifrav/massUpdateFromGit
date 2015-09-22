using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using Ookii.Dialogs.Wpf;

namespace updateFromGit
{
    public partial class NewPath : Window
    {
        public string path2dir
        {
            get { return path.Text.Trim().TrimEnd('\\'); }
        }

        public NewPath()
        {
            InitializeComponent();

            path.Focus();
        }

        private void createClicked(object sender, RoutedEventArgs e)
        {
            string dir = path.Text.Trim();
            if (string.IsNullOrEmpty(dir))
            {
                MessageBox.Show(
                    "Вы не указали путь до каталога.",
                    "Не выбран каталог",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                    );
            }
            else
            {
                if (Directory.Exists(dir)) { this.DialogResult = true; }
                else
                {
                    MessageBox.Show(
                        "Не удалось найти каталог по указанному пути.",
                        "Каталог не найден",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                        );
                }
            }
        }

        private void cancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void btn_dir_clicked(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            if ((bool)dialog.ShowDialog(this)) { path.Text = dialog.SelectedPath + @"\"; }

        }
    }
}
