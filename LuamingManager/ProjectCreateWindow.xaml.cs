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
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;
using System.Net.Json;
using System.IO;
using System.Text.RegularExpressions;

namespace LuamingManager
{
    /// <summary>
    /// Interaction logic for ProjectCreateWindow.xaml
    /// </summary>
    public partial class ProjectCreateWindow : Window
    {
        private BackgroundWorker thread;
        private ProgressDialog pd;

        private int maxFileCount = 0;
        private int currentFileCount = 0;
        private string srcPath;
        private string dstPath;
        private string projectName;
        private string packageName;
        private bool isProjectNameValid;
        private bool isPackageValid;
        private bool isLandscape = true;

        public ProjectCreateWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            project_name_textbox.KeyUp += new System.Windows.Input.KeyEventHandler(project_name_textbox_KeyUp);
            package_name_textbox.KeyUp += new System.Windows.Input.KeyEventHandler(package_name_textbox_KeyUp);
        }

        private void browse_button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Set path
                srcPath = System.Windows.Forms.Application.StartupPath + @"\Template";
                dstPath = dialog.SelectedPath;
                project_location_textbox.Text = dstPath;

                setCreateButtonEnabled();
            }
        }

        private void create_project_button_Click(object sender, RoutedEventArgs e)
        {
            isLandscape = (bool)radioLandscape.IsChecked;

            thread = new BackgroundWorker();
            thread.DoWork += new DoWorkEventHandler(thread_DoWork);
            thread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(thread_RunWorkerCompleted);
            thread.RunWorkerAsync();

            pd = new ProgressDialog();
            pd.Show();
        }

        private void cancel_button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void project_name_textbox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            projectName = project_name_textbox.Text;
            isProjectNameValid = Regex.IsMatch(projectName, @"^[a-zA-Z](\w?)+");
            setCreateButtonEnabled();
        }

        private void package_name_textbox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            packageName = package_name_textbox.Text;
            isPackageValid = Regex.IsMatch(packageName, @"^[a-zA-Z]([a-zA-Z0-9_]?)+((\.[a-zA-Z0-9_]+)+)$");
            setCreateButtonEnabled();
        }

        private void setCreateButtonEnabled()
        {
            if (project_name_textbox.Text.ToString().Length > 0 &&
                package_name_textbox.Text.ToString().Length > 0 &&
                project_location_textbox.Text.ToString().Length > 0 &&
                isPackageValid && isProjectNameValid)
                create_project_button.IsEnabled = true;
            else
                create_project_button.IsEnabled = false;
        }

        private void thread_DoWork(object sender, DoWorkEventArgs e)
        {
            maxFileCount = fileCount(srcPath);
            currentFileCount = 0;
            Thread.Sleep(100);
            dstPath += @"\" + projectName;
            copyAll(srcPath, dstPath);

            if (isLandscape)
                File.Delete(dstPath + @"\assets\main_portrait.lua");
            else
            {
                File.Delete(dstPath + @"\assets\main.lua");
                File.Move(dstPath + @"\assets\main_portrait.lua", dstPath + @"\assets\main.lua");
            }

            createProjectInformationFile();
        }

        private void thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pd.Finish();

            this.DialogResult = true;
            MainWindow.projectPath = dstPath;

            this.Close();
        }

        private int fileCount(string path)
        {
            int cnt = 0;
            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                cnt += fileCount(dir);
            }

            string[] files = Directory.GetFiles(path);
            cnt += files.Count();

            return cnt;
        }

        private void copyAll(string srcPath, string dstPath)
        {
            string[] dirs = Directory.GetDirectories(srcPath);
            foreach (string dir in dirs)
            {
                string dirName = System.IO.Path.GetFileName(dir);
                string dstDir = dstPath + @"\" + dirName;
                if (!Directory.Exists(dstDir))
                    Directory.CreateDirectory(dstDir);

                copyAll(dir, dstDir);
            }

            string[] files = Directory.GetFiles(srcPath);
            foreach (string file in files)
            {
                string fileName = System.IO.Path.GetFileName(file);
                string dstFile = dstPath + @"\" + fileName;

                pd.UpdateStatus(dstFile);
                File.Copy(file, dstFile, true);
                currentFileCount++;
                pd.UpdateProgress((double)(currentFileCount) / (double)maxFileCount);
            }
        }

        private void createProjectInformationFile()
        {
            // Create Project Information File
            JsonObjectCollection luamingJson = new JsonObjectCollection();
            JsonStringValue projectNameJson = new JsonStringValue("PROJECT_NAME");
            projectNameJson.Value = projectName;
            JsonStringValue packageNameJson = new JsonStringValue("PACKAGE_NAME");
            packageNameJson.Value = packageName;
            JsonStringValue versionNameJson = new JsonStringValue("VERSION_NAME");
            versionNameJson.Value = "1.0.0";
            JsonNumericValue versionCodeJson = new JsonNumericValue("VERSION_CODE");
            versionCodeJson.Value = 1;
            JsonStringValue mainScriptJson = new JsonStringValue("MAIN_SCRIPT");
            mainScriptJson.Value = "main.lua";
            JsonStringValue orientationJson = new JsonStringValue("ORIENTATION");
            if (isLandscape == true)
                orientationJson.Value = "landscape";
            else
                orientationJson.Value = "portrait";
            luamingJson.Add(projectNameJson);
            luamingJson.Add(packageNameJson);
            luamingJson.Add(versionNameJson);
            luamingJson.Add(versionCodeJson);
            luamingJson.Add(mainScriptJson);
            luamingJson.Add(orientationJson);

            StreamWriter sw = File.CreateText(dstPath + @"\assets\LuamingProject.json");
            sw.Write(luamingJson.ToString());
            sw.Close();
        }

        private void image1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void label3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void image2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
