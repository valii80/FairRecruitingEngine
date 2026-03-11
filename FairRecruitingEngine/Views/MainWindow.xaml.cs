using FairRecruitingEngine.ViewModels;
using System.Windows;
using System.Windows.Input;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FairRecruitingEngine.Views
{
    public partial class MainWindow : Window
    {
        private readonly string[] languages =
        {
            "Deutsch",
            "English",
            "Français",
            "Español",
            "Italiano",
            "Română"
        };

        private int progress = 0;
        private DispatcherTimer progressTimer;
        private string BuildProgressBar(int percent, string lang)
        {
            int totalBlocks = 14;
            int filledBlocks = (percent * totalBlocks) / 100;

            string bar = new string('█', filledBlocks) + new string('░', totalBlocks - filledBlocks);

            return $"🌍 Übersetze Analyse nach {lang}...\n{bar} {percent}%";
        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();

            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (this.DataContext is MainViewModel vm)
                    {
                        vm.PasteImageCommand.Execute(null);
                        if (vm.HasImage) e.Handled = true;
                    }
                }
            };
        }

        private void TranslateButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();

            foreach (var lang in languages)
            {
                var item = new MenuItem();
                item.Header = lang;

                item.Click += async (s, ev) =>
                {
                    var vm = (MainViewModel)DataContext;

                    // ORIGINAL ANALYSE SPEICHERN
                    string analysisText = vm.StatusMessage;

                    progress = 0;

                    vm.StatusMessage = BuildProgressBar(0, lang);

                    progressTimer = new DispatcherTimer();
                    progressTimer.Interval = TimeSpan.FromMilliseconds(120);

                    progressTimer.Tick += (ts, te) =>
                    {
                        if (progress < 95)
                        {
                            progress++;
                            vm.StatusMessage = BuildProgressBar(progress, lang);
                        }
                    };

                    progressTimer.Start();

                    // WICHTIG: HIER analysisText verwenden
                    string prompt = $"Translate the following text into {lang}. Return only the translated text.\n\n{analysisText}";

                    var client = new HttpClient();

                    var body = new
                    {
                        model = "llama3:8b",
                        prompt = prompt,
                        stream = false
                    };

                    var json = JsonSerializer.Serialize(body);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("http://localhost:11434/api/generate", content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    progressTimer.Stop();

                    var doc = JsonDocument.Parse(responseString);
                    string result = doc.RootElement.GetProperty("response").GetString();

                    vm.StatusMessage = $"Übersetzung abgeschlossen ✔\n\n{result}";
                };

                menu.Items.Add(item);
            }

            menu.IsOpen = true;
        }
    }
}