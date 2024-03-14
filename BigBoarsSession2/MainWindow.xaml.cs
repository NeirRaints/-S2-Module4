using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace BigBoarsSession2
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<PersonLocations> personLocationsList = new ObservableCollection<PersonLocations>(); // список, полученный с api
        private Random random = new Random();
        private DispatcherTimer timer;
        private Dictionary<string, PersonLocations> currentPersonLocations = new Dictionary<string, PersonLocations>(); // хранение текущих местоположений

        public MainWindow()
        {
            InitializeComponent();

            LoadPersonLocations();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        // что происходит каждый тик
        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateMap();
        }

        private void UpdateMap()
        {
            // Отображаем персонал на карте
            DisplayPeopleOnMap();
        }

        private async void LoadPersonLocations()
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "https://localhost:7177/api/persons";
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    IEnumerable<PersonLocations> newPersonLocations = JsonConvert.DeserializeObject<IEnumerable<PersonLocations>>(json);

                    personLocationsList.Clear();

                    foreach (var location in newPersonLocations)
                    {
                        personLocationsList.Add(location);
                    }

                    DisplayPeopleOnMap(); // Отобразить персонал на карте
                }
                else
                {
                    MessageBox.Show("Ошибка при загрузке данных");
                }
            }
        }


        // метод для отображения персонала на карте помещений
        private void DisplayPeopleOnMap()
        {
            if (personLocationsList.Any())
            {
                foreach (var person in personLocationsList)
                {
                    Canvas canvas = GetCanvasBySkudNumber(person.LastSecurityPointNumber); // Получаем Canvas по номеру помещения
                    if (canvas != null)
                    {
                        Border border = new Border();
                        Ellipse ellipse = new Ellipse();

                        ellipse.Width = 15;
                        ellipse.Height = 15;

                        SolidColorBrush brush = person.PersonRole == "Сотрудник" ? Brushes.Blue : Brushes.Green;
                        ellipse.Fill = brush;

                        border.Child = ellipse;
                        border.CornerRadius = new CornerRadius(7.5); // Половина высоты круга

                        Canvas.SetLeft(border, GetRandomXPosition(canvas, ellipse.Width));
                        Canvas.SetTop(border, GetRandomYPosition(canvas, ellipse.Height));
                        Canvas.SetZIndex(border, 1);

                        canvas.Children.Add(border);
                    }
                }
            }
        }

        // возвращать Canvas в зависимости от номера помещения
        private Canvas GetCanvasBySkudNumber(int skudNumber)
        {
            var canvas = FindCanvasRecursively(SkudsMap, skudNumber);

            if (canvas == null)
            {
                MessageBox.Show($"Canvas с именем Skud{skudNumber} не найден.");
            }

            return canvas;
        }

        private Canvas FindCanvasRecursively(DependencyObject parent, int skudNumber)
        {
            if (parent is Canvas canvas && canvas.Name == $"Skud{skudNumber}")
            {
                return canvas;
            }

            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindCanvasRecursively(child, skudNumber);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        // методы для генерации случайных координат в пределах Canvas
        private double GetRandomXPosition(Canvas canvas, double elementWidth)
        {
            return random.Next(0, (int)(canvas.ActualWidth - elementWidth));
        }

        private double GetRandomYPosition(Canvas canvas, double elementHeight)
        {
            return random.Next(0, (int)(canvas.ActualHeight - elementHeight));
        }
    }
}
