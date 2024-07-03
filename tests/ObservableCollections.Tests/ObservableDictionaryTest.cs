using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace ObservableCollections.Tests
{
    public class ObservableDictionaryTest
    {
        [Fact]
        public void View()
        {
            var dict = new ObservableDictionary<int, int>();
            var view = dict.CreateView(x => new ViewContainer<int>(x.Value));

            dict.Add(10, -10); // 0
            dict.Add(50, -50); // 1
            dict.Add(30, -30); // 2
            dict.Add(20, -20); // 3
            dict.Add(40, -40); // 4

            void Equal(params int[] expected)
            {
                dict.Select(x => x.Value).OrderByDescending(x => x).Should().Equal(expected);
                view.Select(x => x.Value.Value).OrderByDescending(x => x).Should().Equal(expected);
            }

            Equal(-10, -20, -30, -40, -50);

            dict[99] = -100;
            Equal(-10, -20, -30, -40, -50, -100);

            dict[10] = -5;
            Equal(-5, -20, -30, -40, -50, -100);

            dict.Remove(20);
            Equal(-5, -30, -40, -50, -100);

            dict.Clear();
            Equal(new int[0]);
        }

        [Fact]
        public void ViewSorted()
        {
            var dict = new ObservableDictionary<int, int>();
            
            dict.Add(10, 10); // 0
            dict.Add(50, 50); // 1
            dict.Add(30, 30); // 2
            dict.Add(20, 20); // 3
            dict.Add(40, 40); // 4

            void Equal(params int[] expected)
            {
                dict.Select(x => x.Value).OrderBy(x => x).Should().Equal(expected);
            }

            Equal(10, 20, 30, 40, 50);

            dict[99] = 100;
            Equal(10, 20, 30, 40, 50, 100);

            dict[10] = -5;
            Equal(-5, 20, 30, 40, 50, 100);

            dict.Remove(20);
            Equal(-5, 30, 40, 50, 100);

            dict.Clear();
            Equal(new int[0]);
        }
        
        [Fact]
        public void FilterAndInvokeAddEvent()
        {
            var dict = new ObservableDictionary<int, int>();
            var view1 = dict.CreateView(x => new ViewContainer<int>(x.Value));
            var filter1 = new TestFilter2<int>((x, v) => x.Value % 2 == 0);

            dict.Add(10, -12); // 0
            dict.Add(50, -53); // 1
            dict.Add(30, -34); // 2
            dict.Add(20, -25); // 3
            dict.Add(40, -40); // 4
            
            view1.AttachFilter(filter1, true);

            filter1.CalledOnCollectionChanged.Count.Should().Be(5);
            filter1.CalledOnCollectionChanged[0].Action.Should().Be(NotifyCollectionChangedAction.Add);
            filter1.CalledOnCollectionChanged[0].NewValue.Key.Should().Be(10);
            filter1.CalledOnCollectionChanged[1].Action.Should().Be(NotifyCollectionChangedAction.Add);
            filter1.CalledOnCollectionChanged[1].NewValue.Key.Should().Be(50);
            filter1.CalledOnCollectionChanged[2].Action.Should().Be(NotifyCollectionChangedAction.Add);
            filter1.CalledOnCollectionChanged[2].NewValue.Key.Should().Be(30);
            filter1.CalledOnCollectionChanged[3].Action.Should().Be(NotifyCollectionChangedAction.Add);
            filter1.CalledOnCollectionChanged[3].NewValue.Key.Should().Be(20);
            filter1.CalledOnCollectionChanged[4].Action.Should().Be(NotifyCollectionChangedAction.Add);
            filter1.CalledOnCollectionChanged[4].NewValue.Key.Should().Be(40);

            filter1.CalledWhenTrue.Count.Should().Be(3);
            filter1.CalledWhenFalse.Count.Should().Be(2);
        }   
    }
}
