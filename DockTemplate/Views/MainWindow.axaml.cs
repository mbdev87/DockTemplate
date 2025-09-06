using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using DockTemplate.ViewModels;
using NLog;

namespace DockTemplate.Views;

public partial class MainWindow : Window
{
    private bool _isDark = false;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public MainWindow()
    {
        InitializeComponent();
        InitializePlatformSpecificUI();
        InitializeThemes();
        InitializeDragDrop();
    }

    private void InitializePlatformSpecificUI()
    {
        // Hide in-window menu on macOS (native menu will be used instead)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var menuBarGrid = this.FindControl<Grid>("MenuBarGrid");
            var mainGrid = this.FindControl<Grid>("MainGrid");
            
            if (menuBarGrid != null)
            {
                menuBarGrid.IsVisible = false;
            }
            
            // Set the first row (menu row) height to 0 on macOS
            if (mainGrid != null && mainGrid.RowDefinitions.Count > 0)
            {
                mainGrid.RowDefinitions[0].Height = new GridLength(0);
            }
            
            Logger.Info("macOS detected - using native menu bar");
        }
        else
        {
            Logger.Info($"Platform detected: {RuntimeInformation.OSDescription} - using in-window menu");
        }
    }

    private void InitializeThemes()
    {
        if (ThemeButton is not null)
        {
            ThemeButton.Click += (_, _) =>
            {
                _isDark = !_isDark;
                App.ThemeService?.Switch(_isDark ? 1 : 0);
            };
        }
    }

    private void InitializeDragDrop()
    {
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (IsPluginFile(e))
        {
            e.DragEffects = DragDropEffects.Copy;
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ShowDropOverlay = true;
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ShowDropOverlay = false;
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (!IsPluginFile(e))
            {
                viewModel.ShowDropOverlay = false;
                return;
            }

            var files = e.Data.GetFiles()?.Select(f => f.Path.LocalPath).Where(f => !string.IsNullOrEmpty(f)).ToArray();
            if (files == null || !files.Any())
            {
                viewModel.ShowDropOverlay = false;
                return;
            }

            // Immediately start spinner on UI thread
            viewModel.ShowSpinner = true;
            viewModel.InstallStatusText = "Installing plugin...";
            viewModel.InstallSubText = "Preparing installation";
            
            Logger.Info("Starting plugin installation UI sequence");

            foreach (var file in files)
            {
                if (IsPluginFile(file))
                {
                    Logger.Info($"Sending plugin installation request: {System.IO.Path.GetFileName(file)}");
                    
                    // Send message to background service to handle installation
                    ReactiveUI.MessageBus.Current.SendMessage(new DockTemplate.Messages.InstallPluginMessage(file));
                }
            }
        }
    }

    private bool IsPluginFile(DragEventArgs e)
    {
        var files = e.Data.GetFiles()?.Select(f => f.Path.LocalPath).Where(f => !string.IsNullOrEmpty(f)).ToArray();
        return files?.Any(IsPluginFile) == true;
    }

    private bool IsPluginFile(string filePath)
    {
        var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".dockplugin" || extension == ".zip";
    }


}