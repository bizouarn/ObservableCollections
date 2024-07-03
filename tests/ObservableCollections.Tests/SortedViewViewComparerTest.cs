using System.Collections.Generic;
using System.Collections.Specialized;

namespace ObservableCollections.Tests;

public class SortedViewViewComparerTest
{
    [Fact]
    public void ObserveIndex()
    {
        var list = new ObservableList<int>();

        var filter = new TestFilter<int>((value, view) => value % 2 == 0);
        list.Add(50);
        list.Add(10);
        
        list.Add(20);
        filter.CalledOnCollectionChanged[0].Action.Should().Be(NotifyCollectionChangedAction.Add);
        filter.CalledOnCollectionChanged[0].NewValue.Should().Be(20);
        filter.CalledOnCollectionChanged[0].NewView.Should().Be(new ViewContainer<int>(20));
        filter.CalledOnCollectionChanged[0].NewViewIndex.Should().Be(1);

        list.Remove(20);
        filter.CalledOnCollectionChanged[1].Action.Should().Be(NotifyCollectionChangedAction.Remove);
        filter.CalledOnCollectionChanged[1].OldValue.Should().Be(20);
        filter.CalledOnCollectionChanged[1].OldView.Should().Be(new ViewContainer<int>(20));
        filter.CalledOnCollectionChanged[1].OldViewIndex.Should().Be(1);

        list[1] = 999; // from 10(at 0 in original) to 999
        filter.CalledOnCollectionChanged[2].Action.Should().Be(NotifyCollectionChangedAction.Replace);
        filter.CalledOnCollectionChanged[2].NewValue.Should().Be(999);
        filter.CalledOnCollectionChanged[2].OldValue.Should().Be(10);
        filter.CalledOnCollectionChanged[2].NewView.Should().Be(new ViewContainer<int>(999));
        filter.CalledOnCollectionChanged[2].OldView.Should().Be(new ViewContainer<int>(10));
        filter.CalledOnCollectionChanged[2].NewViewIndex.Should().Be(1);
        filter.CalledOnCollectionChanged[2].OldViewIndex.Should().Be(0);
    }
}