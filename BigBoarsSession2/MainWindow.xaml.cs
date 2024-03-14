using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace BigBoarsSession2
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<PersonLocations> personLocationsList = new ObservableCollection<PersonLocations>();

        public MainWindow()
        {
            InitializeComponent();

            LoadPersonLocations();
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
                    IEnumerable<PersonLocations> personLocations = JsonConvert.DeserializeObject<IEnumerable<PersonLocations>>(json);

                    foreach (var personLocation in personLocations)
                    {
                        personLocationsList.Add(personLocation);
                    }

                    // Привязать personLocationsList к элементу управления WPF
                    // Например:
                    TestGrid.ItemsSource = personLocationsList;
                }
                else
                {
                    MessageBox.Show("Ошибка при загрузке данных");
                }
            }
        }
    }
}
