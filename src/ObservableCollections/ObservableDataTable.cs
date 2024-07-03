using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using ObservableCollections;

namespace Herakles.Modules.PlanningChantier.Resources.Observable;

public class ObservableDataTable : Synchronized, INotifyCollectionChanged, INotifyPropertyChanged
{
    private static readonly PropertyChangedEventArgs _countPropertyChangedEventArgs = new("Count");
    public readonly ObservableDataColumnCollection Columns;
    public readonly string TableName = string.Empty;
    private DataTable _table;

    public ObservableDataTable()
    {
        lock (SyncRoot)
        {
            _table = new DataTable();
            Columns = new ObservableDataColumnCollection();
            _count = 0;
        }
    }

    public ObservableDataTable(string name)
    {
        lock (SyncRoot)
        {
            _table = new DataTable(name);
            TableName = name;
            Columns = new ObservableDataColumnCollection();
            _count = 0;
        }
    }

    public object SyncRoot { get; } = new();
    private int _count { get; set; }

    public DataRow? this[int index]
    {
        get
        {
            lock (SyncRoot)
            {
                return _table.Rows[index];
            }
        }
        set
        {
            lock (SyncRoot)
            {
                _table.Rows.RemoveAt(index);
                _table.Rows.InsertAt(value, index);
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, index));
            }
        }
    }

    public int Count => _count;

    public bool IsReadOnly => false;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;


    public event PropertyChangedEventHandler? PropertyChanged;


    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<DataRow> GetEnumerator()
    {
        lock (SyncRoot)
        {
            foreach (DataRow row in _table.Rows)
                yield return row;
        }
    }

    public void Add(DataRow row)
    {
        lock (SyncRoot)
        {
            _table.Rows.Add(row);
            _count++;
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, row, _table.Rows.Count - 1));
            PropertyChanged?.Invoke(this, _countPropertyChangedEventArgs);
        }
    }

    public void Clear()
    {
        lock (SyncRoot)
        {
            _table.Clear();
            _count = 0;
            Reset();
        }
    }

    public bool Contains(DataRow item)
    {
        lock (SyncRoot)
        {
            return _table.Rows.Contains(item);
        }
    }

    public void CopyTo(DataRow[] array, int arrayIndex)
    {
        lock (SyncRoot)
        {
            _table.Rows.CopyTo(array, arrayIndex);
            _count = _table.Rows.Count;
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, array, arrayIndex));
            PropertyChanged?.Invoke(this, _countPropertyChangedEventArgs);
        }
    }

    public bool Remove(DataRow item)
    {
        lock (SyncRoot)
        {
            var index = _table.Rows.IndexOf(item);
            _table.Rows.RemoveAt(index);
            _count--;
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            PropertyChanged?.Invoke(this, _countPropertyChangedEventArgs);
            return true;
        }
    }

    public void FullClear()
    {
        lock (SyncRoot)
        {
            if (string.IsNullOrEmpty(TableName))
                _table = new DataTable();
            else
                _table = new DataTable(TableName);
            _count = 0;
            Columns.Clear(this);
        }
    }

    public void Reset()
    {
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        PropertyChanged?.Invoke(this, _countPropertyChangedEventArgs);
    }

    public void AddColumn(DataColumn column)
    {
        lock (SyncRoot)
        {
            _table.Columns.Add(column);
            Columns.Add(this, column);
        }
    }

    public bool TryAddColumn(string columnName, Type? columnType)
    {
        lock (SyncRoot)
        {
            var ret = TryAddColumn(_table, columnName, columnType);
            if (ret)
                Columns.Add(this, _table.Columns[columnName]);
            return ret;
        }
    }

    private bool TryAddColumn(DataTable dataTable, string columnName, Type? columnType)
    {
        try
        {
            if (columnType is {IsGenericType: true}
                && columnType.GetGenericTypeDefinition() == typeof(Nullable<>))
                columnType = Nullable.GetUnderlyingType(columnType);

            // Vérifier si le nom est vide ou si la colonne existe déjà
            if (string.IsNullOrWhiteSpace(columnName)
                || dataTable.Columns.Contains(columnName))
                return false; // La colonne existe déjà ou le nom est invalide

            // Ajouter la colonne à la DataTable
            if (columnType == null)
                dataTable.Columns.Add(new DataColumn(columnName));
            else
                dataTable.Columns.Add(columnName, columnType);
        }
        catch (Exception ex) when (ex is DuplicateNameException
                                       or ArgumentNullException
                                       or ArgumentException
                                       or InvalidExpressionException)
        {
            return false; // Échec d'ajout de la colonne
        }

        return true; // Ajout de la colonne réussi
    }

    public void AddRange(IList<DataRow> rows)
    {
        lock (SyncRoot)
        {
            foreach (var row in rows)
            {
                _table.Rows.Add(row);
                _count++;
            }

            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, rows,
                    _table.Rows.Count - rows.Count));
            PropertyChanged?.Invoke(this, _countPropertyChangedEventArgs);
        }
    }

    public DataRow NewRow()
    {
        lock (SyncRoot)
        {
            return _table.NewRow();
        }
    }
}