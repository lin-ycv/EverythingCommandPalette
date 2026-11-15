using EverythingCmdPal3.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace EverythingCmdPal3.Helpers;

/*
 * REF: https://github.com/jiripolasek/PowerToys/blob/main/src/modules/cmdpal/ext/Microsoft.CmdPal.Ext.Indexer/Helpers/DataPackageHelper.cs
 * This is needed for drag and drop
*/
internal static class DataPackageHelper
{
    public static DataPackage? CreateDataPackageForPath(SearchResult r)
    {
        if (string.IsNullOrWhiteSpace(r.FullPath))
        {
            return null;
        }

        // Capture now; don't rely on listItem still being valid later.
        var title = Path.GetFileName(r.FullPath);
        var description = Path.GetDirectoryName(r.FullPath);
        var capturedPath = r.FullPath;

        var dataPackage = new DataPackage
        {
            RequestedOperation = DataPackageOperation.Copy,
            Properties =
            {
                Title = title,
                Description = description,
            },
        };
        dataPackage.SetText(capturedPath);

        // Expensive + only computed if the consumer asks for StorageItems.
        dataPackage.SetDataProvider(StandardDataFormats.StorageItems, async void (request) =>
        {
            var deferral = request.GetDeferral();
            try
            {
                var items = await TryGetStorageItemAsync(capturedPath).ConfigureAwait(false);
                if (items is not null)
                {
                    request.SetData(items);
                }

                // If null: just don't provide StorageItems. Text still works.
            }
            catch
            {
                // Swallow: better to provide partial data (text) than fail the whole package.
            }
            finally
            {
                deferral.Complete();
            }
        });

        return dataPackage;
    }

    private static async Task<IStorageItem[]?> TryGetStorageItemAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var file = await StorageFile.GetFileFromPathAsync(filePath);
                return [file];
            }

            if (Directory.Exists(filePath))
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(filePath);
                return [folder];
            }

            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }
}
