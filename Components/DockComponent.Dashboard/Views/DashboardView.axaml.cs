using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Styling;
using DockComponent.Dashboard.ViewModels;
using ScottPlot;
using ScottPlot.Avalonia;
using System.Collections.Specialized;
using Avalonia;

namespace DockComponent.Dashboard.Views;

public partial class DashboardView : UserControl
{
    private AvaPlot? _pieChart;
    private AvaPlot? _barChart;

    public DashboardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        
        // Listen for theme changes
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(ActualThemeVariant))
        {
            OnThemeChanged();
        }
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Trigger chart updates when the view is loaded
        Dispatcher.UIThread.Post(() =>
        {
            UpdatePieChart();
            UpdateBarChart();
        });
    }

    private void OnThemeChanged()
    {
        // Refresh charts when theme changes to update colors
        Dispatcher.UIThread.Post(() =>
        {
            UpdatePieChart();
            UpdateBarChart();
        });
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DashboardViewModel viewModel)
        {
            // Find the chart controls
            _pieChart = this.FindControl<AvaPlot>("PieChart");
            _barChart = this.FindControl<AvaPlot>("BarChart");

            // Subscribe to collection changes
            if (viewModel.FileTypeStats != null)
            {
                viewModel.FileTypeStats.CollectionChanged += OnFileTypeStatsChanged;
            }

            if (viewModel.Files != null)
            {
                viewModel.Files.CollectionChanged += OnFilesChanged;
            }

            // Initialize charts
            InitializeCharts();

            // Update charts with initial data
            Dispatcher.UIThread.Post(() =>
            {
                UpdatePieChart();
                UpdateBarChart();
            });
        }
    }

    private void OnFileTypeStatsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(UpdatePieChart);
    }

    private void OnFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(UpdateBarChart);
    }

    private void InitializeCharts()
    {
        InitializePieChart();
        InitializeBarChart();
    }

    private void InitializePieChart()
    {
        if (_pieChart?.Plot == null) return;

        _pieChart.Plot.Clear();
        
        // Configure the plot appearance using current theme
        var bgColor = GetThemeBackgroundColor();
        var fgColor = GetThemeForegroundColor();
        
        _pieChart.Plot.FigureBackground.Color = bgColor;
        _pieChart.Plot.DataBackground.Color = bgColor;

        // Add a placeholder message
        var text = _pieChart.Plot.Add.Text("No data available", 0, 0);
        text.LabelFontColor = fgColor;
        text.LabelFontSize = 12;

        _pieChart.Refresh();
    }

    private void InitializeBarChart()
    {
        if (_barChart?.Plot == null) return;

        _barChart.Plot.Clear();
        
        // Configure the plot appearance using current theme
        var bgColor = GetThemeBackgroundColor();
        var fgColor = GetThemeForegroundColor();
        
        _barChart.Plot.FigureBackground.Color = bgColor;
        _barChart.Plot.DataBackground.Color = bgColor;

        // Add a placeholder message
        var text = _barChart.Plot.Add.Text("No data available", 0, 0);
        text.LabelFontColor = fgColor;
        text.LabelFontSize = 12;

        _barChart.Refresh();
    }

    private void UpdatePieChart()
    {
        if (_pieChart?.Plot == null || DataContext is not DashboardViewModel viewModel) return;

        _pieChart.Plot.Clear();
        
        // Configure the plot appearance using current theme
        var bgColor = GetThemeBackgroundColor();
        var fgColor = GetThemeForegroundColor();
        
        _pieChart.Plot.FigureBackground.Color = bgColor;
        _pieChart.Plot.DataBackground.Color = bgColor;

        var stats = viewModel.FileTypeStats.Take(8).ToList(); // Limit to top 8 for readability
        if (!stats.Any())
        {
            var text = _pieChart.Plot.Add.Text("No data available", 0, 0);
            text.LabelFontColor = fgColor;
            text.LabelFontSize = 12;
            _pieChart.Refresh();
            return;
        }

        // Prepare data for pie chart
        try
        {
            var values = stats.Select(s => (double)s.Count).ToArray();

            // Create pie chart - simplified for compatibility
            var pie = _pieChart.Plot.Add.Pie(values);
            
            // Set colors for different file types
            var colors = new ScottPlot.Color[]
            {
                ScottPlot.Color.FromHex("#22C55E"), // Green
                ScottPlot.Color.FromHex("#3B82F6"), // Blue
                ScottPlot.Color.FromHex("#F59E0B"), // Orange
                ScottPlot.Color.FromHex("#8B5CF6"), // Purple
                ScottPlot.Color.FromHex("#EF4444"), // Red
                ScottPlot.Color.FromHex("#06B6D4"), // Cyan
                ScottPlot.Color.FromHex("#F97316"), // Orange variant
                ScottPlot.Color.FromHex("#84CC16")  // Lime
            };

            // Apply colors to pie slices
            for (int i = 0; i < Math.Min(values.Length, colors.Length); i++)
            {
                if (i < pie.Slices.Count)
                {
                    pie.Slices[i].FillColor = colors[i];
                }
            }

            // Add legend with file extension labels
            var legend = _pieChart.Plot.ShowLegend();
            legend.BackgroundColor = GetThemeCardColor();
            legend.FontColor = fgColor;
            legend.OutlineColor = fgColor;

            // Set legend labels
            for (int i = 0; i < Math.Min(stats.Count, pie.Slices.Count); i++)
            {
                pie.Slices[i].LegendText = stats[i].Extension;
            }
        }
        catch (Exception ex)
        {
            // If pie chart fails, show a simple message
            var errorText = _pieChart.Plot.Add.Text($"Chart error: {ex.Message}", 0, 0);
            errorText.LabelFontColor = ScottPlot.Color.FromHex("#EF4444");
            errorText.LabelFontSize = 10;
        }

        _pieChart.Refresh();
    }

    private void UpdateBarChart()
    {
        if (_barChart?.Plot == null || DataContext is not DashboardViewModel viewModel) return;

        _barChart.Plot.Clear();
        
        // Configure the plot appearance using current theme
        var bgColor = GetThemeBackgroundColor();
        var fgColor = GetThemeForegroundColor();
        
        _barChart.Plot.FigureBackground.Color = bgColor;
        _barChart.Plot.DataBackground.Color = bgColor;

        var files = viewModel.Files.Where(f => f.Size > 0).OrderByDescending(f => f.Size).Take(10).ToList();
        if (!files.Any())
        {
            var text = _barChart.Plot.Add.Text("No data available", 0, 0);
            text.LabelFontColor = fgColor;
            text.LabelFontSize = 12;
            _barChart.Refresh();
            return;
        }

        try
        {
            // Prepare data
            var positions = Enumerable.Range(0, files.Count).Select(i => (double)i).ToArray();
            var sizes = files.Select(f => (double)f.Size).ToArray();

            // Create bar chart for file sizes
            var bars = _barChart.Plot.Add.Bars(positions, sizes);
            
            // Set bar colors
            foreach (var bar in bars.Bars)
            {
                bar.FillColor = ScottPlot.Color.FromHex("#3B82F6");
                bar.LineColor = ScottPlot.Color.FromHex("#1E40AF");
            }

            // Configure axes labels
            _barChart.Plot.Axes.Left.Label.Text = "File Size (bytes)";
            _barChart.Plot.Axes.Left.Label.ForeColor = fgColor;
            _barChart.Plot.Axes.Bottom.Label.Text = "Files";
            _barChart.Plot.Axes.Bottom.Label.ForeColor = fgColor;

            // Set axis colors and tick colors
            _barChart.Plot.Axes.Left.FrameLineStyle.Color = fgColor;
            _barChart.Plot.Axes.Bottom.FrameLineStyle.Color = fgColor;
            _barChart.Plot.Axes.Left.TickLabelStyle.ForeColor = fgColor;
            _barChart.Plot.Axes.Bottom.TickLabelStyle.ForeColor = fgColor;
            _barChart.Plot.Axes.Left.MajorTickStyle.Color = fgColor;
            _barChart.Plot.Axes.Bottom.MajorTickStyle.Color = fgColor;

            // Set tick labels for file names (abbreviated)
            var tickLabels = files.Select(f => f.Name.Length > 10 ? f.Name[..7] + "..." : f.Name).ToArray();
            _barChart.Plot.Axes.Bottom.SetTicks(positions, tickLabels);
        }
        catch (Exception ex)
        {
            // If bar chart fails, show a simple message
            var errorText = _barChart.Plot.Add.Text($"Chart error: {ex.Message}", 0, 0);
            errorText.LabelFontColor = ScottPlot.Color.FromHex("#EF4444");
            errorText.LabelFontSize = 10;
        }

        _barChart.Refresh();
    }

    private ScottPlot.Color GetThemeBackgroundColor()
    {
        // Determine if we're in dark mode
        var isDarkMode = ActualThemeVariant == ThemeVariant.Dark;
        
        // Use appropriate colors based on theme
        return isDarkMode ? 
            ScottPlot.Color.FromHex("#1E1E1E") :  // Dark theme background
            ScottPlot.Color.FromHex("#FFFFFF");   // Light theme background
    }

    private ScottPlot.Color GetThemeForegroundColor()
    {
        // Determine if we're in dark mode
        var isDarkMode = ActualThemeVariant == ThemeVariant.Dark;
        
        // Use appropriate colors based on theme
        return isDarkMode ? 
            ScottPlot.Color.FromHex("#CCCCCC") :  // Dark theme foreground
            ScottPlot.Color.FromHex("#1E1E1E");   // Light theme foreground
    }

    private ScottPlot.Color GetThemeCardColor()
    {
        // Determine if we're in dark mode
        var isDarkMode = ActualThemeVariant == ThemeVariant.Dark;
        
        // Use appropriate colors based on theme
        return isDarkMode ? 
            ScottPlot.Color.FromHex("#2D2D2D") :  // Dark theme card color
            ScottPlot.Color.FromHex("#F5F5F5");   // Light theme card color
    }
}