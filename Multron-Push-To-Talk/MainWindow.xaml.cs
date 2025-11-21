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
        private int listening = 0;
        private int mouseload = 0;
        MouseHook mouseHook = new MouseHook();
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
    
        private readonly object keysLock = new object();
        private readonly object mouseLock = new object();
        private readonly object talkingLock = new object();

        public void refresh()
        {
            Thread _refreshThread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(100000);

                    try
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            keyboardHook.KeyDown -= new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyDown);
                            keyboardHook.KeyUp -= new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyUp);
                            keyboardHook.Uninstall();

                            lock (talkingLock)
                            {
                                isTalking = false;
                            }

                            if (PushToTalkText != null)
                                PushToTalkText.Text = $"Press and Hold {selectedkeystring} to Talk";

                            if (defaultmicdevice != null)
                                SafeSetMute(true);

                            keyboardHook.KeyDown += new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyDown);
                            keyboardHook.KeyUp += new KeyboardHook.KeyboardHookCallback(keyboardHook_KeyUp);
                            keyboardHook.Install();
                        }));
                    }
                    catch (Exception ex)
                    {
                        PushToTalkText.Text = $"Refresh error: {ex.Message}";
                   
                    }

                    Thread.Sleep(100);
                }
            });

            _refreshThread.IsBackground = true;
            _refreshThread.Start();
        }

        public void LoadSelectedKeys()
        {
            if (File.Exists(Environment.CurrentDirectory + "\\device.txt"))
            {
                string selectedindex = File.ReadAllText(Environment.CurrentDirectory + "\\device.txt");
                if (int.TryParse(selectedindex, out int index) && MicrophoneComboBox != null)
                {
                    MicrophoneComboBox.SelectedIndex = index;
                }
            }

            if (File.Exists(Environment.CurrentDirectory + "\\pressedmouses.txt"))
            {
                var lines = File.ReadAllLines(Environment.CurrentDirectory + "\\pressedmouses.txt");
                lock (mouseLock)
                {
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
            }

            if (File.Exists(Environment.CurrentDirectory + "\\selectedkeys.txt"))
            {
                var lines = File.ReadAllLines(Environment.CurrentDirectory + "\\selectedkeys.txt");
                lock (keysLock)
                {
                    foreach (var line in lines)
                    {
                        if (Enum.TryParse(line, out KeyboardHook.VKeys key))
                        {
                            selectedkeys.Add(key);
                        }
                    }
                }

                var selectedMicName = MicrophoneComboBox?.SelectedItem as string;

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

                string combo;
                lock (keysLock)
                {
                    combo = string.Join(" + ", selectedkeys);
                }

                if (combo.Length > 3)
                {
                    selectedkeystring += " + " + combo;
                }

                if (KeySelectionTextBox != null)
                    KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                if (PushToTalkText != null)
                    PushToTalkText.Text = $"Press and Hold {selectedkeystring} to Talk";

                if (StopListen != null)
                    StopListen.Content = "Start Listening";

                listening = 1;
            }
        }

        public void SaveSelectedKeys()
        {
            if (MicrophoneComboBox != null)
            {
                File.WriteAllText(Environment.CurrentDirectory + "\\device.txt", MicrophoneComboBox.SelectedIndex.ToString());
            }

            lock (keysLock)
            {
                File.WriteAllLines(Environment.CurrentDirectory + "\\selectedkeys.txt", selectedkeys.Select(k => k.ToString()));
            }

            lock (mouseLock)
            {
                File.WriteAllLines(Environment.CurrentDirectory + "\\pressedmouses.txt", pressedmouses.Select(i => i.ToString()));
            }
        }

        private void keyboardHook_KeyDown(KeyboardHook.VKeys key)
        {
            if (listening != 1)
                return;

            lock (keysLock)
            {
                if (selectedkeys.Count > 0 && selectedkeys.Contains(key))
                {
                    pressedkeys.Add(key);
                }

                bool keysMatch = pressedkeys.SetEquals(selectedkeys);

                if (keysMatch)
                {
                    bool shouldTalk = false;

                    lock (talkingLock)
                    {
                        if (!isTalking && defaultmicdevice != null && selectedmousehook == 0)
                        {
                            isTalking = true;
                            shouldTalk = true;
                        }
                    }

                    if (shouldTalk)
                    {
                        try
                        {
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (PushToTalkText != null)
                                    PushToTalkText.Text = "Talking...";
                            }));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error in keyDown UI update: {ex.Message}");
                        }

                        if (defaultmicdevice != null)
                            SafeSetMute(false);

                        pressedkeys.Clear();
                    }
                }
            }
        }

        private void keyboardHook_KeyUp(KeyboardHook.VKeys key)
        {
            Thread t = new Thread(() =>
            {
                bool shouldProcess = false;

                lock (talkingLock)
                {
                    shouldProcess = isTalking;
                }

                if (shouldProcess)
                {
                    int sleeptime = 0;

                    try
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            if (MsSettingTextBox != null && !string.IsNullOrEmpty(MsSettingTextBox.Text))
                            {
                                int.TryParse(MsSettingTextBox.Text, out sleeptime);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error reading sleep time: {ex.Message}");
                    }

                    Thread.Sleep(sleeptime);

                    try
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            lock (talkingLock)
                            {
                                isTalking = false;
                            }

                            if (PushToTalkText != null)
                                PushToTalkText.Text = $"Press and Hold {selectedkeystring} to Talk";

                            if (defaultmicdevice != null)
                                SafeSetMute(true);
                        }));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in keyUp: {ex.Message}");
                    }
                }
            });

            t.IsBackground = true;
            t.Start();
        }

        private void keyboardHook_KeyDown2(KeyboardHook.VKeys key)
        {
            if (listening != 0)
                return;

            lock (keysLock)
            {
                if (!selectedkeys.Contains(key))
                {
                    selectedkeys.Add(key);
                }
                else
                {
                    var combo = string.Join(" + ", selectedkeys);
                    selectedkeystring = combo;

                    try
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (KeySelectionTextBox != null)
                                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                            if (PushToTalkText != null)
                                PushToTalkText.Text = $"Press and Hold {selectedkeystring} to Talk";

                            if (defaultmicdevice != null)
                                SafeSetMute(false);
                        }));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in keyDown2: {ex.Message}");
                    }
                }
            }
        }

        private void MouseHook_MouseButton4Down(MSLLHOOKSTRUCT mouseStruct)
        {
            lock (mouseLock)
            {
                clickedmouses.Add("MOUSE BUTTON4");
            }

            if (listening == 0)
            {
                selectedmousehook = 5;

                lock (mouseLock)
                {
                    if (!pressedmouses.Contains("MOUSE BUTTON4"))
                    {
                        pressedmouses.Add("MOUSE BUTTON4");
                    }
                }

                if (!selectedkeystring.Contains(" + MOUSE BUTTON4"))
                {
                    selectedkeystring += " + MOUSE BUTTON4";
                }

                try
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (KeySelectionTextBox != null)
                            KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                        if (PushToTalkText != null)
                            PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";

                        if (defaultmicdevice != null)
                            SafeSetMute(true);
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            }

            bool shouldTalk = false;
            lock (mouseLock)
            {
                lock (keysLock)
                {
                    shouldTalk = pressedmouses.SetEquals(clickedmouses) && listening == 1 && pressedkeys.SetEquals(selectedkeys);
                }
            }

            if (shouldTalk)
            {
                lock (talkingLock)
                {
                    isTalking = true;
                }

                try
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (PushToTalkText != null)
                            PushToTalkText.Text = "Talking...";

                        if (defaultmicdevice != null)
                            SafeSetMute(false);
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }

                lock (keysLock)
                {
                    pressedkeys.Clear();
                }
            }
        }

        private void MouseHook_MouseButton4Up(MSLLHOOKSTRUCT mouseStruct)
        {
            Thread t = new Thread(() =>
            {
                int sleeptime = 0;

                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (MsSettingTextBox != null && !string.IsNullOrEmpty(MsSettingTextBox.Text))
                        {
                            int.TryParse(MsSettingTextBox.Text, out sleeptime);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }

                Thread.Sleep(sleeptime);

                try
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        if (listening == 0)
                        {
                            if (KeySelectionTextBox != null)
                                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                            if (PushToTalkText != null)
                                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";

                            if (defaultmicdevice != null)
                                SafeSetMute(true);
                        }

                        bool shouldUnmute = false;
                        lock (mouseLock)
                        {
                            shouldUnmute = pressedmouses.SetEquals(clickedmouses) && listening == 1;
                        }

                        if (shouldUnmute)
                        {
                            lock (talkingLock)
                            {
                                isTalking = false;
                            }

                            if (PushToTalkText != null)
                                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";

                            if (defaultmicdevice != null)
                                SafeSetMute(true);
                        }

                        lock (mouseLock)
                        {
                            clickedmouses.Remove("MOUSE BUTTON4");
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            });

            t.IsBackground = true;
            t.Start();
        }

        private void MouseHook_MouseButton3Down(MSLLHOOKSTRUCT mouseStruct)
        {


            lock (mouseLock)
            {
                if (!clickedmouses.Contains("MOUSE BUTTON3"))
                {
                    clickedmouses.Add("MOUSE BUTTON3");
                }
            }

            if (listening == 0)
            {
                selectedmousehook = 4;

                lock (mouseLock)
                {
                    if (!pressedmouses.Contains("MOUSE BUTTON3"))
                    {
                        pressedmouses.Add("MOUSE BUTTON3");
                    }
                }

                if (!selectedkeystring.Contains(" + MOUSE BUTTON3"))
                {
                    selectedkeystring += " + MOUSE BUTTON3";
                }

                try
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (KeySelectionTextBox != null)
                            KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                        if (PushToTalkText != null)
                            PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";

                        if (defaultmicdevice != null)
                            SafeSetMute(true);
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            }

            bool shouldTalk = false;
            lock (mouseLock)
            {
                lock (keysLock)
                {
                    shouldTalk = pressedmouses.SetEquals(clickedmouses) && listening == 1 && pressedkeys.SetEquals(selectedkeys);
                }
            }

            if (shouldTalk)
            {
                lock (talkingLock)
                {
                    isTalking = true;
                }

                try
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (PushToTalkText != null)
                            PushToTalkText.Text = "Talking...";

                        if (defaultmicdevice != null)
                            SafeSetMute(false);
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private void MouseHook_MouseButton3Up(MSLLHOOKSTRUCT mouseStruct)
        {
            Thread t = new Thread(() =>
            {
                int sleeptime = 0;

                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (MsSettingTextBox != null && !string.IsNullOrEmpty(MsSettingTextBox.Text))
                        {
                            int.TryParse(MsSettingTextBox.Text, out sleeptime);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }

                Thread.Sleep(sleeptime);

                try
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        if (listening == 0)
                        {
                            selectedmousehook = 4;

                            if (KeySelectionTextBox != null)
                                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                            if (PushToTalkText != null)
                                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";

                            if (defaultmicdevice != null)
                                SafeSetMute(true);
                        }

                        bool shouldUnmute = false;
                        lock (mouseLock)
                        {
                            lock (keysLock)
                            {
                                shouldUnmute = pressedmouses.SetEquals(clickedmouses) && listening == 1 && pressedkeys.SetEquals(selectedkeys);
                            }
                        }

                        if (shouldUnmute)
                        {
                            lock (talkingLock)
                            {
                                isTalking = false;
                            }

                            if (PushToTalkText != null)
                                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";

                            if (defaultmicdevice != null)
                                SafeSetMute(true);
                        }

                        lock (mouseLock)
                        {
                            clickedmouses.Remove("MOUSE BUTTON3");
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            });

            t.IsBackground = true;
            t.Start();
        }

        private void MouseHook_RightButtonDown(MSLLHOOKSTRUCT mouseStruct)
        {
            lock (mouseLock)
            {
                clickedmouses.Add("MOUSE RIGHT BUTTON");
            }

            if (listening == 0)
            {
                selectedmousehook = 2;

                lock (mouseLock)
                {
                    if (!pressedmouses.Contains("MOUSE RIGHT BUTTON"))
                    {
                        pressedmouses.Add("MOUSE RIGHT BUTTON");
                    }
                }

                if (!selectedkeystring.Contains(" + MOUSE RIGHT BUTTON"))
                {
                    selectedkeystring += " + MOUSE RIGHT BUTTON";
                }

                try
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (KeySelectionTextBox != null)
                            KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                        if (PushToTalkText != null)
                            PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";

                        if (defaultmicdevice != null)
                            SafeSetMute(true);
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            }

            bool shouldTalk = false;
            lock (mouseLock)
            {
                lock (keysLock)
                {
                    shouldTalk = pressedmouses.SetEquals(clickedmouses) && listening == 1 && pressedkeys.SetEquals(selectedkeys);
                }
            }

            if (shouldTalk)
            {
                lock (talkingLock)
                {
                    isTalking = true;
                }

                try
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (PushToTalkText != null)
                            PushToTalkText.Text = "Talking...";

                        if (defaultmicdevice != null)
                            SafeSetMute(false);
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }

                lock (keysLock)
                {
                    pressedkeys.Clear();
                }
            }
        }

        private void MouseHook_RightButtonUp(MSLLHOOKSTRUCT mouseStruct)
        {
            Thread t = new Thread(() =>
            {
                int sleeptime = 0;

                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (MsSettingTextBox != null && !string.IsNullOrEmpty(MsSettingTextBox.Text))
                        {
                            int.TryParse(MsSettingTextBox.Text, out sleeptime);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }

                Thread.Sleep(sleeptime);

                try
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        if (listening == 0)
                        {
                            if (KeySelectionTextBox != null)
                                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                            if (PushToTalkText != null)
                                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";

                            if (defaultmicdevice != null)
                                SafeSetMute(true);
                        }

                        bool shouldUnmute = false;
                        lock (mouseLock)
                        {
                            shouldUnmute = pressedmouses.SetEquals(clickedmouses) && listening == 1;
                        }

                        if (shouldUnmute)
                        {
                            lock (talkingLock)
                            {
                                isTalking = false;
                            }

                            if (PushToTalkText != null)
                                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + "  to Talk";

                            if (defaultmicdevice != null)
                                SafeSetMute(true);
                        }

                        lock (mouseLock)
                        {
                            clickedmouses.Remove("MOUSE RIGHT BUTTON");
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            });

            t.IsBackground = true;
            t.Start();
        }

        private void MouseHook_MiddleButtonDown(MSLLHOOKSTRUCT mouseStruct)
        {
            lock (mouseLock)
            {
                clickedmouses.Add("MOUSE MIDDLE BUTTON");
            }

            if (listening == 0)
            {
                selectedmousehook = 3;

                lock (mouseLock)
                {
                    if (!pressedmouses.Contains("MOUSE MIDDLE BUTTON"))
                    {
                        pressedmouses.Add("MOUSE MIDDLE BUTTON");
                    }
                }

                if (!selectedkeystring.Contains(" + MOUSE MIDDLE BUTTON"))
                {
                    selectedkeystring += " + MOUSE MIDDLE BUTTON";
                }

                try
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (KeySelectionTextBox != null)
                            KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                        if (PushToTalkText != null)
                            PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";

                        lock (talkingLock)
                        {
                            isTalking = false;
                        }

                        if (defaultmicdevice != null)
                            SafeSetMute(true);
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            }

            bool shouldTalk = false;
            lock (mouseLock)
            {
                lock (keysLock)
                {
                    shouldTalk = pressedmouses.SetEquals(clickedmouses) && listening == 1 && pressedkeys.SetEquals(selectedkeys);
                }
            }

            if (shouldTalk)
            {
                lock (talkingLock)
                {
                    isTalking = true;
                }

                try
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (PushToTalkText != null)
                            PushToTalkText.Text = "Talking...";

                        if (defaultmicdevice != null)
                            SafeSetMute(false);
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }

                lock (keysLock)
                {
                    pressedkeys.Clear();
                }
            }
        }

        private void MouseHook_MiddleButtonUp(MSLLHOOKSTRUCT mouseStruct)
        {
            Thread t = new Thread(() =>
            {
                int sleeptime = 0;

                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (MsSettingTextBox != null && !string.IsNullOrEmpty(MsSettingTextBox.Text))
                        {
                            int.TryParse(MsSettingTextBox.Text, out sleeptime);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }

                Thread.Sleep(sleeptime);

                try
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        if (listening == 0)
                        {
                            selectedmousehook = 3;

                            if (KeySelectionTextBox != null)
                                KeySelectionTextBox.Text = $"Selecteds: {selectedkeystring}";

                            if (PushToTalkText != null)
                                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";

                            lock (talkingLock)
                            {
                                isTalking = false;
                            }

                            if (defaultmicdevice != null)
                                SafeSetMute(true);
                        }

                        bool shouldUnmute = false;
                        lock (mouseLock)
                        {
                            lock (keysLock)
                            {
                                shouldUnmute = pressedmouses.SetEquals(clickedmouses) && listening == 1 && pressedkeys.SetEquals(selectedkeys);
                            }
                        }

                        if (shouldUnmute)
                        {
                            lock (talkingLock)
                            {
                                isTalking = false;
                            }

                            if (PushToTalkText != null)
                                PushToTalkText.Text = $"Press and Hold " + selectedkeystring + " to Talk";

                            if (defaultmicdevice != null)
                                SafeSetMute(true);
                        }

                        lock (mouseLock)
                        {
                            clickedmouses.Remove("MOUSE MIDDLE BUTTON");
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            });

            t.IsBackground = true;
            t.Start();
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
               SafeSetMute(false);
          
            
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
            SafeSetMute(true);
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
                    SafeSetMute(false);

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
        private readonly object deviceLock = new object();

    
        private void SafeSetMute(bool muteState)
        {
            try
            {
                lock (deviceLock)
                {
                 


                   if (defaultmicdevice == null || defaultmicdevice.State != DeviceState.Active)
                   {
                        PushToTalkText.Text = "Device is not active";
                    
                   } else
                   {
                        defaultmicdevice.AudioEndpointVolume.Mute = muteState;
                   }

                   
                  
                }
            }
            catch (COMException ex)
            {
            
                PushToTalkText.Text = $"COM Exception while setting mute: {ex.Message} (0x{ex.HResult:X})";

                if (ex.HResult == unchecked((int)0x88890004))
                {
                    defaultmicdevice = null;
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (PushToTalkText != null)
                            PushToTalkText.Text = "Microphone disconnected! Please reselect device.";
                    }));
                }
              
            }
            catch (Exception ex)
            {
                PushToTalkText.Text = $"Unexpected error while setting mute: {ex.Message}";
                
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
