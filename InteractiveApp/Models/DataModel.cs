using System.Collections.Generic;
using System.Text;
using Avalonia;
using ReactiveUI;

namespace InteractiveApp.Models;

/// <summary>
/// data available globally
/// </summary>
public class DataModel : ReactiveObject
{
    public class DebugList : List<string>
    {
        public DebugList() {}
        public DebugList(DebugList dblist)
        : base()
        {
            this.AddRange(dblist);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in this)
            {
                sb.AppendLine(s);
            }
            return sb.ToString();
        }
    }

    // DEBUG VARIABLES
    private  DebugList _debugValue = new DebugList();
    private readonly object _lockObject = new object();

    public DebugList DebugValue
    {
        get => _debugValue;
        set
        {
            lock(_lockObject)
            {
                this.RaiseAndSetIfChanged(ref _debugValue, value);
            }
        }
    }
}