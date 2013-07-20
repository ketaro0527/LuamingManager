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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.ComponentModel;
using System.Net.Json;
using Ionic.Zip;
using System.Threading;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Windows.Threading;

namespace LuamingManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string projectPath = null;
        private string exportPath;
        private string lastBrowsingDataFile = System.Windows.Forms.Application.UserAppDataPath + @"\lastBrowsingPath.txt";
        private string luamingReferenceURL = "http://210.118.74.97/LuamingReference/";

        private BackgroundWorker thread;
        private ProgressDialog pd;
        /*
        private delegate void UpdateValueLabel(System.Windows.Controls.Label label, string str);
        private delegate void UpdateValueTextBox(System.Windows.Controls.TextBox textbox, string str);
        private delegate void UpdateValueButton(System.Windows.Controls.Button button, bool enable);

        private void UpdateLabelContent(System.Windows.Controls.Label label, string str)
        {
            label.Content = str;
        }

        private void UpdateTextBoxText(System.Windows.Controls.TextBox textbox, string str)
        {
            textbox.Text = str;
        }

        private void UpdateButtonEnable(System.Windows.Controls.Button button, bool enable)
        {
            button.IsEnabled = enable;
        }
        */
        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (WindowStyle != WindowStyle.None)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (DispatcherOperationCallback)delegate(object unused)
                {
                    WindowStyle = WindowStyle.None;
                    return null;
                }
                , null);
            }
        }

        private void create_button_Click(object sender, RoutedEventArgs e)
        {
            createProject();
        }

        private void load_button_Click(object sender, RoutedEventArgs e)
        {
            loadProject();
        }

        private void simulate_button_Click(object sender, RoutedEventArgs e)
        {
            if (checkProjectValid())
            {
                string simulatorPath = System.Windows.Forms.Application.StartupPath;
                if (getOrientation().Equals("landscape"))
                    simulatorPath += @"\Simulator\LuamingSimulator.exe";
                else
                    simulatorPath += @"\Simulator\LuamingSimulator.Portrait.exe";

                System.Diagnostics.Process.Start(simulatorPath, projectPath);
            }
        }

        private void export_button_Click(object sender, RoutedEventArgs e)
        {
            if (checkProjectValid())
            {
                System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
                sfd.Title = "Export Luaming Project";
                sfd.DefaultExt = "apk";
                sfd.Filter = "Luaming Application Package (*.apk)|*.apk";
                sfd.CheckPathExists = true;
                sfd.FileName = project_name_label.Content + ".apk";
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    exportPath = sfd.FileName;

                    thread = new BackgroundWorker();
                    thread.DoWork += new DoWorkEventHandler(thread_DoWork_Export);
                    thread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(thread_RunWorkerCompleted_Export);
                    thread.RunWorkerAsync();
                }
            }
        }

        private void createProject()
        {
            ProjectCreateWindow pcWindow = new ProjectCreateWindow();
            if (pcWindow.ShowDialog() == true)
            {
                StreamWriter sw = File.CreateText(lastBrowsingDataFile);
                sw.WriteLine(projectPath);
                sw.Close();

                project_name_label.Content = System.IO.Path.GetFileName(projectPath);
                project_path_textbox.Text = projectPath;
                simulate_button.IsEnabled = true;
                export_button.IsEnabled = true;
                config_button.IsEnabled = true;
                openproj_button.IsEnabled = true;
                edit_script_button.IsEnabled = true;
            }
        }

        private void thread_DoWork_Export(object sender, DoWorkEventArgs e)
        {
            exportProject();
        }

        private void thread_RunWorkerCompleted_Export(object sender, RunWorkerCompletedEventArgs e)
        {
            System.Windows.MessageBox.Show(project_name_label.Content + " is successfully exported!!");
        }


        private void loadProject()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (File.Exists(lastBrowsingDataFile)) {
                StreamReader sr = File.OpenText(lastBrowsingDataFile);
                string lastPath = sr.ReadToEnd();
                sr.Close();
                dialog.SelectedPath = lastPath.Trim() ;
            }
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = dialog.SelectedPath;
                StreamWriter sw = File.CreateText(lastBrowsingDataFile);
                sw.WriteLine(path);
                sw.Close();

                if (isValidProjectPath(path))
                {
                    projectPath = path;
                    project_path_textbox.Text = projectPath;
                    project_name_label.Content = System.IO.Path.GetFileName(projectPath);
                    simulate_button.IsEnabled = true;
                    export_button.IsEnabled = true;
                    config_button.IsEnabled = true;
                    openproj_button.IsEnabled = true;
                    edit_script_button.IsEnabled = true;
                }
                else
                {
                    if (System.Windows.Forms.MessageBox.Show("\"" + path + "\"\nis not a Luaming Project folder") == System.Windows.Forms.DialogResult.OK)
                        loadProject();
                }
            }
        }

        private bool isValidProjectPath(string path)
        {
            if (!Directory.Exists(path + @"\assets"))
                return false;
            if (!File.Exists(path + @"\assets\LuamingProject.json"))
                return false;
            return true;
        }

        private void exportProject()
        {
            ZipFile zip = new ZipFile();
            
            string[] dirs = Directory.GetDirectories(projectPath);
            foreach (string dir in dirs)
            {
                string dirName = System.IO.Path.GetFileName(dir);
                zip.AddDirectory(dir, dirName);   
            }
            
            string[] files = Directory.GetFiles(projectPath);
            foreach (string file in files)
            {
                zip.AddFile(file, @"");
            }

            zip.Save(exportPath);
        }

        private bool checkProjectValid()
        {
            string projectConfigPath = projectPath + @"\assets\LuamingProject.json";
            if (!File.Exists(projectConfigPath))
            {
                System.Windows.MessageBox.Show("LuamingProject.json 파일이 없습니다!");
                return false;
            }
            try
            {
                StreamReader sr = File.OpenText(projectConfigPath);
                string config = sr.ReadToEnd();
                sr.Close();

                JsonTextParser jsonParser = new JsonTextParser();
                JsonObjectCollection configObject = (JsonObjectCollection)jsonParser.Parse(config);
                string projectName = configObject["PROJECT_NAME"].GetValue().ToString();
                if (!Regex.IsMatch(projectName, @"^[a-zA-Z](\w?)+"))
                {
                    System.Windows.MessageBox.Show("LuamingProject.json: 프로젝트 이름이 올바르지 않습니다!");
                    return false;
                }
                string packageName = configObject["PACKAGE_NAME"].GetValue().ToString();
                if (!Regex.IsMatch(packageName, @"^[a-zA-Z]([a-zA-Z0-9_]?)+((\.[a-zA-Z0-9_]+)+)$"))
                {
                    System.Windows.MessageBox.Show("LuamingProject.json: 패키지 이름이 올바르지 않습니다!");
                    return false;
                }
                string versionName = configObject["VERSION_NAME"].GetValue().ToString();
                if (!Regex.IsMatch(versionName, @"^[1-9]([0-9]?)+((\.[0-9]+)+)$"))
                {
                    System.Windows.MessageBox.Show("LuamingProject.json: 버전 이름이 올바르지 않습니다!");
                    return false;
                }
                if (!configObject["VERSION_CODE"].GetValue().GetType().ToString().Equals("System.Double"))
                {
                    System.Windows.MessageBox.Show("LuamingProject.json: 버전 코드가 올바르지 않습니다!");
                    return false;
                }
                string mainScript = configObject["MAIN_SCRIPT"].GetValue().ToString();
                if (!File.Exists(projectPath + @"\assets\" + mainScript))
                {
                    System.Windows.MessageBox.Show("LuamingProject.json: 메인 스크립트가 존재하지 않습니다!");
                    return false;
                }
                string orientation = configObject["ORIENTATION"].GetValue().ToString().ToLower();
                if (!orientation.Equals("landscape") && !orientation.Equals("portrait"))
                {
                    System.Windows.MessageBox.Show("LuamingProject.json: Orientation이 올바르지 않습니다!");
                    return false;
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
                return false;
            }
            return true;
        }

        private string getOrientation()
        {
            string projectConfigPath = projectPath + @"\assets\LuamingProject.json";
            string orientation = "landscape";
            if (!File.Exists(projectConfigPath))
            {
                System.Windows.MessageBox.Show("LuamingProject.json 파일이 없습니다!");
                return orientation;
            }
            try
            {
                StreamReader sr = File.OpenText(projectConfigPath);
                string config = sr.ReadToEnd();
                sr.Close();

                JsonTextParser jsonParser = new JsonTextParser();
                JsonObjectCollection configObject = (JsonObjectCollection)jsonParser.Parse(config);
                orientation = configObject["ORIENTATION"].GetValue().ToString().ToLower();
                if (!orientation.Equals("landscape") && !orientation.Equals("portrait"))
                {
                    return "landscape";
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
                return "landscape";
            }
            return orientation;
        }

        private void config_button_Click(object sender, RoutedEventArgs e)
        {
            string configFilePath = projectPath + @"\assets\LuamingProject.json";
            if (!File.Exists(configFilePath))
            {
                System.Windows.MessageBox.Show(configFilePath + " 파일이 없습니다!");
                return;
            }
            System.Diagnostics.Process.Start("notepad.exe", configFilePath);
        }

        private void openproj_button_Click(object sender, RoutedEventArgs e)
        {
            string assetsPath = projectPath + @"\assets";
            if (!Directory.Exists(assetsPath))
            {
                System.Windows.MessageBox.Show(assetsPath + " 폴더가 없습니다!");
                return;
            }
            System.Diagnostics.Process.Start("explorer.exe", assetsPath);
        }

        private void edit_script_button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegistryKey reg = Registry.ClassesRoot;
                reg = reg.CreateSubKey(@"Lua.Script\Shell\Edit\Command", RegistryKeyPermissionCheck.ReadSubTree);
                string path = reg.GetValue("").ToString();
                reg.Close();
                if (path != null && path.Length > 0)
                {
                    char[] spliter = { '\"' };
                    path = path.Split(spliter)[1];

                    System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                    openFileDialog.Filter = "Lua Script Files (*.lua)|*.lua";
                    openFileDialog.Title = "Select Lua Script File to edit";
                    openFileDialog.InitialDirectory = projectPath + @"\assets";

                    if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        System.Diagnostics.Process.Start(path, openFileDialog.FileName);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Lua for Windows:SciTE를 설치해주세요!");
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Windows.MessageBox.Show("Lua for Windows:SciTE를 설치해주세요!");
            }
        }

        private void title_bar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void close_button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void minimize_button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Minimized;
        }

        private void title_label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void reference_button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                RegistryKey reg = Registry.ClassesRoot;
                reg = reg.CreateSubKey(@"ChromeHTML\shell\open\command", RegistryKeyPermissionCheck.ReadSubTree);
                string path = reg.GetValue("").ToString();
                reg.Close();
                if (path != null && path.Length > 0)
                {
                    char[] spliter = { '\"' };
                    path = path.Split(spliter)[1];

                    System.Diagnostics.Process.Start(path, luamingReferenceURL);
                }
                else
                {
                    System.Diagnostics.Process.Start("explorer.exe", luamingReferenceURL);
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Diagnostics.Process.Start("explorer.exe", luamingReferenceURL);
            }
        }
    }
}
