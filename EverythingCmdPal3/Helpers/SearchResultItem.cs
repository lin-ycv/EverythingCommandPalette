using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal3.Helpers;

internal sealed partial class SearchResultItem : ListItem
{
    private readonly SearchResult _result;
    private readonly ResultIconCache _icons;
    private readonly CancellationToken _token;
    private int _requested;

    internal SearchResultItem(ICommand command, SearchResult result, ResultIconCache icons,
        CancellationToken token) : base(command)
    {
        (_result, _icons, _token) = (result, icons, token);
        base.Icon = new IconInfo(result.IsFolder ? "\uE8B7" : "\uE8A5");
    }

    public override IIconInfo? Icon
    {
        get
        {
            if (!_token.IsCancellationRequested && Interlocked.Exchange(ref _requested, 1) == 0)
                _ = Task.Run(LoadIconAsync);
            return base.Icon;
        }
        set => base.Icon = value;
    }

    private async Task LoadIconAsync()
    {
        try
        {
            var icon = await _icons.GetAsync(_result.FullPath, _result.IsFolder, _token);
            if (icon is null || _token.IsCancellationRequested) return;
            base.Icon = icon;
            if (Details is Details details) details.HeroImage = icon;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ExtensionHost.LogMessage($"ECP3: Icon unavailable: {ex.Message}"); }
    }
}
