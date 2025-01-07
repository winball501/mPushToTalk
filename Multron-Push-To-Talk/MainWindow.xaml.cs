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
using static GlobalLowLevelHooks.MouseHook;
using System.Windows.Forms;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.IO;
namespace Multron_Push_To_Talk
{
 
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon trayIcon;
        private bool isTalking = false;
        private KeyboardHook.VKeys selectedKey = KeyboardHook.VKeys.KEY_H;
        int selectedmousehook = 0;
        private System.Windows.Forms.ContextMenuStrip trayContextMenu;
        MMDevice defaultmicdevice;
        KeyboardHook keyboardHook = new KeyboardHook();
      
        string settingspath = Directory.GetCurrentDirectory() + "\\" + "settings.txt";
    
        public MainWindow()
        {
            InitializeComponent();
            keyboardHook.KeyDown += new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyDown);
            keyboardHook.KeyUp += new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyUp);
            keyboardHook.Install();
            refresh();
            loadsettings();
            
        }
        public string stringtokenizer(string input, string token, int index)
        {

            string[] tokens = input.Split(token);

            if (index >= 0 && index < tokens.Length)
            {
                return (tokens[index]);
            }
            else
            {
                return (null);
            }
        }
        public void loadsettings()
        {

            if (System.IO.File.Exists(settingspath))
            {
              
                using (FileStream fs = new FileStream(settingspath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        string line = "";
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("Tray="))
                            {
                                string setting = stringtokenizer(line, "=", 1);
                                if (setting == "1")
                                {
                                    EnableTrayIconCheckBox.IsChecked = true;
                                }
                            }
                            if (line.StartsWith("Hider="))
                            {
                                string setting = stringtokenizer(line, "=", 1);
                                if (setting == "1")
                                {
                                    Hider.IsChecked = true;
                                }
                            }
                        }
                    }
                }
            }
        }
        public void removesettings(string set)
        {
            var lines = File.ReadAllLines(settingspath).ToList();

         
            var updatedLines = lines.Where(line => line != set).ToList();

        
            File.WriteAllLines(settingspath, updatedLines);
        }
        private static Mutex fileMutex = new Mutex();
        public void savesettings(string set)
        {
            
            int retries = 0;
            bool fileOpened = false;

            while (retries < 10 && !fileOpened)
            {
                try
                {
                   using (FileStream fsWrite = new FileStream(settingspath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using (StreamWriter writer = new StreamWriter(fsWrite))
                        {
                         
                            writer.WriteLine(set);
                        }
                    }


                    fileOpened = true;
                }
                catch (IOException ex)
                {
                    retries++;
                    Thread.Sleep(100); 
                }
            }
 

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
                            PushToTalkText.Text = $"Press and Hold {selectedKey} to Talk";
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
            if(listening == 1 && selectedmousehook == 0)
            {
                if (key == selectedKey)
                {
                    if (!isTalking && defaultmicdevice != null)
                    {
                        isTalking = true;
                        PushToTalkText.Text = "Talking...";
                        defaultmicdevice.AudioEndpointVolume.Mute = false;




                    }
                }
            }
           
        }
        private void keyboardHook_KeyUp(KeyboardHook.VKeys key)
        {
            if (key == selectedKey && selectedmousehook == 0)
            {
                if (isTalking)
                {
                    isTalking = false;

                    PushToTalkText.Text = $"Press and Hold {selectedKey} to Talk";
                    defaultmicdevice.AudioEndpointVolume.Mute = true;



                }
            }
        }
      



     
        private void keyboardHook_KeyDown2(KeyboardHook.VKeys key)
        {
              
                KeySelectionTextBox.Text = $"Selected: {key}";
                PushToTalkText.Text = $"Press and Hold {key} to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = false;
                selectedKey = key;
                selectedmousehook = 0;
           
              


        }
        MouseHook mouseHook = new MouseHook();
        int mouseload = 0;
        int listening = 0;
        private void KeySelectionTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
        

           if(mouseload == 0)
            {
 
                mouseHook.RightButtonDown += MouseHook_RightButtonDown;
                mouseHook.RightButtonUp += MouseHook_RightButtonUp;
                mouseHook.MiddleButtonDown += MouseHook_MiddleButtonDown;
                mouseHook.MiddleButtonUp += MouseHook_MiddleButtonUp;


                mouseHook.Install();
                mouseload = 1;

            }
       
        }
     
        private void MouseHook_RightButtonDown(MSLLHOOKSTRUCT mouseStruct)
        {
            if(listening == 0)
            {
                selectedmousehook = 2;

                KeySelectionTextBox.Text = $"Selected: {"MOUSE RIGHT BUTTON"}";
             
                PushToTalkText.Text = $"Press and Hold MOUSE RIGHT BUTTON to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
            }
          
            if (selectedmousehook == 2 && listening == 1)
            {
                isTalking = true;
                PushToTalkText.Text = "Talking...";
                defaultmicdevice.AudioEndpointVolume.Mute = false;
            }
        }

        private void MouseHook_RightButtonUp(MSLLHOOKSTRUCT mouseStruct)
        {
       
            if(listening == 0)
            {
                selectedmousehook = 2;
                KeySelectionTextBox.Text = $"Selected: {"MOUSE RIGHT BUTTON"}";
                PushToTalkText.Text = $"Press and Hold MOUSE RIGHT BUTTON to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
            }
            if (selectedmousehook == 2 && listening == 1)
            {
                isTalking = false;

                PushToTalkText.Text = $"Press and Hold MOUSE RIGHT BUTTON to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
            }
        }

        private void MouseHook_MiddleButtonDown(MSLLHOOKSTRUCT mouseStruct)
        {
            if(listening == 0)
            {
                selectedmousehook = 3;
                KeySelectionTextBox.Text = $"Selected: {"MOUSE MIDDLE BUTTON"}";
                PushToTalkText.Text = $"Press and Hold MOUSE MIDDLE BUTTON to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
                isTalking = false;
            }
      
            if (selectedmousehook == 3 && listening == 1)
            {
                isTalking = true;
                PushToTalkText.Text = "Talking...";
                defaultmicdevice.AudioEndpointVolume.Mute = false;
            }
        }

        private void MouseHook_MiddleButtonUp(MSLLHOOKSTRUCT mouseStruct)
        {
            if(listening == 0)
            {
                selectedmousehook = 3;
                KeySelectionTextBox.Text = $"Selected: {"MOUSE MIDDLE BUTTON"}";
                PushToTalkText.Text = $"Press and Hold MOUSE MIDDLE BUTTON to Talk";
                isTalking = false;
                defaultmicdevice.AudioEndpointVolume.Mute = true;
            }
         
            if (selectedmousehook == 3 && listening == 1)
            {
                isTalking = false;

                PushToTalkText.Text = $"Press and Hold MOUSE MIDDLE BUTTON to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
            }
        }
       

        KeyboardHook keyboardHook_selection = new KeyboardHook();
        int load = 0;
        private void KeySelectionTextBox_KeyDown555(object sender, KeyEventArgs e)
        {
        
            if (load == 0)
            {
               

                keyboardHook_selection.KeyDown += new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyDown2);
                keyboardHook_selection.Install();
                load = 1;
                 
               
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
                MicrophoneComboBox.SelectedIndex = 0;


            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error while enumerating devices: {ex.Message}");
            }
        }
        private void MakeHider_Checked(object sender, RoutedEventArgs e)
        {
              savesettings("Hider=1");
        }
        private void MakeCloser_Unchecked(object sender, RoutedEventArgs e)
        {
              removesettings("Hider=1");
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
            savesettings("Tray=1");
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
            if(Hider.IsChecked == true)
            {
                e.Cancel = true;
                this.Hide();
                this.WindowState = WindowState.Minimized;
                this.Activate();
            } else
            {
                if (trayIcon != null)
                    trayIcon.Visible = false;
                if (defaultmicdevice != null)
                    defaultmicdevice.AudioEndpointVolume.Mute = false;

                Environment.Exit(0);
                base.OnClosing(e);
            }
           
        }
        private void EnableTrayIconCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            trayIcon.Visible = false;
            removesettings("Tray=1");

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(!StopListen.Content.Equals("Start Listening"))
            {
               
                keyboardHook_selection.KeyDown -= new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyDown2);
                keyboardHook_selection.Uninstall();
                StopListen.Content = "Start Listening";
                listening = 1;
            } else
            {
                StopListen.Content = "Stop Listening";
                load = 0;
                listening = 0;
            }
            

        }
    }
}