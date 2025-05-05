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
using System.Diagnostics;
using System.Threading.Tasks;
namespace Multron_Push_To_Talk
{
 
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon trayIcon;
        private bool isTalking = false;
        private HashSet<KeyboardHook.VKeys> selectedkeys = new HashSet<KeyboardHook.VKeys>();
        private HashSet<KeyboardHook.VKeys> pressedkeys = new HashSet<KeyboardHook.VKeys>();
        
        private HashSet<string> pressedmouses = new HashSet<string>();
        private HashSet<string> clickedmouses = new HashSet<string>();
        private string selectedkeystring = "";
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
            LoadSelectedKeys();
            loadmouses();
        }
        public void loadmouses()
        {
            if (mouseload == 0)
            {

                mouseHook.RightButtonDown += MouseHook_RightButtonDown;
                mouseHook.RightButtonUp += MouseHook_RightButtonUp;
                mouseHook.MiddleButtonDown += MouseHook_MiddleButtonDown;
                mouseHook.MiddleButtonUp += MouseHook_MiddleButtonUp;
                mouseHook.MouseButton3Down += MouseHook_MouseButton3Down;
                mouseHook.MouseButton3Up += MouseHook_MouseButton3Up;
                mouseHook.MouseButton4Down += MouseHook_MouseButton4Down;
                mouseHook.MouseButton4Up += MouseHook_MouseButton4Up;
                mouseHook.Install();
                mouseload = 1;

            }
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
                            PushToTalkText.Text = $"Press and Hold {selectedkeys} to Talk";
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
        public void LoadSelectedKeys()
        {
           
            if (File.Exists(Environment.CurrentDirectory + "\\device.txt"))
            {
                string selectedindex = File.ReadAllText(Environment.CurrentDirectory + "\\device.txt");
                MicrophoneComboBox.SelectedIndex = int.Parse(selectedindex);
            }
            if(File.Exists(Environment.CurrentDirectory + "\\pressedmouses.txt"))
            {
                var lines = File.ReadAllLines(Environment.CurrentDirectory + "\\pressedmouses.txt");
                foreach (var line in lines)
                {

                       
                            pressedmouses.Add(line);
                            selectedmousehook = 5;
                            if (!selectedkeystring.Contains(" + " + line))
                            {
                                selectedkeystring += " + " + line;
                            }
 
                    
                }
            }
           
            if (File.Exists(Environment.CurrentDirectory + "\\selectedkeys.txt"))
            {
                var lines = File.ReadAllLines(Environment.CurrentDirectory + "\\selectedkeys.txt");
                foreach (var line in lines)
                {
                    if (Enum.TryParse(line, out KeyboardHook.VKeys key))
                    {
                        selectedkeys.Add(key);
                    }
                }
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
             
                var combo = string.Join(" + ", selectedkeys);
                if(combo.Length > 3)
                {
                    selectedkeystring += " + " + combo;

                  
                }
                
                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                PushToTalkText.Text = $"Press and Hold {selectedkeystring} to Talk";

                StopListen.Content = "Start Listening";
                listening = 1;
            

            }

          
        }
        public void SaveSelectedKeys()
        {
         
            File.WriteAllText(Environment.CurrentDirectory + "\\device.txt", MicrophoneComboBox.SelectedIndex.ToString());
            File.WriteAllLines(Environment.CurrentDirectory + "\\selectedkeys.txt", selectedkeys.Select(k => k.ToString()));
            File.WriteAllLines(Environment.CurrentDirectory + "\\pressedmouses.txt", pressedmouses.Select(i => i.ToString()));
        }
        private void keyboardHook_KeyDown(KeyboardHook.VKeys key)
        {

            if (listening == 1 && selectedkeys.Count > 0)
            {
                if (selectedkeys.Contains(key))
                {
                    pressedkeys.Add(key);
                }
                if (pressedkeys.SetEquals(selectedkeys))
                {
                    if (!isTalking && defaultmicdevice != null)
                    {
                        
                        isTalking = true;
                        
                        if (selectedmousehook == 0)
                        {
                            isTalking = true;
                            PushToTalkText.Text = "Talking...";
                            defaultmicdevice.AudioEndpointVolume.Mute = false;
                            pressedkeys.Clear();




                        }
                    }
                }
            }
        }
        private void keyboardHook_KeyUp(KeyboardHook.VKeys key)
        {
            if (isTalking)
                {
                    isTalking = false;

                   
                    PushToTalkText.Text = $"Press and Hold {selectedkeystring} to Talk";
                    defaultmicdevice.AudioEndpointVolume.Mute = true;
                   

            }
            
        }



       

        private void keyboardHook_KeyDown2(KeyboardHook.VKeys key)
        {
          
       
            if(listening == 0)
            {
                if (!selectedkeys.Contains(key))
                {
                    selectedkeys.Add(key);
                 
                }
                else
                {
                    var combo = string.Join(" + ", selectedkeys);

                    selectedkeystring = combo;
                    KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                    PushToTalkText.Text = $"Press and Hold {selectedkeystring} to Talk";
                    defaultmicdevice.AudioEndpointVolume.Mute = false;

                }
              
                
            }
        }
        MouseHook mouseHook = new MouseHook();
        int mouseload = 0;
        int listening = 0;
     
         private void MouseHook_MouseButton4Down(MSLLHOOKSTRUCT mouseStruct)
        {
            clickedmouses.Add("MOUSE BUTTON4");
            if (listening == 0)
                {
                    selectedmousehook = 5;
               
                if (!pressedmouses.Contains("MOUSE BUTTON4"))
                    {
                        pressedmouses.Add("MOUSE BUTTON4");
                    }


                    if (!selectedkeystring.Contains(" + MOUSE BUTTON4"))
                    {
                        selectedkeystring += " + MOUSE BUTTON4";
                    }

                    KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                    PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";
                    defaultmicdevice.AudioEndpointVolume.Mute = true;
                }
             
                if (pressedmouses.SetEquals(clickedmouses) && listening == 1 && this.pressedkeys.SetEquals(this.selectedkeys))
                {
                    isTalking = true;
                    PushToTalkText.Text = "Talking...";
                    defaultmicdevice.AudioEndpointVolume.Mute = false;
                    pressedkeys.Clear();

                }
        }
        private void MouseHook_MouseButton4Up(MSLLHOOKSTRUCT mouseStruct)
        {

            if (listening == 0)
            {
                 
                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
              
            }
          
            if (pressedmouses.SetEquals(clickedmouses) && listening == 1)
            {
                isTalking = false;

                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
            }
            clickedmouses.Remove("MOUSE BUTTON4");
        }
        private void MouseHook_MouseButton3Down(MSLLHOOKSTRUCT mouseStruct)
        {
            clickedmouses.Add("MOUSE BUTTON3");
            if (listening == 0)
            {
                selectedmousehook = 4;
              
                if (!pressedmouses.Contains("MOUSE BUTTON3"))
                {
                    pressedmouses.Add("MOUSE BUTTON3");
                }
                if (!selectedkeystring.Contains(" + MOUSE BUTTON3"))
                {

                    selectedkeystring += " + MOUSE BUTTON3";
                }
                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";
              
                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
            }
            if(!clickedmouses.Contains("MOUSE BUTTON3"))
            {
                clickedmouses.Add("MOUSE BUTTON3");
            }
            if (pressedmouses.SetEquals(clickedmouses) && listening == 1 && pressedkeys.SetEquals(selectedkeys))
            {
                isTalking = true;
                PushToTalkText.Text = "Talking...";
                defaultmicdevice.AudioEndpointVolume.Mute = false;
            }
        }
        private void MouseHook_MouseButton3Up(MSLLHOOKSTRUCT mouseStruct)
        {

            if (listening == 0)
            {
                selectedmousehook = 4;
                
                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";    

                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
            }
            if (pressedmouses.SetEquals(clickedmouses) && listening == 1 && pressedkeys.SetEquals(selectedkeys))
            {
                isTalking = false;

                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
            }
            clickedmouses.Remove("MOUSE BUTTON3");
        }
        private void MouseHook_RightButtonDown(MSLLHOOKSTRUCT mouseStruct)
        {
            clickedmouses.Add("MOUSE RIGHT BUTTON");
            if (listening == 0)
            {
                selectedmousehook = 2;
             
                if (!pressedmouses.Contains("MOUSE RIGHT BUTTON"))
                {
                    pressedmouses.Add("MOUSE RIGHT BUTTON");
                }
                if (!selectedkeystring.Contains(" + MOUSE RIGHT BUTTON"))
                {
                    selectedkeystring += " + MOUSE RIGHT BUTTON";
                }
               
                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";
             
                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
            }
          
            if (pressedmouses.SetEquals(clickedmouses) && listening == 1 && pressedkeys.SetEquals(selectedkeys))
            {
                isTalking = true;
                PushToTalkText.Text = "Talking...";
                defaultmicdevice.AudioEndpointVolume.Mute = false;
                pressedkeys.Clear();
            }
        }

        private void MouseHook_RightButtonUp(MSLLHOOKSTRUCT mouseStruct)
        {
       
            if(listening == 0)
            {
             
                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";
                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
            }
            if (pressedmouses.SetEquals(clickedmouses) && listening == 1 && pressedkeys.SetEquals(selectedkeys))
            {
                isTalking = false;

                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + "  to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
               
            }
            clickedmouses.Remove("MOUSE RIGHT BUTTON");
        }

        private void MouseHook_MiddleButtonDown(MSLLHOOKSTRUCT mouseStruct)
        {
            clickedmouses.Add("MOUSE MIDDLE BUTTON");
            if (listening == 0)
            {
                selectedmousehook = 3;
           
                if (!pressedmouses.Contains("MOUSE MIDDLE BUTTON"))
                {
                    pressedmouses.Add("MOUSE MIDDLE BUTTON");
                }
                if (!selectedkeystring.Contains(" + MOUSE MIDDLE BUTTON"))
                {

                    selectedkeystring += " + MOUSE MIDDLE BUTTON";
                }
                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";
                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
                isTalking = false;

            }
      
            if (pressedmouses.SetEquals(clickedmouses) && listening == 1 && pressedkeys.SetEquals(selectedkeys))
            {
                isTalking = true;
                PushToTalkText.Text = "Talking...";
                defaultmicdevice.AudioEndpointVolume.Mute = false;
                pressedkeys.Clear();
            }
        }

        private void MouseHook_MiddleButtonUp(MSLLHOOKSTRUCT mouseStruct)
        {
            if(listening == 0)
            {
                selectedmousehook = 3;
                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";
                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";
                isTalking = false;
                defaultmicdevice.AudioEndpointVolume.Mute = true;
            }
         
            if (pressedmouses.SetEquals(clickedmouses) && listening == 1 && pressedkeys.SetEquals(selectedkeys))
            {
                isTalking = false;

                PushToTalkText.Text = $"Press and Hold " + selectedkeystring +" to Talk";
                defaultmicdevice.AudioEndpointVolume.Mute = true;
             
            }
            clickedmouses.Remove("MOUSE MIDDLE BUTTON");
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
        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (Hider.IsChecked == true)
            {
          
                this.Hide();
                this.WindowState = WindowState.Minimized;
                this.Activate();
            }
            else
            {
                if (trayIcon != null)
                    trayIcon.Visible = false;
                if (defaultmicdevice != null)
                    defaultmicdevice.AudioEndpointVolume.Mute = false;

                Environment.Exit(0);
                
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(!StopListen.Content.Equals("Start Listening"))
            {
               
                
                StopListen.Content = "Start Listening";
                listening = 1;
                SaveSelectedKeys();
            } else
            {
                StopListen.Content = "Stop Listening";
                load = 0;
                listening = 0;
            }
            

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            pressedkeys.Clear();
            selectedkeys.Clear();
            pressedmouses.Clear();
            KeySelectionTextBox.Text = "Press a key or click a mouse button...";

            StopListen.Content = "Stop Listening";
            load = 0;
            listening = 0;
            selectedmousehook = 0;
            selectedkeystring = "";
            PushToTalkText.Text = "";
            if(File.Exists(Environment.CurrentDirectory + "\\selectedkeys.txt"))
            {
                File.Delete(Environment.CurrentDirectory + "\\selectedkeys.txt");
            }
            if(File.Exists(Environment.CurrentDirectory + "\\pressedmouses.txt"))
            {
                File.Delete(Environment.CurrentDirectory + "\\pressedmouses.txt");
            }
        }
    }
}