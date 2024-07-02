using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using ObservableCollections;
using R3;

namespace WpfApp;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    //ObservableList<int> list;
    //public INotifyCollectionChangedSynchronizedView<int> ItemsView { get; set; }


    public MainWindow()
    {
        InitializeComponent();


        WpfProviderInitializer.SetDefaultObservableSystem(x => { Trace.WriteLine(x); });

        DataContext = new ViewModel();


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
    public ViewModel()
    {
        observableList.Add(1);
        observableList.Add(2);

        ItemsView = observableList.CreateView().ToNotifyCollectionChanged();

        BindingOperations.EnableCollectionSynchronization(ItemsView, new object());

        // var iii = 10;
        ClearCommand.Subscribe(_ =>
        {
            // observableList.Add(iii++);
            observableList.Clear();
        });
    }

    private ObservableList<int> observableList { get; } = new();
    public INotifyCollectionChangedSynchronizedView<int> ItemsView { get; }
    public ReactiveCommand<Unit> ClearCommand { get; } = new();
}