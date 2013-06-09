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
using System.Windows.Threading;
using System.Threading;

namespace LuamingManager
{
    public interface IProgressContext 
    { 
        void UpdateProgress(double progress); 
        void UpdateStatus(string status); 
        void Finish(); 
        bool Canceled { get; } 
    }

    /// <summary>
    /// Interaction logic for Progress.xaml
    /// </summary>
    public partial class ProgressDialog : Window, IProgressContext 
    { 
        private bool canceled = false; 
        public bool Canceled 
        {
            get { return canceled; } 
        } 

        public ProgressDialog() 
        { 
            InitializeComponent(); 
            CancelButton.Click += new RoutedEventHandler(CancelButton_Click); 
        } 

        void CancelButton_Click(object sender, RoutedEventArgs e) 
        { 
            canceled = true; 
            CancelButton.IsEnabled = false; 
        } 

        public void UpdateProgress(double progress) 
        { 
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate { Progress.SetValue(ProgressBar.ValueProperty, progress); }, null); 
        } 

        public void UpdateStatus(string status) 
        { 
            Dispatcher.BeginInvoke(DispatcherPriority.Background, 
            (SendOrPostCallback)delegate { StatusText.SetValue(TextBlock.TextProperty, status); }, null); 
        } 

        public void Finish() 
        { 
            Dispatcher.BeginInvoke(DispatcherPriority.Background, 
                (SendOrPostCallback)delegate { Close(); }, null); 
        } 
    }
}
