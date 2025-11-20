using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EverythingCmdPal.Commands;
using EverythingCmdPal.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal;

public partial class EverythingFallbackItem(int offset) 
    : FallbackCommandItem(new NoOpCommand(), "Everything"), IDisposable
{
    CancellationTokenSource cts = new CancellationTokenSource();
    Lock _lock = new Lock();

    public override void UpdateQuery(string query)
    {
        ExtensionHost.LogMessage($"Starting query {query} for {offset}");

        // Trying to prevent long searches, probably not good for actual implementation.
        if(query.Length < 3)
        {
            SetData(null);
            return;
        }
        
        // Dunno if we actually need to lock here, as Query should handle multi threading, but not sure.
        lock (_lock)
        {
            cts.Cancel();
            cts.Dispose();
            cts = new();
        }

        List<Result> results = Query.Search(query, cts.Token);
        var r = cts.Token.IsCancellationRequested ? null : results.Skip(offset).FirstOrDefault();

        SetData(r);
    }

    private void SetData(Result? r)
    {
        Title = r?.FileName!;
        Subtitle = r?.FilePath!;
        Icon = r?.Icon;
        Command = r is null ? new NoOpCommand() : new OpenCommand(r.FullName);
        MoreCommands = r is null ? [] : new CommandHandler().LoadCommands(r.FullName, r.IsFolder);
    }

    public void Dispose()
    {
        cts.Dispose();
        GC.SuppressFinalize(this);
    }
}