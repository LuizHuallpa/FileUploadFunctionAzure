using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;

namespace FileUploadFunction
{
    public static class Function1
    {
        [FunctionName("FileUpload")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            try
            {
                var file = req.Form.Files.GetFile("file");
                if (file == null)
                {
                    return new BadRequestObjectResult("File is missing in the request.");
                }

                var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                if (string.IsNullOrEmpty(storageConnectionString))
                {
                    return new BadRequestObjectResult("AzureWebJobsStorage connection string is missing.");
                }

                // Create a CloudStorageAccount object using the connection string
                var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

                // Create a CloudBlobClient object to interact with the Blob Storage service
                var blobClient = storageAccount.CreateCloudBlobClient();

                var containerName = Environment.GetEnvironmentVariable("ContainerName");
                var container = blobClient.GetContainerReference(containerName);

                await container.CreateIfNotExistsAsync();

                var fileName = file.FileName;

                // Create a CloudBlockBlob object to represent the file in Azure Blob Storage
                var blob = container.GetBlockBlobReference(fileName);

                using (var fileStream = file.OpenReadStream())
                {
                    await blob.UploadFromStreamAsync(fileStream);
                }

                return new OkObjectResult($"File uploaded successfully. Blob URL: {blob.Uri}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while uploading the file.");
                return new StatusCodeResult(500);
            }
        }
    }
}
