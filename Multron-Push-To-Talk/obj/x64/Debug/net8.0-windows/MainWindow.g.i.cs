﻿#pragma checksum "..\..\..\..\MainWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "B17D890310D28118F50EB72ECE8D7DC019B15828"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Multron_Push_To_Talk;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace Multron_Push_To_Talk {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    public partial class MainWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 45 "..\..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock PushToTalkText;
        
        #line default
        #line hidden
        
        
        #line 59 "..\..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox KeySelectionTextBox;
        
        #line default
        #line hidden
        
        
        #line 70 "..\..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox MicrophoneComboBox;
        
        #line default
        #line hidden
        
        
        #line 73 "..\..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox EnableTrayIconCheckBox;
        
        #line default
        #line hidden
        
        
        #line 74 "..\..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button StopListen;
        
        #line default
        #line hidden
        
        
        #line 75 "..\..\..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox Hider;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.8.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/Multron-Push-To-Talk;component/mainwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\MainWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.8.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 13 "..\..\..\..\MainWindow.xaml"
            ((Multron_Push_To_Talk.MainWindow)(target)).Loaded += new System.Windows.RoutedEventHandler(this.Window_Loaded);
            
            #line default
            #line hidden
            return;
            case 2:
            this.PushToTalkText = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 3:
            this.KeySelectionTextBox = ((System.Windows.Controls.TextBox)(target));
            
            #line 62 "..\..\..\..\MainWindow.xaml"
            this.KeySelectionTextBox.KeyDown += new System.Windows.Input.KeyEventHandler(this.KeySelectionTextBox_KeyDown555);
            
            #line default
            #line hidden
            
            #line 63 "..\..\..\..\MainWindow.xaml"
            this.KeySelectionTextBox.PreviewMouseDown += new System.Windows.Input.MouseButtonEventHandler(this.KeySelectionTextBox_PreviewMouseDown);
            
            #line default
            #line hidden
            return;
            case 4:
            this.MicrophoneComboBox = ((System.Windows.Controls.ComboBox)(target));
            
            #line 70 "..\..\..\..\MainWindow.xaml"
            this.MicrophoneComboBox.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.MicrophoneComboBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 5:
            this.EnableTrayIconCheckBox = ((System.Windows.Controls.CheckBox)(target));
            
            #line 73 "..\..\..\..\MainWindow.xaml"
            this.EnableTrayIconCheckBox.Checked += new System.Windows.RoutedEventHandler(this.EnableTrayIconCheckBox_Checked);
            
            #line default
            #line hidden
            
            #line 73 "..\..\..\..\MainWindow.xaml"
            this.EnableTrayIconCheckBox.Unchecked += new System.Windows.RoutedEventHandler(this.EnableTrayIconCheckBox_Unchecked);
            
            #line default
            #line hidden
            return;
            case 6:
            this.StopListen = ((System.Windows.Controls.Button)(target));
            
            #line 74 "..\..\..\..\MainWindow.xaml"
            this.StopListen.Click += new System.Windows.RoutedEventHandler(this.Button_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.Hider = ((System.Windows.Controls.CheckBox)(target));
            
            #line 75 "..\..\..\..\MainWindow.xaml"
            this.Hider.Checked += new System.Windows.RoutedEventHandler(this.MakeHider_Checked);
            
            #line default
            #line hidden
            
            #line 75 "..\..\..\..\MainWindow.xaml"
            this.Hider.Unchecked += new System.Windows.RoutedEventHandler(this.MakeCloser_Unchecked);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

