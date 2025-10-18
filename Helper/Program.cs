using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;
using System.Windows.Forms;

internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Environment.ExitCode = 1;
        if (args.Length < 2)
        {
            Console.Error.WriteLine("No file/folder path provided.");
            return;
        }

        var path = args[0];
        bool file = File.Exists(path);
        bool folder = Directory.Exists(path);

        if (!file && !folder)
        {
            Console.Error.WriteLine("Invalid path.");
            return;
        }

        try
        {
            switch(args[1].ToLowerInvariant())
            {
                case "--copy":
                    Clipboard.SetData(DataFormats.FileDrop, new[] { path });
                    break;
                case "--as-text":
                    Clipboard.SetText(path);
                    break;
                case "--delete":
                    if (file)
                        FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    else if (folder)
                        FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    break;
                default:
                    throw new ArgumentException(args[1]+" : Argument not recognised");
            }
            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
    }
}
