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

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        // что происходит каждый тик
        private void Timer_Tick(object sender, EventArgs e)
        {
            LoadPersonLocationsFromApi();
        }

        // получить данные из api
        private async void LoadPersonLocationsFromApi()
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

                    LoadPersonLocations(); // Выполнить проверки по текущим местоположениям
                }
                else
                {
                    MessageBox.Show("Ошибка при загрузке данных с API.");
                }
            }
        }

        // проверка персон на карте
        private void LoadPersonLocations()
        {
            var currentTime = DateTime.Now;

            List<PersonLocations> personsToRemove = new List<PersonLocations>();
            List<PersonLocations> newPersonsToShow = new List<PersonLocations>();

            foreach (var location in personLocationsList.ToList())
            {
                if (location.LastSecurityPointDirection == "in" && location.PersonCode.EndsWith("A"))
                {
                    string oppositePersonCode = GetOppositePersonCode(location.PersonCode);
                    var oppositeLocation = personLocationsList.FirstOrDefault(l => l.PersonCode == oppositePersonCode && l.LastSecurityPointDirection == "out");

                    if (oppositeLocation != null && DateTime.Parse(oppositeLocation.LastSecurityPointTime) < currentTime)
                    {
                        personsToRemove.Add(location);
                    }
                    else if (DateTime.Parse(location.LastSecurityPointTime) < currentTime)
                    {
                        newPersonsToShow.Add(location);
                    }
                }
            }

            // Удаление точек, которые не должны отображаться
            foreach (var personToRemove in personsToRemove)
            {
                personLocationsList.Remove(personToRemove);
                currentPersonLocations.Remove(personToRemove.PersonCode); // Удаляем из currentPersonLocations
            }

            // Добавляем новые точки для отображения
            foreach (var newPerson in newPersonsToShow)
            {
                personLocationsList.Add(newPerson);
                currentPersonLocations[newPerson.PersonCode] = newPerson;
            }

            DisplayPeopleOnMap();
        }

        // Метод для получения противоположного идентификатора
        private string GetOppositePersonCode(string personCode)
        {
            return personCode.EndsWith("A") ? personCode.Replace("A", "B") : personCode.Replace("B", "A");
        }

        // метод для отображения персонала на карте помещений
        private void DisplayPeopleOnMap()
        {
            ClearMap(); // Очистить карту от старых точек

            foreach (var person in personLocationsList)
            {
                Canvas canvas = GetCanvasBySkudNumber(person.LastSecurityPointNumber);
                if (canvas != null)
                {
                    foreach (var location in currentPersonLocations.Values)
                    {
                        if (location.LastSecurityPointNumber == person.LastSecurityPointNumber)
                        {
                            if (!IsPersonAlreadyOnMap(canvas, location))
                            {
                                AddPersonToMap(canvas, location);
                            }
                        }
                    }
                }
            }
        }

        //Очистка карты
        private void ClearMap()
        {
            ClearRoomElements(SkudsMap);
        }

        private void ClearRoomElements(Canvas canvas)
        {
            foreach (StackPanel stackPanel in canvas.Children.OfType<StackPanel>())
            {
                foreach (Canvas roomCanvas in stackPanel.Children.OfType<Canvas>())
                {
                    roomCanvas.Children.Clear();
                }
            }
        }

        // проверка на наличие персоны на карте
        private bool IsPersonAlreadyOnMap(Canvas canvas, PersonLocations person)
        {
            foreach (Border border in canvas.Children.OfType<Border>())
            {
                // Проверяем наличие персонала с таким же PersonCode на данном Canvas
                if (border.Tag.ToString() == person.PersonCode)
                {
                    return true;
                }
            }
            return false;
        }

        // добавить персону на карту
        private void AddPersonToMap(Canvas canvas, PersonLocations person)
        {
            if (currentPersonLocations.ContainsKey(person.PersonCode))
            {
                // Убедимся, что персонаж еще не отображается на карте
                if (!IsPersonAlreadyOnMap(canvas, person))
                {
                    // Добавляем персонажа на карту
                    Border border = new Border();
                    Ellipse ellipse = new Ellipse();

                    ellipse.Width = 15;
                    ellipse.Height = 15;

                    SolidColorBrush brush = person.PersonRole == "Сотрудник" ? Brushes.Blue : Brushes.Green;
                    ellipse.Fill = brush;

                    border.Child = ellipse;
                    border.CornerRadius = new CornerRadius(7.5);
                    border.Tag = person.PersonCode;

                    Canvas.SetLeft(border, GetRandomXPosition(canvas, ellipse.Width));
                    Canvas.SetTop(border, GetRandomYPosition(canvas, ellipse.Height));
                    Canvas.SetZIndex(border, 1);

                    canvas.Children.Add(border);
                }
            }
        }

        // возвращать Canvas в зависимости от номера помещения
        private Canvas GetCanvasBySkudNumber(int skudNumber)
        {
            var canvas = FindCanvasRecursively(SkudsMap, skudNumber);
            return canvas;
        }

        // найти нужный канвас среди всех элементов 
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
