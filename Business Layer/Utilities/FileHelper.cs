using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Utilities
{
    public static class FileHelper
    {

        public static void InsertFileAfterRename(IFormFile file, string path, string updateName)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string[] oldfiles = Directory.GetFiles(path, updateName + ".*");
            foreach (string f in oldfiles)
            {
                System.IO.File.Delete(f);
            }

            string extension = Path.GetExtension(file.FileName);

            string fileName = updateName + extension;

            string fullPath = Path.Combine(path, fileName);

            using FileStream stream = new(fullPath, FileMode.Create);
            file.CopyTo(stream);
        }

        public static void InsertFileAtPath(IFormFile document, string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string fileName = document.FileName;
            string fullPath = Path.Combine(path, fileName);

            using FileStream stream = new(fullPath, FileMode.Create);
            document.CopyTo(stream);
        }

        public static void InsertFileForRequest(IFormFile file, string webRootPath, int requestId)
        {
            string requestFilePath = Path.Combine(webRootPath, "document", "request", requestId.ToString());

            InsertFileAtPath(file, requestFilePath);
        }

        public static void InsertFileForTimeSheetReceipt(IFormFile file, string webRootPath, int phyId, int timeSheetId, int recordId)
        {

            string receiptPath = Path.Combine(webRootPath, "document", "timesheet", $"physician{phyId}", timeSheetId.ToString());

            InsertFileAfterRename(file, receiptPath, recordId.ToString());
        }

    }
}
