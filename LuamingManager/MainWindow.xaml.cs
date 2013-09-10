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
using System.Xml;

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
        private string androidSDKPathFile = System.Windows.Forms.Application.UserAppDataPath + @"\android_sdk_path.txt";
        private string luamingReferenceURL = "http://210.118.74.97/LuamingReference/";
        private string projectName = "";
        private string versionName = "";
        private string packageName = "";
        private string androidSDKPath = "";

        private BackgroundWorker thread;
        private ProgressDialog pd;

        private int currentEntry = 0;
        private int totalEntry = 0;
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
            if (!checkAll(projectPath))
            {
                System.Windows.MessageBox.Show("프로젝트 내의 폴더 또는 파일 이름에 한글이나 공백이 포함되어 있습니다!");
                return;
            }

            if (checkProjectValid())
            {
                System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
                sfd.Title = "Export Luaming Project";
                sfd.DefaultExt = "apk";
                sfd.Filter = "Luaming Application Package (*.apk)|*.apk";
                sfd.CheckPathExists = true;
                sfd.FileName = projectName + "_v" + versionName + ".apk";
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    exportPath = sfd.FileName;
                    //projectName = project_name_label.Content.ToString();

                    thread = new BackgroundWorker();
                    thread.DoWork += new DoWorkEventHandler(thread_DoWork_Export);
                    thread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(thread_RunWorkerCompleted_Export);
                    thread.RunWorkerAsync();

                    pd = new ProgressDialog();
                    pd.Show();
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

                projectName = System.IO.Path.GetFileName(projectPath);
                project_name_label.Content = projectName;
                project_path_textbox.Text = projectPath;
                android_simulate_button.IsEnabled = true;
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
            //System.Windows.MessageBox.Show(projectName + " is successfully exported!!");
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
                    projectName = System.IO.Path.GetFileName(projectPath);
                    project_name_label.Content = projectName;
                    android_simulate_button.IsEnabled = true;
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

        private void zip_AddProgress(object sender, AddProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Adding_AfterAddEntry)
            {
                pd.UpdateProgress((double)(e.EntriesTotal) / (double)(totalEntry));
                pd.UpdateStatus("Adding " + e.CurrentEntry.FileName);
            }
        }

        private void zip_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Saving_Started)
            {
                currentEntry = 0;
            }
            else if (e.EventType == ZipProgressEventType.Saving_AfterWriteEntry)
            {
                pd.UpdateProgress((double)(++currentEntry) / (double)(totalEntry));
                pd.UpdateStatus("Saving " + e.CurrentEntry.FileName);
            }
            else if (e.EventType == ZipProgressEventType.Saving_Completed)
            {
                pd.Finish();
                System.Windows.MessageBox.Show(projectName + " is successfully exported!!");
            }
        }

        private bool checkAll(string srcPath)
        {
            string[] dirs = Directory.GetDirectories(srcPath);
            foreach (string dir in dirs)
            {
                string dirName = System.IO.Path.GetFileName(dir);

                if (hasBlank(dirName))
                    return false;
                if (isHangle(dirName))
                    return false;

                bool subResult = checkAll(dir);
                if (subResult == false)
                    return false;
            }

            string[] files = Directory.GetFiles(srcPath);
            foreach (string file in files)
            {
                string fileName = System.IO.Path.GetFileName(file);
                if (hasBlank(fileName))
                    return false;
                if (isHangle(fileName))
                    return false;
            }

            return true;
        }

        public bool hasBlank(string name)
        {
            if (name.Contains(" "))
                return true;
            if (name.Contains("\n"))
                return true;
            if (name.Contains("\r"))
                return true;
            if (name.Contains("\t"))
                return true;
            if (name.Contains("\v"))
                return true;

            return false;
        }

        public bool isHangle(char c)
        {
            bool ret = false;

            if (c >= '\xAC00' && c <= '\xD7AF')
            {
                ret = true;
            }
            else if (c >= '\x3130' && c <= '\x318F')
            {
                ret = true;
            }

            return ret;
        }

        public bool isHangle(string c)
        {
            bool ret = false;
            for (int i = 0; i < c.Length; i++)
            {
                char cur = c[i];
                if (isHangle(cur))
                {
                    ret = true;
                    break;
                }
            }

            return ret;

        }

        private void exportProject()
        {
            ZipFile zip = new ZipFile();
            zip.AddProgress += new EventHandler<AddProgressEventArgs>(zip_AddProgress);
            zip.SaveProgress += new EventHandler<SaveProgressEventArgs>(zip_SaveProgress);

            totalEntry = 0;
            setTotalEntry(projectPath);

            string[] dirs = Directory.GetDirectories(projectPath);
            //string[] files = Directory.GetFiles(projectPath);
            
            foreach (string dir in dirs)
            {
                string dirName = System.IO.Path.GetFileName(dir);
                zip.AddDirectory(dir, dirName);   
            }
            /*
            foreach (string file in files)
            {
                zip.AddFile(file, @"");
            }
            */
            zip.Save(exportPath);
        }

        private void setTotalEntry(string path)
        {
            string[] dirs = Directory.GetDirectories(path);
            string[] files = Directory.GetFiles(path);

            totalEntry += dirs.Length + files.Length;

            for (int i = 0; i < dirs.Length; i++)
            {
                setTotalEntry(dirs[i]);
            }
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
                string pName = configObject["PROJECT_NAME"].GetValue().ToString();
                if (!Regex.IsMatch(pName, @"^[a-zA-Z](\w?)+"))
                {
                    System.Windows.MessageBox.Show("LuamingProject.json: 프로젝트 이름이 올바르지 않습니다!");
                    return false;
                }
                projectName = pName;
                string packName = configObject["PACKAGE_NAME"].GetValue().ToString();
                if (!Regex.IsMatch(packName, @"^[a-zA-Z]([a-zA-Z0-9_]?)+((\.[a-zA-Z0-9_]+)+)$"))
                {
                    System.Windows.MessageBox.Show("LuamingProject.json: 패키지 이름이 올바르지 않습니다!");
                    return false;
                }
                packageName = packName;
                string vName = configObject["VERSION_NAME"].GetValue().ToString();
                if (!Regex.IsMatch(vName, @"^[1-9]([0-9]?)+((\.[0-9]+)+)$"))
                {
                    System.Windows.MessageBox.Show("LuamingProject.json: 버전 이름이 올바르지 않습니다!");
                    return false;
                }
                versionName = vName;
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
            ProjectConfigurationWindow pcw = new ProjectConfigurationWindow();
            if (pcw.ShowDialog() == true)
            {
                applyConfig();
            }
            /*
            string configFilePath = projectPath + @"\assets\LuamingProject.json";
            if (!File.Exists(configFilePath))
            {
                System.Windows.MessageBox.Show(configFilePath + " 파일이 없습니다!");
                return;
            }
            System.Diagnostics.Process.Start("notepad.exe", configFilePath);
             */
        }

        private void applyConfig()
        {
            string projectConfigPath = MainWindow.projectPath + @"\assets\LuamingProject.json";
            if (!File.Exists(projectConfigPath))
            {
                System.Windows.MessageBox.Show("LuamingProject.json 파일이 없습니다!");
            }
            try
            {
                StreamReader sr = File.OpenText(projectConfigPath);
                string config = sr.ReadToEnd();
                sr.Close();

                JsonTextParser jsonParser = new JsonTextParser();
                JsonObjectCollection configObject = (JsonObjectCollection)jsonParser.Parse(config);
                string prevProjectName = projectName;
                projectName = configObject["PROJECT_NAME"].GetValue().ToString();
                project_name_label.Content = projectName;
                versionName = configObject["VERSION_NAME"].GetValue().ToString();
                packageName = configObject["PACKAGE_NAME"].GetValue().ToString();

                if (prevProjectName != projectName)
                {
                    string newPath = projectPath.Replace(prevProjectName, projectName);
                    Directory.Move(projectPath, newPath);
                    projectPath = newPath;
                    project_path_textbox.Text = projectPath;
                    StreamWriter sw = File.CreateText(lastBrowsingDataFile);
                    sw.WriteLine(projectPath);
                    sw.Close();
                }
            }
            catch (IOException)
            {
                System.Windows.MessageBox.Show("프로젝트 폴더가 열려있어 이름을 바꿀 수 없습니다.");
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
            }
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

        private bool isAndroidSDKFolder(string path)
        {
            if (Directory.Exists(path + @"\platform-tools") &&
                        File.Exists(path + @"\platform-tools\adb.exe") &&
                        Directory.Exists(path + @"\tools") &&
                        File.Exists(path + @"\tools\ddms.bat"))
            {
                string asPath = System.Windows.Forms.Application.StartupPath + @"\AndroidSimulator";

                if (!File.Exists(path + @"\platform-tools\aapt.exe"))
                    File.Copy(asPath + @"\aapt.exe", path + @"\platform-tools\aapt.exe");
                if (!File.Exists(path + @"\tools\apkbuilder.bat"))
                    File.Copy(asPath + @"\apkbuilder.bat", path + @"\tools\apkbuilder.bat");

                return true;
            }

            return false;
        }

        public static bool IsWindowOpen<T>(string name = "") where T : Window
        {
            return string.IsNullOrEmpty(name)
               ? System.Windows.Application.Current.Windows.OfType<T>().Any()
               : System.Windows.Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
        }

        private string makeTempAPK(string sdkPath)
        {
            string androidSimulatorPath = System.Windows.Forms.Application.StartupPath +@"\AndroidSimulator";
            if (!Directory.Exists(androidSimulatorPath))
            {
                System.Windows.Forms.MessageBox.Show("필요 모듈이 삭제, 또는 이동되었습니다\nLuaming Manager를 재설치해주세요");
                return null;
            }

            // Check Devices
            pd.UpdateProgress(0.1);
            pd.UpdateStatus("Check Android Device...");
            System.Diagnostics.Process adbDevice = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo adbDeviceStartInfo = new System.Diagnostics.ProcessStartInfo(sdkPath + @"\platform-tools\adb", "devices");
            adbDeviceStartInfo.CreateNoWindow = true;
            adbDeviceStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            adbDeviceStartInfo.UseShellExecute = false;
            adbDeviceStartInfo.RedirectStandardOutput = true;
            adbDevice.StartInfo = adbDeviceStartInfo;
            adbDevice.Start();
            adbDevice.WaitForExit();
            adbDevice.StandardOutput.ReadLine(); // Skip
            int device_count = 0;
            while (!adbDevice.StandardOutput.EndOfStream)
            {
                string line = adbDevice.StandardOutput.ReadLine();
                if (line.Contains("device") && !line.Contains("emulator"))
                    device_count++;
            }
            if (device_count == 0)
            {
                System.Windows.Forms.MessageBox.Show("연결된 Android Device가 없습니다\n연결 후 다시 시도하세요");
                pd.Close();
                return null;
            }
            else if (device_count > 1)
            {
                System.Windows.Forms.MessageBox.Show("연결된 Android Device가 2대 이상입니다\n1대만 연결 후 다시 시도하세요");
                pd.Close();
                return null;
            }

            // 해당 프로젝트 폴더 확인 및 생성
            pd.UpdateProgress(0.2);
            pd.UpdateStatus("Check Project Folder...");
            string asProjectPath = System.Windows.Forms.Application.UserAppDataPath + @"\" + projectName;
            if (!Directory.Exists(asProjectPath))
                Directory.CreateDirectory(asProjectPath);

            // strings.xml 수정
            pd.UpdateProgress(0.3);
            pd.UpdateStatus("Modify Application Name...");
            XmlDocument stringXML = new XmlDocument();
            stringXML.Load(androidSimulatorPath + @"\LuamingAndroidSimulator\res\values\strings.xml");
            XmlElement strings_child = (XmlElement)stringXML.DocumentElement.FirstChild;
            if (strings_child.GetAttribute("name").Equals("app_name"))
                strings_child.InnerText = projectName;
            stringXML.Save(androidSimulatorPath + @"\LuamingAndroidSimulator\res\values\strings.xml");

            // AndroidManifest.xml 수정
            pd.UpdateProgress(0.4);
            pd.UpdateStatus("Modify Package Name...");
            XmlDocument manifestXML = new XmlDocument();
            manifestXML.Load(androidSimulatorPath + @"\AndroidManifest.xml");
            XmlElement manifest_child = (XmlElement)manifestXML.DocumentElement;
            if (manifest_child.HasAttribute("package"))
                manifest_child.SetAttribute("package", packageName);
            manifestXML.Save(androidSimulatorPath + @"\AndroidManifest.xml");

            // 1차 압축
            pd.UpdateProgress(0.5);
            pd.UpdateStatus("Compressing Files...");
            ZipFile zipfile = new ZipFile();
            zipfile.AddDirectory(androidSimulatorPath + @"\LuamingAndroidSimulator");
            zipfile.AddDirectory(projectPath + @"\assets", "/assets");
            zipfile.Save(asProjectPath + @"\" + projectName + "_v" + versionName + ".apk");

            // AndroidManifest.xml 합치기
            pd.UpdateProgress(0.6);
            pd.UpdateStatus("Add AndroidManifest...");
            System.Diagnostics.Process aapt = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo aaptStartInfo = new System.Diagnostics.ProcessStartInfo("\"" + sdkPath + @"\platform-tools\aapt" + "\"", "package -u -M " + "\"" + androidSimulatorPath + @"\AndroidManifest.xml" + "\"" + " -S " + "\"" + androidSimulatorPath + @"\LuamingAndroidSimulator\res" + "\"" + " -A " + "\"" + projectPath + @"\assets" + "\"" + " -I " + "\"" + androidSimulatorPath + @"\android.jar" + "\"" + " -F " + "\"" + asProjectPath + @"\" + projectName + "_v" + versionName + ".apk" + "\"");
            aaptStartInfo.CreateNoWindow = true;
            aaptStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            aapt.StartInfo = aaptStartInfo;
            aapt.Start();
            aapt.WaitForExit();
            
            // Signing 하기
            pd.UpdateProgress(0.7);
            pd.UpdateStatus("Signing...");
            System.Diagnostics.Process apkbuilder = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo apkbuilderStartInfo = new System.Diagnostics.ProcessStartInfo("\"" + sdkPath + @"\tools\apkbuilder" + "\"", "\"" + asProjectPath + @"\" + projectName + "_v" + versionName + "-Debug.apk" + "\"" + " -z " + "\"" + asProjectPath + @"\" + projectName + "_v" + versionName + ".apk" + "\"");
            apkbuilderStartInfo.CreateNoWindow = true;
            apkbuilderStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            apkbuilder.StartInfo = apkbuilderStartInfo;
            apkbuilder.Start();
            apkbuilder.WaitForExit();

            return asProjectPath;
        }

        private void startAndroidSimulation(string sdkPath)
        {
            string asProjectPath = makeTempAPK(sdkPath);
            if (asProjectPath != null)
            {
                // Open DDMS
                pd.UpdateProgress(0.8);
                pd.UpdateStatus("Open DDMS...");
                bool isDDMSRunning = false;
                System.Diagnostics.Process[] procList = System.Diagnostics.Process.GetProcesses();
                foreach (System.Diagnostics.Process process in procList)
                {
                    if (process.MainWindowTitle.Contains("Dalvik"))
                    {
                        isDDMSRunning = true;
                        break;
                    }
                }
                if (!isDDMSRunning)
                {
                    System.Diagnostics.Process ddms = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo ddmsStartInfo = new System.Diagnostics.ProcessStartInfo("\"" + sdkPath + @"\tools\ddms" + "\"");
                    ddmsStartInfo.CreateNoWindow = true;
                    ddmsStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    ddms.StartInfo = ddmsStartInfo;
                    ddms.Start();
                }

                // Install
                pd.UpdateProgress(0.9);
                pd.UpdateStatus("Install Application to Device...");
                System.Diagnostics.Process adbInstall = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo adbInstallStartInfo = new System.Diagnostics.ProcessStartInfo("\"" + sdkPath + @"\platform-tools\adb" + "\"", "-d install -r " + "\"" + asProjectPath + @"\" + projectName + "_v" + versionName + "-Debug.apk" + "\"");
                adbInstallStartInfo.CreateNoWindow = true;
                adbInstallStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                adbInstall.StartInfo = adbInstallStartInfo;
                adbInstall.Start();
                adbInstall.WaitForExit();

                // Excute
                pd.UpdateProgress(1.0);
                pd.UpdateStatus("Starting Application...");
                System.Diagnostics.Process adbExcute = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo adbExcuteStartInfo = new System.Diagnostics.ProcessStartInfo("\"" + sdkPath + @"\platform-tools\adb" + "\"", "shell am start -n" + packageName + "/com.luaming.lib.LuamingAndroidSimulator");
                adbExcuteStartInfo.CreateNoWindow = true;
                adbExcuteStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                adbExcute.StartInfo = adbExcuteStartInfo;
                adbExcute.Start();
                adbExcute.WaitForExit();

                pd.UpdateStatus("Done!");
            }
        }

        private void android_simulate_button_handler()
        {
            if (!File.Exists(androidSDKPathFile))
            {
                System.Windows.Forms.MessageBox.Show("Android Simulate를 위해서는 Android SDK가 필요합니다\nAndroid SDK 폴더를 선택해주세요");
SDKSELECT:
                FolderBrowserDialog fd = new FolderBrowserDialog();
                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (isAndroidSDKFolder(fd.SelectedPath))
                    {
                        StreamWriter sw = File.CreateText(androidSDKPathFile);
                        sw.WriteLine(fd.SelectedPath);
                        sw.Close();

                        androidSDKPath = fd.SelectedPath;
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Android SDK 폴더가 아닙니다\n다시 선택해주세요");
                        goto SDKSELECT;
                    }
                }
            }
            else
            {
                StreamReader sr = File.OpenText(androidSDKPathFile);
                string tempPath = sr.ReadToEnd();
                sr.Close();
                tempPath = tempPath.Trim();

                if (isAndroidSDKFolder(tempPath))
                    androidSDKPath = tempPath;
                else
                    android_simulate_button_handler();
            }

            if (androidSDKPath.Length > 0)
            {
                thread = new BackgroundWorker();
                thread.DoWork += new DoWorkEventHandler(thread_DoWork_AndroidSimulate);
                thread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(thread_RunWorkerCompleted_AndroidSimulate);
                thread.RunWorkerAsync();

                pd = new ProgressDialog();
                pd.Show();
            }
        }

        private void android_simulate_button_Click(object sender, RoutedEventArgs e)
        {
            checkProjectValid();
            android_simulate_button_handler();
        }

        private void thread_DoWork_AndroidSimulate(object sender, DoWorkEventArgs e)
        {
            startAndroidSimulation(androidSDKPath);
        }

        private void thread_RunWorkerCompleted_AndroidSimulate(object sender, RunWorkerCompletedEventArgs e)
        {
            pd.Close();
        }
    }
}
