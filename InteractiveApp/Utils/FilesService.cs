using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace InteractiveApp.Utility;

public interface IFilesUtility
{
    public Task<List<Bitmap>> OpenFileAsBitmaps();
    public Task<IStorageFile?> OpenFileAsync();
    public Task<IStorageFile?> SaveFileAsync();
}

public class FilesUtility : IFilesUtility
{
    private readonly TopLevel _target;

    public FilesUtility(TopLevel target)
    {
        _target = target;
    }

    public async Task<List<Bitmap>> OpenFileAsBitmaps()
    {
        List<Bitmap> bitmaps = new List<Bitmap>();

        var files = await _target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions());
        var file = files.Count >= 1 ? files[0] : null;

        if (file != null)
        {
            var filePath = Path.GetFullPath(file.Path.AbsolutePath);
            var fileSize = (await file.GetBasicPropertiesAsync()).Size;

            // do something with file size...

            HandleFile(ref bitmaps, filePath);     
        }
        
        return bitmaps;
    }

    private void HandleFile(ref List<Bitmap> bitmaps, string path)
    {
        string extension = Path.GetExtension(path).ToLower();

        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
            case ".png":
                bitmaps.Add(new Bitmap(path));
                break;
            case ".pdf":
                HandlePdf(ref bitmaps, path);
                break;
        }
    }

    private void HandlePdf(ref List<Bitmap> bitmaps, string path)
    {
        
    }

    public async Task<IStorageFile?> OpenFileAsync()
    {
        var files = await _target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open Text File",
            AllowMultiple = false
        });

        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IStorageFile?> SaveFileAsync()
    {
        return await _target.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save Text File"
        });
    }
}