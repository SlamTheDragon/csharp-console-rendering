namespace Utilities.Data;

public class SharedData : Configure
{
    public new Properties Properties { get; set; }

    public SharedData() // Load
    { Properties = Configure.Properties; }

    public void Refresh() // Append New
    { Configure.Properties = Properties; }

    public new void Save()
    { Configure.Save(); }
}
