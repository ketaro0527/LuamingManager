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
using Ionic.Zip;

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
        private bool isDemoProject = false;
        private bool createResult = true;

        private string lastCreatingDataFile = System.Windows.Forms.Application.UserAppDataPath + @"\lastCreatingPath.txt";

        public ProjectCreateWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void browse_button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (File.Exists(lastCreatingDataFile))
            {
                StreamReader sr = File.OpenText(lastCreatingDataFile);
                string lastPath = sr.ReadToEnd();
                sr.Close();
                dialog.SelectedPath = lastPath.Trim();
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = dialog.SelectedPath;
                StreamWriter sw = File.CreateText(lastCreatingDataFile);
                sw.WriteLine(path);
                sw.Close();

                // Set path
                srcPath = System.Windows.Forms.Application.StartupPath + @"\Template";
                dstPath = dialog.SelectedPath;
                project_location_textbox.Text = dstPath;

                setCreateButtonEnabled();
            }
        }

        private void create_project_button_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(dstPath + @"\" + project_name_textbox.Text))
            {
                dstPath += @"\" + project_name_textbox.Text;
                isLandscape = (bool)radioLandscape.IsChecked;
                isDemoProject = (bool)radioApiDemo.IsChecked;

                thread = new BackgroundWorker();
                thread.DoWork += new DoWorkEventHandler(thread_DoWork);
                thread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(thread_RunWorkerCompleted);
                thread.RunWorkerAsync();

                pd = new ProgressDialog();
                pd.Show();
            }
            else
            {
                System.Windows.MessageBox.Show("프로젝트 폴더가 이미 존재합니다!");
            }
        }

        private void cancel_button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
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
            if (isDemoProject)
            {
                string demoZipPath = System.Windows.Forms.Application.StartupPath + @"\APIDemo.lmg";
                if (File.Exists(demoZipPath))
                {
                    ZipFile zip = new ZipFile(demoZipPath);
                    if (Directory.Exists(dstPath + @"\APIDemo"))
                    {
                        createResult = false;
                        return;
                    }
                    IEnumerator<ZipEntry> entries = zip.GetEnumerator();

                    maxFileCount = 0;
                    while (entries.MoveNext())
                    {
                        ZipEntry entry = entries.Current;
                        maxFileCount++;
                    }

                    entries = zip.GetEnumerator();
                    currentFileCount = 0;
                    while (entries.MoveNext())
                    {
                        ZipEntry entry = entries.Current;
                        entry.Extract(dstPath, ExtractExistingFileAction.OverwriteSilently);
                        pd.UpdateProgress((double)(++currentFileCount) / (double)maxFileCount);
                        pd.UpdateStatus(entry.FileName);
                    }
                }
                else
                {
                    createResult = false;
                }
            }
            else
            {
                maxFileCount = fileCount(srcPath);
                currentFileCount = 0;
                Thread.Sleep(100);
                //dstPath += @"\" + projectName;
                if (Directory.Exists(dstPath))
                {
                    createResult = false;
                    return;
                }
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
        }

        private void thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pd.Finish();

            this.DialogResult = createResult;
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
                pd.UpdateStatus(@"assets\" + fileName);
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

        private void radioLuamingProject_Checked(object sender, RoutedEventArgs e)
        {
            project_name_textbox.Text = "";
            project_name_textbox.IsEnabled = true;
            isProjectNameValid = false;
            package_name_textbox.Text = "";
            package_name_textbox.IsEnabled = true;
            isPackageValid = false;
            radioLandscape.IsChecked = true;
            radioPortrait.IsChecked = false;
            radioLandscape.IsEnabled = true;
            radioPortrait.IsEnabled = true;

            setCreateButtonEnabled();
        }

        private void radioApiDemo_Checked(object sender, RoutedEventArgs e)
        {
            project_name_textbox.Text = "APIDemo";
            project_name_textbox.IsEnabled = false;
            isProjectNameValid = true;
            package_name_textbox.Text = "com.luaming.demo";
            package_name_textbox.IsEnabled = false;
            isPackageValid = true;
            radioLandscape.IsChecked = true;
            radioPortrait.IsChecked = false;
            radioLandscape.IsEnabled = false;
            radioPortrait.IsEnabled = false;
            
            setCreateButtonEnabled();
        }

        private void project_name_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            projectName = project_name_textbox.Text;
            isProjectNameValid = Regex.IsMatch(projectName, @"^[a-zA-Z](\w?)+");
            setCreateButtonEnabled();
        }

        private void package_name_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            packageName = package_name_textbox.Text;
            isPackageValid = Regex.IsMatch(packageName, @"^[a-zA-Z]([a-zA-Z0-9_]?)+((\.[a-zA-Z0-9_]+)+)$");
            setCreateButtonEnabled();
        }
    }
}
