# Optimizely (Episerver) Blob Provider for SQL Server
Store all blobs in the database instead of disk.

Easy deployment in load balanced environments, perfect for development teams, avoids committing your blobs into source control too.

Also works for Optimizely Commerce as the assets are stored as regular media.

## Requirements

Optimizely CMS 12 is required. For CMS 11 support, use previous version of the library.

## Installation

Install package via NuGet.
Configure blob provider in the [BlobProviders](https://docs.developers.optimizely.com/content-management-system/docs/configuring-cms#blobprovideroptions) options section.


```json
  "Episerver": {
    "CMS": {
      "BlobProviders" : {
        "DefaultProvider" : "sqlBlobProvider",
        "Providers" : {
          "sqlBlobProvider" : "EPiCode.SqlBlobProvider.SqlBlobProvider, EPiCode.SqlBlobProvider"
        }
      }
    }
  }
```

## Local Caching
The provider can cache files on local disk to decrease the number of database calls. Set `LoadFromDisk` to `true` on the blob configuration.


```json
  "Episerver": {
    "CMS": {
      "SqlBlobProvider": {
        "LoadFromDisk": true,
        "Path": "c:\\my_custom_location"
      },
      "BlobProviders" : {
        "DefaultProvider" : "sqlBlobProvider",
        "Providers" : {
          "sqlBlobProvider" : "EPiCode.SqlBlobProvider.SqlBlobProvider, EPiCode.SqlBlobProvider"
        }
      }
    }
  }
```

If you do not specify the `Path` property, it will default to `[appDataPath]\sqlProviderBlobs`

**Note!** Make sure the user configured for your application pool has write access to the local folder.

## Migration
If you are installing the SqlBlobProvider in an existing project that already uses the standard file blob provider, you will need to convert existing FileBlobs into SqlBlobs. This can easily be done with EPiCode.BlobConverter. This contains a scheduled job, which will convert all file blobs into the currently configured blob type. The conversion tool is not restricted to the SqlBlobProvider, so you could use it for other blob types as well.

## Usage
Used as any other blob provider. Install, and you are good to go.

**Important:** If you are going to serve a lot of large files on your site, memory consumption will probably increase as files will be loaded into server memory before being served.

## Building NuGet package

Open project directory (e.g., EPiCode.SqlBlobProvider) in command line and run `dotnet pack -c Release` command.

## Support
This is an unsupported module. Use at your own risk.
