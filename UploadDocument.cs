using Microsoft.Extensions.Logging;
using DocumentUploadUtility.BusinessLogic;
using DocumentUploadUtility.RequestandResponse;
using Serilog;
namespace DocumentUploadUtility.BusinessLogic


{   
    public class UploadDocument

    {
        public int ProcessCustomerDocuments(DocumentUploadRequest request)
        {
            Log.Information("Fetching customer documents for CNIC {CNIC}", "12345");

            //List<CustomerDocuments> customerDocuments = Dochelper.GetCustomerDocuments(request.CNIC, request.DeviceImei);

              var docs = new List<CustomerDocuments>
           {
              new() { DocumentName = "CNIC Front", Document = "https://github.com/MuhammadTayyab789/abc/blob/main/1.jpg" },
              new() { DocumentName = "CNIC Back", Document = "https://github.com/MuhammadTayyab789/abc/blob/main/2.jpg" },
              new() { DocumentName = "Signature", Document = "https://github.com/MuhammadTayyab789/abc/blob/main/3.jpg" },
    // Miscellaneous missing intentionally
        };


            string script = Dochelper.DocumentUploadScriptbuilder(request.RapidCustomerId, docs, false);

            Log.Information($"Script: "+ script);


            // int rows = Dochelper.SaveCustomerImages(
            //     request.RapidCustomerId,
            //     customerDocuments,
            //     request.isAsaanAccountWithoutSignature                               
            // );           
            return 1;


        }


    }
}
