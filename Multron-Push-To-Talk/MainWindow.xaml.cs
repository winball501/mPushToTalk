using System.Data;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Windows.Input;
using System.Windows.Media;
using NAudio.CoreAudioApi;
using System.Runtime.InteropServices;
using GlobalLowLevelHooks;
namespace Multron_Push_To_Talk
{
 
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon trayIcon;
        private bool isTalking = false;
        private KeyboardHook.VKeys selectedKey = KeyboardHook.VKeys.KEY_H;
        private System.Windows.Forms.ContextMenuStrip trayContextMenu;
        MMDevice defaultmicdevice;
        KeyboardHook keyboardHook = new KeyboardHook();
         public MainWindow()
        {
            InitializeComponent();
            keyboardHook.KeyDown += new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyDown);
            keyboardHook.KeyUp += new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyUp);
            keyboardHook.Install();
            refresh();
        }


        public void refresh()
        {
            Thread _refreshThread = new Thread(() =>
            {
                while (true)
                {
                
                    Thread.Sleep(100000);

                     this.Dispatcher.Invoke(new Action(() =>
                        {
                        
                            keyboardHook.KeyDown -= new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyDown);
                            keyboardHook.KeyUp -= new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyUp);
                            keyboardHook.Uninstall();

                            isTalking = false;
                            PushToTalkText.Text = $"Press and Hold {selectedItem.Content} to Talk";
                            defaultmicdevice.AudioEndpointVolume.Mute = true;

                           
                            keyboardHook.KeyDown += new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyDown);
                            keyboardHook.KeyUp += new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyUp);
                            keyboardHook.Install();
                     }));
                
                  
                    Thread.Sleep(100);
                }
            });

            _refreshThread.Start();
        }

        private void keyboardHook_KeyDown(KeyboardHook.VKeys key)
        {
            if (key == selectedKey)
            {
                if (!isTalking)
                {
                    isTalking = true;
                    PushToTalkText.Text = "Talking...";
                    defaultmicdevice.AudioEndpointVolume.Mute = false;




                }
            }
        }
        private void keyboardHook_KeyUp(KeyboardHook.VKeys key)
        {
            if (key == selectedKey)
            {
                if (isTalking)
                {
                    isTalking = false;

                    PushToTalkText.Text = $"Press and Hold {selectedItem.Content} to Talk";
                    defaultmicdevice.AudioEndpointVolume.Mute = true;



                }
            }
        }
        ComboBoxItem selectedItem = null;
    
              
   
 
        private void KeyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            selectedItem = KeyComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
            if (selectedItem != null)
            {
                switch (selectedItem.Content.ToString())
                {
                    case "Spacebar":
                        selectedKey = KeyboardHook.VKeys.SPACE;
                        break;
                    case "Ctrl":
                        selectedKey = KeyboardHook.VKeys.CONTROL;
                        break;
                    case "H":
                        selectedKey = KeyboardHook.VKeys.KEY_H;
                        break;
                    case "F":
                        selectedKey = KeyboardHook.VKeys.KEY_F;
                        break;
                    case "V":
                        selectedKey = KeyboardHook.VKeys.KEY_V;
                        break;
                    case "E":
                        selectedKey = KeyboardHook.VKeys.KEY_E;
                        break;
                    case "Q":
                        selectedKey = KeyboardHook.VKeys.KEY_Q;
                        break;
                }
                PushToTalkText.Text = $"Press and Hold {selectedItem.Content} to Talk";
            }





        }
        private void PopulateMicrophoneComboBox()
        {
            try
            { 
                var enumerator = new MMDeviceEnumerator();

             
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

               
                MicrophoneComboBox.Items.Clear();

        
                if (devices == null || devices.Count == 0)
                {
                     return;
                }

                
                foreach (var device in devices)
                {
                    MicrophoneComboBox.Items.Add(device.FriendlyName);
                   

                }
                


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while enumerating devices: {ex.Message}");
            }
        }

        private void EnableTrayIconCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            trayIcon = new System.Windows.Forms.NotifyIcon();
            trayIcon.Icon = System.Drawing.SystemIcons.Information; 
            trayIcon.Visible = true;
            trayIcon.Text = "Push to Talk is running";
            var showMenuItem = new System.Windows.Forms.ToolStripMenuItem("Show", null, ShowWindow);
            var hideMenuItem = new System.Windows.Forms.ToolStripMenuItem("Hide", null, HideWindow);
            var exitMenuItem = new System.Windows.Forms.ToolStripMenuItem("Exit", null, ExitApplication);

            trayContextMenu = new System.Windows.Forms.ContextMenuStrip();
            trayContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { showMenuItem, hideMenuItem, exitMenuItem });

      
            trayIcon.ContextMenuStrip = trayContextMenu;
 
            trayIcon.DoubleClick += TrayIcon_DoubleClick;
        }
        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowWindow(sender, e);
        }
        private void ShowWindow(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal; 
            this.Activate();  
        }
        private void HideWindow(object sender, EventArgs e)
        {
            this.Hide();
            this.WindowState = WindowState.Minimized;
            this.Activate();
        }
        private void ExitApplication(object sender, EventArgs e)
        {
            
            if (trayIcon != null)
                trayIcon.Visible = false;
            if(defaultmicdevice != null)
               defaultmicdevice.AudioEndpointVolume.Mute = false;
          
            
            Environment.Exit(0);
        }


        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if(trayIcon != null)
              trayIcon.Visible = false;
            if(defaultmicdevice != null)
              defaultmicdevice.AudioEndpointVolume.Mute = false;

            Environment.Exit(0);
            base.OnClosing(e);  
        }
        private void EnableTrayIconCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            trayIcon.Visible = false;

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        
            PopulateMicrophoneComboBox();
        }
        private void MicrophoneComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedMicName = MicrophoneComboBox.SelectedItem as string;

            if (!string.IsNullOrEmpty(selectedMicName))
            {

                var enumerator = new MMDeviceEnumerator();
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

                foreach (var device in devices)
                {
                    if (device.FriendlyName == selectedMicName)
                    {
                        defaultmicdevice = device;
                        Console.WriteLine($"Selected Microphone: {device.FriendlyName}");
                        break;
                    }
                }
            }
            defaultmicdevice.AudioEndpointVolume.Mute = true;
        }
    }
}