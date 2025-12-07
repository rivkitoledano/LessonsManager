using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace LessonsManager
{
    public enum MessageDialogResult
    {
        None,
        OK,
        Yes,
        No,
        Cancel
    }

    public enum MessageType
    {
        Info,
        Warning,
        Error,
        Question,
        Success
    }

    public enum MessageButtons
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel
    }

    public partial class CustomMessageBox : Window
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public MessageType MessageType { get; set; }
        public MessageButtons Buttons { get; set; }
        public MessageDialogResult Result { get; private set; } = MessageDialogResult.None;

        public PackIconKind IconKind
        {
            get
            {
                return MessageType switch
                {
                    MessageType.Info => PackIconKind.InformationOutline,
                    MessageType.Warning => PackIconKind.AlertOutline,
                    MessageType.Error => PackIconKind.AlertCircleOutline,
                    MessageType.Question => PackIconKind.QuestionMarkCircleOutline,
                    MessageType.Success => PackIconKind.CheckCircleOutline,
                    _ => PackIconKind.InformationOutline
                };
            }
        }

        public Brush IconBrush
        {
            get
            {
                return MessageType switch
                {
                    MessageType.Info => new SolidColorBrush(Colors.DodgerBlue),
                    MessageType.Warning => new SolidColorBrush(Colors.Orange),
                    MessageType.Error => new SolidColorBrush(Colors.IndianRed),
                    MessageType.Question => new SolidColorBrush(Colors.DodgerBlue),
                    MessageType.Success => new SolidColorBrush(Colors.ForestGreen),
                    _ => new SolidColorBrush(Colors.DodgerBlue)
                };
            }
        }

        public bool ShowOkButton => Buttons == MessageButtons.OK || Buttons == MessageButtons.OKCancel;
        public bool ShowYesButton => Buttons == MessageButtons.YesNo || Buttons == MessageButtons.YesNoCancel;
        public bool ShowNoButton => Buttons == MessageButtons.YesNo || Buttons == MessageButtons.YesNoCancel;
        public bool ShowCancelButton => Buttons == MessageButtons.OKCancel || Buttons == MessageButtons.YesNoCancel;

        public CustomMessageBox(string title, string message, MessageType messageType = MessageType.Info, MessageButtons buttons = MessageButtons.OK)
        {
            Title = title;
            Message = message;
            MessageType = messageType;
            Buttons = buttons;
            
            InitializeComponent();
            DataContext = this;
            
            // Apply icon color
            var icon = (PackIcon)FindName("MessageIcon");
            if (icon != null)
            {
                icon.Foreground = IconBrush;
            }
            
            // Set initial focus
            Loaded += (s, e) =>
            {
                var defaultButton = FindName("OkButton") as Button ?? 
                                  FindName("YesButton") as Button;
                defaultButton?.Focus();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.OK;
            DialogResult = true;
            Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.Yes;
            DialogResult = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.No;
            DialogResult = false;
            Close();
        }

        public new MessageDialogResult ShowDialog()
        {
            base.ShowDialog();
            return Result;
        }

        public static MessageDialogResult Show(string title, string message, MessageType messageType = MessageType.Info, MessageButtons buttons = MessageButtons.OK)
        {
            var dialog = new CustomMessageBox(title, message, messageType, buttons);
            dialog.ShowDialog();
            return dialog.Result;
        }

        public static async Task<MessageDialogResult> ShowAsync(string title, string message, MessageType messageType = MessageType.Info, MessageButtons buttons = MessageButtons.OK)
        {
            var dialog = new CustomMessageBox(title, message, messageType, buttons);
            await Application.Current.Dispatcher.InvokeAsync(() => dialog.ShowDialog());
            return dialog.Result;
        }
    }
}
