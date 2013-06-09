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

namespace LuamingManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string projectPath = null;
        private string exportPath;

        private BackgroundWorker thread;
        private ProgressDialog pd;

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

        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
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
            System.Diagnostics.Process.Start(System.Windows.Forms.Application.StartupPath + @"\Simulator\LuamingSimulator.exe", projectPath);
        }

        private void export_button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
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

        private void createProject()
        {
            ProjectCreateWindow pcWindow = new ProjectCreateWindow();
            if (pcWindow.ShowDialog() == true)
            {
                project_name_label.Content = System.IO.Path.GetFileName(projectPath);
                project_path_textbox.Text = projectPath;
                simulate_button.IsEnabled = true;
                export_button.IsEnabled = true;
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
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = dialog.SelectedPath;

                if (isValidProjectPath(path))
                {
                    projectPath = path;
                    project_path_textbox.Text = projectPath;
                    project_name_label.Content = System.IO.Path.GetFileName(projectPath);
                    simulate_button.IsEnabled = true;
                    export_button.IsEnabled = true;
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
    }
}
