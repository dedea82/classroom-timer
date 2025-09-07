using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ClassroomTimerApp
{
    public partial class MainWindow : Window
    {
        class ClassroomTimer
        {
            public string? Description;
            public DateTime StartTime;
            public DateTime EndTime;
        }

        readonly DispatcherTimer Timer;
        readonly double TotalDots;
        readonly ClassroomTimer[] ClassroomTimers;

        public MainWindow()
        {
            InitializeComponent();
            IConfiguration Config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            Timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(Config.GetValue<int>("ClassroomTimer:RefreshSeconds"))
            };
            Timer.Tick += Timer_Tick;

            TotalDots = Config.GetValue<double>("ClassroomTimer:Dots");
            ClassroomTimers = [.. Config.GetSection("ClassroomTimer:Timers").GetChildren()
                .Select(p => new ClassroomTimer
                {
                    Description = p.GetValue<string>("Description"),
                    StartTime = DateTime.Now.Date.Add(TimeSpan.Parse(p.GetValue<string?>("StartTime") ?? "")),
                    EndTime = DateTime.Now.Date.Add(TimeSpan.Parse(p.GetValue<string?>("EndTime") ?? ""))
                })];
        }

        private void UI_Refresh()
        {
            ClassroomTimer? currentTimer =
                ClassroomTimers.Where(p => DateTime.Now > p.StartTime && DateTime.Now <= p.EndTime)
                .FirstOrDefault();
            if (currentTimer != null)
            {
                double steps = currentTimer.EndTime.Subtract(currentTimer.StartTime).TotalMinutes;
                double currentStep = DateTime.Now.Subtract(currentTimer.StartTime).TotalMinutes - 1;
                UI_Refresh(TotalDots, steps, currentStep, currentTimer.Description ?? "");
            }
            else UI_Clear();
        }
        private void UI_Refresh(double dots, double steps, double currentStep, string description)
        {
            double currentDot = (currentStep / steps) * dots;
            if (WindowCanvas.Children.Count > currentDot + 1)
                WindowCanvas.Children.Clear();

            if (WindowCanvas.Children.Count < currentDot)
            {
                double circleMargin = TemplateCanvas.Margin.Top;
                var template = TemplateCanvas.Children[0];
                var xaml = System.Windows.Markup.XamlWriter.Save(template);

                int startingStep = WindowCanvas.Children.Count;
                for (int i = startingStep; i < currentDot; i++)
                {
                    double angle = (i / dots) * Math.PI * 2;
                    var deepCopy = (Shape)System.Windows.Markup.XamlReader.Parse(xaml);
                    deepCopy.SetValue(Canvas.LeftProperty, (circleMargin / 2d) + ((this.Width - deepCopy.Width - circleMargin) / 2d) * (1 - Math.Sin(angle)));
                    deepCopy.SetValue(Canvas.TopProperty, (circleMargin / 2d) + ((this.Height - deepCopy.Height - circleMargin) / 2d) * (1 - Math.Cos(angle)));
                    WindowCanvas.Children.Add(deepCopy);
                }
            }
            TextCurrentStesp.Text = ((int)(steps - currentStep)).ToString();
            TextStepDescription.Text = description;
        }
        private void UI_Clear()
        {
            WindowCanvas.Children.Clear();
            TextCurrentStesp.Text = "-";
            TextStepDescription.Text = "";
        }

        private void Timer_Tick(object? sender, EventArgs e) { UI_Refresh(); }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var workingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = workingArea.Right - this.Width;
            this.Top = workingArea.Bottom - this.Height;
            this.Timer.Start();
            UI_Refresh();
        }
        private void Window_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.Left == 0)
            {
                var workingArea = System.Windows.SystemParameters.WorkArea;
                this.Left = workingArea.Right - this.Width;
            }
            else this.Left = 0;
        }


    }
}
