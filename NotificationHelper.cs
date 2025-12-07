using System;
using System.Windows;
using MaterialDesignThemes.Wpf;

namespace LessonsManager
{
    public static class NotificationHelper
    {
        public static void ShowSuccess(string message, int duration = 3000)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow && mainWindow.MainSnackbar != null)
                {
                    mainWindow.MainSnackbar.MessageQueue?.Enqueue(
                        message,
                        null,
                        null,
                        null,
                        false,
                        false,
                        TimeSpan.FromMilliseconds(duration));
                }
            });
        }

        public static void ShowError(string message, int duration = 4000)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow && mainWindow.MainSnackbar != null)
                {
                    mainWindow.MainSnackbar.MessageQueue?.Enqueue(
                        message,
                        null,
                        null,
                        null,
                        false,
                        true,
                        TimeSpan.FromMilliseconds(duration));
                }
            });
        }

        public static void ShowWarning(string message, int duration = 3500)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow && mainWindow.MainSnackbar != null)
                {
                    mainWindow.MainSnackbar.MessageQueue?.Enqueue(
                        message,
                        null,
                        null,
                        null,
                        true,
                        false,
                        TimeSpan.FromMilliseconds(duration));
                }
            });
        }
    }
}