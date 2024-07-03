using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Herakles.Modules.PlanningChantier.Resources.Observable;

public class ObservableDataColumnCollection : IReadOnlyCollection<DataColumn>
{
    private readonly List<DataColumn> _columns = new();

    public DataColumn? this[string colName]
    {
        get { return _columns.FirstOrDefault(x => x.ColumnName == colName); }
    }

    public DataColumn? this[int index] => _columns[index];

    public int Count => _columns.Count;

    public IEnumerator<DataColumn> GetEnumerator()
    {
        foreach (var col in _columns) yield return col;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    internal void Add(ObservableDataTable dataTable, DataColumn column)
    {
        _columns.Add(column);
        dataTable.Reset();
    }

    internal void Clear(ObservableDataTable dataTable)
    {
        _columns.Clear();
        dataTable.Reset();
    }

    public bool Contains(string colName)
    {
        return _columns.Exists(x => x.ColumnName == colName);
    }
}