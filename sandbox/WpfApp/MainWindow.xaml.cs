using ObservableCollections;
using R3;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //ObservableList<int> list;
        //public INotifyCollectionChangedSynchronizedView<int> ItemsView { get; set; }




        public MainWindow()
        {
            InitializeComponent();


            R3.WpfProviderInitializer.SetDefaultObservableSystem(x =>
            {
                Trace.WriteLine(x);
            });

            this.DataContext = new ViewModel();




            //list = new ObservableList<int>();
            //list.AddRange(new[] { 1, 10, 188 });
            //ItemsView = list.CreateSortedView(x => x, x => x, comparer: Comparer<int>.Default).ToNotifyCollectionChanged();


            //BindingOperations.EnableCollectionSynchronization(ItemsView, new object());
        }

        //int adder = 99;

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    ThreadPool.QueueUserWorkItem(_ =>
        //    {
        //        list.Add(adder++);
        //    });
        //}

        //protected override void OnClosed(EventArgs e)
        //{
        //    ItemsView.Dispose();
        //}
    }

    public class ViewModel
    {
        private ObservableList<int> observableList { get; } = new ObservableList<int>();
        public INotifyCollectionChangedSynchronizedView<int> ItemsView { get; }
        public ReactiveCommand<Unit> ClearCommand { get; } = new ReactiveCommand<Unit>();

        public ViewModel()
        {
            observableList.Add(1);
            observableList.Add(2);

            ItemsView = observableList.CreateView(x => x).ToNotifyCollectionChanged();

            BindingOperations.EnableCollectionSynchronization(ItemsView, new object());

            // var iii = 10;
            ClearCommand.Subscribe(_ =>
            {
                // observableList.Add(iii++);
                observableList.Clear();
            });
        }
    }
}