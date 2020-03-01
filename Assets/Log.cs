using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class Log
{
    string path;

    public Log()
    {
        path = Path.Combine(Application.streamingAssetsPath, "log.txt");
    }

    public void Print(string msg)
    {
        File.AppendAllLines(path, new[] { $"[{DateTime.Now.ToString("yyyyMMdd hhmmss")}] {msg}" });
        Debug.Log(msg);
    }
}
