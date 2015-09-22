using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace updateFromGit
{
    public partial class NewScenario : Window
    {
        public string NameOfScenario
        {
            get { return scenarioName.Text.Trim(); }
        }

        public NewScenario()
        {
            InitializeComponent();

            scenarioName.Focus();
        }

        private void createClicked(object sender, RoutedEventArgs e)
        {
            string sn = scenarioName.Text.Trim();
            if (string.IsNullOrEmpty(sn))
            {
                MessageBox.Show(
                    "Вы не указали имя сценария. Введите имя сценария в поле ввода и нажмите на кнопку Создать.",
                    "Создание сценария отменено",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                    );
            }
            else
            {
                Regex regex = new Regex("^[a-zA-Zа-яА-Я0-9]*$");
                if (!regex.IsMatch(sn))
                {
                    MessageBox.Show(
                        "В поле имени сценария введены недопустимые символы. Имя должно быть "
                            + "без пробелов и может состоять только из букв алфавита и цифр.",
                        "Некорректное имя сценария",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
                else
                {
                    if (File.Exists(System.IO.Path.Combine(
                        Properties.Settings.Default.dirOfScenaries,
                        scenarioName.Text.Trim() + ".xml"
                        )))
                    {
                        MessageBox.Show(
                            "Файл с таким именем уже существует. Выберите другое.",
                            "Недопустимое имя сценария",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                            );
                        return;
                    }
                    { this.DialogResult = true; }
                }
            }
        }

        private void cancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
