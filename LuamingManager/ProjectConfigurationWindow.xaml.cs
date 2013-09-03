using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Net.Json;

namespace LuamingManager
{
    /// <summary>
    /// Interaction logic for ProjectConfigurationWindow.xaml
    /// </summary>
    public partial class ProjectConfigurationWindow : Window
    {
        bool isPackageValid = true;
        bool isProjectNameValid = true;
        bool isVersionNameValid = true;
        bool isVersionCodeValid = true;

        public ProjectConfigurationWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Init();
            setChangeButtonEnabled();
        }

        private void Init()
        {
            string projectConfigPath = MainWindow.projectPath + @"\assets\LuamingProject.json";
            if (!File.Exists(projectConfigPath))
            {
                System.Windows.MessageBox.Show("LuamingProject.json 파일이 없습니다!");
                Close();
            }
            try
            {
                StreamReader sr = File.OpenText(projectConfigPath);
                string config = sr.ReadToEnd();
                sr.Close();

                JsonTextParser jsonParser = new JsonTextParser();
                JsonObjectCollection configObject = (JsonObjectCollection)jsonParser.Parse(config);
                project_name_textbox.Text = configObject["PROJECT_NAME"].GetValue().ToString();
                package_name_textbox.Text = configObject["PACKAGE_NAME"].GetValue().ToString();
                version_name_textbox.Text = configObject["VERSION_NAME"].GetValue().ToString();
                version_code_textbox.Text = configObject["VERSION_CODE"].GetValue().ToString();
                JsonObject offlineIconObject = configObject["OFFLINE_ICON"];
                if (offlineIconObject != null)
                    offline_icon_textbox.Text = offlineIconObject.GetValue().ToString();
                main_script_textbox.Text = configObject["MAIN_SCRIPT"].GetValue().ToString();
                string orientation = configObject["ORIENTATION"].GetValue().ToString().ToLower();
                if (orientation.Equals("portrait"))
                {
                    radioLandscape.IsChecked = false;
                    radioPortrait.IsChecked = true;
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
                this.DialogResult = false;
                Close();
            }
        }

        public bool AreAllValidNumericChars(string str)
        {
            bool ret = true;
            if (str == System.Globalization.NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.CurrencyGroupSeparator |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.CurrencySymbol |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.NegativeSign |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.NegativeInfinitySymbol |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.PercentDecimalSeparator |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.PercentGroupSeparator |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.PercentSymbol |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.PerMilleSymbol |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.PositiveInfinitySymbol |
                str == System.Globalization.NumberFormatInfo.CurrentInfo.PositiveSign)
                return ret;

            int l = str.Length;
            for (int i = 0; i < l; i++)
            {
                char ch = str[i];
                ret &= Char.IsDigit(ch);
            }
            return ret;
        }

        private void setChangeButtonEnabled()
        {
            if (project_name_textbox.Text.ToString().Length > 0 &&
                package_name_textbox.Text.ToString().Length > 0 &&
                version_name_textbox.Text.ToString().Length > 0 &&
                version_code_textbox.Text.ToString().Length > 0 &&
                main_script_textbox.Text.ToString().Length > 0 &&
                offline_icon_textbox.Text.ToString().Length > 0 &&
                isPackageValid && isProjectNameValid &&
                isVersionNameValid && isVersionCodeValid)
                change_configuration_button.IsEnabled = true;
            else
                change_configuration_button.IsEnabled = false;
        }

        private void createProjectInformationFile()
        {
            // Create Project Information File
            JsonObjectCollection luamingJson = new JsonObjectCollection();
            JsonStringValue projectNameJson = new JsonStringValue("PROJECT_NAME");
            projectNameJson.Value = project_name_textbox.Text.ToString();
            JsonStringValue packageNameJson = new JsonStringValue("PACKAGE_NAME");
            packageNameJson.Value = package_name_textbox.Text.ToString();
            JsonStringValue versionNameJson = new JsonStringValue("VERSION_NAME");
            versionNameJson.Value = version_name_textbox.Text.ToString();
            JsonNumericValue versionCodeJson = new JsonNumericValue("VERSION_CODE");
            versionCodeJson.Value = Int32.Parse(version_code_textbox.Text.ToString());
            JsonStringValue offlineIconJson = new JsonStringValue("OFFLINE_ICON");
            offlineIconJson.Value = offline_icon_textbox.Text.ToString();
            JsonStringValue mainScriptJson = new JsonStringValue("MAIN_SCRIPT");
            mainScriptJson.Value = main_script_textbox.Text.ToString();
            JsonStringValue orientationJson = new JsonStringValue("ORIENTATION");
            if (radioLandscape.IsChecked == true)
                orientationJson.Value = "landscape";
            else
                orientationJson.Value = "portrait";
            luamingJson.Add(projectNameJson);
            luamingJson.Add(packageNameJson);
            luamingJson.Add(versionNameJson);
            luamingJson.Add(versionCodeJson);
            luamingJson.Add(offlineIconJson);
            luamingJson.Add(mainScriptJson);
            luamingJson.Add(orientationJson);

            StreamWriter sw = File.CreateText(MainWindow.projectPath + @"\assets\LuamingProject.json");
            sw.Write(luamingJson.ToString());
            sw.Close();
        }

        private void project_name_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isProjectNameValid = Regex.IsMatch(project_name_textbox.Text, @"^[a-zA-Z](\w?)+");
            setChangeButtonEnabled();
        }

        private void package_name_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isPackageValid = Regex.IsMatch(package_name_textbox.Text, @"^[a-zA-Z]([a-zA-Z0-9_]?)+((\.[a-zA-Z0-9_]+)+)$");
            setChangeButtonEnabled();
        }

        private void version_name_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isVersionNameValid = Regex.IsMatch(version_name_textbox.Text, @"^[1-9]([0-9]?)+((\.[0-9]+)+)$");
            setChangeButtonEnabled();
        }

        private void version_code_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isVersionCodeValid = AreAllValidNumericChars(version_code_textbox.Text);
            setChangeButtonEnabled();
        }

        private void icon_browse_button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp) | *.jpg; *.jpeg; *.png; *.bmp";
            dialog.Title = "Select Image File for Offline Icon";
            dialog.InitialDirectory = MainWindow.projectPath + @"\assets";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (dialog.FileName.Contains(MainWindow.projectPath))
                {
                    string[] separator = { "assets\\" };
                    string[] temp = dialog.FileName.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    offline_icon_textbox.Text = temp[1].Replace('\\', '/');
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("프로젝트 폴더 내에 있는 파일을 선택해주세요!");
                    icon_browse_button_Click(sender, e);
                }
            }

            setChangeButtonEnabled();
        }

        private void script_browse_button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Lua Script Files (*.lua)|*.lua";
            dialog.Title = "Select Lua Script File";
            dialog.InitialDirectory = MainWindow.projectPath + @"\assets";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (dialog.FileName.Contains(MainWindow.projectPath))
                {
                    string[] separator = { "assets\\" };
                    string[] temp = dialog.FileName.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    main_script_textbox.Text = temp[1].Replace('\\', '/');
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("프로젝트 폴더 내에 있는 파일을 선택해주세요!");
                    script_browse_button_Click(sender, e);
                }
            }

            setChangeButtonEnabled();
        }

        private void label3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void image1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void close_button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private void cancel_button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private void change_configuration_button_Click(object sender, RoutedEventArgs e)
        {
            createProjectInformationFile();
            this.DialogResult = true;
            Close();
        }
    }
}