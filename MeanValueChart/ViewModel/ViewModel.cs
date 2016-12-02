using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using MeanValueChart.Extensions;

namespace MeanValueChart.ViewModel
{
    public class ViewModel : MMVVM.ViewModelBase.ViewModelBase
    {
        public SeriesCollection SeriesCollection { get; set; }
        public Func<double, string> Formatter { get; set; }

        private List<DateTimeDataPoint> points { get; set; }

        public ViewModel()
        {
            /*points = new ObservableCollection<DateTimeDataPoint>()
            {
                new DateTimeDataPoint(new DateTime(2016,11,04,12,15,34), 43.2),
                new DateTimeDataPoint(new DateTime(2016,11,04,13,24,11), 24.5),
                new DateTimeDataPoint(new DateTime(2016,11,05,09,26,21), 13.2),
                new DateTimeDataPoint(new DateTime(2016,11,07,13,54,00), 33.7),
                new DateTimeDataPoint(new DateTime(2016,11,07,11,21,00), 36.3),
                new DateTimeDataPoint(new DateTime(2016,11,08,14,16,00), 28.4),
                new DateTimeDataPoint(new DateTime(2016,11,09,15,54,00), 42.9)
            };*/
            points = GenerateRandomValues(500, DateTime.Now);

            var meanPoints = CreateMeanPoints(points);

            var dayConfig = Mappers.Xy<DateTimeDataPoint>()
                .X(dayModel => (double)dayModel.DateTime.Ticks / TimeSpan.FromDays(1).Ticks)
                .Y(dayModel => dayModel.Value);


            var mean2Points = LowPassList(meanPoints);

            SeriesCollection = new SeriesCollection(dayConfig)
            {
                new ScatterSeries
                {
                    Values = new ChartValues<DateTimeDataPoint>(points)
                },
                new LineSeries
                {
                    Values = new ChartValues<DateTimeDataPoint>(meanPoints), Fill = Brushes.Transparent
                },
                new LineSeries
                {
                    Values = new ChartValues<DateTimeDataPoint>(mean2Points), Fill = Brushes.Transparent
                }

            };

            Formatter = value => new DateTime((long)(value * TimeSpan.FromDays(1).Ticks)).ToString("G");
        }


        enum MeanInterval { Days, Hours };
        private List<DateTimeDataPoint> CreateMeanPoints(List<DateTimeDataPoint> points, MeanInterval interval = MeanInterval.Days)
        {
            List<DateTimeDataPoint> averagePoints = new List<DateTimeDataPoint>();
            
            if (points.Count() == 0) return averagePoints;

            //TODO: Add functionality (or not) to handle means for both days and hours
            var firstDataPointTime = points.First().DateTime.Date;

            var iteration = 0;
            while (firstDataPointTime.AddDays(iteration) <= points.Last().DateTime.Date)
            {
                var iterationDate = firstDataPointTime.AddDays(iteration++);
                var iterationDatePoints = points.Where(x => x.DateTime.Date == iterationDate);
                if (iterationDatePoints.Count() > 0)
                {
                    var iterationAvg = iterationDatePoints.Select(dp => dp.Value).Average();
                    averagePoints.Add(new DateTimeDataPoint(iterationDate.AddDays(0.5), iterationAvg));
                }
            }

            return averagePoints;
        }

        private List<DateTimeDataPoint> LowPassList(List<DateTimeDataPoint> meanPoints)
        {
            var lowPassList = new List<DateTimeDataPoint>();
            int degree = 5, meanSize = meanPoints.Count;
            for (int i = 0; i < meanSize; ++i)
            {
                lowPassList.Add(new DateTimeDataPoint(meanPoints[i].DateTime, (meanPoints[Math.Max(i - 2, 0)].Value + meanPoints[Math.Max(i - 1, 0)].Value + meanPoints[i].Value + meanPoints[Math.Min(i + 1, meanSize - 1)].Value + meanPoints[Math.Min(i + 2, meanSize - 1)].Value) / degree));
            }
            return lowPassList;
        }





        static private List<DateTimeDataPoint> GenerateRandomValues(int number, DateTime startDate)
        {
            List<DateTimeDataPoint> randomValues = new List<DateTimeDataPoint>();

            Random r = new Random();

            var lastDate = startDate;
            for (int i = 0; i < number; i++)
            {
                var rndDateTime = lastDate.AddDays(Math.Abs(r.NextGaussian()) * 0.08);
                lastDate = rndDateTime;

                if (rndDateTime.DayOfWeek == DayOfWeek.Saturday || rndDateTime.DayOfWeek == DayOfWeek.Sunday)
                {
                    //No work on sat sun
                    i--;
                    continue;
                }

                var rndValue = (r.NextGaussian() * 3) + (i>(number/2)?20.0:16.0);

                randomValues.Add(new DateTimeDataPoint(rndDateTime, rndValue));
            }

            return randomValues;
        }
    }



    public class DateTimeDataPoint
    {
        public DateTime DateTime { get; private set; }
        public Double Value { get; set; }

        public DateTimeDataPoint(DateTime dateTime, Double value)
        {
            this.DateTime = dateTime;
            this.Value = value;
        }
    }
}
