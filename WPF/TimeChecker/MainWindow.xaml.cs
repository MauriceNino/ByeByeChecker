using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using System.Collections;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace TimeChecker
{
    public partial class MainWindow : Window
    {
        private string v_timeInput = "--:--";
        // Mo - Do 30 min pause
        private static int dayOfWeek = (int)(DateTime.Now).DayOfWeek;
        private static string todayWorktime;
        private bool alertButtonClicked = false;
        private string endTime;
        private string startTime;
        private Thread clockUpdateThread;
        private Thread alertFunctionThread;
        private bool windowClosed = false;
        private static readonly string settingsFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"TimeChecker\Settings.xml");
        private TimeCheckerSettings settings;

        private PercentShow pshow;

        public List<PercentMsg> percentMsgs = new List<PercentMsg>
        {
            new PercentMsg(){percent=30, msg="30% better get some coffee"},
            new PercentMsg(){percent=50, msg="50% half way m8"},
            new PercentMsg(){percent=70, msg="70% getting close...to the 10th coffee"},
            new PercentMsg(){percent=90, msg="90% !! get ready to say bye bye"}
        };

        public MainWindow()
        {

            /////////////////////////////////
            InitializeComponent();
            /////////////////////////////////

            this.loadSettings();

            clockUpdateThread = new Thread(() =>
            {
                while (true)
                {
                    Dispatcher.Invoke(() =>
                    {
                        actTimeOutput.Content = DateTime.Now.ToString("HH:mm:ss");
                    });
                    Thread.Sleep(1000);
                }
            });
            clockUpdateThread.Start();

            EventManager.RegisterClassHandler(typeof(TextBox),
            TextBox.KeyUpEvent,
            new System.Windows.Input.KeyEventHandler(TextBox_KeyUp));
            this.Top = System.Windows.SystemParameters.PrimaryScreenHeight - this.Height - 30 - 20;
            this.Left = System.Windows.SystemParameters.PrimaryScreenWidth - this.Width - 20;
            AlertButton.Background = new SolidColorBrush(Colors.White);
        }

        private void loadSettings()
        {
            System.IO.Directory.CreateDirectory(System.IO.Directory.GetParent(settingsFilePath).ToString());
            if (!File.Exists(settingsFilePath))
            {
                XDocument doc = new XDocument(new XElement("timeCheckerSettings",
                                                new XElement("timeFormatRegex", @"([0-9]|1[0-9]|2[0-3]):([0-9]|[1-5][0-9])"),
                                                new XElement("times",
                                                    new XElement("mon", @"08:48"),
                                                    new XElement("tue", @"08:48"),
                                                    new XElement("wed", @"08:48"),
                                                    new XElement("thu", @"08:48"),
                                                    new XElement("fri", @"05:18"),
                                                    new XElement("sat", @"00:00"),
                                                    new XElement("sun", @"00:00")),
                                                new XElement("autoStartWhenTyping", false)));

                doc.Save(settingsFilePath);
            }

            XmlDocument ldoc = new XmlDocument();
            ldoc.Load(settingsFilePath);

            XmlNode regex = ldoc.DocumentElement.SelectSingleNode("/timeCheckerSettings/timeFormatRegex");
            XmlNode mon = ldoc.DocumentElement.SelectSingleNode("/timeCheckerSettings/times/mon");
            XmlNode tue = ldoc.DocumentElement.SelectSingleNode("/timeCheckerSettings/times/tue");
            XmlNode wed = ldoc.DocumentElement.SelectSingleNode("/timeCheckerSettings/times/wed");
            XmlNode thu = ldoc.DocumentElement.SelectSingleNode("/timeCheckerSettings/times/thu");
            XmlNode fri = ldoc.DocumentElement.SelectSingleNode("/timeCheckerSettings/times/fri");
            XmlNode sat = ldoc.DocumentElement.SelectSingleNode("/timeCheckerSettings/times/sat");
            XmlNode sun = ldoc.DocumentElement.SelectSingleNode("/timeCheckerSettings/times/sun");
            XmlNode auto = ldoc.DocumentElement.SelectSingleNode("/timeCheckerSettings/autoStartWhenTyping");

            settings = new TimeCheckerSettings();

            settings.timeFormatRegex = regex.InnerText;

            settings.times = new string[] { mon.InnerText, tue.InnerText, wed.InnerText, thu.InnerText, fri.InnerText, sat.InnerText, sun.InnerText };
            todayWorktime = settings.times[dayOfWeek - 1];

            settings.autoStartWhenTyping = Convert.ToBoolean(auto.InnerText);

            mondayEntry.Text = settings.times[0];
            tuesdayEntry.Text = settings.times[1];
            wednsdayEntry.Text = settings.times[2];
            thursdayEntry.Text = settings.times[3];
            fridayEntry.Text = settings.times[4];
            saturdayEntry.Text = settings.times[5];
            sundayEntry.Text = settings.times[6];

            regexEntry.Text = settings.timeFormatRegex;

            autoSetWhenTypeCheckbox.IsChecked = settings.autoStartWhenTyping;
        }

        private void saveSettings()
        {
            XDocument doc = new XDocument(new XElement("timeCheckerSettings",
                                                   new XElement("timeFormatRegex", settings.timeFormatRegex),
                                                   new XElement("times",
                                                       new XElement("mon", settings.times[0]),
                                                       new XElement("tue", settings.times[1]),
                                                       new XElement("wed", settings.times[2]),
                                                       new XElement("thu", settings.times[3]),
                                                       new XElement("fri", settings.times[4]),
                                                       new XElement("sat", settings.times[5]),
                                                       new XElement("sun", settings.times[6])),
                                                   new XElement("autoStartWhenTyping", settings.autoStartWhenTyping)));

            doc.Save(settingsFilePath);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            if (pshow == null && !this.windowClosed)
            {
                pshow = new PercentShow();
                pshow.Show();
            }

            base.OnDeactivated(e);
        }

        protected override void OnActivated(EventArgs e)
        {
            if (pshow != null)
            {
                pshow.Close();
                pshow = null;
            }

            base.OnActivated(e);
        }

        private double timeStrToSeconds(string time)
        {
            return Convert.ToDouble(time.Split(':').Length == 3 ? time.Split(':')[0] : "") * 60 * 60 +
                Convert.ToDouble(time.Split(':').Length == 3 ? time.Split(':')[1] : "") * 60 +
                Convert.ToDouble(time.Split(':').Length == 3 ? time.Split(':')[2] : "");
        }

        private void timeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            v_timeInput = timeInput.Text;
            Regex timeFormatRegexMatcher = new Regex(settings.timeFormatRegex);

            if (timeFormatRegexMatcher.IsMatch(v_timeInput))
            {
                DateTime startTime_d;
                if (DateTime.TryParse(v_timeInput, out startTime_d))
                {
                    startTime = startTime_d.ToString("HH:mm");
                    endTime = startTime_d.Add(TimeSpan.Parse(todayWorktime)).ToString("HH:mm");

                    timeOutput.Content = endTime;
                }
                AlertButton.IsEnabled = true;
                if (settings.autoStartWhenTyping)
                {
                    if (alertFunctionThread != null && alertFunctionThread.IsAlive)
                    {
                        AlertButton_Click(null, null);
                        AlertButton_Click(null, null);
                    }
                    else
                    {
                        AlertButton_Click(null, null);
                    }
                }
            }
            else if (timeOutput != null && timeOutput.IsLoaded)
            {
                timeOutput.Content = "Invalid Input";
                AlertButton.IsEnabled = false;
            }

        }

        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape) this.Close();
            if (e.Key != System.Windows.Input.Key.Enter) return;

            Regex timeFormatRegexMatcher = new Regex(settings.timeFormatRegex);

            if (timeFormatRegexMatcher.IsMatch(v_timeInput))
            {
                AlertButton_Click(null, null);
            }
        }

        private void AlertButton_Click(object sender, RoutedEventArgs e)
        {
            alertButtonClicked = !alertButtonClicked;

            if (alertButtonClicked)
            {
                alertFunctionThread = new Thread(() =>
                {
                    Dispatcher.Invoke(() => { AlertButton.Background = new SolidColorBrush(Colors.LightGray); });
                    while (!DateTime.Now.ToString("HH:mm").Equals(endTime))
                    {
                        Thread.Sleep(1000);

                        double actperc =
                        (
                            (timeStrToSeconds(DateTime.Now.ToString("HH:mm:ss")) +
                                (timeStrToSeconds(endTime + ":0") < timeStrToSeconds(startTime + ":0") ?
                                    (timeStrToSeconds("24:00:0") - timeStrToSeconds(startTime + ":0")) :
                                    (timeStrToSeconds(startTime + ":0") * (-1))
                                )
                            ) / timeStrToSeconds(todayWorktime + ":0")
                        ) * 100;

                        Dispatcher.Invoke(() =>
                        {
                            percentOutput.Content = actperc >= 100.0 ? "100%" : actperc.ToString("0.00") + "%";
                            percentProgressBar.Value = (int)actperc;
                            taskbarPercentProgressBar.ProgressValue = actperc / 100;
                            if (pshow != null) pshow.percentShow.Text = actperc >= 100.0 ? "100%" : actperc.ToString("0.00") + "%";
                        });

                        foreach (PercentMsg msg in percentMsgs)
                        {
                            if (msg.percent == (int)actperc && !msg.alerted)
                            {
                                System.Windows.MessageBox.Show(msg.msg, "TimeChecker");
                                msg.alerted = true;
                            }
                        }
                    }
                    Dispatcher.Invoke(() =>
                    {
                        this.Topmost = true;
                    });
                    while (true)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Random rnd = new Random();

                            if ((int)rnd.Next(0, 2) == 0)
                            {
                                if (this.Left > 0 && this.Left < System.Windows.SystemParameters.PrimaryScreenWidth)
                                    this.Left += (int)rnd.Next(0, 2) == 0 ? 10 : -10;
                                else if (!(this.Left > 0) && this.Left < System.Windows.SystemParameters.PrimaryScreenWidth)
                                    this.Left += 10;
                                else if (this.Left > 0 && !(this.Left < System.Windows.SystemParameters.PrimaryScreenWidth))
                                    this.Left -= 10;
                            }
                            if ((int)rnd.Next(0, 2) == 0)
                            {
                                if (this.Top > 0 && this.Left < System.Windows.SystemParameters.PrimaryScreenHeight)
                                    this.Top += (int)rnd.Next(0, 2) == 0 ? 10 : -10;
                                else if (!(this.Top > 0) && this.Top < System.Windows.SystemParameters.PrimaryScreenHeight)
                                    this.Top += 10;
                                else if (this.Top > 0 && !(this.Top < System.Windows.SystemParameters.PrimaryScreenHeight))
                                    this.Top -= 10;
                            }
                        });

                        Thread.Sleep(50);
                    }
                });
                alertFunctionThread.Start();
            }
            else
            {
                AlertButton.Background = new SolidColorBrush(Colors.White);
                this.Topmost = false;
                alertFunctionThread.Abort();

                Dispatcher.Invoke(() =>
                {
                    percentOutput.Content = "0%";
                    percentProgressBar.Value = 0.0;
                    taskbarPercentProgressBar.ProgressValue = 0.0;
                });
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.windowClosed = true;

            if (pshow != null) pshow.Close();

            if (alertFunctionThread != null && alertFunctionThread.IsAlive)
                alertFunctionThread.Abort();
            if (clockUpdateThread != null && clockUpdateThread.IsAlive)
                clockUpdateThread.Abort();

        }

        private bool autoSetWhenTypeCheckboxState;
        private void settingsSaveButton_Click(object sender, RoutedEventArgs e)
        {
            settings.autoStartWhenTyping = autoSetWhenTypeCheckboxState;
            settings.times = new string[]{
                mondayEntry.Text,
                tuesdayEntry.Text,
                wednsdayEntry.Text,
                thursdayEntry.Text,
                fridayEntry.Text,
                saturdayEntry.Text,
                sundayEntry.Text
            };
            settings.timeFormatRegex = regexEntry.Text;

            this.saveSettings();
            this.loadSettings();
        }

        private void autoSetWhenTypeCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            autoSetWhenTypeCheckboxState = true;
        }

        private void autoSetWhenTypeCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            autoSetWhenTypeCheckboxState = false;
        }
    }
}